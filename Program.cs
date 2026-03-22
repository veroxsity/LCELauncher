using System.Text;
using LceLauncher.Services;
using LceLauncher.UI;

namespace LceLauncher;

internal static class Program
{
    private static int _crashLogged;

    [STAThread]
    private static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) =>
        {
            var path = WriteCrashLog("winforms-thread", args.Exception, terminating: true);
            try
            {
                MessageBox.Show(
                    $"The launcher hit an unhandled exception and needs to close.\n\nCrash log: {path}",
                    "Launcher Crash",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
            }

            Environment.Exit(1);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            WriteCrashLog(
                "appdomain",
                args.ExceptionObject as Exception ?? new Exception($"Non-exception crash object: {args.ExceptionObject}"),
                args.IsTerminating);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            WriteCrashLog("task", args.Exception, terminating: false);
            args.SetObserved();
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static string WriteCrashLog(string source, Exception exception, bool terminating)
    {
        var appPaths = new AppPaths();
        Directory.CreateDirectory(appPaths.LauncherCrashLogsRoot);
        var timestamp = DateTime.Now;
        var path = Path.Combine(
            appPaths.LauncherCrashLogsRoot,
            $"launcher-crash-{timestamp:yyyy-MM-dd_HH-mm-ss-fff}.log");

        var builder = new StringBuilder()
            .AppendLine($"Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}")
            .AppendLine($"Source: {source}")
            .AppendLine($"Terminating: {terminating}")
            .AppendLine($"OS: {Environment.OSVersion}")
            .AppendLine($".NET: {Environment.Version}")
            .AppendLine()
            .AppendLine(exception.ToString());

        try
        {
            if (Interlocked.Exchange(ref _crashLogged, 1) == 0)
            {
                File.AppendAllText(appPaths.LauncherLatestLogPath,
                    $"[{timestamp:HH:mm:ss}] ERROR Launcher crash captured from {source}: {exception.Message}{Environment.NewLine}",
                    Encoding.UTF8);
            }
        }
        catch
        {
        }

        try
        {
            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        }
        catch
        {
        }

        return path;
    }
}
