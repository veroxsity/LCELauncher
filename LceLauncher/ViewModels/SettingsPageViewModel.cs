using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LceLauncher.Models;
using LceLauncher.Services;

namespace LceLauncher.ViewModels;

public sealed partial class SettingsPageViewModel : ViewModelBase
{
    private readonly LauncherConfig _config;
    private readonly LauncherAuthService _authService;
    private readonly LauncherConfigService _configService;
    private readonly LauncherLogger _logger;

    // Account
    [ObservableProperty] private bool _isOnlineAuth;
    [ObservableProperty] private string _localUsername = string.Empty;
    [ObservableProperty] private bool _syncUsernameFromOnline;
    [ObservableProperty] private string? _microsoftAuthClientId;
    [ObservableProperty] private string _signedInUsername = string.Empty;
    [ObservableProperty] private bool _isSignedIn;
    [ObservableProperty] private string _authStatus = string.Empty;
    [ObservableProperty] private bool _isAuthWorking;

    // Client
    [ObservableProperty] private bool _preferManagedClientInstall;
    [ObservableProperty] private string _clientExecutablePath = string.Empty;
    [ObservableProperty] private string _launchArguments = string.Empty;
    [ObservableProperty] private bool _clientIsRelease;
    [ObservableProperty] private bool _clientIsDebug;

    // Bridge
    [ObservableProperty] private bool _preferManagedBridgeInstall;
    [ObservableProperty] private string _bridgeJarPath = string.Empty;
    [ObservableProperty] private string _javaExecutablePath = string.Empty;
    [ObservableProperty] private string _bridgeLogLevel = "info";
    [ObservableProperty] private bool _bridgeLogPackets;

    // Advanced
    [ObservableProperty] private int _firstBridgePort = 25570;
    [ObservableProperty] private bool _closeBridgeOnExit;

    [ObservableProperty] private string _saveStatus = string.Empty;

    public IReadOnlyList<string> LogLevels { get; } = ["trace", "debug", "info", "warn", "error"];

    public SettingsPageViewModel(
        LauncherConfig config,
        LauncherAuthService authService,
        LauncherConfigService configService,
        LauncherLogger logger)
    {
        _config = config;
        _authService = authService;
        _configService = configService;
        _logger = logger;

        Load();
    }

    private void Load()
    {
        IsOnlineAuth = _config.AuthMode == AuthMode.Online;
        LocalUsername = _config.LocalUsername;
        SyncUsernameFromOnline = _config.SyncUsernameFromOnlineAccount;
        MicrosoftAuthClientId = _config.MicrosoftAuthClientId;

        var profile = _authService.GetCachedProfile();
        IsSignedIn = profile is not null;
        SignedInUsername = profile?.MinecraftUsername ?? string.Empty;

        PreferManagedClientInstall = _config.PreferManagedClientInstall;
        ClientExecutablePath = _config.ClientExecutablePath ?? string.Empty;
        LaunchArguments = _config.LaunchArguments;
        ClientIsRelease = _config.ManagedClientLaunchStream == ManagedClientStream.Release;
        ClientIsDebug = _config.ManagedClientLaunchStream == ManagedClientStream.Debug;

        PreferManagedBridgeInstall = _config.PreferManagedBridgeInstall;
        BridgeJarPath = _config.BridgeJarPath ?? string.Empty;
        JavaExecutablePath = _config.JavaExecutablePath;
        BridgeLogLevel = _config.ManagedBridgeLogLevel;
        BridgeLogPackets = _config.ManagedBridgeLogPackets;

        FirstBridgePort = _config.FirstBridgePort;
        CloseBridgeOnExit = _config.CloseBridgeOnExit;
    }

    [RelayCommand]
    private void Save()
    {
        _config.AuthMode = IsOnlineAuth ? AuthMode.Online : AuthMode.Local;
        _config.LocalUsername = LocalUsername.Trim();
        _config.SyncUsernameFromOnlineAccount = SyncUsernameFromOnline;
        _config.MicrosoftAuthClientId = MicrosoftAuthClientId?.Trim();

        _config.PreferManagedClientInstall = PreferManagedClientInstall;
        _config.ClientExecutablePath = string.IsNullOrWhiteSpace(ClientExecutablePath) ? null : ClientExecutablePath.Trim();
        _config.LaunchArguments = LaunchArguments.Trim();
        _config.ManagedClientLaunchStream = ClientIsDebug ? ManagedClientStream.Debug : ManagedClientStream.Release;

        _config.PreferManagedBridgeInstall = PreferManagedBridgeInstall;
        _config.BridgeJarPath = string.IsNullOrWhiteSpace(BridgeJarPath) ? null : BridgeJarPath.Trim();
        _config.JavaExecutablePath = string.IsNullOrWhiteSpace(JavaExecutablePath) ? "java" : JavaExecutablePath.Trim();
        _config.ManagedBridgeLogLevel = BridgeLogLevel;
        _config.ManagedBridgeLogPackets = BridgeLogPackets;

        _config.FirstBridgePort = FirstBridgePort;
        _config.CloseBridgeOnExit = CloseBridgeOnExit;

        _configService.Save(_config);
        SaveStatus = "Saved.";
        _logger.Info("Settings saved.");
    }

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync(CancellationToken cancellationToken)
    {
        IsAuthWorking = true;
        AuthStatus = "Waiting for device code…";

        try
        {
            var profile = await _authService.SignInAsync(
                code =>
                {
                    AuthStatus = $"Go to {code.VerificationUrl} and enter: {code.UserCode}";
                    return Task.CompletedTask;
                },
                cancellationToken);

            IsSignedIn = true;
            SignedInUsername = profile.MinecraftUsername;
            AuthStatus = $"Signed in as {profile.MinecraftUsername}";
            _logger.Info($"Signed in as {profile.MinecraftUsername}");
            ProfileChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) { AuthStatus = "Sign-in cancelled."; }
        catch (Exception ex)
        {
            AuthStatus = $"Sign-in failed: {ex.Message}";
            _logger.Error($"Sign-in failed: {ex}");
        }
        finally { IsAuthWorking = false; }
    }

    [RelayCommand(CanExecute = nameof(CanSignOut))]
    private async Task SignOutAsync()
    {
        IsAuthWorking = true;
        try
        {
            await _authService.SignOutAsync();
            IsSignedIn = false;
            SignedInUsername = string.Empty;
            AuthStatus = "Signed out.";
            ProfileChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) { AuthStatus = $"Sign-out failed: {ex.Message}"; }
        finally { IsAuthWorking = false; }
    }

    private bool CanSignIn() => !IsAuthWorking && !IsSignedIn;
    private bool CanSignOut() => !IsAuthWorking && IsSignedIn;

    /// <summary>Fired after sign-in or sign-out so the main window can refresh the avatar.</summary>
    public event EventHandler? ProfileChanged;

    // File browse callbacks invoked by the View codebehind
    public event EventHandler? BrowseClientExeRequested;
    public event EventHandler? BrowseBridgeJarRequested;

    [RelayCommand]
    private void BrowseClientExe() => BrowseClientExeRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void BrowseBridgeJar() => BrowseBridgeJarRequested?.Invoke(this, EventArgs.Empty);
}
