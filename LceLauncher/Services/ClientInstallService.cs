using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class ClientInstallService
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly AppPaths _paths;
    private readonly LauncherLogger _logger;

    public ClientInstallService(AppPaths paths, LauncherLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public bool HasManagedInstall(ManagedClientStream stream) =>
        File.Exists(_paths.GetManagedClientExecutablePath(stream));

    public ManagedClientInstallInfo GetManagedInstallInfo(ManagedClientStream stream)
    {
        var metadata = ReadMetadata(stream);
        return new ManagedClientInstallInfo
        {
            Stream = stream,
            StreamLabel = stream.GetDisplayName(),
            IsInstalled = HasManagedInstall(stream),
            InstallRoot = _paths.GetManagedClientInstallRoot(stream),
            ClientExecutablePath = _paths.GetManagedClientExecutablePath(stream),
            DisplayVersion = metadata?.ReleaseName ?? GetDefinition(stream).DefaultVersionName,
            PublishedAtUtc = metadata?.PublishedAtUtc,
            InstalledAtUtc = metadata?.InstalledAtUtc,
        };
    }

    public async Task<ManagedClientUpdateInfo> GetUpdateInfoAsync(ManagedClientStream stream, CancellationToken cancellationToken)
    {
        var definition = GetDefinition(stream);
        var installInfo = GetManagedInstallInfo(stream);
        if (!installInfo.IsInstalled)
        {
            return new ManagedClientUpdateInfo
            {
                Stream = stream,
                StreamLabel = stream.GetDisplayName(),
                IsInstalled = false,
                StatusText = $"Managed {stream.GetDisplayName().ToLowerInvariant()} client is not installed.",
            };
        }

        var release = await FetchReleaseAsync(definition, cancellationToken);
        var metadata = ReadMetadata(stream);
        var current = metadata?.ReleaseName ?? installInfo.DisplayVersion;
        var needsUpdate = metadata is null
            || (!string.IsNullOrWhiteSpace(release.ZipAsset.Sha256)
                && !string.Equals(metadata.ZipSha256, release.ZipAsset.Sha256, StringComparison.OrdinalIgnoreCase))
            || metadata.PublishedAtUtc != release.PublishedAtUtc;

        if (definition.SupportsLightweightUpdates
            && metadata is not null
            && !string.IsNullOrWhiteSpace(release.ExecutableAsset?.Sha256)
            && !string.Equals(metadata.ExecutableSha256, release.ExecutableAsset.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            needsUpdate = true;
        }

        return new ManagedClientUpdateInfo
        {
            Stream = stream,
            StreamLabel = stream.GetDisplayName(),
            IsInstalled = true,
            CheckedRemotely = true,
            UpdateAvailable = needsUpdate,
            SupportsLightweightUpdate = definition.SupportsLightweightUpdates,
            CurrentVersion = current,
            LatestVersion = release.ReleaseName,
            LatestPublishedAtUtc = release.PublishedAtUtc,
            StatusText = needsUpdate
                ? $"Update available: {release.ReleaseName}"
                : $"Up to date: {release.ReleaseName}",
        };
    }

    public Task<ManagedClientInstallInfo> InstallAsync(
        ManagedClientStream stream,
        IProgress<(long received, long total)>? progress,
        CancellationToken cancellationToken) =>
        ReplaceManagedInstallFromZipAsync(stream, progress, cancellationToken,
            "Installing", "Installed",
            $"No managed {stream.GetDisplayName().ToLowerInvariant()} client install found.");

    public Task<ManagedClientInstallInfo> RepairAsync(
        ManagedClientStream stream,
        IProgress<(long received, long total)>? progress,
        CancellationToken cancellationToken) =>
        ReplaceManagedInstallFromZipAsync(stream, progress, cancellationToken,
            "Repairing", "Repaired",
            $"Managed {stream.GetDisplayName().ToLowerInvariant()} client install is missing.");

    public async Task<ManagedClientInstallInfo> UpdateAsync(
        ManagedClientStream stream,
        IProgress<(long received, long total)>? progress,
        CancellationToken cancellationToken)
    {
        var definition = GetDefinition(stream);
        if (!HasManagedInstall(stream))
            return await InstallAsync(stream, progress, cancellationToken);

        var release = await FetchReleaseAsync(definition, cancellationToken);
        var metadata = ReadMetadata(stream);
        var zipMatches = metadata is not null
            && string.Equals(metadata.ZipSha256, release.ZipAsset.Sha256, StringComparison.OrdinalIgnoreCase)
            && metadata.PublishedAtUtc == release.PublishedAtUtc;

        if (!definition.SupportsLightweightUpdates)
        {
            if (zipMatches) return GetManagedInstallInfo(stream);
            return await ReplaceManagedInstallFromZipAsync(stream, progress, cancellationToken,
                "Updating", "Updated", "Replacing install from latest zip.");
        }

        var execMatches = metadata is not null
            && string.Equals(metadata.ExecutableSha256, release.ExecutableAsset?.Sha256, StringComparison.OrdinalIgnoreCase);

        if (zipMatches && execMatches) return GetManagedInstallInfo(stream);

        if (release.ExecutableAsset is null)
        {
            return await ReplaceManagedInstallFromZipAsync(stream, progress, cancellationToken,
                "Updating", "Updated", "No lightweight asset found; falling back to full zip.");
        }

        _logger.Info($"Downloading {release.ExecutableAsset.Name} for lightweight update.");
        var targetPath = _paths.GetManagedClientExecutablePath(stream);
        var tempPath = $"{targetPath}.download";
        Directory.CreateDirectory(_paths.GetManagedClientInstallRoot(stream));

        await DownloadFileAsync(release.ExecutableAsset.DownloadUrl, targetPath, progress, cancellationToken);

        WriteMetadata(stream, new ManagedClientInstallMetadata(
            definition.StreamKey, release.ReleaseTag, release.ReleaseName,
            release.PublishedAtUtc, DateTimeOffset.UtcNow,
            release.ZipAsset.Sha256, release.ExecutableAsset.Sha256));

        _logger.Info($"Lightweight update complete at {targetPath}.");
        _ = tempPath; // suppress unused warning
        return GetManagedInstallInfo(stream);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<ManagedClientInstallInfo> ReplaceManagedInstallFromZipAsync(
        ManagedClientStream stream,
        IProgress<(long, long)>? progress,
        CancellationToken cancellationToken,
        string startVerb,
        string completeVerb,
        string logMessage)
    {
        var definition = GetDefinition(stream);
        var installRoot = _paths.GetManagedClientInstallRoot(stream);
        var downloadPath = _paths.GetManagedClientDownloadPath(stream);

        Directory.CreateDirectory(_paths.DownloadsRoot);
        Directory.CreateDirectory(_paths.InstallsRoot);

        var release = await FetchReleaseAsync(definition, cancellationToken);
        _logger.Info($"{startVerb} managed {stream.GetDisplayName().ToLowerInvariant()} client. {logMessage}");
        _logger.Info($"Downloading {release.ZipAsset.Name} from {release.ReleaseName}.");

        await DownloadFileAsync(release.ZipAsset.DownloadUrl, downloadPath, progress, cancellationToken);

        var stagingRoot = Path.Combine(_paths.InstallsRoot, $".{definition.StreamKey}-staging-{Guid.NewGuid():N}");
        var installStagingRoot = Path.Combine(_paths.InstallsRoot, $".{definition.StreamKey}-install-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingRoot);

        try
        {
            _logger.Info($"Extracting {stream.GetDisplayName().ToLowerInvariant()} client zip.");
            ZipFile.ExtractToDirectory(downloadPath, stagingRoot);

            var extractedRoot = FindInstallRoot(stagingRoot);
            CopyDirectory(extractedRoot, installStagingRoot);

            if (!File.Exists(Path.Combine(installStagingRoot, "Minecraft.Client.exe")))
                throw new InvalidOperationException($"{stream.GetDisplayName()} install did not contain Minecraft.Client.exe.");

            PreserveKnownUserFiles(installRoot, installStagingRoot);
            ReplaceDirectory(installRoot, installStagingRoot);

            WriteMetadata(stream, new ManagedClientInstallMetadata(
                definition.StreamKey, release.ReleaseTag, release.ReleaseName,
                release.PublishedAtUtc, DateTimeOffset.UtcNow,
                release.ZipAsset.Sha256, release.ExecutableAsset?.Sha256));

            _logger.Info($"{completeVerb} managed {stream.GetDisplayName().ToLowerInvariant()} client at {installRoot}.");
            return GetManagedInstallInfo(stream);
        }
        finally
        {
            TryDeleteDirectory(stagingRoot);
            TryDeleteDirectory(installStagingRoot);
        }
    }

    private async Task<ManagedClientRelease> FetchReleaseAsync(
        ManagedClientStreamDefinition definition, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(definition.ReleaseUri, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var assets = root.GetProperty("assets").EnumerateArray().ToArray();

        var zipAsset = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == definition.ZipAssetName);
        if (zipAsset.ValueKind == JsonValueKind.Undefined)
            throw new InvalidOperationException($"{definition.DisplayName} release assets are missing the expected zip.");

        JsonElement? exeAsset = null;
        if (!string.IsNullOrWhiteSpace(definition.ExecutableAssetName))
        {
            var candidate = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == definition.ExecutableAssetName);
            if (candidate.ValueKind != JsonValueKind.Undefined)
                exeAsset = candidate;
        }

        return new ManagedClientRelease(
            root.GetProperty("tag_name").GetString() ?? "nightly",
            root.GetProperty("name").GetString() ?? definition.DefaultVersionName,
            root.GetProperty("published_at").GetDateTimeOffset(),
            ParseAsset(zipAsset),
            exeAsset is null ? null : ParseAsset(exeAsset.Value));
    }

    private static ManagedClientAsset ParseAsset(JsonElement asset) => new(
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

    private static string FindInstallRoot(string extractionRoot)
    {
        var exe = Directory.EnumerateFiles(extractionRoot, "Minecraft.Client.exe", SearchOption.AllDirectories)
            .FirstOrDefault() ?? throw new InvalidOperationException("Extracted zip did not contain Minecraft.Client.exe.");
        return Path.GetDirectoryName(exe) ?? extractionRoot;
    }

    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var dir in Directory.EnumerateDirectories(src, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(dst, Path.GetRelativePath(src, dir)));
        foreach (var file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(src, file);
            var dstFile = Path.Combine(dst, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dstFile)!);
            File.Copy(file, dstFile, overwrite: true);
        }
    }

    private static void ReplaceDirectory(string target, string replacement)
    {
        if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        Directory.Move(replacement, target);
    }

    private static void PreserveKnownUserFiles(string existingRoot, string newRoot)
    {
        if (!Directory.Exists(existingRoot)) return;
        foreach (var name in new[] { "uid.dat", "username.txt", "servers.db" })
        {
            var src = Path.Combine(existingRoot, name);
            if (File.Exists(src))
                File.Copy(src, Path.Combine(newRoot, name), overwrite: true);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { }
    }

    private void WriteMetadata(ManagedClientStream stream, ManagedClientInstallMetadata metadata)
    {
        Directory.CreateDirectory(_paths.GetManagedClientInstallRoot(stream));
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_paths.GetManagedClientMetadataPath(stream), json);
    }

    private ManagedClientInstallMetadata? ReadMetadata(ManagedClientStream stream)
    {
        var path = _paths.GetManagedClientMetadataPath(stream);
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<ManagedClientInstallMetadata>(File.ReadAllText(path)); }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to read {stream.GetDisplayName().ToLowerInvariant()} install metadata: {ex.Message}");
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

    private static ManagedClientStreamDefinition GetDefinition(ManagedClientStream stream) => stream switch
    {
        ManagedClientStream.Release => new ManagedClientStreamDefinition(
            stream, stream.GetKey(), stream.GetDisplayName(), "Release Nightly",
            new Uri("https://api.github.com/repos/smartcmd/MinecraftConsoles/releases/tags/nightly"),
            "LCEWindows64.zip", "Minecraft.Client.exe", true),
        ManagedClientStream.Debug => new ManagedClientStreamDefinition(
            stream, stream.GetKey(), stream.GetDisplayName(), "Debug Nightly",
            new Uri("https://api.github.com/repos/veroxsity/LCEDebug/releases/tags/nightly"),
            "LCEDebug-nightly-win-x64.zip", null, false),
        _ => throw new ArgumentOutOfRangeException(nameof(stream), stream, null),
    };

    private sealed record ManagedClientStreamDefinition(
        ManagedClientStream Stream, string StreamKey, string DisplayName, string DefaultVersionName,
        Uri ReleaseUri, string ZipAssetName, string? ExecutableAssetName, bool SupportsLightweightUpdates);
    private sealed record ManagedClientRelease(
        string ReleaseTag, string ReleaseName, DateTimeOffset PublishedAtUtc,
        ManagedClientAsset ZipAsset, ManagedClientAsset? ExecutableAsset);
    private sealed record ManagedClientAsset(string Name, Uri DownloadUrl, string? Sha256);
    private sealed record ManagedClientInstallMetadata(
        string StreamKey, string ReleaseTag, string ReleaseName,
        DateTimeOffset PublishedAtUtc, DateTimeOffset InstalledAtUtc,
        string? ZipSha256, string? ExecutableSha256);
}
