using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using InputLib;
using InputLib.EventArgs;
using InputLib.Tty;
using TuiLib.Ui.Input;
using SystemEventArgs = System.EventArgs;

namespace TuiLib.Ui.Platform;

internal sealed class UnixTerminalBackend : ITerminalBackend
{
    private const int StdIn = 0;
    private const int StdOut = 1;
    private const int TermiosSize = 60;
    private const int LocalFlagsOffset = 12;
    private const int ControlCharsOffset = 17;
    private const int VTime = 5;
    private const int VMin = 6;
    private const uint Brkint = 0x2;
    private const uint Inpck = 0x10;
    private const uint Istrip = 0x20;
    private const uint Icrnl = 0x100;
    private const uint Ixon = 0x400;
    private const uint Isig = 0x1;
    private const uint Icanon = 0x2;
    private const uint Echo = 0x8;
    private const uint Iexten = 0x8000;
    private const short PollIn = 0x1;
    private const int PollTimeoutMs = 30;

    private readonly TerminalMouseParser _parser = new();
    private readonly object _restoreLock = new();
    private byte[]? _original;
    private Thread? _thread;
    private volatile bool _running;
    private bool _entered;

    [StructLayout(LayoutKind.Sequential)]
    private struct PollFd
    {
        public int Fd;
        public short Events;
        public short Revents;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int tcgetattr(int fd, byte[] termios);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcsetattr(int fd, int actions, byte[] termios);

    [DllImport("libc")]
    private static extern int isatty(int fd);

    [DllImport("libc")]
    private static extern int poll([In, Out] PollFd[] fds, uint count, int timeout);

    [DllImport("libc")]
    private static extern int read(int fd, byte[] buffer, int count);

    public static bool IsSupported
    {
        get
        {
            try
            {
                return isatty(StdIn) == 1 && isatty(StdOut) == 1;
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                return false;
            }
        }
    }

    public Stream Output { get; } = Console.OpenStandardOutput();

    public Size GetSize() => TerminalBackend.GetConsoleSize();

    public void Enter(bool enableMouse)
    {
        byte[] original = new byte[TermiosSize + 64];
        if (tcgetattr(StdIn, original) == 0)
        {
            _original = original;
            byte[] raw = (byte[]) original.Clone();
            WriteFlags(raw, 0, ReadFlags(raw, 0) & ~(Brkint | Inpck | Istrip | Icrnl | Ixon));
            WriteFlags(raw, LocalFlagsOffset, ReadFlags(raw, LocalFlagsOffset) & ~(Isig | Icanon | Echo | Iexten));
            raw[ControlCharsOffset + VMin] = 1;
            raw[ControlCharsOffset + VTime] = 0;
            tcsetattr(StdIn, 0, raw);
        }

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TerminalBackend.Write(Output, TerminalBackend.EnterSequence(enableMouse));
        _entered = true;
    }

    public void Exit()
    {
        lock (_restoreLock)
        {
            _running = false;
            if (!_entered)
            {
                return;
            }

            _entered = false;
            TerminalBackend.Write(Output, TerminalBackend.ExitSequence);
            if (_original != null)
            {
                tcsetattr(StdIn, 0, _original);
            }
        }

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        if (_thread != null && _thread != Thread.CurrentThread)
        {
            _thread.Join(PollTimeoutMs * 4);
        }
    }

    public void SetTitle(string? title)
    {
        TerminalBackend.Write(Output, TerminalBackend.TitleSequence(title));
    }

    public void StartInput(Action<KeyEventArgs> onKey, Action<PointerEventKind, PointerEventArgs> onPointer)
    {
        _parser.OnText = text =>
        {
            foreach (KeyEventArgs key in TerminalInputDecoder.Decode(text))
            {
                onKey(key);
            }
        };
        _parser.OnMouse = mouse => onPointer(MapKind(mouse), MapPointer(mouse));

        _running = true;
        _thread = new(ReadLoop)
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

    private void ReadLoop()
    {
        byte[] buffer = new byte[1024];
        PollFd[] fds = { new() { Fd = StdIn, Events = PollIn } };
        while (_running)
        {
            fds[0].Revents = 0;
            if (poll(fds, 1, PollTimeoutMs) <= 0 || (fds[0].Revents & PollIn) == 0)
            {
                _parser.FlushPending();
                continue;
            }

            int count = read(StdIn, buffer, buffer.Length);
            if (count <= 0)
            {
                Thread.Sleep(PollTimeoutMs);
                continue;
            }

            _parser.Feed(buffer, count);
        }
    }

    private static PointerEventKind MapKind(TerminalMouseEventArgs mouse)
    {
        return mouse.Kind switch
        {
            TerminalMouseEventKind.Press => PointerEventKind.Pressed,
            TerminalMouseEventKind.Release => PointerEventKind.Released,
            TerminalMouseEventKind.Scroll => PointerEventKind.Wheel,
            _ => PointerEventKind.Moved,
        };
    }

    private static PointerEventArgs MapPointer(TerminalMouseEventArgs mouse)
    {
        int delta = mouse.Button == MouseButton.ScrollUp ? -1 : mouse.Button == MouseButton.ScrollDown ? 1 : 0;
        return new(new(mouse.Column, mouse.Row), mouse.Button, mouse.Modifiers, delta);
    }

    private static uint ReadFlags(byte[] termios, int offset) => BitConverter.ToUInt32(termios, offset);

    private static void WriteFlags(byte[] termios, int offset, uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Array.Copy(bytes, 0, termios, offset, bytes.Length);
    }

    private void OnProcessExit(object? sender, SystemEventArgs e) => Exit();

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e) => Exit();
}
