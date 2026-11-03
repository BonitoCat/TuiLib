using TuiLib.Ui.Input;

namespace TuiLib.Ui.Controls;

public sealed class ItemActivatedEventArgs : TuiEventArgs
{
    public object? Item { get; }
    public int Index { get; }

    public ItemActivatedEventArgs(object? item, int index)
    {
        Item = item;
        Index = index;
    }
}
