using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using TuiLib.Rendering;
using TuiLib.Rendering.Borders;
using TuiLib.Ui.Controls;

namespace TuiLib.Ui;

public static class TypeConversion
{
    private static readonly Dictionary<Type, Func<string, object?>> Parsers = new();
    private static readonly object ParsersLock = new();

    static TypeConversion()
    {
        Register(ParseColor);
        Register(Thickness.Parse);
        Register(GridLength.Parse);
        Register(RowDefinitions.Parse);
        Register(ColumnDefinitions.Parse);
        Register(ParseBorderStyle);
        Register(ParseFontStyles);
    }

    public static void Register<T>(Func<string, T> parser)
    {
        lock (ParsersLock)
        {
            Parsers[typeof(T)] = text => parser(text);
        }
    }

    public static bool TryConvert(object? value, Type targetType, out object? result)
    {
        return TryConvert(value, targetType, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryConvert(object? value, Type targetType, CultureInfo culture, out object? result)
    {
        Type? underlying = Nullable.GetUnderlyingType(targetType);
        bool acceptsNull = !targetType.IsValueType || underlying != null;
        Type type = underlying ?? targetType;

        if (value == null)
        {
            result = acceptsNull ? null : Activator.CreateInstance(targetType);
            return acceptsNull;
        }

        if (type.IsInstanceOfType(value))
        {
            result = value;
            return true;
        }

        if (type == typeof(string))
        {
            result = value is IFormattable formattable ? formattable.ToString(null, culture) : value.ToString();
            return true;
        }

        try
        {
            if (value is string text)
            {
                result = FromString(text, type, culture);
                return true;
            }

            if (type.IsEnum)
            {
                result = Enum.ToObject(type, value);
                return true;
            }

            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(type))
            {
                result = Convert.ChangeType(value, type, culture);
                return true;
            }

            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(value.GetType()))
            {
                result = converter.ConvertFrom(null, culture, value);
                return true;
            }
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException or NotSupportedException)
        {
        }

        result = null;
        return false;
    }

    public static object? FromString(string text, Type targetType)
    {
        return FromString(text, targetType, CultureInfo.InvariantCulture);
    }

    public static object? FromString(string text, Type targetType, CultureInfo culture)
    {
        Type? underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null && (text.Trim().Length == 0 || text.Trim().Equals("Auto", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        Type type = underlying ?? targetType;
        if (type == typeof(string) || type == typeof(object))
        {
            return text;
        }

        Func<string, object?>? parser;
        lock (ParsersLock)
        {
            parser = FindParser(type);
        }

        if (parser != null)
        {
            return parser(text);
        }

        if (type.IsEnum)
        {
            return Enum.Parse(type, text.Replace('|', ','), true);
        }

        if (type == typeof(char))
        {
            return text.Length == 0 ? '\0' : text[0];
        }

        if (type == typeof(TimeSpan))
        {
            return TimeSpan.Parse(text, culture);
        }

        if (typeof(IConvertible).IsAssignableFrom(type))
        {
            return Convert.ChangeType(text, type, culture);
        }

        TypeConverter converter = TypeDescriptor.GetConverter(type);
        if (converter.CanConvertFrom(typeof(string)))
        {
            return converter.ConvertFromString(null, culture, text);
        }

        throw new FormatException($"Cannot convert '{text}' to {type.Name}.");
    }

    private static Func<string, object?>? FindParser(Type type)
    {
        if (Parsers.TryGetValue(type, out Func<string, object?>? parser))
        {
            return parser;
        }

        foreach (KeyValuePair<Type, Func<string, object?>> entry in Parsers)
        {
            if (type.IsAssignableFrom(entry.Key) && type != typeof(object))
            {
                return entry.Value;
            }
        }

        return null;
    }

    public static Color? ParseColor(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length == 0
            || trimmed.Equals("Transparent", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Default", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            string hex = trimmed.Substring(1);
            if ((hex.Length != 3 && hex.Length != 6) || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            {
                throw new FormatException($"Invalid color '{text}'.");
            }

            return Color.FromHex(hex);
        }

        if (trimmed.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(")", StringComparison.Ordinal))
        {
            string[] parts = trimmed.Substring(4, trimmed.Length - 5).Split(',');
            if (parts.Length != 3)
            {
                throw new FormatException($"Invalid color '{text}'.");
            }

            return new(
                int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                int.Parse(parts[2].Trim(), CultureInfo.InvariantCulture));
        }

        FieldInfo? field = typeof(Color).GetField(trimmed, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        if (field?.GetValue(null) is Color named)
        {
            return named;
        }

        PropertyInfo? themeColor = typeof(Theme).GetProperty(trimmed, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
        if (themeColor?.GetValue(null) is Color theme)
        {
            return theme;
        }

        throw new FormatException($"Unknown color '{text}'.");
    }

    private static FontStyles ParseFontStyles(string text)
    {
        FontStyles styles = FontStyles.None;
        foreach (string part in text.Split(new[] { ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            styles |= part.Trim().ToLowerInvariant() switch
            {
                "bold" or "strong" => FontStyles.Strong,
                "italic" => FontStyles.Italic,
                "underline" or "underlined" => FontStyles.Underlined,
                "strikeout" or "strikethrough" => FontStyles.Strikeout,
                "none" or "normal" => FontStyles.None,
                _ => throw new FormatException($"Unknown font style '{part}'."),
            };
        }

        return styles;
    }

    private static IBorderStyle ParseBorderStyle(string text)
    {
        if (BorderStyles.TryGet(text, out IBorderStyle style))
        {
            return style;
        }

        throw new FormatException($"Unknown border style '{text}'.");
    }
}
