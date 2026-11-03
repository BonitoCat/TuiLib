namespace TuiLib.Components;

public interface ICommand
{
    string Name { get; }
    void Execute();
}