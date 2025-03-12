using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Nui;
using TestBlit.WebUI;


namespace TestBlit;

public class WebBlitter : Blitter
{
    public Vector2 viewSize { get; set; }

    private byte[] _textBytes;
    private MemoryStream _stream;
    private BinaryWriter _writer;

    public WebBlitter()
    {      
        _textBytes = new byte[1000];
    }

    public Input Input;
    public Layout Layout => Input.Layout;
    public void Start()
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

        _onAfterDrawn = _onDrawn;
        _onDrawn = null;

        _defaultSize = viewSize;
        var defaultArea = new Rect(0, 0, _defaultSize.X, _defaultSize.Y);

        Layout.Start(defaultArea, Input);
        Layout._scroll.Start();
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

        var tmp = _onAfterDrawn;
        _onAfterDrawn = null;

        if (tmp != null)
        {
            tmp.Invoke();
        }
    }

    public void DrawRect(Rect rectDest, Color32 color, CustomRect customRect, Color32 color2)
    {
        if (rectDest.Y > viewSize.Y) return;

        var colorResult = color;

        if (customRect == CustomRect.Hoverable)
        {
            _writer.Write((byte)Command.RectHoverable);

            Write.WriteRect(_writer, rectDest);
            Write.WriteColor(_writer, color);
            Write.WriteColor(_writer, color2);
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
        Write.WriteRect(_writer, rectDest);
        Write.WriteColor(_writer, color);
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

    int charW = 6;
    int charH = 12;
    private byte[] _written;

    public byte[] WrittenBytes => _written;
    public List<Input.PointerEventType> PointerEvents = new List<Input.PointerEventType>();
    public List<Input.TextEventType> TextEvents = new List<Input.TextEventType>();
    public Vector2 GetTextLineSize(string text)
    {
        return new Vector2(charW * text.Length, charH);
    }

    public void DrawSelection(Rect rectDest, string text, int selectionStart, int selectionEnd, int caretPos)
    {
        if (rectDest.Y > viewSize.Y) return;
        if (string.IsNullOrEmpty(text)) return;

        var pos = new Vector2(rectDest.X + 2, rectDest.Y + (rectDest.Height - charH) / 2);

        _writer.Write((byte)Command.TextSelection);
        _writer.Write((short)pos.X);
        _writer.Write((short)pos.Y);

        _writer.Write((short)selectionStart);
        _writer.Write((short)selectionEnd);
        _writer.Write((short)caretPos);

        if (selectionStart >= 0 && selectionEnd > selectionStart)
        {
            int selCount = selectionEnd - selectionStart;
            DrawRectRaw(new Rect(pos.X + charW * selectionStart, pos.Y, charW * selCount, charH),
                Color32.blue);
        }

        int count = Encoding.ASCII.GetBytes(text, 0, text.Length, _textBytes, 0);

        _writer.Write((short)count);

        for (int i = 0; i < count; i++)
        {
            int index = _textBytes[i] - 32;

            Write.WriteUShortComp(_writer, index);
        }

        if (selectionStart < 0 || selectionStart == selectionEnd)
        {
            var rect1 = new Rect(pos.X + caretPos * charW, pos.Y, 1, charH);
            DrawRect(rect1, Color32.yellow);
        }
    }

    public void DrawText(Rect rectDest, string text, Align align = Align.Middle)
    {
        if (rectDest.Y > viewSize.Y) return;
        if (string.IsNullOrEmpty(text)) return;

        Vector2 pos;

        switch (align)
        {
            case Align.Center:
                pos = new Vector2(
                    rectDest.X + (rectDest.Width - charW * text.Length) / 2,
                    rectDest.Y + (rectDest.Height - charH) / 2);
                break;
            case Align.TopUp:
                pos = new Vector2(rectDest.X, rectDest.Y);
                break;
            case Align.Middle:
            default:
                pos = new Vector2(rectDest.X + 2, rectDest.Y + (rectDest.Height - charH) / 2);
                break;
        }

        if (align == Align.Center)
        {
        }

        _writer.Write((byte)Command.Text);
        _writer.Write((short)pos.X);
        _writer.Write((short)pos.Y);

        int count = Encoding.ASCII.GetBytes(text, 0, text.Length, _textBytes, 0);

        _writer.Write((short)count);

        for (int i = 0; i < count; i++)
        {
            int index = _textBytes[i] - 32;
            Write.WriteUShortComp(_writer, index);
        }
    }

    public int GetCaretPos(Rect area, string text, Vector2 pointer)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        var pos = new Vector2(area.X, area.Y + (area.Height - charH) / 2);

        float ipos = (pointer.X - pos.X) / charW;
        int index = (int)MathF.Round(ipos);
        if (index > text.Length) return text.Length;
        if (index < 0) return 0;
        return index;
    }

    public void TextEntered(string str)
    {
        InputString += str;
        TextEvents.Add(Input.TextEventType.Entered);
    }

    public string InputString = String.Empty;
    private Action _onDrawn;
    private Action _onAfterDrawn;
    private Vector2 _defaultSize;

    public void OnDrawn(Action action)
    {
        _onDrawn = action;
    }


    public float BeginScroll(float shift, Rect area, float height)
    {
        Layout.layer.IsClipping = true;
        Layout.layer.ClipInnerArea = new Rect(area.X, area.Y - shift, area.Width, height);
        Layout.layer.ClipOuterArea = area;


        float shiftResult = Layout._scroll.BeginScroll(area, shift, height, Input);


        _writer.Write((byte)Command.ScrollBegin);
        Write.WriteRect(_writer, area);
        _writer.Write((short)Layout._scroll._scrollHeight);
        Write.WriteUShortComp(_writer, (int)shiftResult);
        _writer.Write(Layout._scroll._scrollIsActive);

        return shiftResult;
    }

    public void EndScroll()
    {
        if (Layout._scroll._scrollRectEnabled)
        {
            DrawRectRaw(Layout._scroll._scrollRect, Layout._scroll._scrollColor);
        }

        _writer.Write((byte)Command.ScrollEnd);

        Layout._scroll.EndScroll(Input);
        //Layout.CurrentArea = Layout._defaultArea;
        Layout.layer.IsClipping = false;
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


