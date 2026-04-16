namespace LceLauncher.Models;

public sealed class ManagedClientInstallInfo
{
    public ManagedClientStream Stream { get; init; }
    public bool IsInstalled { get; init; }
    public string InstallRoot { get; init; } = string.Empty;
    public string ClientExecutablePath { get; init; } = string.Empty;
    public string DisplayVersion { get; init; } = "Nightly";
    public string StreamLabel { get; init; } = "Release";
    public DateTimeOffset? PublishedAtUtc { get; init; }
    public DateTimeOffset? InstalledAtUtc { get; init; }
}
