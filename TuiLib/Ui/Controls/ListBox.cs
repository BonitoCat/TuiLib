using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using InputLib;
using TuiLib.Ui.Data;
using TuiLib.Ui.Input;
using TuiLib.Ui.Rendering;

namespace TuiLib.Ui.Controls;

public class ListBox : Element
{
    public static readonly TuiProperty<IEnumerable?> ItemsSourceProperty = TuiProperty.Register<ListBox, IEnumerable?>(nameof(ItemsSource), null, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<DataTemplate?> ItemTemplateProperty = TuiProperty.Register<ListBox, DataTemplate?>(nameof(ItemTemplate), null, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<int> SelectedIndexProperty = TuiProperty.Register<ListBox, int>(nameof(SelectedIndex), -1, PropertyFlags.AffectsRender, BindingMode.TwoWay);
    public static readonly TuiProperty<object?> SelectedItemProperty = TuiProperty.Register<ListBox, object?>(nameof(SelectedItem), null, PropertyFlags.AffectsRender, BindingMode.TwoWay);
    public static readonly TuiProperty<bool> ShowScrollBarProperty = TuiProperty.Register<ListBox, bool>(nameof(ShowScrollBar), true, PropertyFlags.AffectsMeasure);

    private const int UnboundedRealizeLimit = 200;
    private static readonly TimeSpan DoubleClickTime = TimeSpan.FromMilliseconds(400);

    private readonly Dictionary<int, ListBoxItem> _realized = new();
    private INotifyCollectionChanged? _observed;
    private IList _view;
    private int _scrollOffset;
    private bool _bringSelectionIntoView;
    private bool _isSyncingSelection;
    private bool _hasScrollBar;
    private int _lastClickIndex = -1;
    private int _pendingSelectedIndex = -1;
    private DateTime _lastClickTime;

    public event EventHandler<TuiEventArgs>? SelectionChanged;
    public event EventHandler<ItemActivatedEventArgs>? ItemActivated;

    [Content]
    public ItemCollection Items { get; } = new();

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public DataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public bool ShowScrollBar
    {
        get => GetValue(ShowScrollBarProperty);
        set => SetValue(ShowScrollBarProperty, value);
    }

    public int ItemCount => _view.Count;

    public int ScrollOffset
    {
        get => _scrollOffset;
        set
        {
            _scrollOffset = Layout.Clamp(value, 0, Math.Max(0, _view.Count - 1));
            InvalidateMeasure();
        }
    }

    public ListBox()
    {
        Focusable = true;
        _view = Items;
        Observe(Items);
    }

    public void ScrollIntoView(int index)
    {
        if (index < 0 || index >= _view.Count)
        {
            return;
        }

        if (index < _scrollOffset)
        {
            _scrollOffset = index;
        }

        _bringSelectionIntoView = true;
        InvalidateMeasure();
    }

    protected override void OnPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == ItemsSourceProperty)
        {
            UpdateView();
            return;
        }

        if (e.Property == ItemTemplateProperty)
        {
            ClearRealized();
            return;
        }

        if (e.Property == SelectedIndexProperty)
        {
            OnSelectedIndexChanged();
            return;
        }

        if (e.Property == SelectedItemProperty && !_isSyncingSelection)
        {
            SetSelectedIndexCore(e.NewValue == null ? -1 : _view.IndexOf(e.NewValue));
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int height = Layout.IsInfinite(availableSize.Height) ? UnboundedRealizeLimit : availableSize.Height;
        int scrollBarWidth = ShowScrollBar && _view.Count > height ? 1 : 0;
        List<ListBoxItem> containers = Realize(Layout.Subtract(availableSize.Width, scrollBarWidth), height, out int usedHeight);
        int width = containers.Count == 0 ? 0 : containers.Max(container => container.DesiredSize.Width);
        return new(Layout.Add(width, scrollBarWidth), usedHeight);
    }

    protected override void ArrangeOverride(Rect bounds)
    {
        if (_bringSelectionIntoView)
        {
            _bringSelectionIntoView = false;
            BringIndexIntoView(SelectedIndex, bounds);
        }

        _scrollOffset = Layout.Clamp(_scrollOffset, 0, Math.Max(0, _view.Count - 1));
        Realize(bounds.Width, bounds.Height, out _);
        _hasScrollBar = ShowScrollBar && (_scrollOffset > 0 || _scrollOffset + _realized.Count < _view.Count);
        int width = _hasScrollBar ? bounds.Width - 1 : bounds.Width;
        List<ListBoxItem> containers = Realize(width, bounds.Height, out _);

        int y = bounds.Y;
        foreach (ListBoxItem container in containers)
        {
            int height = Math.Max(1, container.DesiredSize.Height);
            container.Arrange(new(bounds.X, y, width, height));
            y += height;
        }
    }

    protected override void OnRender(DrawingContext context)
    {
        if (!_hasScrollBar || _view.Count == 0)
        {
            return;
        }

        Rect bounds = Bounds;
        int x = bounds.Right - 1;
        int visible = Math.Max(1, _realized.Count);
        int thumbSize = Math.Max(1, bounds.Height * visible / _view.Count);
        int maxOffset = Math.Max(1, _view.Count - visible);
        int thumbStart = (bounds.Height - thumbSize) * Math.Min(_scrollOffset, maxOffset) / maxOffset;
        for (int i = 0; i < bounds.Height; i++)
        {
            bool isThumb = i >= thumbStart && i < thumbStart + thumbSize;
            context.DrawChar(x, bounds.Y + i, isThumb ? '┃' : '│', isThumb ? Foreground : Theme.TrackBackground);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        int count = _view.Count;
        if (count == 0)
        {
            return;
        }

        int page = Math.Max(1, _realized.Count - 1);
        int index = SelectedIndex;
        switch (e.Key)
        {
            case TuiKey.Up:
                index = index <= 0 ? 0 : index - 1;
                break;
            case TuiKey.Down:
                index = Math.Min(count - 1, index + 1);
                break;
            case TuiKey.PageUp:
                index = Math.Max(0, index - page);
                break;
            case TuiKey.PageDown:
                index = Math.Min(count - 1, Math.Max(0, index) + page);
                break;
            case TuiKey.Home:
                index = 0;
                break;
            case TuiKey.End:
                index = count - 1;
                break;
            case TuiKey.Enter when index >= 0:
                e.Handled = true;
                Activate(index);
                return;
            default:
                return;
        }

        e.Handled = true;
        SelectedIndex = index;
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != MouseButton.Left)
        {
            return;
        }

        e.Handled = true;
        if (_hasScrollBar && e.Position.X == Bounds.Right - 1)
        {
            int maxOffset = Math.Max(0, _view.Count - _realized.Count);
            ScrollOffset = Bounds.Height <= 1 ? 0 : maxOffset * (e.Position.Y - Bounds.Y) / (Bounds.Height - 1);
            return;
        }

        foreach (KeyValuePair<int, ListBoxItem> entry in _realized)
        {
            if (!entry.Value.Bounds.Contains(e.Position))
            {
                continue;
            }

            DateTime now = DateTime.UtcNow;
            bool isDoubleClick = entry.Key == _lastClickIndex && now - _lastClickTime <= DoubleClickTime;
            _lastClickIndex = entry.Key;
            _lastClickTime = now;
            SelectedIndex = entry.Key;
            if (isDoubleClick)
            {
                Activate(entry.Key);
            }

            return;
        }
    }

    protected override void OnPointerWheelChanged(PointerEventArgs e)
    {
        e.Handled = true;
        int maxOffset = Math.Max(0, _view.Count - _realized.Count);
        _scrollOffset = Layout.Clamp(_scrollOffset + e.WheelDelta * 3, 0, maxOffset);
        InvalidateMeasure();
    }

    protected override void OnGotFocus(TuiEventArgs e)
    {
        if (SelectedIndex < 0 && _view.Count > 0)
        {
            SelectedIndex = 0;
        }
    }

    protected virtual void OnItemActivated(ItemActivatedEventArgs e)
    {
        ItemActivated?.Invoke(this, e);
    }

    private void Activate(int index)
    {
        if (index < 0 || index >= _view.Count)
        {
            return;
        }

        OnItemActivated(new(_view[index], index));
    }

    private List<ListBoxItem> Realize(int width, int height, out int usedHeight)
    {
        List<ListBoxItem> containers = new();
        usedHeight = 0;
        int index = _scrollOffset;
        while (index < _view.Count && usedHeight < height)
        {
            ListBoxItem container = GetOrCreateContainer(index);
            container.Measure(new(width, Layout.Infinite));
            containers.Add(container);
            usedHeight += Math.Max(1, container.DesiredSize.Height);
            index++;
        }

        int last = index;
        foreach (int realizedIndex in _realized.Keys.ToList())
        {
            if (realizedIndex >= _scrollOffset && realizedIndex < last)
            {
                continue;
            }

            ReleaseContainer(_realized[realizedIndex]);
            _realized.Remove(realizedIndex);
        }

        return containers;
    }

    private void BringIndexIntoView(int index, Rect bounds)
    {
        if (index < 0 || index >= _view.Count)
        {
            return;
        }

        if (index < _scrollOffset)
        {
            _scrollOffset = index;
            return;
        }

        int width = ShowScrollBar ? bounds.Width - 1 : bounds.Width;
        int used = 0;
        for (int i = _scrollOffset; i <= index; i++)
        {
            used += MeasureContainerHeight(i, width);
        }

        if (used <= bounds.Height)
        {
            return;
        }

        int offset = index;
        int total = MeasureContainerHeight(index, width);
        while (offset > 0)
        {
            int previous = MeasureContainerHeight(offset - 1, width);
            if (total + previous > bounds.Height)
            {
                break;
            }

            total += previous;
            offset--;
        }

        _scrollOffset = offset;
    }

    private int MeasureContainerHeight(int index, int width)
    {
        ListBoxItem container = GetOrCreateContainer(index);
        container.Measure(new(width, Layout.Infinite));
        return Math.Max(1, container.DesiredSize.Height);
    }

    private ListBoxItem GetOrCreateContainer(int index)
    {
        if (_realized.TryGetValue(index, out ListBoxItem? existing))
        {
            return existing;
        }

        object? item = _view[index];
        ListBoxItem container = item as ListBoxItem ?? new GeneratedListBoxItem
        {
            ContentTemplate = ItemTemplate,
            Content = item,
        };

        if (item is not Element)
        {
            container.DataContext = item;
        }

        container.IsSelected = index == SelectedIndex;
        _realized[index] = container;
        AttachChild(container);
        return container;
    }

    private void ClearRealized()
    {
        foreach (ListBoxItem container in _realized.Values.ToList())
        {
            ReleaseContainer(container);
        }

        _realized.Clear();
        InvalidateMeasure();
    }

    private void ReleaseContainer(ListBoxItem container)
    {
        DetachChild(container);
        if (container is GeneratedListBoxItem && container.Content is Element)
        {
            container.Content = null;
        }
    }

    private void UpdateView()
    {
        if (_observed != null)
        {
            _observed.CollectionChanged -= OnCollectionChanged;
            _observed = null;
        }

        IEnumerable source = ItemsSource ?? Items;
        _view = source as IList ?? source.Cast<object?>().ToList();
        Observe(source);
        OnItemsChanged();
    }

    private void Observe(IEnumerable source)
    {
        if (source is not INotifyCollectionChanged observable)
        {
            return;
        }

        _observed = observable;
        observable.CollectionChanged += OnCollectionChanged;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (ItemsSource != null && ItemsSource is not IList)
            {
                _view = ItemsSource.Cast<object?>().ToList();
            }

            OnItemsChanged();
        });
    }

    private void OnItemsChanged()
    {
        ClearRealized();
        object? selected = SelectedItem;
        int index = selected == null ? -1 : _view.IndexOf(selected);
        if (index < 0 && SelectedIndex >= 0 && _view.Count > 0)
        {
            index = Math.Min(SelectedIndex, _view.Count - 1);
        }

        if (index < 0 && _pendingSelectedIndex >= 0 && _pendingSelectedIndex < _view.Count)
        {
            index = _pendingSelectedIndex;
        }

        SetSelectedIndexCore(index);
        _scrollOffset = Layout.Clamp(_scrollOffset, 0, Math.Max(0, _view.Count - 1));
    }

    private void SetSelectedIndexCore(int index)
    {
        if (index == SelectedIndex)
        {
            SyncSelectedItem();
            return;
        }

        SelectedIndex = index;
    }

    private void OnSelectedIndexChanged()
    {
        int index = SelectedIndex;
        if (index < -1 || index >= _view.Count)
        {
            _pendingSelectedIndex = index;
            SelectedIndex = _view.Count == 0 ? -1 : Layout.Clamp(index, -1, _view.Count - 1);
            return;
        }

        if (index >= 0)
        {
            _pendingSelectedIndex = -1;
        }

        foreach (KeyValuePair<int, ListBoxItem> entry in _realized)
        {
            entry.Value.IsSelected = entry.Key == index;
        }

        SyncSelectedItem();
        _bringSelectionIntoView = true;
        InvalidateMeasure();
        SelectionChanged?.Invoke(this, new());
    }

    private void SyncSelectedItem()
    {
        int index = SelectedIndex;
        _isSyncingSelection = true;
        try
        {
            SelectedItem = index >= 0 && index < _view.Count ? _view[index] : null;
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private sealed class GeneratedListBoxItem : ListBoxItem
    {
    }
}
