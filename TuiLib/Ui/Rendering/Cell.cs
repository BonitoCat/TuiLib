using TuiLib.Rendering;

namespace TuiLib.Ui.Rendering;

public struct Cell
{
    public static readonly Cell Blank = new(" ", null, null, FontStyles.None);

    public string Text { get; set; }
    public Color? Foreground { get; set; }
    public Color? Background { get; set; }
    public FontStyles Styles { get; set; }

    public bool IsContinuation => Text.Length == 0;

    public Cell(string text, Color? foreground, Color? background, FontStyles styles)
    {
        Text = text;
        Foreground = foreground;
        Background = background;
        Styles = styles;
    }

    public bool HasSameStyle(Cell other)
    {
        return Styles == other.Styles && ColorEquals(Foreground, other.Foreground) && ColorEquals(Background, other.Background);
    }

    public bool ContentEquals(Cell other)
    {
        return Text == other.Text && HasSameStyle(other);
    }

    public static bool ColorEquals(Color? a, Color? b)
    {
        bool aDefault = IsDefault(a);
        bool bDefault = IsDefault(b);
        if (aDefault || bDefault)
        {
            return aDefault == bDefault;
        }

        return a!.R == b!.R && a.G == b.G && a.B == b.B;
    }

    public static bool IsDefault(Color? color)
    {
        return color is null || color.Type != Color.ColorType.Normal;
    }
}
