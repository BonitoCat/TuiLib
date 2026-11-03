using System.IO;
using System.Text;
using TuiLib.Rendering;

namespace TuiLib.Ui.Rendering;

public sealed class TerminalPresenter
{
    private const string Esc = "\x1b";

    private readonly Stream _output;
    private readonly UTF8Encoding _encoding = new(false);
    private CellBuffer? _previous;

    public bool UseSynchronizedOutput { get; set; } = true;

    public TerminalPresenter(Stream output)
    {
        _output = output;
    }

    public void Invalidate()
    {
        _previous = null;
    }

    public void Present(CellBuffer buffer, Point? cursor)
    {
        string frame = BuildFrame(buffer, cursor);
        byte[] bytes = _encoding.GetBytes(frame);
        _output.Write(bytes, 0, bytes.Length);
        _output.Flush();
    }

    public string BuildFrame(CellBuffer buffer, Point? cursor)
    {
        StringBuilder builder = new();
        if (UseSynchronizedOutput)
        {
            builder.Append(Esc).Append("[?2026h");
        }

        bool fullRedraw = _previous == null || _previous.Width != buffer.Width || _previous.Height != buffer.Height;
        if (fullRedraw)
        {
            builder.Append(Esc).Append("[0m").Append(Esc).Append("[2J");
        }

        Cell? currentStyle = null;
        int cursorX = -1;
        int cursorY = -1;
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                Cell cell = buffer[x, y];
                if (cell.IsContinuation)
                {
                    continue;
                }

                bool wide = x + 1 < buffer.Width && buffer[x + 1, y].IsContinuation;
                if (!fullRedraw && cell.ContentEquals(_previous![x, y]) && (!wide || buffer[x + 1, y].ContentEquals(_previous[x + 1, y])))
                {
                    continue;
                }

                if (cursorX != x || cursorY != y)
                {
                    builder.Append(Esc).Append('[').Append(y + 1).Append(';').Append(x + 1).Append('H');
                }

                if (currentStyle == null || !currentStyle.Value.HasSameStyle(cell))
                {
                    AppendStyle(builder, cell);
                    currentStyle = cell;
                }

                builder.Append(cell.Text);
                cursorX = x + (wide ? 2 : 1);
                cursorY = y;
            }
        }

        builder.Append(Esc).Append("[0m");
        if (cursor is { } position && buffer.Contains(position.X, position.Y))
        {
            builder.Append(Esc).Append('[').Append(position.Y + 1).Append(';').Append(position.X + 1).Append('H');
            builder.Append(Esc).Append("[?25h");
        }
        else
        {
            builder.Append(Esc).Append("[?25l");
        }

        if (UseSynchronizedOutput)
        {
            builder.Append(Esc).Append("[?2026l");
        }

        _previous = buffer.Clone();
        return builder.ToString();
    }

    private static void AppendStyle(StringBuilder builder, Cell cell)
    {
        builder.Append(Esc).Append("[0");
        if ((cell.Styles & FontStyles.Strong) != 0)
        {
            builder.Append(";1");
        }

        if ((cell.Styles & FontStyles.Italic) != 0)
        {
            builder.Append(";3");
        }

        if ((cell.Styles & FontStyles.Underlined) != 0)
        {
            builder.Append(";4");
        }

        if ((cell.Styles & FontStyles.Strikeout) != 0)
        {
            builder.Append(";9");
        }

        if (!Cell.IsDefault(cell.Foreground))
        {
            Color color = cell.Foreground!;
            builder.Append(";38;2;").Append(color.R).Append(';').Append(color.G).Append(';').Append(color.B);
        }

        if (!Cell.IsDefault(cell.Background))
        {
            Color color = cell.Background!;
            builder.Append(";48;2;").Append(color.R).Append(';').Append(color.G).Append(';').Append(color.B);
        }

        builder.Append('m');
    }
}
