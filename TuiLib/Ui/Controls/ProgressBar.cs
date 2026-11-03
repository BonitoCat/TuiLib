using System;
using System.Globalization;
using TuiLib.Ui.Rendering;

namespace TuiLib.Ui.Controls;

public class ProgressBar : Element
{
    public static readonly TuiProperty<double> ValueProperty = TuiProperty.Register<ProgressBar, double>(nameof(Value), 0, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<double> MinimumProperty = TuiProperty.Register<ProgressBar, double>(nameof(Minimum), 0, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<double> MaximumProperty = TuiProperty.Register<ProgressBar, double>(nameof(Maximum), 100, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<bool> ShowProgressTextProperty = TuiProperty.Register<ProgressBar, bool>(nameof(ShowProgressText), false, PropertyFlags.AffectsRender);

    private static readonly char[] Partials = { ' ', '▏', '▎', '▍', '▌', '▋', '▊', '▉' };

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public bool ShowProgressText
    {
        get => GetValue(ShowProgressTextProperty);
        set => SetValue(ShowProgressTextProperty, value);
    }

    public double Percentage
    {
        get
        {
            double range = Maximum - Minimum;
            return range <= 0 ? 0 : Math.Max(0, Math.Min(1, (Value - Minimum) / range));
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new(Layout.IsInfinite(availableSize.Width) ? 20 : availableSize.Width, 1);
    }

    protected override void OnRender(DrawingContext context)
    {
        Rect bounds = Bounds;
        Color? fill = Foreground ?? Theme.Accent;
        Color track = Background ?? Theme.TrackBackground;
        int eighths = (int) Math.Round(Percentage * bounds.Width * 8);
        int full = eighths / 8;
        int partial = eighths % 8;

        for (int x = 0; x < bounds.Width; x++)
        {
            char glyph = x < full ? '█' : x == full ? Partials[partial] : ' ';
            for (int y = bounds.Y; y < bounds.Bottom; y++)
            {
                context.DrawChar(bounds.X + x, y, glyph, fill, track);
            }
        }

        if (!ShowProgressText)
        {
            return;
        }

        string text = (Percentage * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        int start = bounds.X + (bounds.Width - text.Length) / 2;
        int row = bounds.Y + bounds.Height / 2;
        for (int i = 0; i < text.Length; i++)
        {
            bool filled = start + i - bounds.X < full;
            context.DrawChar(start + i, row, text[i], filled ? track : Theme.AccentForeground, filled ? fill : track);
        }
    }
}
