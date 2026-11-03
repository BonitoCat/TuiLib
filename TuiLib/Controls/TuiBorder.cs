using System;
using System.Collections.Generic;
using TuiLib.Rendering;
using TuiLib.Components;
using TuiLib.Rendering.Borders;

namespace TuiLib.Controls;

public class TuiBorder : Control
{
    public override ControlType Type => ControlType.Border;
    public Control? Child { get; set; }
    public TuiLabel Title { get; set; } = new("");
    public Color Color { get; set; } = Color.Reset;
    public IBorderStyle BorderStyle { get; set; } = new BorderStyleRounded();
    public bool RenderChild { get; set; } = true;
    
    public override int X => Child?.X - 1 ?? 0;
    public override int Y => Child?.Y - 1 ?? 0;
    public override int MaxWidth => Child?.MaxWidth ?? 0 + 2;
    public override int MaxHeight => Child?.MaxHeight ?? 0 + 2;
    
    public void OnFocus()
    {
    }

    public void OnBlur()
    {
    }

    protected override IEnumerable<RenderPart> RenderCore()
    {
        if (Child == null)
        {
            yield break;
        }
        
        (int dx, int dy) = Child.GetAnchorOffset();
        yield return new RenderPart
        {
            X = Child.X - dx - 1,
            Y = Child.Y - dy - 1,
            Text =
                $"{BorderStyle.GetChar(BorderSide.TopLeft)}" +
                $"{new string(BorderStyle.GetChar(BorderSide.Top), Child.Width)}" +
                $"{BorderStyle.GetChar(BorderSide.TopRight)}",
            Foreground = Color,
            ZIndex = Child.ZIndex,
        };

        for (int i = 0; i < Child.Height; i++)
        {
            yield return new RenderPart
            {
                X = Child.X - dx - 1,
                Y = Child.Y - dy + i,
                Text =
                    $"{BorderStyle.GetChar(BorderSide.Left)}" +
                    $"{new string(' ', Child.Width)}" +
                    $"{BorderStyle.GetChar(BorderSide.Right)}",
                Foreground = Color,
                ZIndex = Child.ZIndex,
            };
        }

        yield return new RenderPart
        {
            X = Child.X - dx - 1,
            Y = Child.Y - dy + Child.Height,
            Text =
                $"{BorderStyle.GetChar(BorderSide.BottomLeft)}" +
                $"{new string(BorderStyle.GetChar(BorderSide.Bottom), Child.Width)}" +
                $"{BorderStyle.GetChar(BorderSide.BottomRight)}",
            Foreground = Color,
            ZIndex = Child.ZIndex,
        };
        
        string title = Title.ToString();
        if (Title.Length - 3 > Child.Width + 2)
        {
            title = title.Substring(0, Math.Max(Child.Width - 7, 0)) + "...";
        }
        
        if (Title.Visibility == Visibility.Visible && !string.IsNullOrEmpty(title))
        {
            yield return new RenderPart
            {
                X = Child.X - dx + 1,
                Y = Child.Y - dy - 1,
                Text = title,
                Foreground = Title.Style.Foreground,
                Background = Title.Style.Background,
                ZIndex = Title.ZIndex,
            };
        }

        if (!RenderChild)
        {
            yield break;
        }
        
        foreach (RenderPart part in Child.Render())
        {
            yield return part;
        }
    }
}