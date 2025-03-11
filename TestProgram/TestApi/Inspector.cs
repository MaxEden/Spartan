using System;
using System.Globalization;
using System.Numerics;
using Nui;

namespace TestBlit.TestApi;

public class Inspector
{
    private int _stateId;
    private int _focusedId;
    private string _inputBuffer;

    private bool _selectionInProgress;
    private int _selectionFist;
    private int _selectionSecond;
    private int _caretPos;
    private EnumDropdown _enumBropdown;

    private bool GetSelection(out int start, out int end)
    {
        if (_selectionFist < 0 || _selectionFist == _selectionSecond)
        {
            start = 0;
            end = 0;
            return false;
        }

        if (_selectionSecond > _selectionFist)
        {
            start = _selectionFist;
            end = _selectionSecond;
            return true;
        }

        start = _selectionSecond;
        end = _selectionFist;
        return true;
    }

    private void ClearSelection()
    {
        _selectionFist = -1;
        _selectionSecond = -1;
        _selectionInProgress = false;
    }

    public void Draw(Rect area, Blitter blitter, Input input, object target)
    {
        _stateId = 0;

        //blitter.DrawRect(area, Color.white);
        var textHeight = blitter.GetTextLineSize("Test").Y;
        float height = textHeight + 6;
        int i = 0;
        foreach (var fieldInfo in target.GetType().GetFields())
        {
            var row1 = new Rect(area.X, area.Y + height * i, area.Width / 2, height);

            blitter.DrawRect(Pad.Inside(row1, 1), Color32.white);

            var row2 = new Rect(area.X + area.Width / 2, area.Y + height * i, area.Width / 2, height);

            blitter.DrawText(row1, fieldInfo.Name);

            if (fieldInfo.FieldType == typeof(bool))
            {
                var value = (bool)fieldInfo.GetValue(target);
                value = Elements.DrawToggle(row2, blitter, input, value);
                fieldInfo.SetValue(target, value);
            }
            else if (fieldInfo.FieldType == typeof(int))
            {
                if (DrawParsable<int>(row2, blitter, input, fieldInfo.GetValue(target), out var result))
                {
                    fieldInfo.SetValue(target, result);
                }
            }
            else if (fieldInfo.FieldType == typeof(float))
            {
                if (DrawParsable<float>(row2, blitter, input, fieldInfo.GetValue(target), out var result))
                {
                    fieldInfo.SetValue(target, result);
                }
            }
            else if (fieldInfo.FieldType == typeof(Vector2))
            {
                var value = (Vector2)fieldInfo.GetValue(target);

                var row21 = new Rect(row2.X, row2.Y, row2.Width / 2, row2.Height);
                var row22 = new Rect(row2.X + row2.Width / 2, row2.Y, row2.Width / 2, row2.Height);

                if (DrawParsable<float>(row21, blitter, input, value.X, out var result))
                {
                    value.X = result;
                    fieldInfo.SetValue(target, value);
                }

                if (DrawParsable<float>(row22, blitter, input, value.Y, out var result2))
                {
                    value.Y = result2;
                    fieldInfo.SetValue(target, value);
                }
            }
            else if (fieldInfo.FieldType == typeof(Vector3))
            {
                var value = (Vector3)fieldInfo.GetValue(target);

                var row21 = new Rect(row2.X, row2.Y, row2.Width / 3, row2.Height);
                var row22 = new Rect(row2.X + row2.Width / 3, row2.Y, row2.Width / 3, row2.Height);
                var row23 = new Rect(row2.X + 2 * row2.Width / 3, row2.Y, row2.Width / 3, row2.Height);

                if (DrawParsable<float>(row21, blitter, input, value.X, out var result))
                {
                    value.X = result;
                    fieldInfo.SetValue(target, value);
                }

                if (DrawParsable<float>(row22, blitter, input, value.Y, out var result2))
                {
                    value.Y = result2;
                    fieldInfo.SetValue(target, value);
                }

                if (DrawParsable<float>(row23, blitter, input, value.Z, out var result3))
                {
                    value.Z = result3;
                    fieldInfo.SetValue(target, value);
                }
            }
            else if (fieldInfo.FieldType == typeof(string))
            {
                var value = (string)fieldInfo.GetValue(target);
                if (DrawInput(row2, blitter, input, value))
                {
                    fieldInfo.SetValue(target, _inputBuffer);
                }
            }
            else if (fieldInfo.FieldType.IsEnum)
            {
                var value = fieldInfo.GetValue(target);
                if (Elements.DrawButton(row2, blitter, input, Enum.GetName(fieldInfo.FieldType, value)))
                {
                    _enumBropdown = new EnumDropdown
                    {
                        Rect = input.Layout.ToScreen(row2),
                        Values = Enum.GetValues(fieldInfo.FieldType),
                        Selected = p =>
                        {
                            fieldInfo.SetValue(target, p);
                            _enumBropdown = null;
                        }
                    };
                }
            }

            i++;
        }

        foreach (var methodInfo in target.GetType().GetMethods())
        {
            if (methodInfo.ReturnType == typeof(void))
            {
                var row1 = new Rect(area.X, area.Y + height * i, area.Width, height);
                if (Elements.DrawButton(row1, blitter, input, methodInfo.Name))
                {
                    methodInfo.Invoke(target, null);
                }
            }

            i++;
        }

        if (_enumBropdown != null)
        {
            _enumBropdown.Draw(blitter, input);

            //if(!input.Layout.Popups.Check(input.DefaultPointer.Position) &&               
            //    input.Layout.CursorDepth == 0 && input.DefaultPointer.State == Input.PointerState.GoingUp)
            //{
            //    _enumBropdown = null;
            //}
        }
    }

