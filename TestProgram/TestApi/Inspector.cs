using System.Globalization;
using System.Numerics;
using Spartan.BasicElements;

namespace Spartan.TestApi;

public class Inspector
{
    private EnumDropdown _enumDropdown;

    private TextFields _textFields = new TextFields();


    public void Draw(Rect fullArea, IBlitter blitter, Input input, object target)
    {
        _textFields.ResetState();

        //blitter.DrawRect(area, Color.white);
        var textHeight = blitter.GetTextLineSize("Test").Y;
        float height = textHeight + 6;
        int i = 0;
        var stack = new Stack();
        stack.Begin(fullArea, height, 1);

        foreach (var fieldInfo in target.GetType().GetFields())
        {
            var area = stack.GetLine();
            area.Split2(out var row1, out var row2);

            blitter.DrawRect(row1.Pad(1), Color32.white);
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

                row2.Split2(out var row21, out var row22);

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

                row2.Split3(out var row21, out var row22, out var row23);

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
                if (_textFields.DrawInput(row2, blitter, input, value))
                {
                    fieldInfo.SetValue(target, _textFields.InputBuffer);
                }
            }
            else if (fieldInfo.FieldType.IsEnum)
            {
                var value = fieldInfo.GetValue(target);
                if (Elements.DrawButton(row2, blitter, input, Enum.GetName(fieldInfo.FieldType, value)))
                {
                    _enumDropdown = new EnumDropdown
                    {
                        Rect = input.Layout.ToScreen(row2),
                        Values = Enum.GetValues(fieldInfo.FieldType),
                        Selected = p =>
                        {
                            fieldInfo.SetValue(target, p);
                            _enumDropdown = null;
                        }
                    };
                }
            }

            i++;
        }

        foreach (var methodInfo in target.GetType().GetMethods())
        {
            if (!methodInfo.IsPublic) continue;
            if (methodInfo.Name.StartsWith("get_")) continue;
            if (methodInfo.Name.StartsWith("set_")) continue;

            if (methodInfo.ReturnType == typeof(void))
            {
                var row1 = stack.GetLine();
                if (Elements.DrawButton(row1, blitter, input, methodInfo.Name))
                {
                    methodInfo.Invoke(target, null);
                }
            }

            i++;
        }

        if (_enumDropdown != null)
        {
            _enumDropdown.Draw(blitter, input);
        }
    }

    public bool DrawParsable<T>(Rect area, IBlitter blitter, Input input, object val, out T result)
        where T : IParsable<T>
    {
        var value = Convert.ToString(val, CultureInfo.InvariantCulture);
        if (_textFields.DrawInput(area, blitter, input, value))
        {
            if (T.TryParse(_textFields.InputBuffer, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }
        }

        result = default;
        return false;
    }
}