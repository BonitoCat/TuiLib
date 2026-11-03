namespace TuiLib.Rendering;

public class TextStyle
{
    public Color Foreground { get; set; } = Color.Reset;
    public Color Background { get; set; } = Color.Reset;
    public FontStyles FontStyles { get; set; } = new();
    public bool Ellipse { get; set; } = true;
    public bool TextWrapping
    {
        get => _textWrapping;
        set
        {
            _textWrapping = value;
            _wordWrapping = !value && _wordWrapping;
        }
    }

    public bool WordWrapping
    {
        get => _wordWrapping;
        set
        {
            _wordWrapping = value;
            _textWrapping = !value && _textWrapping;
        }
    }
    
    private bool _textWrapping;
    private bool _wordWrapping;
}