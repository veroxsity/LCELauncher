using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class ClientProfileManager
{
    private readonly AppPaths _paths;
    private readonly ServerManager _serverManager;
    private readonly LauncherLogger _logger;

    public ClientProfileManager(AppPaths paths, ServerManager serverManager, LauncherLogger logger)
    {
        _paths = paths;
        _serverManager = serverManager;
        _logger = logger;
    }

    public string GetClientWorkingDirectory(LauncherConfig config) =>
        Path.GetDirectoryName(RequireClientExecutable(config))!;

    /// <summary>
    /// Returns the path to the client executable that will actually be launched.
    /// If managed installs are preferred, resolves the path from the configured launch stream.
    /// Falls back to the manually-configured ClientExecutablePath otherwise.
    /// </summary>
    public string RequireClientExecutable(LauncherConfig config)
    {
        if (config.PreferManagedClientInstall)
        {
            var managedPath = _paths.GetManagedClientExecutablePath(config.ManagedClientLaunchStream);
            if (!File.Exists(managedPath))
                throw new FileNotFoundException(
                    $"Managed {config.ManagedClientLaunchStream.GetDisplayName()} client is not installed. " +
                    "Go to the Downloads tab and install it first.", managedPath);
            return managedPath;
        }

        var clientExe = config.ClientExecutablePath?.Trim();
        if (string.IsNullOrWhiteSpace(clientExe))
            throw new InvalidOperationException(
                "No client executable configured. Either enable managed installs or set a custom client path in Settings.");
        if (!File.Exists(clientExe))
            throw new FileNotFoundException("Client executable was not found at the configured path.", clientExe);
        return clientExe;
    }

    public void PrepareClientFiles(LauncherConfig config, string? onlineUsername = null)
    {
        var workingDirectory = GetClientWorkingDirectory(config);
        Directory.CreateDirectory(workingDirectory);

        var username = !string.IsNullOrWhiteSpace(onlineUsername)
            && config.AuthMode == AuthMode.Online
            && config.SyncUsernameFromOnlineAccount
                ? onlineUsername.Trim()
                : string.IsNullOrWhiteSpace(config.LocalUsername) ? "Player" : config.LocalUsername.Trim();

        File.WriteAllText(Path.Combine(workingDirectory, "username.txt"), $"{username}{Environment.NewLine}");
        _logger.Info($"Updated username.txt with name '{username}'.");

        var serverEntries = _serverManager.BuildClientServerEntries(config);
        ServerListWriter.Write(Path.Combine(workingDirectory, "servers.db"), serverEntries);
        _logger.Info($"Wrote servers.db with {serverEntries.Count} launcher-managed entries.");
    }
}
