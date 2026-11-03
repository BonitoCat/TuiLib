using TuiLib.Rendering.Borders;
using TuiLib.Ui.Rendering;

namespace TuiLib.Ui.Controls;

public class Border : Element
{
    public static readonly TuiProperty<Element?> ChildProperty = TuiProperty.Register<Border, Element?>(nameof(Child), null, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<Thickness> BorderThicknessProperty = TuiProperty.Register<Border, Thickness>(nameof(BorderThickness), default, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<Color?> BorderBrushProperty = TuiProperty.Register<Border, Color?>(nameof(BorderBrush), null, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<IBorderStyle> BorderStyleProperty = TuiProperty.Register<Border, IBorderStyle>(nameof(BorderStyle), BorderStyles.Rounded, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<Thickness> PaddingProperty = TuiProperty.Register<Border, Thickness>(nameof(Padding), default, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<string?> TitleProperty = TuiProperty.Register<Border, string?>(nameof(Title), null, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<HorizontalAlignment> TitleAlignmentProperty = TuiProperty.Register<Border, HorizontalAlignment>(nameof(TitleAlignment), HorizontalAlignment.Left, PropertyFlags.AffectsRender);

    [Content]
    public Element? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public Color? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public IBorderStyle BorderStyle
    {
        get => GetValue(BorderStyleProperty);
        set => SetValue(BorderStyleProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public HorizontalAlignment TitleAlignment
    {
        get => GetValue(TitleAlignmentProperty);
        set => SetValue(TitleAlignmentProperty, value);
    }

    private Thickness Inset
    {
        get
        {
            Thickness border = BorderThickness;
            Thickness padding = Padding;
            return new(border.Left + padding.Left, border.Top + padding.Top, border.Right + padding.Right, border.Bottom + padding.Bottom);
        }
    }

    protected override void OnPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property != ChildProperty)
        {
            return;
        }

        if (e.OldValue is Element oldChild)
        {
            DetachChild(oldChild);
        }

        if (e.NewValue is Element newChild)
        {
            AttachChild(newChild);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Thickness inset = Inset;
        Element? child = Child;
        if (child == null)
        {
            return new Size(0, 0).Inflate(inset);
        }

        child.Measure(availableSize.Deflate(inset));
        Size desired = child.DesiredSize.Inflate(inset);
        int titleWidth = string.IsNullOrEmpty(Title) || BorderThickness.Top <= 0 ? 0 : TextMeasure.GetWidth(Title) + 4;
        return new(desired.Width < titleWidth ? titleWidth : desired.Width, desired.Height);
    }

    protected override void ArrangeOverride(Rect bounds)
    {
        Child?.Arrange(bounds.Deflate(Inset));
    }

    protected override void OnRender(DrawingContext context)
    {
        Thickness thickness = BorderThickness;
        if (thickness.Left <= 0 && thickness.Top <= 0 && thickness.Right <= 0 && thickness.Bottom <= 0)
        {
            return;
        }

        Rect bounds = Bounds;
        Color? brush = IsEffectivelyEnabled ? BorderBrush ?? Foreground : Theme.DisabledForeground;
        context.DrawBox(bounds, BorderStyle, thickness, brush);

        string? title = Title;
        if (string.IsNullOrEmpty(title) || thickness.Top <= 0 || bounds.Width < 5)
        {
            return;
        }

        string text = TextMeasure.Truncate($" {title} ", bounds.Width - 4, true);
        int width = TextMeasure.GetWidth(text);
        int x = TitleAlignment switch
        {
            HorizontalAlignment.Center => bounds.X + (bounds.Width - width) / 2,
            HorizontalAlignment.Right => bounds.Right - width - 2,
            _ => bounds.X + 2,
        };
        context.DrawText(x, bounds.Y, text, EffectiveForeground, null, FontStyles);
    }
}
