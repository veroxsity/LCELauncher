using System.Text.Json;
using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class LauncherConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

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
        Directory.CreateDirectory(_paths.AuthRoot);

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
        if (string.IsNullOrWhiteSpace(config.ManagedBridgeLogLevel))
        {
            config.ManagedBridgeLogLevel = "info";
        }

        if (string.IsNullOrWhiteSpace(config.MicrosoftAuthClientId))
        {
            config.MicrosoftAuthClientId = LauncherAuthService.DefaultCompatibilityClientId;
        }

        MigrateLegacyManagedClientUpdateState(config);

        var preferredManagedClientPath = _paths.GetManagedClientExecutablePath(config.ManagedClientLaunchStream);

        if (config.PreferManagedClientInstall && File.Exists(preferredManagedClientPath))
        {
            if (!PathsEqual(config.ClientExecutablePath, preferredManagedClientPath))
            {
                config.ClientExecutablePath = preferredManagedClientPath;
                _logger.Info($"Using managed {config.ManagedClientLaunchStream.GetDisplayName().ToLowerInvariant()} client at {preferredManagedClientPath}");
            }
        }

        if (config.PreferManagedBridgeInstall && File.Exists(_paths.ManagedBridgeJarPath))
        {
            if (!PathsEqual(config.BridgeJarPath, _paths.ManagedBridgeJarPath))
            {
                config.BridgeJarPath = _paths.ManagedBridgeJarPath;
                _logger.Info($"Using managed bridge runtime at {_paths.ManagedBridgeJarPath}");
            }
        }
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static void MigrateLegacyManagedClientUpdateState(LauncherConfig config)
    {
        var key = config.ManagedClientLaunchStream.GetKey();
        if (config.ManagedClientUpdateStates.ContainsKey(key)) return;

        var hasLegacy = config.ManagedClientLastCheckedAtUtc is not null
            || !string.IsNullOrWhiteSpace(config.ManagedClientLastUpdateStatusText)
            || config.ManagedClientLastUpdateAvailable
            || !string.IsNullOrWhiteSpace(config.ManagedClientLastKnownLatestVersion)
            || !string.IsNullOrWhiteSpace(config.ManagedClientLastNotifiedVersion);

        if (!hasLegacy) return;

        config.ManagedClientUpdateStates[key] = new ManagedClientUpdateState
        {
            LastCheckedAtUtc = config.ManagedClientLastCheckedAtUtc,
            LastUpdateStatusText = config.ManagedClientLastUpdateStatusText,
            LastUpdateAvailable = config.ManagedClientLastUpdateAvailable,
            LastKnownLatestVersion = config.ManagedClientLastKnownLatestVersion,
            LastNotifiedVersion = config.ManagedClientLastNotifiedVersion,
        };
    }
}
