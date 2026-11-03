using System;

namespace TuiLib.Ui;

[Flags]
public enum PropertyFlags
{
    None = 0,
    AffectsMeasure = 1 << 0,
    AffectsRender = 1 << 1,
    Inherits = 1 << 2,
}
