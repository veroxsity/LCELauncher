namespace LceLauncher.Models;

public sealed class ManagedBridgeInstallInfo
{
    public bool IsInstalled { get; init; }

    public string InstallRoot { get; init; } = string.Empty;

    public string BridgeJarPath { get; init; } = string.Empty;

    public string DisplayVersion { get; init; } = "Bridge runtime";

    public DateTimeOffset? PublishedAtUtc { get; init; }

    public DateTimeOffset? InstalledAtUtc { get; init; }
}
