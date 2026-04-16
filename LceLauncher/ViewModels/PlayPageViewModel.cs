using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LceLauncher.Models;
using LceLauncher.Services;

namespace LceLauncher.ViewModels;

public sealed partial class PlayPageViewModel : ViewModelBase
{
    private readonly LauncherConfig _config;
    private readonly LaunchCoordinator _launchCoordinator;
    private readonly LauncherAuthService _authService;
    private readonly ServersPageViewModel _serversVm;
    private readonly LauncherLogger _logger;

    [ObservableProperty] private ServerEntry? _selectedServer;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isLaunching;
    [ObservableProperty] private bool _hasError;

    public ObservableCollection<ServerEntry> Servers => _serversVm.Servers;

    public string DisplayUsername =>
        string.IsNullOrWhiteSpace(_config.LocalUsername) ? "Player" : _config.LocalUsername;

    public PlayPageViewModel(
        LauncherConfig config,
        LaunchCoordinator launchCoordinator,
        LauncherAuthService authService,
        ServersPageViewModel serversVm,
        LauncherLogger logger)
    {
        _config = config;
        _launchCoordinator = launchCoordinator;
        _authService = authService;
        _serversVm = serversVm;
        _logger = logger;

        // Restore previously selected server
        SelectedServer = Servers.FirstOrDefault(s => s.Id == config.SelectedServerId)
            ?? Servers.FirstOrDefault();
    }

    partial void OnSelectedServerChanged(ServerEntry? value)
    {
        _config.SelectedServerId = value?.Id;
    }

    [RelayCommand]
    private void CopyUsername()
    {
        // Clipboard access is done via the View codebehind; raise a notification
        CopyUsernameRequested?.Invoke(this, DisplayUsername);
    }

    public event EventHandler<string>? CopyUsernameRequested;

    [RelayCommand(CanExecute = nameof(CanPlay))]
    private async Task PlayAsync(CancellationToken cancellationToken)
    {
        IsLaunching = true;
        HasError = false;
        StatusMessage = "Launching…";

        try
        {
            await _launchCoordinator.LaunchAsync(_config, SelectedServer, cancellationToken);
            StatusMessage = "Game launched successfully.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Launch cancelled.";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = ex.Message;
            _logger.Error($"Launch failed: {ex}");
        }
        finally
        {
            IsLaunching = false;
        }
    }

    private bool CanPlay() => !IsLaunching;
}
