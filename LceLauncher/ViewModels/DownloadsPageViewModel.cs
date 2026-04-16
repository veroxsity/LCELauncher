using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LceLauncher.Models;
using LceLauncher.Services;

namespace LceLauncher.ViewModels;

public sealed partial class DownloadsPageViewModel : ViewModelBase
{
    private readonly ClientInstallService _clientInstallService;
    private readonly BridgeInstallService _bridgeInstallService;
    private readonly LauncherConfig _config;
    private readonly LauncherConfigService _configService;
    private readonly LauncherLogger _logger;

    // ── Release client ────────────────────────────────────
    [ObservableProperty] private string _releaseVersion = "Checking…";
    [ObservableProperty] private string _releaseStatus = string.Empty;
    [ObservableProperty] private bool _releaseInstalled;
    [ObservableProperty] private bool _releaseUpdateAvailable;
    [ObservableProperty] private double _releaseProgress;
    [ObservableProperty] private bool _releaseProgressVisible;
    [ObservableProperty] private bool _releaseWorking;

    // ── Debug client ──────────────────────────────────────
    [ObservableProperty] private string _debugVersion = "Checking…";
    [ObservableProperty] private string _debugStatus = string.Empty;
    [ObservableProperty] private bool _debugInstalled;
    [ObservableProperty] private bool _debugUpdateAvailable;
    [ObservableProperty] private double _debugProgress;
    [ObservableProperty] private bool _debugProgressVisible;
    [ObservableProperty] private bool _debugWorking;

    // ── Bridge ────────────────────────────────────────────
    [ObservableProperty] private string _bridgeVersion = "Checking…";
    [ObservableProperty] private string _bridgeStatus = string.Empty;
    [ObservableProperty] private bool _bridgeInstalled;
    [ObservableProperty] private bool _bridgeUpdateAvailable;
    [ObservableProperty] private double _bridgeProgress;
    [ObservableProperty] private bool _bridgeProgressVisible;
    [ObservableProperty] private bool _bridgeWorking;

    public DownloadsPageViewModel(
        ClientInstallService clientInstallService,
        BridgeInstallService bridgeInstallService,
        LauncherConfig config,
        LauncherConfigService configService,
        LauncherLogger logger)
    {
        _clientInstallService = clientInstallService;
        _bridgeInstallService = bridgeInstallService;
        _config = config;
        _configService = configService;
        _logger = logger;

        RefreshLocalInfo();
    }

    public void RefreshLocalInfo()
    {
        var ri = _clientInstallService.GetManagedInstallInfo(ManagedClientStream.Release);
        ReleaseInstalled = ri.IsInstalled;
        ReleaseVersion = ri.IsInstalled ? ri.DisplayVersion : "Not installed";
        ReleaseStatus = ri.IsInstalled ? $"Installed {ri.InstalledAtUtc?.LocalDateTime.ToString("g") ?? ""}" : string.Empty;

        var di = _clientInstallService.GetManagedInstallInfo(ManagedClientStream.Debug);
        DebugInstalled = di.IsInstalled;
        DebugVersion = di.IsInstalled ? di.DisplayVersion : "Not installed";
        DebugStatus = di.IsInstalled ? $"Installed {di.InstalledAtUtc?.LocalDateTime.ToString("g") ?? ""}" : string.Empty;

        var bi = _bridgeInstallService.GetManagedInstallInfo();
        BridgeInstalled = bi.IsInstalled;
        BridgeVersion = bi.IsInstalled ? bi.DisplayVersion : "Not installed";
        BridgeStatus = bi.IsInstalled ? $"Installed {bi.InstalledAtUtc?.LocalDateTime.ToString("g") ?? ""}" : string.Empty;
    }

    // ── Release commands ──────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanInstallRelease))]
    private async Task InstallReleaseAsync(CancellationToken cancellationToken)
    {
        ReleaseWorking = true;
        ReleaseProgressVisible = true;
        ReleaseStatus = "Downloading…";
        try
        {
            var progress = new Progress<(long r, long t)>(p =>
            {
                ReleaseProgress = p.t > 0 ? (double)p.r / p.t * 100 : 50;
                ReleaseStatus = p.t > 0
                    ? $"Downloading… {p.r / 1024 / 1024:F1} / {p.t / 1024 / 1024:F1} MB"
                    : $"Downloading… {p.r / 1024 / 1024:F1} MB";
            });
            var info = await _clientInstallService.InstallAsync(ManagedClientStream.Release, progress, cancellationToken);
            UpdateConfigClient(ManagedClientStream.Release, info.ClientExecutablePath);
            ReleaseInstalled = true;
            ReleaseVersion = info.DisplayVersion;
            ReleaseStatus = "Installed successfully.";
        }
        catch (OperationCanceledException) { ReleaseStatus = "Cancelled."; }
        catch (Exception ex) { ReleaseStatus = $"Error: {ex.Message}"; _logger.Error($"Release install failed: {ex}"); }
        finally { ReleaseWorking = false; ReleaseProgressVisible = false; ReleaseProgress = 0; }
    }

