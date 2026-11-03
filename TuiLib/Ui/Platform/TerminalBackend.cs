using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TuiLib.Ui.Platform;

public static class TerminalBackend
{
    public static ITerminalBackend Create()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && UnixTerminalBackend.IsSupported)
        {
            return new UnixTerminalBackend();
        }

        return new ConsoleTerminalBackend();
    }

    internal static Size GetConsoleSize()
    {
        try
        {
            return new(Math.Max(1, Console.WindowWidth), Math.Max(1, Console.WindowHeight));
        }
        catch (IOException)
        {
            return new(80, 24);
        }
    }

    internal static void Write(Stream output, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        output.Write(bytes, 0, bytes.Length);
        output.Flush();
    }

    internal static string EnterSequence(bool enableMouse)
    {
        return "\x1b[?1049h\x1b[?25l\x1b[2J" + (enableMouse ? "\x1b[?1003h\x1b[?1006h" : string.Empty);
    }

    internal const string ExitSequence = "\x1b[?1003l\x1b[?1006l\x1b[0m\x1b[?25h\x1b[?1049l";

    internal static string TitleSequence(string? title)
    {
        return "\x1b]0;" + (title ?? string.Empty).Replace("\x07", string.Empty).Replace("\x1b", string.Empty) + "\x07";
    }
}
