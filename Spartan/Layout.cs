using System.Collections.Generic;
using System.Numerics;
using Spartan;

namespace Spartan
{
    public class Layout
    {
        public int charW = 6;
        public int charH = 12;
        public Vector2 ViewSize { get; set; }

        internal Rect _defaultArea;
        internal Vector2 _defaultPointerPos;

        Layer defaultLayer = new Layer()
        {
            Depth = 0
        };

        Layer popupLayer = new Layer()
        {
            Depth = 1
        };

        public Layer layer;

        public void Start(Rect defaultArea, Input input)
        {
            _defaultPointerPos = input.DefaultPointer.Position;
            _defaultArea = defaultArea;
            //CurrentArea = _defaultArea;

            CursorDepth = 0;
            if (Popups.Check(_defaultPointerPos))
            {
                CursorDepth = 1;
            }

            Popups.FlipMasks();
            //else if (Clip.Check(_defaultPointerPos))
            //{
            //    CursorDepth = -1;
            //}

            layer = defaultLayer;

            Scroll.Start();
        }

        public Vector2 Get_defaultPointerPos()
        {
            return _defaultPointerPos;
        }

        public bool HoversOver(Rect rect, Vector2 _defaultPointerPos)
        {
            if (layer.Depth != CursorDepth) return false;

            if (layer.IsClipping)
            {
                if(Scroll.HoversScrollRect(_defaultPointerPos)) return false;
                if (!layer.ClipOuterArea.Contains(_defaultPointerPos)) return false;
                if (!rect.Contains(_defaultPointerPos - layer.ClipInnerArea.position)) return false;
            }
            else
            {
                if (!rect.Contains(_defaultPointerPos)) return false;
            }
           
            return true;
        }

        public void TryAddMask(Rect rect)
        {
            //if (IsClipping)
            //{
            //    Clip.TryAddMask(rect);
            //}

            if (IsPoppingUp)
            {
                var rect2 = ToScreen(rect);
                if (layer.IsClipping)
                {
                    rect2 = Intersect(rect2, layer.ClipOuterArea);
                }

                Popups.TryAddMask(rect2);
            }
        }

        public Rect Intersect(Rect input, Rect area) 
        {
            //return Rect.MinMaxRect(
            //    Mathf.Clamp(input.X, area.X, area.xMax),
            //    Mathf.Clamp(input.Y, area.Y, area.yMax),
            //    Mathf.Clamp(input.xMax, area.X, area.xMax),
            //    Mathf.Clamp(input.yMax, area.Y, area.yMax));

            float axMin = area.X;
            float axMax = area.X + area.Width;
            float ayMin = area.Y;
            float ayMax = area.Y + area.Height;

            float xMin = Math.Clamp(input.X, axMin, axMax);
            float yMin = Math.Clamp(input.Y, ayMin, ayMax);
            float xMax = Math.Clamp(input.X + input.Width, axMin, axMax);
            float yMax = Math.Clamp(input.Y + input.Height, ayMin, ayMax);

            return new Rect()
            {
                X = xMin,
                Y = yMin,
                Width = xMax - xMin,
                Height = yMax - yMin
            };
        }

        //public bool IsClipping;

        //public Rect CurrentArea;
        public int CursorDepth;
        public MaskLayer Popups = new MaskLayer();
        //public Layer Clip = new Layer();
        //internal Rect ClipArea;
        public bool IsPoppingUp;

        public Scroll Scroll = new Scroll();
        public void End()
        {
            //Popups.FlipMasks();
            //Clip.FlipMasks();
        }

        public Vector2 ToScreen(Vector2 pos)
        {
            if (layer.IsClipping)
            {
                return pos + layer.ClipInnerArea.position;
            }

            return pos;
        }

        public Rect ToScreen(Rect rect)
        {
            if (layer.IsClipping)
            {
                return new Rect()
                {
                    X = rect.X + layer.ClipInnerArea.X,
                    Y = rect.Y + layer.ClipInnerArea.Y,
                    Width = rect.Width,
                    Height = rect.Height
                };
            }

            return rect;
        }

        public void BeginPopup()
        {
            IsPoppingUp = true;
            layer = popupLayer;
        }

        public void EndPopup()
        {
            IsPoppingUp = false;
            layer = defaultLayer;
        }

        public float BeginScroll(Rect area, float shift, float height, Input input)
        {
            layer.IsClipping = true;
            layer.ClipInnerArea = new Rect(area.X, area.Y - shift, area.Width, height);
            layer.ClipOuterArea = area;

            float shiftResult = Scroll.BeginScroll(area, shift, height, input);
            return shiftResult;
        }

        public void EndScroll()
        {
            Scroll.EndScroll();
            //Layout.CurrentArea = Layout._defaultArea;
            layer.IsClipping = false;
        }
    }

    public class MaskLayer
    {
        public int Depth;
        public List<Rect> _popupMasks = new List<Rect>();
        internal void FlipMasks()
        {
            _popupMasks.Clear();
        }

        internal bool Check(Vector2 pos)
        {
            foreach (var mask in _popupMasks)
            {
                if (mask.Contains(pos)) return true;
            }

            return false;
        }

        internal void TryAddMask(Rect rect)
        {
            for (int i = 0; i < _popupMasks.Count; i++)
            {
                var r = _popupMasks[i];
                if (r.Contains(rect)) return;
            }
            _popupMasks.Add(rect);
        }
    }
}


public class Layer
{
    public int Depth;
    public bool IsClipping;
    public Rect ClipInnerArea;
    public Rect ClipOuterArea;
}