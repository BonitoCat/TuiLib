using System;
using System.Collections.Generic;

namespace TuiLib.Ui.Controls;

public sealed class RowDefinitions : List<RowDefinition>
{
    public RowDefinitions()
    {
    }

    public RowDefinitions(string text)
    {
        foreach (string part in text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            Add(new(GridLength.Parse(part)));
        }
    }

    public static RowDefinitions Parse(string text) => new(text);
}
