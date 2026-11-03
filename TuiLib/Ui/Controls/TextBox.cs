using System;
using System.Text;
using InputLib;
using TuiLib.Ui.Data;
using TuiLib.Ui.Input;
using TuiLib.Ui.Rendering;

namespace TuiLib.Ui.Controls;

public class TextBox : Element
{
    public static readonly TuiProperty<string> TextProperty = TuiProperty.Register<TextBox, string>(nameof(Text), string.Empty, PropertyFlags.AffectsRender, BindingMode.TwoWay);
    public static readonly TuiProperty<string?> WatermarkProperty = TuiProperty.Register<TextBox, string?>(nameof(Watermark), null, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<char> PasswordCharProperty = TuiProperty.Register<TextBox, char>(nameof(PasswordChar), '\0', PropertyFlags.AffectsRender);
    public static readonly TuiProperty<bool> IsReadOnlyProperty = TuiProperty.Register<TextBox, bool>(nameof(IsReadOnly), false);
    public static readonly TuiProperty<int> MaxLengthProperty = TuiProperty.Register<TextBox, int>(nameof(MaxLength), 0);
    public static readonly TuiProperty<int> CaretIndexProperty = TuiProperty.Register<TextBox, int>(nameof(CaretIndex), 0, PropertyFlags.AffectsRender);

    private int _scroll;

    public event EventHandler<TuiEventArgs>? TextChanged;

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public char PasswordChar
    {
        get => GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    public int CaretIndex
    {
        get => GetValue(CaretIndexProperty);
        set => SetValue(CaretIndexProperty, Layout.Clamp(value, 0, Text.Length));
    }

    public TextBox()
    {
        Focusable = true;
    }

    public void Clear()
    {
        Text = string.Empty;
    }

    protected override void OnPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property != TextProperty)
        {
            return;
        }

        if (CaretIndex > Text.Length)
        {
            CaretIndex = Text.Length;
        }

        TextChanged?.Invoke(this, new());
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int width = Math.Max(TextMeasure.GetWidth(DisplayText), TextMeasure.GetWidth(Watermark)) + 1;
        return new(Math.Min(width, availableSize.Width), 1);
    }

    protected override Color? GetRenderBackground()
    {
        return Background ?? (IsFocused ? Theme.InputFocusedBackground : Theme.InputBackground);
    }

    protected override void OnRender(DrawingContext context)
    {
        Rect bounds = Bounds;
        string display = DisplayText;
        if (display.Length == 0)
        {
            context.DrawText(bounds.X, bounds.Y, TextMeasure.Truncate(Watermark ?? string.Empty, bounds.Width), Theme.WatermarkForeground);
            _scroll = 0;
        }
        else
        {
            EnsureCaretVisible(display, bounds.Width);
            context.DrawText(bounds.X, bounds.Y, TextMeasure.Truncate(display.Substring(_scroll), bounds.Width), EffectiveForeground, null, FontStyles);
        }

        if (!IsFocused)
        {
            return;
        }

        int caret = Math.Min(CaretIndex, display.Length);
        int offset = TextMeasure.GetWidth(display.Substring(_scroll, Math.Max(0, caret - _scroll)));
        context.CursorPosition = new(bounds.X + Math.Min(offset, Math.Max(0, bounds.Width - 1)), bounds.Y);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        string text = Text;
        int caret = CaretIndex;
        switch (e.Key)
        {
            case TuiKey.Left:
                CaretIndex = e.Ctrl ? FindWordStart(text, caret) : caret - 1;
                break;
            case TuiKey.Right:
                CaretIndex = e.Ctrl ? FindWordEnd(text, caret) : caret + 1;
                break;
            case TuiKey.Home:
                CaretIndex = 0;
                break;
            case TuiKey.End:
                CaretIndex = text.Length;
                break;
            case TuiKey.Backspace when !IsReadOnly && caret > 0:
            {
                int start = e.Ctrl || e.Alt ? FindWordStart(text, caret) : caret - 1;
                ReplaceRange(start, caret - start, string.Empty);
                break;
            }
            case TuiKey.Delete when !IsReadOnly && caret < text.Length:
            {
                int end = e.Ctrl ? FindWordEnd(text, caret) : caret + 1;
                ReplaceRange(caret, end - caret, string.Empty);
                break;
            }
            case TuiKey.Character when e.Ctrl && e.IsCharacter('u') && !IsReadOnly:
                ReplaceRange(0, caret, string.Empty);
                break;
            case TuiKey.Character when e.Ctrl && e.IsCharacter('k') && !IsReadOnly:
                ReplaceRange(caret, text.Length - caret, string.Empty);
                break;
            case TuiKey.Character when e.Ctrl && e.IsCharacter('w') && !IsReadOnly:
            {
                int start = FindWordStart(text, caret);
                ReplaceRange(start, caret - start, string.Empty);
                break;
            }
            case TuiKey.Character when !e.Ctrl && !e.Alt:
            {
                TextInputEventArgs input = new(e.Text);
                OnTextInput(input);
                if (!input.Handled)
                {
                    return;
                }

                break;
            }
            default:
                return;
        }

        e.Handled = true;
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (IsReadOnly)
        {
            return;
        }

        StringBuilder filtered = new();
        foreach (char c in e.Text)
        {
            if (!char.IsControl(c))
            {
                filtered.Append(c);
            }
        }

        string insert = filtered.ToString();
        if (MaxLength > 0)
        {
            insert = insert.Substring(0, Math.Max(0, Math.Min(insert.Length, MaxLength - Text.Length)));
        }

        if (insert.Length == 0)
        {
            return;
        }

        e.Handled = true;
        ReplaceRange(CaretIndex, 0, insert);
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != MouseButton.Left)
        {
            return;
        }

        e.Handled = true;
        string display = DisplayText;
        int target = e.GetPosition(this).X;
        int width = 0;
        int index = _scroll;
        while (index < display.Length && width + TextMeasure.GetWidth(display[index].ToString()) <= target)
        {
            width += TextMeasure.GetWidth(display[index].ToString());
            index++;
        }

        CaretIndex = index;
    }

    private string DisplayText
    {
        get
        {
            string text = Text;
            return PasswordChar == '\0' ? text : new(PasswordChar, text.Length);
        }
    }

    private void ReplaceRange(int start, int length, string replacement)
    {
        string text = Text;
        start = Layout.Clamp(start, 0, text.Length);
        length = Layout.Clamp(length, 0, text.Length - start);
        Text = text.Substring(0, start) + replacement + text.Substring(start + length);
        CaretIndex = start + replacement.Length;
    }

    private void EnsureCaretVisible(string display, int width)
    {
        int caret = Math.Min(CaretIndex, display.Length);
        if (caret < _scroll)
        {
            _scroll = caret;
        }

        _scroll = Math.Min(_scroll, display.Length);
        while (_scroll < caret && TextMeasure.GetWidth(display.Substring(_scroll, caret - _scroll)) >= width)
        {
            _scroll++;
        }

        while (_scroll > 0 && TextMeasure.GetWidth(display.Substring(_scroll - 1)) < width)
        {
            _scroll--;
        }
    }

    private static int FindWordStart(string text, int caret)
    {
        int index = caret;
        while (index > 0 && char.IsWhiteSpace(text[index - 1]))
        {
            index--;
        }

        while (index > 0 && !char.IsWhiteSpace(text[index - 1]))
        {
            index--;
        }

        return index;
    }

    private static int FindWordEnd(string text, int caret)
    {
        int index = caret;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        while (index < text.Length && !char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        return index;
    }
}
