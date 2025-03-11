using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MonoFontGen
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var fontName = "Hack"; //args[0];
            var fontFamily =
                SystemFonts.Families.First(p => p.Name.Contains(fontName, StringComparison.OrdinalIgnoreCase));

            int fontW = 20;
            int fontH = 15;
            var font = fontFamily.CreateFont(20, FontStyle.Regular);
            string defaultInput =
                "QWERTYUIOP{}ASDFGHJKL:\"ZXCVBNM<>?\"qwertyuiop[]asdfghjkl;'zxcvbnm,./`1234567890-=~!@#$%^&*()_+|ёйцукенгшщзхъфывапролджэячсмитьбю.ЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ,";

            //==============================================
            var vals = font
                .FontMetrics
                .GetAvailableCodePoints()
                .Select(p => p.Value)
                .ToHashSet();

            var sb = new StringBuilder();

            List<(int, int)> ranges = new();
            int rangeStart = -1;
            int rangeEnd = -1;

            void AddRange(int from, int to)
            {
                for (int i = from; i < to; i++)
                {
                    char c = (char)i;
                    if (!vals.Contains(i) || Char.IsControl(c))
                    {
                        if (rangeEnd >= 0)
                        {
                            ranges.Add((rangeStart, rangeEnd));
                            rangeStart = -1;
                            rangeEnd = -1;
                        }

                        continue;
                    }
                    else
                    {
                        sb.Append(c);
                        if (rangeStart < 0) rangeStart = i;
                        rangeEnd = i;
                    }
                }
            }

            AddRange(0x000, 0x100); //Latin
            AddRange(0x370, 0x530); //Greek, Cyrilic
            AddRange(0x25A0, 0x2600); //Geometric shapes
            AddRange(0x2600, 0x2C60); //Misc symbols

            var input = sb.ToString();

            //====================================

            var chars = input //defaultInput
                .Where(p => char.IsLetterOrDigit(p))
                .Select(p => p.ToString())
                .ToArray();

            var textOptions = new TextOptions(font);
            var sizes = chars
                .Select(p => TextMeasurer.MeasureBounds(p, textOptions))
                .ToArray();

            var widths = sizes
                .Select(p => p.X + p.Width)
                .ToArray();

            Array.Sort(widths);
            var medW = (int)widths[widths.Length / 2] + 1;

            var heights = sizes
                .Select(p => p.Y + p.Height)
                .ToArray();

            var maxY = (int)heights.Max();
            var minY = (int)(sizes.Select(p => p.Y).Min() - 1);


            var medH = (int)MathF.Max(heights.Max(), maxY) + 1 + 1;

            Console.WriteLine(medW);
            Console.WriteLine(medH);

            //===================================================

            int size = 128;

            while (true)
            {
                int countX = size / medW;
                int countY = input.Length / countX;
                if (countY * medH > size)
                {
                    size *= 2;
                }
                else
                {
                    break;
                }
            }


            var image = new Image<Rgba32>(size, size);

            image.Mutate(p =>
            {
                p.Clear(new Color(new Rgba32(0, 0, 0, 0)));
                int x = 0;
                int y = 0;

                for (int i = 0; i < input.Length; i++)
                {
                    var rect = new RectangleF(x, y, medW, medH);
                    //p.Fill(new Color(new Rgba32(0,0,0,0)), rect);

                    var ch = input[i];
                    p.Clip(new RectangularPolygon(rect),
                        op => { op.DrawText(ch.ToString(), font, Color.White, new PointF(x, y - minY)); });

                    //p.Draw(Color.Red, 1, rect);

                    x += medW;

                    if (x + medW > image.Width)
                    {
                        x = 0;
                        y += medH;
                    }
                }
            });

            image.SaveAsPng("output.png");

            var output = new StringBuilder();
            output.AppendLine(medW.ToString());
            output.AppendLine(medH.ToString());
            output.AppendLine(input);

            File.WriteAllText("output.txt", output.ToString());
        }
    }
}