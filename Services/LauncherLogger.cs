using System.Collections.Concurrent;

namespace LceLauncher.Services;

public sealed class LauncherLogger
{
    private readonly ConcurrentQueue<string> _lines = new();

    public event Action<string>? LineLogged;

    public IReadOnlyCollection<string> Snapshot() => _lines.ToArray();

    public void Clear()
    {
        while (_lines.TryDequeue(out _))
        {
        }
    }

    public void Info(string message) => Write("INFO", message);

    public void Error(string message) => Write("ERROR", message);

    public void Warn(string message) => Write("WARN", message);

    private void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {level,-5} {message}";
        _lines.Enqueue(line);

        while (_lines.Count > 500 && _lines.TryDequeue(out _))
        {
        }

        LineLogged?.Invoke(line);
    }
}
