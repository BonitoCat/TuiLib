using System;
using System.Diagnostics;
using System.IO;
using TuiLib.Context;
using TuiLib.Helpers;

namespace TuiLib;

public class Window
{
    public delegate void OnWindowResizeHandle();
    
    public int Id { get; set; }
    public StreamWriter? Input { get; }
    public StreamReader? Output { get; }
    
    public string Title { get; set; } = "";
    public string? WorkingDirectory { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public bool RaiseEvents { get; set; } = true;
    public OnWindowResizeHandle OnWindowResize = () => {};

    public TerminalLaunchInfo LaunchInfo => _launchInfo ??= TerminalHelper.GetBestTerminal();

    private TerminalLaunchInfo? _launchInfo;

    public Window() { }
    
    public void Launch(string? extraArgs = null)
    {
        _launchInfo ??= TerminalHelper.GetBestTerminal();
        if (!string.IsNullOrEmpty(WorkingDirectory))
        {
            _launchInfo.Process.StartInfo.WorkingDirectory = WorkingDirectory;
        }

        _launchInfo.Launch(extraArgs);
        
        Id = _launchInfo.Process.Id;
    }
    
    public void Close()
    {
        if (_launchInfo != null)
        {
            _launchInfo.Kill();
        }
    }
}