using System.Net.Http.Headers;
using System.Security.Cryptography;
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

    public async Task<OnlineAccountProfile> SignInAsync(Func<DeviceCodeResult, Task> promptCallback, CancellationToken cancellationToken)
    {
        var publicClientApp = GetPublicClientApp();
        var authResult = await publicClientApp
            .AcquireTokenWithDeviceCode(DeviceCodeScopes, promptCallback)
            .ExecuteAsync(cancellationToken);

        var bridgeAuth = await BuildBridgeAuthContextAsync(authResult.AccessToken, cancellationToken);
        var profile = new OnlineAccountProfile
        {
            HomeAccountId = authResult.Account.HomeAccountId.Identifier,
            MicrosoftUsername = authResult.Account.Username ?? string.Empty,
            MinecraftProfileId = bridgeAuth.MinecraftProfileId,
            MinecraftUsername = bridgeAuth.MinecraftUsername,
            LastAuthenticatedAtUtc = DateTimeOffset.UtcNow,
        };

        PersistProfile(profile);
        _logger.Info($"Signed in launcher account '{profile.MinecraftUsername}'.");
        return profile;
    }

    public async Task<BridgeAuthContext> GetBridgeAuthContextAsync(CancellationToken cancellationToken)
    {
        var publicClientApp = GetPublicClientApp();
        var profile = _cachedProfile ?? throw new InvalidOperationException("No online account is signed in.");
        var account = (await publicClientApp.GetAccountsAsync()).FirstOrDefault(item => item.HomeAccountId.Identifier == profile.HomeAccountId);
        if (account is null)
        {
            throw new InvalidOperationException("Saved online account could not be found in the local token cache. Sign in again.");
        }

        AuthenticationResult authResult;
        try
        {
            authResult = await publicClientApp
                .AcquireTokenSilent(DeviceCodeScopes, account)
                .ExecuteAsync(cancellationToken);
        }
        catch (MsalUiRequiredException ex)
        {
            throw new InvalidOperationException("Online sign-in expired or requires reauthentication. Sign in again.", ex);
        }

        var bridgeAuth = await BuildBridgeAuthContextAsync(authResult.AccessToken, cancellationToken);

        var updatedProfile = new OnlineAccountProfile
        {
            HomeAccountId = profile.HomeAccountId,
            MicrosoftUsername = string.IsNullOrWhiteSpace(authResult.Account.Username) ? profile.MicrosoftUsername : authResult.Account.Username,
            MinecraftProfileId = bridgeAuth.MinecraftProfileId,
            MinecraftUsername = bridgeAuth.MinecraftUsername,
            LastAuthenticatedAtUtc = DateTimeOffset.UtcNow,
        };

        PersistProfile(updatedProfile);
        return bridgeAuth;
    }

    public async Task SignOutAsync()
    {
        var publicClientApp = GetPublicClientApp();
        var accounts = await publicClientApp.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await publicClientApp.RemoveAsync(account);
        }

        TryDeleteFile(_paths.MsalTokenCachePath);
        TryDeleteFile(_paths.OnlineAccountProfilePath);
        _cachedProfile = null;
        _logger.Info("Signed out launcher online account and cleared cached tokens.");
    }

    private IPublicClientApplication GetPublicClientApp()
    {
        var clientId = GetEffectiveClientId();
        if (_publicClientApp is not null && string.Equals(clientId, _activeClientId, StringComparison.Ordinal))
        {
            return _publicClientApp;
        }

        _activeClientId = clientId;
        _publicClientApp = PublicClientApplicationBuilder
            .Create(clientId)
            .WithAuthority(Authority)
            .WithDefaultRedirectUri()
            .Build();

        InitializeTokenCache(_publicClientApp.UserTokenCache);
        _logger.Info($"Configured Microsoft auth provider client ID: {clientId}");
        return _publicClientApp;
    }

    private async Task<BridgeAuthContext> BuildBridgeAuthContextAsync(string microsoftAccessToken, CancellationToken cancellationToken)
    {
        var xbl = await AuthenticateWithXboxLiveAsync(microsoftAccessToken, cancellationToken);
        var xsts = await AuthorizeWithXstsAsync(xbl.Token, cancellationToken);
        var minecraftLogin = await LoginToMinecraftAsync(xsts.UserHash, xsts.Token, cancellationToken);
        await EnsureMinecraftEntitlementAsync(minecraftLogin.AccessToken, cancellationToken);
        var minecraftProfile = await FetchMinecraftProfileAsync(minecraftLogin.AccessToken, cancellationToken);

        return new BridgeAuthContext
        {
            MinecraftProfileId = NormalizeMinecraftProfileId(minecraftProfile.Id),
            MinecraftUsername = minecraftProfile.Name,
            MinecraftAccessToken = minecraftLogin.AccessToken,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, minecraftLogin.ExpiresIn - 60)),
        };
    }

    private async Task<XboxAuthResponse> AuthenticateWithXboxLiveAsync(string microsoftAccessToken, CancellationToken cancellationToken)
    {
        var payload = new
        {
            Properties = new
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={microsoftAccessToken}",
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT",
        };

        using var response = await PostJsonAsync(XboxUserAuthenticateUri, payload, cancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        return new XboxAuthResponse(
            root.GetProperty("Token").GetString() ?? throw new InvalidOperationException("Xbox Live token was missing."),
            root.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString() ?? throw new InvalidOperationException("Xbox Live user hash was missing."));
    }

    private async Task<XboxAuthResponse> AuthorizeWithXstsAsync(string userToken, CancellationToken cancellationToken)
    {
        var payload = new
        {
            Properties = new
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { userToken },
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT",
        };

        using var response = await PostJsonAsync(XboxXstsAuthorizeUri, payload, cancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        return new XboxAuthResponse(
            root.GetProperty("Token").GetString() ?? throw new InvalidOperationException("XSTS token was missing."),
            root.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString() ?? throw new InvalidOperationException("XSTS user hash was missing."));
    }

    private async Task<MinecraftLoginResponse> LoginToMinecraftAsync(string userHash, string xstsToken, CancellationToken cancellationToken)
    {
        var payload = new
        {
            identityToken = $"XBL3.0 x={userHash};{xstsToken}",
        };

        using var response = await PostJsonAsync(MinecraftLoginUri, payload, cancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        return new MinecraftLoginResponse(
            root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Minecraft access token was missing."),
            root.GetProperty("expires_in").GetInt32());
    }

    private async Task EnsureMinecraftEntitlementAsync(string minecraftAccessToken, CancellationToken cancellationToken)
    {
        using var request = CreateBearerRequest(HttpMethod.Get, MinecraftEntitlementsUri, minecraftAccessToken);
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("This Microsoft account does not appear to own Minecraft Java Edition.");
        }
    }

    private async Task<MinecraftProfileResponse> FetchMinecraftProfileAsync(string minecraftAccessToken, CancellationToken cancellationToken)
    {
        using var request = CreateBearerRequest(HttpMethod.Get, MinecraftProfileUri, minecraftAccessToken);
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        return new MinecraftProfileResponse(
            root.GetProperty("id").GetString() ?? throw new InvalidOperationException("Minecraft profile ID was missing."),
            root.GetProperty("name").GetString() ?? throw new InvalidOperationException("Minecraft profile name was missing."));
    }

    private async Task<HttpResponseMessage> PostJsonAsync(Uri uri, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };

        var response = await HttpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.Dispose();
        throw new InvalidOperationException($"Auth request to {uri.Host} failed with {(int)response.StatusCode}: {Truncate(body, 240)}");
    }

    private void InitializeTokenCache(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(args =>
        {
            var cacheBytes = LoadProtectedBytes(_paths.MsalTokenCachePath);
            if (cacheBytes is not null)
            {
                args.TokenCache.DeserializeMsalV3(cacheBytes, shouldClearExistingCache: true);
            }
        });

        tokenCache.SetAfterAccess(args =>
        {
            if (!args.HasStateChanged)
            {
                return;
            }

            var cacheBytes = args.TokenCache.SerializeMsalV3();
            SaveProtectedBytes(_paths.MsalTokenCachePath, cacheBytes);
        });
    }

    private OnlineAccountProfile? LoadPersistedProfile()
    {
        if (!File.Exists(_paths.OnlineAccountProfilePath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OnlineAccountProfile>(File.ReadAllText(_paths.OnlineAccountProfilePath), _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Failed to read saved online account profile: {ex.Message}");
            return null;
        }
    }

    private void PersistProfile(OnlineAccountProfile profile)
    {
        Directory.CreateDirectory(_paths.AuthRoot);
        File.WriteAllText(_paths.OnlineAccountProfilePath, JsonSerializer.Serialize(profile, _jsonOptions));
        _cachedProfile = profile;
    }

    private static void SaveProtectedBytes(string path, byte[] bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(path, protectedBytes);
    }

    private static byte[]? LoadProtectedBytes(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var protectedBytes = File.ReadAllBytes(path);
            return ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        }
        catch
        {
            return null;
        }
    }

    private static HttpRequestMessage CreateBearerRequest(HttpMethod method, Uri uri, string accessToken)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
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
        if (Guid.TryParseExact(value, "N", out var guid))
        {
            return guid.ToString("D");
        }

        if (Guid.TryParse(value, out guid))
        {
            return guid.ToString("D");
        }

        throw new InvalidOperationException("Minecraft profile ID was not a valid UUID.");
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private sealed record XboxAuthResponse(string Token, string UserHash);

    private sealed record MinecraftLoginResponse(string AccessToken, int ExpiresIn);

    private sealed record MinecraftProfileResponse(string Id, string Name);
}
