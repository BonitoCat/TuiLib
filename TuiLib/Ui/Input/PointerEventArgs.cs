using InputLib;

namespace TuiLib.Ui.Input;

public sealed class PointerEventArgs : TuiEventArgs
{
    public Point Position { get; }
    public MouseButton Button { get; }
    public KeyModifiers Modifiers { get; }
    public int WheelDelta { get; }

    public PointerEventArgs(Point position, MouseButton button, KeyModifiers modifiers = KeyModifiers.None, int wheelDelta = 0)
    {
        Position = position;
        Button = button;
        Modifiers = modifiers;
        WheelDelta = wheelDelta;
    }

    public Point GetPosition(Element relativeTo)
    {
        return new(Position.X - relativeTo.Bounds.X, Position.Y - relativeTo.Bounds.Y);
    }
}
