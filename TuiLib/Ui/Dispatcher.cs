using System;
using System.Threading;
using System.Threading.Tasks;

namespace TuiLib.Ui;

public static class Dispatcher
{
    private static readonly UiDispatcher Queue = new();
    private static readonly AutoResetEvent WakeEvent = new(false);
    private static int _uiThreadId = -1;

    public static bool IsRunning => _uiThreadId != -1;

    public static bool CheckAccess()
    {
        return _uiThreadId == -1 || Thread.CurrentThread.ManagedThreadId == _uiThreadId;
    }

    public static void Post(Action action)
    {
        Queue.Post(action);
        WakeEvent.Set();
    }

    public static void Invoke(Action action)
    {
        if (CheckAccess())
        {
            action();
            return;
        }

        Post(action);
    }

    public static Task<T> InvokeAsync<T>(Func<T> function)
    {
        TaskCompletionSource<T> completion = new();
        Invoke(() =>
        {
            try
            {
                completion.SetResult(function());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        return completion.Task;
    }

    public static Task InvokeAsync(Action action)
    {
        return InvokeAsync(() =>
        {
            action();
            return true;
        });
    }

    public static IDisposable StartTimer(TimeSpan interval, Func<bool> tick)
    {
        Timer? timer = null;
        int busy = 0;
        timer = new(_ =>
        {
            if (Interlocked.Exchange(ref busy, 1) == 1)
            {
                return;
            }

            Post(() =>
            {
                busy = 0;
                if (!tick())
                {
                    timer?.Dispose();
                }
            });
        }, null, interval, interval);
        return timer;
    }

    public static void Wake() => WakeEvent.Set();

    internal static void Attach()
    {
        _uiThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    internal static void Detach()
    {
        _uiThreadId = -1;
        Queue.ClearPending();
    }

    internal static void ProcessPending()
    {
        Queue.ExecutePending();
    }

    internal static void WaitForWork(int timeoutMs)
    {
        WakeEvent.WaitOne(timeoutMs);
    }
}
