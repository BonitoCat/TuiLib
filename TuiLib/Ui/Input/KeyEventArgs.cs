using InputLib;

namespace TuiLib.Ui.Input;

public sealed class KeyEventArgs : TuiEventArgs
{
    public TuiKey Key { get; }
    public char Character { get; }
    public string Text { get; }
    public KeyModifiers Modifiers { get; }

    public bool Shift => (Modifiers & KeyModifiers.Shift) != 0;
    public bool Ctrl => (Modifiers & KeyModifiers.Ctrl) != 0;
    public bool Alt => (Modifiers & KeyModifiers.Alt) != 0;

    public KeyEventArgs(TuiKey key, KeyModifiers modifiers = KeyModifiers.None, char character = '\0', string? text = null)
    {
        Key = key;
        Modifiers = modifiers;
        Character = character;
        Text = text ?? (character == '\0' ? string.Empty : character.ToString());
    }

    public bool IsCharacter(char character)
    {
        return Key == TuiKey.Character && char.ToLowerInvariant(Character) == char.ToLowerInvariant(character);
    }

    public override string ToString()
    {
        string name = Key == TuiKey.Character ? $"'{Text}'" : Key.ToString();
        return Modifiers == KeyModifiers.None ? name : $"{Modifiers}+{name}";
    }
}
