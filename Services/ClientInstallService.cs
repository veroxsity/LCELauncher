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

    public bool HasManagedInstall(ManagedClientStream stream) => File.Exists(_paths.GetManagedClientExecutablePath(stream));

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
                CheckedRemotely = false,
                UpdateAvailable = false,
                SupportsLightweightUpdate = definition.SupportsLightweightUpdates,
                CurrentVersion = "Not installed",
                LatestVersion = definition.DefaultVersionName,
                StatusText = $"Managed {stream.GetDisplayName().ToLowerInvariant()} client is not installed.",
            };
        }

        var release = await FetchReleaseAsync(definition, cancellationToken);
        var metadata = ReadMetadata(stream);
        var currentVersion = metadata?.ReleaseName ?? installInfo.DisplayVersion;
        var updateAvailable = metadata is null ||
            (!string.IsNullOrWhiteSpace(release.ZipAsset.Sha256) &&
             !string.Equals(metadata.ZipSha256, release.ZipAsset.Sha256, StringComparison.OrdinalIgnoreCase)) ||
            metadata.PublishedAtUtc != release.PublishedAtUtc;

        if (definition.SupportsLightweightUpdates &&
            metadata is not null &&
            !string.IsNullOrWhiteSpace(release.ExecutableAsset?.Sha256) &&
            !string.Equals(metadata.ExecutableSha256, release.ExecutableAsset.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            updateAvailable = true;
        }

        return new ManagedClientUpdateInfo
        {
            Stream = stream,
            StreamLabel = stream.GetDisplayName(),
            IsInstalled = true,
            CheckedRemotely = true,
            UpdateAvailable = updateAvailable,
            SupportsLightweightUpdate = definition.SupportsLightweightUpdates,
            CurrentVersion = currentVersion,
            LatestVersion = release.ReleaseName,
            LatestPublishedAtUtc = release.PublishedAtUtc,
            StatusText = updateAvailable
                ? $"Update available for {stream.GetDisplayName().ToLowerInvariant()}: {release.ReleaseName}"
                : $"Up to date: {release.ReleaseName}",
        };
    }

    public Task<ManagedClientInstallInfo> InstallAsync(ManagedClientStream stream, CancellationToken cancellationToken)
    {
        return ReplaceManagedInstallFromZipAsync(
            stream,
            cancellationToken,
            "Installing",
            "Installed",
            $"No managed {stream.GetDisplayName().ToLowerInvariant()} client install found. Installing the full package.");
    }

    public Task<ManagedClientInstallInfo> RepairAsync(ManagedClientStream stream, CancellationToken cancellationToken)
    {
        return ReplaceManagedInstallFromZipAsync(
            stream,
            cancellationToken,
            "Repairing",
            "Repaired",
            $"Managed {stream.GetDisplayName().ToLowerInvariant()} client install is missing or incomplete. Repairing it from the full package.");
    }

    public async Task<ManagedClientInstallInfo> UpdateAsync(ManagedClientStream stream, CancellationToken cancellationToken)
    {
        var definition = GetDefinition(stream);
        if (!HasManagedInstall(stream))
        {
            _logger.Info($"No managed {stream.GetDisplayName().ToLowerInvariant()} client install found. Falling back to full install.");
            return await InstallAsync(stream, cancellationToken);
        }

        var release = await FetchReleaseAsync(definition, cancellationToken);
        var metadata = ReadMetadata(stream);
        var zipMatches = metadata is not null &&
            string.Equals(metadata.ZipSha256, release.ZipAsset.Sha256, StringComparison.OrdinalIgnoreCase) &&
            metadata.PublishedAtUtc == release.PublishedAtUtc;

        if (!definition.SupportsLightweightUpdates)
        {
            if (zipMatches)
            {
                _logger.Info($"Managed {stream.GetDisplayName().ToLowerInvariant()} client is already up to date.");
                return GetManagedInstallInfo(stream);
            }

            _logger.Info($"Managed {stream.GetDisplayName().ToLowerInvariant()} client requires a full zip update.");
            return await ReplaceManagedInstallFromZipAsync(
                stream,
                cancellationToken,
                "Updating",
                "Updated",
                $"Managed {stream.GetDisplayName().ToLowerInvariant()} client install exists. Replacing it from the latest zip.");
        }

        var executableShaMatches = metadata is not null &&
            string.Equals(metadata.ExecutableSha256, release.ExecutableAsset?.Sha256, StringComparison.OrdinalIgnoreCase);

        if (zipMatches && executableShaMatches)
        {
            _logger.Info($"Managed {stream.GetDisplayName().ToLowerInvariant()} client is already up to date.");
            return GetManagedInstallInfo(stream);
        }

        if (release.ExecutableAsset is null)
        {
            _logger.Info($"No lightweight executable asset was found for {stream.GetDisplayName().ToLowerInvariant()}. Falling back to full zip update.");
            return await ReplaceManagedInstallFromZipAsync(
                stream,
                cancellationToken,
                "Updating",
                "Updated",
                $"Managed {stream.GetDisplayName().ToLowerInvariant()} client install exists. Replacing it from the latest zip.");
        }

        _logger.Info($"Downloading {release.ExecutableAsset.Name} for lightweight {stream.GetDisplayName().ToLowerInvariant()} update.");

        Directory.CreateDirectory(_paths.GetManagedClientInstallRoot(stream));
        var targetPath = _paths.GetManagedClientExecutablePath(stream);
        var tempPath = $"{targetPath}.download";

        using var response = await HttpClient.GetAsync(release.ExecutableAsset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using (var streamHandle = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await streamHandle.CopyToAsync(file, cancellationToken);
        }

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        File.Move(tempPath, targetPath);
        WriteMetadata(stream, new ManagedClientInstallMetadata(
            definition.StreamKey,
            release.ReleaseTag,
            release.ReleaseName,
            release.PublishedAtUtc,
            DateTimeOffset.UtcNow,
            release.ZipAsset.Sha256,
            release.ExecutableAsset.Sha256));

        _logger.Info($"Updated managed {stream.GetDisplayName().ToLowerInvariant()} executable at {targetPath}.");
        return GetManagedInstallInfo(stream);
    }

    private async Task<ManagedClientInstallInfo> ReplaceManagedInstallFromZipAsync(
        ManagedClientStream stream,
        CancellationToken cancellationToken,
        string startVerb,
        string completeVerb,
        string missingInstallLogMessage)
    {
        var definition = GetDefinition(stream);
        var installRoot = _paths.GetManagedClientInstallRoot(stream);
        var downloadPath = _paths.GetManagedClientDownloadPath(stream);

        Directory.CreateDirectory(_paths.DownloadsRoot);
        Directory.CreateDirectory(_paths.InstallsRoot);

        if (!HasManagedInstall(stream))
        {
            _logger.Info(missingInstallLogMessage);
        }

        var release = await FetchReleaseAsync(definition, cancellationToken);
        _logger.Info($"{startVerb} managed {stream.GetDisplayName().ToLowerInvariant()} client from {release.ReleaseName}.");
        _logger.Info($"Downloading {release.ZipAsset.Name} from {release.ReleaseName}.");

        var downloadedPath = await DownloadFileAsync(release.ZipAsset.DownloadUrl, downloadPath, cancellationToken);
        _logger.Info($"Saved {stream.GetDisplayName().ToLowerInvariant()} client zip to {downloadedPath}.");

        var stagingRoot = Path.Combine(_paths.InstallsRoot, $".{definition.StreamKey}-staging-{Guid.NewGuid():N}");
        var installStagingRoot = Path.Combine(_paths.InstallsRoot, $".{definition.StreamKey}-install-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingRoot);

        try
        {
            _logger.Info($"Extracting {stream.GetDisplayName().ToLowerInvariant()} client zip.");
            ZipFile.ExtractToDirectory(downloadedPath, stagingRoot);

            var extractedRoot = FindInstallRoot(stagingRoot);
            CopyDirectory(extractedRoot, installStagingRoot);

            if (!File.Exists(Path.Combine(installStagingRoot, "Minecraft.Client.exe")))
            {
                throw new InvalidOperationException($"{stream.GetDisplayName()} install did not contain Minecraft.Client.exe.");
            }

            PreserveKnownUserFiles(installRoot, installStagingRoot);

            ReplaceDirectory(installRoot, installStagingRoot);
            WriteMetadata(stream, new ManagedClientInstallMetadata(
                definition.StreamKey,
                release.ReleaseTag,
                release.ReleaseName,
                release.PublishedAtUtc,
                DateTimeOffset.UtcNow,
                release.ZipAsset.Sha256,
                release.ExecutableAsset?.Sha256));

            _logger.Info($"{completeVerb} managed {stream.GetDisplayName().ToLowerInvariant()} client at {installRoot}.");
            return GetManagedInstallInfo(stream);
        }
        finally
        {
            TryDeleteDirectory(stagingRoot);
            TryDeleteDirectory(installStagingRoot);
        }
    }

    private async Task<ManagedClientRelease> FetchReleaseAsync(ManagedClientStreamDefinition definition, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(definition.ReleaseUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var assets = root.GetProperty("assets").EnumerateArray().ToArray();

        var zipAsset = assets.FirstOrDefault(asset => asset.GetProperty("name").GetString() == definition.ZipAssetName);
        if (zipAsset.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"{definition.DisplayName} release assets are missing the expected zip download.");
        }

        JsonElement? executableAssetElement = null;
        if (!string.IsNullOrWhiteSpace(definition.ExecutableAssetName))
        {
            var candidate = assets.FirstOrDefault(asset => asset.GetProperty("name").GetString() == definition.ExecutableAssetName);
            if (candidate.ValueKind != JsonValueKind.Undefined)
            {
                executableAssetElement = candidate;
            }
        }

        return new ManagedClientRelease(
            root.GetProperty("tag_name").GetString() ?? "nightly",
            root.GetProperty("name").GetString() ?? definition.DefaultVersionName,
            root.GetProperty("published_at").GetDateTimeOffset(),
            ParseAsset(zipAsset),
            executableAssetElement is null ? null : ParseAsset(executableAssetElement.Value));
    }

    private static ManagedClientAsset ParseAsset(JsonElement asset) =>
        new(
            asset.GetProperty("name").GetString() ?? string.Empty,
            new Uri(asset.GetProperty("browser_download_url").GetString() ?? throw new InvalidOperationException("Missing browser download URL.")),
            asset.TryGetProperty("digest", out var digestProperty) ? digestProperty.GetString() : null);

    private async Task<string> DownloadFileAsync(Uri downloadUri, string destinationPath, CancellationToken cancellationToken)
    {
        var tempPath = $"{destinationPath}.download";

        using var response = await HttpClient.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(file, cancellationToken);
        }

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        File.Move(tempPath, destinationPath);
        return destinationPath;
    }

    private static string FindInstallRoot(string extractionRoot)
    {
        var clientExe = Directory.EnumerateFiles(extractionRoot, "Minecraft.Client.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (clientExe is null)
        {
            throw new InvalidOperationException("Extracted client zip did not contain Minecraft.Client.exe.");
        }

        return Path.GetDirectoryName(clientExe) ?? extractionRoot;
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
    }

    private static void ReplaceDirectory(string targetDirectory, string replacementDirectory)
    {
        if (Directory.Exists(targetDirectory))
        {
            Directory.Delete(targetDirectory, recursive: true);
        }

        Directory.Move(replacementDirectory, targetDirectory);
    }

    private static void PreserveKnownUserFiles(string existingInstallRoot, string newInstallRoot)
    {
        if (!Directory.Exists(existingInstallRoot))
        {
            return;
        }

        foreach (var fileName in new[] { "uid.dat", "username.txt", "servers.db" })
        {
            var sourcePath = Path.Combine(existingInstallRoot, fileName);
            if (!File.Exists(sourcePath))
            {
                continue;
            }

            var destinationPath = Path.Combine(newInstallRoot, fileName);
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }

    private void WriteMetadata(ManagedClientStream stream, ManagedClientInstallMetadata metadata)
    {
        Directory.CreateDirectory(_paths.GetManagedClientInstallRoot(stream));
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_paths.GetManagedClientMetadataPath(stream), json);
    }

    private ManagedClientInstallMetadata? ReadMetadata(ManagedClientStream stream)
    {
        var metadataPath = _paths.GetManagedClientMetadataPath(stream);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ManagedClientInstallMetadata>(File.ReadAllText(metadataPath));
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to read {stream.GetDisplayName().ToLowerInvariant()} install metadata: {ex.Message}");
            return null;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
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
            stream,
            stream.GetKey(),
            stream.GetDisplayName(),
            "Release Nightly",
            new Uri("https://api.github.com/repos/smartcmd/MinecraftConsoles/releases/tags/nightly"),
            "LCEWindows64.zip",
            "Minecraft.Client.exe",
            true),
        ManagedClientStream.Debug => new ManagedClientStreamDefinition(
            stream,
            stream.GetKey(),
            stream.GetDisplayName(),
            "Debug Nightly",
            new Uri("https://api.github.com/repos/veroxsity/LCEDebug/releases/tags/nightly"),
            "LCEDebug-nightly-win-x64.zip",
            null,
            false),
        _ => throw new ArgumentOutOfRangeException(nameof(stream), stream, null),
    };

    private sealed record ManagedClientStreamDefinition(
        ManagedClientStream Stream,
        string StreamKey,
        string DisplayName,
        string DefaultVersionName,
        Uri ReleaseUri,
        string ZipAssetName,
        string? ExecutableAssetName,
        bool SupportsLightweightUpdates);

    private sealed record ManagedClientRelease(
        string ReleaseTag,
        string ReleaseName,
        DateTimeOffset PublishedAtUtc,
        ManagedClientAsset ZipAsset,
        ManagedClientAsset? ExecutableAsset);

    private sealed record ManagedClientAsset(string Name, Uri DownloadUrl, string? Sha256);

    private sealed record ManagedClientInstallMetadata(
        string StreamKey,
        string ReleaseTag,
        string ReleaseName,
        DateTimeOffset PublishedAtUtc,
        DateTimeOffset InstalledAtUtc,
        string? ZipSha256,
        string? ExecutableSha256);
}
