namespace Spartan.Silk.Windows
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var program = new TestProgram.TestProgram1();
            var launcher = new Launcher();
            launcher.Load = () => program.Create();
            launcher.Draw = (blitter, input) =>
            {
                program.Update(blitter, input);
            };
            launcher.Run(1);
        }
    }
}
