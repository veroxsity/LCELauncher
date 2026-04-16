using System.Diagnostics;
using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class LaunchCoordinator
{
    private readonly ServerManager _serverManager;
    private readonly ClientProfileManager _clientProfileManager;
    private readonly BridgeInstallService _bridgeInstallService;
    private readonly LauncherAuthService _launcherAuthService;
    private readonly BridgeRuntimeManager _bridgeRuntimeManager;
    private readonly LauncherLogger _logger;

    public LaunchCoordinator(
        ServerManager serverManager,
        ClientProfileManager clientProfileManager,
        BridgeInstallService bridgeInstallService,
        LauncherAuthService launcherAuthService,
        BridgeRuntimeManager bridgeRuntimeManager,
        LauncherLogger logger)
    {
        _serverManager = serverManager;
        _clientProfileManager = clientProfileManager;
        _bridgeInstallService = bridgeInstallService;
        _launcherAuthService = launcherAuthService;
        _bridgeRuntimeManager = bridgeRuntimeManager;
        _logger = logger;
    }

    public async Task LaunchAsync(LauncherConfig config, ServerEntry? selectedServer, CancellationToken cancellationToken)
    {
        _serverManager.Normalize(config);

        BridgeAuthContext? authContext = null;
        if (config.AuthMode == AuthMode.Online)
        {
            authContext = await _launcherAuthService.GetBridgeAuthContextAsync(cancellationToken);
        }

        _clientProfileManager.PrepareClientFiles(config, authContext?.MinecraftUsername);

        if (selectedServer?.Type == ServerType.JavaBridge)
        {
            if (config.PreferManagedBridgeInstall)
            {
                var bridgeInstall = await _bridgeInstallService.EnsureManagedBridgeInstalledAsync(cancellationToken);
                config.BridgeJarPath = bridgeInstall.BridgeJarPath;
            }

            if (selectedServer.RequiresOnlineAuth && config.AuthMode != AuthMode.Online)
            {
                throw new InvalidOperationException(
                    "This server requires online auth. Sign in with Microsoft and switch to online auth mode first.");
            }

            await _bridgeRuntimeManager.EnsureRunningAsync(config, selectedServer, authContext, cancellationToken);
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
