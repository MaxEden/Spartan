using System.Numerics;

namespace Spartan.TestApi;

public static class Pad
{
    public static Rect Inside(Rect rect, float pad)
    {
        return new Rect(rect.X + pad, rect.Y + pad, rect.Width - 2 * pad, rect.Height - 2 * pad);
    }

    public static Rect AlignMiddle(Vector2 size, Rect rectDst)
    {
        var rect = new Rect(rectDst.X, rectDst.Y + (rectDst.Height - size.Y) / 2,
            rectDst.Width,
            size.Y
        );
        return rect;
    }
}