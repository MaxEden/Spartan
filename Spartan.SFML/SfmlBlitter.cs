using System.Numerics;
using System.Text;
using Nui;
using SFML.Graphics;
using SFML.System;
using SColor = SFML.Graphics.Color;

namespace TestBlit;

public class SfmlBlitter : Blitter
{
    Vector2f viewSize { get; set; }

    private RenderTarget render;
    
    private readonly Texture texture;
    private Sprite sprite;
    private IntRect rectWhite;
    private readonly IntRect border;
    private byte[] _textBytes;
    private RenderWindow _defaultWindow;

    public SfmlBlitter(RenderWindow window, Texture texture)
    {
        _defaultWindow = window;
        this.render = window;
        this.texture = texture;
        this.sprite = new Sprite();
        sprite.Texture = texture;

        rectWhite = new IntRect(96, 0, 8, 8);
        border = new IntRect(112, 0, 8, 8);

        _textBytes = new byte[1000];

        _popupTexture = new RenderTexture(window.Size.X, window.Size.Y);
    }

    public Input Input;
    public Layout Layout => Input.Layout;
    public void Start()
    {
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

        var view = render.GetView();
        viewSize = view.Size;

        if (_defaultWindow.Size != _popupTexture.Size)
        {
            _popupTexture.Dispose();
            _popupTexture = new RenderTexture(_defaultWindow.Size.X, _defaultWindow.Size.Y);
        }

        _popupTexture.Clear(SColor.Transparent);

        _defaultViewport = view.Viewport;
        _defaultSize = view.Size;
        var defaultArea = new Rect(0, 0, _defaultSize.X, _defaultSize.Y);

        Layout.Start(defaultArea, Input);
        Layout._scroll.Start();
    }

    public void End()
    {
        _popupTexture.Display();

        sprite.Texture = _popupTexture.Texture;
        sprite.Position = new Vector2f(0, 0);
        sprite.Scale = new Vector2f(1, 1);
        sprite.Color = SColor.White;
        sprite.TextureRect = new IntRect(0, 0, (int)_popupTexture.Size.X, (int)_popupTexture.Size.Y);

        _defaultWindow.Draw(sprite);


        //foreach (var item in Layout.Popups._popupMasks)
        //{
        //    DrawRectRaw(item, new Color32(255, 0, 255, 200), false);
        //}

        Layout.End();

        var tmp = _onAfterDrawn;
        _onAfterDrawn = null;

        tmp?.Invoke();
    }

    public void DrawRect(Rect rectDest, Color32 color, CustomRect customRect, Color32 color2)
    {
        if (rectDest.Y > viewSize.Y) return;

        var colorResult = color;

        if (customRect == CustomRect.Hoverable)
        {
            if (Layout.HoversOver(rectDest, Layout.Get_defaultPointerPos()))
            {
                colorResult = color2;
            }
        }

        DrawRectRaw(rectDest, colorResult);
    }

    private void DrawRectRaw(Rect rectDest, Color32 color, bool addMask = true)
    {
        sprite.Color = new SColor(color.r, color.g, color.b, color.a);

        sprite.Texture = texture;
        sprite.TextureRect = rectWhite;
        sprite.Scale = new Vector2f(rectDest.Width / (float)rectWhite.Width,
            rectDest.Height / (float)rectWhite.Height);
        sprite.Position = new Vector2f(rectDest.X, rectDest.Y);
        render.Draw(sprite);

        if(addMask)        Layout.TryAddMask(rectDest);
    }
       
    public void DrawRect(Rect rectDest, Color32 color)
    {
        DrawRectRaw(rectDest, color);
    }


    int charW = 6;
    int charH = 12;

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

        if (selectionStart >= 0 && selectionEnd > selectionStart)
        {
            int selCount = selectionEnd - selectionStart;
            DrawRectRaw(new Rect(pos.X + charW * selectionStart, pos.Y, charW * selCount, charH),
                Color32.blue);
        }

        int count = Encoding.ASCII.GetBytes(text, 0, text.Length, _textBytes, 0);
        
        for (int i = 0; i < count; i++)
        {
            int index = _textBytes[i] - 32;
            
            int y = index / 16;
            int x = index % 16;

            var rect = new IntRect(x * charW, y * charH, charW, charH);
            sprite.Position = new Vector2f(pos.X + i * charW, pos.Y);

            sprite.TextureRect = rect;
            sprite.Scale = new Vector2f(1, 1);
            sprite.Color = SColor.Black;

            if (i >= selectionStart && i < selectionEnd)
            {
                sprite.Color = SColor.White;
            }

            render.Draw(sprite);
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

 
        int count = Encoding.ASCII.GetBytes(text, 0, text.Length, _textBytes, 0);
        

        for (int i = 0; i < count; i++)
        {
            int index = _textBytes[i] - 32;

            int y = index / 16;
            int x = index % 16;

            var rect = new IntRect(x * charW, y * charH, charW, charH);
            sprite.Position = new Vector2f(pos.X + i * charW, pos.Y);

            sprite.TextureRect = rect;
            sprite.Scale = new Vector2f(1, 1);
            sprite.Color = SFML.Graphics.Color.Black;
            render.Draw(sprite);
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

    public void OnDrawn(Action action)
    {
        _onDrawn = action;
    }
       

    FloatRect _defaultViewport;
    Vector2f _defaultSize;
   
    private RenderTexture _popupTexture;   

    public float BeginScroll(float shift, Rect area, float height)
    {
        var size = render.Size;
        var view = render.GetView();
        
        view.Viewport = new FloatRect(area.X / size.X, area.Y / size.Y, area.Width / size.X, area.Height / size.Y);
        view.Size = new Vector2f(area.Width, area.Height);
        view.Center = new Vector2f(area.Width * 0.5f, area.Height * 0.5f + shift);
        render.SetView(view);

        Layout.layer.IsClipping = true;
        Layout.layer.ClipInnerArea = new Rect(area.X, area.Y - shift, area.Width, height);
        Layout.layer.ClipOuterArea = area;

       float shiftResult = Layout._scroll.BeginScroll(area, shift, height, Input);
        
        return shiftResult;
    }

    public void EndScroll()
    {
        var view = render.GetView();
        view.Viewport = _defaultViewport;
        view.Size = _defaultSize;
        view.Center = new Vector2f(_defaultSize.X / 2, _defaultSize.Y / 2);
        render.SetView(view);
        _popupTexture.SetView(view);              

        if (Layout._scroll._scrollRectEnabled)
        {
            DrawRectRaw(Layout._scroll._scrollRect, Layout._scroll._scrollColor);
        }
        
        Layout._scroll.EndScroll(Input);
        //Layout.CurrentArea = Layout._defaultArea;
        Layout.layer.IsClipping = false;
    }

    public void BeginPopup()
    {
        Layout.BeginPopup();
        render = _popupTexture;
    }

    public void EndPopup()
    {
        Layout.EndPopup();
        render = _defaultWindow;
    }
}

