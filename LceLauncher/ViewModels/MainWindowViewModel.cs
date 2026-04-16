using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LceLauncher.Models;
using LceLauncher.Services;

namespace LceLauncher.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly AppPaths _paths;
    private readonly LauncherLogger _logger;
    private readonly LauncherConfigService _configService;
    private readonly LauncherConfig _config;
    private readonly LauncherAuthService _authService;

    // Sub-page VMs created once
    private readonly HomePageViewModel _homeVm;
    private readonly PlayPageViewModel _playVm;
    private readonly DownloadsPageViewModel _downloadsVm;
    private readonly ServersPageViewModel _serversVm;
    private readonly SettingsPageViewModel _settingsVm;
    private readonly AboutPageViewModel _aboutVm;

    // Two separate selection properties — one per ListBox.
    // When one fires, we null the other so each ListBox's internal state stays in sync.
    [ObservableProperty] private SidebarNavItem? _selectedTopItem;
    [ObservableProperty] private SidebarNavItem? _selectedBottomItem;

    [ObservableProperty] private TabNavItem? _selectedTab;
    [ObservableProperty] private ViewModelBase? _currentContent;
    [ObservableProperty] private bool _showTabBar;
    [ObservableProperty] private ObservableCollection<TabNavItem> _currentTabs = [];
    [ObservableProperty] private string _displayUsername = "Player";
    [ObservableProperty] private Bitmap? _profileAvatar;

    public ObservableCollection<SidebarNavItem> SidebarItems { get; } = [];
    public ObservableCollection<SidebarNavItem> BottomSidebarItems { get; } = [];
    public string Version => "v0.1.0";

    public MainWindowViewModel(
        AppPaths paths,
        LauncherLogger logger,
        LauncherConfigService configService,
        LauncherConfig config,
        ClientInstallService clientInstallService,
        BridgeInstallService bridgeInstallService,
        LaunchCoordinator launchCoordinator,
        LauncherAuthService authService,
        ServerManager serverManager)
    {
        _paths = paths;
        _logger = logger;
        _configService = configService;
        _config = config;
        _authService = authService;

        _homeVm = new HomePageViewModel();
        _downloadsVm = new DownloadsPageViewModel(clientInstallService, bridgeInstallService, config, configService, logger);
        _serversVm = new ServersPageViewModel(config, serverManager, configService);
        _playVm = new PlayPageViewModel(config, launchCoordinator, authService, _serversVm, logger);
        _settingsVm = new SettingsPageViewModel(config, authService, configService, logger);
        _aboutVm = new AboutPageViewModel(paths, logger);

        // Refresh avatar + username whenever the user signs in or out
        _settingsVm.ProfileChanged += (_, _) =>
        {
            UpdateDisplayUsername();
            // Give the auth service ~1.5 s to finish writing the avatar file, then reload
            Task.Delay(1500).ContinueWith(_ =>
                Avalonia.Threading.Dispatcher.UIThread.Post(LoadCachedAvatar));
        };

        BuildNavigation();
        UpdateDisplayUsername();

        // Load avatar from disk if cached, otherwise fetch it if already signed in
        if (File.Exists(_paths.AvatarCachePath))
        {
            LoadCachedAvatar();
        }
        else
        {
            EnsureAvatarFetchedAsync();
        }

        // Select LCE entry (second item) by default
        SelectedTopItem = SidebarItems.Count > 1 ? SidebarItems[1] : SidebarItems.FirstOrDefault();
    }

    /// <summary>
    /// If the user is already signed in but we have no avatar cached yet,
    /// fetch it using the stored URL (set during sign-in from the Xbox profile API).
    /// </summary>
    private async void EnsureAvatarFetchedAsync()
    {
        var profile = _authService.GetCachedProfile();
        if (profile?.AvatarUrl is null)
        {
            // No stored URL (profile predates this feature) — nothing to do until next sign-in
            return;
        }

        await _authService.FetchAndCacheAvatarAsync(profile.AvatarUrl, CancellationToken.None);
        Avalonia.Threading.Dispatcher.UIThread.Post(LoadCachedAvatar);
    }

    partial void OnSelectedTopItemChanged(SidebarNavItem? value)
    {
        if (value is null) return;
        SelectedBottomItem = null;
        ApplySidebarSelection(value);
    }

    partial void OnSelectedBottomItemChanged(SidebarNavItem? value)
    {
        if (value is null) return;
        SelectedTopItem = null;
        ApplySidebarSelection(value);
    }

    private void ApplySidebarSelection(SidebarNavItem value)
    {
        if (value.Tabs is { Count: > 0 })
        {
            ShowTabBar = true;
            CurrentTabs = new ObservableCollection<TabNavItem>(value.Tabs);
            SelectedTab = CurrentTabs.FirstOrDefault();
        }
        else
        {
            ShowTabBar = false;
            CurrentTabs = [];
            SelectedTab = null;
            CurrentContent = value.DirectContent;
        }
    }

    partial void OnSelectedTabChanged(TabNavItem? value)
    {
        if (value is not null)
            CurrentContent = value.Content;
    }

    public void UpdateDisplayUsername()
    {
        DisplayUsername = string.IsNullOrWhiteSpace(_config.LocalUsername)
            ? "Player"
            : _config.LocalUsername;
    }

    public void LoadCachedAvatar()
    {
        try
        {
            if (File.Exists(_paths.AvatarCachePath))
                ProfileAvatar = new Bitmap(_paths.AvatarCachePath);
            else
                ProfileAvatar = null;
        }
        catch { /* ignore corrupt/missing cache */ }
    }

    private void BuildNavigation()
    {
        SidebarItems.Add(new SidebarNavItem
        {
            Id = "home",
            DisplayName = "HOME",
            DirectContent = _homeVm,
        });

        SidebarItems.Add(new SidebarNavItem
        {
            Id = "lce",
            DisplayName = "MINECRAFT",
            Subtitle = "LEGACY EDITION",
            Tabs =
            [
                new TabNavItem { DisplayName = "Play",      Content = _playVm },
                new TabNavItem { DisplayName = "Downloads", Content = _downloadsVm },
                new TabNavItem { DisplayName = "Servers",   Content = _serversVm },
            ],
        });

        BottomSidebarItems.Add(new SidebarNavItem
        {
            Id = "settings",
            DisplayName = "SETTINGS",
            DirectContent = _settingsVm,
        });

        BottomSidebarItems.Add(new SidebarNavItem
        {
            Id = "about",
            DisplayName = "ABOUT",
            DirectContent = _aboutVm,
        });
    }
}
