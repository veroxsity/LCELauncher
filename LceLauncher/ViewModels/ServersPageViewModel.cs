using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LceLauncher.Models;
using LceLauncher.Services;

namespace LceLauncher.ViewModels;

public sealed partial class ServersPageViewModel : ViewModelBase
{
    private readonly LauncherConfig _config;
    private readonly ServerManager _serverManager;
    private readonly LauncherConfigService _configService;

    public ObservableCollection<ServerEntry> Servers { get; } = [];
    public ObservableCollection<ServerEntryViewModel> ServerVms { get; } = [];

    public event EventHandler<ServerEntry?>? EditServerRequested;
    public event EventHandler? AddServerRequested;

    public ServersPageViewModel(
        LauncherConfig config,
        ServerManager serverManager,
        LauncherConfigService configService)
    {
        _config = config;
        _serverManager = serverManager;
        _configService = configService;

        foreach (var server in config.Servers)
        {
            Servers.Add(server);
            ServerVms.Add(new ServerEntryViewModel(server));
        }
    }

    [RelayCommand]
    private void AddServer() => AddServerRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void EditServer(ServerEntryViewModel vm) => EditServerRequested?.Invoke(this, vm.Entry);

    [RelayCommand]
    private void DeleteServer(ServerEntryViewModel vm)
    {
        _config.Servers.Remove(vm.Entry);
        Servers.Remove(vm.Entry);
        ServerVms.Remove(vm);
        _serverManager.Normalize(_config);
        _configService.Save(_config);
    }

    public void CommitAdd(ServerEntry newEntry)
    {
        _config.Servers.Add(newEntry);
        _serverManager.Normalize(_config);
        Servers.Add(newEntry);
        ServerVms.Add(new ServerEntryViewModel(newEntry));
        _configService.Save(_config);
    }

    public void CommitEdit(ServerEntry updated, ServerEntryViewModel vm)
    {
        _serverManager.Normalize(_config);
        vm.Refresh();
        _configService.Save(_config);
    }
}
