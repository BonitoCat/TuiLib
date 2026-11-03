using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using TuiLib.Ui.Data;

namespace TuiDemo;

public sealed class MainViewModel : ObservableObject
{
    private TodoItem? _selectedTodo;
    private string _newTitle = string.Empty;
    private string _status = string.Empty;

    public string Title => "TuiLib demo";

    public ObservableCollection<TodoItem> Todos { get; } = new()
    {
        new("Write the markup loader", 100) { IsDone = true },
        new("Add data binding", 80),
        new("Build a demo app", 35),
        new("Port Ferret's CLI", 0),
    };

    public TodoItem? SelectedTodo
    {
        get => _selectedTodo;
        set
        {
            if (!SetField(ref _selectedTodo, value))
            {
                return;
            }

            RemoveCommand.RaiseCanExecuteChanged();
            UpdateStatus();
        }
    }

    public string NewTitle
    {
        get => _newTitle;
        set
        {
            if (!SetField(ref _newTitle, value))
            {
                return;
            }

            AddCommand.RaiseCanExecuteChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public RelayCommand AddCommand { get; }
    public RelayCommand RemoveCommand { get; }

    public MainViewModel()
    {
        AddCommand = new(Add, () => NewTitle.Trim().Length > 0);
        RemoveCommand = new(Remove, () => SelectedTodo != null);
        Todos.CollectionChanged += OnTodosChanged;
        foreach (TodoItem todo in Todos)
        {
            todo.PropertyChanged += OnTodoChanged;
        }

        UpdateStatus();
    }

    public void Tick()
    {
        foreach (TodoItem todo in Todos.Where(todo => !todo.IsDone && todo.Progress > 0))
        {
            todo.Progress = Math.Min(99, todo.Progress + 1);
        }
    }

    private void Add()
    {
        TodoItem todo = new(NewTitle.Trim());
        Todos.Add(todo);
        SelectedTodo = todo;
        NewTitle = string.Empty;
    }

    private void Remove()
    {
        if (SelectedTodo == null)
        {
            return;
        }

        int index = Todos.IndexOf(SelectedTodo);
        Todos.Remove(SelectedTodo);
        SelectedTodo = Todos.Count == 0 ? null : Todos[Math.Min(index, Todos.Count - 1)];
    }

    private void OnTodosChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (TodoItem todo in e.OldItems?.OfType<TodoItem>() ?? Enumerable.Empty<TodoItem>())
        {
            todo.PropertyChanged -= OnTodoChanged;
        }

        foreach (TodoItem todo in e.NewItems?.OfType<TodoItem>() ?? Enumerable.Empty<TodoItem>())
        {
            todo.PropertyChanged += OnTodoChanged;
        }

        UpdateStatus();
    }

    private void OnTodoChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodoItem.IsDone))
        {
            UpdateStatus();
        }
    }

    private void UpdateStatus()
    {
        int done = Todos.Count(todo => todo.IsDone);
        Status = $"{done}/{Todos.Count} done · Tab/arrows move focus · Enter toggles · Ctrl+C quits";
    }
}
