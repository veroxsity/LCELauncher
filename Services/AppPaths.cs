namespace LceLauncher.Services;

public sealed class AppPaths
{
    private const string NightlyChannel = "nightly";

    public AppPaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DataRoot = Path.Combine(localAppData, "Minecraft Legacy Edition", "Launcher");
        ConfigPath = Path.Combine(DataRoot, "config.json");
        RuntimeRoot = Path.Combine(DataRoot, "runtime");
        DownloadsRoot = Path.Combine(DataRoot, "downloads");
        InstallsRoot = Path.Combine(DataRoot, "installs");
    }

    public string DataRoot { get; }

    public string ConfigPath { get; }

    public string RuntimeRoot { get; }

    public string DownloadsRoot { get; }

    public string InstallsRoot { get; }

    public string ManagedBridgeInstallRoot => Path.Combine(DataRoot, "bridge");

    public string ManagedBridgeJarPath => Path.Combine(ManagedBridgeInstallRoot, "bootstrap-standalone.jar");

    public string NightlyInstallRoot => GetClientInstallDirectory(NightlyChannel);

    public string NightlyClientExecutablePath => Path.Combine(NightlyInstallRoot, "Minecraft.Client.exe");

    public string NightlyMetadataPath => Path.Combine(NightlyInstallRoot, "release.json");

    public string NightlyDownloadPath => Path.Combine(DownloadsRoot, "LCEWindows64-nightly.zip");

    public string GetBridgeRuntimeDirectory(string serverId) => Path.Combine(RuntimeRoot, "bridges", serverId);

    public string GetClientInstallDirectory(string channel) => Path.Combine(InstallsRoot, channel);
}
