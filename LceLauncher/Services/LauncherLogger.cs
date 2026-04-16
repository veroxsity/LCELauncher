using System.Collections.Concurrent;
using System.Text;

namespace LceLauncher.Services;

public sealed class LauncherLogger
{
    private readonly ConcurrentQueue<string> _lines = new();
    private readonly string _latestLogPath;
    private readonly object _fileLock = new();

    public event Action<string>? LineLogged;

    public LauncherLogger(AppPaths appPaths)
    {
        Directory.CreateDirectory(appPaths.LauncherLogsRoot);
        _latestLogPath = appPaths.LauncherLatestLogPath;
    }

    public IReadOnlyCollection<string> Snapshot() => _lines.ToArray();

    public void Clear()
    {
        while (_lines.TryDequeue(out _)) { }
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {level,-5} {message}";
        _lines.Enqueue(line);
        AppendToDisk(line);

        while (_lines.Count > 500 && _lines.TryDequeue(out _)) { }

        LineLogged?.Invoke(line);
    }

    private void AppendToDisk(string line)
    {
        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(_latestLogPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch { }
    }
}
