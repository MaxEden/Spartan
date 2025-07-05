using System.Numerics;

namespace Spartan;

public class DoubleClicker
{
    private DateTime _lastClickTime;
    private Vector2  _lastClickPosition;
    private int      _clickCount;
    private TimeSpan _doubleClickTime        = TimeSpan.FromMilliseconds(300);
    private float    _doubleClickMaxDistance = 5f;
    private bool     _doubleClicked;

    public bool IsDoubleClick()
    {
        return _doubleClicked;
    }

    public void ProcessDoubleClick(Vector2 position)
    {
        if (_doubleClicked) return;

        var now = DateTime.UtcNow;
        if (_clickCount == 0
            || now - _lastClickTime > _doubleClickTime
            || (_lastClickPosition - position).Length() > _doubleClickMaxDistance)
        {
            _clickCount = 1;
            _lastClickTime = now;
            _lastClickPosition = position;
            return;
        }

        _clickCount++;
        _lastClickTime = now;
        _lastClickPosition = position;

        if (_clickCount >= 2)
        {
            _doubleClicked = true;
        }
    }

    public void Clear()
    {
        if (_doubleClicked)
        {
            _doubleClicked = false;
            _clickCount = 0;
        }
    }
}