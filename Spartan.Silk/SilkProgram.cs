using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;
using StbImageSharp;
using TestProgram;
using static System.Net.Mime.MediaTypeNames;

namespace Spartan.Silk
{
    public class SilkProgram
    {
        private static IView _window;
        private GL _gl;
        private uint _quadProgram;
        private int _worldLoc;
        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private int _texLoc;
        private uint _fontTex;
        private ImageResult _font;
        private Vector2 _fontSize;
        private TestProgram1 _program;
        private SilkBlitter _blitter;
        private double _time;
        private float _zoom;

        public void Run(float zoom = 1)
        {
            var options = ViewOptions.Default;
            //options.Size = new Vector2D<int>(800, 600);
            //options.Title = "Spartan Silk";
            options.API = new GraphicsAPI(ContextAPI.OpenGLES, ContextProfile.Compatability, ContextFlags.Default,
                new APIVersion(2, 0));

            _zoom = zoom;
            _window = Window.GetView(options);
            _window.Load += OnLoad;
            _window.Render += OnRender;
            _window.Update += Update;
            _window.FramebufferResize += OnFramebufferResize;

            _window.Run();
        }

        private void BuildInput()
        {
            IInputContext input = _window.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
            {
                input.Keyboards[i].KeyChar += OnKeyChar;
                input.Keyboards[i].KeyDown += OnKeyDown;
            }


            foreach (var mouse in input.Mice)
            {
                mouse.MouseMove += MouseOnMouseMove;
                mouse.MouseDown += MouseOnMouseDown;
                mouse.MouseUp += MouseOnMouseUp;
                mouse.Scroll += MouseOnScroll;
            }

            _blitter.Input.SetToClipboard += text =>
            {
                foreach (var keyboard in input.Keyboards)
                {
                    keyboard.ClipboardText = text;
                }
            };

            _blitter.Input.OpenKeyboard += open =>
            {
                if (open)
                {
                    foreach (var keyboard in input.Keyboards)
                    {
                        keyboard.BeginInput();
                    }
                }
                else
                {
                    foreach (var keyboard in input.Keyboards)
                    {
                        keyboard.EndInput();
                    }
                }
            };
        }

        private void MouseOnScroll(IMouse arg1, ScrollWheel arg2)
        {
            _blitter.Input.PointerEvent(new Vector2(0, 10 * arg2.Y), Input.PointerEventType.Scrolled);
        }

        private void MouseOnMouseUp(IMouse arg1, MouseButton arg2)
        {
            _blitter.Input.PointerEvent(arg1.Position / _zoom, Input.PointerEventType.Moved);
            _blitter.Input.PointerEvent(arg1.Position / _zoom, Input.PointerEventType.Up);
        }

        private void MouseOnMouseDown(IMouse arg1, MouseButton arg2)
        {
            _blitter.Input.PointerEvent(arg1.Position / _zoom, Input.PointerEventType.Moved);
            _blitter.Input.PointerEvent(arg1.Position / _zoom, Input.PointerEventType.Down);
        }

        private void MouseOnMouseMove(IMouse arg1, Vector2 arg2)
        {
            _blitter.Input.PointerEvent(arg2 / _zoom, Input.PointerEventType.Moved);
        }

        private void OnKeyDown(IKeyboard arg1, Key code, int arg3)
        {
            var input = _blitter.Input;
            input.InputShift = arg1.IsKeyPressed(Key.ShiftLeft);

            if (code == Key.Enter) input.TextEvent(Input.TextEventType.EnterPressed, null);
            if (code == Key.Backspace) input.TextEvent(Input.TextEventType.Backspaced, null);

            if (code == Key.Delete) input.TextEvent(Input.TextEventType.Deleted, null);
            if (code == Key.Right) input.TextEvent(Input.TextEventType.Right, null);
            if (code == Key.Left) input.TextEvent(Input.TextEventType.Left, null);

            if (code == Key.Tab) input.TextEvent(Input.TextEventType.Entered, "\t");

            var control = arg1.IsKeyPressed(Key.ControlLeft);

            if (code == Key.V && control)
            {
                var content = arg1.ClipboardText;

                if (!string.IsNullOrEmpty(content))
                {
                    input.TextEvent(Input.TextEventType.Entered, content);
                }
            }

            if (code == Key.C && control)
            {
                input.TextEvent(Input.TextEventType.Copy, null);
            }
        }

        private void OnKeyChar(IKeyboard arg1, char arg2)
        {
            var input = _blitter.Input;
            var ch = arg2;
            if (char.IsControl(ch)) return;

            input.TextEvent(Input.TextEventType.Entered, arg2.ToString());
        }

        private void Update(double obj)
        {

        }

        private void OnFramebufferResize(Vector2D<int> newSize)
        {
            _gl.Viewport(newSize);
        }

