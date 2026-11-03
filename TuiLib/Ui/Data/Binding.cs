namespace TuiLib.Ui.Data;

public sealed class Binding
{
    public static readonly object UnsetValue = new();

    public string Path { get; set; } = string.Empty;
    public BindingMode Mode { get; set; } = BindingMode.Default;
    public object? Source { get; set; }
    public string? ElementName { get; set; }
    public string? StringFormat { get; set; }
    public IValueConverter? Converter { get; set; }
    public object? ConverterParameter { get; set; }
    public object? FallbackValue { get; set; } = UnsetValue;
    public object? TargetNullValue { get; set; } = UnsetValue;

    public Binding()
    {
    }

    public Binding(string path, BindingMode mode = BindingMode.Default)
    {
        Path = path;
        Mode = mode;
    }

    public override string ToString() => $"{{Binding {Path}}}";
}
