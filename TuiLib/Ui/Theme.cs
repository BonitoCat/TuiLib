namespace TuiLib.Ui;

public static class Theme
{
    public static Color Accent { get; set; } = new(0, 120, 212);
    public static Color AccentForeground { get; set; } = Color.White;
    public static Color ControlBackground { get; set; } = new(58, 58, 64);
    public static Color ControlHoverBackground { get; set; } = new(78, 78, 86);
    public static Color ControlPressedBackground { get; set; } = new(36, 36, 40);
    public static Color InputBackground { get; set; } = new(36, 36, 40);
    public static Color InputFocusedBackground { get; set; } = new(28, 28, 32);
    public static Color TrackBackground { get; set; } = new(50, 50, 56);
    public static Color BorderBrush { get; set; } = Color.Gray;
    public static Color FocusedBorderBrush { get; set; } = new(0, 120, 212);
    public static Color DisabledForeground { get; set; } = Color.Gray;
    public static Color WatermarkForeground { get; set; } = Color.Gray;
    public static Color InactiveSelectionBackground { get; set; } = new(70, 70, 78);
}
