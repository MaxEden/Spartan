namespace Spartan.TestApi;

public static class Elements
{
    public static bool DrawButton(Rect area, IBlitter blitter, Input input, string text)
    {
        var rect = Pad.Inside(area, 1);
        var over = input.Layout.HoversOver(rect, input.Layout.Get_defaultPointerPos());

        var clicked = over && input.DefaultPointer.State == Input.PointerState.GoingDown;
        
        blitter.DrawRect(rect, Color32.gray, CustomRect.Hoverable, Color32.Float(0.7f, 0.7f, 0.7f, 1));

        blitter.DrawText(rect, text, Align.Center);

        return clicked;
    }
    public static bool DrawToggle(Rect area, IBlitter blitter, Input input, bool value)
    {
        var rect = Pad.Inside(area, 2);

        var over = input.Layout.HoversOver(rect, input.Layout.Get_defaultPointerPos());
        var clicked = over && input.DefaultPointer.State == Input.PointerState.GoingDown;
        var dotRect = Pad.Inside(rect, 4);
        
        blitter.DrawRect(rect, Color32.black, CustomRect.Hoverable, Color32.gray);
        blitter.DrawRect(dotRect, value ? Color32.white : Color32.blue);

        if (clicked)
        {
            return !value;
        }

        return value;
    }    
}
