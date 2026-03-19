namespace LceLauncher.Models;

public sealed class LauncherConfig
{
    public bool PreferManagedClientInstall { get; set; } = true;

    public string? ClientExecutablePath { get; set; }

    public string? BridgeJarPath { get; set; }

    public string JavaExecutablePath { get; set; } = "java";

    public AuthMode AuthMode { get; set; } = AuthMode.Local;

    public string LocalUsername { get; set; } = "Player";

    public int FirstBridgePort { get; set; } = 25570;

    public bool CloseBridgeOnExit { get; set; } = true;

    public string LaunchArguments { get; set; } = string.Empty;

    public string? SelectedServerId { get; set; }

    public List<ServerEntry> Servers { get; set; } = [];
}
