using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace TuiLib;

internal struct WindowData
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class WindowManager
{
    public static WindowManager Instance { get; } = new();

    public Window MainWindow { get; private set; }
    public readonly List<Window> Windows = [];
    
    private Dictionary<Window, WindowData> _windowData = new();

    private WindowManager()
    {
        MainWindow = new();
        MainWindow.Id = Process.GetCurrentProcess().Id;
        MainWindow.Title = Process.GetCurrentProcess().ProcessName;
        
        _windowData.Add(MainWindow, new WindowData()
        {
            Id = MainWindow.Id,
            Title = MainWindow.Title,
            Width = Console.WindowWidth,
            Height = Console.WindowHeight,
        });
        
        LifetimeLoop();
    }

    private void LifetimeLoop()
    {
        Task.Run(() =>
        {
            while (true)
            {
                UpdateWindow(MainWindow);
                foreach (Window window in Windows)
                {
                    UpdateWindow(window);
                }

                Thread.Sleep(16);
            }
        });
    }
    
    private void UpdateWindow(Window window)
    {
        WindowData data = _windowData[window];
        if (window.Id == MainWindow.Id)
        {
            if (window.Title != data.Title)
            {
                Console.Title = window.Title;
                data.Title = window.Title;
            }

            window.Width = Console.WindowWidth;
            window.Height = Console.WindowHeight;
            
            if (window.Width != data.Width || window.Height != data.Height)
            {
                data.Width = window.Width;
                data.Height = window.Height;

                if (window.RaiseEvents)
                {
                    window.OnWindowResize.Invoke();
                }
            }
        }
        
        _windowData[window] = data;
    }
    
    public void Add(Window window)
    {
        Windows.Add(window);
        _windowData.Add(window, new WindowData()
        {
            Id = window.Id,
            Title = window.Title,
        });
    }
}