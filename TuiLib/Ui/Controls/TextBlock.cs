using System;
using System.Collections.Generic;
using System.Linq;
using TuiLib.Ui.Rendering;

namespace TuiLib.Ui.Controls;

public class TextBlock : Element
{
    public static readonly TuiProperty<string?> TextProperty = TuiProperty.Register<TextBlock, string?>(nameof(Text), null, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<TextWrapping> TextWrappingProperty = TuiProperty.Register<TextBlock, TextWrapping>(nameof(TextWrapping), TextWrapping.NoWrap, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<TextTrimming> TextTrimmingProperty = TuiProperty.Register<TextBlock, TextTrimming>(nameof(TextTrimming), TextTrimming.None, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<TextAlignment> TextAlignmentProperty = TuiProperty.Register<TextBlock, TextAlignment>(nameof(TextAlignment), TextAlignment.Left, PropertyFlags.AffectsRender);

    [Content]
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public TextTrimming TextTrimming
    {
        get => GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        List<string> lines = GetLines(availableSize.Width);
        int width = lines.Count == 0 ? 0 : lines.Max(TextMeasure.GetWidth);
        return new(width, string.IsNullOrEmpty(Text) ? 0 : lines.Count);
    }

    protected override void OnRender(DrawingContext context)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        Rect bounds = Bounds;
        List<string> lines = GetLines(bounds.Width);
        bool ellipsis = TextTrimming == TextTrimming.CharacterEllipsis;
        for (int i = 0; i < lines.Count && i < bounds.Height; i++)
        {
            string line = lines[i];
            if (i == bounds.Height - 1 && i < lines.Count - 1 && ellipsis)
            {
                line = TextMeasure.Truncate(line + " …", bounds.Width, true);
            }
            else
            {
                line = TextMeasure.Truncate(line, bounds.Width, ellipsis);
            }

            int width = TextMeasure.GetWidth(line);
            int x = TextAlignment switch
            {
                TextAlignment.Center => bounds.X + Math.Max(0, (bounds.Width - width) / 2),
                TextAlignment.Right => bounds.X + Math.Max(0, bounds.Width - width),
                _ => bounds.X,
            };
            context.DrawText(x, bounds.Y + i, line, EffectiveForeground, null, FontStyles);
        }
    }

    private List<string> GetLines(int width)
    {
        string text = Text ?? string.Empty;
        if (TextWrapping == TextWrapping.Wrap && !Layout.IsInfinite(width))
        {
            return TextMeasure.Wrap(text, width);
        }

        return TextMeasure.SplitLines(text);
    }
}