        private unsafe void OnLoad()
        {
            _gl = GL.GetApi(_window);

            Resources.LoadAll();

            _quadProgram = ShaderProgram.CreateProgram(_gl,
                Resources.AsString("VertQuad.glsl"),
                Resources.AsString("FragQuad.glsl"));

            var vao = _gl.GenVertexArray();
            var vbo = _gl.GenBuffer();
            var ebo = _gl.GenBuffer();

            _gl.BindVertexArray(vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(TexVertex), (void*)0);

            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)sizeof(TexVertex), (void*)sizeof(Vector2));

            _gl.EnableVertexAttribArray(2);
            _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, (uint)sizeof(TexVertex), (void*)(sizeof(Vector2) + sizeof(ColorF)));

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

            _worldLoc = _gl.GetUniformLocation(_quadProgram, "World"u8);
            _texLoc = _gl.GetUniformLocation(_quadProgram, "uTexture"u8);

            _vao = vao;
            _vbo = vbo;
            _ebo = ebo;

            var font = ImageResult.FromMemory(Resources.Loaded["font.png"], ColorComponents.RedGreenBlueAlpha);
            var fontTex = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, fontTex);
            fixed (byte* ptr = font.Data)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                    (uint)font.Width, (uint)font.Height,
                    0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
            _gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
            _gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
            _fontTex = fontTex;
            _font = font;
            _fontSize = new Vector2(_font.Width, _font.Height);

            _gl.BindTexture(TextureTarget.Texture2D, 0);

            _gl.Enable(EnableCap.Blend);
            _gl.ClearColor(0, 1, 1, 1);

            _program = new TestProgram.TestProgram1();

            _blitter = new SilkBlitter(_fontSize);

            _program.blitter = _blitter;
            _blitter.Input = _program.input;

            _program.Create();

            BuildInput();
        }

        private unsafe void OnRender(double delta)
        {
            var viewPort = stackalloc int[4];
            _gl.GetInteger(GLEnum.Viewport, viewPort);
            float width = viewPort[2];
            float height = viewPort[3];

            _time += delta;

            width /= _zoom;
            height /= _zoom;
            _blitter.Input.Layout.ViewSize = new Vector2(width, height);
            _program.Update();

            //_gl.ClearColor((float)_time % 1f, 0.584f, 0.929f, 1.0f);
            //_gl.Clear(ClearBufferMask.ColorBufferBit);
            _gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            _gl.UseProgram(_quadProgram);
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            var aspectRatio = width / height;
            var world = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);

            //var camPos = new Vector3(0, 0, -15);
            //var view = Matrix4x4.CreateLookAt(camPos, Vector3.UnitZ, Vector3.UnitY);
            //var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, aspectRatio, 0.001f, 500f);
            //world = ( Matrix4x4.CreateScale(0.01f)* view) * proj;


            SetMatrix(_gl, _worldLoc, ref world);

            //var verts = new TexVertex[]
            //{
            //    new() { Position = new Vector2(-1, 0), Color = new ColorF(1,0,0,1)},
            //    new() { Position = new Vector2(0, 1),Color = new ColorF(1,1,0,1) },
            //    new() { Position = new Vector2(1, -1),Color = new ColorF(1,0,1,1) },
            //};

            //var indices = new int[]
            //{
            //    0, 1, 2
            //};

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, _fontTex);
            _gl.Uniform1(_texLoc, 0);

            DrawQuadList(_blitter.MainLayer);
            DrawQuadList(_blitter.PopupLayer);

            //_gl.BindVertexArray(_vao);
            //_gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            //_gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

            //_gl.BufferData<TexVertex>(BufferTargetARB.ArrayBuffer, verts, BufferUsageARB.StreamDraw);
            //_gl.BufferData<int>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StreamDraw);
            //_gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt,
            //    (void*)0);

            //_gl.BindVertexArray(0);
            //_gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            //_gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

            //_window.SwapBuffers();
        }

        private unsafe void DrawQuadList(TexQuadList quadList)
        {
            _gl.BindVertexArray(_vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

            _gl.BufferData<TexVertex>(BufferTargetARB.ArrayBuffer, quadList.GetVertexSpan(), BufferUsageARB.StreamDraw);
            _gl.BufferData<int>(BufferTargetARB.ElementArrayBuffer, quadList.GetIndexSpan(), BufferUsageARB.StreamDraw);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)quadList.GetIndexSpan().Length, DrawElementsType.UnsignedInt,
                (void*)0);

            _gl.BindVertexArray(0);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }

        private unsafe void SetMatrix(GL gl, int loc, ref Matrix4x4 mat)
        {
            fixed (float* ptr = &mat.M11)
            {
                gl.UniformMatrix4(loc, 1, false, ptr);
                var err = gl.GetError();
                if (err != GLEnum.NoError)
                {
                    Console.WriteLine(err);
                }
            }
        }
    }
}