namespace TuiLib.Ui.Controls;

public class Panel : Element
{
    [Content]
    public ElementCollection Children { get; }

    public Panel()
    {
        Children = new(this);
    }
}
