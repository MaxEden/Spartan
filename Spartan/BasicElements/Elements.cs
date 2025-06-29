using System.Numerics;

namespace Spartan.BasicElements;

public static class Elements
{
    public static bool DrawButton(Rect area, IBlitter blitter, Input input, string text, Align align = Align.Center)
    {
        var rect = area.Pad(1);
        var over = input.Layout.HoversOver(rect);

        var clicked = over && input.Pointer.State == PointerState.GoingDown;

        blitter.DrawRect(rect, Color32.gray, CustomRect.Hoverable, Color32.Float(0.7f, 0.7f, 0.7f, 1));

        blitter.DrawText(rect, text, align);

        return clicked;
    }

    public static bool DrawToggle(Rect area, IBlitter blitter, Input input, bool value)
    {
        var rect = area.Pad(2);

        var over = input.Layout.HoversOver(rect);
        var clicked = over && input.Pointer.State == PointerState.GoingDown;
        var dotRect = rect.Pad(4);

        blitter.DrawRect(rect, Color32.black, CustomRect.Hoverable, Color32.gray);
        blitter.DrawRect(dotRect, value ? Color32.white : Color32.blue);

        if (clicked)
        {
            return !value;
        }

        return value;
    }

    public static bool DrawSlider(Rect rect, IBlitter blitter, Input input, float value, out float result)
    {
        return DrawSlider(rect, blitter, input, value, out result, 0, 1);
    }

    public static bool DrawSlider(Rect rect, IBlitter blitter, Input input, float value, out float result, float from,
        float to)
    {
        var slider = rect.MiddleStripe(4);

        float x = slider.xMin + value * slider.Width;
        float y = 0.5f * (slider.yMin + slider.yMax);

        if (input.Layout.HoversOver(rect) && input.Pointer.State == PointerState.Down)
        {
            var pos = input.Layout.LocalPointerPosition;
            x = pos.X;
            if (slider.xMin > x) x = slider.xMin;
            if (slider.xMax < x) x = slider.xMax;
        }

        var point = new Vector2(x, y);
        var thumb = Rect.FromCenterSize(point, slider.Height * 2);

        blitter.DrawRect(slider, Color32.orange.Lighter(), CustomRect.Hoverable, Color32.orange.Lighter(100));
        blitter.DrawRect(thumb, Color32.red, CustomRect.Hoverable, Color32.red.Lighter(100));

        if (input.Layout.HoversOver(rect) && input.Pointer.State == PointerState.Down)
        {
            float t = (point.X - slider.xMin) / (slider.xMax - slider.xMin);
            result = from + (to - from) * t;
            return true;
        }

        result = value;
        return false;
    }
}