using System;
using System.IO;
using TuiLib.Ui.Input;

namespace TuiLib.Ui.Platform;

public interface ITerminalBackend : IDisposable
{
    Stream Output { get; }
    Size GetSize();
    void Enter(bool enableMouse);
    void Exit();
    void SetTitle(string? title);
    void StartInput(Action<KeyEventArgs> onKey, Action<PointerEventKind, PointerEventArgs> onPointer);
}
