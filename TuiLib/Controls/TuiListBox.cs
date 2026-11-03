using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using TuiLib.Rendering;
using TuiLib.Components;
using TuiLib.Helpers;
using TuiLib.Options;

namespace TuiLib.Controls;

public class TuiListBox<TItem> : Control where TItem : class 
{
    public override ControlType Type => ControlType.ListBox;
    public int SelectedIndex { get; set; }
    public TuiListBoxItem<TItem>? SelectedItem
    {
        get
        {
            lock (Lock)
            {
                return SelectedIndex >= 0 && SelectedIndex < _items.Count
                    ? _items[SelectedIndex]
                    : null;
            }
        }
    }
    
    public Func<TItem, IEnumerable<RenderPart>> ItemRenderer { get; set; } = _ => [];
    public Action<TItem>? OnItemClick { get; set; }

    public int ViewIndex { get; set; }
    public int ViewRange
    {
        get => _viewRange ?? Height;
        set => _viewRange = value;
    }
    public int ScrollOvershoot { get; set; } = 2;
    public bool ShowScrollBar { get; set; } = true;
    public Color ScrollBarColor { get; set; } = Color.White;
    public bool UseDefaultMarker { get; set; } = true;
    public object Lock { get; } = new();

    public int MaxContentWidth => MaxWidth - (ShowScrollBar ? 1 : 0);

    public event Action<TuiListBoxItem<TItem>> OnItemAdded = _ => {};
    public event Action<TuiListBoxItem<TItem>> OnItemRemoved = _ => {};
    public Action MenuUpdate = () => {};

    public IReadOnlyList<TuiListBoxItem<TItem>> Items
    {
        get
        {
            lock (Lock)
            {
                return _items;
            }
        }
    }

    public List<TuiListBoxItem<TItem>> ItemsSource
    {
        get => _itemsSource.ToList();
        set
        {
            _itemsSource.CollectionChanged -= OnItemsSourceChanged;
            
            _itemsSource = new ObservableCollection<TuiListBoxItem<TItem>>(value);
            _itemsSource.CollectionChanged += OnItemsSourceChanged;

            ClearItems();
            AddItemRange(_itemsSource);
        }
    }

    public TuiListBoxItem<TItem> this[int index]
    {
        get
        {
            return GetItemAt(index);
        }
        set
        {
            lock (Lock)
            {
                _items[index] = value;
            }
        }
    }
    
    private readonly List<TuiListBoxItem<TItem>> _items = [];
    private ObservableCollection<TuiListBoxItem<TItem>> _itemsSource = [];
    private int? _viewRange;
    
    public void OnFocus() { }
    public void OnBlur() { }

    public TuiListBox()
    {
        _itemsSource.CollectionChanged += OnItemsSourceChanged;
    }
    
    protected override IEnumerable<RenderPart> RenderCore()
    {
        int goneItems = 0;
    
        List<TuiListBoxItem<TItem>> items = GetViewItems();
        for (int i = 0; i < items.Count; i++)
        {
            int index = ViewIndex + i;
            foreach (RenderPart part in items[i].Render())
            {
                yield return new RenderPart
                {
                    X = part.X + X,
                    Y = part.Y + Y + i - goneItems,
                    Text = part.Text,
                    Foreground = index == SelectedIndex && UseDefaultMarker ? Color.Black : part.Foreground,
                    Background = index == SelectedIndex && UseDefaultMarker ? Color.White : part.Background,
                };
            }
        
            goneItems += items[i].Visibility == Visibility.Gone ? 1 : 0;
        }

        if (!ShowScrollBar)
        {
            yield break;
        }

        int totalItems = GetItemCount();
        if (totalItems <= ViewRange)
        {
            yield break;
        }

        int scrollable = totalItems - ViewRange;
        if (scrollable <= 0)
        {
            yield break;
        }

        int trackHeight = Math.Max(ViewRange - 2, 1);
        int thumbHeight = Math.Max((int) Math.Round((double) ViewRange / totalItems * trackHeight), 1);
        int maxThumbStart = trackHeight - thumbHeight;
        int thumbStart = (int) Math.Round((double) ViewIndex / scrollable * maxThumbStart);

        int scrollX = X + (MaxWidth == 0 ? Width : MaxWidth) - 1;

        yield return new RenderPart
        {
            X = scrollX,
            Y = Y,
            Text = "▲",
            Foreground = ScrollBarColor,
        };

        for (int i = 0; i < trackHeight; i++)
        {
            bool isThumb = i >= thumbStart && i < thumbStart + thumbHeight;
            yield return new RenderPart
            {
                X = scrollX,
                Y = Y + i + 1,
                Text = isThumb ? "█" : "░",
                Foreground = ScrollBarColor,
            };
        }

        yield return new RenderPart
        {
            X = scrollX,
            Y = Y + ViewRange - 1,
            Text = "▼",
            Foreground = ScrollBarColor,
        };
    }
    