    public bool DrawParsable<T>(Rect area, Blitter blitter, Input input, object val, out T result)
        where T : IParsable<T>
    {
        var value = Convert.ToString(val, CultureInfo.InvariantCulture);
        if (DrawInput(area, blitter, input, value))
        {
            if (T.TryParse(_inputBuffer, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }
        }

        result = default;
        return false;
    }

    public bool DrawInput(Rect area, Blitter blitter, Input input, string value)
    {
        _stateId++;

        area = Pad.Inside(area, 1);
        var focused = _focusedId == _stateId;
        var pointer = input.DefaultPointer.Position;
        var over = area.Contains(pointer);
        var clicked = over && input.DefaultPointer.State == Input.PointerState.GoingDown;

        if (focused) blitter.DrawRect(area, Color32.gray);
        else blitter.DrawRect(area, Color32.green, CustomRect.Hoverable, Color32.blue);

        if (focused)
        {
            GetSelection(out var start, out var end);
            blitter.DrawSelection(area, _inputBuffer, start, end, _caretPos);
        }
        else
        {
            blitter.DrawText(area, value);
        }

        if (clicked && !focused)
        {
            _focusedId = _stateId;
            _inputBuffer = value;
            _selectionFist = 0;
            if (string.IsNullOrEmpty(value))
            {
                _selectionSecond = 0;
                _caretPos = 0;
            }
            else
            {
                _selectionSecond = value.Length;
                _caretPos = value.Length;
            }

            return false;
        }

        if (!focused) return false;

        if (clicked)
        {
            _caretPos = blitter.GetCaretPos(area, _inputBuffer, pointer);
            if (_caretPos >= 0)
            {
                _selectionFist = _caretPos;
                _selectionSecond = _caretPos;
                _selectionInProgress = true;
            }
        }

        if (input.DefaultPointer.State == Input.PointerState.Down && _selectionInProgress)
        {
            _caretPos = blitter.GetCaretPos(area, _inputBuffer, pointer);

            if (_selectionFist >= 0)
            {
                _selectionSecond = _caretPos;
            }
        }

        if (input.DefaultPointer.State == Input.PointerState.GoingUp)
        {
            _selectionInProgress = false;
        }

        if (input.InputTextEvent == Input.TextEventType.Entered && input.InputText != string.Empty)
        {
            if (string.IsNullOrEmpty(_inputBuffer))
            {
                _inputBuffer = input.InputText;
                _caretPos = _inputBuffer.Length;
            }
            else if (GetSelection(out var start, out var end))
            {
                var result = _inputBuffer.Remove(start, end - start);
                result = result.Insert(start, input.InputText);
                _caretPos = start + input.InputText.Length;
                _inputBuffer = result;
                ClearSelection();
            }
            else if (_caretPos >= 0 && _caretPos <= _inputBuffer.Length)
            {
                _inputBuffer = _inputBuffer.Insert(_caretPos, input.InputText);
                _caretPos += input.InputText.Length;
            }

            return true;
        }

        if (string.IsNullOrEmpty(_inputBuffer)) return false;

        if (input.InputTextEvent == Input.TextEventType.Backspaced)
        {
            if (GetSelection(out var start, out var end))
            {
                _inputBuffer = _inputBuffer.Remove(start, end - start);
                _caretPos = start;
                ClearSelection();
            }
            else if (_caretPos > 0)
            {
                _inputBuffer = _inputBuffer.Remove(_caretPos - 1, 1);
                _caretPos--;
            }

            return true;
        }
        else if (input.InputTextEvent == Input.TextEventType.Deleted)
        {
            if (GetSelection(out var start, out var end))
            {
                _inputBuffer = _inputBuffer.Remove(start, end - start);
                _caretPos = start;
                ClearSelection();
            }
            else if (_caretPos < _inputBuffer.Length)
            {
                _inputBuffer = _inputBuffer.Remove(_caretPos, 1);
                _caretPos = Math.Clamp(_caretPos, 0, _inputBuffer.Length);
            }

            return true;
        }
        else if (input.InputTextEvent == Input.TextEventType.Left)
        {
            if (input.InputShift)
            {
                if (_caretPos > 0)
                {
                    if (!_selectionInProgress)
                    {
                        _selectionInProgress = true;
                        _selectionFist = _caretPos;
                        _selectionSecond = _caretPos - 1;
                    }
                    else
                    {
                        _selectionSecond = _caretPos - 1;
                    }

                    _caretPos--;
                }
            }
            else if (GetSelection(out int start, out int end))
            {
                _caretPos = start;
                ClearSelection();
            }
            else
            {
                _caretPos--;
                _caretPos = Math.Clamp(_caretPos, 0, _inputBuffer.Length);
            }
        }
        else if (input.InputTextEvent == Input.TextEventType.Right)
        {
            if (input.InputShift)
            {
                if (_caretPos < _inputBuffer.Length)
                {
                    if (!_selectionInProgress)
                    {
                        _selectionInProgress = true;
                        _selectionFist = _caretPos;
                        _selectionSecond = _caretPos + 1;
                    }
                    else
                    {
                        _selectionSecond = _caretPos + 1;
                    }

                    _caretPos++;
                }
            }
            else if (GetSelection(out int start, out int end))
            {
                _caretPos = end;
                ClearSelection();
            }
            else
            {
                _caretPos++;
                _caretPos = Math.Clamp(_caretPos, 0, _inputBuffer.Length);
            }
        }
        else if (input.InputTextEvent == Input.TextEventType.EnterPressed)
        {
            _focusedId = 0;
            return false;
        }
        else if (input.InputTextEvent == Input.TextEventType.Copy)
        {
            if (GetSelection(out int start, out int end))
            {
                var str = _inputBuffer.Substring(start, end - start);
                if (!string.IsNullOrEmpty(str))
                {
                    input.SetToClipboard?.Invoke(str);
                }
            }
        }

        return false;
    }

    public Rect AlignMiddle(Vector2 size, Rect rectDst)
    {
        var rect = new Rect(rectDst.X, rectDst.Y + (rectDst.Height - size.Y) / 2,
            rectDst.Width,
            size.Y
        );
        return rect;
    }
}

public class EnumDropdown
{
    internal Rect Rect;
    internal Array Values;
    public Action<object> Selected;

