using System.Diagnostics;

namespace TuiLib.Context;

public sealed class TerminalLaunchInfo
{
    public Process Process { get; set; }
    public string ExecutablePath { get; }
    public string DefaultArgs { get; }

    public TerminalLaunchInfo(string executablePath, string? defaultArgs = null)
    {
        ExecutablePath = executablePath;
        defaultArgs ??= "";
        DefaultArgs = defaultArgs;

        Process = new()
        {
            StartInfo = new()
            {
                FileName = ExecutablePath,
                Arguments = defaultArgs,
                UseShellExecute = false,  
            },
            EnableRaisingEvents = true,
        };
    }

    public void Launch(string? extraArgs = null)
    {
        if (!string.IsNullOrEmpty(extraArgs))
        {
            Process.StartInfo.Arguments += $" {extraArgs}";
            Process.StartInfo.Arguments = Process.StartInfo.Arguments.Trim();
        }
        
        Process.Start();
    }

    public void Kill()
    {
        if (!Process.HasExited)
        {
            Process.Kill();
        }
    }
}