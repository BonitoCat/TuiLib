using System;
using InputLib;
using TuiLib.Ui.Data;
using TuiLib.Ui.Input;
using TuiLib.Ui.Rendering;

namespace TuiLib.Ui.Controls;

public class CheckBox : ContentControl
{
    public static readonly TuiProperty<bool?> IsCheckedProperty = TuiProperty.Register<CheckBox, bool?>(nameof(IsChecked), false, PropertyFlags.AffectsRender, BindingMode.TwoWay);
    public static readonly TuiProperty<bool> IsThreeStateProperty = TuiProperty.Register<CheckBox, bool>(nameof(IsThreeState), false);

    public event EventHandler<TuiEventArgs>? IsCheckedChanged;

    public bool? IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public bool IsThreeState
    {
        get => GetValue(IsThreeStateProperty);
        set => SetValue(IsThreeStateProperty, value);
    }

    public CheckBox()
    {
        Focusable = true;
    }

    protected virtual string Glyph => IsChecked switch
    {
        true => "[x]",
        null => "[-]",
        _ => "[ ]",
    };

    protected override Thickness ContentInset
    {
        get
        {
            Thickness padding = Padding;
            int glyphWidth = TextMeasure.GetWidth(Glyph) + (Content == null ? 0 : 1);
            return new(padding.Left + glyphWidth, padding.Top, padding.Right, padding.Bottom);
        }
    }

    public virtual void Toggle()
    {
        if (IsThreeState)
        {
            IsChecked = IsChecked switch
            {
                false => true,
                true => null,
                _ => false,
            };
            return;
        }

        IsChecked = IsChecked != true;
    }

    protected override void OnPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == IsCheckedProperty)
        {
            IsCheckedChanged?.Invoke(this, new());
        }
    }

    protected override void OnRender(DrawingContext context)
    {
        Rect bounds = Bounds;
        int y = bounds.Y + Padding.Top;
        Color? foreground = IsFocused && IsEffectivelyEnabled ? Theme.AccentForeground : EffectiveForeground;
        Color? background = IsFocused && IsEffectivelyEnabled ? Theme.Accent : null;
        context.DrawText(bounds.X + Padding.Left, y, Glyph, foreground, background, FontStyles);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key != TuiKey.Enter && !(e.Key == TuiKey.Character && e.Character == ' '))
        {
            return;
        }

        e.Handled = true;
        if (IsEffectivelyEnabled)
        {
            Toggle();
        }
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != MouseButton.Left)
        {
            return;
        }

        e.Handled = true;
        if (IsEffectivelyEnabled)
        {
            Toggle();
        }
    }
}
