using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class ClientProfileManager
{
    private readonly ServerManager _serverManager;
    private readonly LauncherLogger _logger;

    public ClientProfileManager(ServerManager serverManager, LauncherLogger logger)
    {
        _serverManager = serverManager;
        _logger = logger;
    }

    public string GetClientWorkingDirectory(LauncherConfig config)
    {
        var clientExe = RequireClientExecutable(config);
        return Path.GetDirectoryName(clientExe)!;
    }

    public string RequireClientExecutable(LauncherConfig config)
    {
        var clientExe = config.ClientExecutablePath?.Trim();
        if (string.IsNullOrWhiteSpace(clientExe))
        {
            throw new InvalidOperationException("Client executable path is not configured.");
        }

        if (!File.Exists(clientExe))
        {
            throw new FileNotFoundException("Client executable was not found.", clientExe);
        }

        return clientExe;
    }

    public void PrepareClientFiles(LauncherConfig config)
    {
        var workingDirectory = GetClientWorkingDirectory(config);
        Directory.CreateDirectory(workingDirectory);

        var username = string.IsNullOrWhiteSpace(config.LocalUsername) ? "Player" : config.LocalUsername.Trim();
        File.WriteAllText(Path.Combine(workingDirectory, "username.txt"), $"{username}{Environment.NewLine}");
        _logger.Info($"Updated username.txt with local name '{username}'.");

        var serverEntries = _serverManager.BuildClientServerEntries(config);
        ServerListWriter.Write(Path.Combine(workingDirectory, "servers.db"), serverEntries);
        _logger.Info($"Wrote servers.db with {serverEntries.Count} launcher-managed entries.");
        _logger.Info("Left uid.dat untouched; the client can generate it naturally if it is missing.");
    }
}
