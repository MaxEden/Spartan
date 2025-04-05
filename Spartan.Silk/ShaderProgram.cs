using System.Diagnostics;
using Silk.NET.OpenGLES;

namespace Spartan.Silk
{
    internal class ShaderProgram
    {
        public static uint CreateShader(GL gl, string source, ShaderType type)
        {
            uint shader = gl.CreateShader(type);
            gl.ShaderSource(shader, source);
            gl.CompileShader(shader);
            gl.GetShader(shader, ShaderParameterName.CompileStatus, out var res);
            gl.GetShaderInfoLog(shader, out var infoString);
            if (infoString != null) Console.WriteLine(infoString);
            Debug.Assert(res != 0);
            return shader;
        }

        public static uint CreateProgram(GL gl, string vertexSource, string fragmentSource)
        {
            uint vertexShader = CreateShader(gl, vertexSource, ShaderType.VertexShader);
            uint fragmentShader = CreateShader(gl, fragmentSource, ShaderType.FragmentShader);
            uint program = gl.CreateProgram();
            gl.AttachShader(program, vertexShader);
            gl.AttachShader(program, fragmentShader);
            gl.LinkProgram(program);
            gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var res);
            gl.GetProgramInfoLog(program, out var log);
            if (log != null) Console.WriteLine(log);
            Debug.Assert(res != 0);
            return program;
        }
    }
}
