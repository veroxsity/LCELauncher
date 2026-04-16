using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LceLauncher.Models;
using Microsoft.Identity.Client;

namespace LceLauncher.Services;

public sealed class LauncherAuthService
{
    public const string DefaultCompatibilityClientId = "c36a9fb6-4f2a-41ff-90bd-ae7cc92031eb";
    private const string Authority = "https://login.microsoftonline.com/consumers";
    private static readonly string[] DeviceCodeScopes = ["XboxLive.signin", "offline_access"];
    private static readonly Uri XboxUserAuthenticateUri = new("https://user.auth.xboxlive.com/user/authenticate");
    private static readonly Uri XboxXstsAuthorizeUri = new("https://xsts.auth.xboxlive.com/xsts/authorize");
    private static readonly Uri XboxProfileSettingsUri = new("https://profile.xboxlive.com/users/me/profile/settings?settings=GameDisplayPicRaw");
    private static readonly Uri MinecraftLoginUri = new("https://api.minecraftservices.com/authentication/login_with_xbox");
    private static readonly Uri MinecraftEntitlementsUri = new("https://api.minecraftservices.com/entitlements/mcstore");
    private static readonly Uri MinecraftProfileUri = new("https://api.minecraftservices.com/minecraft/profile");
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly AppPaths _paths;
    private readonly LauncherConfig _config;
    private readonly LauncherLogger _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private IPublicClientApplication? _publicClientApp;
    private string? _activeClientId;
    private OnlineAccountProfile? _cachedProfile;

    public LauncherAuthService(AppPaths paths, LauncherLogger logger, LauncherConfig config)
    {
        _paths = paths;
        _config = config;
        _logger = logger;
        Directory.CreateDirectory(_paths.AuthRoot);
        _cachedProfile = LoadPersistedProfile();
    }

    public OnlineAccountProfile? GetCachedProfile() => _cachedProfile;

    public string GetEffectiveClientId() =>
        string.IsNullOrWhiteSpace(_config.MicrosoftAuthClientId)
            ? DefaultCompatibilityClientId
            : _config.MicrosoftAuthClientId.Trim();

    public async Task<OnlineAccountProfile> SignInAsync(
        Func<DeviceCodeResult, Task> promptCallback, CancellationToken cancellationToken)
    {
        var app = GetPublicClientApp();
        var authResult = await app
            .AcquireTokenWithDeviceCode(DeviceCodeScopes, promptCallback)
            .ExecuteAsync(cancellationToken);

        var (bridgeAuth, picUrl) = await BuildBridgeAuthContextAsync(authResult.AccessToken, cancellationToken);
        var profile = new OnlineAccountProfile
        {
            HomeAccountId = authResult.Account.HomeAccountId.Identifier,
            MicrosoftUsername = authResult.Account.Username ?? string.Empty,
            MinecraftProfileId = bridgeAuth.MinecraftProfileId,
            MinecraftUsername = bridgeAuth.MinecraftUsername,
            LastAuthenticatedAtUtc = DateTimeOffset.UtcNow,
            AvatarUrl = picUrl,
        };

        PersistProfile(profile);
        if (picUrl is not null)
            _ = FetchAndCacheAvatarAsync(picUrl, CancellationToken.None); // fire-and-forget
        _logger.Info($"Signed in as '{profile.MinecraftUsername}'.");
        return profile;
    }

    public async Task<BridgeAuthContext> GetBridgeAuthContextAsync(CancellationToken cancellationToken)
    {
        var app = GetPublicClientApp();
        var profile = _cachedProfile ?? throw new InvalidOperationException("No online account is signed in.");
        var account = (await app.GetAccountsAsync())
            .FirstOrDefault(a => a.HomeAccountId.Identifier == profile.HomeAccountId)
            ?? throw new InvalidOperationException("Saved account not found in token cache. Sign in again.");

        AuthenticationResult authResult;
        try
        {
            authResult = await app.AcquireTokenSilent(DeviceCodeScopes, account).ExecuteAsync(cancellationToken);
        }
        catch (MsalUiRequiredException ex)
        {
            throw new InvalidOperationException("Online sign-in expired. Sign in again.", ex);
        }

        var (bridgeAuth, refreshedPicUrl) = await BuildBridgeAuthContextAsync(authResult.AccessToken, cancellationToken);
        PersistProfile(new OnlineAccountProfile
        {
            HomeAccountId = profile.HomeAccountId,
            MicrosoftUsername = string.IsNullOrWhiteSpace(authResult.Account.Username)
                ? profile.MicrosoftUsername : authResult.Account.Username,
            MinecraftProfileId = bridgeAuth.MinecraftProfileId,
            MinecraftUsername = bridgeAuth.MinecraftUsername,
            LastAuthenticatedAtUtc = DateTimeOffset.UtcNow,
            AvatarUrl = refreshedPicUrl ?? profile.AvatarUrl,
        });

        return bridgeAuth;
    }