    [RelayCommand(CanExecute = nameof(CanUpdateRelease))]
    private async Task UpdateReleaseAsync(CancellationToken cancellationToken)
    {
        ReleaseWorking = true;
        ReleaseProgressVisible = true;
        ReleaseStatus = "Checking for update…";
        try
        {
            var progress = new Progress<(long r, long t)>(p =>
            {
                ReleaseProgress = p.t > 0 ? (double)p.r / p.t * 100 : 50;
                ReleaseStatus = p.t > 0
                    ? $"Downloading… {p.r / 1024 / 1024:F1} / {p.t / 1024 / 1024:F1} MB"
                    : $"Downloading… {p.r / 1024 / 1024:F1} MB";
            });
            var info = await _clientInstallService.UpdateAsync(ManagedClientStream.Release, progress, cancellationToken);
            UpdateConfigClient(ManagedClientStream.Release, info.ClientExecutablePath);
            ReleaseVersion = info.DisplayVersion;
            ReleaseStatus = "Up to date.";
        }
        catch (OperationCanceledException) { ReleaseStatus = "Cancelled."; }
        catch (Exception ex) { ReleaseStatus = $"Error: {ex.Message}"; _logger.Error($"Release update failed: {ex}"); }
        finally { ReleaseWorking = false; ReleaseProgressVisible = false; ReleaseProgress = 0; }
    }

    [RelayCommand(CanExecute = nameof(CanCheckRelease))]
    private async Task CheckReleaseAsync(CancellationToken cancellationToken)
    {
        ReleaseStatus = "Checking…";
        try
        {
            var update = await _clientInstallService.GetUpdateInfoAsync(ManagedClientStream.Release, cancellationToken);
            ReleaseUpdateAvailable = update.UpdateAvailable;
            ReleaseStatus = update.StatusText;
        }
        catch (Exception ex) { ReleaseStatus = $"Error: {ex.Message}"; }
    }

    private bool CanInstallRelease() => !ReleaseWorking && !ReleaseInstalled;
    private bool CanUpdateRelease() => !ReleaseWorking && ReleaseInstalled;
    private bool CanCheckRelease() => !ReleaseWorking;

    // ── Debug commands ────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanInstallDebug))]
    private async Task InstallDebugAsync(CancellationToken cancellationToken)
    {
        DebugWorking = true;
        DebugProgressVisible = true;
        DebugStatus = "Downloading…";
        try
        {
            var progress = new Progress<(long r, long t)>(p =>
            {
                DebugProgress = p.t > 0 ? (double)p.r / p.t * 100 : 50;
                DebugStatus = p.t > 0
                    ? $"Downloading… {p.r / 1024 / 1024:F1} / {p.t / 1024 / 1024:F1} MB"
                    : $"Downloading… {p.r / 1024 / 1024:F1} MB";
            });
            var info = await _clientInstallService.InstallAsync(ManagedClientStream.Debug, progress, cancellationToken);
            UpdateConfigClient(ManagedClientStream.Debug, info.ClientExecutablePath);
            DebugInstalled = true;
            DebugVersion = info.DisplayVersion;
            DebugStatus = "Installed successfully.";
        }
        catch (OperationCanceledException) { DebugStatus = "Cancelled."; }
        catch (Exception ex) { DebugStatus = $"Error: {ex.Message}"; _logger.Error($"Debug install failed: {ex}"); }
        finally { DebugWorking = false; DebugProgressVisible = false; DebugProgress = 0; }
    }

    [RelayCommand(CanExecute = nameof(CanUpdateDebug))]
    private async Task UpdateDebugAsync(CancellationToken cancellationToken)
    {
        DebugWorking = true;
        DebugProgressVisible = true;
        DebugStatus = "Checking for update…";
        try
        {
            var progress = new Progress<(long r, long t)>(p =>
            {
                DebugProgress = p.t > 0 ? (double)p.r / p.t * 100 : 50;
                DebugStatus = p.t > 0
                    ? $"Downloading… {p.r / 1024 / 1024:F1} / {p.t / 1024 / 1024:F1} MB"
                    : $"Downloading… {p.r / 1024 / 1024:F1} MB";
            });
            var info = await _clientInstallService.UpdateAsync(ManagedClientStream.Debug, progress, cancellationToken);
            UpdateConfigClient(ManagedClientStream.Debug, info.ClientExecutablePath);
            DebugVersion = info.DisplayVersion;
            DebugStatus = "Up to date.";
        }
        catch (OperationCanceledException) { DebugStatus = "Cancelled."; }
        catch (Exception ex) { DebugStatus = $"Error: {ex.Message}"; _logger.Error($"Debug update failed: {ex}"); }
        finally { DebugWorking = false; DebugProgressVisible = false; DebugProgress = 0; }
    }

