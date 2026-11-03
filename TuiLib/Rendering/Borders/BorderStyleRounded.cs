using System;

namespace TuiLib.Rendering.Borders;

public class BorderStyleRounded : IBorderStyle
{
    public char GetChar(BorderSide side)
    {
        return side switch
        {
            BorderSide.Left => '│',
            BorderSide.Right => '│',
            BorderSide.Top => '─',
            BorderSide.Bottom => '─',
            BorderSide.TopLeft => '╭',
            BorderSide.TopRight => '╮',
            BorderSide.BottomLeft => '╰',
            BorderSide.BottomRight => '╯',
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
        };
    }
}