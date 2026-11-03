using System;
using System.Collections.Generic;

namespace TuiLib.Helpers;

public sealed class SortChain<T>
{
    private readonly List<Func<T, object>> _keys = [];
    private readonly List<IComparer<object>?> _comparers = [];

    public SortChain<T> ThenBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey>? comparer = null)
    {
        _keys.Add(x => keySelector(x)!);
        _comparers.Add(comparer == null ? null : Comparer<object>.Create((a, b) => comparer.Compare((TKey) a, (TKey) b)));
        
        return this;
    }

    public int Compare(T x, T y)
    {
        for (int i = 0; i < _keys.Count; i++)
        {
            object a = _keys[i](x);
            object b = _keys[i](y);

            if (Equals(a, b))
            {
                continue;
            }

            if (_comparers[i] != null)
            {
                return _comparers[i]!.Compare(a, b);
            }

            return Comparer<object>.Default.Compare(a, b);
        }

        return 0;
    }
}