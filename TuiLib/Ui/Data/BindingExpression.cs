using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace TuiLib.Ui.Data;

public sealed class BindingExpression
{
    private readonly TuiObject _target;
    private readonly TuiProperty _property;
    private readonly List<Action> _subscriptions = new();
    private readonly PathSegment[] _segments;

    private object? _leafOwner;
    private PathSegment? _leafSegment;
    private bool _isDetached;
    private bool _isUpdatingSource;
    private bool _isRefreshing;

    public Binding Binding { get; }
    public BindingMode Mode { get; }
    public object? ResolvedSource { get; private set; }

    internal BindingExpression(TuiObject target, TuiProperty property, Binding binding)
    {
        _target = target;
        _property = property;
        Binding = binding;
        Mode = binding.Mode == BindingMode.Default ? property.DefaultBindingMode : binding.Mode;
        _segments = PathSegment.Parse(binding.Path);
    }

    public void Refresh()
    {
        if (_isDetached || _isRefreshing)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Post(Refresh);
            return;
        }

        _isRefreshing = true;
        try
        {
            Unsubscribe();
            ResolvedSource = ResolveSource();

            bool subscribe = Mode != BindingMode.OneTime;
            bool resolved = TryWalk(ResolvedSource, subscribe, out object? value);
            if (Mode == BindingMode.OneWayToSource)
            {
                UpdateSource(_target.GetValue(_property));
                return;
            }

            _target.SetValueFromBinding(_property, resolved ? ConvertToTarget(value) : GetFallback());
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    public void UpdateSource(object? targetValue)
    {
        if (_isDetached || _isUpdatingSource || _leafSegment == null || _leafOwner == null)
        {
            return;
        }

        if (Mode != BindingMode.TwoWay && Mode != BindingMode.OneWayToSource)
        {
            return;
        }

        Type? sourceType = _leafSegment.GetValueType(_leafOwner);
        if (sourceType == null)
        {
            return;
        }

        object? value = targetValue;
        if (Binding.Converter != null)
        {
            value = Binding.Converter.ConvertBack(value, sourceType, Binding.ConverterParameter, CultureInfo.CurrentCulture);
            if (ReferenceEquals(value, Binding.UnsetValue))
            {
                return;
            }
        }

        if (!TypeConversion.TryConvert(value, sourceType, CultureInfo.CurrentCulture, out object? converted))
        {
            return;
        }

        _isUpdatingSource = true;
        try
        {
            _leafSegment.SetValue(_leafOwner, converted);
        }
        catch (Exception ex) when (ex is ArgumentException or TargetInvocationException or InvalidOperationException)
        {
        }
        finally
        {
            _isUpdatingSource = false;
        }
    }

    internal void Detach()
    {
        _isDetached = true;
        Unsubscribe();
    }

    private object? ResolveSource()
    {
        if (Binding.Source != null)
        {
            return Binding.Source;
        }

        if (_target is not Element element)
        {
            return null;
        }

        if (Binding.ElementName != null)
        {
            Element root = element;
            while (root.Parent != null)
            {
                root = root.Parent;
            }

            return root.FindControl<Element>(Binding.ElementName);
        }

        if (_property == Element.DataContextProperty)
        {
            return element.Parent?.DataContext;
        }

        return element.DataContext;
    }

    private bool TryWalk(object? source, bool subscribe, out object? value)
    {
        _leafOwner = null;
        _leafSegment = null;
        value = source;

        for (int i = 0; i < _segments.Length; i++)
        {
            if (value == null)
            {
                return false;
            }

            PathSegment segment = _segments[i];
            if (subscribe)
            {
                Subscribe(value, segment);
            }

            _leafOwner = value;
            _leafSegment = segment;
            if (!segment.TryGetValue(value, out value))
            {
                return false;
            }
        }

        return true;
    }

    private void Subscribe(object owner, PathSegment segment)
    {
        if (segment.Index != null && segment.Name == null)
        {
            if (owner is INotifyCollectionChanged collection)
            {
                NotifyCollectionChangedEventHandler handler = (_, _) => Refresh();
                collection.CollectionChanged += handler;
                _subscriptions.Add(() => collection.CollectionChanged -= handler);
            }

            return;
        }

        if (owner is TuiObject tuiObject)
        {
            EventHandler<TuiPropertyChangedEventArgs> handler = (_, e) =>
            {
                if (e.Property.Name == segment.Name)
                {
                    Refresh();
                }
            };
            tuiObject.PropertyChanged += handler;
            _subscriptions.Add(() => tuiObject.PropertyChanged -= handler);
            return;
        }

        if (owner is INotifyPropertyChanged notify)
        {
            PropertyChangedEventHandler handler = (_, e) =>
            {
                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == segment.Name)
                {
                    Refresh();
                }
            };
            notify.PropertyChanged += handler;
            _subscriptions.Add(() => notify.PropertyChanged -= handler);
        }
    }

    private void Unsubscribe()
    {
        foreach (Action unsubscribe in _subscriptions)
        {
            unsubscribe();
        }

        _subscriptions.Clear();
    }

