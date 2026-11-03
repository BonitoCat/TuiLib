namespace TuiLib.Ui.Controls;

public class ListBoxItem : ContentControl
{
    public static readonly TuiProperty<bool> IsSelectedProperty = TuiProperty.Register<ListBoxItem, bool>(nameof(IsSelected), false, PropertyFlags.AffectsRender);

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public ListBoxItem()
    {
        Padding = new(1, 0);
    }

    protected override Color? GetRenderBackground()
    {
        if (IsSelected)
        {
            bool active = FindAncestor<ListBox>()?.IsFocused ?? false;
            return active ? Theme.Accent : Theme.InactiveSelectionBackground;
        }

        if (IsPointerOver && IsEffectivelyEnabled)
        {
            return Theme.ControlHoverBackground;
        }

        return Background;
    }
}
