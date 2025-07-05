using System.Numerics;

namespace Spartan.BasicElements;

public struct Grid
{
    private Vector2 _postition;
    private float _width;
    private Vector2 _cellSize;
    private float _padding;
    private int _cellCount;
    private int _inRow;

    public void Begin(Vector2 postition, float width, Vector2 cellSize, float padding)
    {
        _postition = postition;
        _width = width;
        _cellSize = cellSize;
        _padding = padding;
        _inRow = (int)((_width - _padding * 2) / (_cellSize.X + _padding));
    }

    public Rect GetCell()
    {
        int row = _cellCount / _inRow;
        int col = _cellCount % _inRow;

        var rect = new Rect(
            _padding + col * (_cellSize.X + _padding),
            _padding + row * (_cellSize.Y + _padding),
            _cellSize.X,
            _cellSize.Y
        );

        _cellCount++;
        return rect;
    }

    public float GetHeightFor(int count)
    {
        if (count < 1) count = 1;

        int row = (count-1) / _inRow;
        return (_padding + row * (_cellSize.Y + _padding))
               + _cellSize.Y + _padding;
    }
}