using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Nui;
using TestBlit;
using TestBlit.TestApi;

namespace TestProgram
{
    public class TestProgram
    {
        private Class1 testObj;
        private Inspector inpector;
        private DebugConsole console;
        public Blitter blitter { get; set; }
        public Input input { get; set; }
        public Vector2 viewSize { get; set; }
        
        public void Create() {

            var menu = new Menu("File", "Edit", "Assets", "GameObject", "Component", "Window", "Help");
            menu.Add("New", "Open", "Save", "Save as", "Quit");

            testObj = new Class1();
            inpector = new Inspector();
            console = new DebugConsole();
            
        }

        public void Update()
        {
            blitter.Start();
            
            blitter.DrawRect(
                new Rect(0, 0, (int)viewSize.X, (int)viewSize.Y),
                Color32.blue);

            //blitter.DrawText(new Rect(10, 10, 200, 25), "Hello, pidor! Speed: [10.5f; 0.8f]");

            //for (int i = 0; i < 100; i++)
            //{
            //    menu.Draw(new Rect(0, 20*i, viewSize.X, 20), blitter, input);
            //}

            // menu.Draw(new Rect(0, 0, viewSize.X, 20), blitter, input);

            inpector.Draw(new Rect(viewSize.X - 200, 40, 200, viewSize.Y), blitter, input, testObj);


            //blitter.DrawRect(new Rect(10, 30, 255, 255), Color32.green);

            //_scroll1 = blitter.BeginScroll(_scroll1, new Rect(10, 30, 255, 255), 500);

            ////blitter.DrawRect(new Rect(0, 0, 15, 15), OnityEngine.Color.magenta);

            //for (int i = 0; i < 5; i++)
            //{
            //    blitter.DrawRect(new Rect(0, 100 * i, 255, 100), i%2==0?Color32.green:Color32.red);
            //}

            ////blitter.DrawRect(new Rect(0, 0, 255, 500), Color32.green);

            ////for (int i = 0; i < 10; i++)
            ////{
            ////    menu.Draw(
            ////        //new Rect(input.Layout.CurrentArea.X, input.Layout.CurrentArea.Y + 20 * i, 255, 20),
            ////        new Rect(0, 20 * i, 255, 20),
            ////        blitter,
            ////        input);
            ////}

            ////blitter.DrawRect(new Rect(10, 30, 255, 255), OnityEngine.Color.red);


            //blitter.EndScroll();

            console.Draw(new Rect(0, viewSize.Y - 100, viewSize.X, 100), blitter);

            //blitter.DrawRect(new Rect(10, 30, 255, 255), OnityEngine.Color.red);

            //blitter.DrawRect(new Rect(10,10, 255,255), OnityEngine.Color.green);

            //blitter.DrawRect(new Rect(10, 10, 127, 127), OnityEngine.Color.red);

            // DrawTextMono(new IntRect(10, 100, 200, 50), "Hello, pidor! Speed: [10.5f; 0.8f]");
            //
            // void DrawTextMono(IntRect rectDest, string text)
            // {
            //     var charW = 10;
            //     var charH = 25;
            //
            //     var sprite = new Sprite(monoTex);
            //
            //     int ci = 0;
            //     for (int i = 0; i < text.Length; i++)
            //     {
            //         if (text[i] == '\ufe0e') continue;
            //         if (text[i] == 65038) continue;
            //         charTable.TryGetValue(text[i], out int index); // - 32;
            //
            //         int Y = index / 51;
            //         int X = index % 51;
            //
            //         var rect = new IntRect(X * charW, Y * charH, charW, charH);
            //         sprite.Position = new Vector2f(rectDest.Left + ci * charW, rectDest.Top);
            //
            //         sprite.TextureRect = rect;
            //         sprite.Scale = new Vector2f(1, 1);
            //         sprite.Color = SFML.Graphics.Color.White;
            //         window.Draw(sprite);
            //
            //         ci++;
            //     }
            // }

            // blitter.DrawRect(
            //     new Rect(input.DefaultPointer.Position - Vector2.one, 2 * Vector2.one),
            //     OnityEngine.Color.magenta);

            blitter.End();


            input.PostStep();
        }
    }
}
