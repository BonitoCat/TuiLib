using System;
using TuiLib.Ui;
using TuiLib.Ui.Controls;
using TuiLib.Ui.Input;
using TuiLib.Ui.Markup;

namespace TuiDemo;

public class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private TextBox _newTodoBox = null!;
    private ListBox _todoList = null!;

    public MainWindow()
    {
        TuiXamlLoader.Load(this);
        DataContext = _viewModel;
        _todoList.Focus();
        Opened += (_, _) => Dispatcher.StartTimer(TimeSpan.FromMilliseconds(250), () =>
        {
            _viewModel.Tick();
            return true;
        });
    }

    private void OnTodoActivated(object? sender, ItemActivatedEventArgs e)
    {
        if (e.Item is TodoItem todo)
        {
            todo.IsDone = !todo.IsDone;
        }
    }

    private void OnNewTodoKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != TuiKey.Enter)
        {
            return;
        }

        e.Handled = true;
        _viewModel.AddCommand.Execute(null);
        _newTodoBox.Focus();
    }

    private async void OnRemoveClick()
    {
        if (_viewModel.SelectedTodo == null)
        {
            return;
        }

        ConfirmDialog dialog = new($"Remove \"{_viewModel.SelectedTodo.Title}\"?");
        if (await ShowDialog(dialog) is true)
        {
            _viewModel.RemoveCommand.Execute(null);
        }
    }
}
