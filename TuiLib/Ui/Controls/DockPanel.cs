using System;

namespace TuiLib.Ui.Controls;

public class DockPanel : Panel
{
    public static readonly TuiProperty<Dock> DockProperty = TuiProperty.RegisterAttached<DockPanel, Dock>("Dock", Dock.Left);
    public static readonly TuiProperty<bool> LastChildFillProperty = TuiProperty.Register<DockPanel, bool>(nameof(LastChildFill), true, PropertyFlags.AffectsMeasure);

    public bool LastChildFill
    {
        get => GetValue(LastChildFillProperty);
        set => SetValue(LastChildFillProperty, value);
    }

    public static Dock GetDock(Element element) => element.GetValue(DockProperty);

    public static void SetDock(Element element, Dock value) => element.SetValue(DockProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        int usedWidth = 0;
        int usedHeight = 0;
        int maxWidth = 0;
        int maxHeight = 0;
        foreach (Element child in Children)
        {
            Size remaining = new(Layout.Subtract(availableSize.Width, usedWidth), Layout.Subtract(availableSize.Height, usedHeight));
            child.Measure(remaining);
            Size desired = child.DesiredSize;
            if (GetDock(child) is Dock.Left or Dock.Right)
            {
                maxHeight = Math.Max(maxHeight, usedHeight + desired.Height);
                usedWidth = Layout.Add(usedWidth, desired.Width);
                continue;
            }

            maxWidth = Math.Max(maxWidth, usedWidth + desired.Width);
            usedHeight = Layout.Add(usedHeight, desired.Height);
        }

        return new(Math.Max(maxWidth, usedWidth), Math.Max(maxHeight, usedHeight));
    }

    protected override void ArrangeOverride(Rect bounds)
    {
        int left = bounds.X;
        int top = bounds.Y;
        int right = bounds.Right;
        int bottom = bounds.Bottom;
        for (int i = 0; i < Children.Count; i++)
        {
            Element child = Children[i];
            Rect remaining = new(left, top, right - left, bottom - top);
            if (LastChildFill && i == Children.Count - 1)
            {
                child.Arrange(remaining);
                break;
            }

            Size desired = child.DesiredSize;
            switch (GetDock(child))
            {
                case Dock.Left:
                {
                    int width = Math.Min(desired.Width, remaining.Width);
                    child.Arrange(new(left, top, width, remaining.Height));
                    left += width;
                    break;
                }

                case Dock.Right:
                {
                    int width = Math.Min(desired.Width, remaining.Width);
                    child.Arrange(new(right - width, top, width, remaining.Height));
                    right -= width;
                    break;
                }

                case Dock.Top:
                {
                    int height = Math.Min(desired.Height, remaining.Height);
                    child.Arrange(new(left, top, remaining.Width, height));
                    top += height;
                    break;
                }

                default:
                {
                    int height = Math.Min(desired.Height, remaining.Height);
                    child.Arrange(new(left, bottom - height, remaining.Width, height));
                    bottom -= height;
                    break;
                }
            }
        }
    }
}
