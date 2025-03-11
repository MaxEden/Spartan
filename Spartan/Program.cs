using System;
using SFML.Graphics;
using SFML.Window;
using System.Numerics;
using Nui;
using TestBlit.TestApi;
using TestBlit.WebUI;
using Color = SFML.Graphics.Color;

namespace TestBlit
{
    internal class Program
    {
        private static float _scroll1;
        private static bool _stopMoving;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            var mode = new SFML.Window.VideoMode(800, 600);
            var window = new SFML.Graphics.RenderWindow(mode, "SFML works!");
            window.SetVerticalSyncEnabled(true);

            var windowSize = window.Size;
            window.SetView(new View(new FloatRect(0, 0, windowSize.X * 1f, windowSize.Y * 1f)));
            window.Closed += (sender, args) => window.Close();

            var input = new Input();
            window.TextEntered += (sender, eventArgs) =>
            {
                if (eventArgs.Unicode.Length > 2) return;

                for (int i = 0; i < eventArgs.Unicode.Length; i++)
                {
                    var ch = eventArgs.Unicode[i];
                    if (ch == '\b')
                    {
                        input.TextEvent(Input.TextEventType.Backspaced, null);
                        return;
                    }

                    if (ch == '\r')
                    {
                        input.TextEvent(Input.TextEventType.EnterPressed, null);
                        return;
                    }

                    if (ch == '\t')
                    {
                        input.TextEvent(Input.TextEventType.Entered, "\t");
                        return;
                    }

                    if (char.IsControl(ch)) return;
                }

                input.TextEvent(Input.TextEventType.Entered, eventArgs.Unicode);
            };
            window.KeyPressed += (sender, eventArgs) =>
            {
                input.InputShift = eventArgs.Shift;
                //if (eventArgs.Code == Keyboard.Key.Backspace) input.TextEvent(Input.TextEventType.Backspaced, null);
                if (eventArgs.Code == Keyboard.Key.Delete) input.TextEvent(Input.TextEventType.Deleted, null);
                //if (eventArgs.Code == Keyboard.Key.Enter) input.TextEvent(Input.TextEventType.EnterPressed, null);

                if (eventArgs.Code == Keyboard.Key.Right) input.TextEvent(Input.TextEventType.Right, null);
                if (eventArgs.Code == Keyboard.Key.Left) input.TextEvent(Input.TextEventType.Left, null);

                if (eventArgs.Code == Keyboard.Key.V && eventArgs.Control)
                {
                    var content = SFML.Window.Clipboard.Contents;

                    if (!string.IsNullOrEmpty(content))
                    {
                        input.TextEvent(Input.TextEventType.Entered, content);
                    }
                }

                if (eventArgs.Code == Keyboard.Key.C && eventArgs.Control)
                {
                    input.TextEvent(Input.TextEventType.Copy, null);
                }
            };
            window.MouseEntered += (sender, args) => { input.PointerEvent(default, Input.PointerEventType.Enter); };
            window.MouseLeft += (sender, args) => { input.PointerEvent(default, Input.PointerEventType.Left); };
            window.MouseMoved += (sender, args) =>
            {
                if (_stopMoving) return;
                input.PointerEvent(new Vector2(args.X, args.Y), Input.PointerEventType.Moved);
            };
            window.MouseButtonPressed += (sender, args) =>
            {
                if (args.Button == Mouse.Button.Right)
                {
                    _stopMoving = !_stopMoving;
                }
                else
                {
                    input.PointerEvent(default, Input.PointerEventType.Down);
                }
            };
            window.MouseButtonReleased += (sender, args) => { input.PointerEvent(default, Input.PointerEventType.Up); };
            window.MouseWheelScrolled += (sender, args) => {
                input.PointerEvent(new Vector2(0, 10*args.Delta), Input.PointerEventType.Scrolled);
            };
           
            // string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            // var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceNames[0]);
            // var fontTexture1 = new Texture(stream);

            var fontTexture = new Texture("Resources/font.png");
            Blitter blitter = new SfmlBlitter(window, fontTexture);
            


            //var monoTex = new Texture("Resources/DejaVu Sans Mono.png");
            //var charSet = File.ReadAllText("Resources/charset.txt");
            //var charTable = new Dictionary<char, int>(charSet.Length);

            //int index = 0;

            //foreach (var ch in charSet)
            //{
            //    if (char.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark)
            //    {
            //        continue;
            //    }
            //    else
            //    {
            //        charTable[ch] = index;
            //        index++;
            //    }
            //}


            // for (int i = 0; i < charSet.Length; i++)
            // {
            //     if (charSet[i] == 65038) 
            //         continue;
            //     charTable[charSet[i]] = index;
            //     index++;
            // }

           

           

            while (window.IsOpen)
            {
                // Process events
                window.DispatchEvents();
                window.Clear(Color.Blue);

                if (windowSize != window.Size)
                {
                    windowSize = window.Size;
                    window.SetView(new View(new FloatRect(0, 0, windowSize.X, windowSize.Y)));
                }

                var viewSize = window.GetView().Size;

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

                // Finally, display the rendered frame on screen
                window.Display();

                input.PostStep();

                server.TrySend();
            }
        }
    }
}