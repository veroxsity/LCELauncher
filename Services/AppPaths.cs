namespace LceLauncher.Services;

public sealed class AppPaths
{
    public AppPaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DataRoot = Path.Combine(localAppData, "Minecraft Legacy Edition", "Launcher");
        ConfigPath = Path.Combine(DataRoot, "config.json");
        RuntimeRoot = Path.Combine(DataRoot, "runtime");
    }

    public string DataRoot { get; }

    public string ConfigPath { get; }

    public string RuntimeRoot { get; }

    public string GetBridgeRuntimeDirectory(string serverId) => Path.Combine(RuntimeRoot, "bridges", serverId);
}
