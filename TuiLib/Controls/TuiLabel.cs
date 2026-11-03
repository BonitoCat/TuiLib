using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TuiLib.Components;
using TuiLib.Rendering;

namespace TuiLib.Controls;

public class TuiLabel : Control
{
    public override ControlType Type => ControlType.Label;
    public string Text
    {
        get
        {
            return _text;
        }
        set
        {
            _text = value;
            
            Width = _text.Length > MaxWidth ? MaxWidth : _text.Length;

            int height = _text.Split('\n').Length + 1;
            Height = height > MaxHeight ? MaxHeight : height;
        }
    }
    
    public string Prefix { get; set; } = "";
    public string Suffix { get; set; } = "";
    public TextStyle Style { get; set; } = new();
    
    public int Length => PrefixLength + TextLength + SuffixLength;
    public int PrefixLength => Color.TrimAnsi(Prefix).Length;
    public int TextLength => Color.TrimAnsi(Text).Length;
    public int SuffixLength => Color.TrimAnsi(Suffix).Length;

    private string _text = "Label";

    public TuiLabel() { }
    
    public TuiLabel(string text, TextStyle style)
    {
        Text = text;
        Style = style;
    }
    
    public TuiLabel(string text, [DefaultValue("Color.Reset")] Color? foregroundColor = null, [DefaultValue("Color.Reset")] Color? backgroundColor = null)
    {
        foregroundColor ??= Color.Reset;
        backgroundColor ??= Color.Reset;
        
        Text = text;
        Width = text.Length > MaxWidth ? MaxWidth : text.Length;

        int height = text.Split('\n').Length + 1;
        Height = height > MaxHeight ? MaxHeight : height;
        
        Style = new()
        {
            Background = backgroundColor,
            Foreground = foregroundColor,
        };
    }

    protected override IEnumerable<RenderPart> RenderCore()
    {
        yield return new()
        {
            X = X,
            Y = Y,
            Text = ToString(),
            Foreground = Style.Foreground,
            Background = Style.Background,
            ZIndex = ZIndex,
        };
    }

    public override string ToString()
    {
        if (Parent == null)
        {
            return $"{Prefix}" +
                   $"{Style.Foreground.ToAnsi()}" +
                   $"{Style.Background.ToAnsi(Color.AnsiType.Background)}" +
                   $"{Text}" +
                   $"{Color.Reset.ToAnsi(Color.AnsiType.Both)}" +
                   $"{Suffix}";
        }
        
        string text = Text;
        if (!Style.TextWrapping && !Style.WordWrapping && Parent != null && Length > Parent.MaxWidth)
        {
            text = $"{Prefix}" +
                   $"{Style.Foreground.ToAnsi()}" +
                   $"{Style.Background.ToAnsi(Color.AnsiType.Background)}";
            
            if (Style.Ellipse)
            {
                text += $"{Text.Substring(0, Parent.MaxWidth - PrefixLength - SuffixLength)}...";
            }
            else
            {
                text += Text.Substring(0, Parent.MaxWidth - PrefixLength - SuffixLength);
            }
            
            text += $"{Color.Reset.ToAnsi(Color.AnsiType.Both)}" +
                    $"{Suffix}";
        }
        else if (Style.TextWrapping && Parent != null && Length > Parent.MaxWidth)
        {
            List<string> parts = [Prefix + text];
            bool addedSuffix = false;
            while (parts[parts.Count - 1].Length > Parent.MaxWidth)
            {
                string sub1 = parts[parts.Count - 1].Substring(0, Parent.MaxWidth);
                string sub2 = parts[parts.Count - 1].Substring(Parent.MaxWidth).TrimStart();

                if (sub2.Length < Parent.MaxWidth && !addedSuffix)
                {
                    sub2 += Suffix;
                    addedSuffix = true;
                }
                
                parts[parts.Count - 1] = sub1;
                parts.Add(sub2);
            }

            text = "";
            parts.ForEach(part =>
            {
                text +=
                    $"{Style.Foreground.ToAnsi()}" +
                    $"{Style.Background.ToAnsi(Color.AnsiType.Background)}" +
                    $"{part}" +
                    $"{Color.Reset.ToAnsi(Color.AnsiType.Both)}\n";
            });

            text = text.TrimEnd('\n');
        }
        else if (Style.WordWrapping && Parent != null && Length > Parent?.MaxWidth)
        {
            // FINISH
            
            text =
                $"{Prefix}" +
                $"{Style.Foreground.ToAnsi()}" +
                $"{Style.Background.ToAnsi(Color.AnsiType.Background)}" +
                $"{text.Substring(0, Parent.MaxWidth - PrefixLength - SuffixLength)}\n" +
                $"{Color.Reset.ToAnsi(Color.AnsiType.Both)}" +
                $"{Suffix}";
        }
        else
        {
            return $"{Prefix}" +
                   $"{Style.Foreground.ToAnsi()}" +
                   $"{Style.Background.ToAnsi(Color.AnsiType.Background)}" +
                   $"{Text}" +
                   $"{Color.Reset.ToAnsi(Color.AnsiType.Both)}" +
                   $"{Suffix}";
        }

        return text;
    }
    
    public static implicit operator TuiLabel(string str)
    {
        return new(str);
    }
}