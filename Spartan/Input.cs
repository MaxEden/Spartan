using System.Numerics;
using static Spartan.Input;

namespace Spartan
{
    public class Input
    {
        public readonly Pointer Pointer = new Pointer();
        public readonly DragNDrop DragNDrop = new DragNDrop();

        public void PostStep()
        {
            UpdateDragNDrop();


            NextState(ref Pointer.State);
            NextState(ref Pointer.AltState);

            InputTextEvent = TextEventType.None;
            InputText = string.Empty;
            Shift = false;

            // DefaultPointer.ScrollDelta = default;
        }

        private void UpdateDragNDrop()
        {
            DragNDrop.Started = false;
            DragNDrop.Dropped = false;

            if (Pointer.State == PointerState.GoingDown)
            {
                DragNDrop.StartPosition = Pointer.Position;
                return;
            }

            if (Pointer.State == PointerState.Down
                && (Pointer.Position - DragNDrop.StartPosition).Length() > 2
                && !DragNDrop.IsDragging)
            {
                DragNDrop.Started = true;
                DragNDrop.IsDragging = true;
                return;
            }
            
            if (DragNDrop.IsDragging)
            {
                if (Pointer.State == PointerState.GoingUp)
                {
                    if (DragNDrop.Object != null)
                    {
                        DragNDrop.Dropped = true;
                        return;
                    }
                }

                if (Pointer.State == PointerState.Up)
                {
                    DragNDrop.Object = null;
                    DragNDrop.StartPosition = default;
                    DragNDrop.IsDragging = false;
                    return;
                }
            }
        }

        private void NextState(ref PointerState state)
        {
            if (state == PointerState.None) return;

            if (state == PointerState.GoingDown)
            {
                state = PointerState.Down;
            }

            if (state == PointerState.GoingUp)
            {
                state = PointerState.Up;
            }
        }

        public void PointerEvent(Vector2 position, PointerButton button, PointerEventType eventType)
        {
            switch (eventType)
            {
                case PointerEventType.Enter:
                    Pointer.State = PointerState.Up;
                    Pointer.AltState = PointerState.Up;
                    break;
                case PointerEventType.Moved:
                    Pointer.Position = position;
                    break;
                case PointerEventType.Left:
                    Pointer.State = PointerState.None;
                    Pointer.AltState = PointerState.None;
                    break;
                case PointerEventType.Down:
                    if (button == PointerButton.Main) Pointer.State = PointerState.GoingDown;
                    else Pointer.AltState = PointerState.GoingDown;
                    break;
                case PointerEventType.Up:
                    if (button == PointerButton.Main) Pointer.State = PointerState.GoingUp;
                    else Pointer.AltState = PointerState.GoingUp;
                    break;
                case PointerEventType.Scrolled:
                    Pointer.ScrollDelta = position.Y;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
            }
        }

        public enum PointerEventType : byte
        {
            Enter = 0,
            Moved,
            Down,
            Up,
            Left,
            Scrolled
        }

        public enum PointerButton : byte
        {
            Main,
            Alt,
        }

        public enum TextEventType : byte
        {
            None,
            Typed,
            Deleted,
            Backspaced,
            EnterPressed,
            Right,
            Left,
            Copy
        }

        public TextEventType InputTextEvent { get; private set; }
        public string InputText { get; private set; }
        public bool Shift { get; set; }
        public bool Ctrl { get; set; }

        public Layout Layout = new Layout();
        public void TextEvent(TextEventType eventType, string inputText)
        {
            if (eventType == TextEventType.Typed && !string.IsNullOrEmpty(inputText))
            {
                InputText += inputText;
            }

            InputTextEvent = eventType;
        }

        public Action<string> SetToClipboard;
        public Action<bool> OpenKeyboard;
        public bool ClickOver(Rect rect)
        {
            return Layout.HoversOver(rect) && Pointer.State == PointerState.GoingDown;
        }
        public bool ReleaseOver(Rect rect)
        {
            return Layout.HoversOver(rect) && Pointer.State == PointerState.GoingUp;
        }
        public bool ClickOutside(Rect area)
        {
            return !Layout.HoversOver(area) && Layout.Scroll.FocusedId == 0 && Pointer.State == PointerState.GoingDown;
        }
    }

    public enum PointerState
    {
        None,
        GoingDown,
        Down,
        GoingUp,
        Up
    }

    public class Pointer
    {
        public Vector2 Position;
        public PointerState State;
        public PointerState AltState;

        public float ScrollDelta { get; internal set; }
    }

    public class DragNDrop
    {
        public Vector2 StartPosition { get; internal set; }
        public object Object;
        public bool Started { get; internal set; }
        public bool IsDragging { get; internal set; }
        public bool Dropped { get; internal set; }
    }
}