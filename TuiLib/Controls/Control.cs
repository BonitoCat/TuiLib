using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TuiLib.Components;
using TuiLib.Rendering;

namespace TuiLib;

public abstract class Control : IRenderable
{
    public abstract ControlType Type { get; }
    public Control? Parent { get; internal set; }

    public virtual int X { get; set; }
    public virtual int Y { get; set; }
    public virtual AnchorPoint AnchorPoint { get; set; } = AnchorPoint.TopLeft;
    public Dictionary<string, string> Data { get; set; } = new();

    public int Width
    {
        get => _width ?? MaxWidth;
        set => _width = Math.Min(value, MaxWidth);
    }

    public int Height
    {
        get => _height ?? MaxHeight;
        set => _height = Math.Min(value, MaxHeight);
    }
    
    public virtual int MaxWidth { get; set; }
    public virtual int MaxHeight { get; set; }
    public int ZIndex { get; set; }
    public Visibility Visibility { get; set; } = Visibility.Visible;

    private int? _width;
    private int? _height;
    
    public (int dx, int dy) GetAnchorOffset()
    {
        return AnchorPoint switch
        {
            AnchorPoint.TopLeft => (0, 0),
            AnchorPoint.TopCenter => (Width / 2, 0),
            AnchorPoint.TopRight => (Width, 0),
            
            AnchorPoint.CenterLeft => (0, Height / 2),
            AnchorPoint.Center => (Width / 2, Height / 2),
            AnchorPoint.CenterRight => (Width, Height / 2),
            
            AnchorPoint.BottomLeft => (0, Height),
            AnchorPoint.BottomCenter => (Width / 2, Height),
            AnchorPoint.BottomRight => (Width, Height),

            _ => (0, 0),
        };
    }
    
    /*
    /// <summary>
    /// The base render logic for every control element. Not intended to be overriden unless you want to change base rendering logic.
    /// </summary>
    /// <seealso cref="RenderCore"/>
    */
    public IEnumerable<RenderPart> Render()
    {
        if (Visibility == Visibility.Gone)
        {
            yield break;
        }

        if (Visibility == Visibility.Hidden)
        {
            yield return new RenderPart();
            yield break;
        }

        (int dx, int dy) = GetAnchorOffset();
        foreach (RenderPart part in RenderCore())
        {
            yield return new RenderPart
            {
                X = part.X - dx,
                Y = part.Y - dy,
                Text = part.Text,
                Foreground = part.Foreground,
                Background = part.Background,
                ZIndex = part.ZIndex,
            };
        }
    }

    /// <summary>
    /// The core rendering logic for each control element. Implement this to render your object correctly.
    /// </summary>
    protected abstract IEnumerable<RenderPart> RenderCore();
}