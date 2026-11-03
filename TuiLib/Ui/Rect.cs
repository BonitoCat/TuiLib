using System;

namespace TuiLib.Ui;

public readonly struct Rect
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public int Right => X + Width;
    public int Bottom => Y + Height;
    public Size Size => new(Width, Height);
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public Rect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width < 0 ? 0 : width;
        Height = height < 0 ? 0 : height;
    }

    public bool Contains(Point point)
    {
        return point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;
    }

    public Rect Intersect(Rect other)
    {
        int left = Math.Max(X, other.X);
        int top = Math.Max(Y, other.Y);
        int right = Math.Min(Right, other.Right);
        int bottom = Math.Min(Bottom, other.Bottom);
        if (right <= left || bottom <= top)
        {
            return new(left, top, 0, 0);
        }

        return new(left, top, right - left, bottom - top);
    }

    public Rect Deflate(Thickness thickness)
    {
        return new(
            X + thickness.Left,
            Y + thickness.Top,
            Layout.Subtract(Width, thickness.Horizontal),
            Layout.Subtract(Height, thickness.Vertical));
    }

    public override string ToString() => $"{X},{Y} {Width}x{Height}";
}
