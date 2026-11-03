using System.Collections.Generic;
using System.Globalization;
using InputLib;

namespace TuiLib.Ui.Input;

public static class TerminalInputDecoder
{
    private const char Esc = '\x1b';

    public static List<KeyEventArgs> Decode(string text)
    {
        List<KeyEventArgs> keys = new();
        int index = 0;
        while (index < text.Length)
        {
            index = DecodeOne(text, index, keys);
        }

        return keys;
    }

    private static int DecodeOne(string text, int index, List<KeyEventArgs> keys)
    {
        char c = text[index];
        if (c == Esc)
        {
            return DecodeEscape(text, index, keys);
        }

        switch (c)
        {
            case '\r':
            case '\n':
                keys.Add(new(TuiKey.Enter));
                return index + 1;
            case '\t':
                keys.Add(new(TuiKey.Tab));
                return index + 1;
            case '\x7f':
                keys.Add(new(TuiKey.Backspace));
                return index + 1;
            case '\b':
                keys.Add(new(TuiKey.Backspace, KeyModifiers.Ctrl));
                return index + 1;
            case '\0':
                keys.Add(new(TuiKey.Character, KeyModifiers.Ctrl, ' '));
                return index + 1;
        }

        if (c >= 1 && c <= 26)
        {
            keys.Add(new(TuiKey.Character, KeyModifiers.Ctrl, (char) ('a' + c - 1)));
            return index + 1;
        }

        if (c < 0x20)
        {
            return index + 1;
        }

        string element = StringInfo.GetNextTextElement(text, index);
        keys.Add(new(TuiKey.Character, KeyModifiers.None, element[0], element));
        return index + element.Length;
    }

    private static int DecodeEscape(string text, int index, List<KeyEventArgs> keys)
    {
        if (index + 1 >= text.Length || text[index + 1] == Esc)
        {
            keys.Add(new(TuiKey.Escape));
            return index + 1;
        }

        char next = text[index + 1];
        if (next == '[' && index + 2 < text.Length)
        {
            return DecodeCsi(text, index + 2, keys);
        }

        if (next == 'O' && index + 2 < text.Length)
        {
            return DecodeSs3(text, index + 2, keys);
        }

        List<KeyEventArgs> inner = new();
        int end = DecodeOne(text, index + 1, inner);
        foreach (KeyEventArgs key in inner)
        {
            keys.Add(new(key.Key, key.Modifiers | KeyModifiers.Alt, key.Character, key.Text));
        }

        return end;
    }

    private static int DecodeCsi(string text, int start, List<KeyEventArgs> keys)
    {
        int index = start;
        while (index < text.Length && text[index] >= 0x20 && text[index] <= 0x3f)
        {
            index++;
        }

        if (index >= text.Length)
        {
            return text.Length;
        }

        char final = text[index];
        string body = text.Substring(start, index - start);
        int[] parameters = ParseParameters(body);
        int first = parameters.Length > 0 ? parameters[0] : 1;
        KeyModifiers modifiers = parameters.Length > 1 ? ToModifiers(parameters[1]) : KeyModifiers.None;

        if (final == '~')
        {
            TuiKey? key = MapTilde(first);
            if (key != null)
            {
                keys.Add(new(key.Value, modifiers));
            }

            return index + 1;
        }

        if (final == 'u')
        {
            if (first > 0 && first < 0x110000)
            {
                string character = char.ConvertFromUtf32(first);
                keys.Add(MapCodePoint(character, modifiers));
            }

            return index + 1;
        }

        if (final == 'Z')
        {
            keys.Add(new(TuiKey.Tab, KeyModifiers.Shift));
            return index + 1;
        }

        TuiKey? letterKey = MapLetter(final);
        if (letterKey != null)
        {
            keys.Add(new(letterKey.Value, modifiers));
        }

        return index + 1;
    }

    private static int DecodeSs3(string text, int start, List<KeyEventArgs> keys)
    {
        TuiKey? key = MapLetter(text[start]);
        if (key != null)
        {
            keys.Add(new(key.Value));
        }

        return start + 1;
    }

    private static KeyEventArgs MapCodePoint(string character, KeyModifiers modifiers)
    {
        return character switch
        {
            "\r" => new(TuiKey.Enter, modifiers),
            "\t" => new(TuiKey.Tab, modifiers),
            "\x1b" => new(TuiKey.Escape, modifiers),
            "\x7f" => new(TuiKey.Backspace, modifiers),
            _ => new(TuiKey.Character, modifiers, character[0], character),
        };
    }

    private static TuiKey? MapLetter(char final)
    {
        return final switch
        {
            'A' => TuiKey.Up,
            'B' => TuiKey.Down,
            'C' => TuiKey.Right,
            'D' => TuiKey.Left,
            'H' => TuiKey.Home,
            'F' => TuiKey.End,
            'P' => TuiKey.F1,
            'Q' => TuiKey.F2,
            'R' => TuiKey.F3,
            'S' => TuiKey.F4,
            _ => null,
        };
    }

    private static TuiKey? MapTilde(int code)
    {
        return code switch
        {
            1 or 7 => TuiKey.Home,
            2 => TuiKey.Insert,
            3 => TuiKey.Delete,
            4 or 8 => TuiKey.End,
            5 => TuiKey.PageUp,
            6 => TuiKey.PageDown,
            11 => TuiKey.F1,
            12 => TuiKey.F2,
            13 => TuiKey.F3,
            14 => TuiKey.F4,
            15 => TuiKey.F5,
            17 => TuiKey.F6,
            18 => TuiKey.F7,
            19 => TuiKey.F8,
            20 => TuiKey.F9,
            21 => TuiKey.F10,
            23 => TuiKey.F11,
            24 => TuiKey.F12,
            _ => null,
        };
    }

    private static int[] ParseParameters(string body)
    {
        if (body.Length == 0)
        {
            return [];
        }

        string[] parts = body.Split(';');
        int[] values = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];
            int colon = part.IndexOf(':');
            if (colon >= 0)
            {
                part = part.Substring(0, colon);
            }

            values[i] = int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 1;
        }

        return values;
    }

    private static KeyModifiers ToModifiers(int value)
    {
        int bits = value - 1;
        KeyModifiers modifiers = KeyModifiers.None;
        if ((bits & 1) != 0)
        {
            modifiers |= KeyModifiers.Shift;
        }

        if ((bits & 2) != 0)
        {
            modifiers |= KeyModifiers.Alt;
        }

        if ((bits & 4) != 0)
        {
            modifiers |= KeyModifiers.Ctrl;
        }

        return modifiers;
    }
}
