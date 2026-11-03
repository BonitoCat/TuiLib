using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using TuiLib.Ui.Controls;
using TuiLib.Ui.Data;

namespace TuiLib.Ui.Markup;

internal sealed class XamlReader
{
    private const string AvaloniaNamespace = "https://github.com/avaloniaui";
    private const BindingFlags DeclaredMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly string[] DefaultNamespaces = { "TuiLib.Ui.Controls", "TuiLib.Ui", "TuiLib.Ui.Data" };
    private static readonly Dictionary<string, Type> TypeCache = new();
    private static readonly object TypeCacheLock = new();

    private readonly object? _codeBehind;
    private readonly Stack<object> _instances;
    private readonly bool _isTemplate;

    public XamlReader(object? codeBehind) : this(codeBehind, Array.Empty<object>(), false)
    {
    }

    private XamlReader(object? codeBehind, IEnumerable<object> ancestors, bool isTemplate)
    {
        _codeBehind = codeBehind;
        _instances = new(ancestors);
        _isTemplate = isTemplate;
    }

    public object CreateRoot(XElement element)
    {
        return CreateObject(element);
    }

    public void PopulateRoot(object root, XElement element)
    {
        Type type = ResolveType(element.Name, element);
        if (!type.IsInstanceOfType(root))
        {
            throw Error($"Markup root <{element.Name.LocalName}> does not match {root.GetType().Name}.", element);
        }

        Populate(root, element);
    }

    private object CreateObject(XElement element)
    {
        Type type = ResolveType(element.Name, element);
        if (type == typeof(DataTemplate))
        {
            return CreateDataTemplate(element);
        }

        object instance;
        try
        {
            instance = Activator.CreateInstance(type)!;
        }
        catch (Exception ex)
        {
            throw Error($"Cannot create {type.Name}: {Unwrap(ex).Message}", element, ex);
        }

        Populate(instance, element);
        return instance;
    }

