using System;

namespace TuiLib.Ui;

public static class Layout
{
    public const int Infinite = int.MaxValue / 4;

    public static bool IsInfinite(int value) => value >= Infinite;

    public static int Add(int a, int b)
    {
        long sum = (long) a + b;
        return sum >= Infinite ? Infinite : (int) Math.Max(0, sum);
    }

    public static int Subtract(int a, int b)
    {
        if (IsInfinite(a))
        {
            return Infinite;
        }

        return Math.Max(0, a - b);
    }

    public static int Clamp(int value, int min, int max)
    {
        if (max < min)
        {
            max = min;
        }

        return value < min ? min : value > max ? max : value;
    }

    public static Rect Align(Rect area, Size size, HorizontalAlignment horizontal, VerticalAlignment vertical)
    {
        int width = Math.Min(size.Width, area.Width);
        int height = Math.Min(size.Height, area.Height);
        int x = horizontal switch
        {
            HorizontalAlignment.Left => area.X,
            HorizontalAlignment.Right => area.Right - width,
            _ => area.X + (area.Width - width) / 2,
        };
        int y = vertical switch
        {
            VerticalAlignment.Top => area.Y,
            VerticalAlignment.Bottom => area.Bottom - height,
            _ => area.Y + (area.Height - height) / 2,
        };

        return new(x, y, width, height);
    }
}
