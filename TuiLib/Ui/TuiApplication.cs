using System;
using TuiLib.Ui.Input;
using TuiLib.Ui.Platform;
using TuiLib.Ui.Rendering;

namespace TuiLib.Ui;

using Window = TuiLib.Ui.Controls.Window;

public class TuiApplication
{
    private volatile bool _shutdownRequested;

    public static TuiApplication? Current { get; private set; }

    public Window? MainWindow { get; private set; }
    public bool ExitOnCtrlC { get; set; } = true;
    public bool EnableMouse { get; set; } = true;
    public int FrameIntervalMs { get; set; } = 16;
    public int ExitCode { get; private set; }

    public static int Run<TWindow>() where TWindow : Window, new()
    {
        return new TuiApplication().Run(new TWindow());
    }

    public int Run(Window window)
    {
        return Run(window, TerminalBackend.Create());
    }

    public int Run(Window window, ITerminalBackend backend)
    {
        if (Current != null)
        {
            throw new InvalidOperationException("A TuiApplication is already running.");
        }

        Current = this;
        MainWindow = window;
        _shutdownRequested = false;
        Dispatcher.Attach();

        TerminalPresenter presenter = new(backend.Output);
        EventHandler<TuiEventArgs> titleChanged = (_, _) => backend.SetTitle(window.Title);
        try
        {
            backend.Enter(EnableMouse);
            backend.SetTitle(window.Title);
            window.TitleChanged += titleChanged;
            backend.StartInput(
                key => Dispatcher.Post(() => window.ProcessKey(key)),
                (kind, pointer) => Dispatcher.Post(() => window.ProcessPointer(kind, pointer)));
            window.Show();

            int width = -1;
            int height = -1;
            while (!_shutdownRequested)
            {
                Dispatcher.ProcessPending();
                if (_shutdownRequested)
                {
                    break;
                }

                Size size = backend.GetSize();
                if (size.Width != width || size.Height != height)
                {
                    width = size.Width;
                    height = size.Height;
                    presenter.Invalidate();
                    window.RequestFrame(true);
                }

                if (window.NeedsFrame)
                {
                    CellBuffer buffer = window.RenderFrame(width, height, out Point? cursor);
                    presenter.Present(buffer, cursor);
                }

                Dispatcher.WaitForWork(window.NeedsFrame ? FrameIntervalMs : 100);
            }
        }
        finally
        {
            window.TitleChanged -= titleChanged;
            backend.Exit();
            backend.Dispose();
            Dispatcher.Detach();
            Current = null;
        }

        return ExitCode;
    }

    public void Shutdown(int exitCode = 0)
    {
        ExitCode = exitCode;
        _shutdownRequested = true;
        Dispatcher.Wake();
    }

    internal void OnWindowClosed(Window window)
    {
        if (ReferenceEquals(window, MainWindow))
        {
            Shutdown(ExitCode);
        }
    }
}
