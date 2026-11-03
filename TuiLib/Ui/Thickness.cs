using System;
using System.Globalization;
using System.Linq;

namespace TuiLib.Ui;

public readonly struct Thickness : IEquatable<Thickness>
{
    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }

    public int Horizontal => Left + Right;
    public int Vertical => Top + Bottom;

    public Thickness(int uniform) : this(uniform, uniform, uniform, uniform)
    {
    }

    public Thickness(int horizontal, int vertical) : this(horizontal, vertical, horizontal, vertical)
    {
    }

    public Thickness(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public static Thickness Parse(string text)
    {
        int[] values = text
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.Parse(part.Trim(), CultureInfo.InvariantCulture))
            .ToArray();

        return values.Length switch
        {
            1 => new(values[0]),
            2 => new(values[0], values[1]),
            4 => new(values[0], values[1], values[2], values[3]),
            _ => throw new FormatException($"Invalid thickness '{text}'."),
        };
    }

    public bool Equals(Thickness other)
    {
        return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
    }

    public override bool Equals(object? obj) => obj is Thickness other && Equals(other);

    public override int GetHashCode() => (Left * 397) ^ (Top * 131) ^ (Right * 31) ^ Bottom;

    public override string ToString() => $"{Left},{Top},{Right},{Bottom}";
}
