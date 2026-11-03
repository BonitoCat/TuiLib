namespace TuiLib.Components;

public sealed class RenderPart
{
    public int X { get; set; } = 0;
    public int Y { get; set; } = 0;
    public string Text { get; set; } = "";
    public Color Foreground { get; set; } = Color.Reset;
    public Color Background { get; set; } = Color.Reset;
    public int ZIndex { get; set; } = 0;
}