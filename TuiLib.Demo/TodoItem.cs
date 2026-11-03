using TuiLib.Ui.Data;

namespace TuiDemo;

public sealed class TodoItem : ObservableObject
{
    private string _title;
    private bool _isDone;
    private int _progress;

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public bool IsDone
    {
        get => _isDone;
        set
        {
            if (!SetField(ref _isDone, value))
            {
                return;
            }

            OnPropertyChanged(nameof(Glyph));
            if (value)
            {
                Progress = 100;
            }
        }
    }

    public int Progress
    {
        get => _progress;
        set => SetField(ref _progress, value);
    }

    public string Glyph => IsDone ? "✔" : "·";

    public TodoItem(string title, int progress = 0)
    {
        _title = title;
        _progress = progress;
    }
}
