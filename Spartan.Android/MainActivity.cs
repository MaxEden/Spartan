using Silk.NET.Windowing.Sdl.Android;
using Spartan.Silk;

namespace Spartan.Android
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : SilkActivity
    {
        protected override void OnRun()
        {
            var silkProgram = new SilkProgram();
            silkProgram.Run(3);
        }
    }
}