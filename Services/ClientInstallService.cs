using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using LceLauncher.Models;

namespace LceLauncher.Services;

public sealed class ClientInstallService
{
    private static readonly Uri NightlyReleaseUri = new("https://api.github.com/repos/smartcmd/MinecraftConsoles/releases/tags/nightly");
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly AppPaths _paths;
    private readonly LauncherLogger _logger;

    public ClientInstallService(AppPaths paths, LauncherLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public bool HasManagedInstall() => File.Exists(_paths.NightlyClientExecutablePath);

    public ManagedClientInstallInfo GetManagedInstallInfo()
    {
        NightlyInstallMetadata? metadata = null;
        if (File.Exists(_paths.NightlyMetadataPath))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<NightlyInstallMetadata>(File.ReadAllText(_paths.NightlyMetadataPath));
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to read nightly install metadata: {ex.Message}");
            }
        }

        return new ManagedClientInstallInfo
        {
            IsInstalled = HasManagedInstall(),
            InstallRoot = _paths.NightlyInstallRoot,
            ClientExecutablePath = _paths.NightlyClientExecutablePath,
            DisplayVersion = metadata?.ReleaseName ?? "Nightly",
            PublishedAtUtc = metadata?.PublishedAtUtc,
            InstalledAtUtc = metadata?.InstalledAtUtc,
        };
    }

    public async Task<ManagedClientInstallInfo> InstallNightlyAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_paths.DownloadsRoot);
        Directory.CreateDirectory(_paths.InstallsRoot);

        var release = await FetchNightlyReleaseAsync(cancellationToken);
        _logger.Info($"Downloading {release.ZipAsset.Name} from {release.ReleaseName}.");

        var downloadPath = await DownloadFileAsync(release.ZipAsset.DownloadUrl, _paths.NightlyDownloadPath, cancellationToken);
        _logger.Info($"Saved nightly zip to {downloadPath}.");

        var stagingRoot = Path.Combine(_paths.InstallsRoot, $".nightly-staging-{Guid.NewGuid():N}");
        var installRoot = Path.Combine(_paths.InstallsRoot, $".nightly-install-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingRoot);

        try
        {
            _logger.Info("Extracting nightly client zip.");
            ZipFile.ExtractToDirectory(downloadPath, stagingRoot);

            var extractedRoot = FindInstallRoot(stagingRoot);
            CopyDirectory(extractedRoot, installRoot);

            if (!File.Exists(Path.Combine(installRoot, "Minecraft.Client.exe")))
            {
                throw new InvalidOperationException("Nightly install did not contain Minecraft.Client.exe.");
            }

            PreserveKnownUserFiles(_paths.NightlyInstallRoot, installRoot);

            ReplaceDirectory(_paths.NightlyInstallRoot, installRoot);
            WriteMetadata(new NightlyInstallMetadata(
                release.ReleaseTag,
                release.ReleaseName,
                release.PublishedAtUtc,
                DateTimeOffset.UtcNow,
                release.ZipAsset.Sha256,
                release.ExecutableAsset.Sha256));

            _logger.Info($"Installed nightly client to {_paths.NightlyInstallRoot}.");
            return GetManagedInstallInfo();
        }
        finally
        {
            TryDeleteDirectory(stagingRoot);
            TryDeleteDirectory(installRoot);
        }
    }

    public async Task<ManagedClientInstallInfo> UpdateNightlyExecutableAsync(CancellationToken cancellationToken)
    {
        if (!HasManagedInstall())
        {
            _logger.Info("No managed nightly install found. Falling back to full nightly install.");
            return await InstallNightlyAsync(cancellationToken);
        }

        var release = await FetchNightlyReleaseAsync(cancellationToken);
        _logger.Info($"Downloading {release.ExecutableAsset.Name} for lightweight nightly update.");

        Directory.CreateDirectory(_paths.NightlyInstallRoot);
        var targetPath = _paths.NightlyClientExecutablePath;
        var tempPath = $"{targetPath}.download";

        using var response = await HttpClient.GetAsync(release.ExecutableAsset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(file, cancellationToken);
        }

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }

        File.Move(tempPath, targetPath);
        WriteMetadata(new NightlyInstallMetadata(
            release.ReleaseTag,
            release.ReleaseName,
            release.PublishedAtUtc,
            DateTimeOffset.UtcNow,
            release.ZipAsset.Sha256,
            release.ExecutableAsset.Sha256));

        _logger.Info($"Updated managed nightly executable at {targetPath}.");
        return GetManagedInstallInfo();
    }

    private async Task<NightlyRelease> FetchNightlyReleaseAsync(CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(NightlyReleaseUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var assets = root.GetProperty("assets").EnumerateArray().ToArray();

        var zipAsset = assets.FirstOrDefault(asset => asset.GetProperty("name").GetString() == "LCEWindows64.zip");
        var exeAsset = assets.FirstOrDefault(asset => asset.GetProperty("name").GetString() == "Minecraft.Client.exe");

        if (zipAsset.ValueKind == JsonValueKind.Undefined || exeAsset.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException("Nightly release assets are missing the expected client downloads.");
        }

        return new NightlyRelease(
            root.GetProperty("tag_name").GetString() ?? "nightly",
            root.GetProperty("name").GetString() ?? "Nightly Client Release",
            root.GetProperty("published_at").GetDateTimeOffset(),
            ParseAsset(zipAsset),
            ParseAsset(exeAsset));
    }

    private static NightlyAsset ParseAsset(JsonElement asset) =>
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
            throw new InvalidOperationException("Extracted nightly zip did not contain Minecraft.Client.exe.");
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

    private void WriteMetadata(NightlyInstallMetadata metadata)
    {
        Directory.CreateDirectory(_paths.NightlyInstallRoot);
        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_paths.NightlyMetadataPath, json);
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

    private sealed record NightlyRelease(
        string ReleaseTag,
        string ReleaseName,
        DateTimeOffset PublishedAtUtc,
        NightlyAsset ZipAsset,
        NightlyAsset ExecutableAsset);

    private sealed record NightlyAsset(string Name, Uri DownloadUrl, string? Sha256);

    private sealed record NightlyInstallMetadata(
        string ReleaseTag,
        string ReleaseName,
        DateTimeOffset PublishedAtUtc,
        DateTimeOffset InstalledAtUtc,
        string? ZipSha256,
        string? ExecutableSha256);
}
