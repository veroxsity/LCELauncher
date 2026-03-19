using System.Text.Json;
using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class LauncherConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly AppPaths _paths;
    private readonly LauncherLogger _logger;

    public LauncherConfigService(AppPaths paths, LauncherLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public LauncherConfig Load()
    {
        Directory.CreateDirectory(_paths.DataRoot);
        Directory.CreateDirectory(_paths.RuntimeRoot);
        Directory.CreateDirectory(_paths.DownloadsRoot);
        Directory.CreateDirectory(_paths.InstallsRoot);

        LauncherConfig config;
        if (File.Exists(_paths.ConfigPath))
        {
            var json = File.ReadAllText(_paths.ConfigPath);
            config = JsonSerializer.Deserialize<LauncherConfig>(json, JsonOptions) ?? new LauncherConfig();
            _logger.Info($"Loaded launcher config from {_paths.ConfigPath}");
        }
        else
        {
            config = new LauncherConfig();
            _logger.Info($"Creating new launcher config at {_paths.ConfigPath}");
        }

        ApplyDiscoveredDefaults(config);
        return config;
    }

    public void Save(LauncherConfig config)
    {
        Directory.CreateDirectory(_paths.DataRoot);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_paths.ConfigPath, json);
        _logger.Info($"Saved launcher config to {_paths.ConfigPath}");
    }

    private void ApplyDiscoveredDefaults(LauncherConfig config)
    {
        var repoRoot = TryFindRepoRoot(AppContext.BaseDirectory);

        if (config.PreferManagedClientInstall)
        {
            if (File.Exists(_paths.NightlyClientExecutablePath))
            {
                if (!PathsEqual(config.ClientExecutablePath, _paths.NightlyClientExecutablePath))
                {
                    config.ClientExecutablePath = _paths.NightlyClientExecutablePath;
                    _logger.Info($"Using managed nightly client install at {_paths.NightlyClientExecutablePath}");
                }
            }
            else if (repoRoot is not null && IsWorkspaceClientPath(repoRoot, config.ClientExecutablePath))
            {
                config.ClientExecutablePath = null;
                _logger.Info("Cleared workspace client path so the launcher can use managed installs instead.");
            }
        }

        if (config.PreferManagedBridgeInstall)
        {
            if (File.Exists(_paths.ManagedBridgeJarPath))
            {
                if (!PathsEqual(config.BridgeJarPath, _paths.ManagedBridgeJarPath))
                {
                    config.BridgeJarPath = _paths.ManagedBridgeJarPath;
                    _logger.Info($"Using managed bridge runtime at {_paths.ManagedBridgeJarPath}");
                }
            }
            else if (repoRoot is not null && IsWorkspaceBridgePath(repoRoot, config.BridgeJarPath))
            {
                config.BridgeJarPath = null;
                _logger.Info("Cleared workspace bridge path so the launcher can use a managed bridge runtime instead.");
            }
        }

        if (repoRoot is null)
        {
            return;
        }

        if (!config.PreferManagedBridgeInstall && string.IsNullOrWhiteSpace(config.BridgeJarPath))
        {
            var preferred = Path.Combine(repoRoot, "bridge", "scripts", "output");
            var fallback = Path.Combine(repoRoot, "bridge", "_build", "bootstrap-standalone", "libs");
            var bridgeJar =
                TryFindLatestFile(preferred, "*.jar") ??
                TryFindLatestFile(fallback, "*.jar");

            if (bridgeJar is not null)
            {
                config.BridgeJarPath = bridgeJar;
                _logger.Info($"Auto-detected bridge jar: {bridgeJar}");
            }
        }
    }

    private static string? TryFindRepoRoot(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);
        while (current is not null)
        {
            var hasMarkers =
                Directory.Exists(Path.Combine(current.FullName, ".git")) &&
                Directory.Exists(Path.Combine(current.FullName, "bridge")) &&
                Directory.Exists(Path.Combine(current.FullName, "client"));

            if (hasMarkers)
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? TryFindLatestFile(string directory, string filter)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(directory, filter, SearchOption.AllDirectories)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Select(file => file.FullName)
            .FirstOrDefault();
    }

    private static bool IsWorkspaceClientPath(string repoRoot, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        var workspaceClientRoot = Path.GetFullPath(Path.Combine(repoRoot, "client")) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(workspaceClientRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWorkspaceBridgePath(string repoRoot, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        var workspaceBridgeRoot = Path.GetFullPath(Path.Combine(repoRoot, "bridge")) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(workspaceBridgeRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
    }
}
