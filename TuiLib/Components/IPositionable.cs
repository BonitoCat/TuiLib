namespace TuiLib.Components;

public interface IPositionable
{
    public int X { get; set; }
    public int Y { get; set; }
    public AnchorPoint AnchorPoint { get; set; }
}