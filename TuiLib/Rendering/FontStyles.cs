using System;

namespace TuiLib.Rendering;

[Flags]
public enum FontStyles
{
    None = 0,
    Strong = 1 << 0,
    Italic = 1 << 2,
    Underlined = 1 << 3,
    Strikeout = 1 << 4,
}