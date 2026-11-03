using TuiLib.Ui.Input;

namespace TuiLib.Ui;

public interface IRenderRoot
{
    FocusManager FocusManager { get; }
    void RequestFrame(bool layout);
}
