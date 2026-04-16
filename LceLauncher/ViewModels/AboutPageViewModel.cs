using CommunityToolkit.Mvvm.Input;
using LceLauncher.Services;

namespace LceLauncher.ViewModels;

public sealed partial class AboutPageViewModel : ViewModelBase
{
    private readonly AppPaths _paths;
    private readonly LauncherLogger _logger;

    public string LauncherVersion => "0.1.0";
    public string ProjectDescription =>
        "A cross-platform launcher for the Minecraft Legacy Console Edition community project. " +
        "Manages game builds, the Java bridge, and server connections.";

    public AboutPageViewModel(AppPaths paths, LauncherLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        try { OpenFolder(_paths.LauncherLogsRoot); }
        catch (Exception ex) { _logger.Error($"Failed to open logs folder: {ex.Message}"); }
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        try { OpenFolder(_paths.DataRoot); }
        catch (Exception ex) { _logger.Error($"Failed to open data folder: {ex.Message}"); }
    }

    [RelayCommand]
    private void OpenClientGitHub() =>
        OpenUrl("https://github.com/smartcmd/MinecraftConsoles");

    [RelayCommand]
    private void OpenBridgeGitHub() =>
        OpenUrl("https://github.com/veroxsity/LCEBridge");

    [RelayCommand]
    private void OpenDebugGitHub() =>
        OpenUrl("https://github.com/veroxsity/LCEDebug");

    private static void OpenUrl(string url)
    {
        var psi = new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true };
        System.Diagnostics.Process.Start(psi);
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        var psi = new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true };
        System.Diagnostics.Process.Start(psi);
    }
}