    private void Populate(object instance, XElement element)
    {
        _instances.Push(instance);
        try
        {
            List<object?> content = new();
            foreach (XNode node in element.Nodes())
            {
                if (node is XElement child)
                {
                    if (IsPropertyElement(child))
                    {
                        ApplyPropertyElement(instance, child);
                        continue;
                    }

                    content.Add(CreateObject(child));
                    continue;
                }

                if (node is XText text)
                {
                    string normalized = NormalizeText(text.Value, element);
                    if (normalized.Length > 0)
                    {
                        content.Add(normalized);
                    }
                }
            }

            if (content.Count > 0)
            {
                ApplyContent(instance, content, element);
            }

            foreach (XAttribute attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration)
                {
                    continue;
                }

                ApplyAttribute(instance, attribute, element);
            }
        }
        finally
        {
            _instances.Pop();
        }
    }

    private void ApplyAttribute(object instance, XAttribute attribute, XElement element)
    {
        Type type = instance.GetType();
        string name = attribute.Name.LocalName;
        try
        {
            if (attribute.Name.NamespaceName == TuiXamlLoader.XamlNamespace)
            {
                if (name == "Name")
                {
                    SetName(instance, attribute.Value);
                }

                return;
            }

            if (attribute.Name.NamespaceName == XNamespace.Xml.NamespaceName)
            {
                return;
            }

            if (name == "Name" && instance is Element)
            {
                SetName(instance, attribute.Value);
                return;
            }

            int dot = name.IndexOf('.');
            if (dot > 0)
            {
                Type ownerType = ResolveType(XName.Get(name.Substring(0, dot), element.GetDefaultNamespace().NamespaceName), attribute);
                string propertyName = name.Substring(dot + 1);
                TuiProperty? attached = TuiProperty.Find(ownerType, propertyName);
                if (attached == null || instance is not TuiObject attachedTarget)
                {
                    throw Error($"Unknown attached property '{name}'.", attribute);
                }

                ApplyPropertyValue(attachedTarget, attached, attribute.Value, element);
                return;
            }

            EventInfo? eventInfo = type.GetEvent(name, BindingFlags.Instance | BindingFlags.Public);
            if (eventInfo != null)
            {
                WireEvent(instance, eventInfo, attribute.Value, attribute);
                return;
            }

            TuiProperty? property = instance is TuiObject ? TuiProperty.Find(type, name) : null;
            if (property != null)
            {
                ApplyPropertyValue((TuiObject) instance, property, attribute.Value, element);
                return;
            }

            PropertyInfo? clrProperty = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (clrProperty == null || !clrProperty.CanWrite)
            {
                throw Error($"{type.Name} has no settable property or event named '{name}'.", attribute);
            }

            object? value = EvaluateValue(attribute.Value, element);
            if (value is Binding)
            {
                throw Error($"'{name}' on {type.Name} is not a bindable property.", attribute);
            }

            clrProperty.SetValue(instance, Coerce(value, clrProperty.PropertyType));
        }
        catch (TuiXamlException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Error($"Cannot set '{name}' on {type.Name}: {Unwrap(ex).Message}", attribute, ex);
        }
    }

    private void ApplyPropertyValue(TuiObject target, TuiProperty property, string raw, XElement element)
    {
        object? value = EvaluateValue(raw, element);
        if (value is Binding binding)
        {
            target.Bind(property, binding);
            return;
        }

        target.SetValue(property, Coerce(value, property.PropertyType));
    }

    private void ApplyPropertyElement(object instance, XElement propertyElement)
    {
        string local = propertyElement.Name.LocalName;
        int dot = local.IndexOf('.');
        Type ownerType = ResolveType(XName.Get(local.Substring(0, dot), propertyElement.Name.NamespaceName), propertyElement);
        string propertyName = local.Substring(dot + 1);
        Type instanceType = instance.GetType();

        PropertyInfo? clrProperty = ownerType.IsAssignableFrom(instanceType) ? instanceType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public) : null;
        TuiProperty? property = instance is TuiObject ? TuiProperty.Find(ownerType.IsAssignableFrom(instanceType) ? instanceType : ownerType, propertyName) : null;
        if (clrProperty == null && property == null)
        {
            throw Error($"{instanceType.Name} has no property named '{propertyName}'.", propertyElement);
        }

        try
        {
            object? existing = clrProperty?.CanRead == true ? clrProperty.GetValue(instance) : null;
            if (existing is IDictionary dictionary)
            {
                foreach (XElement child in propertyElement.Elements())
                {
                    string? key = child.Attribute(XName.Get("Key", TuiXamlLoader.XamlNamespace))?.Value;
                    if (key == null)
                    {
                        throw Error("Resources need an x:Key.", child);
                    }

                    dictionary[key] = CreateObject(child);
                }

                return;
            }

            List<object?> values = new();
            foreach (XNode node in propertyElement.Nodes())
            {
                if (node is XElement child)
                {
                    values.Add(CreateObject(child));
                    continue;
                }

                if (node is XText text)
                {
                    string normalized = NormalizeText(text.Value, propertyElement);
                    if (normalized.Length > 0)
                    {
                        values.Add(normalized);
                    }
                }
            }

            Type propertyType = property?.PropertyType ?? clrProperty!.PropertyType;
            if (existing is IList list && !(values.Count == 1 && propertyType.IsInstanceOfType(values[0])))
            {
                foreach (object? value in values)
                {
                    list.Add(value);
                }

                return;
            }

            if (values.Count != 1)
            {
                throw Error($"Property '{propertyName}' takes exactly one value.", propertyElement);
            }

            SetMember(instance, property, clrProperty, values[0]);
        }
        catch (TuiXamlException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Error($"Cannot set '{propertyName}' on {instanceType.Name}: {Unwrap(ex).Message}", propertyElement, ex);
        }
    }

    private void ApplyContent(object instance, List<object?> items, XElement element)
    {
        Type type = instance.GetType();
        PropertyInfo? contentProperty = FindContentProperty(type);
        if (contentProperty == null)
        {
            throw Error($"{type.Name} does not accept content.", element);
        }

        try
        {
            object? existing = contentProperty.GetValue(instance);
            if (existing is IList list && !(items.Count == 1 && contentProperty.PropertyType.IsInstanceOfType(items[0])))
            {
                foreach (object? item in items)
                {
                    list.Add(existing is ElementCollection && item is string text ? new TextBlock { Text = text } : item);
                }

                return;
            }

            object? value;
            if (items.Count == 1)
            {
                value = items[0];
            }
            else if (items.All(item => item is string))
            {
                value = string.Join(" ", items.Cast<string>());
            }
            else
            {
                throw Error($"{type.Name} accepts only a single child.", element);
            }

            TuiProperty? property = instance is TuiObject ? TuiProperty.Find(type, contentProperty.Name) : null;
            SetMember(instance, property, contentProperty, value);
        }
        catch (TuiXamlException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Error($"Cannot set content of {type.Name}: {Unwrap(ex).Message}", element, ex);
        }
    }

    private static void SetMember(object instance, TuiProperty? property, PropertyInfo? clrProperty, object? value)
    {
        if (property != null && instance is TuiObject target)
        {
            target.SetValue(property, Coerce(value, property.PropertyType));
            return;
        }

        if (clrProperty == null || !clrProperty.CanWrite)
        {
            throw new InvalidOperationException("Property is read-only.");
        }

        clrProperty.SetValue(instance, Coerce(value, clrProperty.PropertyType));
    }

    private static object? Coerce(object? value, Type targetType)
    {
        if (value is string text && targetType != typeof(string) && targetType != typeof(object))
        {
            return TypeConversion.FromString(text, targetType);
        }

        if (TypeConversion.TryConvert(value, targetType, out object? converted))
        {
            return converted;
        }

        throw new InvalidCastException($"Cannot convert {value?.GetType().Name ?? "null"} to {targetType.Name}.");
    }

    private object? EvaluateValue(string raw, XElement element)
    {
        if (MarkupExtensionParser.IsExtension(raw))
        {
            return EvaluateExtension(raw, element);
        }

        return MarkupExtensionParser.Unescape(raw);
    }

    private object? EvaluateExtension(string raw, XElement element)
    {
        (string name, List<string> positional, List<KeyValuePair<string, string>> named) = MarkupExtensionParser.Parse(raw);
        string local = name.Contains(":") ? name.Substring(name.IndexOf(':') + 1) : name;
        switch (local)
        {
            case "Binding":
                return CreateBinding(positional, named, element);
            case "Null":
                return null;
            case "Static":
                return ResolveStatic(RequireArgument(positional, named, "Member", raw, element), element);
            case "Type":
                return ResolveTypeName(RequireArgument(positional, named, "TypeName", raw, element), element);
            case "StaticResource":
                return FindResource(RequireArgument(positional, named, "ResourceKey", raw, element), element);
            default:
                throw Error($"Unknown markup extension '{name}'.", element);
        }
    }

    private string RequireArgument(List<string> positional, List<KeyValuePair<string, string>> named, string key, string raw, XElement element)
    {
        if (positional.Count > 0)
        {
            return positional[0];
        }

        foreach (KeyValuePair<string, string> pair in named)
        {
            if (pair.Key == key)
            {
                return pair.Value;
            }
        }

        throw Error($"Missing argument in '{raw}'.", element);
    }

    private Binding CreateBinding(List<string> positional, List<KeyValuePair<string, string>> named, XElement element)
    {
        Binding binding = new();
        if (positional.Count > 0)
        {
            SetBindingPath(binding, positional[0]);
        }

        foreach (KeyValuePair<string, string> pair in named)
        {
            switch (pair.Key)
            {
                case "Path":
                    SetBindingPath(binding, pair.Value);
                    break;
                case "Mode":
                    binding.Mode = (BindingMode) Enum.Parse(typeof(BindingMode), pair.Value, true);
                    break;
                case "ElementName":
                    binding.ElementName = pair.Value;
                    break;
                case "StringFormat":
                    binding.StringFormat = MarkupExtensionParser.Unescape(pair.Value);
                    break;
                case "Converter":
                    binding.Converter = EvaluateValue(pair.Value, element) as IValueConverter ?? throw Error($"'{pair.Value}' is not an IValueConverter.", element);
                    break;
                case "ConverterParameter":
                    binding.ConverterParameter = EvaluateValue(pair.Value, element);
                    break;
                case "FallbackValue":
                    binding.FallbackValue = EvaluateValue(pair.Value, element);
                    break;
                case "TargetNullValue":
                    binding.TargetNullValue = EvaluateValue(pair.Value, element);
                    break;
                case "Source":
                    binding.Source = EvaluateValue(pair.Value, element);
                    break;
                default:
                    throw Error($"Unknown binding option '{pair.Key}'.", element);
            }
        }

        return binding;
    }

    private static void SetBindingPath(Binding binding, string path)
    {
        string trimmed = path.Trim();
        if (!trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            binding.Path = trimmed;
            return;
        }

        int dot = trimmed.IndexOf('.');
        binding.ElementName = dot < 0 ? trimmed.Substring(1) : trimmed.Substring(1, dot - 1);
        binding.Path = dot < 0 ? string.Empty : trimmed.Substring(dot + 1);
    }

    private object? FindResource(string key, XElement element)
    {
        foreach (object instance in _instances)
        {
            if (instance is Element owner && owner.Resources.TryGetValue(key, out object? value))
            {
                return value;
            }
        }

        if (TuiXamlLoader.GlobalResources.TryGetValue(key, out object? global))
        {
            return global;
        }

        throw Error($"Resource '{key}' not found.", element);
    }

    private object? ResolveStatic(string member, XElement element)
    {
        int dot = member.LastIndexOf('.');
        if (dot <= 0)
        {
            throw Error($"Invalid static member '{member}'.", element);
        }

        Type type = ResolveTypeName(member.Substring(0, dot), element);
        string memberName = member.Substring(dot + 1);
        BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        FieldInfo? field = type.GetField(memberName, flags);
        if (field != null)
        {
            return field.GetValue(null);
        }

        PropertyInfo? property = type.GetProperty(memberName, flags);
        if (property != null)
        {
            return property.GetValue(null);
        }

        throw Error($"{type.Name} has no static member '{memberName}'.", element);
    }

    private Type ResolveTypeName(string qualifiedName, XElement element)
    {
        int colon = qualifiedName.IndexOf(':');
        XNamespace? xmlNamespace = colon < 0 ? element.GetDefaultNamespace() : element.GetNamespaceOfPrefix(qualifiedName.Substring(0, colon));
        if (xmlNamespace == null)
        {
            throw Error($"Unknown namespace prefix in '{qualifiedName}'.", element);
        }

        return ResolveType(xmlNamespace + qualifiedName.Substring(colon + 1), element);
    }

    private DataTemplate CreateDataTemplate(XElement element)
    {
        XElement[] children = element.Elements().ToArray();
        if (children.Length != 1)
        {
            throw Error("A DataTemplate must contain exactly one element.", element);
        }

        XElement body = children[0];
        object[] ancestors = _instances.Reverse().ToArray();
        object? codeBehind = _codeBehind;
        DataTemplate template = new(_ =>
        {
            XamlReader reader = new(codeBehind, ancestors, true);
            return reader.CreateObject(body) as Element ?? throw Error("A DataTemplate must produce an Element.", body);
        });

        XAttribute? dataType = element.Attribute(XName.Get("DataType", TuiXamlLoader.XamlNamespace)) ?? element.Attribute("DataType");
        if (dataType != null)
        {
            template.DataType = MarkupExtensionParser.IsExtension(dataType.Value)
                ? EvaluateExtension(dataType.Value, element) as Type
                : ResolveTypeName(dataType.Value, element);
        }

        return template;
    }

    private void SetName(object instance, string name)
    {
        if (instance is Element element)
        {
            element.Name = name;
        }

        if (_isTemplate || _codeBehind == null || ReferenceEquals(instance, _codeBehind))
        {
            return;
        }

        string prefixed = "_" + char.ToLowerInvariant(name[0]) + name.Substring(1);
        for (Type? type = _codeBehind.GetType(); type != null && type != typeof(object); type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, DeclaredMembers) ?? type.GetField(prefixed, DeclaredMembers);
            if (field != null && field.FieldType.IsInstanceOfType(instance))
            {
                field.SetValue(_codeBehind, instance);
                return;
            }

            PropertyInfo? property = type.GetProperty(name, DeclaredMembers);
            if (property != null && property.CanWrite && property.PropertyType.IsInstanceOfType(instance))
            {
                property.SetValue(_codeBehind, instance);
                return;
            }
        }
    }

    private void WireEvent(object instance, EventInfo eventInfo, string methodName, XAttribute source)
    {
        if (_codeBehind == null)
        {
            throw Error($"Event '{eventInfo.Name}' needs a code-behind object.", source);
        }

        Type handlerType = eventInfo.EventHandlerType!;
        ParameterInfo[] eventParameters = handlerType.GetMethod("Invoke")!.GetParameters();
        MethodInfo? method = FindMethod(_codeBehind.GetType(), methodName.Trim(), eventParameters.Length);
        if (method == null)
        {
            throw Error($"{_codeBehind.GetType().Name} has no method '{methodName}' for event '{eventInfo.Name}'.", source);
        }

        Delegate handler;
        if (method.GetParameters().Length == eventParameters.Length)
        {
            handler = Delegate.CreateDelegate(handlerType, _codeBehind, method);
        }
        else
        {
            ParameterExpression[] parameters = eventParameters.Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name)).ToArray();
            handler = Expression.Lambda(handlerType, Expression.Call(Expression.Constant(_codeBehind), method), parameters).Compile();
        }

        eventInfo.AddEventHandler(instance, handler);
    }

    private static MethodInfo? FindMethod(Type type, string name, int parameterCount)
    {
        for (Type? current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            MethodInfo[] candidates = current.GetMethods(DeclaredMembers).Where(method => method.Name == name).ToArray();
            MethodInfo? match = candidates.FirstOrDefault(method => method.GetParameters().Length == parameterCount)
                ?? candidates.FirstOrDefault(method => method.GetParameters().Length == 0);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static PropertyInfo? FindContentProperty(Type type)
    {
        PropertyInfo? best = null;
        int bestDepth = -1;
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.IsDefined(typeof(ContentAttribute), true))
            {
                continue;
            }

            int depth = 0;
            for (Type? current = property.DeclaringType; current != null; current = current.BaseType)
            {
                depth++;
            }

            if (depth > bestDepth)
            {
                best = property;
                bestDepth = depth;
            }
        }

        return best;
    }

    private static bool IsPropertyElement(XElement element)
    {
        return element.Name.LocalName.IndexOf('.') > 0;
    }

    private static string NormalizeText(string text, XElement element)
    {
        for (XElement? current = element; current != null; current = current.Parent)
        {
            XAttribute? space = current.Attribute(XNamespace.Xml + "space");
            if (space != null)
            {
                return space.Value == "preserve" ? text : Collapse(text);
            }
        }

        return Collapse(text);
    }

    private static string Collapse(string text)
    {
        return string.Join(" ", text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private Type ResolveType(XName name, XObject source)
    {
        string uri = name.NamespaceName;
        string local = name.LocalName;
        string key = uri + "|" + local;
        lock (TypeCacheLock)
        {
            if (TypeCache.TryGetValue(key, out Type? cached))
            {
                return cached;
            }
        }

        Type? type = null;
        foreach ((string clrNamespace, string? assemblyName) in GetClrNamespaces(uri, source))
        {
            type = FindType(clrNamespace + "." + local, assemblyName);
            if (type != null)
            {
                break;
            }
        }

        if (type == null)
        {
            throw Error($"Unknown type '{local}'" + (uri.Length == 0 ? "." : $" in '{uri}'."), source);
        }

        lock (TypeCacheLock)
        {
            TypeCache[key] = type;
        }

        return type;
    }

    private IEnumerable<(string Namespace, string? Assembly)> GetClrNamespaces(string uri, XObject source)
    {
        if (uri.Length == 0 || uri == TuiXamlLoader.DefaultNamespace || uri == AvaloniaNamespace)
        {
            return DefaultNamespaces.Select(clrNamespace => (clrNamespace, (string?) typeof(Element).Assembly.GetName().Name));
        }

        if (uri.StartsWith("using:", StringComparison.Ordinal))
        {
            return new[] { (uri.Substring(6).Trim(), (string?) null) };
        }

        if (uri.StartsWith("clr-namespace:", StringComparison.Ordinal))
        {
            string[] parts = uri.Substring(14).Split(';');
            string? assembly = parts.Skip(1)
                .Select(part => part.Trim())
                .Where(part => part.StartsWith("assembly=", StringComparison.Ordinal))
                .Select(part => part.Substring(9))
                .FirstOrDefault();
            return new (string, string?)[] { (parts[0].Trim(), assembly) };
        }

        throw Error($"Unknown XML namespace '{uri}'.", source);
    }

    private Type? FindType(string fullName, string? assemblyName)
    {
        List<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        if (_codeBehind != null && !assemblies.Contains(_codeBehind.GetType().Assembly))
        {
            assemblies.Add(_codeBehind.GetType().Assembly);
        }

        foreach (Assembly assembly in assemblies)
        {
            if (assemblyName != null && assembly.GetName().Name != assemblyName)
            {
                continue;
            }

            Type? type = assembly.GetType(fullName, false);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private static Exception Unwrap(Exception exception)
    {
        Exception current = exception;
        while (current is TargetInvocationException { InnerException: not null } invocation)
        {
            current = invocation.InnerException;
        }

        return current;
    }

    private static TuiXamlException Error(string message, XObject source, Exception? inner = null)
    {
        IXmlLineInfo info = source;
        return info.HasLineInfo() ? new(message, info.LineNumber, info.LinePosition, inner) : new(message, 0, 0, inner);
    }
}
