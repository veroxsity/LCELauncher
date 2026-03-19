using System.Diagnostics;
using LceLauncher.Models;

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

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLog(line)));
            }
            else
            {
                AppendLog(line);
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
        _serversListView.DoubleClick += (_, _) => EditSelectedServer();
        Shown += async (_, _) => await InitializeManagedClientUpdateChecksAsync();
    }

    private void LoadConfigIntoControls()
    {
        if (_config.PreferManagedClientInstall && File.Exists(_appPaths.NightlyClientExecutablePath))
        {
            _config.ClientExecutablePath = _appPaths.NightlyClientExecutablePath;
        }

        _authModeComboBox.SelectedIndex = _config.AuthMode == AuthMode.Online ? 1 : 0;
        _localUsernameTextBox.Text = _config.LocalUsername;
        _clientExecutableTextBox.Text = _config.ClientExecutablePath ?? string.Empty;
        _bridgeJarTextBox.Text = _config.BridgeJarPath ?? string.Empty;
        _javaExecutableTextBox.Text = _config.JavaExecutablePath;
        _firstBridgePortUpDown.Value = Math.Clamp(_config.FirstBridgePort, (int)_firstBridgePortUpDown.Minimum, (int)_firstBridgePortUpDown.Maximum);
        _closeBridgeOnExitCheckBox.Checked = _config.CloseBridgeOnExit;
        _launchArgumentsTextBox.Text = _config.LaunchArguments;
        _checkForManagedClientUpdatesOnStartupCheckBox.Checked = _config.CheckForManagedClientUpdatesOnStartup;
        _notifyWhenManagedClientUpdateAvailableCheckBox.Checked = _config.NotifyWhenManagedClientUpdateAvailable;
    }

    private void SaveConfigFromControls()
    {
        var configuredClientPath = NullIfWhitespace(_clientExecutableTextBox.Text);
        _config.AuthMode = _authModeComboBox.SelectedIndex == 1 ? AuthMode.Online : AuthMode.Local;
        _config.LocalUsername = string.IsNullOrWhiteSpace(_localUsernameTextBox.Text) ? "Player" : _localUsernameTextBox.Text.Trim();
        _config.PreferManagedClientInstall = string.IsNullOrWhiteSpace(configuredClientPath) || PathsEqual(configuredClientPath, _appPaths.NightlyClientExecutablePath);
        _config.ClientExecutablePath = configuredClientPath;

        if (_config.PreferManagedClientInstall && File.Exists(_appPaths.NightlyClientExecutablePath))
        {
            _config.ClientExecutablePath = _appPaths.NightlyClientExecutablePath;
            _clientExecutableTextBox.Text = _config.ClientExecutablePath;
        }

        _config.BridgeJarPath = NullIfWhitespace(_bridgeJarTextBox.Text);
        _config.JavaExecutablePath = string.IsNullOrWhiteSpace(_javaExecutableTextBox.Text) ? "java" : _javaExecutableTextBox.Text.Trim();
        _config.FirstBridgePort = decimal.ToInt32(_firstBridgePortUpDown.Value);
        _config.CloseBridgeOnExit = _closeBridgeOnExitCheckBox.Checked;
        _config.LaunchArguments = _launchArgumentsTextBox.Text.Trim();
        _config.SelectedServerId = (_selectedServerComboBox.SelectedItem as ServerSelectionItem)?.ServerId ?? _config.SelectedServerId;
        _config.CheckForManagedClientUpdatesOnStartup = _checkForManagedClientUpdatesOnStartupCheckBox.Checked;
        _config.NotifyWhenManagedClientUpdateAvailable = _notifyWhenManagedClientUpdateAvailableCheckBox.Checked;

        _serverManager.Normalize(_config);
        _configService.Save(_config);
        RefreshServerViews();
        RefreshStatus();
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
            var notes = server.Type == ServerType.JavaBridge && server.RequiresOnlineAuth ? "Needs online auth later" : "Ready for local phase";

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
        var username = string.IsNullOrWhiteSpace(_localUsernameTextBox.Text) ? "Player" : _localUsernameTextBox.Text.Trim();
        var authModeText = _authModeComboBox.SelectedIndex == 1 ? "Online (planned)" : "Local";
        var clientConfigured = !string.IsNullOrWhiteSpace(_clientExecutableTextBox.Text);

        if (!clientConfigured)
        {
            _promoTitleLabel.Text = "Install the managed nightly client";
            _selectedServerDetailsLabel.Text = "No client install is configured yet. Install the smartcmd nightly build to AppData or set a custom client executable path.";
            _heroServerLabel.Text = "Client not installed";
        }
        else if (selectedServer is null)
        {
            _promoTitleLabel.Text = "Launch the local client shell";
            _selectedServerDetailsLabel.Text = "The launcher will update username.txt and servers.db, then start the client without spinning up a bridge route.";
            _heroServerLabel.Text = "Client only, no bridge target selected";
        }
        else if (selectedServer.Type == ServerType.JavaBridge)
        {
            _promoTitleLabel.Text = selectedServer.RequiresOnlineAuth ? "Server saved, waiting for online auth" : "Java bridge route ready to launch";
            _selectedServerDetailsLabel.Text = $"Server {selectedServer.DisplayName} will be exposed to the client as 127.0.0.1:{selectedServer.LocalBridgePort}. {(selectedServer.RequiresOnlineAuth ? "It is currently blocked until online auth is implemented." : "Phase one can launch it through the bundled bridge.")}";
            _heroServerLabel.Text = $"{selectedServer.DisplayName} via 127.0.0.1:{selectedServer.LocalBridgePort}";
        }
        else
        {
            _promoTitleLabel.Text = "Direct Legacy server selected";
            _selectedServerDetailsLabel.Text = $"Native LCE entry {selectedServer.DisplayName} remains direct at {selectedServer.RemoteAddress}:{selectedServer.RemotePort}.";
            _heroServerLabel.Text = $"{selectedServer.DisplayName} direct to {selectedServer.RemoteAddress}:{selectedServer.RemotePort}";
        }

        _bridgeStatusLabel.Text = _bridgeRuntimeManager.StatusText;
        _promoStatusPill.Text = _authModeComboBox.SelectedIndex == 1 ? "ONLINE MODE PLANNED" : "LOCAL MODE ACTIVE";
        _promoStatusPill.BackColor = _authModeComboBox.SelectedIndex == 1 ? DangerRed : AccentGreenDark;

        _heroProfileLabel.Text = $"{username} | {authModeText}";
        _serversCardBodyLabel.Text = $"{_config.Servers.Count} saved server(s). Java routes keep stable localhost mappings, and native LCE entries stay direct.";
        _runtimeCardBodyLabel.Text = $"{_bridgeRuntimeManager.StatusText}. Use the logs page to inspect bridge startup and launcher actions.";

        var onlineSelected = _authModeComboBox.SelectedIndex == 1;
        _launchClientButton.Enabled = clientConfigured;
        _launchSelectedServerButton.Enabled = clientConfigured && !onlineSelected;
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
        _checkNightlyUpdatesButton.Enabled = !busy;
        var installInfo = _clientInstallService.GetManagedInstallInfo();
        _installNightlyButton.Enabled = !busy;
        _updateNightlyButton.Enabled = !busy && installInfo.IsInstalled;
        _repairNightlyButton.Enabled = !busy;
        _useManagedNightlyButton.Enabled = !busy && installInfo.IsInstalled;
        _openManagedInstallButton.Enabled = !busy && installInfo.IsInstalled;
    }

    private void RefreshManagedInstallStatus()
    {
        var installInfo = _clientInstallService.GetManagedInstallInfo();
        var isUsingManagedClient = PathsEqual(_clientExecutableTextBox.Text, installInfo.ClientExecutablePath);

        _managedClientStatusLabel.Text = installInfo.IsInstalled
            ? (isUsingManagedClient ? $"Installed and active: {installInfo.DisplayVersion}" : $"Installed: {installInfo.DisplayVersion}")
            : "Not installed";

        _managedClientDetailsLabel.Text = installInfo.IsInstalled
            ? $"Root: {installInfo.InstallRoot}{Environment.NewLine}Published: {FormatTimestamp(installInfo.PublishedAtUtc)}{Environment.NewLine}Installed: {FormatTimestamp(installInfo.InstalledAtUtc)}"
            : $"Expected install root: {installInfo.InstallRoot}{Environment.NewLine}Use Install Nightly to download LCEWindows64.zip into AppData and extract it here.";

        _managedClientDetailsLabel.MaximumSize = new Size(720, 0);
        _managedClientUpdateLabel.MaximumSize = new Size(720, 0);
        _managedClientLastCheckedLabel.MaximumSize = new Size(720, 0);

        RestoreManagedInstallUpdateState(installInfo);
        _updateNightlyButton.Enabled = installInfo.IsInstalled;
        _repairNightlyButton.Enabled = true;
        _useManagedNightlyButton.Enabled = installInfo.IsInstalled;
        _openManagedInstallButton.Enabled = installInfo.IsInstalled;
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            SetBusy(true);
            _managedClientUpdateLabel.Text = "Checking nightly release...";
            _managedClientUpdateLabel.ForeColor = TextMuted;
            await RefreshManagedInstallUpdateStatusAsync(notifyIfAvailable: false);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task InitializeManagedClientUpdateChecksAsync()
    {
        RefreshManagedInstallStatus();

        if (!_config.CheckForManagedClientUpdatesOnStartup)
        {
            return;
        }

        await CheckForStartupUpdatesAsync();
    }

    private async Task CheckForStartupUpdatesAsync()
    {
        try
        {
            SetBusy(true);
            _managedClientUpdateLabel.Text = "Checking nightly release...";
            _managedClientUpdateLabel.ForeColor = TextMuted;
            await RefreshManagedInstallUpdateStatusAsync(notifyIfAvailable: _config.NotifyWhenManagedClientUpdateAvailable);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RefreshManagedInstallUpdateStatusAsync(bool notifyIfAvailable)
    {
        var checkedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            var updateInfo = await _clientInstallService.GetNightlyUpdateInfoAsync(CancellationToken.None);
            ApplyManagedInstallUpdateInfo(updateInfo, checkedAtUtc, notifyIfAvailable);
        }
        catch (Exception ex)
        {
            _logger.Warn($"Nightly update check failed: {ex.Message}");
            ApplyManagedInstallUpdateFailure(checkedAtUtc);
        }
    }

    private void ApplyManagedInstallUpdateInfo(ManagedClientUpdateInfo updateInfo, DateTimeOffset checkedAtUtc, bool notifyIfAvailable)
    {
        _config.ManagedClientLastCheckedAtUtc = checkedAtUtc;
        _config.ManagedClientLastUpdateStatusText = updateInfo.StatusText;
        _config.ManagedClientLastUpdateAvailable = updateInfo.UpdateAvailable;
        _config.ManagedClientLastKnownLatestVersion = string.IsNullOrWhiteSpace(updateInfo.LatestVersion) ? null : updateInfo.LatestVersion;

        if (!updateInfo.UpdateAvailable)
        {
            _config.ManagedClientLastNotifiedVersion = null;
        }

        if (notifyIfAvailable &&
            updateInfo.UpdateAvailable &&
            !string.IsNullOrWhiteSpace(updateInfo.LatestVersion) &&
            !string.Equals(_config.ManagedClientLastNotifiedVersion, updateInfo.LatestVersion, StringComparison.Ordinal))
        {
            _config.ManagedClientLastNotifiedVersion = updateInfo.LatestVersion;
            _configService.Save(_config);

            MessageBox.Show(
                this,
                $"A newer managed nightly client is available: {updateInfo.LatestVersion}",
                "Client Update Available",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            _configService.Save(_config);
        }

        _managedClientUpdateLabel.Text = updateInfo.StatusText;
        _managedClientUpdateLabel.ForeColor = updateInfo.UpdateAvailable ? AccentGold : TextMuted;
        _managedClientLastCheckedLabel.Text = $"Last checked: {FormatTimestamp(checkedAtUtc)}";
        _managedClientLastCheckedLabel.ForeColor = TextMuted;
    }

    private void ApplyManagedInstallUpdateFailure(DateTimeOffset checkedAtUtc)
    {
        _config.ManagedClientLastCheckedAtUtc = checkedAtUtc;
        _config.ManagedClientLastUpdateStatusText = "Update check failed";
        _config.ManagedClientLastUpdateAvailable = false;
        _configService.Save(_config);

        _managedClientUpdateLabel.Text = "Update check failed";
        _managedClientUpdateLabel.ForeColor = DangerRed;
        _managedClientLastCheckedLabel.Text = $"Last checked: {FormatTimestamp(checkedAtUtc)}";
        _managedClientLastCheckedLabel.ForeColor = TextMuted;
    }

    private async Task InstallNightlyAsync()
    {
        try
        {
            SetBusy(true);
            var installInfo = await _clientInstallService.InstallNightlyAsync(CancellationToken.None);
            _config.PreferManagedClientInstall = true;
            _config.ClientExecutablePath = installInfo.ClientExecutablePath;
            _clientExecutableTextBox.Text = installInfo.ClientExecutablePath;
            _configService.Save(_config);
            RefreshStatus();
            RefreshManagedInstallStatus();
            await RefreshManagedInstallUpdateStatusAsync(notifyIfAvailable: false);
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
            await RefreshManagedInstallUpdateStatusAsync(notifyIfAvailable: false);
        }
    }

    private async Task UpdateManagedNightlyAsync()
    {
        try
        {
            SetBusy(true);
            var installInfo = await _clientInstallService.UpdateNightlyExecutableAsync(CancellationToken.None);
            if (_config.PreferManagedClientInstall || PathsEqual(_clientExecutableTextBox.Text, installInfo.ClientExecutablePath))
            {
                _config.PreferManagedClientInstall = true;
                _config.ClientExecutablePath = installInfo.ClientExecutablePath;
                _clientExecutableTextBox.Text = installInfo.ClientExecutablePath;
                _configService.Save(_config);
            }

            RefreshStatus();
            RefreshManagedInstallStatus();
            await RefreshManagedInstallUpdateStatusAsync(notifyIfAvailable: false);
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
            await RefreshManagedInstallUpdateStatusAsync(notifyIfAvailable: false);
        }
    }

    private async Task RepairManagedNightlyAsync()
    {
        var installInfo = _clientInstallService.GetManagedInstallInfo();
        var prompt = installInfo.IsInstalled
            ? "Repair will reinstall the full nightly client into AppData and preserve uid.dat, username.txt, and servers.db. Continue?"
            : "No managed nightly install was found. Repair will install the full nightly client into AppData. Continue?";
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
            var repairedInstall = await _clientInstallService.RepairNightlyAsync(CancellationToken.None);
            if (_config.PreferManagedClientInstall || !installInfo.IsInstalled || PathsEqual(_clientExecutableTextBox.Text, installInfo.ClientExecutablePath))
            {
                _config.PreferManagedClientInstall = true;
                _config.ClientExecutablePath = repairedInstall.ClientExecutablePath;
                _clientExecutableTextBox.Text = repairedInstall.ClientExecutablePath;
                _configService.Save(_config);
            }

            RefreshStatus();
            RefreshManagedInstallStatus();
            await RefreshManagedInstallUpdateStatusAsync(notifyIfAvailable: false);
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
            await RefreshManagedInstallUpdateStatusAsync(notifyIfAvailable: false);
        }
    }

    private void UseManagedNightlyInstall()
    {
        var installInfo = _clientInstallService.GetManagedInstallInfo();
        if (!installInfo.IsInstalled)
        {
            MessageBox.Show(this, "The managed nightly client is not installed yet.", "Managed Client", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _config.PreferManagedClientInstall = true;
        _config.ClientExecutablePath = installInfo.ClientExecutablePath;
        _clientExecutableTextBox.Text = installInfo.ClientExecutablePath;
        _configService.Save(_config);
        RefreshStatus();
        RefreshManagedInstallStatus();
        _ = RefreshManagedInstallUpdateStatusAsync(notifyIfAvailable: false);
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

    private void RestoreManagedInstallUpdateState(ManagedClientInstallInfo installInfo)
    {
        if (!installInfo.IsInstalled)
        {
            _managedClientUpdateLabel.Text = "Install required";
            _managedClientUpdateLabel.ForeColor = AccentGold;
            _managedClientLastCheckedLabel.Text = _config.ManagedClientLastCheckedAtUtc is null
                ? "Not checked yet"
                : $"Last checked: {FormatTimestamp(_config.ManagedClientLastCheckedAtUtc)}";
            _managedClientLastCheckedLabel.ForeColor = TextMuted;
            return;
        }

        _managedClientUpdateLabel.Text = string.IsNullOrWhiteSpace(_config.ManagedClientLastUpdateStatusText)
            ? "Not checked yet"
            : _config.ManagedClientLastUpdateStatusText;
        _managedClientUpdateLabel.ForeColor = string.Equals(_config.ManagedClientLastUpdateStatusText, "Update check failed", StringComparison.Ordinal)
            ? DangerRed
            : _config.ManagedClientLastUpdateAvailable ? AccentGold : TextMuted;
        _managedClientLastCheckedLabel.Text = _config.ManagedClientLastCheckedAtUtc is null
            ? "Not checked yet"
            : $"Last checked: {FormatTimestamp(_config.ManagedClientLastCheckedAtUtc)}";
        _managedClientLastCheckedLabel.ForeColor = TextMuted;
    }

    private void AppendExistingLogs()
    {
        foreach (var line in _logger.Snapshot()) AppendLog(line);
    }

    private void AppendLog(string line)
    {
        if (_logsTextBox.TextLength > 0) _logsTextBox.AppendText(Environment.NewLine);
        _logsTextBox.AppendText(line);
        _logsTextBox.SelectionStart = _logsTextBox.TextLength;
        _logsTextBox.ScrollToCaret();
    }

    private void ShowPage(string pageKey)
    {
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

    private sealed record ServerSelectionItem(string? ServerId, string Label)
    {
        public override string ToString() => Label;
    }
}
