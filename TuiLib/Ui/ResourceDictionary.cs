using System;
using System.Collections.Generic;

namespace TuiLib.Ui;

public sealed class ResourceDictionary : Dictionary<string, object?>
{
    public ResourceDictionary() : base(StringComparer.Ordinal)
    {
    }
}
