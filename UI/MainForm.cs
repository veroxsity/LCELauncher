using System.Diagnostics;
using LceLauncher.Models;
using LceLauncher.Services;

namespace LceLauncher.UI;

public sealed partial class MainForm : Form
{
    private const int WsExComposited = 0x02000000;
    private const int SidebarWidth = 236;
    private const int ShellPadding = 24;
    private const int HomeMinContentWidth = 880;
    private const int HomeMaxContentWidth = 1800;
    private const int PageMinContentWidth = 820;
    private const int PageHeaderMaxWidth = 1200;
    private const int HeroStackBreakpoint = 1100;
    private const string PageHome = "home";
    private const string PageServers = "servers";
    private const string PageSettings = "settings";
    private const string PageLogs = "logs";

    private static readonly Color AppBackground = Color.FromArgb(20, 18, 18);
    private static readonly Color SidebarBackground = Color.FromArgb(34, 31, 31);
    private static readonly Color SurfaceBackground = Color.FromArgb(42, 38, 38);
    private static readonly Color SurfaceRaised = Color.FromArgb(54, 49, 49);
    private static readonly Color SurfaceAlt = Color.FromArgb(27, 24, 24);
    private static readonly Color BorderColor = Color.FromArgb(78, 71, 71);
    private static readonly Color AccentGreen = Color.FromArgb(110, 185, 65);
    private static readonly Color AccentGreenDark = Color.FromArgb(71, 132, 34);
    private static readonly Color AccentGold = Color.FromArgb(230, 188, 77);
    private static readonly Color TextPrimary = Color.FromArgb(245, 242, 237);
    private static readonly Color TextMuted = Color.FromArgb(193, 187, 177);
    private static readonly Color DangerRed = Color.FromArgb(158, 64, 64);

    private readonly AppPaths _appPaths;
    private readonly LauncherLogger _logger;
    private readonly LauncherConfigService _configService;
    private readonly ServerManager _serverManager;
    private readonly ClientProfileManager _clientProfileManager;
    private readonly ClientInstallService _clientInstallService;
    private readonly BridgeInstallService _bridgeInstallService;
    private readonly LauncherAuthService _launcherAuthService;
    private readonly BridgeRuntimeManager _bridgeRuntimeManager;
    private readonly LaunchCoordinator _launchCoordinator;
    private readonly LauncherConfig _config;

    private readonly ComboBox _authModeComboBox;
    private readonly TextBox _localUsernameTextBox;
    private readonly ComboBox _selectedServerComboBox;
    private readonly Label _selectedServerDetailsLabel;
    private readonly Label _bridgeStatusLabel;
    private readonly Button _launchClientButton;
    private readonly Button _launchSelectedServerButton;
    private readonly Button _stopBridgeButton;

    private readonly ListView _serversListView;

    private readonly TextBox _clientExecutableTextBox;
    private readonly TextBox _bridgeJarTextBox;
    private readonly TextBox _javaExecutableTextBox;
    private readonly NumericUpDown _firstBridgePortUpDown;
    private readonly CheckBox _closeBridgeOnExitCheckBox;
    private readonly TextBox _launchArgumentsTextBox;
    private readonly CheckBox _checkForManagedClientUpdatesOnStartupCheckBox;
    private readonly CheckBox _notifyWhenManagedClientUpdateAvailableCheckBox;
    private readonly CheckBox _syncUsernameFromOnlineAccountCheckBox;
    private readonly TextBox _microsoftAuthClientIdTextBox;
    private readonly Label _onlineAccountStatusLabel;
    private readonly Label _onlineAccountDetailsLabel;
    private readonly Label _managedBridgeStatusLabel;
    private readonly Label _managedBridgeDetailsLabel;
    private readonly Label _managedClientStatusLabel;
    private readonly Label _managedClientDetailsLabel;
    private readonly Label _managedClientUpdateLabel;
    private readonly Label _managedClientLastCheckedLabel;
    private readonly Button _installBridgeButton;
    private readonly Button _useManagedBridgeButton;
    private readonly Button _openManagedBridgeButton;
    private readonly Button _signInButton;
    private readonly Button _signOutButton;
    private readonly Button _useCompatibilityAuthClientIdButton;
    private readonly Button _checkNightlyUpdatesButton;
    private readonly Button _installNightlyButton;
    private readonly Button _updateNightlyButton;
    private readonly Button _repairNightlyButton;
    private readonly Button _useManagedNightlyButton;
    private readonly Button _openManagedInstallButton;

