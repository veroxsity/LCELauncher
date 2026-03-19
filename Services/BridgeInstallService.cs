using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class BridgeInstallService
{
    private readonly AppPaths _paths;
    private readonly LauncherLogger _logger;

    public BridgeInstallService(AppPaths paths, LauncherLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public ManagedBridgeInstallInfo GetManagedInstallInfo()
    {
        var jarPath = _paths.ManagedBridgeJarPath;
        var fileInfo = File.Exists(jarPath) ? new FileInfo(jarPath) : null;

        return new ManagedBridgeInstallInfo
        {
            IsInstalled = fileInfo is not null,
            InstallRoot = _paths.ManagedBridgeInstallRoot,
            BridgeJarPath = jarPath,
            DisplayVersion = fileInfo is null ? "Bridge runtime" : Path.GetFileNameWithoutExtension(fileInfo.Name),
            InstalledAtUtc = fileInfo?.LastWriteTimeUtc,
        };
    }

    public async Task<ManagedBridgeInstallInfo> EnsureManagedBridgeInstalledAsync(CancellationToken cancellationToken)
    {
        var existing = GetManagedInstallInfo();
        if (existing.IsInstalled)
        {
            _logger.Info($"Using managed bridge runtime at {existing.BridgeJarPath}.");
            return existing;
        }

        var sourcePath = FindSourceBridgeJar();
        if (sourcePath is null)
        {
            throw new InvalidOperationException(
                "No bridge runtime source was found. Bundle bootstrap-standalone.jar with the launcher or build the bridge repo locally.");
        }

        Directory.CreateDirectory(_paths.ManagedBridgeInstallRoot);
        var destinationPath = _paths.ManagedBridgeJarPath;
        var tempPath = $"{destinationPath}.download";

        await using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        await using (var destination = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await source.CopyToAsync(destination, cancellationToken);
        }

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        File.Move(tempPath, destinationPath);
        _logger.Info($"Installed managed bridge runtime from {sourcePath} to {destinationPath}.");
        return GetManagedInstallInfo();
    }

    private string? FindSourceBridgeJar()
    {
        var bundledCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Bridge", "bootstrap-standalone.jar"),
            Path.Combine(AppContext.BaseDirectory, "bridge", "bootstrap-standalone.jar"),
            Path.Combine(AppContext.BaseDirectory, "bootstrap-standalone.jar"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "bootstrap-standalone.jar"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "bootstrap-standalone.jar"),
        };

        foreach (var candidate in bundledCandidates.Select(Path.GetFullPath))
        {
            if (File.Exists(candidate))
            {
                _logger.Info($"Found bundled bridge runtime candidate at {candidate}.");
                return candidate;
            }
        }

        var repoRoot = TryFindRepoRoot(AppContext.BaseDirectory);
        if (repoRoot is null)
        {
            return null;
        }

        var preferred = Path.Combine(repoRoot, "bridge", "scripts", "output");
        var fallback = Path.Combine(repoRoot, "bridge", "_build", "bootstrap-standalone", "libs");
        var bridgeJar =
            TryFindLatestFile(preferred, "*.jar") ??
            TryFindLatestFile(fallback, "*.jar");

        if (bridgeJar is not null)
        {
            _logger.Info($"Falling back to workspace bridge jar at {bridgeJar}.");
        }

        return bridgeJar;
    }

    private static string? TryFindRepoRoot(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);
        while (current is not null)
        {
            var hasMarkers =
                Directory.Exists(Path.Combine(current.FullName, ".git")) &&
                Directory.Exists(Path.Combine(current.FullName, "bridge"));

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
}
