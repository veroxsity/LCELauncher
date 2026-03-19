namespace LceLauncher.Models;

public sealed class ManagedClientUpdateInfo
{
    public bool IsInstalled { get; init; }

    public bool CheckedRemotely { get; init; }

    public bool UpdateAvailable { get; init; }

    public string CurrentVersion { get; init; } = string.Empty;

    public string LatestVersion { get; init; } = string.Empty;

    public DateTimeOffset? LatestPublishedAtUtc { get; init; }

    public string StatusText { get; init; } = string.Empty;
}
