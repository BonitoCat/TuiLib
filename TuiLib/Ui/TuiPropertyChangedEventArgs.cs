namespace TuiLib.Ui;

public sealed class TuiPropertyChangedEventArgs
{
    public TuiProperty Property { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public TuiPropertyChangedEventArgs(TuiProperty property, object? oldValue, object? newValue)
    {
        Property = property;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
