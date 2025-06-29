using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.Windowing;
using StbImageSharp;
using TestProgram;

namespace Spartan.Silk
{
    public class SpartanSilk
    {
        private uint _quadProgram;
        private int _worldLoc;
        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private int _texLoc;
        private SilkBlitter _blitter;
        private float _zoom;
        private float _width;
        private float _height;
        private readonly Input _input;
        private Rect _viewArea;
        //private STexture _fontTexture;
        private STexture _monofontTexture;
        private GL _gl;
        private List<STexture> _loadQueue = new();
        private List<STexture> _loadQueueTmp = new();
        public SpartanSilk(Input input, float zoom)
        {
            _input = input;
            _zoom = zoom;
        }

        public void BuildFullInput(IInputContext input)
        {
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

            _input.SetToClipboard += text =>
            {
                foreach (var keyboard in input.Keyboards)
                {
                    keyboard.ClipboardText = text;
                }
            };

            _input.OpenKeyboard += open =>
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

        public void MouseOnScroll(IMouse arg1, ScrollWheel arg2)
        {
            _input.PointerEvent(new Vector2(0, 10 * arg2.Y), Input.PointerButton.Main, Input.PointerEventType.Scrolled);
        }

        public void MouseOnMouseUp(IMouse mouse, MouseButton arg2)
        {
            var button = arg2 == MouseButton.Left ? Input.PointerButton.Main : Input.PointerButton.Alt;

            _input.PointerEvent(mouse.Position / _zoom, button, Input.PointerEventType.Moved);
            _input.PointerEvent(mouse.Position / _zoom, button, Input.PointerEventType.Up);
        }

        public void MouseOnMouseDown(IMouse mouse, MouseButton arg2)
        {
            var button = arg2 == MouseButton.Left ? Input.PointerButton.Main : Input.PointerButton.Alt;
            _input.PointerEvent(mouse.Position / _zoom, button, Input.PointerEventType.Moved);
            _input.PointerEvent(mouse.Position / _zoom, button, Input.PointerEventType.Down);
        }

        public void MouseOnMouseMove(IMouse mouse, Vector2 arg2)
        {
            _input.PointerEvent(arg2 / _zoom, Input.PointerButton.Main, Input.PointerEventType.Moved);
        }

        public void OnKeyDown(IKeyboard keyboard, Key code, int arg3)
        {
            var input = _input;
            input.Shift = keyboard.IsKeyPressed(Key.ShiftLeft);
            input.Ctrl = keyboard.IsKeyPressed(Key.ControlLeft);

            if (code == Key.Enter) input.TextEvent(Input.TextEventType.EnterPressed, null);
            if (code == Key.Backspace) input.TextEvent(Input.TextEventType.Backspaced, null);

            if (code == Key.Delete) input.TextEvent(Input.TextEventType.Deleted, null);
            if (code == Key.Right) input.TextEvent(Input.TextEventType.Right, null);
            if (code == Key.Left) input.TextEvent(Input.TextEventType.Left, null);

            if (code == Key.Tab) input.TextEvent(Input.TextEventType.Typed, "\t");

            var control = keyboard.IsKeyPressed(Key.ControlLeft);

            if (code == Key.V && control)
            {
                var content = keyboard.ClipboardText;

                if (!string.IsNullOrEmpty(content))
                {
                    input.TextEvent(Input.TextEventType.Typed, content);
                }
            }

            if (code == Key.C && control)
            {
                input.TextEvent(Input.TextEventType.Copy, null);
            }
        }

        public void OnKeyChar(IKeyboard keyboard, char arg2)
        {
            var input = _input;
            var ch = arg2;
            if (char.IsControl(ch)) return;

            input.TextEvent(Input.TextEventType.Typed, arg2.ToString());
        }
        
        public unsafe STexture LoadTexture(GL gl, byte[] bytes, string path, bool sync = false)
        {
            var stext =  new STexture()
            {
                TexID = 0,
                Size = default,
                InvSize = default,
                Bytes = bytes,
                Path = path
            };

            if (sync)
            {
                if (path != null) stext.Bytes = File.ReadAllBytes(path);
                stext.LoadResult = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
                stext.FinishLoad(gl);
            }
            else
            {
                stext.Task = Task.Run(() =>
                {
                    if (stext.Path != null) stext.Bytes = File.ReadAllBytes(stext.Path);
                    stext.LoadResult = ImageResult.FromMemory(stext.Bytes, ColorComponents.RedGreenBlueAlpha);
                });

                _loadQueue.Add(stext);
            }

            return stext;
        }
        public unsafe SilkBlitter OnLoad(GL gl)
        {
            Resources.LoadAll();

            _quadProgram = ShaderProgram.CreateProgram(gl,
                Resources.AsString("VertQuad.glsl"),
                Resources.AsString("FragQuad.glsl"));

            var vao = gl.GenVertexArray();
            var vbo = gl.GenBuffer();
            var ebo = gl.GenBuffer();

            gl.BindVertexArray(vao);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(TexVertex), (void*)0);

            gl.EnableVertexAttribArray(1);
            gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)sizeof(TexVertex),
                (void*)sizeof(Vector2));

