using System.Numerics;
using System.Text;
using SFML.Graphics;
using SFML.System;
using Spartan;
using SColor = SFML.Graphics.Color;

namespace Spartan.SFML
{
    public class SfmlBlitter : IBlitter
    {
        private Vector2 viewSize => Layout.ViewSize;

        private RenderTarget render;

        private readonly Texture texture;
        private Sprite sprite;
        private IntRect rectWhite;
        private byte[] _textBytes;
        private RenderWindow _defaultWindow;

        public DefaultTextRenderer DefaultTextRenderer = new();

        public SfmlBlitter(RenderWindow window, Texture texture)
        {
            _defaultWindow = window;
            this.render = window;
            this.texture = texture;
            this.sprite = new Sprite();
            sprite.Texture = texture;

            rectWhite = new IntRect(96, 0, 8, 8);

            _textBytes = new byte[1000];

            _popupTexture = new RenderTexture(window.Size.X, window.Size.Y);
        }

        public Input Input;
        public Layout Layout => Input.Layout;
        public void Begin()
        {
            var view = render.GetView();
            Layout.ViewSize = new Vector2(view.Size.X, view.Size.Y);

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
            Layout.End();
        }


        public void DrawMasks()
        {
            foreach (var item in Input.Layout.Popups._popupMasks)
            {
                DrawRectRaw(item, new Color32(255, 0, 255, 200), false);
            }
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

            if (addMask) Layout.TryAddMask(rectDest);
        }

        public void DrawRect(Rect rectDest, Color32 color)
        {
            DrawRectRaw(rectDest, color);
        }


        //int charW = 6;
        //int charH = 12;
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

            if (DefaultTextRenderer.SelectRect.HasValue)
            {
                DrawRectRaw(DefaultTextRenderer.SelectRect.Value, Color32.blue);
            }
            
            DefaultTextRenderer.BuildGliphRects(pos, text);

            for (int i = 0; i < DefaultTextRenderer.GliphCount; i++)
            {
                sprite.Position = DefaultTextRenderer.GliphsTo[i].ToVector2f();
                sprite.TextureRect = DefaultTextRenderer.GliphsFrom[i].ToIntRect();
                sprite.Scale = new Vector2f(1, 1);
                sprite.Color = SColor.Black;

                if (i >= selectionStart && i < selectionEnd)
                {
                    sprite.Color = SColor.White;
                }

                render.Draw(sprite);
            }

            if (DefaultTextRenderer.CoursorRect.HasValue)
            {
                DrawRect(DefaultTextRenderer.CoursorRect.Value, Color32.yellow);
            }
        }

        public void DrawText(Rect rectDest, string text, Align align = Align.Middle)
        {
            if (rectDest.Y > viewSize.Y) return;
            if (string.IsNullOrEmpty(text)) return;

            Vector2 pos = DefaultTextRenderer.GetTextPosition(rectDest, text, align);
            DefaultTextRenderer.BuildGliphRects(pos, text);
            
            for (int i = 0; i < DefaultTextRenderer.GliphCount; i++)
            {
                sprite.Position = DefaultTextRenderer.GliphsTo[i].ToVector2f();
                sprite.TextureRect = DefaultTextRenderer.GliphsFrom[i].ToIntRect();
                sprite.Scale = new Vector2f(1, 1);
                sprite.Color = SColor.Black;

                render.Draw(sprite);
            }
        }

        public int GetCaretPos(Rect area, string text, Vector2 pointer)
        {
            return DefaultTextRenderer.GetCaretPos(area, text, pointer);
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
        
            float shiftResult = Layout.BeginScroll(area, shift, height, Input);
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

            if (Layout.Scroll.ScrollRectEnabled)
            {
                DrawRectRaw(Layout.Scroll.ScrollRect, Layout.Scroll.Color);
            }

            Layout.EndScroll();
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
}

