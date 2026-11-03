using System;
using System.Globalization;

namespace TuiLib.Ui.Data;

public sealed class FuncValueConverter<TIn, TOut> : IValueConverter
{
    private readonly Func<TIn, TOut> _convert;
    private readonly Func<TOut, TIn>? _convertBack;

    public FuncValueConverter(Func<TIn, TOut> convert, Func<TOut, TIn>? convertBack = null)
    {
        _convert = convert;
        _convertBack = convertBack;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TIn input)
        {
            return _convert(input);
        }

        return value == null && default(TIn) == null ? _convert(default!) : Binding.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (_convertBack == null || value is not TOut output)
        {
            return Binding.UnsetValue;
        }

        return _convertBack(output);
    }
}