    private object? ConvertToTarget(object? value)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        if (Binding.Converter != null)
        {
            value = Binding.Converter.Convert(value, _property.PropertyType, Binding.ConverterParameter, culture);
            if (ReferenceEquals(value, Binding.UnsetValue))
            {
                return GetFallback();
            }
        }

        if (value == null && !ReferenceEquals(Binding.TargetNullValue, Binding.UnsetValue))
        {
            value = Binding.TargetNullValue;
        }

        if (Binding.StringFormat != null)
        {
            string format = Binding.StringFormat.Contains("{0") ? Binding.StringFormat : "{0:" + Binding.StringFormat + "}";
            value = string.Format(culture, format, value);
        }

        return TypeConversion.TryConvert(value, _property.PropertyType, culture, out object? converted) ? converted : GetFallback();
    }

    private object? GetFallback()
    {
        if (ReferenceEquals(Binding.FallbackValue, Binding.UnsetValue))
        {
            return _property.DefaultValue;
        }

        return TypeConversion.TryConvert(Binding.FallbackValue, _property.PropertyType, out object? converted) ? converted : _property.DefaultValue;
    }

    private sealed class PathSegment
    {
        public string? Name { get; }
        public string? Index { get; }

        private PathSegment(string? name, string? index)
        {
            Name = name;
            Index = index;
        }

        public static PathSegment[] Parse(string path)
        {
            string trimmed = path.Trim();
            if (trimmed.Length == 0 || trimmed == ".")
            {
                return [];
            }

            List<PathSegment> segments = new();
            foreach (string part in trimmed.Split('.'))
            {
                string name = part.Trim();
                int bracket = name.IndexOf('[');
                if (bracket < 0)
                {
                    segments.Add(new(name, null));
                    continue;
                }

                if (bracket > 0)
                {
                    segments.Add(new(name.Substring(0, bracket), null));
                }

                string rest = name.Substring(bracket);
                while (rest.StartsWith("[", StringComparison.Ordinal))
                {
                    int close = rest.IndexOf(']');
                    if (close < 0)
                    {
                        throw new FormatException($"Invalid binding path '{path}'.");
                    }

                    segments.Add(new(null, rest.Substring(1, close - 1).Trim()));
                    rest = rest.Substring(close + 1);
                }
            }

            return segments.ToArray();
        }

        public bool TryGetValue(object owner, out object? value)
        {
            value = null;
            if (Name == null)
            {
                return TryGetIndexed(owner, out value);
            }

            if (owner is TuiObject tuiObject)
            {
                TuiProperty? property = TuiProperty.Find(owner.GetType(), Name);
                if (property != null)
                {
                    value = tuiObject.GetValue(property);
                    return true;
                }
            }

            PropertyInfo? info = owner.GetType().GetProperty(Name, BindingFlags.Public | BindingFlags.Instance);
            if (info == null || info.GetIndexParameters().Length > 0)
            {
                return false;
            }

            value = info.GetValue(owner);
            return true;
        }

        public Type? GetValueType(object owner)
        {
            if (Name == null)
            {
                return owner is IList ? GetListItemType(owner.GetType()) : typeof(object);
            }

            if (owner is TuiObject)
            {
                TuiProperty? property = TuiProperty.Find(owner.GetType(), Name);
                if (property != null)
                {
                    return property.PropertyType;
                }
            }

            PropertyInfo? info = owner.GetType().GetProperty(Name, BindingFlags.Public | BindingFlags.Instance);
            return info?.CanWrite == true ? info.PropertyType : null;
        }

        public void SetValue(object owner, object? value)
        {
            if (Name == null)
            {
                SetIndexed(owner, value);
                return;
            }

            if (owner is TuiObject tuiObject)
            {
                TuiProperty? property = TuiProperty.Find(owner.GetType(), Name);
                if (property != null)
                {
                    tuiObject.SetValue(property, value);
                    return;
                }
            }

            PropertyInfo? info = owner.GetType().GetProperty(Name, BindingFlags.Public | BindingFlags.Instance);
            if (info?.CanWrite == true)
            {
                info.SetValue(owner, value);
            }
        }

        private bool TryGetIndexed(object owner, out object? value)
        {
            value = null;
            if (owner is IList list && int.TryParse(Index, NumberStyles.Integer, CultureInfo.InvariantCulture, out int position))
            {
                if (position < 0 || position >= list.Count)
                {
                    return false;
                }

                value = list[position];
                return true;
            }

            if (owner is IDictionary dictionary && Index != null)
            {
                if (!dictionary.Contains(Index))
                {
                    return false;
                }

                value = dictionary[Index];
                return true;
            }

            return false;
        }

        private void SetIndexed(object owner, object? value)
        {
            if (owner is IList list && int.TryParse(Index, NumberStyles.Integer, CultureInfo.InvariantCulture, out int position) && position >= 0 && position < list.Count)
            {
                list[position] = value;
                return;
            }

            if (owner is IDictionary dictionary && Index != null)
            {
                dictionary[Index] = value;
            }
        }

        private static Type GetListItemType(Type listType)
        {
            foreach (Type contract in listType.GetInterfaces())
            {
                if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    return contract.GetGenericArguments()[0];
                }
            }

            return typeof(object);
        }
    }
}
