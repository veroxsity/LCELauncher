using Avalonia.Controls;
using LceLauncher.ViewModels;

namespace LceLauncher.Views;

public partial class ServersPageView : UserControl
{
    public ServersPageView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ServersPageViewModel vm)
        {
            vm.AddServerRequested -= OnAddServerRequested;
            vm.EditServerRequested -= OnEditServerRequested;
            vm.AddServerRequested += OnAddServerRequested;
            vm.EditServerRequested += OnEditServerRequested;
        }
    }

    private async void OnAddServerRequested(object? sender, EventArgs e)
    {
        if (sender is not ServersPageViewModel vm) return;
        var editVm = new ServerEditViewModel();
        var dialog = new ServerEditDialog { DataContext = editVm };

        editVm.Confirmed += (_, entry) =>
        {
            vm.CommitAdd(entry);
            dialog.Close();
        };
        editVm.Cancelled += (_, _) => dialog.Close();

        await dialog.ShowDialog(App.MainWindow!);
    }

    private async void OnEditServerRequested(object? sender, Models.ServerEntry? entry)
    {
        if (sender is not ServersPageViewModel vm || entry is null) return;

        var editVm = new ServerEditViewModel(entry);
        var dialog = new ServerEditDialog { DataContext = editVm };
        ServerEntryViewModel? entryVm = null;
        foreach (var s in vm.ServerVms)
        {
            if (s.Entry == entry) { entryVm = s; break; }
        }

        editVm.Confirmed += (_, updated) =>
        {
            if (entryVm is not null)
                vm.CommitEdit(updated, entryVm);
            dialog.Close();
        };
        editVm.Cancelled += (_, _) => dialog.Close();

        await dialog.ShowDialog(App.MainWindow!);
    }
}
