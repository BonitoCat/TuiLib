namespace TuiLib.Ui.Input;

public sealed class TextInputEventArgs : TuiEventArgs
{
    public string Text { get; }

    public TextInputEventArgs(string text)
    {
        Text = text;
    }
}
