using Avalonia.Controls;
using Avalonia.Platform.Storage;
using LceLauncher.ViewModels;

namespace LceLauncher.Views;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SettingsPageViewModel vm)
        {
            vm.BrowseClientExeRequested -= OnBrowseClientExe;
            vm.BrowseBridgeJarRequested -= OnBrowseBridgeJar;
            vm.BrowseClientExeRequested += OnBrowseClientExe;
            vm.BrowseBridgeJarRequested += OnBrowseBridgeJar;
        }
    }

    private async void OnBrowseClientExe(object? sender, EventArgs e)
    {
        if (sender is not SettingsPageViewModel vm) return;
        var path = await PickFileAsync("Select Minecraft.Client.exe",
            new FilePickerFileType("Executable") { Patterns = ["*.exe", "*"] });
        if (path is not null) vm.ClientExecutablePath = path;
    }

    private async void OnBrowseBridgeJar(object? sender, EventArgs e)
    {
        if (sender is not SettingsPageViewModel vm) return;
        var path = await PickFileAsync("Select bootstrap-standalone.jar",
            new FilePickerFileType("JAR file") { Patterns = ["*.jar"] });
        if (path is not null) vm.BridgeJarPath = path;
    }

    private async Task<string?> PickFileAsync(string title, FilePickerFileType fileType)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [fileType],
        });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }
}
