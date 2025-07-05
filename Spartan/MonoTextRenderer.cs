using System.Numerics;
using System.Text;

namespace Spartan;

public class MonoTextRenderer
{
    private int _charW = 0;
    private int _charH = 0;
    private int _until;

    public int GliphCount;

    const  int       DefaultGliphCount = 1000;

    public Rect[]    GliphsFrom        = new Rect[DefaultGliphCount];
    public Vector2[] GliphsTo          = new Vector2[DefaultGliphCount];

    public  Vector2 SelectPos;
    public  Rect?   SelectRect;
    public  Rect?   CoursorRect;
    private int     _texSize;
    private int     _rowNum;

    private Dictionary<int, Rect> _dictRects = new();

    public void Load(string text)
    {
        var parts = text.Split('\n', '\r', StringSplitOptions.RemoveEmptyEntries);
        _charW = int.Parse(parts[0]);
        _charH = int.Parse(parts[1]);
        _until = int.Parse(parts[2]);
        _texSize = int.Parse(parts[3]);
        var dictChars = parts[4];

        _rowNum = _texSize / _charW;
        int di = 0;
        foreach (var ch in dictChars)
        {
            int index = _until + di;
            int y = index / _rowNum;
            int x = index % _rowNum;
            var rect = new Rect(x * _charW, y * _charH, _charW, _charH);
            _dictRects.Add(ch, rect);
            di++;
        }
    }

    public Vector2 GetTextLineSize(string text)
    {
        return new Vector2(_charW * text.Length, _charH);
    }

    public int GetCaretPos(Rect area, string text, Vector2 pointer)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var pos = new Vector2(area.X, area.Y + (area.Height - _charH) / 2);

        float ipos = (pointer.X - pos.X) / _charW;
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
                    rectDest.X + (rectDest.Width - _charW * text.Length) / 2,
                    rectDest.Y + (rectDest.Height - _charH) / 2);
                pos.X = MathF.Round(pos.X);
                pos.Y = MathF.Round(pos.Y);
                break;
            case Align.TopLeft:
                pos = new Vector2(rectDest.X, rectDest.Y);
                pos.X = MathF.Round(pos.X);
                pos.Y = MathF.Round(pos.Y);
                break;
            case Align.Middle:
            default:
                pos = new Vector2(rectDest.X + 2, rectDest.Y + (rectDest.Height - _charH) / 2);
                pos.X = MathF.Round(pos.X);
                pos.Y = MathF.Round(pos.Y);
                break;
        }

        return pos;
    }

    public void BuildSelection(Rect rectDest, int selectionStart, int selectionEnd, int caretPos)
    {
        var pos = new Vector2(rectDest.X + 2, rectDest.Y + (rectDest.Height - _charH) / 2);
        SelectPos = pos;

        if (selectionStart >= 0 && selectionEnd > selectionStart)
        {
            int selCount = selectionEnd - selectionStart;
            SelectRect = new Rect(pos.X + _charW * selectionStart, pos.Y, _charW * selCount, _charH);
        }
        else
        {
            SelectRect = null;
        }

        if (selectionStart < 0 || selectionStart == selectionEnd)
        {
            CoursorRect = new Rect(pos.X + caretPos * _charW, pos.Y, 1, _charH);
        }
        else
        {
            CoursorRect = null;
        }
    }

    public void BuildGliphRects(Vector2 pos, string text, Rect rectDest)
    {
        int count = Math.Min(text.Length,(int)(rectDest.xMax - pos.X) / _charW);
            
        GliphCount = count;
        if (GliphsFrom.Length < count) GliphsFrom = new Rect[count * 2];
        if (GliphsTo.Length < count) GliphsTo = new Vector2[count * 2];

        for (int i = 0; i < count; i++)
        {
            var resPos = new Vector2(pos.X + i * _charW, pos.Y);
            GliphsTo[i] = resPos;
            
            int ch = text[i];
            
            if (ch <= _until)
            {
                int y = ch / _rowNum;
                int x = ch % _rowNum;
                var rect = new Rect(x * _charW, y * _charH, _charW, _charH);
                GliphsFrom[i] = rect;
            }
            else
            {
                GliphsFrom[i] = _dictRects[ch];
            }
        }
    }
}