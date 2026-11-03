using System;
using System.IO;
using System.Threading;
using InputLib;
using TuiLib.Ui.Input;

namespace TuiLib.Ui.Platform;

internal sealed class ConsoleTerminalBackend : ITerminalBackend
{
    private Thread? _thread;
    private volatile bool _running;
    private bool _entered;
    private bool _previousTreatControlC;

    public Stream Output { get; } = Console.OpenStandardOutput();

    public Size GetSize() => TerminalBackend.GetConsoleSize();

    public void Enter(bool enableMouse)
    {
        try
        {
            _previousTreatControlC = Console.TreatControlCAsInput;
            Console.TreatControlCAsInput = true;
        }
        catch (IOException)
        {
        }

        TerminalBackend.Write(Output, TerminalBackend.EnterSequence(false));
        _entered = true;
    }

    public void Exit()
    {
        _running = false;
        if (!_entered)
        {
            return;
        }

        _entered = false;
        TerminalBackend.Write(Output, TerminalBackend.ExitSequence);
        try
        {
            Console.TreatControlCAsInput = _previousTreatControlC;
        }
        catch (IOException)
        {
        }
    }

    public void SetTitle(string? title)
    {
        TerminalBackend.Write(Output, TerminalBackend.TitleSequence(title));
    }

    public void StartInput(Action<KeyEventArgs> onKey, Action<PointerEventKind, PointerEventArgs> onPointer)
    {
        _running = true;
        _thread = new(() => ReadLoop(onKey))
        {
            IsBackground = true,
            Name = "TuiLib input",
        };
        _thread.Start();
    }

    public void Dispose()
    {
        Exit();
    }

    private void ReadLoop(Action<KeyEventArgs> onKey)
    {
        while (_running)
        {
            bool available;
            try
            {
                available = Console.KeyAvailable;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (!available)
            {
                Thread.Sleep(15);
                continue;
            }

            onKey(Map(Console.ReadKey(true)));
        }
    }

    private static KeyEventArgs Map(ConsoleKeyInfo info)
    {
        KeyModifiers modifiers = KeyModifiers.None;
        if ((info.Modifiers & ConsoleModifiers.Shift) != 0)
        {
            modifiers |= KeyModifiers.Shift;
        }

        if ((info.Modifiers & ConsoleModifiers.Control) != 0)
        {
            modifiers |= KeyModifiers.Ctrl;
        }

        if ((info.Modifiers & ConsoleModifiers.Alt) != 0)
        {
            modifiers |= KeyModifiers.Alt;
        }

        TuiKey key = info.Key switch
        {
            ConsoleKey.Enter => TuiKey.Enter,
            ConsoleKey.Escape => TuiKey.Escape,
            ConsoleKey.Tab => TuiKey.Tab,
            ConsoleKey.Backspace => TuiKey.Backspace,
            ConsoleKey.Delete => TuiKey.Delete,
            ConsoleKey.Insert => TuiKey.Insert,
            ConsoleKey.UpArrow => TuiKey.Up,
            ConsoleKey.DownArrow => TuiKey.Down,
            ConsoleKey.LeftArrow => TuiKey.Left,
            ConsoleKey.RightArrow => TuiKey.Right,
            ConsoleKey.Home => TuiKey.Home,
            ConsoleKey.End => TuiKey.End,
            ConsoleKey.PageUp => TuiKey.PageUp,
            ConsoleKey.PageDown => TuiKey.PageDown,
            >= ConsoleKey.F1 and <= ConsoleKey.F12 => TuiKey.F1 + (info.Key - ConsoleKey.F1),
            _ => TuiKey.Character,
        };

        if (key != TuiKey.Character)
        {
            return new(key, modifiers);
        }

        char character = info.KeyChar;
        if ((modifiers & KeyModifiers.Ctrl) != 0 && character >= 1 && character <= 26)
        {
            character = (char) ('a' + character - 1);
        }

        if (character == '\0' && info.Key >= ConsoleKey.A && info.Key <= ConsoleKey.Z)
        {
            character = (char) ('a' + (info.Key - ConsoleKey.A));
        }

        return new(TuiKey.Character, modifiers & ~KeyModifiers.Shift, character);
    }
}
