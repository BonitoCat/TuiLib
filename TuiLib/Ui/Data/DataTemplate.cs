using System;

namespace TuiLib.Ui.Data;

public sealed class DataTemplate
{
    private readonly Func<object?, Element> _factory;

    public Type? DataType { get; set; }

    public DataTemplate(Func<Element> factory)
    {
        _factory = _ => factory();
    }

    public DataTemplate(Func<object?, Element> factory)
    {
        _factory = factory;
    }

    public bool Match(object? data)
    {
        return DataType == null || DataType.IsInstanceOfType(data);
    }

    public Element Build(object? data)
    {
        Element element = _factory(data);
        element.DataContext = data;
        return element;
    }
}
