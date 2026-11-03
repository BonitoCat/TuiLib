using System.Collections.Generic;
using TuiLib.Rendering;
using TuiLib.Rendering.Borders;

namespace TuiLib.Ui.Rendering;

public sealed class DrawingContext
{
    private readonly CellBuffer _buffer;
    private readonly Stack<Rect> _clips = new();
    private Rect _clip;

    public Rect Clip => _clip;
    public Point? CursorPosition { get; set; }

    public DrawingContext(CellBuffer buffer)
    {
        _buffer = buffer;
        _clip = buffer.Bounds;
    }

    public void PushClip(Rect rect)
    {
        _clips.Push(_clip);
        _clip = _clip.Intersect(rect);
    }

    public void PopClip()
    {
        if (_clips.Count == 0)
        {
            return;
        }

        _clip = _clips.Pop();
    }

    public void FillRect(Rect rect, Color? background, char fill = ' ')
    {
        Rect area = rect.Intersect(_clip);
        string text = fill.ToString();
        for (int y = area.Y; y < area.Bottom; y++)
        {
            for (int x = area.X; x < area.Right; x++)
            {
                SetCell(x, y, new(text, null, background, FontStyles.None));
            }
        }
    }

    public void SetBackground(Rect rect, Color? background)
    {
        Rect area = rect.Intersect(_clip);
        for (int y = area.Y; y < area.Bottom; y++)
        {
            for (int x = area.X; x < area.Right; x++)
            {
                _buffer[x, y].Background = background;
            }
        }
    }

    public void SetForeground(Rect rect, Color? foreground)
    {
        Rect area = rect.Intersect(_clip);
        for (int y = area.Y; y < area.Bottom; y++)
        {
            for (int x = area.X; x < area.Right; x++)
            {
                _buffer[x, y].Foreground = foreground;
            }
        }
    }

    public int DrawText(int x, int y, string? text, Color? foreground, Color? background = null, FontStyles styles = FontStyles.None)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int column = x;
        foreach (string element in TextMeasure.GetElements(text!))
        {
            int width = TextMeasure.GetElementWidth(element);
            if (width == 0)
            {
                if (element[0] == '\t')
                {
                    DrawElement(column, y, " ", 1, foreground, background, styles);
                    column++;
                }

                continue;
            }

            DrawElement(column, y, element, width, foreground, background, styles);
            column += width;
        }

        return column - x;
    }

    public void DrawChar(int x, int y, char character, Color? foreground, Color? background = null, FontStyles styles = FontStyles.None)
    {
        DrawElement(x, y, character.ToString(), 1, foreground, background, styles);
    }

    public void DrawHorizontalLine(int x, int y, int length, char character, Color? foreground, Color? background = null)
    {
        for (int i = 0; i < length; i++)
        {
            DrawChar(x + i, y, character, foreground, background);
        }
    }

    public void DrawVerticalLine(int x, int y, int length, char character, Color? foreground, Color? background = null)
    {
        for (int i = 0; i < length; i++)
        {
            DrawChar(x, y + i, character, foreground, background);
        }
    }

    public void DrawBox(Rect rect, IBorderStyle style, Color? foreground, Color? background = null)
    {
        DrawBox(rect, style, new(1), foreground, background);
    }

    public void DrawBox(Rect rect, IBorderStyle style, Thickness sides, Color? foreground, Color? background = null)
    {
        if (rect.IsEmpty)
        {
            return;
        }

        bool left = sides.Left > 0;
        bool top = sides.Top > 0;
        bool right = sides.Right > 0 && rect.Width > 1;
        bool bottom = sides.Bottom > 0 && rect.Height > 1;
        int innerLeft = rect.X + (left ? 1 : 0);
        int innerRight = rect.Right - (right ? 1 : 0);
        int innerTop = rect.Y + (top ? 1 : 0);
        int innerBottom = rect.Bottom - (bottom ? 1 : 0);

        if (top)
        {
            DrawHorizontalLine(innerLeft, rect.Y, innerRight - innerLeft, style.GetChar(BorderSide.Top), foreground, background);
        }

        if (bottom)
        {
            DrawHorizontalLine(innerLeft, rect.Bottom - 1, innerRight - innerLeft, style.GetChar(BorderSide.Bottom), foreground, background);
        }

        if (left)
        {
            DrawVerticalLine(rect.X, innerTop, innerBottom - innerTop, style.GetChar(BorderSide.Left), foreground, background);
        }

        if (right)
        {
            DrawVerticalLine(rect.Right - 1, innerTop, innerBottom - innerTop, style.GetChar(BorderSide.Right), foreground, background);
        }

        DrawCorner(left, top, rect.X, rect.Y, style, BorderSide.TopLeft, foreground, background);
        DrawCorner(right, top, rect.Right - 1, rect.Y, style, BorderSide.TopRight, foreground, background);
        DrawCorner(left, bottom, rect.X, rect.Bottom - 1, style, BorderSide.BottomLeft, foreground, background);
        DrawCorner(right, bottom, rect.Right - 1, rect.Bottom - 1, style, BorderSide.BottomRight, foreground, background);
    }

    private void DrawCorner(bool vertical, bool horizontal, int x, int y, IBorderStyle style, BorderSide corner, Color? foreground, Color? background)
    {
        if (vertical && horizontal)
        {
            DrawChar(x, y, style.GetChar(corner), foreground, background);
            return;
        }

        if (vertical)
        {
            DrawChar(x, y, style.GetChar(BorderSide.Left), foreground, background);
            return;
        }

        if (horizontal)
        {
            DrawChar(x, y, style.GetChar(BorderSide.Top), foreground, background);
        }
    }

    private void DrawElement(int x, int y, string element, int width, Color? foreground, Color? background, FontStyles styles)
    {
        if (!_clip.Contains(new(x, y)))
        {
            return;
        }

        Color? cellBackground = background ?? _buffer[x, y].Background;
        if (width == 2 && !_clip.Contains(new(x + 1, y)))
        {
            SetCell(x, y, new(" ", foreground, cellBackground, styles));
            return;
        }

        SetCell(x, y, new(element, foreground, cellBackground, styles));
        if (width == 2)
        {
            Color? nextBackground = background ?? _buffer[x + 1, y].Background;
            SetCell(x + 1, y, new(string.Empty, foreground, nextBackground, styles));
        }
    }

    private void SetCell(int x, int y, Cell cell)
    {
        ref Cell target = ref _buffer[x, y];
        if (target.IsContinuation && !cell.IsContinuation && x > 0)
        {
            _buffer[x - 1, y].Text = " ";
        }

        bool wasWide = !target.IsContinuation && x + 1 < _buffer.Width && _buffer[x + 1, y].IsContinuation;
        target = cell;
        if (wasWide && !cell.IsContinuation && TextMeasure.GetElementWidth(cell.Text) < 2)
        {
            _buffer[x + 1, y].Text = " ";
        }
    }
}
