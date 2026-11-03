using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TuiLib.Components;

namespace TuiLib.Rendering;

public sealed class TuiRenderer : IRenderer
{
    private readonly object _outLock = new();
    private static readonly Stream Output = Console.OpenStandardOutput();
    
    public void Render(IEnumerable<RenderPart> parts)
    {
        lock (_outLock)
        {
            StringBuilder frameBuilder = new();
            foreach (RenderPart part in parts.OrderBy(p => p.ZIndex))
            {
                if (part.X < 0 || part.X >= Console.WindowWidth)
                {
                    int absX = Math.Abs(part.X);
                    if (absX > part.Text.Length)
                    {
                        continue;
                    }

                    part.Text = part.Text.Substring(absX);
                    part.X = 0;
                }

                if (part.Y < 0 || part.Y >= Console.WindowHeight)
                {
                    continue;
                }

                frameBuilder.Append($"\x1b[{part.Y + 1};{part.X + 1}H");
                frameBuilder.Append(part.Foreground.ToAnsi());
                frameBuilder.Append(part.Background.ToAnsi(Color.AnsiType.Background));
                frameBuilder.Append(part.Text);
            }

            frameBuilder.Append("\x1b[0m\x1b[?2026l");
            byte[] frame = Encoding.UTF8.GetBytes("\x1b[?2026h" + frameBuilder);
            Console.Out.Flush();
            Output.Write(frame, 0, frame.Length);
            Output.Flush();
        }
    }

    public void ClearScreen()
    {
        lock (_outLock)
        {
            Console.Clear();
        }
    }
}
