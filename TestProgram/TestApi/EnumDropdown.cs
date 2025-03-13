namespace Spartan.TestApi;

public class EnumDropdown
{
    internal Rect Rect;
    internal Array Values;
    public Action<object> Selected;

    private float Shift;

    public void Draw(IBlitter blitter, Input input)
    {
        blitter.BeginPopup();

        var clipRect = new Rect(Rect.X,
            Rect.Y,
            Rect.Width,
            Rect.Height * 2.5f);

        Shift = blitter.BeginScroll(Shift, clipRect, Rect.Height * Values.Length);

        for (int j = 0; j < Values.Length; j++)
        {
            //var rect = new Rect(Rect.X,
            //    Rect.Y + j * Rect.Height,
            //    Rect.Width,
            //    Rect.Height);

            var rect = new Rect(0,
                0 + j * Rect.Height,
                Rect.Width,
                Rect.Height);

            if (Elements.DrawButton(rect, blitter, input, Values.GetValue(j).ToString()))
            {
                Selected(Values.GetValue(j));
                //break;
            }
        }

        blitter.EndScroll();
        blitter.EndPopup();
    }
}