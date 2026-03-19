using System.Diagnostics;
using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class LaunchCoordinator
{
    private readonly ServerManager _serverManager;
    private readonly ClientProfileManager _clientProfileManager;
    private readonly BridgeInstallService _bridgeInstallService;
    private readonly BridgeRuntimeManager _bridgeRuntimeManager;
    private readonly LauncherLogger _logger;

    public LaunchCoordinator(
        ServerManager serverManager,
        ClientProfileManager clientProfileManager,
        BridgeInstallService bridgeInstallService,
        BridgeRuntimeManager bridgeRuntimeManager,
        LauncherLogger logger)
    {
        _serverManager = serverManager;
        _clientProfileManager = clientProfileManager;
        _bridgeInstallService = bridgeInstallService;
        _bridgeRuntimeManager = bridgeRuntimeManager;
        _logger = logger;
    }

    public async Task LaunchAsync(LauncherConfig config, ServerEntry? selectedServer, CancellationToken cancellationToken)
    {
        _serverManager.Normalize(config);

        if (config.AuthMode == AuthMode.Online)
        {
            throw new InvalidOperationException("Online auth is planned for phase two and is not implemented yet.");
        }

        _clientProfileManager.PrepareClientFiles(config);

        if (selectedServer?.Type == ServerType.JavaBridge)
        {
            if (config.PreferManagedBridgeInstall)
            {
                var bridgeInstall = await _bridgeInstallService.EnsureManagedBridgeInstalledAsync(cancellationToken);
                config.BridgeJarPath = bridgeInstall.BridgeJarPath;
            }

            if (selectedServer.RequiresOnlineAuth)
            {
                throw new InvalidOperationException("This server is marked as requiring online auth. Phase one only supports local auth flow.");
            }

            await _bridgeRuntimeManager.EnsureRunningAsync(config, selectedServer, cancellationToken);
        }
        else
        {
            await _bridgeRuntimeManager.StopAsync();
        }

        var clientExe = _clientProfileManager.RequireClientExecutable(config);
        var workingDirectory = _clientProfileManager.GetClientWorkingDirectory(config);

        var startInfo = new ProcessStartInfo
        {
            FileName = clientExe,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true,
            Arguments = config.LaunchArguments?.Trim() ?? string.Empty,
        };

        Process.Start(startInfo);
        _logger.Info($"Launched client from {clientExe}");
    }
}
