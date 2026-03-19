using LceLauncher.Models;

namespace LceLauncher.UI;

public sealed class ServerEditForm : Form
{
    private static readonly Color DialogBackground = Color.FromArgb(28, 25, 25);
    private static readonly Color SurfaceBackground = Color.FromArgb(42, 38, 38);
    private static readonly Color BorderColor = Color.FromArgb(78, 71, 71);
    private static readonly Color AccentGreen = Color.FromArgb(110, 185, 65);
    private static readonly Color AccentGreenDark = Color.FromArgb(71, 132, 34);
    private static readonly Color TextPrimary = Color.FromArgb(245, 242, 237);
    private static readonly Color TextMuted = Color.FromArgb(193, 187, 177);
    private static readonly Color AccentGold = Color.FromArgb(230, 188, 77);

    private readonly TextBox _displayNameTextBox;
    private readonly ComboBox _typeComboBox;
    private readonly TextBox _addressTextBox;
    private readonly NumericUpDown _portUpDown;
    private readonly CheckBox _requiresOnlineAuthCheckBox;
    private readonly Label _localBridgePortValueLabel;

    public ServerEditForm(ServerEntry server)
    {
        Text = "Server Entry";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 250);
        BackColor = DialogBackground;
        ForeColor = TextPrimary;

        _displayNameTextBox = CreateTextBox();
        _typeComboBox = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = SurfaceBackground,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
        };
        _typeComboBox.Items.AddRange(["Native LCE", "Java Bridge"]);

        _addressTextBox = CreateTextBox();
        _portUpDown = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Minimum = 1,
            Maximum = 65535,
            Width = 120,
            BackColor = SurfaceBackground,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
        };
        _requiresOnlineAuthCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Requires online auth later",
            ForeColor = TextPrimary,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
        };
        _localBridgePortValueLabel = new Label
        {
            AutoSize = true,
            Text = "Assigned automatically",
            ForeColor = TextMuted,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(16),
            BackColor = DialogBackground,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(CreateLabel("Display Name"), 0, 0);
        layout.Controls.Add(_displayNameTextBox, 1, 0);
        layout.Controls.Add(CreateLabel("Server Type"), 0, 1);
        layout.Controls.Add(_typeComboBox, 1, 1);
        layout.Controls.Add(CreateLabel("Remote Host"), 0, 2);
        layout.Controls.Add(_addressTextBox, 1, 2);
        layout.Controls.Add(CreateLabel("Remote Port"), 0, 3);
        layout.Controls.Add(_portUpDown, 1, 3);
        layout.Controls.Add(CreateLabel("Local Bridge Port"), 0, 4);
        layout.Controls.Add(_localBridgePortValueLabel, 1, 4);
        layout.Controls.Add(CreateLabel("Phase 2 Flag"), 0, 5);
        layout.Controls.Add(_requiresOnlineAuthCheckBox, 1, 5);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(16, 0, 16, 16),
            Height = 56,
            BackColor = DialogBackground,
        };

        var okButton = CreateButton("SAVE", AccentGreen, AccentGreenDark, 0);
        okButton.DialogResult = DialogResult.OK;
        var cancelButton = CreateButton("CANCEL", SurfaceBackground, Color.FromArgb(66, 60, 60), 1);
        cancelButton.DialogResult = DialogResult.Cancel;

        okButton.Click += (_, _) =>
        {
            if (!TryBuildServer(out var updatedServer, out var error))
            {
                MessageBox.Show(this, error, "Invalid Server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            Server = updatedServer;
        };
        _typeComboBox.SelectedIndexChanged += (_, _) => UpdateTypeState();

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);

        Controls.Add(layout);
        Controls.Add(buttonPanel);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Server = Clone(server);
        LoadFromServer(Server);
        UpdateTypeState();
    }

    public ServerEntry Server { get; private set; }

    private static Label CreateLabel(string text) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 8, 8),
            ForeColor = AccentGold,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
        };

    private static TextBox CreateTextBox() =>
        new()
        {
            Dock = DockStyle.Fill,
            BackColor = SurfaceBackground,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
        };

    private static Button CreateButton(string text, Color backColor, Color hoverColor, int borderSize)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            Padding = new Padding(14, 6, 14, 6),
        };
        button.FlatAppearance.BorderSize = borderSize;
        button.FlatAppearance.BorderColor = BorderColor;
        button.FlatAppearance.MouseOverBackColor = hoverColor;
        button.FlatAppearance.MouseDownBackColor = hoverColor;
        return button;
    }

    private void LoadFromServer(ServerEntry server)
    {
        _displayNameTextBox.Text = server.DisplayName;
        _typeComboBox.SelectedIndex = server.Type == ServerType.JavaBridge ? 1 : 0;
        _addressTextBox.Text = server.RemoteAddress;
        _portUpDown.Value = Math.Clamp(server.RemotePort, (int)_portUpDown.Minimum, (int)_portUpDown.Maximum);
        _requiresOnlineAuthCheckBox.Checked = server.RequiresOnlineAuth;
        _localBridgePortValueLabel.Text = server.LocalBridgePort?.ToString() ?? "Assigned automatically";
    }

    private void UpdateTypeState()
    {
        var isJavaBridge = _typeComboBox.SelectedIndex == 1;
        _requiresOnlineAuthCheckBox.Enabled = isJavaBridge;
        _localBridgePortValueLabel.Enabled = isJavaBridge;
    }

    private bool TryBuildServer(out ServerEntry server, out string error)
    {
        server = Clone(Server);
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(_displayNameTextBox.Text))
        {
            error = "Display name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_addressTextBox.Text))
        {
            error = "Remote host is required.";
            return false;
        }

        server.DisplayName = _displayNameTextBox.Text.Trim();
        server.Type = _typeComboBox.SelectedIndex == 1 ? ServerType.JavaBridge : ServerType.NativeLce;
        server.RemoteAddress = _addressTextBox.Text.Trim();
        server.RemotePort = decimal.ToInt32(_portUpDown.Value);
        server.RequiresOnlineAuth = server.Type == ServerType.JavaBridge && _requiresOnlineAuthCheckBox.Checked;
        if (server.Type == ServerType.NativeLce)
        {
            server.LocalBridgePort = null;
            server.RequiresOnlineAuth = false;
        }

        return true;
    }

    private static ServerEntry Clone(ServerEntry server) =>
        new()
        {
            Id = server.Id,
            DisplayName = server.DisplayName,
            Type = server.Type,
            RemoteAddress = server.RemoteAddress,
            RemotePort = server.RemotePort,
            LocalBridgePort = server.LocalBridgePort,
            RequiresOnlineAuth = server.RequiresOnlineAuth,
        };
}