    private readonly TextBox _logsTextBox;

    private readonly Dictionary<string, Control> _pages = new();
    private readonly List<(string Key, Button Button)> _pageButtons = [];
    private readonly List<(string Key, Button Button)> _topButtons = [];

    private readonly Panel _contentHost;
    private readonly Label _heroProfileLabel;
    private readonly Label _heroServerLabel;
    private readonly Label _promoTitleLabel;
    private readonly Label _promoStatusPill;
    private readonly Label _serversCardBodyLabel;
    private readonly Label _runtimeCardBodyLabel;
    private readonly Label _footerVersionLabel;

    private readonly Image? _heroImage;
    private readonly Image? _javaCardImage;
    private readonly Image? _bundleCardImage;

    public MainForm()
    {
        _appPaths = new AppPaths();
        _logger = new LauncherLogger();
        _configService = new LauncherConfigService(_appPaths, _logger);
        _serverManager = new ServerManager();
        _clientProfileManager = new ClientProfileManager(_serverManager, _logger);
        _clientInstallService = new ClientInstallService(_appPaths, _logger);
        _bridgeInstallService = new BridgeInstallService(_appPaths, _logger);
        _bridgeRuntimeManager = new BridgeRuntimeManager(_appPaths, _logger);
        _config = _configService.Load();
        _launcherAuthService = new LauncherAuthService(_appPaths, _logger, _config);
        _launchCoordinator = new LaunchCoordinator(_serverManager, _clientProfileManager, _bridgeInstallService, _launcherAuthService, _bridgeRuntimeManager, _logger);
        _serverManager.Normalize(_config);
        _configService.Save(_config);

        _heroImage = LoadAsset("hero.png");
        _javaCardImage = LoadAsset("card-java.png");
        _bundleCardImage = LoadAsset("card-bundle.png");

        Text = "Minecraft Legacy Edition Launcher";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 760);
        ClientSize = new Size(1380, 900);
        BackColor = AppBackground;
        ForeColor = TextPrimary;
        AutoScaleMode = AutoScaleMode.Dpi;
        ResizeRedraw = true;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        EnableDoubleBuffering(this);

        _authModeComboBox = CreateComboBox();
        _authModeComboBox.Items.AddRange(["Local", "Online"]);

        _localUsernameTextBox = CreateTextBox();
        _selectedServerComboBox = CreateComboBox();

        _selectedServerDetailsLabel = CreateBodyLabel(string.Empty);
        _selectedServerDetailsLabel.MaximumSize = new Size(320, 0);

        _bridgeStatusLabel = CreateBodyLabel(string.Empty);
        _bridgeStatusLabel.MaximumSize = new Size(320, 0);

        _launchClientButton = CreateSecondaryButton("CLIENT ONLY");
        _launchSelectedServerButton = CreatePrimaryButton("PLAY");
        _stopBridgeButton = CreateSecondaryButton("STOP BRIDGE");

        _serversListView = CreateServersListView();

