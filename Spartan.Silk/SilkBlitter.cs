using System.Numerics;

namespace Spartan.Silk
{
    internal class SilkBlitter : IBlitter
    {
        public Input Input;
        public Layout Layout => Input.Layout;

        private Vector2 viewSize => Layout.ViewSize;
        public TexQuadList MainLayer => _mainLayer;
        public TexQuadList PopupLayer => _popupLayer;

        private TexQuadList _mainLayer = new();
        private TexQuadList _popupLayer = new();
        private TexQuadList _layer;

        public DefaultTextRenderer DefaultTextRenderer = new();
        private readonly Vector2 _fontSize;

        public SilkBlitter(Vector2 fontSize)
        {
            _fontSize = fontSize;
            _mainLayer.Init(_fontSize, new Rect(98, 2, 1, 1));
            _popupLayer.Init(_fontSize, new Rect(98, 2, 1, 1));
        }
        public void Begin()
        {
            _layer = _mainLayer;
            _mainLayer.Clear();
            _popupLayer.Clear();

          

            Layout.ViewSize = viewSize;
            var defaultArea = new Rect(0, 0, viewSize.X, viewSize.Y);
            Layout.Start(defaultArea, Input);
        }

        public void End()
        {
            Layout.End();
        }

        public void DrawRect(Rect rectDest, Color32 color, CustomRect customRect, Color32 color2)
        {
            if (rectDest.Y > viewSize.Y) return;

            var colorResult = color;

            if (customRect == CustomRect.Hoverable)
            {
                if (Layout.HoversOver(rectDest))
                {
                    colorResult = color2;
                }
            }

            DrawRectRaw(rectDest, colorResult);
        }

        public void DrawRect(Rect rectDest, Color32 color)
        {
            DrawRectRaw(rectDest, color);
        }

        private void DrawRectRaw(Rect rectDest, Color32 color, bool addMask = true)
        {
            _layer.AddSolid(rectDest, color);
            if (addMask) Layout.TryAddMask(rectDest);
        }

        public Vector2 GetTextLineSize(string text)
        {
            return DefaultTextRenderer.GetTextLineSize(text);
        }

        public int GetCaretPos(Rect area, string text, Vector2 pointer)
        {
            return DefaultTextRenderer.GetCaretPos(area, text, pointer);
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
                var position = DefaultTextRenderer.GliphsTo[i];
                var textureRect = DefaultTextRenderer.GliphsFrom[i];
                var color = new ColorF(0, 0, 0, 1);

                if (i >= selectionStart && i < selectionEnd)
                {
                    color = new ColorF(1, 1, 1, 1);
                }

                _layer.Add(textureRect, new Rect(position, textureRect.size), color);
            }

            if (DefaultTextRenderer.CoursorRect.HasValue)
            {
                DrawRect(DefaultTextRenderer.CoursorRect.Value, Color32.yellow);
            }
        }

        public void DrawText(Rect rectDest, string text, Align align = Align.Middle)
        {
            //if (rectDest.Y > viewSize.Y) return;
            if (string.IsNullOrEmpty(text)) return;

            Vector2 pos = DefaultTextRenderer.GetTextPosition(rectDest, text, align);
            DefaultTextRenderer.BuildGliphRects(pos, text);

            for (int i = 0; i < DefaultTextRenderer.GliphCount; i++)
            {
                var position = DefaultTextRenderer.GliphsTo[i];
                var textureRect = DefaultTextRenderer.GliphsFrom[i];
                var color = new ColorF(0, 0, 0, 1);

                _layer.Add(textureRect, new Rect(position, textureRect.size), color);
            }
        }

        public float BeginScroll(float shift, Rect area, float height)
        {
            float shiftResult = Layout.BeginScroll(area, shift, height, Input);
            _layer.BeginScroll(shiftResult, area, height);
            return shiftResult;
        }

        public void EndScroll()
        {
            _layer.EndScroll();

            if (Layout.Scroll.ScrollRectEnabled)
            {
                DrawRectRaw(Layout.Scroll.ScrollRect, Layout.Scroll.Color);
            }

            Layout.EndScroll();
        }
        
        public void BeginPopup()
        {
            Layout.BeginPopup();
            _layer = _popupLayer;
        }

        public void EndPopup()
        {
            Layout.EndPopup();
            _layer = _mainLayer;
        }
    }
}
