using Spartan.Web.WebUI;

namespace Spartan.Web
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = new Server();
            server.Start();

            while (true)
            {
                if (Console.KeyAvailable && Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    break;
                }

                Thread.Sleep(20);
            }
        }
    }
}
