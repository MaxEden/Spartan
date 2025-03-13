using System.Numerics;
using Spartan;
using Spartan.Web.WebUI;

namespace Spartan.Web
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = new Server();

            var blitter = new WebBlitter();

            var program = new TestProgram.TestProgram();
            program.blitter = blitter;
            blitter.Input = program.input;

            server.Blitter = blitter;
            server.Start();

            program.Create();
           

            while (true)
            {
                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    break;
                }
                program.input.Layout.ViewSize = new Vector2(1000, 500);
                blitter.viewSize = program.viewSize;
                program.Update();

                server.TrySend();
            }
        }
    }
}
