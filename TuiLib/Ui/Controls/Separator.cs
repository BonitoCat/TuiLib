using TuiLib.Components;
using TuiLib.Ui.Rendering;

namespace TuiLib.Ui.Controls;

public class Separator : Element
{
    public static readonly TuiProperty<Orientation> OrientationProperty = TuiProperty.Register<Separator, Orientation>(nameof(Orientation), Orientation.Horizontal, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<char?> CharacterProperty = TuiProperty.Register<Separator, char?>(nameof(Character), null, PropertyFlags.AffectsRender);

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public char? Character
    {
        get => GetValue(CharacterProperty);
        set => SetValue(CharacterProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return Orientation == Orientation.Horizontal ? new(0, 1) : new(1, 0);
    }

    protected override void OnRender(DrawingContext context)
    {
        Rect bounds = Bounds;
        Color? color = Foreground ?? Theme.BorderBrush;
        if (Orientation == Orientation.Horizontal)
        {
            context.DrawHorizontalLine(bounds.X, bounds.Y, bounds.Width, Character ?? '─', color);
            return;
        }

        context.DrawVerticalLine(bounds.X, bounds.Y, bounds.Height, Character ?? '│', color);
    }
}
