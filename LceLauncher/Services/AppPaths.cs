using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class AppPaths
{
    public AppPaths()
    {
        DataRoot = GetPlatformDataRoot();
        ConfigPath = Path.Combine(DataRoot, "config.json");
        RuntimeRoot = Path.Combine(DataRoot, "runtime");
        DownloadsRoot = Path.Combine(DataRoot, "downloads");
        InstallsRoot = Path.Combine(DataRoot, "installs");
        AuthRoot = Path.Combine(DataRoot, "auth");
    }

    public string DataRoot { get; }
    public string ConfigPath { get; }
    public string RuntimeRoot { get; }
    public string DownloadsRoot { get; }
    public string InstallsRoot { get; }
    public string AuthRoot { get; }

    public string LauncherLogsRoot => Path.Combine(DataRoot, "logs");
    public string LauncherLatestLogPath => Path.Combine(LauncherLogsRoot, "latest.log");
    public string LauncherCrashLogsRoot => Path.Combine(LauncherLogsRoot, "crashes");
    public string MsalTokenCachePath => Path.Combine(AuthRoot, "msal.cache");
    public string OnlineAccountProfilePath => Path.Combine(AuthRoot, "online-account.json");
    public string AvatarCachePath => Path.Combine(AuthRoot, "avatar.png");
    public string ManagedBridgeInstallRoot => Path.Combine(DataRoot, "bridge");
    public string ManagedBridgeLogsRoot => Path.Combine(ManagedBridgeInstallRoot, "logs");
    public string ManagedBridgeJarPath => Path.Combine(ManagedBridgeInstallRoot, "bootstrap-standalone.jar");
    public string ManagedBridgeMetadataPath => Path.Combine(ManagedBridgeInstallRoot, "release.json");
    public string ManagedBridgeDownloadPath => Path.Combine(DownloadsRoot, "bootstrap-standalone-latest.jar");

    public string GetBridgeRuntimeDirectory(string serverId) =>
        Path.Combine(RuntimeRoot, "bridges", serverId);

    public string GetClientInstallDirectory(string channel) =>
        Path.Combine(InstallsRoot, channel);

    public string GetManagedClientInstallRoot(ManagedClientStream stream) =>
        GetClientInstallDirectory(stream.GetInstallDirectoryName());

    public string GetManagedClientExecutablePath(ManagedClientStream stream) =>
        Path.Combine(GetManagedClientInstallRoot(stream), GetClientExecutableName());

    public string GetManagedClientMetadataPath(ManagedClientStream stream) =>
        Path.Combine(GetManagedClientInstallRoot(stream), "release.json");

    public string GetManagedClientDownloadPath(ManagedClientStream stream) => Path.Combine(
        DownloadsRoot,
        stream == ManagedClientStream.Debug ? "LCEDebug-nightly-win-x64.zip" : "LCEWindows64-nightly.zip");

    // The client binary name differs by platform only if a Linux/Mac build ever ships.
    // For now the server is Windows-native; on other platforms users run it via Wine.
    private static string GetClientExecutableName() => "Minecraft.Client.exe";

    private static string GetPlatformDataRoot()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Minecraft Legacy Edition", "Launcher");
        }

        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "Minecraft Legacy Edition", "Launcher");
        }

        // Linux — respect XDG_DATA_HOME
        var xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrWhiteSpace(xdgData))
        {
            xdgData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return Path.Combine(xdgData, "minecraft-legacy-edition", "launcher");
    }
}
