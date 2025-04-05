using System.Numerics;

namespace Spartan
{
    public class Input
    {
        public class Pointer
        {
            public Vector2      Position;
            public PointerState State;

            public float ScrollDelta { get; internal set; }
        }

        public enum PointerState
        {
            None,
            GoingDown,
            Down,
            GoingUp,
            Up
        }

        public readonly Pointer DefaultPointer = new Pointer();

        public void PostStep()
        {
            if(DefaultPointer.State == PointerState.None) return;
            if(DefaultPointer.State == PointerState.GoingDown)
            {
                DefaultPointer.State = PointerState.Down;
            }
            if(DefaultPointer.State == PointerState.GoingUp)
            {
                DefaultPointer.State = PointerState.Up;
            }

            InputTextEvent = TextEventType.None;
            InputText = string.Empty;
            InputShift = false;

           // DefaultPointer.ScrollDelta = default;
        }

        public void PointerEvent(Vector2 position, PointerEventType eventType)
        {
            switch(eventType)
            {

                case PointerEventType.Enter:
                    DefaultPointer.State = PointerState.Up;
                    break;
                case PointerEventType.Moved:
                    DefaultPointer.Position = position;
                    break;
                case PointerEventType.Left:
                    DefaultPointer.State = PointerState.None;
                    break;
                case PointerEventType.Down:
                    DefaultPointer.State = PointerState.GoingDown;
                    break;
                case PointerEventType.Up:
                    DefaultPointer.State = PointerState.GoingUp;
                    break;
                case PointerEventType.Scrolled:
                    DefaultPointer.ScrollDelta = position.Y;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
            }
        }

        public enum PointerEventType : byte
        {
            Enter=0,
            Moved,
            Down,
            Up,
            Left,
            Scrolled
        }

        public enum TextEventType : byte
        {
            None,
            Entered,
            Deleted,
            Backspaced,
            EnterPressed,
            Right,
            Left,
            Copy
        }

        public TextEventType InputTextEvent { get; private set; }
        public string InputText { get; private set; }
        public bool InputShift { get; set; }
        public Layout Layout = new Layout();

        public void TextEvent(TextEventType eventType, string inputText)
        {
            if (eventType == TextEventType.Entered && !string.IsNullOrEmpty(inputText))
            {
                InputText += inputText;
            }

            InputTextEvent = eventType;
        }

        public Action<string> SetToClipboard;

        public Action<bool> OpenKeyboard;

    }
}