using System.Collections.ObjectModel;

namespace TuiLib.Ui.Controls;

public sealed class ElementCollection : Collection<Element>
{
    private readonly Element _owner;

    public ElementCollection(Element owner)
    {
        _owner = owner;
    }

    protected override void InsertItem(int index, Element item)
    {
        base.InsertItem(index, item);
        _owner.AttachChild(item, index);
    }

    protected override void RemoveItem(int index)
    {
        Element item = this[index];
        base.RemoveItem(index);
        _owner.DetachChild(item);
    }

    protected override void SetItem(int index, Element item)
    {
        Element previous = this[index];
        base.SetItem(index, item);
        _owner.DetachChild(previous);
        _owner.AttachChild(item, index);
    }

    protected override void ClearItems()
    {
        Element[] items = new Element[Count];
        CopyTo(items, 0);
        base.ClearItems();
        foreach (Element item in items)
        {
            _owner.DetachChild(item);
        }
    }
}
