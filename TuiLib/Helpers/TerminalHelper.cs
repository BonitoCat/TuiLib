using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using TuiLib.Context;

namespace TuiLib.Helpers;

public static class TerminalHelper
{
    public static void BeginSession()
    {
        Console.Write("\x1b[?7l\x1b[?25l");
    }

    public static void EndSession()
    {
        Console.Write("\x1b[0m\x1b[?7h\x1b[0 q\x1b[?25h");
    }

    public static void HideCursor()
    {
        Console.Write("\x1b[?25l");
    }

    public static void ShowCursor()
    {
        Console.Write("\x1b[?25h");
    }

    public static TerminalLaunchInfo GetBestTerminal()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsTerminal();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return GetMacTerminal();
        }

        return GetLinuxTerminal();
    }

    private static TerminalLaunchInfo GetWindowsTerminal()
    {
        string wt = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "wt.exe");

        if (File.Exists(wt))
        {
            return new TerminalLaunchInfo("wt.exe");
        }

        return new TerminalLaunchInfo("cmd.exe");
    }

    private static TerminalLaunchInfo GetMacTerminal()
    {
        return new TerminalLaunchInfo(
            "open",
            "-a Terminal"
        );
    }

    private static TerminalLaunchInfo GetLinuxTerminal()
    {
        string? overrideTerm = Environment.GetEnvironmentVariable("CMDMENU_TERMINAL");
        if (!string.IsNullOrWhiteSpace(overrideTerm))
        {
            return new TerminalLaunchInfo(overrideTerm);
        }

        string[] candidates =
        [
            "gnome-terminal",
            "kitty",
            "wezterm",
            "alacritty",
            "konsole",
            "xfce4-terminal",
            "xterm",
        ];

        foreach (string term in candidates)
        {
            if (IsExecutableAvailable(term))
            {
                return CreateLinuxTerminal(term);
            }
        }

        return new TerminalLaunchInfo("xterm");
    }

    private static TerminalLaunchInfo CreateLinuxTerminal(string terminal)
    {
        return terminal switch
        {
            "gnome-terminal" => new TerminalLaunchInfo("gnome-terminal", "--"),
            "konsole" => new TerminalLaunchInfo("konsole", "-e"),
            _ => new TerminalLaunchInfo(terminal, "-e"),
        };
    }

    private static bool IsExecutableAvailable(string name)
    {
        try
        {
            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = name,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });

            process!.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
