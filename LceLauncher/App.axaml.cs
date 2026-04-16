using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LceLauncher.Services;
using LceLauncher.ViewModels;
using LceLauncher.Views;

namespace LceLauncher;

public partial class App : Application
{
    /// <summary>Allows ViewModels to open dialogs on the main window without coupling.</summary>
    public static MainWindow? MainWindow { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var paths = new AppPaths();
                var logger = new LauncherLogger(paths);
                var configService = new LauncherConfigService(paths, logger);
                var config = configService.Load();

                var serverManager = new ServerManager();
                var clientInstallService = new ClientInstallService(paths, logger);
                var bridgeInstallService = new BridgeInstallService(paths, logger);
                var bridgeRuntimeManager = new BridgeRuntimeManager(paths, logger);
                var authService = new LauncherAuthService(paths, logger, config);
                var clientProfileManager = new ClientProfileManager(paths, serverManager, logger);
                var launchCoordinator = new LaunchCoordinator(
                    serverManager, clientProfileManager, bridgeInstallService,
                    authService, bridgeRuntimeManager, logger);

                var vm = new MainWindowViewModel(
                    paths, logger, configService, config,
                    clientInstallService, bridgeInstallService,
                    launchCoordinator, authService, serverManager);

                MainWindow = new MainWindow { DataContext = vm };
                desktop.MainWindow = MainWindow;

                desktop.Exit += (_, _) =>
                {
                    if (config.CloseBridgeOnExit)
                        bridgeRuntimeManager.StopAsync().GetAwaiter().GetResult();
                    configService.Save(config);
                };
            }
            catch (Exception ex)
            {
                // Show the error in a simple window so we can diagnose startup failures
                var errorWindow = new Window
                {
                    Title = "LCE Launcher — Startup Error",
                    Width = 700,
                    Height = 400,
                    Content = new TextBlock
                    {
                        Text = ex.ToString(),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Avalonia.Thickness(12),
                        Foreground = Avalonia.Media.Brushes.Red,
                    }
                };
                desktop.MainWindow = errorWindow;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
