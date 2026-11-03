using System;
using System.Collections.Generic;
using TuiLib.Components;

namespace TuiLib.Controls;

public class TuiListBoxItem<TItem> : Control
{
    public override ControlType Type => ControlType.ListBoxItem;
    public TItem? Value { get; set; }

    public Func<TItem, IEnumerable<RenderPart>>? Renderer { get; set; }
    public Action<TItem>? ClickHandler { get; set; }

    public TuiListBoxItem()
    {
        
    }
    
    // TODO: REMOVE LATER WHEN NO LONGER NEEDED
    public TuiListBoxItem(string str, Action action)
    {
        
    }
    
    protected override IEnumerable<RenderPart> RenderCore()
    {
        if (Value == null || Renderer == null)
        {
            yield break;
        }

        foreach (RenderPart part in Renderer(Value))
        {
            yield return part;
        }
    }

    public bool CallOnClick()
    {
        if (Value == null)
        {
            return false;
        }

        ClickHandler?.Invoke(Value);
        return true;
    }
}