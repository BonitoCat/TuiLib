namespace TuiLib.Ui;

public readonly struct Size
{
    public static readonly Size Empty = new(0, 0);
    public static readonly Size Infinite = new(Layout.Infinite, Layout.Infinite);

    public int Width { get; }
    public int Height { get; }

    public Size(int width, int height)
    {
        Width = width < 0 ? 0 : width;
        Height = height < 0 ? 0 : height;
    }

    public Size Deflate(Thickness thickness)
    {
        return new(Layout.Subtract(Width, thickness.Horizontal), Layout.Subtract(Height, thickness.Vertical));
    }

    public Size Inflate(Thickness thickness)
    {
        return new(Layout.Add(Width, thickness.Horizontal), Layout.Add(Height, thickness.Vertical));
    }

    public override string ToString() => $"{Width}x{Height}";
}