    public void AddItem(TuiListBoxItem<TItem> item)
    {
        lock (Lock)
        {
            _items.Add(item);
            item.Parent = this;
        }
        
        OnItemAdded?.Invoke(item);
    }

    public void AddItem(TItem value)
    {
        TuiListBoxItem<TItem> listItem = new()
        {
            Value = value,
            Renderer = ItemRenderer,
            ClickHandler = OnItemClick,
        };

        AddItem(listItem);
    }
    
    public void AddItemRange(IEnumerable<TuiListBoxItem<TItem>> items)
    {
        TuiListBoxItem<TItem>[] itemArr = items as TuiListBoxItem<TItem>[] ?? items.ToArray();
        lock (Lock)
        {
            _items.AddRange(itemArr);
            itemArr.ToList().ForEach(item => item.Parent = this);
        }
        
        itemArr.ToList().ForEach(OnItemAdded.Invoke);
    }

    public void AddItemRange(IEnumerable<TItem> values)
    {
        TItem[] valueArr = values as TItem[] ?? values.ToArray();
        TuiListBoxItem<TItem>[] itemsArr =
            valueArr.Select(item =>
                        {
                            return new TuiListBoxItem<TItem>()
                            {
                                Value = item,
                                Renderer = ItemRenderer,
                                ClickHandler = OnItemClick,
                            };
                        })
                        .ToArray();
        
        lock (Lock)
        {
            _items.AddRange(itemsArr);
            itemsArr.ToList().ForEach(item => item.Parent = this);
        }
        
        itemsArr.ToList().ForEach(OnItemAdded.Invoke);
    }

    public void InsertItem(int index, TuiListBoxItem<TItem> item)
    {
        lock (Lock)
        {
            _items.Insert(index, item);
            item.Parent = this;
        }
        
        OnItemAdded?.Invoke(item);
    }

    public void InsertItem(int index, TItem value)
    {
        TuiListBoxItem<TItem> item = new()
        {
            Value = value,
            Renderer = ItemRenderer,
            ClickHandler = OnItemClick,
        };
        
        lock (Lock)
        {
            _items.Insert(index, item);
            item.Parent = this;
        }
        
        OnItemAdded?.Invoke(item);
    }
    
    public bool RemoveItem(TuiListBoxItem<TItem> item)
    {
        bool success;
        lock (Lock)
        {
            success = _items.Remove(item);
            item.Parent = null;
        }
        
        OnItemRemoved?.Invoke(item);
        return success;
    }

    public bool RemoveItem(TItem value)
    {
        bool success;
        lock (Lock)
        {
            TuiListBoxItem<TItem>? item = _items.FirstOrDefault(item => item.Value == value);
            if (item is null)
            {
                return false;
            }
            
            success = _items.Remove(item);
            item.Parent = null;
            
            OnItemRemoved?.Invoke(item);
        }
        
        return success;
    }

    public void RemoveItemRange(IEnumerable<TuiListBoxItem<TItem>> items)
    {
        TuiListBoxItem<TItem>[] itemArr = items as TuiListBoxItem<TItem>[] ?? items.ToArray();
        foreach (TuiListBoxItem<TItem> item in itemArr)
        {
            RemoveItem(item);
        }
    }

    public void RemoveItemRange(IEnumerable<TItem> values)
    {
        TItem[] valueArr = values as TItem[] ?? values.ToArray();
        TuiListBoxItem<TItem>[] itemsArr =
            valueArr.Select(item =>
            {
                return new TuiListBoxItem<TItem>()
                {
                    Value = item,
                    Renderer = ItemRenderer,
                    ClickHandler = OnItemClick,
                };
            })
            .ToArray();
        
        foreach (TuiListBoxItem<TItem> item in itemsArr)
        {
            RemoveItem(item);
        }
    }
    
    public void RemoveItemAt(int index)
    {
        lock (Lock)
        {
            OnItemRemoved?.Invoke(_items[index]);
            _items[index].Parent = null;
            _items.RemoveAt(index);
        }
    }

    public TuiListBoxItem<TItem> GetItemAt(int index)
    {
        lock (Lock)
        {
            if (index < 0 || index >= _items.Count)
            {
                throw new IndexOutOfRangeException();
            }

            return _items[index];
        }
    }
    
    public TItem? GetItemValueAt(int index)
    {
        return GetItemAt(index)?.Value ?? null;
    }
    
    public bool TryGetItemAt(int index, out TuiListBoxItem<TItem>? item)
    {
        try
        {
            item = GetItemAt(index);
            return true;
        }
        catch (IndexOutOfRangeException)
        {
            item = null;
            return false;
        }
    }

    public bool TryGetItemValueAt(int index, out TItem? value)
    {
        TuiListBoxItem<TItem>? item = GetItemAt(index);
        if (item is null)
        {
            value = null;
            return false;
        }

        value = item.Value;
        return true;
    }

