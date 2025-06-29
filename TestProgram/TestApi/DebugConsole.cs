using Spartan.BasicElements;

namespace Spartan.TestApi;

public class DebugConsole
{
    private static List<string> Lines = new List<string>();

    public static void Log(string text)
    {
        Lines.Add(text);
    }

    public void Draw(Rect area, IBlitter blitter)
    {
        blitter.DrawRect(area, new ColorF(1f,0.5f, 0.5f, 1));
        
        float height = 30;
        for (int i = 0; i < Lines.Count; i++)
        {
            if(area.Y + height * i > area.Y + area.Height) return;

            var rect = new Rect(area.X, area.Y + height * i, area.Width, height);

            blitter.DrawRect(rect.Pad(1), new ColorF(1f, 0.7f, 0.7f, 1));

            blitter.DrawText(rect, Lines[Lines.Count - i - 1]);
        }
    }
}