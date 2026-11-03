namespace TuiLib.Ui.Controls;

public class Canvas : Panel
{
    public static readonly TuiProperty<int> LeftProperty = TuiProperty.RegisterAttached<Canvas, int>("Left", 0);
    public static readonly TuiProperty<int> TopProperty = TuiProperty.RegisterAttached<Canvas, int>("Top", 0);

    public static int GetLeft(Element element) => element.GetValue(LeftProperty);

    public static void SetLeft(Element element, int value) => element.SetValue(LeftProperty, value);

    public static int GetTop(Element element) => element.GetValue(TopProperty);

    public static void SetTop(Element element, int value) => element.SetValue(TopProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (Element child in Children)
        {
            child.Measure(Size.Infinite);
        }

        return Size.Empty;
    }

    protected override void ArrangeOverride(Rect bounds)
    {
        foreach (Element child in Children)
        {
            child.Arrange(new(bounds.X + GetLeft(child), bounds.Y + GetTop(child), child.DesiredSize.Width, child.DesiredSize.Height));
        }
    }
}
