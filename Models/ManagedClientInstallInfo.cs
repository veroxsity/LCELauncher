namespace LceLauncher.Models;

public sealed class ManagedClientInstallInfo
{
    public bool IsInstalled { get; init; }

    public string InstallRoot { get; init; } = string.Empty;

    public string ClientExecutablePath { get; init; } = string.Empty;

    public string DisplayVersion { get; init; } = "Nightly";

    public DateTimeOffset? PublishedAtUtc { get; init; }

    public DateTimeOffset? InstalledAtUtc { get; init; }
}