    public List<TuiListBoxItem<TItem>> GetViewItems()
    {
        lock (Lock)
        {
            if (_items.Count == 0)
            {
                return [];
            }

            int viewRange = MaxHeight;
            if (viewRange <= 0)
            {
                return [];
            }
            
            return _items.GetRange(ViewIndex, ViewIndex + viewRange > _items.Count ? Math.Max(_items.Count - ViewIndex, 1) : viewRange);
        }
    }

    public List<TItem?> GetViewItemValues()
    {
        return GetViewItems().Select(item => item.Value)
                             .ToList();
    }

    public int IndexOf(TuiListBoxItem<TItem> item)
    {
        lock (Lock)
        {
            return _items.IndexOf(item);
        }
    }

    public int IndexOf(TItem value)
    {
        lock (Lock)
        {
            TuiListBoxItem<TItem>? item = _items.FirstOrDefault(item => item.Value == value);
            return item is null ? -1 : _items.IndexOf(item);
        }
    }

    public List<TuiListBoxItem<TItem>> GetItemsClone()
    {
        lock (Lock)
        {
            return new(_items);
        }
    }

    public int GetItemCount()
    {
        lock (Lock)
        {
            return _items.Count;
        }
    }

    public void ClearItems()
    {
        lock (Lock)
        {
            _items.ForEach(item => item.Parent = null);
            _items.Clear();
        }
    }

    public void MoveSelected(int dir)
    {
        dir = Math.Sign(dir);
        if (_items.Count == 0)
        {
            SelectedIndex = -1;
            return;
        }
        
        SelectedIndex = (SelectedIndex + dir + _items.Count) % _items.Count;
        
        if (SelectedIndex - ScrollOvershoot < ViewIndex)
        {
            ViewIndex = Math.Max(SelectedIndex - ScrollOvershoot, 0);
        }
        else if (SelectedIndex + ScrollOvershoot + 1 > ViewIndex + ViewRange)
        {
            ViewIndex = Math.Max(Math.Min(SelectedIndex - ViewRange + 1 + ScrollOvershoot, _items.Count - ViewRange), 0);
        }
    }

    public bool Scroll(int amount)
    {
        int prevIndex = ViewIndex;
        ViewIndex = Math.Min(Math.Max(ViewIndex + amount, 0), GetItemCount() - ViewRange);
        
        return ViewIndex != prevIndex;
    }

    public bool TryScrollTo(int index, ScrollOption scrollOption = ScrollOption.Centered)
    {
        if (index < 0 || index >= _items.Count)
        {
            return false;
        }

        if (scrollOption == ScrollOption.Top)
        {
            ViewIndex = index;
        }

        if (scrollOption == ScrollOption.Bottom)
        {
            ViewIndex = index - (_viewRange ?? 0);
        }

        if (scrollOption == ScrollOption.Centered)
        {
            ViewIndex = index - (_viewRange ?? 0) / 2;
        }

        return true;
    }
    
    public bool TryScrollTo(TuiListBoxItem<TItem> item, ScrollOption scrollOption = ScrollOption.Centered)
    {
        int index = IndexOf(item);
        return TryScrollTo(index, scrollOption);
    }

    public bool CallSelectedItemClick()
    {
        if (SelectedItem == null)
        {
            return false;
        }
        
        lock (Lock)
        {
            return SelectedItem.CallOnClick();
        }
    }
    
    private void OnItemsSourceChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                AddItemRange(args.NewItems!.Cast<TuiListBoxItem<TItem>>());
            break;

            case NotifyCollectionChangedAction.Remove:
                RemoveItemRange(args.OldItems!.Cast<TuiListBoxItem<TItem>>());
            break;

            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
                lock (Lock)
                {
                    TuiListBoxItem<TItem>[] newItems = args.NewItems.Cast<TuiListBoxItem<TItem>>().ToArray();
                    foreach (TuiListBoxItem<TItem> item in args.OldItems.Cast<TuiListBoxItem<TItem>>())
                    {
                        int index = _items.IndexOf(item);
                        _items[index] = newItems[index];
                    }
                }
            break;

            case NotifyCollectionChangedAction.Reset:
                ClearItems();
            break;
        }
    }

    public void Sort(Action<SortChain<TItem>> buildChain)
    {
        SortChain<TItem> chain = new();
        buildChain(chain);

        lock (Lock)
        {
            _items.Sort((a, b) =>
            {
                if (a.Value is null && b.Value is null) return 0;
                else if (a.Value is null) return -1;
                else if (b.Value is null) return 1;

                return chain.Compare(a.Value, b.Value);
            });
        }

        MenuUpdate?.Invoke();
    }
}

public class TuiListBox : TuiListBox<Control>
{
    
}
