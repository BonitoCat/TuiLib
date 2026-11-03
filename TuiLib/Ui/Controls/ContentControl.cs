using TuiLib.Ui.Data;

namespace TuiLib.Ui.Controls;

public class ContentControl : Element
{
    public static readonly TuiProperty<object?> ContentProperty = TuiProperty.Register<ContentControl, object?>(nameof(Content), null, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<DataTemplate?> ContentTemplateProperty = TuiProperty.Register<ContentControl, DataTemplate?>(nameof(ContentTemplate), null, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<Thickness> PaddingProperty = TuiProperty.Register<ContentControl, Thickness>(nameof(Padding), default, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<HorizontalAlignment> HorizontalContentAlignmentProperty = TuiProperty.Register<ContentControl, HorizontalAlignment>(nameof(HorizontalContentAlignment), HorizontalAlignment.Stretch, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<VerticalAlignment> VerticalContentAlignmentProperty = TuiProperty.Register<ContentControl, VerticalAlignment>(nameof(VerticalContentAlignment), VerticalAlignment.Stretch, PropertyFlags.AffectsMeasure);

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public DataTemplate? ContentTemplate
    {
        get => GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    public Element? Presenter { get; private set; }

    protected virtual Thickness ContentInset => Padding;

    protected override void OnPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property != ContentProperty && e.Property != ContentTemplateProperty)
        {
            return;
        }

        UpdatePresenter();
    }

    protected virtual Element? CreatePresenter(object? content)
    {
        if (content == null)
        {
            return null;
        }

        if (content is Element element)
        {
            return element;
        }

        DataTemplate? template = ContentTemplate;
        if (template != null && template.Match(content))
        {
            return template.Build(content);
        }

        return new TextBlock
        {
            Text = content.ToString(),
        };
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Thickness inset = ContentInset;
        if (Presenter == null)
        {
            return new Size(0, 0).Inflate(inset);
        }

        Presenter.Measure(availableSize.Deflate(inset));
        return Presenter.DesiredSize.Inflate(inset);
    }

    protected override void ArrangeOverride(Rect bounds)
    {
        if (Presenter == null)
        {
            return;
        }

        Rect area = bounds.Deflate(ContentInset);
        HorizontalAlignment horizontal = HorizontalContentAlignment;
        VerticalAlignment vertical = VerticalContentAlignment;
        int width = horizontal == HorizontalAlignment.Stretch ? area.Width : Presenter.DesiredSize.Width;
        int height = vertical == VerticalAlignment.Stretch ? area.Height : Presenter.DesiredSize.Height;
        Presenter.Arrange(Layout.Align(area, new(width, height), horizontal, vertical));
    }

    private void UpdatePresenter()
    {
        if (Presenter != null)
        {
            DetachChild(Presenter);
            Presenter = null;
        }

        Element? presenter = CreatePresenter(Content);
        if (presenter == null)
        {
            return;
        }

        Presenter = presenter;
        AttachChild(presenter, 0);
    }
}
