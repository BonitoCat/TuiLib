using System.Collections.Generic;
using TuiLib.Components;

namespace TuiLib.Controls;

public sealed class TuiStackPanel : TuiPanel
{
    public override ControlType Type => ControlType.StackPanel;
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    protected override void Arrange()
    {
        int offset = 0;
        foreach (Control child in Children)
        {
            child.X = 0;
            child.Y = offset;

            offset += child.Height;
        }

        Height = offset;
    }

    protected override IEnumerable<RenderPart> RenderCore()
    {
        int offset = 0;
        foreach (Control child in Children)
        {
            foreach (RenderPart part in child.Render())
            {
                yield return new()
                {
                    X = part.X,
                    Y = part.Y + offset,
                    Text = part.Text,
                    Foreground = part.Foreground,
                    Background = part.Background,
                    ZIndex = part.ZIndex,
                };
            }
            
            offset += child.Height;
        }
    }
}