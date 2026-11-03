namespace TuiLib.Ui.Controls;

public abstract class DefinitionBase
{
    internal abstract GridLength Length { get; }
    internal abstract int MinLength { get; }
    internal abstract int MaxLength { get; }
    internal int ActualLength { get; set; }
    internal int Offset { get; set; }
}
