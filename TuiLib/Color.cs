using System;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

namespace TuiLib;

public class Color
{
    public enum AnsiType
    {
        Foreground, Background, Both,
    }

    internal enum ColorType
    {
        Normal, Reset, ResetFore, ResetBack,
    }

    public static readonly Color Reset = new(-1, -1, -1);
    private static readonly Color ResetFore = new(-2, -2, -2);
    private static readonly Color ResetBack = new(-3, -3, -3);
    public static readonly Color Red = new(255, 0, 0);
    public static readonly Color Green = new(0, 255, 0);
    public static readonly Color Blue = new(0, 0, 255);
    public static readonly Color Cyan = new(0, 255, 255);
    public static readonly Color Magenta = new(220, 140, 220);
    public static readonly Color Yellow = new(250, 240, 110);
    public static readonly Color Orange = new(250, 130, 50);
    public static readonly Color Purple = new(140, 70, 170);
    public static readonly Color White = new(255, 255, 255);
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color Gray = new(120, 120, 120);
    public static readonly Color LightGray = new(200, 200, 200);

    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }
    
    public float H { get; set; }
    public float S { get; set; }
    public float L { get; set; }
    
    internal ColorType Type { get; private set; }
    
    private static readonly Regex AnsiRegex =
        new(@"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])",
            RegexOptions.Compiled);
    
    public Color() { }

    public Color(int r, int g, int b)
    {
        R = ClampByte(r);
        G = ClampByte(g);
        B = ClampByte(b);
        Type = ColorType.Normal;
        
        if (r == -1 && g == -1 && b == -1)
        {
            Type = ColorType.Reset;
        }
        else if (r == -2 && g == -2 && b == -2)
        {
            Type = ColorType.ResetFore;
        }
        else if (r == -3 && g == -3 && b == -3)
        {
            Type = ColorType.ResetBack;
        }
    }
    
    public Color(float h, float s, float l)
    {
        H = h;
        S = s;
        L = l;
        Type = ColorType.Normal;
        
        if ((int) h == -1 && (int) s == -1 && (int) l == -1)
        {
            Type = ColorType.Reset;
        }
        else if ((int) h == -2 && (int) s == -2 && (int) l == -2)
        {
            Type = ColorType.ResetFore;
        }
        else if ((int) h == -3 && (int) s == -3 && (int) l == -3)
        {
            Type = ColorType.ResetBack;
        }
    }

    [Pure]
    public string ToAnsi(AnsiType type = AnsiType.Foreground)
    {
        if (this == Reset)
        {
            string ansiColor = "\x1b[";
            switch (type)
            {
                case AnsiType.Foreground:
                    ansiColor += "39";
                break;
                
                case AnsiType.Background:
                    ansiColor += "49";
                break;
                
                case AnsiType.Both:
                    ansiColor += "0";
                break;
            }

            return ansiColor + "m";
        }
        
        if (type == AnsiType.Foreground)
        {
            return $"\x1b[38;2;{ToRgbString()}m";
        }
        else if (type == AnsiType.Background)
        {
            return $"\x1b[48;2;{ToRgbString()}m";
        }
        else
        {
            return $"\x1b[38;2;{ToRgbString()}m\x1b[48;2;{ToRgbString()}m";
        }
    }

    public string ToRgbString()
    {
        return $"{R};{G};{B}";
    }
    
    [Pure]
    public static Color FromRgbString(string rgb)
    {
        string[] split;
        if (rgb.Contains(";"))
        {
            split = rgb.Trim().Split(';');
        }
        else if (rgb.Contains(" "))
        {
            split = rgb.Trim().Split(' ');
        }
        else
        {
            return new();
        }
        
        if (split.Length != 3)
        {
            return new();
        }

        if (!byte.TryParse(split[0], out byte r)
            || !byte.TryParse(split[1], out byte g)
            || !byte.TryParse(split[2], out byte b))
        {
            return new();
        }
        
        return new(r, g, b);
    }
    
    [Pure]
    public static Color FromHex(string hex)
    {
        byte r = 0;
        byte g = 0;
        byte b = 0;
        
        char[] values = hex.Trim().Replace("#", "").Replace(" ", "").ToCharArray();
        if (values.Length == 3)
        {
            r = Convert.ToByte(new string(values[0], 2), 16);
            g = Convert.ToByte(new string(values[1], 2), 16);
            b = Convert.ToByte(new string(values[2], 2), 16);
        }
        else if (values.Length == 6)
        {
            r = Convert.ToByte(values[0].ToString() + values[1], 16);
            g = Convert.ToByte(values[2].ToString() + values[3], 16);
            b = Convert.ToByte(values[4].ToString() + values[5], 16);
        }
        
        return new(r, g, b);
    }

    public string ToHex()
    {
        return ToHex(R, G, B);
    }
    
    [Pure]
    public static string ToHex(byte r, byte g, byte b)
    {
        string hex = "0123456789ABCDEF";
        return "#"
               + hex[r >> 4] + hex[r & 0xF]
               + hex[g >> 4] + hex[g & 0xF]
               + hex[b >> 4] + hex[b & 0xF];
    }
    
    [Pure]
    public static string ToHex(string rgb)
    {
        string[] split = [];
        
        if (rgb.Trim().Contains(" "))
        {
            split = rgb.Split(' ');
        }
        else if (rgb.Trim().Contains(";"))
        {
            split = rgb.Split(';');
        }

        if (split.Length == 3)
        {
            byte.TryParse(split[0], out byte r);
            byte.TryParse(split[1], out byte g);
            byte.TryParse(split[2], out byte b);

            return ToHex(r, g, b);
        }

        return "#000";
    }

    [Pure]
    public Color Transform(int r, int g, int b)
    {
        return new Color(ClampByte(R + r), ClampByte(G + g), ClampByte(B + b));
    }

    [Pure]
    public static Color LerpColorHsl(Color color1, Color color2, float t)
    {
        Color RgbToHsl(Color color)
        {
            float rf = color.R / 255f, gf = color.G / 255f, bf = color.B / 255f;
            float max = Math.Max(rf, Math.Max(gf, bf));
            float min = Math.Min(rf, Math.Min(gf, bf));
            float l = (max + min) / 2f;
            
            if (max == min)
            {
                return new(0f, 0f, l);
            }
            
            float d = max - min;
            float s = l > 0.5f ? d / (2f - max - min) : d / (max + min);
            float h = max == rf ? (gf - bf) / d + (gf < bf ? 6 : 0)
                : max == gf ? (bf - rf) / d + 2
                : (rf - gf) / d + 4;
            
            return new(h / 6f, s, l);
        }

        Color HslToRgb(Color color)
        {
            if (color.S == 0)
            {
                byte v = (byte) (color.L * 255);
                return new(v, v, v);
            }
            
            float Hue(float p, float q, float t2)
            {
                if (t2 < 0)
                {
                    t2++;
                }

                if (t2 > 1)
                {
                    t2--;
                }
                
                if (t2 < 1 / 6f)
                {
                    return p + (q - p) * 6 * t2;
                }
                
                if (t2 < 1 / 2f)
                {
                    return q;
                }
                
                if (t2 < 2 / 3f)
                {
                    return p + (q - p) * (2/3f - t2) * 6;
                }
                
                return p;
            }
            
            float q = color.L < 0.5f ? color.L * (1 + color.S) : color.L + color.S - color.L * color.S;
            float p = 2 * color.L - q;
            
            return new((byte) (Hue(p, q, color.H + 1/3f) * 255),
                (byte) (Hue(p, q, color.H) * 255),
                (byte) (Hue(p, q, color.H - 1/3f) * 255));
        }

        Color colorA = RgbToHsl(color1);
        Color colorB = RgbToHsl(color2);

        if (Math.Abs(colorB.H - colorA.H) > 0.5f)
        {
            if (colorA.H < colorB.H)
            {
                colorA.H += 1f;
            }
            else
            {
                colorB.H += 1f;
            }
        }

        Color col = new(colorA.H + (colorB.H - colorA.H) * t, colorA.S + (colorB.S - colorA.S) * t, colorA.L + (colorB.L - colorA.L) * t);
        return HslToRgb(col);
    }
    
    [Pure]
    public static string TrimAnsi(string text)
    {
        return AnsiRegex.Replace(text, "");
    }
    
    [Pure]
    private static byte ClampByte(int a)
    {
        return (byte) Math.Min(Math.Max(a, 0), 255);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Color color)
        {
            return false;
        }
        
        return this == color;
    }

    public static bool operator ==(Color color1, Color color2)
    {
        return color1.R == color2.R &&
               color1.G == color2.G &&
               color1.B == color2.B &&
               color1.Type == color2.Type;
    }
    
    public static bool operator !=(Color color1, Color color2)
    {
        return color1.R != color2.R ||
               color1.G != color2.G ||
               color1.B != color2.B ||
               color1.Type != color2.Type;
    }
}
