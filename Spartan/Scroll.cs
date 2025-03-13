using System.Numerics;

namespace Spartan;

public class Scroll
{
    public float _scrollPos;
    public Rect _scrollArea;
    public float Height;
    //public bool _scrollIsActive;
    public bool IsHovered;
    public bool ScrollRectEnabled;
    public Rect ScrollRect;
    public Color32 Color;

    public int _uk;
    public int _focusedId;
    private float _pickShift;

    public bool _scrollIsActive => _focusedId > 0 && _focusedId == _uk;

    public float BeginScroll(Rect area, float scrollPos, float height, Input input)
    {
        _uk++;
        _scrollArea = area;
        Height = height;
        _scrollPos = scrollPos;

        if (_scrollIsActive && input.DefaultPointer.State == Input.PointerState.GoingUp)
        {
            _focusedId = 0;
        }


        var maxShift = height - area.Height;
        var scrollHeight = area.Height * (area.Height / height);
        var scrollHole = area.Height - scrollHeight;

        if (_scrollIsActive)
        {
            var pickY = input.DefaultPointer.Position.Y - _pickShift;
            var diff = pickY - area.Y;
            diff = Math.Clamp(diff, 0, scrollHole);

            float t = diff / scrollHole;

            _scrollPos = t * maxShift;
        }


        {
            ScrollRectEnabled = height > area.Height;

            float t = _scrollPos / maxShift;

            ScrollRect = new Rect(
                area.X + area.Width - 10,
                area.Y + t * scrollHole,
                10,
                scrollHeight
                );
        }

        //_scrollRectEnabled = GetScrollRect(_scrollArea, Height, _scrollPos, out _scrollRect);

        IsHovered = false;
        if (ScrollRectEnabled)
        {
            if (_scrollIsActive)
            {
                Color = new ColorF(0, 0, 0, 0.6f);
            }
            else
            {
                if (ScrollRect.Contains(input.DefaultPointer.Position))
                {
                    IsHovered = true;
                    Color = new ColorF(0, 0, 0, 0.4f);
                }
                else
                {
                    Color = new ColorF(0, 0, 0, 0.25f);
                }
            }
        }

        if (IsHovered)
        {
            if (input.DefaultPointer.State == Input.PointerState.GoingDown)
            {
                _focusedId = _uk;
                _pickShift = input.DefaultPointer.Position.Y -ScrollRect.position.Y;
            }
        }

        if(input.Layout.layer.Depth == input.Layout.CursorDepth && area.Contains(input.DefaultPointer.Position))
        {
            if (input.DefaultPointer.ScrollDelta != 0)
            {
                _scrollPos -= input.DefaultPointer.ScrollDelta;
                _scrollPos = Math.Clamp(_scrollPos, 0, maxShift);

                input.DefaultPointer.ScrollDelta = 0;

                return _scrollPos;
            }
        }

        if (_focusedId == _uk)
        {
            return _scrollPos;
        }

        return scrollPos;
    }

    public bool HoversScrollRect(Vector2 pos)
    {
        //var _scrollRectEnabled = GetScrollRect(_scrollArea, Height, _scrollPos, out _scrollRect);
        return ScrollRectEnabled && ScrollRect.Contains(pos);
    }

    public void EndScroll()
    {

    }

    //public static bool GetScrollRect(Rect area, float Height, float scrollPos, out Rect rect)
    //{
    //    if (Height > area.Height)
    //    {
    //        var pos = scrollPos / Height;
    //        var part = area.Height / Height;

    //        float length = part * area.Height;
    //        float bit = area.Height - length;

    //        rect = new Rect(area.X + area.Width - 10, area.Y + bit * pos, 10, length);
    //        return true;
    //    }
    //    else
    //    {
    //        rect = default;
    //        return false;
    //    }
    //}


    public void Start()
    {
        _uk = 0;
    }
}