using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Spartan;
using static System.Net.Mime.MediaTypeNames;

namespace Spartan
{
    public class DefaultTextRenderer
    {
        int charW = 6;
        int charH = 12;
        public Vector2 GetTextLineSize(string text)
        {
            return new Vector2(charW * text.Length, charH);
        }

        public int GetCaretPos(Rect area, string text, Vector2 pointer)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            var pos = new Vector2(area.X, area.Y + (area.Height - charH) / 2);

            float ipos = (pointer.X - pos.X) / charW;
            int index = (int)MathF.Round(ipos);
            if (index > text.Length) return text.Length;
            if (index < 0) return 0;
            return index;
        }

        public Vector2 GetTextPosition(Rect rectDest, string text, Align align)
        {
            Vector2 pos;

            switch (align)
            {
                case Align.Center:
                    pos = new Vector2(
                        rectDest.X + (rectDest.Width - charW * text.Length) / 2,
                        rectDest.Y + (rectDest.Height - charH) / 2);
                    break;
                case Align.TopUp:
                    pos = new Vector2(rectDest.X, rectDest.Y);
                    break;
                case Align.Middle:
                default:
                    pos = new Vector2(rectDest.X + 2, rectDest.Y + (rectDest.Height - charH) / 2);
                    break;
            }

            return pos;
        }

        public void BuildSelection(Rect rectDest, string text, int selectionStart, int selectionEnd, int caretPos)
        {
            var pos = new Vector2(rectDest.X + 2, rectDest.Y + (rectDest.Height - charH) / 2);
            SelectPos = pos;

            if (selectionStart >= 0 && selectionEnd > selectionStart)
            {
                int selCount = selectionEnd - selectionStart;
                SelectRect = new Rect(pos.X + charW * selectionStart, pos.Y, charW * selCount, charH);
            }
            else
            {
                SelectRect = null;
            }
            
            if (selectionStart < 0 || selectionStart == selectionEnd)
            {
                CoursorRect = new Rect(pos.X + caretPos * charW, pos.Y, 1, charH);
            }
            else
            {
                CoursorRect = null;
            }
        }

        

        public int GliphCount;
        public Rect[] GliphsFrom;
        public Vector2[] GliphsTo;
        private byte[] _textBytes = new byte[1000];

        public Vector2 SelectPos;
        public Rect? SelectRect;
        public Rect? CoursorRect;

        public void BuildGliphRects(Vector2 pos, string text)
        {
            int count = Encoding.ASCII.GetBytes(text, 0, text.Length, _textBytes, 0);
            GliphCount = count;
            if (GliphsFrom.Length < count) GliphsFrom = new Rect[count * 2];
            if (GliphsTo.Length < count) GliphsTo = new Vector2[count * 2];

            for (int i = 0; i < count; i++)
            {
                int index = _textBytes[i] - 32;

                int y = index / 16;
                int x = index % 16;

                var rect = new Rect(x * charW, y * charH, charW, charH);
                var resPos = new Vector2(pos.X + i * charW, pos.Y);
                GliphsFrom[i] = rect;
                GliphsTo[i] = resPos;
            }
        }
    }
}
