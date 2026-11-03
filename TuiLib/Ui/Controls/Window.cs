using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InputLib;
using TuiLib.Ui.Input;
using TuiLib.Ui.Rendering;

namespace TuiLib.Ui.Controls;

public class Window : ContentControl, IRenderRoot
{
    public static readonly TuiProperty<string?> TitleProperty = TuiProperty.Register<Window, string?>(nameof(Title), null);

    private readonly List<DialogEntry> _dialogs = new();
    private readonly List<Element> _hoverChain = new();
    private Element? _pointerCapture;
    private bool _needsLayout = true;
    private bool _needsRender = true;
    private int _lastWidth = -1;
    private int _lastHeight = -1;

    public event EventHandler<TuiEventArgs>? Opened;
    public event EventHandler<TuiEventArgs>? Closed;
    public event EventHandler<TuiEventArgs>? TitleChanged;

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public FocusManager FocusManager { get; }

    public bool NeedsFrame => _needsLayout || _needsRender;

    public bool IsDialogOpen => _dialogs.Count > 0;

    public Window()
    {
        FocusManager = new(this);
    }

    public void RequestFrame(bool layout)
    {
        if (layout)
        {
            _needsLayout = true;
        }

        _needsRender = true;
        Dispatcher.Wake();
    }

    public CellBuffer RenderToBuffer(int width, int height)
    {
        return RenderFrame(width, height, out _);
    }

    public CellBuffer RenderFrame(int width, int height, out Point? cursor)
    {
        for (int pass = 0; pass < 3 && (_needsLayout || width != _lastWidth || height != _lastHeight); pass++)
        {
            _needsLayout = false;
            _lastWidth = width;
            _lastHeight = height;
            Measure(new(width, height));
            Arrange(new(0, 0, width, height));
        }

        CellBuffer buffer = new(width, height);
        DrawingContext context = new(buffer);
        _needsRender = false;
        Render(context);
        cursor = context.CursorPosition;
        return buffer;
    }

    public void Show()
    {
        FocusManager.FocusFirst();
        Opened?.Invoke(this, new());
        RequestFrame(true);
    }

    public void Close()
    {
        Closed?.Invoke(this, new());
        TuiApplication.Current?.OnWindowClosed(this);
    }

    public Task<object?> ShowDialog(Element dialog)
    {
        DialogEntry entry = new(dialog, FocusManager.FocusedElement);
        _dialogs.Add(entry);
        AttachChild(dialog);
        FocusManager.Scope = dialog;
        FocusManager.Focus(null);
        FocusManager.FocusFirst();
        return entry.Completion.Task;
    }

    public void CloseDialog(object? result = null)
    {
        if (_dialogs.Count == 0)
        {
            return;
        }

        CloseDialog(_dialogs[_dialogs.Count - 1].Dialog, result);
    }

    public void CloseDialog(Element dialog, object? result = null)
    {
        DialogEntry? entry = _dialogs.FirstOrDefault(candidate => ReferenceEquals(candidate.Dialog, dialog));
        if (entry == null)
        {
            return;
        }

        _dialogs.Remove(entry);
        DetachChild(dialog);
        FocusManager.Scope = _dialogs.Count == 0 ? this : _dialogs[_dialogs.Count - 1].Dialog;
        if (entry.PreviousFocus != null && FocusManager.CanFocus(entry.PreviousFocus))
        {
            FocusManager.Focus(entry.PreviousFocus);
        }
        else
        {
            FocusManager.FocusFirst();
        }

        entry.Completion.TrySetResult(result);
    }

