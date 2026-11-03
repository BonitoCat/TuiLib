using System;
using System.Text;

namespace TuiLib.Ui.Rendering;

public sealed class CellBuffer
{
    private readonly Cell[] _cells;

    public int Width { get; }
    public int Height { get; }
    public Rect Bounds => new(0, 0, Width, Height);

    public CellBuffer(int width, int height)
    {
        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
        _cells = new Cell[Width * Height];
        Clear();
    }

    public ref Cell this[int x, int y] => ref _cells[y * Width + x];

    public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public void Clear()
    {
        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i] = Cell.Blank;
        }
    }

    public CellBuffer Clone()
    {
        CellBuffer clone = new(Width, Height);
        Array.Copy(_cells, clone._cells, _cells.Length);
        return clone;
    }

    public string GetLine(int y, bool trimEnd = true)
    {
        StringBuilder builder = new();
        for (int x = 0; x < Width; x++)
        {
            builder.Append(this[x, y].Text);
        }

        string line = builder.ToString();
        return trimEnd ? line.TrimEnd() : line;
    }

    public string ToPlainText(bool trimEnd = true)
    {
        StringBuilder builder = new();
        for (int y = 0; y < Height; y++)
        {
            if (y > 0)
            {
                builder.Append('\n');
            }

            builder.Append(GetLine(y, trimEnd));
        }

        return builder.ToString();
    }
}
