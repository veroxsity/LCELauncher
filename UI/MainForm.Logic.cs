using System.Diagnostics;
using LceLauncher.Models;
using LceLauncher.Services;

namespace LceLauncher.UI;

public sealed partial class MainForm
{
    private Button CreateSidebarNavButton(string pageKey, string text)
    {
        var font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        var textHeight = TextRenderer.MeasureText(
            text,
            font,
            new Size(SidebarWidth - 64, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;

        var button = new Button
        {
            Width = SidebarWidth,
            Height = Math.Max(48, textHeight + 18),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat,
            ForeColor = TextPrimary,
            BackColor = SidebarBackground,
            Font = font,
            Margin = Padding.Empty,
            Padding = new Padding(26, 0, 18, 0),
            Tag = false,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = SurfaceRaised;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(66, 60, 60);
        button.Paint += (_, args) =>
        {
            if (button.Tag is true)
            {
                using var brush = new SolidBrush(AccentGreen);
                args.Graphics.FillRectangle(brush, 0, 4, 4, button.Height - 8);
            }
        };
        button.Click += (_, _) => ShowPage(pageKey);
        _pageButtons.Add((pageKey, button));
        return button;
    }

    private Button CreateTopNavButton(string pageKey, string text)
    {
        var button = new Button
        {
            AutoSize = true,
            Height = 38,
            Text = text,
            FlatStyle = FlatStyle.Flat,
            ForeColor = TextMuted,
            BackColor = AppBackground,
            Font = new Font("Segoe UI Semibold", 12.5F, FontStyle.Bold),
            Margin = new Padding(0, 0, 18, 0),
            Padding = new Padding(0, 0, 0, 6),
            Tag = false,
        };
        button.FlatAppearance.BorderSize = 0;
        button.Paint += (_, args) =>
        {
            if (button.Tag is true)
            {
                using var pen = new Pen(TextPrimary, 2);
                args.Graphics.DrawLine(pen, 0, button.Height - 2, button.Width, button.Height - 2);
            }
        };
        button.Click += (_, _) => ShowPage(pageKey);
        _topButtons.Add((pageKey, button));
        return button;
    }

    private Button CreatePrimaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = AccentGreen,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            Height = 56,
            Width = 240,
            Margin = new Padding(0, 0, 0, 12),
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseDownBackColor = AccentGreenDark;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(123, 200, 78);
        SizeButtonToText(button, 200);
        return button;
    }

    private Button CreateSecondaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = SurfaceRaised,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold),
            Height = 38,
            Width = 180,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(14, 0, 14, 0),
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = BorderColor;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(72, 66, 66);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(88, 80, 80);
        SizeButtonToText(button, 128);
        return button;
    }

