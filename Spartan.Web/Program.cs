using System.Numerics;
using TestBlit;
using TestBlit.WebUI;

namespace Spartan.Web
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = new Server();
            var input = new Nui.Input();

            var blitter = new WebBlitter();

            var program = new TestProgram.TestProgram();
            program.blitter = blitter;
            program.input = input;
            blitter.Input = input;

            server.Blitter = blitter;
            server.Input = input;
            server.Blitter.Input = input;
            server.Start();

            program.Create();
           

            while (true)
            {
                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    break;
                }
                program.viewSize = new Vector2(1000, 500);
                blitter.viewSize = program.viewSize;
                program.Update();

                server.TrySend();
            }
        }
    }
}
