using System;
using System.Collections.Generic;

namespace TuiLib.Ui.Controls;

public sealed class ColumnDefinitions : List<ColumnDefinition>
{
    public ColumnDefinitions()
    {
    }

    public ColumnDefinitions(string text)
    {
        foreach (string part in text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            Add(new(GridLength.Parse(part)));
        }
    }

    public static ColumnDefinitions Parse(string text) => new(text);
}
