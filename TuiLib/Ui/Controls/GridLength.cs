using System;
using System.Globalization;

namespace TuiLib.Ui.Controls;

public readonly struct GridLength : IEquatable<GridLength>
{
    public static readonly GridLength Auto = new(1, GridUnitType.Auto);
    public static readonly GridLength Star = new(1, GridUnitType.Star);

    public double Value { get; }
    public GridUnitType UnitType { get; }

    public bool IsAuto => UnitType == GridUnitType.Auto;
    public bool IsStar => UnitType == GridUnitType.Star;
    public bool IsAbsolute => UnitType == GridUnitType.Absolute;

    public GridLength(double value, GridUnitType unitType = GridUnitType.Absolute)
    {
        Value = value;
        UnitType = unitType;
    }

    public static GridLength Parse(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return Auto;
        }

        if (trimmed.EndsWith("*", StringComparison.Ordinal))
        {
            string weight = trimmed.Substring(0, trimmed.Length - 1);
            return new(weight.Length == 0 ? 1 : double.Parse(weight, CultureInfo.InvariantCulture), GridUnitType.Star);
        }

        return new(double.Parse(trimmed, CultureInfo.InvariantCulture));
    }

    public bool Equals(GridLength other) => UnitType == other.UnitType && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is GridLength other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode() ^ (int) UnitType;

    public override string ToString()
    {
        return UnitType switch
        {
            GridUnitType.Auto => "Auto",
            GridUnitType.Star => Value.Equals(1d) ? "*" : $"{Value.ToString(CultureInfo.InvariantCulture)}*",
            _ => Value.ToString(CultureInfo.InvariantCulture),
        };
    }
}
