using Silk.NET.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Maths;
using Silk.NET.OpenGLES;
using Silk.NET.Input;
using TestProgram;

namespace Spartan.Silk
{
    public class Launcher
    {
        private IView _window;
        private GL _gl;

        private IInputContext _silkInput;

        //private TestProgram1 _program;
        private SilkBlitter _blitter;
        private SpartanSilk _spartanSilk;
        private Input _input;

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
            _window.Update += OnUpdate;
            _window.FramebufferResize += OnFramebufferResize;

            _window.Run();
        }

        private void OnLoad()
        {
            _gl = GL.GetApi(_window);

            _silkInput = _window.CreateInput();

            _input = new Input();
            _spartanSilk = new SpartanSilk(_input, _zoom);
            _blitter = _spartanSilk.OnLoad(_gl);
            _spartanSilk.BuildFullInput(_silkInput);

            Load?.Invoke();
            //_program = new TestProgram.TestProgram1();
            //_program.Create(_input);
        }

        private void OnFramebufferResize(Vector2D<int> newSize)
        {
            _gl.Viewport(newSize);
        }

        private void OnUpdate(double obj)
        {
            Update?.Invoke(obj);
        }

        public Action Load;

        public Action<double> Update;

        public Action<IBlitter, Input> Draw;
        private float _zoom;

        private void OnRender(double obj)
        {
            _spartanSilk.PreRender(_gl, _input,
                new Rect(0,0, _window.FramebufferSize.X, _window.FramebufferSize.Y));

            _blitter.Begin();
            Draw?.Invoke(_blitter, _input);
            //_program.Update(_blitter, _input);
            _blitter.End();
            _input.PostStep();

            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _spartanSilk.Render(_gl);
        }
    }
}