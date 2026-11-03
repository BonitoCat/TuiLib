namespace TuiLib.Components;

public interface IViewContainer : IRenderable, ISizable, IPositionable
{
    void OnFocus();
    void OnBlur();
}