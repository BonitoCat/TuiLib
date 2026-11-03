using TuiLib.Ui.Controls;
using TuiLib.Ui.Markup;

namespace TuiDemo;

public class ConfirmDialog : UserControl
{
    private TextBlock _messageText = null!;

    public ConfirmDialog(string message)
    {
        TuiXamlLoader.Load(this);
        _messageText.Text = message;
    }

    private void OnYesClick()
    {
        (Root as Window)?.CloseDialog(this, true);
    }

    private void OnNoClick()
    {
        (Root as Window)?.CloseDialog(this, false);
    }
}
