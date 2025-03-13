using System.Numerics;
using System.Text;
using Spartan.Web.WebUI;

namespace Spartan.Web;

public class WebBlitter : IBlitter
{
    public Vector2 viewSize => Layout.ViewSize;
    public DefaultTextRenderer DefaultTextRenderer = new();
    
    private MemoryStream _stream;
    private BinaryWriter _writer;

    public WebBlitter() { }

    public Input Input;
    public Layout Layout => Input.Layout;

    public void Begin()
    {
        _stream = new MemoryStream();
        _writer = new BinaryWriter(_stream);

        foreach (var pointerEvent in PointerEvents)
        {
            Input.PointerEvent(default, pointerEvent);
        }

        PointerEvents.Clear();

        if (!string.IsNullOrEmpty(InputString))
        {
            Input.TextEvent(Input.TextEventType.Entered, InputString);
            InputString = String.Empty;
        }

        foreach (var textEventType in TextEvents)
        {
            Input.TextEvent(textEventType, null);
        }

        TextEvents.Clear();

        _defaultSize = viewSize;
        var defaultArea = new Rect(0, 0, _defaultSize.X, _defaultSize.Y);

        Layout.Start(defaultArea, Input);
        Layout.Scroll.Start();
    }

    public void End()
    {           
        foreach (var item in Layout.Popups._popupMasks)
        {
            DrawRectRaw(item, new Color32(255, 0, 255, 200), false);
        }

        Layout.End();

        _writer.Flush();
        _written = _stream.ToArray(); //22K //8K
    }

    public void DrawRect(Rect rectDest, Color32 color, CustomRect customRect, Color32 color2)
    {
        if (rectDest.Y > viewSize.Y) return;

        var colorResult = color;

        if (customRect == CustomRect.Hoverable)
        {
            _writer.Write((byte)Command.RectHoverable);
            _writer.WriteRect(rectDest);
            _writer.WriteColor(color);
            _writer.WriteColor(color2);
        }

        DrawRectRaw(rectDest, colorResult);
    }

    private void DrawRectRaw(Rect rectDest, Color32 color, bool addMask = true)
    {
        if (addMask) Layout.TryAddMask(rectDest);
    }

    public void DrawRect(Rect rectDest, Color32 color)
    {
        DrawRectRaw(rectDest, color);

        _writer.Write((byte)Command.Rect);
        _writer.WriteRect(rectDest);
        _writer.WriteColor(color);
    }

    enum Command : byte
    {
        Rect = 1,
        Text = 2,
        RectHoverable = 3,
        RectBlinking = 4,
        TextSelection = 5,
        ScrollBegin = 6,
        ScrollEnd = 7,
        PopupBegin = 8,
        PopupEnd = 9
    }

    private byte[] _written;
    public byte[] WrittenBytes => _written;
    public List<Input.PointerEventType> PointerEvents = new List<Input.PointerEventType>();
    public List<Input.TextEventType> TextEvents = new List<Input.TextEventType>();
    public Vector2 GetTextLineSize(string text)
    {
        return DefaultTextRenderer.GetTextLineSize(text);
    }

    public void DrawSelection(Rect rectDest, string text, int selectionStart, int selectionEnd, int caretPos)
    {
        if (rectDest.Y > viewSize.Y) return;
        if (string.IsNullOrEmpty(text)) return;

        DefaultTextRenderer.BuildSelection(rectDest, text, selectionStart, selectionEnd, caretPos);
        var pos = DefaultTextRenderer.SelectPos;

        _writer.Write((byte)Command.TextSelection);
        _writer.Write((short)pos.X);
        _writer.Write((short)pos.Y);

        _writer.Write((short)selectionStart);
        _writer.Write((short)selectionEnd);
        _writer.Write((short)caretPos);

        if (DefaultTextRenderer.SelectRect.HasValue)
        {
            DrawRectRaw(DefaultTextRenderer.SelectRect.Value, Color32.blue);
        }

        DefaultTextRenderer.BuildGliphRects(pos, text);

        int count = DefaultTextRenderer.GliphCount;

        _writer.Write((short)count);

        for (int i = 0; i < count; i++)
        {
            _writer.WriteUShortComp(DefaultTextRenderer.GliphsIndexes[i]);
        }
    }

    public void DrawText(Rect rectDest, string text, Align align = Align.Middle)
    {
        if (rectDest.Y > viewSize.Y) return;
        if (string.IsNullOrEmpty(text)) return;

        Vector2 pos = DefaultTextRenderer.GetTextPosition(rectDest, text, align);

        _writer.Write((byte)Command.Text);
        _writer.Write((short)pos.X);
        _writer.Write((short)pos.Y);

        DefaultTextRenderer.BuildGliphRects(pos, text);
        int count = DefaultTextRenderer.GliphCount;

        _writer.Write((short)count);

        for (int i = 0; i < count; i++)
        {
            _writer.WriteUShortComp(DefaultTextRenderer.GliphsIndexes[i]);
        }
    }

    public int GetCaretPos(Rect area, string text, Vector2 pointer)
    {
        return DefaultTextRenderer.GetCaretPos(area, text, pointer);
    }

    public void TextEntered(string str)
    {
        InputString += str;
        TextEvents.Add(Input.TextEventType.Entered);
    }

    public string InputString = String.Empty;
    private Vector2 _defaultSize;

    public float BeginScroll(float shift, Rect area, float height)
    {
        float shiftResult = Layout.BeginScroll(area, shift, height, Input);

        _writer.Write((byte)Command.ScrollBegin);
        _writer.WriteRect(area);
        _writer.Write((short)Layout.Scroll.Height);
        _writer.WriteUShortComp((int)shiftResult);
        _writer.Write(Layout.Scroll.IsActive);

        return shiftResult;
    }

    public void EndScroll()
    {
        if (Layout.Scroll.ScrollRectEnabled)
        {
            DrawRectRaw(Layout.Scroll.ScrollRect, Layout.Scroll.Color);
        }

        _writer.Write((byte)Command.ScrollEnd);

        Layout.EndScroll();
    }

    public void BeginPopup()
    {
        Layout.BeginPopup();

        _writer.Write((byte)Command.PopupBegin);
    }

    public void EndPopup()
    {
        Layout.EndPopup();
        _writer.Write((byte)Command.PopupEnd);
    }
}


