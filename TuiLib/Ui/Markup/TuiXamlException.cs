using System;

namespace TuiLib.Ui.Markup;

public sealed class TuiXamlException : Exception
{
    public int LineNumber { get; }
    public int LinePosition { get; }

    public TuiXamlException(string message, int lineNumber = 0, int linePosition = 0, Exception? innerException = null)
        : base(lineNumber > 0 ? $"{message} (line {lineNumber}, position {linePosition})" : message, innerException)
    {
        LineNumber = lineNumber;
        LinePosition = linePosition;
    }
}