    [RelayCommand(CanExecute = nameof(CanCheckDebug))]
    private async Task CheckDebugAsync(CancellationToken cancellationToken)
    {
        DebugStatus = "Checking…";
        try
        {
            var update = await _clientInstallService.GetUpdateInfoAsync(ManagedClientStream.Debug, cancellationToken);
            DebugUpdateAvailable = update.UpdateAvailable;
            DebugStatus = update.StatusText;
        }
        catch (Exception ex) { DebugStatus = $"Error: {ex.Message}"; }
    }

    private bool CanInstallDebug() => !DebugWorking && !DebugInstalled;
    private bool CanUpdateDebug() => !DebugWorking && DebugInstalled;
    private bool CanCheckDebug() => !DebugWorking;

    // ── Bridge commands ───────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanInstallBridge))]
    private async Task InstallBridgeAsync(CancellationToken cancellationToken)
    {
        BridgeWorking = true;
        BridgeProgressVisible = true;
        BridgeStatus = "Downloading…";
        try
        {
            var progress = new Progress<(long r, long t)>(p =>
            {
                BridgeProgress = p.t > 0 ? (double)p.r / p.t * 100 : 50;
                BridgeStatus = p.t > 0
                    ? $"Downloading… {p.r / 1024 / 1024:F1} / {p.t / 1024 / 1024:F1} MB"
                    : $"Downloading… {p.r / 1024 / 1024:F1} MB";
            });
            var info = await _bridgeInstallService.InstallLatestReleaseAsync(progress, cancellationToken);
            if (_config.PreferManagedBridgeInstall)
                _config.BridgeJarPath = info.BridgeJarPath;
            BridgeInstalled = true;
            BridgeVersion = info.DisplayVersion;
            BridgeStatus = "Installed successfully.";
        }
        catch (OperationCanceledException) { BridgeStatus = "Cancelled."; }
        catch (Exception ex) { BridgeStatus = $"Error: {ex.Message}"; _logger.Error($"Bridge install failed: {ex}"); }
        finally { BridgeWorking = false; BridgeProgressVisible = false; BridgeProgress = 0; }
    }

    [RelayCommand(CanExecute = nameof(CanUpdateBridge))]
    private async Task UpdateBridgeAsync(CancellationToken cancellationToken)
    {
        BridgeWorking = true;
        BridgeProgressVisible = true;
        BridgeStatus = "Checking for update…";
        try
        {
            var progress = new Progress<(long r, long t)>(p =>
            {
                BridgeProgress = p.t > 0 ? (double)p.r / p.t * 100 : 50;
                BridgeStatus = p.t > 0
                    ? $"Downloading… {p.r / 1024 / 1024:F1} / {p.t / 1024 / 1024:F1} MB"
                    : $"Downloading… {p.r / 1024 / 1024:F1} MB";
            });
            var info = await _bridgeInstallService.UpdateBridgeAsync(progress, cancellationToken);
            if (_config.PreferManagedBridgeInstall)
                _config.BridgeJarPath = info.BridgeJarPath;
            BridgeVersion = info.DisplayVersion;
            BridgeStatus = "Up to date.";
        }
        catch (OperationCanceledException) { BridgeStatus = "Cancelled."; }
        catch (Exception ex) { BridgeStatus = $"Error: {ex.Message}"; _logger.Error($"Bridge update failed: {ex}"); }
        finally { BridgeWorking = false; BridgeProgressVisible = false; BridgeProgress = 0; }
    }

    private bool CanInstallBridge() => !BridgeWorking && !BridgeInstalled;
    private bool CanUpdateBridge() => !BridgeWorking && BridgeInstalled;

    // ── Helpers ───────────────────────────────────────────

    private void UpdateConfigClient(ManagedClientStream stream, string exePath)
    {
        if (_config.PreferManagedClientInstall
            && _config.ManagedClientLaunchStream == stream)
        {
            _config.ClientExecutablePath = exePath;
        }
        _configService.Save(_config);
    }
}
