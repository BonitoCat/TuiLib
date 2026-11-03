using System;
using System.Collections.Generic;
using System.Text;

namespace TuiLib.Ui.Markup;

internal static class MarkupExtensionParser
{
    public static bool IsExtension(string value)
    {
        string trimmed = value.Trim();
        return trimmed.StartsWith("{", StringComparison.Ordinal) && !trimmed.StartsWith("{}", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal);
    }

    public static string Unescape(string value)
    {
        string trimmed = value.TrimStart();
        return trimmed.StartsWith("{}", StringComparison.Ordinal) ? trimmed.Substring(2) : value;
    }

    public static (string Name, List<string> Positional, List<KeyValuePair<string, string>> Named) Parse(string value)
    {
        string inner = value.Trim();
        inner = inner.Substring(1, inner.Length - 2).Trim();

        int nameEnd = 0;
        while (nameEnd < inner.Length && !char.IsWhiteSpace(inner[nameEnd]) && inner[nameEnd] != ',')
        {
            nameEnd++;
        }

        string name = inner.Substring(0, nameEnd);
        string rest = inner.Substring(nameEnd).Trim();
        if (rest.StartsWith(",", StringComparison.Ordinal))
        {
            rest = rest.Substring(1);
        }

        List<string> positional = new();
        List<KeyValuePair<string, string>> named = new();
        foreach (string argument in SplitTopLevel(rest, ','))
        {
            string trimmed = argument.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            int equals = IndexOfTopLevel(trimmed, '=');
            if (equals < 0)
            {
                positional.Add(Unquote(trimmed));
                continue;
            }

            named.Add(new(trimmed.Substring(0, equals).Trim(), Unquote(trimmed.Substring(equals + 1).Trim())));
        }

        return (name, positional, named);
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 && ((value[0] == '\'' && value[value.Length - 1] == '\'') || (value[0] == '"' && value[value.Length - 1] == '"')))
        {
            return value.Substring(1, value.Length - 2);
        }

        return value;
    }

    private static IEnumerable<string> SplitTopLevel(string text, char separator)
    {
        StringBuilder current = new();
        int depth = 0;
        char quote = '\0';
        foreach (char c in text)
        {
            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }

                current.Append(c);
                continue;
            }

            if (c is '\'' or '"')
            {
                quote = c;
            }
            else if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
            }
            else if (c == separator && depth == 0)
            {
                yield return current.ToString();
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        yield return current.ToString();
    }

    private static int IndexOfTopLevel(string text, char target)
    {
        int depth = 0;
        char quote = '\0';
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (quote != '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (c is '\'' or '"')
            {
                quote = c;
            }
            else if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
            }
            else if (c == target && depth == 0)
            {
                return i;
            }
        }

        return -1;
    }
}
