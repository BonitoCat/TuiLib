using System.Collections.Generic;
using TuiLib.Components;

namespace TuiLib.Controls;

public class TuiButton : Control
{
    public override ControlType Type => ControlType.Button;
    public TuiLabel Label
    {
        get => _label;
        set
        {
            _label = value;
            _border.Child = _label;
        }
    }
    
    private readonly TuiBorder _border = new();

    private TuiLabel _label;

    public TuiButton()
    {
        _label = new();
    }
    
    public TuiButton(string text)
    {
        Label = new(text);
        _border = new()
        {
            Child = Label,
        };
    }
    
    protected override IEnumerable<RenderPart> RenderCore()
    {
        foreach (RenderPart part in _border.Render())
        {
            yield return new()
            {
                X = part.X,
                Y = part.Y,
                Text = part.Text,
                Foreground = part.Foreground,
                Background = part.Background,
                ZIndex = ZIndex,
            };
            yield return part;
        }
    }
}