using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace LceLauncher;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Catch unhandled exceptions that would silently kill the WinExe process
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var msg = e.ExceptionObject?.ToString() ?? "Unknown error";
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LceLauncher-crash.txt");
            File.WriteAllText(path, msg);
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LceLauncher-crash.txt");
            File.WriteAllText(path, ex.ToString());
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