        _clientExecutableTextBox = CreateTextBox();
        _bridgeJarTextBox = CreateTextBox();
        _javaExecutableTextBox = CreateTextBox();
        _firstBridgePortUpDown = CreateNumericUpDown();
        _closeBridgeOnExitCheckBox = CreateCheckBox("Stop managed bridge when the launcher exits");
        _launchArgumentsTextBox = CreateTextBox();
        _checkForManagedClientUpdatesOnStartupCheckBox = CreateCheckBox("Check nightly updates when the launcher starts");
        _notifyWhenManagedClientUpdateAvailableCheckBox = CreateCheckBox("Notify me when a newer managed nightly build is available");
        _syncUsernameFromOnlineAccountCheckBox = CreateCheckBox("Sync username.txt from the signed-in Minecraft account");
        _microsoftAuthClientIdTextBox = CreateTextBox();
        _onlineAccountStatusLabel = CreateBodyLabel(string.Empty);
        _onlineAccountDetailsLabel = CreateBodyLabel(string.Empty);
        _managedBridgeStatusLabel = CreateBodyLabel(string.Empty);
        _managedBridgeDetailsLabel = CreateBodyLabel(string.Empty);
        _managedClientStatusLabel = CreateBodyLabel(string.Empty);
        _managedClientDetailsLabel = CreateBodyLabel(string.Empty);
        _managedClientUpdateLabel = CreateBodyLabel("Checking nightly release...");
        _managedClientLastCheckedLabel = CreateBodyLabel("Not checked yet");
        _installBridgeButton = CreateSecondaryButton("INSTALL BRIDGE");
        _useManagedBridgeButton = CreateSecondaryButton("USE MANAGED BRIDGE");
        _openManagedBridgeButton = CreateSecondaryButton("OPEN BRIDGE FOLDER");
        _signInButton = CreateSecondaryButton("SIGN IN");
        _signOutButton = CreateSecondaryButton("SIGN OUT");
        _useCompatibilityAuthClientIdButton = CreateSecondaryButton("USE COMPATIBILITY ID");
        _checkNightlyUpdatesButton = CreateSecondaryButton("CHECK FOR UPDATES");
        _installNightlyButton = CreateSecondaryButton("INSTALL NIGHTLY");
        _updateNightlyButton = CreateSecondaryButton("UPDATE CLIENT");
        _repairNightlyButton = CreateSecondaryButton("REPAIR CLIENT");
        _useManagedNightlyButton = CreateSecondaryButton("USE MANAGED INSTALL");
        _openManagedInstallButton = CreateSecondaryButton("OPEN INSTALL FOLDER");

        _logsTextBox = CreateLogsTextBox();

        _heroProfileLabel = CreateBodyLabel(string.Empty);
        _heroServerLabel = CreateBodyLabel(string.Empty);
        _promoTitleLabel = CreateSectionTitle(string.Empty, 16F);
        _promoStatusPill = CreateStatusPill();
        _serversCardBodyLabel = CreateBodyLabel(string.Empty);
        _runtimeCardBodyLabel = CreateBodyLabel(string.Empty);
        _footerVersionLabel = CreateBodyLabel("Legacy Launcher v0.1");
        _footerVersionLabel.ForeColor = TextMuted;

        _contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppBackground,
            Padding = new Padding(0),
        };

        BuildShell();
        WireEvents();
        LoadConfigIntoControls();
        RefreshServerViews();
        RefreshOnlineAccountStatus();
        RefreshStatus();
        RefreshManagedBridgeStatus();
        RefreshManagedInstallStatus();
        AppendExistingLogs();
        ShowPage(PageHome);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExComposited;
            return cp;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        try
        {
            SaveConfigFromControls();
            if (_config.CloseBridgeOnExit)
            {
                _bridgeRuntimeManager.StopAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
        }

        base.OnFormClosing(e);
    }

    private void BuildShell()
    {
        Controls.Clear();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = AppBackground,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SidebarWidth));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        EnableDoubleBuffering(root);

        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(_contentHost, 1, 0);

        _pages[PageHome] = BuildHomePage();
        _pages[PageServers] = BuildServersPage();
        _pages[PageSettings] = BuildSettingsPage();
        _pages[PageLogs] = BuildLogsPage();

        foreach (var page in _pages.Values)
        {
            page.Visible = false;
            _contentHost.Controls.Add(page);
        }

        Controls.Add(root);
    }
}
