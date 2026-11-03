using System.Collections.Generic;

namespace TuiLib.Controls;

public abstract class TuiPanel : Control
{
    public readonly List<Control> Children = [];

    public void Add(Control child)
    {
        child.Parent = this;
        Children.Add(child);
        InvalidateLayout();
    }
    
    protected abstract void Arrange();

    protected void InvalidateLayout()
    {
        Arrange();
    }
}