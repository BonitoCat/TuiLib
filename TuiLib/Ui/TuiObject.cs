using System;
using System.Collections.Generic;
using System.Linq;
using TuiLib.Ui.Data;

namespace TuiLib.Ui;

public abstract class TuiObject
{
    private readonly Dictionary<TuiProperty, object?> _values = new();
    private readonly Dictionary<TuiProperty, BindingExpression> _bindings = new();

    public event EventHandler<TuiPropertyChangedEventArgs>? PropertyChanged;

    protected virtual TuiObject? InheritanceParent => null;

    public T GetValue<T>(TuiProperty<T> property)
    {
        return (T) GetValue((TuiProperty) property)!;
    }

    public object? GetValue(TuiProperty property)
    {
        if (_values.TryGetValue(property, out object? value))
        {
            return value;
        }

        if (property.Inherits && InheritanceParent != null)
        {
            return InheritanceParent.GetValue(property);
        }

        return property.DefaultValue;
    }

    public bool IsSet(TuiProperty property) => _values.ContainsKey(property);

    public void SetValue<T>(TuiProperty<T> property, T value)
    {
        SetValue((TuiProperty) property, value);
    }

    public void SetValue(TuiProperty property, object? value)
    {
        if (!SetValueCore(property, value))
        {
            return;
        }

        if (_bindings.TryGetValue(property, out BindingExpression? expression))
        {
            expression.UpdateSource(value);
        }
    }

    public void ClearValue(TuiProperty property)
    {
        if (!_values.ContainsKey(property))
        {
            return;
        }

        object? oldValue = GetValue(property);
        _values.Remove(property);
        object? newValue = GetValue(property);
        if (!Equals(oldValue, newValue))
        {
            OnPropertyChanged(new(property, oldValue, newValue));
        }
    }

    public BindingExpression Bind(TuiProperty property, Binding binding)
    {
        Unbind(property);

        BindingExpression expression = new(this, property, binding);
        _bindings[property] = expression;
        expression.Refresh();
        return expression;
    }

    public void Unbind(TuiProperty property)
    {
        if (!_bindings.TryGetValue(property, out BindingExpression? expression))
        {
            return;
        }

        expression.Detach();
        _bindings.Remove(property);
    }

    public BindingExpression? GetBindingExpression(TuiProperty property)
    {
        return _bindings.TryGetValue(property, out BindingExpression? expression) ? expression : null;
    }

    internal void SetValueFromBinding(TuiProperty property, object? value)
    {
        SetValueCore(property, value);
    }

    internal void RefreshBindings()
    {
        foreach (BindingExpression expression in _bindings.Values.ToList())
        {
            expression.Refresh();
        }
    }

    protected virtual void OnPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    private bool SetValueCore(TuiProperty property, object? value)
    {
        object? oldValue = GetValue(property);
        _values[property] = value;
        if (Equals(oldValue, value))
        {
            return false;
        }

        OnPropertyChanged(new(property, oldValue, value));
        return true;
    }
}
