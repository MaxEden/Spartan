namespace Spartan.BasicElements;

public struct Stack
{
    private float _lineHeight;
    private Rect _area;
    private int _line;
    private int _padding;
    public int Indent;

    public void Begin(Rect area, float lineHeight, int padding)
    {
        _lineHeight = lineHeight;
        _area = area;
        _line = 0;
        _padding = padding;
    }

    public Rect GetLine()
    {
        var rect=  new Rect(_area.X + _padding + Indent * _lineHeight,
            _area.Y + _line * (_lineHeight + _padding),
            _area.Width - 2 * _padding - Indent * _lineHeight, 
            _lineHeight);
        _line++;
        return rect;
    }


    public Rect GetLines(int count)
    {
        var rect = new Rect(_area.X + _padding + Indent * _lineHeight,
            _area.Y + _line * (_lineHeight + _padding),
            _area.Width - 2 * _padding - Indent * _lineHeight,
            (_lineHeight + _padding) * count);
        _line++;
        return rect;
    }
}