using System;
using TuiLib.Ui;
using TuiLib.Ui.Rendering;

namespace TuiDemo;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--snapshot")
        {
            MainWindow window = new();
            CellBuffer buffer = window.RenderToBuffer(80, 24);
            Console.WriteLine(buffer.ToPlainText());
            return 0;
        }

        return new TuiApplication().Run(new MainWindow());
    }
}
