﻿using SFML.Graphics;
using SFML.Window;
using System.Numerics;
using Nui;
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
                input.PointerEvent(new Vector2(0, 10 * args.Delta), Input.PointerEventType.Scrolled);
            };

            // string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            // var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceNames[0]);
            // var fontTexture1 = new Texture(stream);

            var fontTexture = new Texture("Resources/font.png");
            
            var blitter = new SfmlBlitter(window, fontTexture);
            var program = new TestProgram.TestProgram();
            program.blitter = blitter;
            program.input = input;
            blitter.Input = input;

            program.Create();

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
                program.viewSize = new Vector2(viewSize.X, viewSize.Y);
                program.Update();
                
                window.Display();
            }
        }
    }
}