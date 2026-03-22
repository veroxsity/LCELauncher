namespace LceLauncher.Models;

public sealed class LauncherConfig
{
    public bool PreferManagedClientInstall { get; set; } = true;

    public ManagedClientStream ManagedClientInstallStream { get; set; } = ManagedClientStream.Release;

    public ManagedClientStream ManagedClientLaunchStream { get; set; } = ManagedClientStream.Release;

    public bool PreferManagedBridgeInstall { get; set; } = true;

    public bool CheckForManagedClientUpdatesOnStartup { get; set; } = true;

    public bool NotifyWhenManagedClientUpdateAvailable { get; set; } = true;

    public bool CheckForManagedBridgeUpdatesOnStartup { get; set; } = true;

    public bool NotifyWhenManagedBridgeUpdateAvailable { get; set; } = true;

    public string ManagedBridgeLogLevel { get; set; } = "info";

    public bool ManagedBridgeLogPackets { get; set; } = false;

    public string? ClientExecutablePath { get; set; }

    public string? BridgeJarPath { get; set; }

    public string JavaExecutablePath { get; set; } = "java";

    public AuthMode AuthMode { get; set; } = AuthMode.Local;

    public string LocalUsername { get; set; } = "Player";

    public bool SyncUsernameFromOnlineAccount { get; set; } = true;

    public string? MicrosoftAuthClientId { get; set; }

    public int FirstBridgePort { get; set; } = 25570;

    public bool CloseBridgeOnExit { get; set; } = true;

    public string LaunchArguments { get; set; } = string.Empty;

    public string? SelectedServerId { get; set; }

    public DateTimeOffset? ManagedClientLastCheckedAtUtc { get; set; }

    public string? ManagedClientLastUpdateStatusText { get; set; }

    public bool ManagedClientLastUpdateAvailable { get; set; }

    public string? ManagedClientLastKnownLatestVersion { get; set; }

    public string? ManagedClientLastNotifiedVersion { get; set; }

    public Dictionary<string, ManagedClientUpdateState> ManagedClientUpdateStates { get; set; } = [];

    public DateTimeOffset? ManagedBridgeLastCheckedAtUtc { get; set; }

    public string? ManagedBridgeLastUpdateStatusText { get; set; }

    public bool ManagedBridgeLastUpdateAvailable { get; set; }

    public string? ManagedBridgeLastKnownLatestVersion { get; set; }

    public string? ManagedBridgeLastNotifiedVersion { get; set; }

    public List<ServerEntry> Servers { get; set; } = [];
}