    public async Task SignOutAsync()
    {
        var app = GetPublicClientApp();
        foreach (var account in await app.GetAccountsAsync())
            await app.RemoveAsync(account);
        TryDeleteFile(_paths.MsalTokenCachePath);
        TryDeleteFile(_paths.OnlineAccountProfilePath);
        _cachedProfile = null;
        _logger.Info("Signed out and cleared cached tokens.");
    }

    private IPublicClientApplication GetPublicClientApp()
    {
        var clientId = GetEffectiveClientId();
        if (_publicClientApp is not null && string.Equals(clientId, _activeClientId, StringComparison.Ordinal))
            return _publicClientApp;

        _activeClientId = clientId;
        _publicClientApp = PublicClientApplicationBuilder
            .Create(clientId)
            .WithAuthority(Authority)
            .WithDefaultRedirectUri()
            .Build();

        InitializeTokenCache(_publicClientApp.UserTokenCache);
        _logger.Info($"Configured Microsoft auth with client ID: {clientId}");
        return _publicClientApp;
    }

    private async Task<(BridgeAuthContext Bridge, string? XboxProfilePicUrl)> BuildBridgeAuthContextAsync(
        string microsoftAccessToken, CancellationToken cancellationToken)
    {
        var xbl = await AuthenticateWithXboxLiveAsync(microsoftAccessToken, cancellationToken);

        // Get two XSTS tokens in parallel: one for Minecraft, one for the Xbox profile API
        var xstsMinecraftTask = AuthorizeWithXstsAsync(xbl.Token, "rp://api.minecraftservices.com/", cancellationToken);
        var xstsXboxTask     = AuthorizeWithXstsAsync(xbl.Token, "http://xboxlive.com", cancellationToken);
        await Task.WhenAll(xstsMinecraftTask, xstsXboxTask);

        var xsts = xstsMinecraftTask.Result;
        var xstsXbox = xstsXboxTask.Result;

        var mcLogin = await LoginToMinecraftAsync(xsts.UserHash, xsts.Token, cancellationToken);
        await EnsureMinecraftEntitlementAsync(mcLogin.AccessToken, cancellationToken);
        var profile = await FetchMinecraftProfileAsync(mcLogin.AccessToken, cancellationToken);

        // Fetch the Xbox profile picture URL (non-fatal if it fails)
        string? picUrl = null;
        try { picUrl = await FetchXboxProfilePicUrlAsync(xstsXbox.UserHash, xstsXbox.Token, cancellationToken); }
        catch (Exception ex) { _logger.Warn($"Could not fetch Xbox profile picture: {ex.Message}"); }

        return (new BridgeAuthContext
        {
            MinecraftProfileId = NormalizeMinecraftProfileId(profile.Id),
            MinecraftUsername = profile.Name,
            MinecraftAccessToken = mcLogin.AccessToken,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, mcLogin.ExpiresIn - 60)),
        }, picUrl);
    }

    private async Task<XboxAuthResponse> AuthenticateWithXboxLiveAsync(
        string microsoftAccessToken, CancellationToken cancellationToken)
    {
        var payload = new
        {
            Properties = new { AuthMethod = "RPS", SiteName = "user.auth.xboxlive.com", RpsTicket = $"d={microsoftAccessToken}" },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT",
        };
        using var response = await PostJsonAsync(XboxUserAuthenticateUri, payload, cancellationToken);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        return new XboxAuthResponse(
            root.GetProperty("Token").GetString() ?? throw new InvalidOperationException("Xbox Live token missing."),
            root.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString()
                ?? throw new InvalidOperationException("Xbox Live user hash missing."));
    }

    private async Task<XboxAuthResponse> AuthorizeWithXstsAsync(
        string userToken, string relyingParty, CancellationToken cancellationToken)
    {
        var payload = new
        {
            Properties = new { SandboxId = "RETAIL", UserTokens = new[] { userToken } },
            RelyingParty = relyingParty,
            TokenType = "JWT",
        };
        using var response = await PostJsonAsync(XboxXstsAuthorizeUri, payload, cancellationToken);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        return new XboxAuthResponse(
            root.GetProperty("Token").GetString() ?? throw new InvalidOperationException("XSTS token missing."),
            root.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString()
                ?? throw new InvalidOperationException("XSTS user hash missing."));
    }

    private async Task<MinecraftLoginResponse> LoginToMinecraftAsync(
        string userHash, string xstsToken, CancellationToken cancellationToken)
    {
        var payload = new { identityToken = $"XBL3.0 x={userHash};{xstsToken}" };
        using var response = await PostJsonAsync(MinecraftLoginUri, payload, cancellationToken);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        return new MinecraftLoginResponse(
            root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Minecraft access token missing."),
            root.GetProperty("expires_in").GetInt32());
    }

    private async Task EnsureMinecraftEntitlementAsync(string mcToken, CancellationToken cancellationToken)
    {
        using var req = CreateBearerRequest(HttpMethod.Get, MinecraftEntitlementsUri, mcToken);
        using var res = await HttpClient.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(cancellationToken));
        if (!doc.RootElement.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("This Microsoft account does not appear to own Minecraft Java Edition.");
        }
    }

    private async Task<string?> FetchXboxProfilePicUrlAsync(
        string userHash, string xstsToken, CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, XboxProfileSettingsUri);
        req.Headers.TryAddWithoutValidation("Authorization", $"XBL3.0 x={userHash};{xstsToken}");
        req.Headers.TryAddWithoutValidation("x-xbl-contract-version", "2");
        using var res = await HttpClient.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(cancellationToken));
        var settings = doc.RootElement
            .GetProperty("profileUsers")[0]
            .GetProperty("settings")
            .EnumerateArray();
        foreach (var s in settings)
        {
            if (s.GetProperty("id").GetString() == "GameDisplayPicRaw")
                return s.GetProperty("value").GetString();
        }
        return null;
    }

    private async Task<MinecraftProfileResponse> FetchMinecraftProfileAsync(
        string mcToken, CancellationToken cancellationToken)
    {
        using var req = CreateBearerRequest(HttpMethod.Get, MinecraftProfileUri, mcToken);
        using var res = await HttpClient.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        return new MinecraftProfileResponse(
            root.GetProperty("id").GetString() ?? throw new InvalidOperationException("Minecraft profile ID missing."),
            root.GetProperty("name").GetString() ?? throw new InvalidOperationException("Minecraft profile name missing."));
    }

    private async Task<HttpResponseMessage> PostJsonAsync(Uri uri, object payload, CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };
        var res = await HttpClient.SendAsync(req, cancellationToken);
        if (res.IsSuccessStatusCode) return res;
        var body = await res.Content.ReadAsStringAsync(cancellationToken);
        res.Dispose();
        throw new InvalidOperationException(
            $"Auth request to {uri.Host} failed {(int)res.StatusCode}: {Truncate(body, 240)}");
    }

    private void InitializeTokenCache(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(args =>
        {
            var data = LoadProtectedBytes(_paths.MsalTokenCachePath);
            if (data is not null)
                args.TokenCache.DeserializeMsalV3(data, shouldClearExistingCache: true);
        });
        tokenCache.SetAfterAccess(args =>
        {
            if (!args.HasStateChanged) return;
            SaveProtectedBytes(_paths.MsalTokenCachePath, args.TokenCache.SerializeMsalV3());
        });
    }

    private OnlineAccountProfile? LoadPersistedProfile()
    {
        if (!File.Exists(_paths.OnlineAccountProfilePath)) return null;
        try
        {
            return JsonSerializer.Deserialize<OnlineAccountProfile>(
                File.ReadAllText(_paths.OnlineAccountProfilePath), _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to read saved account profile: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Downloads the player face image from Crafatar and saves it to the local avatar cache.
    /// Safe to call fire-and-forget — failures are logged and swallowed.
    /// </summary>
    public async Task FetchAndCacheAvatarAsync(string avatarUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await HttpClient.GetAsync(avatarUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            Directory.CreateDirectory(_paths.AuthRoot);
            await File.WriteAllBytesAsync(_paths.AvatarCachePath, bytes, cancellationToken);
            _logger.Info("Cached player avatar.");
        }
        catch (Exception ex)
        {
            _logger.Warn($"Could not fetch player avatar: {ex.Message}");
        }
    }

    private void PersistProfile(OnlineAccountProfile profile)
    {
        Directory.CreateDirectory(_paths.AuthRoot);
        File.WriteAllText(_paths.OnlineAccountProfilePath,
            JsonSerializer.Serialize(profile, _jsonOptions));
        _cachedProfile = profile;
    }

    // ── Cross-platform DPAPI helpers ──────────────────────────────────────────

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static byte[] ProtectWindows(byte[] data) =>
        System.Security.Cryptography.ProtectedData.Protect(data, null,
            System.Security.Cryptography.DataProtectionScope.CurrentUser);

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static byte[] UnprotectWindows(byte[] data) =>
        System.Security.Cryptography.ProtectedData.Unprotect(data, null,
            System.Security.Cryptography.DataProtectionScope.CurrentUser);

    private static void SaveProtectedBytes(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var data = OperatingSystem.IsWindows() ? ProtectWindows(bytes) : bytes;
        File.WriteAllBytes(path, data);
    }

    private static byte[]? LoadProtectedBytes(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var data = File.ReadAllBytes(path);
            return OperatingSystem.IsWindows() ? UnprotectWindows(data) : data;
        }
        catch { return null; }
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    private static HttpRequestMessage CreateBearerRequest(HttpMethod method, Uri uri, string token)
    {
        var req = new HttpRequestMessage(method, uri);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("LCELauncher", "0.1"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private static string NormalizeMinecraftProfileId(string value)
    {
        if (Guid.TryParseExact(value, "N", out var guid)) return guid.ToString("D");
        if (Guid.TryParse(value, out guid)) return guid.ToString("D");
        throw new InvalidOperationException("Minecraft profile ID is not a valid UUID.");
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }

    private sealed record XboxAuthResponse(string Token, string UserHash);
    private sealed record MinecraftLoginResponse(string AccessToken, int ExpiresIn);
    private sealed record MinecraftProfileResponse(string Id, string Name);
}
