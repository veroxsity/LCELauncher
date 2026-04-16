using Avalonia.Controls;
using Avalonia.Input.Platform;
using LceLauncher.ViewModels;

namespace LceLauncher.Views;

public partial class PlayPageView : UserControl
{
    public PlayPageView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PlayPageViewModel vm)
        {
            vm.CopyUsernameRequested -= OnCopyUsernameRequested;
            vm.CopyUsernameRequested += OnCopyUsernameRequested;
        }
    }

    private void OnCopyUsernameRequested(object? sender, string username)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        clipboard?.SetTextAsync(username);
    }
}
