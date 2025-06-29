using System.Numerics;
using Spartan.BasicElements;

namespace Spartan.TestApi;

class Menu
{
    private readonly string[] _items;

    public Menu(params string[] items)
    {
        _items = items;
        _activeIndex = -1;
    }

    private readonly List<string[]> _subItems = new List<string[]>();
    private Rect _activeRect;
    private int _activeIndex;

    public void Add(params string[] subitems)
    {
        _subItems.Add(subitems);
    }

    public void Draw(Rect area, IBlitter blitter, Input input)
    {
        float x = 0;
        int index = -1;

        blitter.DrawRect(area, Color32.white);

        for (int i = 0; i < _items.Length; i++)
        {
            var size = blitter.GetTextLineSize(_items[i]);
            size.X += 20;

            var rect = new Rect(area.X + x, area.Y, size.X, area.Height);
            x += size.X;

            if (Elements.DrawButton(rect, blitter, input, _items[i]))
            {
                index = i;
                if (_subItems.Count > i)
                {                    
                    _activeRect = input.Layout.ToScreen(rect);
                    _activeIndex = i;
                }
                else
                {
                    _activeIndex = -1;
                }
            }
        }

        if (_activeIndex >= 0)
        {
            blitter.BeginPopup();

            var items = _subItems[_activeIndex];
            var pos = new Vector2(_activeRect.X, _activeRect.Y + _activeRect.Height);
            int h = 20;
            var fullRect = new Rect(pos.X, pos.Y, 200, h * items.Length);

            blitter.DrawRect(fullRect, Color32.blue);

            for (int i = 0; i < items.Length; i++)
            {
                var rect = new Rect(pos.X, pos.Y + i * h, 200, h);
                Elements.DrawButton(rect, blitter, input, items[i]);
                fullRect.Y += h;
            }

            blitter.EndPopup();
        }
    }
}
