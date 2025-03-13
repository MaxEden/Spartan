using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Spartan;
using Spartan.TestApi;

namespace TestProgram
{
    public class TestProgram1
    {
        private Class1 testObj;
        private Inspector inpector;
        private DebugConsole console;
        private Menu menu;
        public IBlitter blitter { get; set; }
        public Input input { get; } = new Input();
        public Vector2 viewSize => input.Layout.ViewSize;
        
        public void Create() {

            menu = new Menu("File", "Edit", "View", "Project", "Debug", "Window", "Help");
            menu.Add("New", "Open", "Save", "Save as", "Quit");

            testObj = new Class1();
            inpector = new Inspector();
            console = new DebugConsole();
        }

        public void Update()
        {
            blitter.Begin();
            
            blitter.DrawRect(new Rect(0, 0, (int)viewSize.X, (int)viewSize.Y), Color32.blue);

            menu.Draw(new Rect(0, 0, viewSize.X, 20), blitter, input);

            inpector.Draw(new Rect(viewSize.X - 200, 40, 200, viewSize.Y), blitter, input, testObj);
            
            console.Draw(new Rect(0, viewSize.Y - 100, viewSize.X, 100), blitter);
            
            blitter.End();
            
            input.PostStep();
        }
    }
}
