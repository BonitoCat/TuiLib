namespace TuiLib.Components;

public interface ISizable
{
    public int MaxWidth { get; set; }
    public int MaxHeight { get; set; }
    
    public int Width { get; set; }
    public int Height { get; set; }
}