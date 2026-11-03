namespace TuiLib.Ui.Controls;

public sealed class RowDefinition : DefinitionBase
{
    public GridLength Height { get; set; } = GridLength.Star;
    public int MinHeight { get; set; }
    public int MaxHeight { get; set; } = int.MaxValue;
    public int ActualHeight => ActualLength;

    public RowDefinition()
    {
    }

    public RowDefinition(GridLength height)
    {
        Height = height;
    }

    internal override GridLength Length => Height;
    internal override int MinLength => MinHeight;
    internal override int MaxLength => MaxHeight;
}
