using System;
using System.Collections.Generic;
using TuiLib.Rendering.Borders;

namespace TuiLib.Ui;

public static class BorderStyles
{
    public static readonly IBorderStyle Rounded = new BorderStyleRounded();
    public static readonly IBorderStyle Square = new BorderStyleSquare();
    public static readonly IBorderStyle Strong = new BorderStyleSquareStrong();
    public static readonly IBorderStyle Double = new BorderStyleDouble();
    public static readonly IBorderStyle Ascii = new BorderStyleAscii();
    public static readonly IBorderStyle None = new BorderStyleNone();

    private static readonly Dictionary<string, IBorderStyle> ByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rounded"] = Rounded,
        ["Square"] = Square,
        ["Strong"] = Strong,
        ["SquareStrong"] = Strong,
        ["Double"] = Double,
        ["Ascii"] = Ascii,
        ["None"] = None,
    };

    public static void Register(string name, IBorderStyle style)
    {
        ByName[name] = style;
    }

    public static bool TryGet(string name, out IBorderStyle style)
    {
        return ByName.TryGetValue(name.Trim(), out style!);
    }
}
