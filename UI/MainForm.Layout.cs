namespace LceLauncher.UI;

public sealed partial class MainForm
{
    private Control BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SidebarBackground,
            Padding = Padding.Empty,
        };
        EnableDoubleBuffering(sidebar);

        var accountCard = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Color.FromArgb(38, 34, 34),
            Padding = new Padding(18, 18, 18, 16),
            Margin = Padding.Empty,
        };
        accountCard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        accountCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var avatar = new Panel
        {
            Width = 36,
            Height = 36,
            BackColor = AccentGold,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Margin = new Padding(0, 2, 10, 0),
        };
        avatar.Paint += (_, args) =>
        {
            using var brush = new SolidBrush(AccentGold);
            args.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            args.Graphics.FillEllipse(brush, 0, 0, avatar.Width - 1, avatar.Height - 1);
            TextRenderer.DrawText(
                args.Graphics,
                "L",
                new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                avatar.ClientRectangle,
                Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        var accountText = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent,
        };
        accountText.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var accountName = CreateSectionTitle("Legacy User", 15F);
        accountName.Margin = Padding.Empty;

        var accountType = CreateBodyLabel("Local launcher profile");
        accountType.Margin = new Padding(0, 4, 0, 0);

        accountText.Controls.Add(accountName, 0, 0);
        accountText.Controls.Add(accountType, 0, 1);

        accountCard.Controls.Add(avatar, 0, 0);
        accountCard.Controls.Add(accountText, 1, 0);

        var navPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(0, 8, 0, 0),
            Margin = Padding.Empty,
            BackColor = Color.Transparent,
        };
        navPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        navPanel.Controls.Add(CreateSidebarNavButton(PageHome, "HOME"), 0, 0);
        navPanel.Controls.Add(CreateSidebarNavButton(PageHome, "MINECRAFT:\nLEGACY EDITION"), 0, 1);
        navPanel.Controls.Add(CreateSidebarNavButton(PageServers, "SERVERS"), 0, 2);

        var bottomNavPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(0, 0, 0, 4),
            Margin = Padding.Empty,
            BackColor = Color.Transparent,
        };
        bottomNavPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomNavPanel.Controls.Add(CreateSidebarNavButton(PageSettings, "SETTINGS"), 0, 0);
        bottomNavPanel.Controls.Add(CreateSidebarNavButton(PageLogs, "LOGS"), 0, 1);

        var footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            BackColor = SidebarBackground,
            Padding = new Padding(18, 20, 18, 18),
        };
        _footerVersionLabel.Dock = DockStyle.Left;
        footerPanel.Controls.Add(_footerVersionLabel);

        sidebar.Controls.Add(footerPanel);
        sidebar.Controls.Add(bottomNavPanel);
        sidebar.Controls.Add(navPanel);
        sidebar.Controls.Add(accountCard);
        return sidebar;
    }

    private Control BuildHomePage()
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppBackground,
            Padding = new Padding(ShellPadding, 10, ShellPadding, ShellPadding),
            AutoScroll = true,
        };
        EnableDoubleBuffering(page);

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            BackColor = AppBackground,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        EnableDoubleBuffering(layout);

        var header = BuildTopHeader();
        var hero = (TableLayoutPanel)BuildHeroSection(out var heroVisual, out var promoPanel);
        var cards = (TableLayoutPanel)BuildCardStrip();

        header.Margin = new Padding(0, 0, 0, 8);
        hero.Margin = new Padding(0, 0, 0, 12);
        cards.Margin = Padding.Empty;

        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(hero, 0, 1);
        layout.Controls.Add(cards, 0, 2);

        void UpdateLayoutMetrics()
        {
            var availableWidth = Math.Max(0, page.ClientSize.Width - page.Padding.Horizontal);
            var availableHeight = Math.Max(0, page.ClientSize.Height - page.Padding.Vertical);
            var targetWidth = Math.Min(
                HomeMaxContentWidth,
                Math.Max(HomeMinContentWidth, availableWidth - 2));

            var headerHeight = 72;
            var heroCardGap = 12;
            var headerGap = 8;
            var remainingHeight = availableHeight - headerHeight - headerGap - heroCardGap;

            var heroFraction = 0.62;
            var heroVisualHeight = Math.Clamp((int)(remainingHeight * heroFraction), 300, 520);
            var cardHeight = Math.Clamp(remainingHeight - heroVisualHeight - heroCardGap, 200, 320);

            var heroIsStacked = targetWidth < HeroStackBreakpoint;
            var promoWidth = Math.Clamp((int)(targetWidth * 0.27), 300, 380);
            var promoHeight = heroIsStacked ? 238 : heroVisualHeight;
            var heroHeight = heroIsStacked ? heroVisualHeight + promoHeight + 16 : heroVisualHeight;
            var left = page.Padding.Left + Math.Max(0, (availableWidth - targetWidth) / 2);

            page.SuspendLayout();
            layout.SuspendLayout();

            layout.SetBounds(left, page.Padding.Top, targetWidth, layout.Height);
            header.Width = targetWidth;
            hero.Width = targetWidth;
            hero.Height = heroHeight;
            cards.Width = targetWidth;
            cards.Height = cardHeight;

            ConfigureHeroShell(hero, heroVisual, promoPanel, heroIsStacked, promoWidth, heroVisualHeight, promoHeight);

            layout.ResumeLayout(true);
            page.ResumeLayout(true);
        }

        page.Resize += (_, _) => UpdateLayoutMetrics();

        page.Controls.Add(layout);
        UpdateLayoutMetrics();
        return page;
    }

    private Control BuildTopHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = AppBackground,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var title = CreateSectionTitle("MINECRAFT: LEGACY EDITION", 14.5F);
        title.Margin = new Padding(0, 6, 0, 10);

        var nav = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent,
        };
        EnableDoubleBuffering(nav);

        nav.Controls.Add(CreateTopNavButton(PageHome, "Play"));
        nav.Controls.Add(CreateTopNavButton(PageServers, "Servers"));
        nav.Controls.Add(CreateTopNavButton(PageSettings, "Settings"));
        nav.Controls.Add(CreateTopNavButton(PageLogs, "Logs"));

        header.Controls.Add(title, 0, 0);
        header.Controls.Add(nav, 0, 1);
        return header;
    }

    private Control BuildHeroSection(out Control heroVisual, out Control promoPanel)
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            BackColor = AppBackground,
            Height = 480,
        };
        EnableDoubleBuffering(shell);

        heroVisual = BuildHeroVisualPanel();
        promoPanel = BuildPromoPanel();
        ConfigureHeroShell(shell, heroVisual, promoPanel, false, 320, 408, 408);
        return shell;
    }

    private void ConfigureHeroShell(
        TableLayoutPanel shell,
        Control heroVisual,
        Control promoPanel,
        bool stacked,
        int promoWidth,
        int heroVisualHeight,
        int promoHeight)
    {
        shell.SuspendLayout();
        shell.Controls.Clear();
        shell.ColumnStyles.Clear();
        shell.RowStyles.Clear();

        heroVisual.Dock = DockStyle.Fill;
        promoPanel.Dock = DockStyle.Fill;

        if (stacked)
        {
            shell.ColumnCount = 1;
            shell.RowCount = 2;
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, heroVisualHeight));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, promoHeight));
            heroVisual.Margin = Padding.Empty;
            promoPanel.Margin = new Padding(0, 16, 0, 0);
            shell.Controls.Add(heroVisual, 0, 0);
            shell.Controls.Add(promoPanel, 0, 1);
        }
        else
        {
            shell.ColumnCount = 2;
            shell.RowCount = 1;
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, promoWidth));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, heroVisualHeight));
            heroVisual.Margin = new Padding(0, 0, 10, 0);
            promoPanel.Margin = Padding.Empty;
            shell.Controls.Add(heroVisual, 0, 0);
            shell.Controls.Add(promoPanel, 1, 0);
        }

        shell.ResumeLayout(true);
    }

    private Control BuildHeroVisualPanel()
    {
        var hero = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBackground,
            Margin = Padding.Empty,
        };
        EnableDoubleBuffering(hero);
        hero.Paint += (_, args) =>
        {
            DrawCoverImage(args.Graphics, _heroImage, hero.ClientRectangle);
            using var overlay = new SolidBrush(Color.FromArgb(54, 12, 10, 10));
            args.Graphics.FillRectangle(overlay, hero.ClientRectangle);
        };

        var overlayTop = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.FromArgb(148, 18, 18, 18),
            Padding = new Padding(24, 18, 24, 14),
        };

        var headline = CreateSectionTitle("Play Minecraft Legacy Edition", 21.5F);
        headline.Margin = Padding.Empty;

        var subtitle = CreateBodyLabel("Launch your local client, manage bridge-backed Java servers, and keep the whole legacy workflow in one place.");
        subtitle.Margin = new Padding(0, 6, 0, 0);

        var topStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent,
        };
        topStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topStack.Controls.Add(headline, 0, 0);
        topStack.Controls.Add(subtitle, 0, 1);
        overlayTop.Controls.Add(topStack);

        var bottomBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 138,
            BackColor = Color.FromArgb(220, 28, 24, 24),
            Padding = new Padding(20, 12, 20, 12),
        };

        var bottomLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        bottomLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));

        var profilePanel = CreateInsetPanel();
        profilePanel.Padding = new Padding(14, 8, 14, 8);

        var profileLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        profileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        profileLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        profileLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var profileRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var profileCaption = CreateCaptionLabel("Launch Profile");
        profileCaption.Dock = DockStyle.Top;
        profileCaption.Margin = new Padding(0, 0, 0, 2);
        _heroProfileLabel.Dock = DockStyle.Fill;
        _heroProfileLabel.Margin = Padding.Empty;
        _heroProfileLabel.AutoSize = false;
        _heroProfileLabel.AutoEllipsis = true;
        profileRow.Controls.Add(_heroProfileLabel);
        profileRow.Controls.Add(profileCaption);

        var routeRow = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var routeCaption = CreateCaptionLabel("Selected Route");
        routeCaption.Dock = DockStyle.Top;
        routeCaption.Margin = new Padding(0, 0, 0, 2);
        _heroServerLabel.Dock = DockStyle.Fill;
        _heroServerLabel.Margin = Padding.Empty;
        _heroServerLabel.AutoEllipsis = true;
        _heroServerLabel.AutoSize = false;
        routeRow.Controls.Add(_heroServerLabel);
        routeRow.Controls.Add(routeCaption);

        profileLayout.Controls.Add(profileRow, 0, 0);
        profileLayout.Controls.Add(routeRow, 0, 1);
        profilePanel.Controls.Add(profileLayout);

        var playPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        playPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _launchSelectedServerButton.Width = 280;
        _launchSelectedServerButton.Height = 68;
        _launchSelectedServerButton.Anchor = AnchorStyles.None;
        _launchSelectedServerButton.Click += async (_, _) => await LaunchAsync(GetSelectedServer());
        playPanel.Controls.Add(_launchSelectedServerButton, 0, 0);

        var utilityPanel = CreateInsetPanel();
        utilityPanel.Padding = new Padding(12, 10, 12, 10);

        var utilityButtons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        utilityButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        utilityButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        utilityButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _launchClientButton.Margin = Padding.Empty;
        _stopBridgeButton.Margin = new Padding(0, 10, 0, 0);
        _launchClientButton.Dock = DockStyle.Fill;
        _stopBridgeButton.Dock = DockStyle.Fill;

        utilityButtons.Controls.Add(_launchClientButton, 0, 0);
        utilityButtons.Controls.Add(_stopBridgeButton, 0, 1);
        utilityPanel.Controls.Add(utilityButtons);

        bottomLayout.Controls.Add(profilePanel, 0, 0);
        bottomLayout.Controls.Add(playPanel, 1, 0);
        bottomLayout.Controls.Add(utilityPanel, 2, 0);
        bottomBar.Controls.Add(bottomLayout);

        hero.Resize += (_, _) =>
        {
            var textWidth = Math.Max(280, hero.ClientSize.Width - 120);
            subtitle.MaximumSize = new Size(Math.Min(760, textWidth), 0);
        };

        hero.Controls.Add(bottomBar);
        hero.Controls.Add(overlayTop);
        return hero;
    }

    private Control BuildPromoPanel()
    {
        var promo = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceAlt,
            Padding = new Padding(22, 20, 22, 20),
            Margin = Padding.Empty,
            AutoScroll = true,
        };
        EnableDoubleBuffering(promo);

        var infoLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var bridgeCaption = CreateCaptionLabel("Legacy Bridge");
        bridgeCaption.Margin = Padding.Empty;

        _promoTitleLabel.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
        _promoTitleLabel.Margin = new Padding(0, 6, 0, 0);

        _selectedServerDetailsLabel.Margin = new Padding(0, 10, 0, 0);
        _bridgeStatusLabel.Margin = new Padding(0, 8, 0, 0);

        var bridgeStatusCaption = CreateCaptionLabel("Bridge Status");
        bridgeStatusCaption.Margin = new Padding(0, 0, 0, 0);

        infoLayout.Controls.Add(bridgeCaption, 0, 0);
        infoLayout.Controls.Add(_promoTitleLabel, 0, 1);
        infoLayout.Controls.Add(_promoStatusPill, 0, 2);
        infoLayout.Controls.Add(_selectedServerDetailsLabel, 0, 3);
        infoLayout.Controls.Add(Spacer(0, 14), 0, 4);
        infoLayout.Controls.Add(bridgeStatusCaption, 0, 5);
        infoLayout.Controls.Add(_bridgeStatusLabel, 0, 6);

        var actionsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        actionsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var saveProfileButton = CreateSecondaryButton("SAVE PROFILE");
        var openClientFolderButton = CreateSecondaryButton("OPEN CLIENT");

        saveProfileButton.Dock = DockStyle.Fill;
        openClientFolderButton.Dock = DockStyle.Fill;
        saveProfileButton.Margin = Padding.Empty;
        openClientFolderButton.Margin = new Padding(0, 10, 0, 0);

        saveProfileButton.Click += (_, _) => SaveConfigFromControls();
        openClientFolderButton.Click += (_, _) => OpenDirectorySafely(GetClientFolderPath());

        actionsPanel.Controls.Add(saveProfileButton, 0, 0);
        actionsPanel.Controls.Add(openClientFolderButton, 0, 1);

        promo.Resize += (_, _) =>
        {
            var textWidth = Math.Max(180, promo.ClientSize.Width - promo.Padding.Horizontal - 4);
            _promoTitleLabel.MaximumSize = new Size(textWidth, 0);
            _selectedServerDetailsLabel.MaximumSize = new Size(textWidth, 0);
            _bridgeStatusLabel.MaximumSize = new Size(textWidth, 0);
        };

        promo.Controls.Add(actionsPanel);
        promo.Controls.Add(infoLayout);
        return promo;
    }
}
