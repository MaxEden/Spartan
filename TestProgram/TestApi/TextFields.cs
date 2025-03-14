using Spartan;
using Spartan.TestApi;

namespace TestProgram.TestApi
{
    internal class TextFields
    {
        public string InputBuffer => _inputBuffer;

        private int _stateId;
        private int _focusedId;
        private string _inputBuffer;

        private bool _selectionInProgress;
        private int _selectionFist;
        private int _selectionSecond;
        private int _caretPos;

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

        public bool DrawInput(Rect area, IBlitter blitter, Input input, string value)
        {
            _stateId++;

            area = Pad.Inside(area, 1);
            var focused = _focusedId == _stateId;
            var pointer = input.DefaultPointer.Position;
            var over = area.Contains(pointer);
            var clicked = over && input.DefaultPointer.State == Input.PointerState.GoingDown;

            void DrawText()
            {
                var focused = _focusedId == _stateId;

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

                DrawText();
                return true;
            }

            if (focused)
            {
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

                    DrawText();
                    return true;
                }

                if (!string.IsNullOrEmpty(_inputBuffer))
                {
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

                        DrawText();
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

                        DrawText();
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
                        DrawText();
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
                }
            }

            DrawText();
            return false;
        }

        public void ResetState()
        {
            _stateId = 0;
        }
    }
}
