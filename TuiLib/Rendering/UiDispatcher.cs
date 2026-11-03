using System;
using System.Collections.Concurrent;

namespace TuiLib;

public sealed class UiDispatcher
{
    private ConcurrentQueue<Action> _queue = new();

    public void Post(Action action)
    {
        _queue.Enqueue(action);
    }

    public void ClearPending()
    {
        _queue = new();
    }

    public void ExecutePending()
    {
        while (_queue.TryDequeue(out Action? action))
        {
            action();
        }
    }
}