    private ComboBox CreateComboBox() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        BackColor = SurfaceRaised,
        ForeColor = TextPrimary,
        Font = new Font("Segoe UI", 10.25F, FontStyle.Regular),
        Width = 320,
        Height = 34,
        IntegralHeight = false,
        DropDownHeight = 240,
        Margin = Padding.Empty,
    };

    private TextBox CreateTextBox() => new()
    {
        BackColor = SurfaceRaised,
        ForeColor = TextPrimary,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10.25F, FontStyle.Regular),
        Width = 420,
        Margin = Padding.Empty,
    };

    private NumericUpDown CreateNumericUpDown() => new()
    {
        BackColor = SurfaceRaised,
        ForeColor = TextPrimary,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 10.25F, FontStyle.Regular),
        Minimum = 1024,
        Maximum = 65535,
        Width = 160,
        Margin = Padding.Empty,
    };

    private CheckBox CreateCheckBox(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = TextPrimary,
        Font = new Font("Segoe UI", 10.25F, FontStyle.Regular),
        BackColor = Color.Transparent,
        Margin = Padding.Empty,
    };

    private TextBox CreateLogsTextBox() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        BackColor = Color.FromArgb(16, 15, 15),
        ForeColor = Color.FromArgb(197, 236, 171),
        BorderStyle = BorderStyle.None,
        Font = new Font("Consolas", 10F, FontStyle.Regular),
    };

    private ListView CreateServersListView()
    {
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            View = View.Details,
            MultiSelect = false,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(24, 22, 22),
            ForeColor = TextPrimary,
            HideSelection = false,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
        };
        list.Columns.Add("Name");
        list.Columns.Add("Type");
        list.Columns.Add("Remote");
        list.Columns.Add("Client Target");
        list.Columns.Add("Notes");
        list.Resize += (_, _) => ResizeServerListColumns();
        ResizeServerListColumns(list);
        return list;
    }

    private Panel CreateInsetPanel() => new() { BackColor = Color.FromArgb(44, 40, 40), Dock = DockStyle.Fill };

    private Label CreateSectionTitle(string text, float size) => new()
    {
        AutoSize = true,
        Text = text,
        ForeColor = TextPrimary,
        Font = new Font("Segoe UI Semibold", size, FontStyle.Bold),
        BackColor = Color.Transparent,
    };

    private Label CreateCaptionLabel(string text) => new()
    {
        AutoSize = true,
        Text = text.ToUpperInvariant(),
        ForeColor = AccentGold,
        Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold),
        BackColor = Color.Transparent,
        Margin = new Padding(0, 0, 0, 4),
    };

    private Label CreateBodyLabel(string text) => new()
    {
        AutoSize = true,
        Text = text,
        ForeColor = TextMuted,
        Font = new Font("Segoe UI", 10.25F, FontStyle.Regular),
        BackColor = Color.Transparent,
    };

    private Label CreateStatusPill() => new()
    {
        AutoSize = true,
        ForeColor = TextPrimary,
        BackColor = AccentGreenDark,
        Font = new Font("Segoe UI Semibold", 8.75F, FontStyle.Bold),
        Padding = new Padding(10, 4, 10, 4),
        Margin = new Padding(0, 10, 0, 12),
    };

    private static Control Spacer(int width, int height) => new Panel { Width = width, Height = height, Margin = Padding.Empty, BackColor = Color.Transparent };

    private Panel CreateCoverImagePanel(Image? image, int height)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = height,
            BackColor = SurfaceRaised,
            Margin = Padding.Empty,
        };
        EnableDoubleBuffering(panel);
        panel.Paint += (_, args) => DrawCoverImage(args.Graphics, image, panel.ClientRectangle);
        return panel;
    }

    private static void EnableDoubleBuffering(Control control)
    {
        typeof(Control)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(control, true);
    }

    private static void SizeButtonToText(Button button, int minWidth)
    {
        var measured = TextRenderer.MeasureText(
            button.Text,
            button.Font,
            new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

        button.Width = Math.Max(minWidth, measured.Width + button.Padding.Horizontal + 20);
    }

    private static void DrawCoverImage(Graphics graphics, Image? image, Rectangle bounds)
    {
        if (image is null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Max((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
        var drawWidth = image.Width * scale;
        var drawHeight = image.Height * scale;
        var drawX = bounds.X + ((bounds.Width - drawWidth) / 2F);
        var drawY = bounds.Y + ((bounds.Height - drawHeight) / 2F);

        var previousInterpolation = graphics.InterpolationMode;
        var previousPixelOffset = graphics.PixelOffsetMode;
        var previousSmoothing = graphics.SmoothingMode;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        graphics.DrawImage(image, Rectangle.Round(new RectangleF(drawX, drawY, drawWidth, drawHeight)));
        graphics.InterpolationMode = previousInterpolation;
        graphics.PixelOffsetMode = previousPixelOffset;
        graphics.SmoothingMode = previousSmoothing;
    }

    private void ResizeServerListColumns()
    {
        ResizeServerListColumns(_serversListView);
    }

    private static void ResizeServerListColumns(ListView list)
    {
        if (list.Columns.Count < 5 || list.ClientSize.Width <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(860, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6);
        var widths = new[]
        {
            Math.Max(150, (int)(availableWidth * 0.19)),
            Math.Max(110, (int)(availableWidth * 0.11)),
            Math.Max(180, (int)(availableWidth * 0.22)),
            Math.Max(180, (int)(availableWidth * 0.20)),
            Math.Max(180, availableWidth),
        };

        widths[4] = Math.Max(180, availableWidth - widths[0] - widths[1] - widths[2] - widths[3]);

        for (var index = 0; index < list.Columns.Count; index++)
        {
            list.Columns[index].Width = widths[index];
        }
    }

    private void WireEvents()
    {
        _logger.LineLogged += line =>
        {
            if (IsDisposed)
            {
                return;
            }

            _pendingLogLines.Enqueue(line);
            if (Interlocked.Exchange(ref _logFlushScheduled, 1) != 0)
            {
                return;
            }

            if (IsHandleCreated)
            {
                BeginInvoke(new Action(EnsureLogFlushTimerRunning));
            }
            else
            {
                EnsureLogFlushTimerRunning();
            }
        };

        _launchClientButton.Click += async (_, _) => await LaunchAsync(null);
        _stopBridgeButton.Click += async (_, _) =>
        {
            await _bridgeRuntimeManager.StopAsync();
            RefreshStatus();
        };

        _selectedServerComboBox.SelectedIndexChanged += (_, _) => RefreshStatus();
        _authModeComboBox.SelectedIndexChanged += (_, _) => RefreshStatus();
        _localUsernameTextBox.TextChanged += (_, _) => RefreshStatus();
        _syncUsernameFromOnlineAccountCheckBox.CheckedChanged += (_, _) => RefreshStatus();
        _managedClientInstallStreamComboBox.SelectedIndexChanged += async (_, _) =>
        {
            RefreshManagedInstallStatus();
            await RefreshManagedInstallUpdateStatusAsync(GetSelectedManagedClientInstallStream(), notifyIfAvailable: false, updateUi: true);
        };
        _managedClientLaunchStreamComboBox.SelectedIndexChanged += (_, _) =>
        {
            SyncManagedClientPathForSelectedLaunchStream();
            RefreshManagedInstallStatus();
            RefreshStatus();
        };
        _serversListView.DoubleClick += (_, _) => EditSelectedServer();
        Shown += async (_, _) => await InitializeManagedRuntimeUpdateChecksAsync();
    }

    private void LoadConfigIntoControls()
    {
        if (_config.PreferManagedClientInstall && File.Exists(_appPaths.GetManagedClientExecutablePath(_config.ManagedClientLaunchStream)))
        {
            _config.ClientExecutablePath = _appPaths.GetManagedClientExecutablePath(_config.ManagedClientLaunchStream);
        }

        _authModeComboBox.SelectedIndex = _config.AuthMode == AuthMode.Online ? 1 : 0;
        _localUsernameTextBox.Text = _config.LocalUsername;
        _clientExecutableTextBox.Text = _config.ClientExecutablePath ?? string.Empty;
        _bridgeJarTextBox.Text = _config.BridgeJarPath ?? string.Empty;
        _javaExecutableTextBox.Text = _config.JavaExecutablePath;
        _firstBridgePortUpDown.Value = Math.Clamp(_config.FirstBridgePort, (int)_firstBridgePortUpDown.Minimum, (int)_firstBridgePortUpDown.Maximum);
        _closeBridgeOnExitCheckBox.Checked = _config.CloseBridgeOnExit;
        _launchArgumentsTextBox.Text = _config.LaunchArguments;
        _syncUsernameFromOnlineAccountCheckBox.Checked = _config.SyncUsernameFromOnlineAccount;
        _microsoftAuthClientIdTextBox.Text = _config.MicrosoftAuthClientId ?? _launcherAuthService.GetEffectiveClientId();
        _checkForManagedBridgeUpdatesOnStartupCheckBox.Checked = _config.CheckForManagedBridgeUpdatesOnStartup;
        _notifyWhenManagedBridgeUpdateAvailableCheckBox.Checked = _config.NotifyWhenManagedBridgeUpdateAvailable;
        _managedBridgeLogLevelComboBox.SelectedItem = NormalizeManagedBridgeLogLevel(_config.ManagedBridgeLogLevel);
        _managedBridgeLogPacketsCheckBox.Checked = _config.ManagedBridgeLogPackets;
        _checkForManagedClientUpdatesOnStartupCheckBox.Checked = _config.CheckForManagedClientUpdatesOnStartup;
        _notifyWhenManagedClientUpdateAvailableCheckBox.Checked = _config.NotifyWhenManagedClientUpdateAvailable;
        _managedClientInstallStreamComboBox.SelectedItem = FindManagedClientStreamSelectionItem(_config.ManagedClientInstallStream);
        _managedClientLaunchStreamComboBox.SelectedItem = FindManagedClientStreamSelectionItem(_config.ManagedClientLaunchStream);
    }

    private void SaveConfigFromControls()
    {
        var configuredClientPath = NullIfWhitespace(_clientExecutableTextBox.Text);
        _config.ManagedClientInstallStream = GetSelectedManagedClientInstallStream();
        _config.ManagedClientLaunchStream = GetSelectedManagedClientLaunchStream();
        _config.AuthMode = _authModeComboBox.SelectedIndex == 1 ? AuthMode.Online : AuthMode.Local;
        _config.LocalUsername = string.IsNullOrWhiteSpace(_localUsernameTextBox.Text) ? "Player" : _localUsernameTextBox.Text.Trim();
        if (TryGetManagedClientStreamForPath(configuredClientPath, out var matchedManagedStream))
        {
            _config.PreferManagedClientInstall = true;
            _config.ManagedClientLaunchStream = matchedManagedStream;
        }
        else
        {
            _config.PreferManagedClientInstall = string.IsNullOrWhiteSpace(configuredClientPath);
        }

        _config.ClientExecutablePath = configuredClientPath;
        var configuredBridgePath = NullIfWhitespace(_bridgeJarTextBox.Text);
        _config.PreferManagedBridgeInstall = string.IsNullOrWhiteSpace(configuredBridgePath) || PathsEqual(configuredBridgePath, _appPaths.ManagedBridgeJarPath);

        var managedLaunchPath = _appPaths.GetManagedClientExecutablePath(_config.ManagedClientLaunchStream);
        if (_config.PreferManagedClientInstall && File.Exists(managedLaunchPath))
        {
            _config.ClientExecutablePath = managedLaunchPath;
            _clientExecutableTextBox.Text = _config.ClientExecutablePath;
        }

        _config.BridgeJarPath = configuredBridgePath;
        if (_config.PreferManagedBridgeInstall && File.Exists(_appPaths.ManagedBridgeJarPath))
        {
            _config.BridgeJarPath = _appPaths.ManagedBridgeJarPath;
            _bridgeJarTextBox.Text = _config.BridgeJarPath;
        }

        _config.JavaExecutablePath = string.IsNullOrWhiteSpace(_javaExecutableTextBox.Text) ? "java" : _javaExecutableTextBox.Text.Trim();
        _config.FirstBridgePort = decimal.ToInt32(_firstBridgePortUpDown.Value);
        _config.CloseBridgeOnExit = _closeBridgeOnExitCheckBox.Checked;
        _config.LaunchArguments = _launchArgumentsTextBox.Text.Trim();
        _config.SyncUsernameFromOnlineAccount = _syncUsernameFromOnlineAccountCheckBox.Checked;
        _config.MicrosoftAuthClientId = string.IsNullOrWhiteSpace(_microsoftAuthClientIdTextBox.Text)
            ? LauncherAuthService.DefaultCompatibilityClientId
            : _microsoftAuthClientIdTextBox.Text.Trim();
        _config.CheckForManagedBridgeUpdatesOnStartup = _checkForManagedBridgeUpdatesOnStartupCheckBox.Checked;
        _config.NotifyWhenManagedBridgeUpdateAvailable = _notifyWhenManagedBridgeUpdateAvailableCheckBox.Checked;
        _config.ManagedBridgeLogLevel = NormalizeManagedBridgeLogLevel(_managedBridgeLogLevelComboBox.SelectedItem as string);
        _config.ManagedBridgeLogPackets = _managedBridgeLogPacketsCheckBox.Checked;
        _config.SelectedServerId = (_selectedServerComboBox.SelectedItem as ServerSelectionItem)?.ServerId ?? _config.SelectedServerId;
        _config.CheckForManagedClientUpdatesOnStartup = _checkForManagedClientUpdatesOnStartupCheckBox.Checked;
        _config.NotifyWhenManagedClientUpdateAvailable = _notifyWhenManagedClientUpdateAvailableCheckBox.Checked;

        _serverManager.Normalize(_config);
        _configService.Save(_config);
        RefreshServerViews();
        RefreshOnlineAccountStatus();
        RefreshStatus();
        RefreshManagedBridgeStatus();
        RefreshManagedInstallStatus();
    }

    private void RefreshServerViews()
    {
        _serversListView.BeginUpdate();
        _serversListView.Items.Clear();

        foreach (var server in _config.Servers)
        {
            var remote = $"{server.RemoteAddress}:{server.RemotePort}";
            var clientTarget = server.Type == ServerType.JavaBridge ? $"127.0.0.1:{server.LocalBridgePort}" : remote;
            var notes = server.Type == ServerType.JavaBridge && server.RequiresOnlineAuth ? "Requires online auth" : "Ready";

            var item = new ListViewItem(server.DisplayName) { Tag = server.Id };
            item.SubItems.Add(server.Type == ServerType.JavaBridge ? "Java Bridge" : "Native LCE");
            item.SubItems.Add(remote);
            item.SubItems.Add(clientTarget);
            item.SubItems.Add(notes);
            _serversListView.Items.Add(item);
        }

        _serversListView.EndUpdate();
        ResizeServerListColumns();

        var selectionItems = new List<ServerSelectionItem> { new(null, "Client only") };
        selectionItems.AddRange(_config.Servers.Select(server => new ServerSelectionItem(server.Id, $"{server.DisplayName} ({(server.Type == ServerType.JavaBridge ? "Java" : "LCE")})")));

        _selectedServerComboBox.BeginUpdate();
        _selectedServerComboBox.Items.Clear();
        _selectedServerComboBox.Items.AddRange(selectionItems.Cast<object>().ToArray());
        _selectedServerComboBox.SelectedItem = selectionItems.FirstOrDefault(item => item.ServerId == _config.SelectedServerId) ?? selectionItems[0];
        _selectedServerComboBox.EndUpdate();
    }

    private void RefreshStatus()
    {
        var selectedServer = GetSelectedServer();
        var onlineProfile = _launcherAuthService.GetCachedProfile();
        var username = _authModeComboBox.SelectedIndex == 1 && _syncUsernameFromOnlineAccountCheckBox.Checked && onlineProfile is not null
            ? onlineProfile.MinecraftUsername
            : string.IsNullOrWhiteSpace(_localUsernameTextBox.Text) ? "Player" : _localUsernameTextBox.Text.Trim();
        var authModeText = _authModeComboBox.SelectedIndex == 1 ? "Online" : "Local";
        var clientConfigured = !string.IsNullOrWhiteSpace(_clientExecutableTextBox.Text);
        var onlineSelected = _authModeComboBox.SelectedIndex == 1;
        var onlineReady = !onlineSelected || onlineProfile is not null;

        if (!clientConfigured)
        {
            _promoTitleLabel.Text = "Install a managed client stream";
            _selectedServerDetailsLabel.Text = "No client install is configured yet. Install the managed release or debug client into AppData, or set a custom client executable path.";
            _heroServerLabel.Text = "Client not installed";
        }
        else if (onlineSelected && onlineProfile is null)
        {
            _promoTitleLabel.Text = "Sign in to use online auth";
            _selectedServerDetailsLabel.Text = "Online auth mode is selected, but no Microsoft/Minecraft account is signed in. Open Settings and sign in with device code first.";
            _heroServerLabel.Text = "Online account required";
        }
        else if (selectedServer is null)
        {
            _promoTitleLabel.Text = onlineSelected ? "Launch the authenticated client shell" : "Launch the local client shell";
            _selectedServerDetailsLabel.Text = onlineSelected
                ? "The launcher will sync username.txt from the signed-in account, update servers.db, and launch the client without starting a bridge route."
                : "The launcher will update username.txt and servers.db, then start the client without spinning up a bridge route.";
            _heroServerLabel.Text = "Client only, no bridge target selected";
        }
        else if (selectedServer.Type == ServerType.JavaBridge)
        {
            _promoTitleLabel.Text = onlineSelected
                ? "Java bridge route ready with online auth"
                : selectedServer.RequiresOnlineAuth ? "Server saved, waiting for online auth" : "Java bridge route ready to launch";
            _selectedServerDetailsLabel.Text = onlineSelected
                ? $"Server {selectedServer.DisplayName} will be exposed to the client as 127.0.0.1:{selectedServer.LocalBridgePort} and the bridge will connect upstream using {onlineProfile!.MinecraftUsername}."
                : $"Server {selectedServer.DisplayName} will be exposed to the client as 127.0.0.1:{selectedServer.LocalBridgePort}. {(selectedServer.RequiresOnlineAuth ? "Switch the launcher to online auth mode and sign in before launching it." : "Phase one can launch it through the bundled bridge.")}";
            _heroServerLabel.Text = $"{selectedServer.DisplayName} via 127.0.0.1:{selectedServer.LocalBridgePort}";
        }
        else
        {
            _promoTitleLabel.Text = "Direct Legacy server selected";
            _selectedServerDetailsLabel.Text = $"Native LCE entry {selectedServer.DisplayName} remains direct at {selectedServer.RemoteAddress}:{selectedServer.RemotePort}.";
            _heroServerLabel.Text = $"{selectedServer.DisplayName} direct to {selectedServer.RemoteAddress}:{selectedServer.RemotePort}";
        }

        _bridgeStatusLabel.Text = _bridgeRuntimeManager.StatusText;
        _promoStatusPill.Text = _authModeComboBox.SelectedIndex == 1 ? "ONLINE MODE ACTIVE" : "LOCAL MODE ACTIVE";
        _promoStatusPill.BackColor = _authModeComboBox.SelectedIndex == 1 ? DangerRed : AccentGreenDark;

        _heroProfileLabel.Text = $"{username} | {authModeText}";
        _serversCardBodyLabel.Text = $"{_config.Servers.Count} saved server(s). Java routes keep stable localhost mappings, and native LCE entries stay direct.";
        _runtimeCardBodyLabel.Text = $"{_bridgeRuntimeManager.StatusText}. Use the logs page to inspect bridge startup and launcher actions.";
        _launchClientButton.Enabled = clientConfigured && onlineReady;
        _launchSelectedServerButton.Enabled = clientConfigured && onlineReady && (selectedServer is null || onlineSelected || !selectedServer.RequiresOnlineAuth);
    }

    private async Task LaunchAsync(ServerEntry? server)
    {
        try
        {
            SetBusy(true);
            SaveConfigFromControls();
            await _launchCoordinator.LaunchAsync(_config, server, CancellationToken.None);
            RefreshStatus();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            MessageBox.Show(this, ex.Message, "Launch Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RefreshStatus();
        }
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        _launchClientButton.Enabled = !busy;
        _launchSelectedServerButton.Enabled = !busy && _authModeComboBox.SelectedIndex != 1;
        _stopBridgeButton.Enabled = !busy;
        _checkBridgeUpdatesButton.Enabled = !busy;
        _checkNightlyUpdatesButton.Enabled = !busy;
        var installInfo = _clientInstallService.GetManagedInstallInfo(GetSelectedManagedClientInstallStream());
        var managedBridge = _bridgeInstallService.GetManagedInstallInfo();
        var onlineProfile = _launcherAuthService.GetCachedProfile();
        _installNightlyButton.Enabled = !busy;
        _updateNightlyButton.Enabled = !busy && installInfo.IsInstalled;
        _repairNightlyButton.Enabled = !busy;
        _useManagedNightlyButton.Enabled = !busy && installInfo.IsInstalled;
        _openManagedInstallButton.Enabled = !busy && installInfo.IsInstalled;
        _installBridgeButton.Enabled = !busy;
        _updateBridgeButton.Enabled = !busy && managedBridge.IsInstalled;
        _useManagedBridgeButton.Enabled = !busy && managedBridge.IsInstalled;
        _openManagedBridgeButton.Enabled = !busy && managedBridge.IsInstalled;
        _signInButton.Enabled = !busy;
        _signOutButton.Enabled = !busy && onlineProfile is not null;
    }

    private void RefreshManagedBridgeStatus()
    {
        var installInfo = _bridgeInstallService.GetManagedInstallInfo();
        var isUsingManagedBridge = PathsEqual(_bridgeJarTextBox.Text, installInfo.BridgeJarPath);

        _managedBridgeStatusLabel.Text = installInfo.IsInstalled
            ? (isUsingManagedBridge ? $"Installed and active: {installInfo.DisplayVersion}" : $"Installed: {installInfo.DisplayVersion}")
            : "Not installed";

        _managedBridgeDetailsLabel.Text = installInfo.IsInstalled
            ? $"Root: {installInfo.InstallRoot}{Environment.NewLine}Published: {FormatTimestamp(installInfo.PublishedAtUtc)}{Environment.NewLine}Installed: {FormatTimestamp(installInfo.InstalledAtUtc)}"
            : $"Expected install root: {installInfo.InstallRoot}{Environment.NewLine}Use Install Bridge to download the latest LCEBridge release jar into AppData.";

        _managedBridgeDetailsLabel.MaximumSize = new Size(720, 0);
        _managedBridgeUpdateLabel.MaximumSize = new Size(720, 0);
        _managedBridgeLastCheckedLabel.MaximumSize = new Size(720, 0);
        RestoreManagedBridgeUpdateState(installInfo);
        _installBridgeButton.Enabled = true;
        _updateBridgeButton.Enabled = installInfo.IsInstalled;
        _useManagedBridgeButton.Enabled = installInfo.IsInstalled;
        _openManagedBridgeButton.Enabled = installInfo.IsInstalled;
    }

    private void RestoreManagedBridgeUpdateState(ManagedBridgeInstallInfo installInfo)
    {
        if (!installInfo.IsInstalled)
        {
            _managedBridgeUpdateLabel.Text = "Install required";
            _managedBridgeUpdateLabel.ForeColor = AccentGold;
            _managedBridgeLastCheckedLabel.Text = _config.ManagedBridgeLastCheckedAtUtc is null
                ? "Not checked yet"
                : $"Last checked: {FormatTimestamp(_config.ManagedBridgeLastCheckedAtUtc)}";
            _managedBridgeLastCheckedLabel.ForeColor = TextMuted;
            return;
        }

        _managedBridgeUpdateLabel.Text = string.IsNullOrWhiteSpace(_config.ManagedBridgeLastUpdateStatusText)
            ? "Not checked yet"
            : _config.ManagedBridgeLastUpdateStatusText;
        _managedBridgeUpdateLabel.ForeColor = string.Equals(_config.ManagedBridgeLastUpdateStatusText, "Update check failed", StringComparison.Ordinal)
            ? DangerRed
            : _config.ManagedBridgeLastUpdateAvailable ? AccentGold : TextMuted;
        _managedBridgeLastCheckedLabel.Text = _config.ManagedBridgeLastCheckedAtUtc is null
            ? "Not checked yet"
            : $"Last checked: {FormatTimestamp(_config.ManagedBridgeLastCheckedAtUtc)}";
        _managedBridgeLastCheckedLabel.ForeColor = TextMuted;
    }

    private void RefreshOnlineAccountStatus()
    {
        var profile = _launcherAuthService.GetCachedProfile();
        var effectiveClientId = _launcherAuthService.GetEffectiveClientId();
        _onlineAccountStatusLabel.Text = profile is null
            ? "Not signed in"
            : $"Signed in: {profile.MinecraftUsername}";

        _onlineAccountDetailsLabel.Text = profile is null
            ? $"Use device-code sign-in to acquire a Minecraft Java session for the launcher-managed bridge.{Environment.NewLine}Client ID: {effectiveClientId}"
            : $"Minecraft: {profile.MinecraftUsername}{Environment.NewLine}Profile ID: {profile.MinecraftProfileId}{Environment.NewLine}Microsoft account: {profile.MicrosoftUsername}{Environment.NewLine}Client ID: {effectiveClientId}{Environment.NewLine}Last authenticated: {FormatTimestamp(profile.LastAuthenticatedAtUtc)}";

        _onlineAccountDetailsLabel.MaximumSize = new Size(720, 0);
        _signOutButton.Enabled = profile is not null;
    }

    private void RefreshManagedInstallStatus()
    {
        var installStream = GetSelectedManagedClientInstallStream();
        var launchStream = GetSelectedManagedClientLaunchStream();
        var installInfo = _clientInstallService.GetManagedInstallInfo(installStream);
        var isUsingManagedClient = PathsEqual(_clientExecutableTextBox.Text, installInfo.ClientExecutablePath);
        var streamName = installStream.GetDisplayName();
        _managedClientSourceLabel.Text = installStream == ManagedClientStream.Debug
            ? "LCEDebug nightly release from veroxsity/LCEDebug"
            : "SmartCMD nightly release from smartcmd/MinecraftConsoles";

        _managedClientStatusLabel.Text = installInfo.IsInstalled
            ? (isUsingManagedClient ? $"Installed and active: {installInfo.DisplayVersion}" : $"Installed: {installInfo.DisplayVersion}")
            : "Not installed";

        _managedClientDetailsLabel.Text = installInfo.IsInstalled
            ? $"Stream: {streamName}{Environment.NewLine}Root: {installInfo.InstallRoot}{Environment.NewLine}Published: {FormatTimestamp(installInfo.PublishedAtUtc)}{Environment.NewLine}Installed: {FormatTimestamp(installInfo.InstalledAtUtc)}"
            : $"Expected install root: {installInfo.InstallRoot}{Environment.NewLine}Use Install Client to download the managed {streamName.ToLowerInvariant()} build into AppData and extract it here.";

        _managedClientDetailsLabel.MaximumSize = new Size(720, 0);
        _managedClientUpdateLabel.MaximumSize = new Size(720, 0);
        _managedClientLastCheckedLabel.MaximumSize = new Size(720, 0);

        RestoreManagedInstallUpdateState(installInfo, installStream);
        _updateNightlyButton.Enabled = installInfo.IsInstalled;
        _repairNightlyButton.Enabled = true;
        _useManagedNightlyButton.Enabled = _clientInstallService.GetManagedInstallInfo(launchStream).IsInstalled;
        _openManagedInstallButton.Enabled = installInfo.IsInstalled;
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            SetBusy(true);
            _managedClientUpdateLabel.Text = $"Checking {GetSelectedManagedClientInstallStream().GetDisplayName().ToLowerInvariant()} release...";
            _managedClientUpdateLabel.ForeColor = TextMuted;
            await RefreshManagedInstallUpdateStatusAsync(GetSelectedManagedClientInstallStream(), notifyIfAvailable: false, updateUi: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CheckBridgeForUpdatesAsync()
    {
        try
        {
            SetBusy(true);
            _managedBridgeUpdateLabel.Text = "Checking latest bridge release...";
            _managedBridgeUpdateLabel.ForeColor = TextMuted;
            await RefreshManagedBridgeUpdateStatusAsync(notifyIfAvailable: false);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task InitializeManagedRuntimeUpdateChecksAsync()
    {
        RefreshManagedBridgeStatus();
        RefreshManagedInstallStatus();

        if (_config.CheckForManagedBridgeUpdatesOnStartup)
        {
            await CheckForStartupBridgeUpdatesAsync();
        }

        if (!_config.CheckForManagedClientUpdatesOnStartup)
        {
            return;
        }

        await CheckForStartupUpdatesAsync();
    }

    private async Task CheckForStartupBridgeUpdatesAsync()
    {
        try
        {
            SetBusy(true);
            _managedBridgeUpdateLabel.Text = "Checking latest bridge release...";
            _managedBridgeUpdateLabel.ForeColor = TextMuted;
            await RefreshManagedBridgeUpdateStatusAsync(notifyIfAvailable: _config.NotifyWhenManagedBridgeUpdateAvailable);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task CheckForStartupUpdatesAsync()
    {
        try
        {
            SetBusy(true);
            if (GetSelectedManagedClientInstallStream() == _config.ManagedClientLaunchStream)
            {
                _managedClientUpdateLabel.Text = $"Checking {_config.ManagedClientLaunchStream.GetDisplayName().ToLowerInvariant()} release...";
                _managedClientUpdateLabel.ForeColor = TextMuted;
            }

            await RefreshManagedInstallUpdateStatusAsync(_config.ManagedClientLaunchStream, notifyIfAvailable: _config.NotifyWhenManagedClientUpdateAvailable, updateUi: GetSelectedManagedClientInstallStream() == _config.ManagedClientLaunchStream);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RefreshManagedInstallUpdateStatusAsync(ManagedClientStream stream, bool notifyIfAvailable, bool updateUi)
    {
        var checkedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            var updateInfo = await _clientInstallService.GetUpdateInfoAsync(stream, CancellationToken.None);
            ApplyManagedInstallUpdateInfo(updateInfo, checkedAtUtc, notifyIfAvailable, updateUi);
        }
        catch (Exception ex)
        {
            _logger.Warn($"{stream.GetDisplayName()} update check failed: {ex.Message}");
            ApplyManagedInstallUpdateFailure(stream, checkedAtUtc, updateUi);
        }
    }

    private async Task RefreshManagedBridgeUpdateStatusAsync(bool notifyIfAvailable)
    {
        var checkedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            var updateInfo = await _bridgeInstallService.GetLatestBridgeUpdateInfoAsync(CancellationToken.None);
            ApplyManagedBridgeUpdateInfo(updateInfo, checkedAtUtc, notifyIfAvailable);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Bridge update check failed: {ex.Message}");
            ApplyManagedBridgeUpdateFailure(checkedAtUtc);
        }
    }

    private void ApplyManagedBridgeUpdateInfo(ManagedBridgeUpdateInfo updateInfo, DateTimeOffset checkedAtUtc, bool notifyIfAvailable)
    {
        _config.ManagedBridgeLastCheckedAtUtc = checkedAtUtc;
        _config.ManagedBridgeLastUpdateStatusText = updateInfo.StatusText;
        _config.ManagedBridgeLastUpdateAvailable = updateInfo.UpdateAvailable;
        _config.ManagedBridgeLastKnownLatestVersion = string.IsNullOrWhiteSpace(updateInfo.LatestVersion) ? null : updateInfo.LatestVersion;

        if (!updateInfo.UpdateAvailable)
        {
            _config.ManagedBridgeLastNotifiedVersion = null;
        }

        if (notifyIfAvailable &&
            updateInfo.UpdateAvailable &&
            !string.IsNullOrWhiteSpace(updateInfo.LatestVersion) &&
            !string.Equals(_config.ManagedBridgeLastNotifiedVersion, updateInfo.LatestVersion, StringComparison.Ordinal))
        {
            _config.ManagedBridgeLastNotifiedVersion = updateInfo.LatestVersion;
            _configService.Save(_config);

            MessageBox.Show(
                this,
                $"A newer managed bridge release is available: {updateInfo.LatestVersion}",
                "Bridge Update Available",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            _configService.Save(_config);
        }

        _managedBridgeUpdateLabel.Text = updateInfo.StatusText;
        _managedBridgeUpdateLabel.ForeColor = updateInfo.UpdateAvailable ? AccentGold : TextMuted;
        _managedBridgeLastCheckedLabel.Text = $"Last checked: {FormatTimestamp(checkedAtUtc)}";
        _managedBridgeLastCheckedLabel.ForeColor = TextMuted;
    }

    private void ApplyManagedBridgeUpdateFailure(DateTimeOffset checkedAtUtc)
    {
        _config.ManagedBridgeLastCheckedAtUtc = checkedAtUtc;
        _config.ManagedBridgeLastUpdateStatusText = "Update check failed";
        _config.ManagedBridgeLastUpdateAvailable = false;
        _configService.Save(_config);

        _managedBridgeUpdateLabel.Text = "Update check failed";
        _managedBridgeUpdateLabel.ForeColor = DangerRed;
        _managedBridgeLastCheckedLabel.Text = $"Last checked: {FormatTimestamp(checkedAtUtc)}";
        _managedBridgeLastCheckedLabel.ForeColor = TextMuted;
    }

    private void ApplyManagedInstallUpdateInfo(ManagedClientUpdateInfo updateInfo, DateTimeOffset checkedAtUtc, bool notifyIfAvailable, bool updateUi)
    {
        var state = GetManagedClientUpdateState(updateInfo.Stream);
        state.LastCheckedAtUtc = checkedAtUtc;
        state.LastUpdateStatusText = updateInfo.StatusText;
        state.LastUpdateAvailable = updateInfo.UpdateAvailable;
        state.LastKnownLatestVersion = string.IsNullOrWhiteSpace(updateInfo.LatestVersion) ? null : updateInfo.LatestVersion;

        if (!updateInfo.UpdateAvailable)
        {
            state.LastNotifiedVersion = null;
        }

        if (notifyIfAvailable &&
            updateInfo.UpdateAvailable &&
            !string.IsNullOrWhiteSpace(updateInfo.LatestVersion) &&
            !string.Equals(state.LastNotifiedVersion, updateInfo.LatestVersion, StringComparison.Ordinal))
        {
            state.LastNotifiedVersion = updateInfo.LatestVersion;
            _configService.Save(_config);

            MessageBox.Show(
                this,
                $"A newer managed {updateInfo.StreamLabel.ToLowerInvariant()} client build is available: {updateInfo.LatestVersion}",
                "Client Update Available",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            _configService.Save(_config);
        }

        if (updateUi)
        {
            _managedClientUpdateLabel.Text = updateInfo.StatusText;
            _managedClientUpdateLabel.ForeColor = updateInfo.UpdateAvailable ? AccentGold : TextMuted;
            _managedClientLastCheckedLabel.Text = $"Last checked: {FormatTimestamp(checkedAtUtc)}";
            _managedClientLastCheckedLabel.ForeColor = TextMuted;
        }
    }

    private void ApplyManagedInstallUpdateFailure(ManagedClientStream stream, DateTimeOffset checkedAtUtc, bool updateUi)
    {
        var state = GetManagedClientUpdateState(stream);
        state.LastCheckedAtUtc = checkedAtUtc;
        state.LastUpdateStatusText = "Update check failed";
        state.LastUpdateAvailable = false;
        _configService.Save(_config);

        if (updateUi)
        {
            _managedClientUpdateLabel.Text = "Update check failed";
            _managedClientUpdateLabel.ForeColor = DangerRed;
            _managedClientLastCheckedLabel.Text = $"Last checked: {FormatTimestamp(checkedAtUtc)}";
            _managedClientLastCheckedLabel.ForeColor = TextMuted;
        }
    }

    private async Task SignInAsync()
    {
        try
        {
            SetBusy(true);
            SaveConfigFromControls();
            var profile = await _launcherAuthService.SignInAsync(deviceCode =>
            {
                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        Clipboard.SetText(deviceCode.UserCode);
                    }
                    catch
                    {
                    }

                    OpenVerificationUri(deviceCode.VerificationUrl);
                    MessageBox.Show(
                        this,
                        $"{deviceCode.Message}{Environment.NewLine}{Environment.NewLine}The code has been copied to your clipboard.",
                        "Microsoft Sign-In",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }));

                return Task.CompletedTask;
            }, CancellationToken.None);

            if (_config.SyncUsernameFromOnlineAccount)
            {
                _localUsernameTextBox.Text = profile.MinecraftUsername;
            }

            RefreshOnlineAccountStatus();
            RefreshStatus();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            MessageBox.Show(this, ex.Message, "Sign-In Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RefreshOnlineAccountStatus();
            RefreshStatus();
        }
    }

    private async Task SignOutAsync()
    {
        var result = MessageBox.Show(
            this,
            "Sign out the saved online account and clear the local launcher token cache?",
            "Sign Out",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            SetBusy(true);
            await _launcherAuthService.SignOutAsync();
            RefreshOnlineAccountStatus();
            RefreshStatus();
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            MessageBox.Show(this, ex.Message, "Sign-Out Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RefreshOnlineAccountStatus();
            RefreshStatus();
        }
    }

    private async Task InstallNightlyAsync()
    {
        var stream = GetSelectedManagedClientInstallStream();
        try
        {
            SetBusy(true);
            var installInfo = await _clientInstallService.InstallAsync(stream, CancellationToken.None);
            if (_config.ManagedClientLaunchStream == stream)
            {
                _config.PreferManagedClientInstall = true;
                _config.ClientExecutablePath = installInfo.ClientExecutablePath;
                _clientExecutableTextBox.Text = installInfo.ClientExecutablePath;
            }

            _configService.Save(_config);
            RefreshStatus();
            RefreshManagedInstallStatus();
            await RefreshManagedInstallUpdateStatusAsync(stream, notifyIfAvailable: false, updateUi: true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            MessageBox.Show(this, ex.Message, "Install Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RefreshManagedInstallStatus();
            await RefreshManagedInstallUpdateStatusAsync(stream, notifyIfAvailable: false, updateUi: true);
        }
    }

    private async Task UpdateManagedNightlyAsync()
    {
        var stream = GetSelectedManagedClientInstallStream();
        try
        {
            SetBusy(true);
            var installInfo = await _clientInstallService.UpdateAsync(stream, CancellationToken.None);
            if (_config.PreferManagedClientInstall || PathsEqual(_clientExecutableTextBox.Text, installInfo.ClientExecutablePath))
            {
                _config.PreferManagedClientInstall = true;
                _config.ManagedClientLaunchStream = stream;
                _managedClientLaunchStreamComboBox.SelectedItem = FindManagedClientStreamSelectionItem(stream);
                _config.ClientExecutablePath = installInfo.ClientExecutablePath;
                _clientExecutableTextBox.Text = installInfo.ClientExecutablePath;
                _configService.Save(_config);
            }

            RefreshStatus();
            RefreshManagedInstallStatus();
            await RefreshManagedInstallUpdateStatusAsync(stream, notifyIfAvailable: false, updateUi: true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            MessageBox.Show(this, ex.Message, "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RefreshManagedInstallStatus();
            await RefreshManagedInstallUpdateStatusAsync(stream, notifyIfAvailable: false, updateUi: true);
        }
    }

    private async Task RepairManagedNightlyAsync()
    {
        var stream = GetSelectedManagedClientInstallStream();
        var installInfo = _clientInstallService.GetManagedInstallInfo(stream);
        var prompt = installInfo.IsInstalled
            ? $"Repair will reinstall the full {stream.GetDisplayName().ToLowerInvariant()} client into AppData and preserve uid.dat, username.txt, and servers.db. Continue?"
            : $"No managed {stream.GetDisplayName().ToLowerInvariant()} install was found. Repair will install the full {stream.GetDisplayName().ToLowerInvariant()} client into AppData. Continue?";
        var result = MessageBox.Show(
            this,
            prompt,
            installInfo.IsInstalled ? "Repair Client" : "Install Client",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var repairedInstall = await _clientInstallService.RepairAsync(stream, CancellationToken.None);
            if (_config.PreferManagedClientInstall || !installInfo.IsInstalled || PathsEqual(_clientExecutableTextBox.Text, installInfo.ClientExecutablePath))
            {
                _config.PreferManagedClientInstall = true;
                _config.ManagedClientLaunchStream = stream;
                _managedClientLaunchStreamComboBox.SelectedItem = FindManagedClientStreamSelectionItem(stream);
                _config.ClientExecutablePath = repairedInstall.ClientExecutablePath;
                _clientExecutableTextBox.Text = repairedInstall.ClientExecutablePath;
                _configService.Save(_config);
            }

            RefreshStatus();
            RefreshManagedInstallStatus();
            await RefreshManagedInstallUpdateStatusAsync(stream, notifyIfAvailable: false, updateUi: true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            MessageBox.Show(this, ex.Message, "Repair Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RefreshManagedInstallStatus();
            await RefreshManagedInstallUpdateStatusAsync(stream, notifyIfAvailable: false, updateUi: true);
        }
    }

    private async Task InstallManagedBridgeAsync()
    {
        try
        {
            SetBusy(true);
            var installInfo = await _bridgeInstallService.InstallLatestReleaseAsync(CancellationToken.None);
            _config.PreferManagedBridgeInstall = true;
            _config.BridgeJarPath = installInfo.BridgeJarPath;
            _bridgeJarTextBox.Text = installInfo.BridgeJarPath;
            _configService.Save(_config);
            RefreshManagedBridgeStatus();
            RefreshStatus();
            await RefreshManagedBridgeUpdateStatusAsync(notifyIfAvailable: false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            MessageBox.Show(this, ex.Message, "Bridge Install Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RefreshManagedBridgeStatus();
        }
    }

    private async Task UpdateManagedBridgeAsync()
    {
        try
        {
            SetBusy(true);
            var installInfo = await _bridgeInstallService.UpdateBridgeAsync(CancellationToken.None);
            if (_config.PreferManagedBridgeInstall || PathsEqual(_bridgeJarTextBox.Text, installInfo.BridgeJarPath))
            {
                _config.PreferManagedBridgeInstall = true;
                _config.BridgeJarPath = installInfo.BridgeJarPath;
                _bridgeJarTextBox.Text = installInfo.BridgeJarPath;
                _configService.Save(_config);
            }

            RefreshManagedBridgeStatus();
            RefreshStatus();
            await RefreshManagedBridgeUpdateStatusAsync(notifyIfAvailable: false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            MessageBox.Show(this, ex.Message, "Bridge Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RefreshManagedBridgeStatus();
            await RefreshManagedBridgeUpdateStatusAsync(notifyIfAvailable: false);
        }
    }

    private void UseManagedBridgeInstall()
    {
        var installInfo = _bridgeInstallService.GetManagedInstallInfo();
        if (!installInfo.IsInstalled)
        {
            MessageBox.Show(this, "The managed bridge runtime is not installed yet.", "Managed Bridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _config.PreferManagedBridgeInstall = true;
        _config.BridgeJarPath = installInfo.BridgeJarPath;
        _bridgeJarTextBox.Text = installInfo.BridgeJarPath;
        _configService.Save(_config);
        RefreshManagedBridgeStatus();
    }

    private void UseManagedNightlyInstall()
    {
        var stream = GetSelectedManagedClientLaunchStream();
        var installInfo = _clientInstallService.GetManagedInstallInfo(stream);
        if (!installInfo.IsInstalled)
        {
            MessageBox.Show(this, $"The managed {stream.GetDisplayName().ToLowerInvariant()} client is not installed yet.", "Managed Client", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _config.PreferManagedClientInstall = true;
        _config.ManagedClientLaunchStream = stream;
        _config.ClientExecutablePath = installInfo.ClientExecutablePath;
        _clientExecutableTextBox.Text = installInfo.ClientExecutablePath;
        _configService.Save(_config);
        RefreshStatus();
        RefreshManagedInstallStatus();
        _ = RefreshManagedInstallUpdateStatusAsync(GetSelectedManagedClientInstallStream(), notifyIfAvailable: false, updateUi: true);
    }

    private void AddServer()
    {
        var entry = new ServerEntry { DisplayName = "New Server", RemoteAddress = "127.0.0.1", RemotePort = 25565 };
        using var dialog = new ServerEditForm(entry);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var saved = dialog.Server;
        if (saved.Type == ServerType.JavaBridge)
        {
            saved.LocalBridgePort = _serverManager.AllocateNextBridgePort(_config);
        }

        _config.Servers.Add(saved);
        _config.SelectedServerId = saved.Id;
        SaveConfigFromControls();
        ShowPage(PageServers);
    }

    private void EditSelectedServer()
    {
        var server = GetListSelection();
        if (server is null) return;

        using var dialog = new ServerEditForm(server);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var updated = dialog.Server;
        if (updated.Type == ServerType.JavaBridge && updated.LocalBridgePort is null)
        {
            updated.LocalBridgePort = server.LocalBridgePort ?? _serverManager.AllocateNextBridgePort(_config);
        }

        var index = _config.Servers.FindIndex(item => item.Id == server.Id);
        if (index >= 0)
        {
            _config.Servers[index] = updated;
            _config.SelectedServerId = updated.Id;
        }

        SaveConfigFromControls();
    }

    private void DeleteSelectedServer()
    {
        var server = GetListSelection();
        if (server is null) return;

        var result = MessageBox.Show(this, $"Delete {server.DisplayName} from the launcher server list?", "Delete Server", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        _config.Servers.RemoveAll(item => item.Id == server.Id);
        if (_config.SelectedServerId == server.Id) _config.SelectedServerId = null;
        SaveConfigFromControls();
    }

    private ServerEntry? GetSelectedServer()
    {
        var selection = _selectedServerComboBox.SelectedItem as ServerSelectionItem;
        return selection?.ServerId is null ? null : _config.Servers.FirstOrDefault(server => server.Id == selection.ServerId);
    }

    private ServerEntry? GetListSelection()
    {
        var item = _serversListView.SelectedItems.Count > 0 ? _serversListView.SelectedItems[0] : null;
        var serverId = item?.Tag as string;
        return serverId is null ? null : _config.Servers.FirstOrDefault(server => server.Id == serverId);
    }

    private void BrowseForFile(TextBox target, string filter)
    {
        using var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        if (dialog.ShowDialog(this) == DialogResult.OK) target.Text = dialog.FileName;
    }

    private string? GetClientFolderPath()
    {
        var clientExe = NullIfWhitespace(_clientExecutableTextBox.Text);
        return clientExe is null ? null : Path.GetDirectoryName(clientExe);
    }

    private static void OpenDirectorySafely(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private static void OpenVerificationUri(string? verificationUrl)
    {
        if (string.IsNullOrWhiteSpace(verificationUrl))
        {
            return;
        }

        Process.Start(new ProcessStartInfo { FileName = verificationUrl, UseShellExecute = true });
    }

    private static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatTimestamp(DateTimeOffset? timestamp) =>
        timestamp?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "Unknown";

    private ManagedClientUpdateState GetManagedClientUpdateState(ManagedClientStream stream)
    {
        var key = stream.GetKey();
        if (!_config.ManagedClientUpdateStates.TryGetValue(key, out var state))
        {
            state = new ManagedClientUpdateState();
            _config.ManagedClientUpdateStates[key] = state;
        }

        return state;
    }

    private ManagedClientStream GetSelectedManagedClientInstallStream() =>
        (_managedClientInstallStreamComboBox.SelectedItem as ManagedClientStreamSelectionItem)?.Stream ?? _config.ManagedClientInstallStream;

    private ManagedClientStream GetSelectedManagedClientLaunchStream() =>
        (_managedClientLaunchStreamComboBox.SelectedItem as ManagedClientStreamSelectionItem)?.Stream ?? _config.ManagedClientLaunchStream;

    private ManagedClientStreamSelectionItem? FindManagedClientStreamSelectionItem(ManagedClientStream stream) =>
        _managedClientInstallStreamComboBox.Items
            .OfType<ManagedClientStreamSelectionItem>()
            .FirstOrDefault(item => item.Stream == stream);

    private bool TryGetManagedClientStreamForPath(string? path, out ManagedClientStream stream)
    {
        foreach (var candidate in Enum.GetValues<ManagedClientStream>())
        {
            if (PathsEqual(path, _appPaths.GetManagedClientExecutablePath(candidate)))
            {
                stream = candidate;
                return true;
            }
        }

        stream = ManagedClientStream.Release;
        return false;
    }

    private void SyncManagedClientPathForSelectedLaunchStream()
    {
        if (!_config.PreferManagedClientInstall && !TryGetManagedClientStreamForPath(_clientExecutableTextBox.Text, out _))
        {
            return;
        }

        var launchPath = _appPaths.GetManagedClientExecutablePath(GetSelectedManagedClientLaunchStream());
        _clientExecutableTextBox.Text = File.Exists(launchPath) ? launchPath : string.Empty;
    }

    private void RestoreManagedInstallUpdateState(ManagedClientInstallInfo installInfo, ManagedClientStream stream)
    {
        var state = GetManagedClientUpdateState(stream);

        if (!installInfo.IsInstalled)
        {
            _managedClientUpdateLabel.Text = "Install required";
            _managedClientUpdateLabel.ForeColor = AccentGold;
            _managedClientLastCheckedLabel.Text = state.LastCheckedAtUtc is null
                ? "Not checked yet"
                : $"Last checked: {FormatTimestamp(state.LastCheckedAtUtc)}";
            _managedClientLastCheckedLabel.ForeColor = TextMuted;
            return;
        }

        _managedClientUpdateLabel.Text = string.IsNullOrWhiteSpace(state.LastUpdateStatusText)
            ? "Not checked yet"
            : state.LastUpdateStatusText;
        _managedClientUpdateLabel.ForeColor = string.Equals(state.LastUpdateStatusText, "Update check failed", StringComparison.Ordinal)
            ? DangerRed
            : state.LastUpdateAvailable ? AccentGold : TextMuted;
        _managedClientLastCheckedLabel.Text = state.LastCheckedAtUtc is null
            ? "Not checked yet"
            : $"Last checked: {FormatTimestamp(state.LastCheckedAtUtc)}";
        _managedClientLastCheckedLabel.ForeColor = TextMuted;
    }

    private void AppendExistingLogs()
    {
        _visibleLogLines.Clear();
        foreach (var line in _logger.Snapshot())
        {
            EnqueueVisibleLogLine(line);
        }
        RenderVisibleLogs();
    }

    private void ClearBridgeLogs()
    {
        var latestLogPath = Path.Combine(_appPaths.ManagedBridgeLogsRoot, "latest.log");
        var result = MessageBox.Show(
            this,
            "Clear the visible launcher log and delete the managed bridge latest.log file?",
            "Clear Logs",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(_appPaths.ManagedBridgeLogsRoot);
            if (File.Exists(latestLogPath))
            {
                File.Delete(latestLogPath);
            }

            _logger.Clear();
            while (_pendingLogLines.TryDequeue(out _))
            {
            }
            _visibleLogLines.Clear();
            _logsTextBox.Clear();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to clear bridge logs: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Clear Logs Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EnsureLogFlushTimerRunning()
    {
        if (!_logFlushTimer.Enabled)
        {
            _logFlushTimer.Start();
        }
    }

    private void FlushPendingLogs()
    {
        var appended = 0;
        while (appended < LogFlushBatchSize && _pendingLogLines.TryDequeue(out var line))
        {
            EnqueueVisibleLogLine(line);
            appended++;
        }

        if (appended > 0)
        {
            if (string.Equals(_currentPageKey, PageLogs, StringComparison.Ordinal))
            {
                RenderVisibleLogs();
            }
        }

        if (_pendingLogLines.IsEmpty)
        {
            Interlocked.Exchange(ref _logFlushScheduled, 0);
            if (_pendingLogLines.IsEmpty)
            {
                _logFlushTimer.Stop();
            }
            else
            {
                EnsureLogFlushTimerRunning();
            }
        }
    }

    private void EnqueueVisibleLogLine(string line)
    {
        _visibleLogLines.Enqueue(line);
        while (_visibleLogLines.Count > MaxVisibleLogLines)
        {
            _visibleLogLines.Dequeue();
        }
    }

    private void RenderVisibleLogs()
    {
        if (IsDisposed || !string.Equals(_currentPageKey, PageLogs, StringComparison.Ordinal))
        {
            return;
        }

        _logsTextBox.Lines = _visibleLogLines.ToArray();
        _logsTextBox.SelectionStart = _logsTextBox.TextLength;
        _logsTextBox.ScrollToCaret();
    }

    private void ShowPage(string pageKey)
    {
        _currentPageKey = pageKey;
        foreach (var pair in _pages)
        {
            pair.Value.Visible = pair.Key == pageKey;
        }

        if (_pages.TryGetValue(pageKey, out var selectedPage))
        {
            selectedPage.BringToFront();
        }

        foreach (var (key, button) in _pageButtons)
        {
            var selected = key == pageKey || (pageKey == PageHome && key == PageHome && button.Text.Contains("MINECRAFT"));
            button.BackColor = selected ? SurfaceRaised : SidebarBackground;
            button.ForeColor = selected ? TextPrimary : TextMuted;
            button.Tag = selected;
            button.Invalidate();
        }

        foreach (var (key, button) in _topButtons)
        {
            var selected = key == pageKey;
            button.ForeColor = selected ? TextPrimary : TextMuted;
            button.Tag = selected;
            button.Invalidate();
        }

        if (string.Equals(pageKey, PageLogs, StringComparison.Ordinal))
        {
            RenderVisibleLogs();
        }
    }

    private Image? LoadAsset(string fileName)
    {
        var paths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", fileName),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", fileName),
            Path.Combine(Environment.CurrentDirectory, "Assets", fileName),
        };

        foreach (var candidate in paths.Select(Path.GetFullPath))
        {
            if (File.Exists(candidate))
            {
                return Image.FromFile(candidate);
            }
        }

        return null;
    }

    private static string? NullIfWhitespace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeManagedBridgeLogLevel(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized is "trace" or "debug" or "info" or "warn" or "error"
            ? normalized
            : "info";
    }

    private sealed record ServerSelectionItem(string? ServerId, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed record ManagedClientStreamSelectionItem(ManagedClientStream Stream, string Label)
    {
        public override string ToString() => Label;
    }
}
