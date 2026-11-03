using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace TuiLib.Ui.Markup;

public static class TuiXamlLoader
{
    public const string DefaultNamespace = "https://github.com/tuilib";
    public const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    public const string FileExtension = ".txaml";

    public static ResourceDictionary GlobalResources { get; } = new();

    public static object Load(string xml, object? codeBehind = null)
    {
        XDocument document = Parse(xml);
        return new XamlReader(codeBehind).CreateRoot(document.Root!);
    }

    public static T Load<T>(string xml, object? codeBehind = null)
    {
        object result = Load(xml, codeBehind);
        if (result is not T typed)
        {
            throw new TuiXamlException($"Markup root is {result.GetType().Name}, expected {typeof(T).Name}.");
        }

        return typed;
    }

    public static object LoadFile(string path, object? codeBehind = null)
    {
        return Load(File.ReadAllText(path), codeBehind);
    }

    public static void LoadInto(object root, string xml)
    {
        XDocument document = Parse(xml);
        new XamlReader(root).PopulateRoot(root, document.Root!);
    }

    public static void Load(object component)
    {
        LoadInto(component, FindMarkup(component.GetType()));
    }

    private static string FindMarkup(Type type)
    {
        Assembly assembly = type.Assembly;
        string[] resources = assembly.GetManifestResourceNames();
        string fileName = type.Name + FileExtension;
        string? resource = resources.FirstOrDefault(name => name == type.FullName + FileExtension)
            ?? resources.FirstOrDefault(name => name == fileName || name.EndsWith("." + fileName, StringComparison.Ordinal));
        if (resource != null)
        {
            using Stream stream = assembly.GetManifestResourceStream(resource)!;
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        string baseDirectory = AppContext.BaseDirectory;
        string? file = Directory.EnumerateFiles(baseDirectory, fileName, SearchOption.AllDirectories).FirstOrDefault();
        if (file != null)
        {
            return File.ReadAllText(file);
        }

        throw new TuiXamlException($"No markup found for {type.FullName}. Add '{fileName}' as an EmbeddedResource or copy it to the output directory.");
    }

    private static XDocument Parse(string xml)
    {
        try
        {
            return XDocument.Parse(xml, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw new TuiXamlException(ex.Message, ex.LineNumber, ex.LinePosition, ex);
        }
    }
}