    public void ProcessKey(KeyEventArgs e)
    {
        Element target = FocusManager.FocusedElement ?? FocusManager.Scope;
        for (Element? current = target; current != null && !e.Handled; current = current.Parent)
        {
            current.RaiseKeyDown(e);
        }

        if (e.Handled)
        {
            return;
        }

        if (e.Key == TuiKey.Tab)
        {
            FocusManager.MoveFocus(e.Shift);
            return;
        }

        if (e.Key is TuiKey.Up or TuiKey.Left && e.Modifiers == KeyModifiers.None)
        {
            FocusManager.MoveFocus(true);
            return;
        }

        if (e.Key is TuiKey.Down or TuiKey.Right && e.Modifiers == KeyModifiers.None)
        {
            FocusManager.MoveFocus(false);
            return;
        }

        if (e.Key == TuiKey.Escape && _dialogs.Count > 0)
        {
            CloseDialog();
            return;
        }

        if (e.Key == TuiKey.Character && e.Ctrl && e.IsCharacter('c') && (TuiApplication.Current?.ExitOnCtrlC ?? false))
        {
            TuiApplication.Current?.Shutdown();
            return;
        }

        if (e.Key == TuiKey.Character && !e.Ctrl && !e.Alt && e.Text.Length > 0)
        {
            ProcessText(e.Text);
        }
    }

    public void ProcessText(string text)
    {
        TextInputEventArgs e = new(text);
        Element target = FocusManager.FocusedElement ?? FocusManager.Scope;
        for (Element? current = target; current != null && !e.Handled; current = current.Parent)
        {
            current.RaiseTextInput(e);
        }
    }

    public void ProcessPointer(PointerEventKind kind, PointerEventArgs e)
    {
        Element? hit = FocusManager.Scope.HitTest(e.Position);
        switch (kind)
        {
            case PointerEventKind.Moved:
                UpdatePointerOver(hit, e);
                Bubble(_pointerCapture ?? hit, e, element => element.RaisePointerMoved(e));
                break;
            case PointerEventKind.Pressed:
                UpdatePointerOver(hit, e);
                FocusFromPointer(hit);
                _pointerCapture = hit;
                Bubble(hit, e, element => element.RaisePointerPressed(e));
                break;
            case PointerEventKind.Released:
                Element? target = _pointerCapture ?? hit;
                _pointerCapture = null;
                Bubble(target, e, element => element.RaisePointerReleased(e));
                UpdatePointerOver(hit, e);
                break;
            case PointerEventKind.Wheel:
                Bubble(hit, e, element => element.RaisePointerWheelChanged(e));
                break;
        }
    }

    protected override void OnPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == TitleProperty)
        {
            TitleChanged?.Invoke(this, new());
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size size = base.MeasureOverride(availableSize);
        foreach (DialogEntry entry in _dialogs)
        {
            entry.Dialog.Measure(availableSize);
        }

        return size;
    }

    protected override void ArrangeOverride(Rect bounds)
    {
        base.ArrangeOverride(bounds);
        foreach (DialogEntry entry in _dialogs)
        {
            entry.Dialog.Arrange(bounds);
        }
    }

    private void FocusFromPointer(Element? hit)
    {
        for (Element? current = hit; current != null; current = current.Parent)
        {
            if (FocusManager.CanFocus(current))
            {
                FocusManager.Focus(current);
                return;
            }
        }
    }

    private void UpdatePointerOver(Element? hit, PointerEventArgs e)
    {
        List<Element> chain = new();
        for (Element? current = hit; current != null; current = current.Parent)
        {
            chain.Add(current);
        }

        foreach (Element element in _hoverChain)
        {
            if (!chain.Contains(element))
            {
                element.SetPointerOver(false, e);
            }
        }

        for (int i = chain.Count - 1; i >= 0; i--)
        {
            chain[i].SetPointerOver(true, e);
        }

        _hoverChain.Clear();
        _hoverChain.AddRange(chain);
    }

    private static void Bubble(Element? start, TuiEventArgs e, Action<Element> raise)
    {
        for (Element? current = start; current != null && !e.Handled; current = current.Parent)
        {
            raise(current);
        }
    }

    private sealed class DialogEntry
    {
        public Element Dialog { get; }
        public Element? PreviousFocus { get; }
        public TaskCompletionSource<object?> Completion { get; } = new();

        public DialogEntry(Element dialog, Element? previousFocus)
        {
            Dialog = dialog;
            PreviousFocus = previousFocus;
        }
    }
}
