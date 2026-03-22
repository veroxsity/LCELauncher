using System.Diagnostics;
using System.Net.Sockets;
using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class BridgeRuntimeManager
{
    private readonly AppPaths _paths;
    private readonly LauncherLogger _logger;
    private ManagedBridgeProcess? _activeBridge;

    public BridgeRuntimeManager(AppPaths paths, LauncherLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public string StatusText =>
        _activeBridge is null
            ? "No active bridge"
            : $"{_activeBridge.ServerDisplayName} on 127.0.0.1:{_activeBridge.LocalPort}";

    public async Task EnsureRunningAsync(LauncherConfig config, ServerEntry server, BridgeAuthContext? authContext, CancellationToken cancellationToken)
    {
        if (server.Type != ServerType.JavaBridge || server.LocalBridgePort is null)
        {
            throw new InvalidOperationException("Only Java bridge-backed servers can start a bridge runtime.");
        }

        if (_activeBridge is not null &&
            !_activeBridge.Process.HasExited &&
            _activeBridge.ServerId == server.Id &&
            _activeBridge.LocalPort == server.LocalBridgePort.Value)
        {
            _logger.Info($"Bridge already running for {server.DisplayName}.");
            return;
        }

        await StopAsync();

        var javaExecutable = string.IsNullOrWhiteSpace(config.JavaExecutablePath) ? "java" : config.JavaExecutablePath.Trim();
        var bridgeJarPath = config.BridgeJarPath?.Trim();
        if (string.IsNullOrWhiteSpace(bridgeJarPath))
        {
            throw new InvalidOperationException("Bridge jar path is not configured.");
        }

        if (!File.Exists(bridgeJarPath))
        {
            throw new FileNotFoundException("Bridge jar was not found.", bridgeJarPath);
        }

        var runtimeDirectory = _paths.GetBridgeRuntimeDirectory(server.Id);
        Directory.CreateDirectory(runtimeDirectory);

        var configPath = Path.Combine(runtimeDirectory, "config.yml");
        File.WriteAllText(configPath, BridgeConfigRenderer.Render(server, config, authContext));

        var startInfo = new ProcessStartInfo
        {
            FileName = javaExecutable,
            WorkingDirectory = runtimeDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-jar");
        startInfo.ArgumentList.Add(bridgeJarPath);
        startInfo.ArgumentList.Add(configPath);

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        process.Exited += (_, _) => _logger.Warn($"Bridge process exited for {server.DisplayName}.");

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the bridge process.");
        }

        _activeBridge = new ManagedBridgeProcess(server.Id, server.DisplayName, server.LocalBridgePort.Value, runtimeDirectory, configPath, process);
        AttachLogging(process, server.DisplayName);
        _logger.Info($"Starting bridge for {server.DisplayName} on 127.0.0.1:{server.LocalBridgePort.Value}.");

        var started = await WaitForPortAsync(server.LocalBridgePort.Value, process, cancellationToken);
        if (!started)
        {
            await StopAsync();
            throw new InvalidOperationException($"Bridge did not become ready on port {server.LocalBridgePort.Value}.");
        }

        _logger.Info($"Bridge is ready for {server.DisplayName}.");
    }

    public async Task StopAsync()
    {
        if (_activeBridge is null)
        {
            return;
        }

        var bridge = _activeBridge;
        _activeBridge = null;

        if (bridge.Process.HasExited)
        {
            return;
        }

        _logger.Info($"Stopping bridge for {bridge.ServerDisplayName}.");
        try
        {
            bridge.Process.Kill(entireProcessTree: true);
            await bridge.Process.WaitForExitAsync();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void AttachLogging(Process process, string displayName)
    {
        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                _logger.Info($"[Bridge:{displayName}] {eventArgs.Data}");
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (!string.IsNullOrWhiteSpace(eventArgs.Data))
            {
                _logger.Warn($"[Bridge:{displayName}] {eventArgs.Data}");
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    private static async Task<bool> WaitForPortAsync(int port, Process process, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

        while (!linked.IsCancellationRequested)
        {
            if (process.HasExited)
            {
                return false;
            }

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync("127.0.0.1", port, linked.Token);
                return true;
            }
            catch
            {
                try
                {
                    await Task.Delay(250, linked.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        return false;
    }

    private sealed record ManagedBridgeProcess(
        string ServerId,
        string ServerDisplayName,
        int LocalPort,
        string RuntimeDirectory,
        string ConfigPath,
        Process Process);
}
