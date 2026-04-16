using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LceLauncher.Models;

namespace LceLauncher.ViewModels;

public sealed partial class ServerEditViewModel : ViewModelBase
{
    private readonly ServerEntry? _existing;

    [ObservableProperty] private string _displayName = "New Server";
    [ObservableProperty] private bool _isJavaBridge;
    [ObservableProperty] private string _remoteAddress = string.Empty;
    [ObservableProperty] private int _remotePort = 25565;
    [ObservableProperty] private bool _requiresOnlineAuth;
    [ObservableProperty] private string? _validationError;

    public string Title => _existing is null ? "Add Server" : "Edit Server";

    public event EventHandler<ServerEntry>? Confirmed;
    public event EventHandler? Cancelled;

    public ServerEditViewModel(ServerEntry? existing = null)
    {
        _existing = existing;
        if (existing is null) return;

        DisplayName = existing.DisplayName;
        IsJavaBridge = existing.Type == ServerType.JavaBridge;
        RemoteAddress = existing.RemoteAddress;
        RemotePort = existing.RemotePort;
        RequiresOnlineAuth = existing.RequiresOnlineAuth;
    }

    [RelayCommand]
    private void Confirm()
    {
        ValidationError = null;

        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ValidationError = "Display name is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(RemoteAddress))
        {
            ValidationError = "Remote address is required.";
            return;
        }
        if (RemotePort is < 1 or > 65535)
        {
            ValidationError = "Port must be between 1 and 65535.";
            return;
        }

        var entry = _existing ?? new ServerEntry();
        entry.DisplayName = DisplayName.Trim();
        entry.Type = IsJavaBridge ? ServerType.JavaBridge : ServerType.NativeLce;
        entry.RemoteAddress = RemoteAddress.Trim();
        entry.RemotePort = RemotePort;
        entry.RequiresOnlineAuth = IsJavaBridge && RequiresOnlineAuth;

        Confirmed?.Invoke(this, entry);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}
