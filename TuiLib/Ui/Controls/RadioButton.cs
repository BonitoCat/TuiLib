using System.Linq;

namespace TuiLib.Ui.Controls;

public class RadioButton : CheckBox
{
    public static readonly TuiProperty<string?> GroupNameProperty = TuiProperty.Register<RadioButton, string?>(nameof(GroupName), null);

    public string? GroupName
    {
        get => GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    protected override string Glyph => IsChecked == true ? "(•)" : "( )";

    public override void Toggle()
    {
        IsChecked = true;
    }

    protected override void OnPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property != IsCheckedProperty || IsChecked != true)
        {
            return;
        }

        Element? scope = GroupName == null ? Parent : GetTopMost();
        if (scope == null)
        {
            return;
        }

        foreach (RadioButton other in scope.GetSelfAndDescendants().OfType<RadioButton>().ToList())
        {
            if (ReferenceEquals(other, this) || other.GroupName != GroupName)
            {
                continue;
            }

            if (GroupName == null && !ReferenceEquals(other.Parent, Parent))
            {
                continue;
            }

            other.IsChecked = false;
        }
    }

    private Element GetTopMost()
    {
        Element current = this;
        while (current.Parent != null)
        {
            current = current.Parent;
        }

        return current;
    }
}
