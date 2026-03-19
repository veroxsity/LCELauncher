namespace LceLauncher.UI;

public sealed partial class MainForm
{
    private Control BuildCardStrip()
    {
        var strip = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = AppBackground,
            Height = 308,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        strip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        EnableDoubleBuffering(strip);

        var serversCard = CreateFeatureCard("Managed Servers", _serversCardBodyLabel, _javaCardImage, "OPEN SERVERS", () => ShowPage(PageServers));
        var settingsCard = CreateFeatureCard("Profile Setup", CreateBodyLabel("Point the launcher at your built client and bridge jar, set your local username, and keep username.txt synced."), _bundleCardImage, "OPEN SETTINGS", () => ShowPage(PageSettings));
        var logsCard = CreateFeatureCard("Runtime Logs", _runtimeCardBodyLabel, _heroImage, "OPEN LOGS", () => ShowPage(PageLogs));

        serversCard.Margin = new Padding(0, 0, 10, 0);
        settingsCard.Margin = new Padding(0, 0, 10, 0);
        logsCard.Margin = Padding.Empty;

        strip.Controls.Add(serversCard, 0, 0);
        strip.Controls.Add(settingsCard, 1, 0);
        strip.Controls.Add(logsCard, 2, 0);
        return strip;
    }

    private Control CreateFeatureCard(string title, Label bodyLabel, Image? image, string buttonText, Action onClick)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBackground,
            Margin = Padding.Empty,
        };
        EnableDoubleBuffering(card);

        var imageBox = CreateCoverImagePanel(image, 130);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = SurfaceBackground,
            Margin = Padding.Empty,
            Padding = new Padding(16, 14, 16, 14),
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleLabel = CreateSectionTitle(title, 14.5F);
        titleLabel.Margin = Padding.Empty;

        bodyLabel.Margin = new Padding(0, 10, 0, 0);
        bodyLabel.Font = new Font("Segoe UI", 10.25F, FontStyle.Regular);

        var spacer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
        };

        var button = CreateSecondaryButton(buttonText);
        button.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        button.Margin = new Padding(0, 12, 0, 0);
        button.Click += (_, _) => onClick();

        body.Controls.Add(titleLabel, 0, 0);
        body.Controls.Add(bodyLabel, 0, 1);
        body.Controls.Add(spacer, 0, 2);
        body.Controls.Add(button, 0, 3);

        card.Resize += (_, _) =>
        {
            var textWidth = Math.Max(220, card.ClientSize.Width - body.Padding.Horizontal - 12);
            bodyLabel.MaximumSize = new Size(textWidth, 0);
        };

        card.Controls.Add(body);
        card.Controls.Add(imageBox);
        return card;
    }

    private Control BuildServersPage()
    {
        var addButton = CreateSecondaryButton("ADD SERVER");
        var editButton = CreateSecondaryButton("EDIT");
        var deleteButton = CreateSecondaryButton("DELETE");
        var refreshButton = CreateSecondaryButton("REFRESH PREVIEW");

        addButton.Click += (_, _) => AddServer();
        editButton.Click += (_, _) => EditSelectedServer();
        deleteButton.Click += (_, _) => DeleteSelectedServer();
        refreshButton.Click += (_, _) =>
        {
            SaveConfigFromControls();
            RefreshServerViews();
        };

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 14),
            Padding = Padding.Empty,
        };
        EnableDoubleBuffering(toolbar);
        toolbar.Controls.Add(addButton);
        toolbar.Controls.Add(editButton);
        toolbar.Controls.Add(deleteButton);
        toolbar.Controls.Add(refreshButton);

        var card = CreateContentCard();
        card.Padding = new Padding(18);
        card.MinimumSize = new Size(0, 420);
        card.Controls.Add(_serversListView);
        card.Controls.Add(toolbar);

        return CreateResponsivePage("Servers", "Manage direct LCE entries and Java bridge-backed routes from one launcher-owned list.", card, 1180, true);
    }

    private Control BuildSettingsPage()
    {
        var container = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            BackColor = Color.Transparent,
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        EnableDoubleBuffering(container);

        container.Controls.Add(BuildManagedClientSettingsCard(), 0, 0);

        container.Controls.Add(BuildSettingsCard("Executables", new[]
        {
            BuildFileRow("Client Executable", _clientExecutableTextBox, () => BrowseForFile(_clientExecutableTextBox, "Minecraft Client|Minecraft.Client.exe|Executable|*.exe")),
            BuildFileRow("Bridge Jar", _bridgeJarTextBox, () => BrowseForFile(_bridgeJarTextBox, "Bridge Jar|*.jar|All Files|*.*")),
            BuildFileRow("Java Executable", _javaExecutableTextBox, () => BrowseForFile(_javaExecutableTextBox, "Java Executable|java.exe|Executable|*.exe")),
        }), 0, 1);

        container.Controls.Add(BuildSettingsCard("Profile", new[]
        {
            BuildFormRow("Auth Mode", _authModeComboBox),
            BuildFormRow("Local Username", _localUsernameTextBox),
            BuildFormRow("Selected Server", _selectedServerComboBox),
            BuildFormRow("Launch Arguments", _launchArgumentsTextBox),
        }), 0, 2);

        container.Controls.Add(BuildSettingsCard("Runtime", new[]
        {
            BuildFormRow("First Bridge Port", _firstBridgePortUpDown),
            BuildFormRow("Close Bridge On Exit", _closeBridgeOnExitCheckBox),
            BuildFormRow("Launcher Data Root", CreateBodyLabel(_appPaths.DataRoot)),
        }), 0, 3);

        var actionsPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };

        var saveButton = CreatePrimaryButton("SAVE SETTINGS");
        saveButton.Width = 232;
        saveButton.Height = 54;
        saveButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        saveButton.Margin = Padding.Empty;
        saveButton.Click += (_, _) => SaveConfigFromControls();
        actionsPanel.Controls.Add(saveButton);

        container.Controls.Add(actionsPanel, 0, 4);

        return CreateResponsivePage("Settings", "Point the launcher at local builds and keep phase-one runtime behavior explicit.", container, 1100, false);
    }

    private Panel BuildManagedClientSettingsCard()
    {
        _managedClientStatusLabel.Margin = new Padding(0, 8, 0, 0);
        _managedClientDetailsLabel.Margin = new Padding(0, 8, 0, 0);
        _managedClientUpdateLabel.Margin = new Padding(0, 8, 0, 0);
        _managedClientLastCheckedLabel.Margin = new Padding(0, 8, 0, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 14, 0, 0),
            Padding = Padding.Empty,
        };
        EnableDoubleBuffering(actions);

        _checkNightlyUpdatesButton.Margin = new Padding(0, 0, 10, 10);
        _installNightlyButton.Margin = new Padding(0, 0, 10, 10);
        _updateNightlyButton.Margin = new Padding(0, 0, 10, 10);
        _repairNightlyButton.Margin = new Padding(0, 0, 10, 10);
        _useManagedNightlyButton.Margin = new Padding(0, 0, 10, 10);
        _openManagedInstallButton.Margin = new Padding(0, 0, 0, 10);

        _checkNightlyUpdatesButton.Click += async (_, _) => await CheckForUpdatesAsync();
        _installNightlyButton.Click += async (_, _) => await InstallNightlyAsync();
        _updateNightlyButton.Click += async (_, _) => await UpdateManagedNightlyAsync();
        _repairNightlyButton.Click += async (_, _) => await RepairManagedNightlyAsync();
        _useManagedNightlyButton.Click += (_, _) => UseManagedNightlyInstall();
        _openManagedInstallButton.Click += (_, _) => OpenDirectorySafely(_appPaths.NightlyInstallRoot);

        actions.Controls.Add(_checkNightlyUpdatesButton);
        actions.Controls.Add(_installNightlyButton);
        actions.Controls.Add(_updateNightlyButton);
        actions.Controls.Add(_repairNightlyButton);
        actions.Controls.Add(_useManagedNightlyButton);
        actions.Controls.Add(_openManagedInstallButton);

        return BuildSettingsCard("Managed Client", new Control[]
        {
            BuildFormRow("Install Channel", CreateBodyLabel("smartcmd nightly")),
            BuildFormRow("Managed Status", _managedClientStatusLabel),
            BuildFormRow("Install Details", _managedClientDetailsLabel),
            BuildFormRow("Update Status", _managedClientUpdateLabel),
            BuildFormRow("Last Checked", _managedClientLastCheckedLabel),
            BuildFormRow("Check On Startup", _checkForManagedClientUpdatesOnStartupCheckBox),
            BuildFormRow("Notify On Update", _notifyWhenManagedClientUpdateAvailableCheckBox),
            actions,
        });
    }

    private Control BuildLogsPage()
    {
        var card = CreateContentCard();
        card.Padding = new Padding(18);
        card.MinimumSize = new Size(0, 420);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 14),
            Padding = Padding.Empty,
        };
        EnableDoubleBuffering(toolbar);

        var openDataButton = CreateSecondaryButton("OPEN DATA");
        openDataButton.Click += (_, _) => OpenDirectorySafely(_appPaths.DataRoot);
        toolbar.Controls.Add(openDataButton);

        card.Controls.Add(_logsTextBox);
        card.Controls.Add(toolbar);
        return CreateResponsivePage("Logs", "Watch launcher and bridge output without dropping to the terminal.", card, 1180, true);
    }

    private Control CreateResponsivePage(string title, string subtitle, Control body, int maxContentWidth, bool fillBodyHeight)
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppBackground,
            Padding = new Padding(ShellPadding, 18, ShellPadding, ShellPadding),
        };
        EnableDoubleBuffering(page);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppBackground,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        EnableDoubleBuffering(root);

        var headerHost = new Panel
        {
            Dock = DockStyle.Top,
            Height = 86,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 10),
            Padding = Padding.Empty,
        };

        var headerContent = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        headerContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var titleLabel = CreateSectionTitle(title, 20.5F);
        titleLabel.Margin = Padding.Empty;

        var subtitleLabel = CreateBodyLabel(subtitle);
        subtitleLabel.Margin = new Padding(0, 8, 0, 0);

        headerContent.Controls.Add(titleLabel, 0, 0);
        headerContent.Controls.Add(subtitleLabel, 0, 1);
        headerHost.Controls.Add(headerContent);

        var bodyHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            AutoScroll = true,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        EnableDoubleBuffering(bodyHost);

        var bodyCanvas = new Panel
        {
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        EnableDoubleBuffering(bodyCanvas);

        bodyCanvas.Controls.Add(body);
        bodyHost.Controls.Add(bodyCanvas);

        root.Controls.Add(headerHost, 0, 0);
        root.Controls.Add(bodyHost, 0, 1);
        page.Controls.Add(root);

        void UpdatePageLayout()
        {
            var availableWidth = Math.Max(0, page.ClientSize.Width - page.Padding.Horizontal);
            var contentWidth = Math.Min(
                maxContentWidth,
                Math.Max(PageMinContentWidth, bodyHost.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4));

            var headerWidth = Math.Min(PageHeaderMaxWidth, contentWidth);
            var bodyLeft = Math.Max(0, (availableWidth - contentWidth) / 2);
            var headerLeft = Math.Max(0, (availableWidth - headerWidth) / 2);

            page.SuspendLayout();
            headerContent.SuspendLayout();
            body.SuspendLayout();

            headerContent.Left = headerLeft;
            headerContent.Top = 0;
            headerContent.Width = headerWidth;
            subtitleLabel.MaximumSize = new Size(headerWidth, 0);

            body.Width = contentWidth;
            body.Left = bodyLeft;
            body.Top = 0;

            if (fillBodyHeight)
            {
                body.Height = Math.Max(420, bodyHost.ClientSize.Height - 2);
            }
            else
            {
                body.Height = Math.Max(body.PreferredSize.Height, body.GetPreferredSize(new Size(contentWidth, 0)).Height);
            }

            if (body is TableLayoutPanel table)
            {
                foreach (Control child in table.Controls)
                {
                    child.Width = contentWidth;
                }
            }

            bodyCanvas.Width = Math.Max(bodyHost.ClientSize.Width, body.Right);
            bodyCanvas.Height = Math.Max(body.Bottom, bodyHost.ClientSize.Height - 2);

            body.ResumeLayout(true);
            headerContent.ResumeLayout(true);
            page.ResumeLayout(true);
        }

        bodyHost.Resize += (_, _) => UpdatePageLayout();
        page.Resize += (_, _) => UpdatePageLayout();
        UpdatePageLayout();
        return page;
    }

    private Panel CreateContentCard() =>
        new()
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBackground,
            Margin = Padding.Empty,
        };

    private Panel BuildSettingsCard(string title, IEnumerable<Control> rows)
    {
        var card = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = SurfaceBackground,
            Padding = new Padding(20, 18, 20, 18),
            Margin = new Padding(0, 0, 0, 18),
        };
        EnableDoubleBuffering(card);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var titleLabel = CreateSectionTitle(title, 16.5F);
        titleLabel.Margin = Padding.Empty;
        layout.Controls.Add(titleLabel, 0, 0);

        var rowIndex = 1;
        foreach (var row in rows)
        {
            layout.Controls.Add(row, 0, rowIndex++);
        }

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildFormRow(string label, Control control)
    {
        var row = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0, 14, 0, 0),
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Padding = Padding.Empty,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var labelControl = CreateCaptionLabel(label);
        labelControl.Margin = new Padding(0, 9, 0, 0);

        control.Margin = Padding.Empty;
        if (control is CheckBox)
        {
            control.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        }
        else
        {
            control.Dock = DockStyle.Fill;

            if (control is Label labelBody)
            {
                labelBody.Margin = new Padding(0, 6, 0, 0);
            }
        }

        row.Controls.Add(labelControl, 0, 0);
        row.Controls.Add(control, 1, 0);
        return row;
    }

    private Control BuildFileRow(string label, TextBox textBox, Action browseAction)
    {
        var row = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 3,
            Margin = new Padding(0, 14, 0, 0),
            BackColor = Color.Transparent,
            Dock = DockStyle.Top,
            Padding = Padding.Empty,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));

        var labelControl = CreateCaptionLabel(label);
        labelControl.Margin = new Padding(0, 9, 0, 0);

        var button = CreateSecondaryButton("BROWSE");
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(12, 0, 0, 0);

        textBox.Dock = DockStyle.Fill;
        textBox.Margin = Padding.Empty;
        button.Click += (_, _) => browseAction();

        row.Controls.Add(labelControl, 0, 0);
        row.Controls.Add(textBox, 1, 0);
        row.Controls.Add(button, 2, 0);
        return row;
    }
}
