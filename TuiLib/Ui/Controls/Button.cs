using System;
using System.Windows.Input;
using InputLib;
using TuiLib.Ui.Input;
using SystemEventArgs = System.EventArgs;

namespace TuiLib.Ui.Controls;

public class Button : ContentControl
{
    public static readonly TuiProperty<ICommand?> CommandProperty = TuiProperty.Register<Button, ICommand?>(nameof(Command), null);
    public static readonly TuiProperty<object?> CommandParameterProperty = TuiProperty.Register<Button, object?>(nameof(CommandParameter), null);
    public static readonly TuiProperty<bool> IsPressedProperty = TuiProperty.Register<Button, bool>(nameof(IsPressed), false, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<Color?> FocusedBackgroundProperty = TuiProperty.Register<Button, Color?>(nameof(FocusedBackground), null, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<Color?> HoverBackgroundProperty = TuiProperty.Register<Button, Color?>(nameof(HoverBackground), null, PropertyFlags.AffectsRender);
    public static readonly TuiProperty<Color?> PressedBackgroundProperty = TuiProperty.Register<Button, Color?>(nameof(PressedBackground), null, PropertyFlags.AffectsRender);

    private bool _canExecute = true;

    public event EventHandler<TuiEventArgs>? Click;

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool IsPressed => GetValue(IsPressedProperty);

    public Color? FocusedBackground
    {
        get => GetValue(FocusedBackgroundProperty);
        set => SetValue(FocusedBackgroundProperty, value);
    }

    public Color? HoverBackground
    {
        get => GetValue(HoverBackgroundProperty);
        set => SetValue(HoverBackgroundProperty, value);
    }

    public Color? PressedBackground
    {
        get => GetValue(PressedBackgroundProperty);
        set => SetValue(PressedBackgroundProperty, value);
    }

    protected override bool IsEnabledCore => _canExecute;

    public Button()
    {
        Focusable = true;
        Padding = new(1, 0);
        HorizontalContentAlignment = HorizontalAlignment.Center;
        VerticalContentAlignment = VerticalAlignment.Center;
    }

    public void PerformClick()
    {
        if (!IsEffectivelyEnabled)
        {
            return;
        }

        OnClick();
    }

    protected virtual void OnClick()
    {
        Click?.Invoke(this, new());

        ICommand? command = Command;
        object? parameter = CommandParameter;
        if (command != null && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }

    protected override Color? GetRenderBackground()
    {
        if (!IsEffectivelyEnabled)
        {
            return Background ?? Theme.ControlBackground;
        }

        if (IsPressed)
        {
            return PressedBackground ?? Theme.ControlPressedBackground;
        }

        if (IsFocused)
        {
            return FocusedBackground ?? Theme.Accent;
        }

        if (IsPointerOver)
        {
            return HoverBackground ?? Theme.ControlHoverBackground;
        }

        return Background ?? Theme.ControlBackground;
    }

    protected override void OnPropertyChanged(TuiPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == CommandProperty)
        {
            if (e.OldValue is ICommand oldCommand)
            {
                oldCommand.CanExecuteChanged -= OnCanExecuteChanged;
            }

            if (e.NewValue is ICommand newCommand)
            {
                newCommand.CanExecuteChanged += OnCanExecuteChanged;
            }

            UpdateCanExecute();
            return;
        }

        if (e.Property == CommandParameterProperty)
        {
            UpdateCanExecute();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key != TuiKey.Enter && !(e.Key == TuiKey.Character && e.Character == ' '))
        {
            return;
        }

        e.Handled = true;
        PerformClick();
    }

    protected override void OnPointerPressed(PointerEventArgs e)
    {
        if (e.Button != MouseButton.Left)
        {
            return;
        }

        e.Handled = true;
        SetValue(IsPressedProperty, true);
    }

    protected override void OnPointerReleased(PointerEventArgs e)
    {
        if (!IsPressed)
        {
            return;
        }

        e.Handled = true;
        SetValue(IsPressedProperty, false);
        if (Bounds.Contains(e.Position))
        {
            PerformClick();
        }
    }

    private void OnCanExecuteChanged(object? sender, SystemEventArgs e)
    {
        UpdateCanExecute();
    }

    private void UpdateCanExecute()
    {
        bool canExecute = Command?.CanExecute(CommandParameter) ?? true;
        if (canExecute == _canExecute)
        {
            return;
        }

        _canExecute = canExecute;
        InvalidateVisual();
        Root?.FocusManager.Validate();
    }
}