            gl.EnableVertexAttribArray(2);
            gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, (uint)sizeof(TexVertex),
                (void*)(sizeof(Vector2) + sizeof(ColorF)));

            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);

            _worldLoc = gl.GetUniformLocation(_quadProgram, "World"u8);
            _texLoc = gl.GetUniformLocation(_quadProgram, "uTexture"u8);

            

            _vao = vao;
            _vbo = vbo;
            _ebo = ebo;

            //_fontTexture = LoadTexture(gl, Resources.Loaded["font.png"], null, true);
            _monofontTexture = LoadTexture(gl, Resources.Loaded["mono_font.png"], null, true);

            gl.BindTexture(TextureTarget.Texture2D, 0);

            gl.Enable(EnableCap.Blend);
            gl.ClearColor(0, 1, 1, 1);

            _gl = gl;

            //_blitter = new SilkBlitter(_fontTexture.Size, _input);
            _blitter = new SilkBlitter(_monofontTexture.Size, _input);
            _blitter.LoadTexture = LoadTexture;
            return _blitter;
        }

        private STexture LoadTexture(string path)
        {
            return LoadTexture(_gl, null, path);
        }

        public unsafe void PreRender(GL gl, Input input, Rect viewArea)
        {
            // var viewPort = stackalloc int[4];
            // gl.GetInteger(GLEnum.Viewport, viewPort);
            // _width = viewPort[2];
            // _height = viewPort[3];
            _viewArea = viewArea;

            _width = _viewArea.Width;
            _height = _viewArea.Height;

            _width /= _zoom;
            _height /= _zoom;

            input.Layout.ViewSize = new Vector2(_width, _height);

            _loadQueueTmp.Clear();
            _loadQueueTmp.AddRange(_loadQueue);
            foreach (var texture in _loadQueueTmp)
            {
                if (texture.Task.IsCompleted)
                {
                    _loadQueue.Remove(texture);
                    if (texture.LoadResult != null)
                    {
                        texture.FinishLoad(gl);
                    }
                }
            }
            _loadQueueTmp.Clear();
        }

        public void Render(GL gl)
        {
            gl.Viewport((int)_viewArea.X, (int)_viewArea.Y, (uint)_viewArea.Width, (uint)_viewArea.Height);

            gl.Disable(GLEnum.DepthTest);

            gl.UseProgram(_quadProgram);
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            var world = Matrix4x4.CreateOrthographicOffCenter(0, _width, _height, 0, -1, 1);
            
            SetMatrix(gl, _worldLoc, ref world);

           

            DrawQuadList(gl, _blitter.MainLayer);
            DrawQuadList(gl, _blitter.PopupLayer);
        }

        private unsafe void DrawQuadList(GL gl, TexQuadList quadList)
        {
            gl.ActiveTexture(TextureUnit.Texture0);
            //gl.BindTexture(TextureTarget.Texture2D, _fontTexture.TexID);
            gl.BindTexture(TextureTarget.Texture2D, _monofontTexture.TexID);
            gl.Uniform1(_texLoc, 0);

            gl.BindVertexArray(_vao);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            

            gl.BufferData<TexVertex>(BufferTargetARB.ArrayBuffer, quadList.GetVertexSpan(), BufferUsageARB.StreamDraw);
            gl.BufferData<int>(BufferTargetARB.ElementArrayBuffer, quadList.GetIndexSpan(), BufferUsageARB.StreamDraw);
            gl.DrawElements(PrimitiveType.Triangles, (uint)quadList.GetIndexSpan().Length, DrawElementsType.UnsignedInt, (void*)0);


            foreach (var quad in quadList.GetQuadsSpan())
            {
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, quad.TexID);
                gl.Uniform1(_texLoc, 0);

                Quad* ptr = &quad;
                var spanVert = new ReadOnlySpan<TexVertex>(&ptr->TexVertex1, sizeof(TexVertex) * 4);

                gl.BufferData<TexVertex>(BufferTargetARB.ArrayBuffer, spanVert, BufferUsageARB.StreamDraw);
                gl.BufferData<int>(BufferTargetARB.ElementArrayBuffer, TexQuadList.QuadIndexes, BufferUsageARB.StreamDraw);
                gl.DrawElements(PrimitiveType.Triangles, (uint)TexQuadList.QuadIndexes.Length, DrawElementsType.UnsignedInt, (void*)0);
            }

            gl.BindVertexArray(0);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
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