    private float Shift;

    public void Draw(Blitter blitter, Input input)
    {
        blitter.BeginPopup();

        var clipRect = new Rect(Rect.X,
                Rect.Y,
                Rect.Width,
                Rect.Height * 2.5f);

        Shift = blitter.BeginScroll(Shift, clipRect, Rect.Height * Values.Length);

        for (int j = 0; j < Values.Length; j++)
        {
            //var rect = new Rect(Rect.X,
            //    Rect.Y + j * Rect.Height,
            //    Rect.Width,
            //    Rect.Height);

            var rect = new Rect(0,
                    0 + j * Rect.Height,
                    Rect.Width,
                    Rect.Height);

            if (Elements.DrawButton(rect, blitter, input, Values.GetValue(j).ToString()))
            {
                Selected(Values.GetValue(j));
                //break;
            }
        }

        blitter.EndScroll();
        blitter.EndPopup();
    }
}

public static class Pad
{
    public static Rect Inside(Rect rect, float pad)
    {
        return new Rect(rect.X + pad, rect.Y + pad, rect.Width - 2 * pad, rect.Height - 2 * pad);
    }

    public static Rect AlignMiddle(Vector2 size, Rect rectDst)
    {
        var rect = new Rect(rectDst.X, rectDst.Y + (rectDst.Height - size.Y) / 2,
            rectDst.Width,
            size.Y
        );
        return rect;
    }
}
