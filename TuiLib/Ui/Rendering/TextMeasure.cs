using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TuiLib.Ui.Rendering;

public static class TextMeasure
{
    private const string Ellipsis = "…";

    public static IEnumerable<string> GetElements(string text)
    {
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            yield return enumerator.GetTextElement();
        }
    }

    public static int GetWidth(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int width = 0;
        foreach (string element in GetElements(text!))
        {
            width += GetElementWidth(element);
        }

        return width;
    }

    public static int GetElementWidth(string element)
    {
        if (element.Length == 0)
        {
            return 0;
        }

        int codePoint = char.IsSurrogatePair(element, 0) ? char.ConvertToUtf32(element, 0) : element[0];
        if (codePoint < 0x20 || (codePoint >= 0x7f && codePoint < 0xa0))
        {
            return 0;
        }

        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(element, 0);
        if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format)
        {
            return 0;
        }

        return IsWide(codePoint) ? 2 : 1;
    }

    public static bool IsWide(int codePoint)
    {
        return codePoint is >= 0x1100 and <= 0x115f
            or >= 0x2e80 and <= 0x303e
            or >= 0x3041 and <= 0x33ff
            or >= 0x3400 and <= 0x4dbf
            or >= 0x4e00 and <= 0x9fff
            or >= 0xa000 and <= 0xa4cf
            or >= 0xac00 and <= 0xd7a3
            or >= 0xf900 and <= 0xfaff
            or >= 0xfe30 and <= 0xfe4f
            or >= 0xff00 and <= 0xff60
            or >= 0xffe0 and <= 0xffe6
            or >= 0x1f300 and <= 0x1f64f
            or >= 0x1f900 and <= 0x1f9ff
            or >= 0x20000 and <= 0x3fffd;
    }

    public static string Truncate(string text, int width, bool ellipsis = false)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (GetWidth(text) <= width)
        {
            return text;
        }

        int limit = ellipsis ? width - 1 : width;
        StringBuilder builder = new();
        int used = 0;
        foreach (string element in GetElements(text))
        {
            int elementWidth = GetElementWidth(element);
            if (used + elementWidth > limit)
            {
                break;
            }

            builder.Append(element);
            used += elementWidth;
        }

        if (ellipsis)
        {
            builder.Append(Ellipsis);
        }

        return builder.ToString();
    }

    public static List<string> SplitLines(string text)
    {
        return new(text.Replace("\r\n", "\n").Split('\n'));
    }

    public static List<string> Wrap(string text, int width)
    {
        List<string> result = new();
        foreach (string line in SplitLines(text))
        {
            WrapLine(line, width, result);
        }

        return result;
    }

    private static void WrapLine(string line, int width, List<string> result)
    {
        if (width <= 0 || GetWidth(line) <= width)
        {
            result.Add(line);
            return;
        }

        StringBuilder current = new();
        int currentWidth = 0;
        foreach (string word in SplitWords(line))
        {
            int wordWidth = GetWidth(word);
            bool isSpace = word.Trim().Length == 0;
            if (currentWidth + wordWidth <= width)
            {
                current.Append(word);
                currentWidth += wordWidth;
                continue;
            }

            if (isSpace)
            {
                result.Add(current.ToString().TrimEnd());
                current.Clear();
                currentWidth = 0;
                continue;
            }

            if (currentWidth > 0)
            {
                result.Add(current.ToString().TrimEnd());
                current.Clear();
                currentWidth = 0;
            }

            foreach (string element in GetElements(word))
            {
                int elementWidth = GetElementWidth(element);
                if (currentWidth + elementWidth > width && currentWidth > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    currentWidth = 0;
                }

                current.Append(element);
                currentWidth += elementWidth;
            }
        }

        if (current.Length > 0 || result.Count == 0)
        {
            result.Add(current.ToString().TrimEnd());
        }
    }

    private static IEnumerable<string> SplitWords(string line)
    {
        StringBuilder word = new();
        bool? isSpace = null;
        foreach (char c in line)
        {
            bool space = c == ' ';
            if (isSpace != null && space != isSpace)
            {
                yield return word.ToString();
                word.Clear();
            }

            word.Append(c);
            isSpace = space;
        }

        if (word.Length > 0)
        {
            yield return word.ToString();
        }
    }
}
