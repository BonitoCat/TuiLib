namespace TuiLib.Ui.Controls;

public sealed class ColumnDefinition : DefinitionBase
{
    public GridLength Width { get; set; } = GridLength.Star;
    public int MinWidth { get; set; }
    public int MaxWidth { get; set; } = int.MaxValue;
    public int ActualWidth => ActualLength;

    public ColumnDefinition()
    {
    }

    public ColumnDefinition(GridLength width)
    {
        Width = width;
    }

    internal override GridLength Length => Width;
    internal override int MinLength => MinWidth;
    internal override int MaxLength => MaxWidth;
}
