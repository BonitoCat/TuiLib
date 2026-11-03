using System;
using TuiLib.Components;

namespace TuiLib.Ui.Controls;

public class StackPanel : Panel
{
    public static readonly TuiProperty<Orientation> OrientationProperty = TuiProperty.Register<StackPanel, Orientation>(nameof(Orientation), Orientation.Vertical, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<int> SpacingProperty = TuiProperty.Register<StackPanel, int>(nameof(Spacing), 0, PropertyFlags.AffectsMeasure);

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public int Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        bool horizontal = Orientation == Orientation.Horizontal;
        Size childAvailable = horizontal ? new(Layout.Infinite, availableSize.Height) : new(availableSize.Width, Layout.Infinite);
        int along = 0;
        int across = 0;
        int visible = 0;
        foreach (Element child in Children)
        {
            child.Measure(childAvailable);
            if (!child.IsVisible)
            {
                continue;
            }

            visible++;
            along = Layout.Add(along, horizontal ? child.DesiredSize.Width : child.DesiredSize.Height);
            across = Math.Max(across, horizontal ? child.DesiredSize.Height : child.DesiredSize.Width);
        }

        along = Layout.Add(along, Math.Max(0, visible - 1) * Spacing);
        return horizontal ? new(along, across) : new(across, along);
    }

    protected override void ArrangeOverride(Rect bounds)
    {
        bool horizontal = Orientation == Orientation.Horizontal;
        int offset = horizontal ? bounds.X : bounds.Y;
        foreach (Element child in Children)
        {
            if (!child.IsVisible)
            {
                child.Arrange(new(bounds.X, bounds.Y, 0, 0));
                continue;
            }

            if (horizontal)
            {
                child.Arrange(new(offset, bounds.Y, child.DesiredSize.Width, bounds.Height));
                offset += child.DesiredSize.Width + Spacing;
                continue;
            }

            child.Arrange(new(bounds.X, offset, bounds.Width, child.DesiredSize.Height));
            offset += child.DesiredSize.Height + Spacing;
        }
    }
}
