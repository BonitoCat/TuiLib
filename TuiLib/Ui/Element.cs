using System;
using System.Collections.Generic;
using TuiLib.Rendering;
using TuiLib.Ui.Input;
using TuiLib.Ui.Rendering;

namespace TuiLib.Ui;

public class Element : TuiObject
{
    public static readonly TuiProperty<int?> WidthProperty = TuiProperty.Register<Element, int?>(nameof(Width), null, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<int?> HeightProperty = TuiProperty.Register<Element, int?>(nameof(Height), null, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<int> MinWidthProperty = TuiProperty.Register<Element, int>(nameof(MinWidth), 0, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<int> MinHeightProperty = TuiProperty.Register<Element, int>(nameof(MinHeight), 0, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<int> MaxWidthProperty = TuiProperty.Register<Element, int>(nameof(MaxWidth), int.MaxValue, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<int> MaxHeightProperty = TuiProperty.Register<Element, int>(nameof(MaxHeight), int.MaxValue, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<Thickness> MarginProperty = TuiProperty.Register<Element, Thickness>(nameof(Margin), default, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<HorizontalAlignment> HorizontalAlignmentProperty = TuiProperty.Register<Element, HorizontalAlignment>(nameof(HorizontalAlignment), HorizontalAlignment.Stretch, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<VerticalAlignment> VerticalAlignmentProperty = TuiProperty.Register<Element, VerticalAlignment>(nameof(VerticalAlignment), VerticalAlignment.Stretch, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<bool> IsVisibleProperty = TuiProperty.Register<Element, bool>(nameof(IsVisible), true, PropertyFlags.AffectsMeasure);
    public static readonly TuiProperty<bool> IsEnabledProperty = TuiProperty.Register<Element, bool>(nameof(IsEnabled), true, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<bool> IsHitTestVisibleProperty = TuiProperty.Register<Element, bool>(nameof(IsHitTestVisible), true);
    public static readonly TuiProperty<bool> FocusableProperty = TuiProperty.Register<Element, bool>(nameof(Focusable), false);
    public static readonly TuiProperty<bool> IsFocusedProperty = TuiProperty.Register<Element, bool>(nameof(IsFocused), false, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<bool> IsPointerOverProperty = TuiProperty.Register<Element, bool>(nameof(IsPointerOver), false, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<Color?> ForegroundProperty = TuiProperty.Register<Element, Color?>(nameof(Foreground), null, PropertyFlags.Inherits | PropertyFlags.AffectsRender);
    public static readonly TuiProperty<Color?> BackgroundProperty = TuiProperty.Register<Element, Color?>(nameof(Background), null, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<FontStyles> FontStylesProperty = TuiProperty.Register<Element, FontStyles>(nameof(FontStyles), FontStyles.None, PropertyFlags.Inherits | PropertyFlags.AffectsRender);
    public static readonly TuiProperty<object?> DataContextProperty = TuiProperty.Register<Element, object?>(nameof(DataContext), null, PropertyFlags.Inherits);
    public static readonly TuiProperty<string?> NameProperty = TuiProperty.Register<Element, string?>(nameof(Name), null);
    public static readonly TuiProperty<object?> TagProperty = TuiProperty.Register<Element, object?>(nameof(Tag), null);

    private readonly List<Element> _visualChildren = new();
    private bool _focusWhenAttached;

    public event EventHandler<KeyEventArgs>? KeyDown;
    public event EventHandler<TextInputEventArgs>? TextInput;
    public event EventHandler<PointerEventArgs>? PointerPressed;
    public event EventHandler<PointerEventArgs>? PointerReleased;
    public event EventHandler<PointerEventArgs>? PointerMoved;
    public event EventHandler<PointerEventArgs>? PointerWheelChanged;
    public event EventHandler<PointerEventArgs>? PointerEntered;
    public event EventHandler<PointerEventArgs>? PointerExited;
    public event EventHandler<TuiEventArgs>? GotFocus;
    public event EventHandler<TuiEventArgs>? LostFocus;
    public event EventHandler<TuiEventArgs>? AttachedToVisualTree;
    public event EventHandler<TuiEventArgs>? DetachedFromVisualTree;

    public int? Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    public int? Height
    {
        get => GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    public int MinWidth
    {
        get => GetValue(MinWidthProperty);
        set => SetValue(MinWidthProperty, value);
    }

    public int MinHeight
    {
        get => GetValue(MinHeightProperty);
        set => SetValue(MinHeightProperty, value);
    }

    public int MaxWidth
    {
        get => GetValue(MaxWidthProperty);
        set => SetValue(MaxWidthProperty, value);
    }

    public int MaxHeight
    {
        get => GetValue(MaxHeightProperty);
        set => SetValue(MaxHeightProperty, value);
    }

    public Thickness Margin
    {
        get => GetValue(MarginProperty);
        set => SetValue(MarginProperty, value);
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => GetValue(HorizontalAlignmentProperty);
        set => SetValue(HorizontalAlignmentProperty, value);
    }

    public VerticalAlignment VerticalAlignment
    {
        get => GetValue(VerticalAlignmentProperty);
        set => SetValue(VerticalAlignmentProperty, value);
    }

    public bool IsVisible
    {
        get => GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    public bool IsEnabled
    {
        get => GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    public bool IsHitTestVisible
    {
        get => GetValue(IsHitTestVisibleProperty);
        set => SetValue(IsHitTestVisibleProperty, value);
    }

    public bool Focusable
    {
        get => GetValue(FocusableProperty);
        set => SetValue(FocusableProperty, value);
    }

    public bool IsFocused => GetValue(IsFocusedProperty);

    public bool IsPointerOver => GetValue(IsPointerOverProperty);

    public Color? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public Color? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public FontStyles FontStyles
    {
        get => GetValue(FontStylesProperty);
        set => SetValue(FontStylesProperty, value);
    }

    public object? DataContext
    {
        get => GetValue(DataContextProperty);
        set => SetValue(DataContextProperty, value);
    }

    public string? Name
    {
        get => GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    public object? Tag
    {
        get => GetValue(TagProperty);
        set => SetValue(TagProperty, value);
    }

    public ResourceDictionary Resources { get; } = new();

    public Element? Parent { get; private set; }
    public Size DesiredSize { get; private set; }
    public Rect Bounds { get; private set; }
    public IReadOnlyList<Element> VisualChildren => _visualChildren;

    public bool IsEffectivelyEnabled => IsEnabled && IsEnabledCore && (Parent?.IsEffectivelyEnabled ?? true);

    public bool IsEffectivelyVisible => IsVisible && (Parent?.IsEffectivelyVisible ?? true);

    public IRenderRoot? Root
    {
        get
        {
            for (Element? current = this; current != null; current = current.Parent)
            {
                if (current is IRenderRoot root)
                {
                    return root;
                }
            }

            return null;
        }
    }

    protected virtual bool IsEnabledCore => true;

    protected Color? EffectiveForeground => IsEffectivelyEnabled ? Foreground : Theme.DisabledForeground;

    protected override TuiObject? InheritanceParent => Parent;

    public void Measure(Size availableSize)
    {
        if (!IsVisible)
        {
            DesiredSize = Size.Empty;
            return;
        }

        Thickness margin = Margin;
        Size inner = availableSize.Deflate(margin);
        Size constraint = new(
            Constrain(inner.Width, Width, MinWidth, MaxWidth),
            Constrain(inner.Height, Height, MinHeight, MaxHeight));
        Size measured = MeasureOverride(constraint);

        int width = Layout.Clamp(Width ?? measured.Width, MinWidth, MaxWidth);
        int height = Layout.Clamp(Height ?? measured.Height, MinHeight, MaxHeight);
        Size desired = new Size(width, height).Inflate(margin);
        DesiredSize = new(Math.Min(desired.Width, availableSize.Width), Math.Min(desired.Height, availableSize.Height));
    }

    public void Arrange(Rect finalRect)
    {
        if (!IsVisible)
        {
            Bounds = new(finalRect.X, finalRect.Y, 0, 0);
            return;
        }

        Rect inner = finalRect.Deflate(Margin);
        Size desired = DesiredSize.Deflate(Margin);
        HorizontalAlignment horizontal = HorizontalAlignment;
        VerticalAlignment vertical = VerticalAlignment;

        int width = horizontal == HorizontalAlignment.Stretch && Width == null ? inner.Width : Math.Min(desired.Width, inner.Width);
        int height = vertical == VerticalAlignment.Stretch && Height == null ? inner.Height : Math.Min(desired.Height, inner.Height);
        width = Math.Min(Layout.Clamp(width, MinWidth, MaxWidth), inner.Width);
        height = Math.Min(Layout.Clamp(height, MinHeight, MaxHeight), inner.Height);

        Bounds = Layout.Align(inner, new(width, height), horizontal, vertical);
        ArrangeOverride(Bounds);
    }

    protected virtual Size MeasureOverride(Size availableSize)
    {
        int width = 0;
        int height = 0;
        foreach (Element child in _visualChildren)
        {
            child.Measure(availableSize);
            width = Math.Max(width, child.DesiredSize.Width);
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new(width, height);
    }

    protected virtual void ArrangeOverride(Rect bounds)
    {
        foreach (Element child in _visualChildren)
        {
            child.Arrange(bounds);
        }
    }

    public void Render(DrawingContext context)
    {
        if (!IsVisible || Bounds.IsEmpty || context.Clip.Intersect(Bounds).IsEmpty)
        {
            return;
        }

        context.PushClip(Bounds);
        try
        {
            Color? background = GetRenderBackground();
            if (background is not null)
            {
                context.FillRect(Bounds, background);
            }

            OnRender(context);
            RenderChildren(context);
        }
        finally
        {
            context.PopClip();
        }
    }

    protected virtual Color? GetRenderBackground() => Background;

    protected virtual void OnRender(DrawingContext context)
    {
    }

    protected virtual void RenderChildren(DrawingContext context)
    {
        foreach (Element child in _visualChildren)
        {
            child.Render(context);
        }
    }

    public Element? HitTest(Point point)
    {
        if (!IsVisible || !IsHitTestVisible || !Bounds.Contains(point))
        {
            return null;
        }

        for (int i = _visualChildren.Count - 1; i >= 0; i--)
        {
            Element? hit = _visualChildren[i].HitTest(point);
            if (hit != null)
            {
                return hit;
            }
        }

        return this;
    }

    public T? FindControl<T>(string name) where T : Element
    {
        foreach (Element element in GetSelfAndDescendants())
        {
            if (element is T match && element.Name == name)
            {
                return match;
            }
        }

        return null;
    }

    public bool TryFindResource(string key, out object? value)
    {
        for (Element? current = this; current != null; current = current.Parent)
        {
            if (current.Resources.TryGetValue(key, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }

    public T? FindAncestor<T>() where T : Element
    {
        for (Element? current = Parent; current != null; current = current.Parent)
        {
            if (current is T match)
            {
                return match;
            }
        }

        return null;
    }

    public IEnumerable<Element> GetSelfAndDescendants()
    {
        Stack<Element> stack = new();
        stack.Push(this);
        while (stack.Count > 0)
        {
            Element current = stack.Pop();
            yield return current;
            for (int i = current._visualChildren.Count - 1; i >= 0; i--)
            {
                stack.Push(current._visualChildren[i]);
            }
        }
    }

    public bool Focus()
    {
        IRenderRoot? root = Root;
        if (root == null)
        {
            _focusWhenAttached = true;
            return false;
        }

        return root.FocusManager.Focus(this);
    }

    public void InvalidateMeasure()
    {
        Root?.RequestFrame(true);
    }

    public void InvalidateVisual()
    {
        Root?.RequestFrame(false);
    }

    protected internal void AttachChild(Element child, int index = -1)
    {
        if (child.Parent != null)
        {
            throw new InvalidOperationException($"{child.GetType().Name} already has a parent.");
        }

        child.Parent = this;
        if (index < 0 || index >= _visualChildren.Count)
        {
            _visualChildren.Add(child);
        }
        else
        {
            _visualChildren.Insert(index, child);
        }

        child.NotifyAttached();
        InvalidateMeasure();
    }

    protected internal void DetachChild(Element child)
    {
        if (!_visualChildren.Remove(child))
        {
            return;
        }

        IRenderRoot? root = Root;
        child.Parent = null;
        child.NotifyDetached();
        root?.FocusManager.Validate();
        root?.RequestFrame(true);
    }

    protected internal void DetachAllChildren()
    {
        for (int i = _visualChildren.Count - 1; i >= 0; i--)
        {
            DetachChild(_visualChildren[i]);
        }
    }

    protected virtual void OnAttachedToVisualTree()
    {
    }

    protected virtual void OnDetachedFromVisualTree()
    {
    }

    protected virtual void OnKeyDown(KeyEventArgs e)
    {
    }

    protected virtual void OnTextInput(TextInputEventArgs e)
    {
    }

    protected virtual void OnPointerPressed(PointerEventArgs e)
    {
    }

    protected virtual void OnPointerReleased(PointerEventArgs e)
    {
    }

    protected virtual void OnPointerMoved(PointerEventArgs e)
    {
    }

    protected virtual void OnPointerWheelChanged(PointerEventArgs e)
    {
    }

    protected virtual void OnPointerEntered(PointerEventArgs e)
    {
    }

    protected virtual void OnPointerExited(PointerEventArgs e)
    {
    }

    protected virtual void OnGotFocus(TuiEventArgs e)
    {
    }

    protected virtual void OnLostFocus(TuiEventArgs e)
    {
    }

    protected override void OnPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property.AffectsMeasure)
        {
            InvalidateMeasure();
        }
        else if (e.Property.AffectsRender)
        {
            InvalidateVisual();
        }

        if (e.Property == DataContextProperty)
        {
            RefreshBindings();
        }

        if (e.Property.Inherits)
        {
            foreach (Element child in _visualChildren.ToArray())
            {
                child.OnParentInheritedPropertyChanged(e);
            }
        }

        if (e.Property == IsVisibleProperty || e.Property == IsEnabledProperty)
        {
            Root?.FocusManager.Validate();
        }
    }

    internal void RaiseKeyDown(KeyEventArgs e)
    {
        OnKeyDown(e);
        if (!e.Handled)
        {
            KeyDown?.Invoke(this, e);
        }
    }

    internal void RaiseTextInput(TextInputEventArgs e)
    {
        OnTextInput(e);
        if (!e.Handled)
        {
            TextInput?.Invoke(this, e);
        }
    }

    internal void RaisePointerPressed(PointerEventArgs e)
    {
        OnPointerPressed(e);
        if (!e.Handled)
        {
            PointerPressed?.Invoke(this, e);
        }
    }

    internal void RaisePointerReleased(PointerEventArgs e)
    {
        OnPointerReleased(e);
        if (!e.Handled)
        {
            PointerReleased?.Invoke(this, e);
        }
    }

    internal void RaisePointerMoved(PointerEventArgs e)
    {
        OnPointerMoved(e);
        if (!e.Handled)
        {
            PointerMoved?.Invoke(this, e);
        }
    }

    internal void RaisePointerWheelChanged(PointerEventArgs e)
    {
        OnPointerWheelChanged(e);
        if (!e.Handled)
        {
            PointerWheelChanged?.Invoke(this, e);
        }
    }

    internal void SetPointerOver(bool isOver, PointerEventArgs e)
    {
        if (IsPointerOver == isOver)
        {
            return;
        }

        SetValue(IsPointerOverProperty, isOver);
        if (isOver)
        {
            OnPointerEntered(e);
            PointerEntered?.Invoke(this, e);
            return;
        }

        OnPointerExited(e);
        PointerExited?.Invoke(this, e);
    }

    internal void SetFocused(bool isFocused)
    {
        SetValue(IsFocusedProperty, isFocused);
        TuiEventArgs args = new();
        if (isFocused)
        {
            OnGotFocus(args);
            GotFocus?.Invoke(this, args);
            return;
        }

        OnLostFocus(args);
        LostFocus?.Invoke(this, args);
    }

    private void OnParentInheritedPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        if (!IsSet(e.Property))
        {
            OnPropertyChanged(e);
            return;
        }

        if (e.Property == DataContextProperty)
        {
            GetBindingExpression(DataContextProperty)?.Refresh();
        }
    }

    private void NotifyAttached()
    {
        RefreshBindings();
        OnAttachedToVisualTree();
        AttachedToVisualTree?.Invoke(this, new());

        foreach (Element child in _visualChildren.ToArray())
        {
            child.NotifyAttached();
        }

        if (_focusWhenAttached && Root != null)
        {
            _focusWhenAttached = false;
            Focus();
        }
    }

    private void NotifyDetached()
    {
        if (IsPointerOver)
        {
            SetValue(IsPointerOverProperty, false);
        }

        OnDetachedFromVisualTree();
        DetachedFromVisualTree?.Invoke(this, new());

        foreach (Element child in _visualChildren.ToArray())
        {
            child.NotifyDetached();
        }
    }

    private static int Constrain(int available, int? explicitValue, int min, int max)
    {
        int value = explicitValue ?? available;
        return Layout.Clamp(value, min, Math.Min(max, Layout.Infinite));
    }
}
