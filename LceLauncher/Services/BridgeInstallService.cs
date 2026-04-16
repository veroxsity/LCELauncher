using System.Net.Http.Headers;
using System.Text.Json;
using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class BridgeInstallService
{
    private static readonly Uri LatestReleaseUri =
        new("https://api.github.com/repos/veroxsity/LCEBridge/releases/latest");
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly AppPaths _paths;
    private readonly LauncherLogger _logger;

    public BridgeInstallService(AppPaths paths, LauncherLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public ManagedBridgeInstallInfo GetManagedInstallInfo()
    {
        var metadata = ReadMetadata();
        return new ManagedBridgeInstallInfo
        {
            IsInstalled = File.Exists(_paths.ManagedBridgeJarPath),
            InstallRoot = _paths.ManagedBridgeInstallRoot,
            BridgeJarPath = _paths.ManagedBridgeJarPath,
            DisplayVersion = metadata?.ReleaseName ?? "Bridge runtime",
            PublishedAtUtc = metadata?.PublishedAtUtc,
            InstalledAtUtc = metadata?.InstalledAtUtc,
        };
    }

    public async Task<ManagedBridgeUpdateInfo> GetLatestBridgeUpdateInfoAsync(CancellationToken cancellationToken)
    {
        var info = GetManagedInstallInfo();
        if (!info.IsInstalled)
        {
            return new ManagedBridgeUpdateInfo
            {
                IsInstalled = false,
                StatusText = "Managed bridge runtime is not installed.",
            };
        }

        var release = await FetchLatestReleaseAsync(cancellationToken);
        var metadata = ReadMetadata();
        var current = metadata?.ReleaseName ?? info.DisplayVersion;
        var needsUpdate = metadata is null
            || (!string.IsNullOrWhiteSpace(release.JarAsset.Sha256)
                && !string.Equals(metadata.JarSha256, release.JarAsset.Sha256, StringComparison.OrdinalIgnoreCase))
            || metadata.PublishedAtUtc != release.PublishedAtUtc;

        return new ManagedBridgeUpdateInfo
        {
            IsInstalled = true,
            CheckedRemotely = true,
            UpdateAvailable = needsUpdate,
            CurrentVersion = current,
            LatestVersion = release.ReleaseName,
            LatestPublishedAtUtc = release.PublishedAtUtc,
            StatusText = needsUpdate
                ? $"Update available: {release.ReleaseName}"
                : $"Up to date: {release.ReleaseName}",
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
        return await InstallLatestReleaseAsync(null, cancellationToken);
    }

    public async Task<ManagedBridgeInstallInfo> InstallLatestReleaseAsync(
        IProgress<(long received, long total)>? progress,
        CancellationToken cancellationToken)
    {
        _logger.Info("Installing the latest LCEBridge release.");
        var release = await FetchLatestReleaseAsync(cancellationToken);
        return await InstallReleaseAsync(release, "Installing", "Installed", progress, cancellationToken);
    }

    public async Task<ManagedBridgeInstallInfo> UpdateBridgeAsync(
        IProgress<(long received, long total)>? progress,
        CancellationToken cancellationToken)
    {
        var existing = GetManagedInstallInfo();
        if (!existing.IsInstalled)
        {
            _logger.Info("No managed bridge install found. Falling back to install.");
            return await InstallLatestReleaseAsync(progress, cancellationToken);
        }

        var release = await FetchLatestReleaseAsync(cancellationToken);
        var metadata = ReadMetadata();
        if (metadata is not null
            && string.Equals(metadata.JarSha256, release.JarAsset.Sha256, StringComparison.OrdinalIgnoreCase)
            && metadata.PublishedAtUtc == release.PublishedAtUtc)
        {
            _logger.Info("Managed bridge runtime is already up to date.");
            return existing;
        }

        return await InstallReleaseAsync(release, "Updating", "Updated", progress, cancellationToken);
    }

    private async Task<ManagedBridgeInstallInfo> InstallReleaseAsync(
        BridgeRelease release,
        string startVerb,
        string completeVerb,
        IProgress<(long, long)>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_paths.DownloadsRoot);
        Directory.CreateDirectory(_paths.ManagedBridgeInstallRoot);

        _logger.Info($"{startVerb} managed bridge runtime from {release.ReleaseName}.");
        _logger.Info($"Downloading {release.JarAsset.Name}.");

        var downloadPath = await DownloadFileAsync(
            release.JarAsset.DownloadUrl, _paths.ManagedBridgeDownloadPath, progress, cancellationToken);

        var targetPath = _paths.ManagedBridgeJarPath;
        var tempPath = $"{targetPath}.download";

        await using (var src = new FileStream(downloadPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        await using (var dst = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await src.CopyToAsync(dst, cancellationToken);
        }

        if (File.Exists(targetPath)) File.Delete(targetPath);
        File.Move(tempPath, targetPath);

        WriteMetadata(new BridgeInstallMetadata(
            release.ReleaseTag, release.ReleaseName,
            release.PublishedAtUtc, DateTimeOffset.UtcNow, release.JarAsset.Sha256));

        _logger.Info($"{completeVerb} managed bridge runtime at {targetPath}.");
        return GetManagedInstallInfo();
    }

    private async Task<BridgeRelease> FetchLatestReleaseAsync(CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(LatestReleaseUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var asset = root.GetProperty("assets").EnumerateArray().FirstOrDefault(item =>
        {
            var name = item.GetProperty("name").GetString();
            return !string.IsNullOrWhiteSpace(name)
                && name.StartsWith("bootstrap-standalone", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".jar", StringComparison.OrdinalIgnoreCase);
        });

        if (asset.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException("Latest LCEBridge release is missing a bootstrap-standalone jar asset.");

        return new BridgeRelease(
            root.GetProperty("tag_name").GetString() ?? "latest",
            root.GetProperty("name").GetString() ?? "Latest LCEBridge Release",
            root.GetProperty("published_at").GetDateTimeOffset(),
            ParseAsset(asset));
    }

    private static BridgeAsset ParseAsset(JsonElement asset) => new(
        asset.GetProperty("name").GetString() ?? string.Empty,
        new Uri(asset.GetProperty("browser_download_url").GetString()
            ?? throw new InvalidOperationException("Missing browser download URL.")),
        asset.TryGetProperty("digest", out var d) ? d.GetString() : null);

    private static async Task<string> DownloadFileAsync(
        Uri downloadUri,
        string destinationPath,
        IProgress<(long received, long total)>? progress,
        CancellationToken cancellationToken)
    {
        var tempPath = $"{destinationPath}.download";
        using var response = await HttpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;

        await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var buffer = new byte[81920];
            long received = 0;
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                received += read;
                progress?.Report((received, total));
            }
        }

        if (File.Exists(destinationPath)) File.Delete(destinationPath);
        File.Move(tempPath, destinationPath);
        return destinationPath;
    }

    private void WriteMetadata(BridgeInstallMetadata metadata)
    {
        Directory.CreateDirectory(_paths.ManagedBridgeInstallRoot);
        var json = System.Text.Json.JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_paths.ManagedBridgeMetadataPath, json);
    }

    private BridgeInstallMetadata? ReadMetadata()
    {
        if (!File.Exists(_paths.ManagedBridgeMetadataPath)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<BridgeInstallMetadata>(
                File.ReadAllText(_paths.ManagedBridgeMetadataPath));
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to read bridge install metadata: {ex.Message}");
            return null;
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LCELauncher", "0.1"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private sealed record BridgeRelease(
        string ReleaseTag, string ReleaseName, DateTimeOffset PublishedAtUtc, BridgeAsset JarAsset);
    private sealed record BridgeAsset(string Name, Uri DownloadUrl, string? Sha256);
    private sealed record BridgeInstallMetadata(
        string ReleaseTag, string ReleaseName, DateTimeOffset PublishedAtUtc,
        DateTimeOffset InstalledAtUtc, string? JarSha256);
}
