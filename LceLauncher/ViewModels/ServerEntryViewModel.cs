using CommunityToolkit.Mvvm.ComponentModel;
using LceLauncher.Models;

namespace LceLauncher.ViewModels;

public sealed partial class ServerEntryViewModel : ViewModelBase
{
    public ServerEntry Entry { get; }

    public string DisplayName => Entry.DisplayName;
    public string TypeLabel => Entry.Type == ServerType.NativeLce ? "Native LCE" : "Java Bridge";
    public string Address => $"{Entry.RemoteAddress}:{Entry.RemotePort}";
    public bool IsJavaBridge => Entry.Type == ServerType.JavaBridge;

    public ServerEntryViewModel(ServerEntry entry)
    {
        Entry = entry;
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(Address));
    }
}
