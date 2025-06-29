using System.Numerics;
namespace Spartan;

public interface IBlitter
{
    void DrawRect(Rect rectDest, Color32 color, CustomRect customRect, Color32 color2);
    void DrawRect(Rect rectDest, Color32 color);
    Vector2 GetTextLineSize(string text);
    int GetCaretPos(Rect area, string text, Vector2 pointer);
    void DrawSelection(Rect rectDest, string text, int selectionStart, int selectionEnd, int caretPos);
    void DrawText(Rect rectDest, string text, Align align = Align.Middle);
    float BeginScroll(float shift, Rect area, float height);
    void EndScroll();
    void Begin();
    void End();
    void BeginPopup();
    void EndPopup();
    object LoadGraphic(string path);

    void DrawGraphic(Rect rest, object graphic);
    void DrawGraphic(Rect rest, object graphic, Color32 color1, CustomRect customRect, Color32 color2);
}


public enum Align
{
    Middle,
    Center,
    TopLeft
}

public enum CustomRect
{
    None,
    Hoverable,
    Blinking
}