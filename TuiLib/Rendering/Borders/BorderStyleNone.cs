namespace TuiLib.Rendering.Borders;

public class BorderStyleNone : IBorderStyle
{
    public char GetChar(BorderSide side)
    {
        return ' ';
    }
}