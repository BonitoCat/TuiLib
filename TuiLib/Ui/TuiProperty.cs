using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TuiLib.Ui.Data;

namespace TuiLib.Ui;

public abstract class TuiProperty
{
    private static readonly Dictionary<Type, Dictionary<string, TuiProperty>> Registry = new();
    private static readonly object RegistryLock = new();

    public string Name { get; }
    public Type OwnerType { get; }
    public Type PropertyType { get; }
    public object? DefaultValue { get; }
    public PropertyFlags Flags { get; }
    public bool IsAttached { get; }
    public BindingMode DefaultBindingMode { get; }

    public bool Inherits => (Flags & PropertyFlags.Inherits) != 0;
    public bool AffectsMeasure => (Flags & PropertyFlags.AffectsMeasure) != 0;
    public bool AffectsRender => (Flags & PropertyFlags.AffectsRender) != 0;

    private protected TuiProperty(string name, Type ownerType, Type propertyType, object? defaultValue, PropertyFlags flags, bool isAttached, BindingMode defaultBindingMode)
    {
        Name = name;
        OwnerType = ownerType;
        PropertyType = propertyType;
        DefaultValue = defaultValue;
        Flags = flags;
        IsAttached = isAttached;
        DefaultBindingMode = defaultBindingMode == BindingMode.Default ? BindingMode.OneWay : defaultBindingMode;

        lock (RegistryLock)
        {
            if (!Registry.TryGetValue(ownerType, out Dictionary<string, TuiProperty>? properties))
            {
                properties = new(StringComparer.Ordinal);
                Registry[ownerType] = properties;
            }

            properties[name] = this;
        }
    }

    public static TuiProperty<T> Register<TOwner, T>(string name, T defaultValue, PropertyFlags flags = PropertyFlags.None, BindingMode defaultBindingMode = BindingMode.OneWay)
    {
        return new(name, typeof(TOwner), defaultValue, flags, false, defaultBindingMode);
    }

    public static TuiProperty<T> RegisterAttached<TOwner, T>(string name, T defaultValue, PropertyFlags flags = PropertyFlags.AffectsMeasure)
    {
        return new(name, typeof(TOwner), defaultValue, flags, true, BindingMode.OneWay);
    }

    public static TuiProperty? Find(Type ownerType, string name)
    {
        for (Type? type = ownerType; type != null && type != typeof(object); type = type.BaseType)
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            lock (RegistryLock)
            {
                if (Registry.TryGetValue(type, out Dictionary<string, TuiProperty>? properties) && properties.TryGetValue(name, out TuiProperty? property))
                {
                    return property;
                }
            }
        }

        return null;
    }

    public override string ToString() => $"{OwnerType.Name}.{Name}";
}

public sealed class TuiProperty<T> : TuiProperty
{
    internal TuiProperty(string name, Type ownerType, T defaultValue, PropertyFlags flags, bool isAttached, BindingMode defaultBindingMode)
        : base(name, ownerType, typeof(T), defaultValue, flags, isAttached, defaultBindingMode)
    {
    }
}
