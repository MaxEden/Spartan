using System.Numerics;

namespace Spartan;

public class Scroll
{
    public float ScrollPos;
    public Rect ScrollArea;

    public float Height;
    public bool IsHovered;
    public bool ScrollRectEnabled;
    public Rect ScrollRect;
    public Color32 Color;

    public int FocusedId;
    private int _uk;
    private float _pickShift;

    public bool IsActive => FocusedId > 0 && FocusedId == _uk;

    public float BeginScroll(Rect area, float scrollPos, float height, Input input)
    {
        _uk++;
        ScrollArea = area;
        Height = height;
        ScrollPos = scrollPos;

        if (IsActive && input.Pointer.State == PointerState.GoingUp)
        {
            FocusedId = 0;
        }


        var maxShift = height - area.Height;
        if (maxShift < 0) maxShift = 0;

        var scrollHeight = area.Height * (area.Height / height);
        var scrollHole = area.Height - scrollHeight;

        if (IsActive)
        {
            var pickY = input.Pointer.Position.Y - _pickShift;
            var diff = pickY - area.Y;
            diff = Math.Clamp(diff, 0, scrollHole);

            float t = diff / scrollHole;

            ScrollPos = t * maxShift;
        }


        {
            ScrollRectEnabled = height > area.Height;

            float t = ScrollPos / maxShift;

            ScrollRect = new Rect(
                area.X + area.Width - 10,
                area.Y + t * scrollHole,
                10,
                scrollHeight
                );
        }
        
        IsHovered = false;
        if (ScrollRectEnabled)
        {
            if (IsActive)
            {
                Color = new ColorF(0, 0, 0, 0.6f);
            }
            else
            {
                if (ScrollRect.Contains(input.Pointer.Position))
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
            if (input.Pointer.State == PointerState.GoingDown)
            {
                FocusedId = _uk;
                _pickShift = input.Pointer.Position.Y -ScrollRect.position.Y;
            }
        }

        if(input.Layout.layer.Depth == input.Layout.CursorDepth && area.Contains(input.Pointer.Position))
        {
            if (input.Pointer.ScrollDelta != 0)
            {
                ScrollPos -= input.Pointer.ScrollDelta;
                ScrollPos = Math.Clamp(ScrollPos, 0, maxShift);

                input.Pointer.ScrollDelta = 0;

                return ScrollPos;
            }
        }

        if (FocusedId == _uk)
        {
            return ScrollPos;
        }

        return scrollPos;
    }

    public bool HoversScrollRect(Vector2 pos)
    {
        return ScrollRectEnabled && ScrollRect.Contains(pos);
    }

    public void EndScroll()
    {

    }
    
    public void Start()
    {
        _uk = 0;
    }
}