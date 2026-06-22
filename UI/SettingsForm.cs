using FNFBot.Interop;
using FNFBot.Services;

namespace FNFBot.UI;

public sealed class SettingsForm : Form
{
    private static readonly string[] LaneNames = ["Left", "Down", "Up", "Right"];

    private readonly ColorScheme _originalColorScheme;
    private readonly NumericUpDown _hitBiasInput = new();
    private readonly NumericUpDown _tapDurationInput = new();
    private readonly NumericUpDown _holdReleaseGuardInput = new();
    private readonly NumericUpDown _menuNavigationDelayInput = new();
    private readonly KeyCaptureTextBox _startHotKey = new();
    private readonly KeyCaptureTextBox _stopHotKey = new();
    private readonly KeyCaptureTextBox _decreaseHitBiasHotKey = new();
    private readonly KeyCaptureTextBox _increaseHitBiasHotKey = new();
    private readonly KeyCaptureTextBox[,] _laneKeys = new KeyCaptureTextBox[4, 2];
    private readonly ComboBox _colorScheme = new();

    public SettingsForm(BotSettings settings)
    {
        _originalColorScheme = settings.ColorScheme;
        Text = "FNFBot Settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9F);
        ClientSize = new Size(455, 535);

        BuildUi(settings);
        ThemeManager.Apply(this, settings.ColorScheme);
    }

    public BotSettings? Result { get; private set; }

    private void BuildUi(BotSettings settings)
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 2
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        Panel scrollArea = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Margin = Padding.Empty
        };
        TableLayoutPanel sections = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty
        };
        sections.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int row = 0; row < sections.RowCount; row++)
        {
            sections.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        sections.Controls.Add(BuildTimingGroup(settings), 0, 0);
        sections.Controls.Add(BuildHotkeyGroup(settings), 0, 1);
        sections.Controls.Add(BuildInputGroup(settings), 0, 2);
        sections.Controls.Add(BuildAppearanceGroup(settings), 0, 3);
        scrollArea.Controls.Add(sections);
        root.Controls.Add(scrollArea, 0, 0);
        root.Controls.Add(BuildButtons(), 0, 1);
    }

    private Control BuildTimingGroup(BotSettings settings)
    {
        GroupBox group = CreateGroup("Timing");
        TableLayoutPanel table = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(7),
            ColumnCount = 2,
            RowCount = 4
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        ConfigureTimingInput(_hitBiasInput, -1000, 1000, settings.HitBiasMs);
        ConfigureTimingInput(_tapDurationInput, 1, 1000, settings.TapDurationMs);
        ConfigureTimingInput(_holdReleaseGuardInput, 0, 1000, settings.HoldReleaseGuardMs);
        ConfigureTimingInput(_menuNavigationDelayInput, 50, 1000, settings.MenuNavigationDelayMs);

        AddNumberRow(table, 0, "Hit bias (ms):", _hitBiasInput);
        AddNumberRow(table, 1, "Tap duration (ms):", _tapDurationInput);
        AddNumberRow(table, 2, "Hold release guard (ms):", _holdReleaseGuardInput);
        AddNumberRow(table, 3, "Menu navigation delay (ms):", _menuNavigationDelayInput);
        group.Controls.Add(table);
        return group;
    }

    private Control BuildHotkeyGroup(BotSettings settings)
    {
        GroupBox group = CreateGroup("Global Hotkeys");
        TableLayoutPanel table = CreateKeyTable(4);
        AddKeyRow(table, 0, "Start:", _startHotKey, settings.StartHotKey);
        AddKeyRow(table, 1, "Stop:", _stopHotKey, settings.StopHotKey);
        AddKeyRow(
            table,
            2,
            "Decrease hit bias:",
            _decreaseHitBiasHotKey,
            settings.DecreaseHitBiasHotKey);
        AddKeyRow(
            table,
            3,
            "Increase hit bias:",
            _increaseHitBiasHotKey,
            settings.IncreaseHitBiasHotKey);
        group.Controls.Add(table);
        return group;
    }

    private Control BuildInputGroup(BotSettings settings)
    {
        GroupBox group = CreateGroup("Lane Inputs");
        TableLayoutPanel container = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2
        };
        TableLayoutPanel table = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(7),
            ColumnCount = 3,
            RowCount = 5
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.Controls.Add(CreateLabel("Lane"), 0, 0);
        table.Controls.Add(CreateLabel("Primary"), 1, 0);
        table.Controls.Add(CreateLabel("Alternate"), 2, 0);

        for (int lane = 0; lane < LaneNames.Length; lane++)
        {
            table.Controls.Add(CreateLabel($"{LaneNames[lane]}:"), 0, lane + 1);
            IReadOnlyList<VirtualKey> keys = settings.InputKeys[lane];
            for (int slot = 0; slot < 2; slot++)
            {
                KeyCaptureTextBox input = new() { AllowEmpty = slot == 1 };
                input.SelectedKey = slot < keys.Count
                    ? (Keys)(ushort)keys[slot]
                    : Keys.None;
                _laneKeys[lane, slot] = input;
                table.Controls.Add(input, slot + 1, lane + 1);
            }
        }

        container.Controls.Add(table, 0, 0);
        container.Controls.Add(new Label
        {
            Text = "Click a field and press a key. Backspace, Delete, or Escape clears an alternate.",
            AutoSize = true,
            Tag = ThemeManager.SemanticMuted,
            Margin = new Padding(10, 0, 3, 7)
        }, 0, 1);
        group.Controls.Add(container);
        return group;
    }

    private Control BuildAppearanceGroup(BotSettings settings)
    {
        GroupBox group = CreateGroup("Appearance");
        FlowLayoutPanel row = CreateRow();
        row.Controls.Add(CreateLabel("Color scheme:"));
        _colorScheme.DropDownStyle = ComboBoxStyle.DropDownList;
        _colorScheme.Items.AddRange(Enum.GetNames<ColorScheme>());
        _colorScheme.SelectedItem = settings.ColorScheme.ToString();
        _colorScheme.Width = 120;
        _colorScheme.SelectedIndexChanged += (_, _) =>
        {
            if (Enum.TryParse(_colorScheme.SelectedItem?.ToString(), out ColorScheme scheme))
            {
                ThemeManager.Apply(this, scheme);
                if (Owner is not null)
                {
                    ThemeManager.Apply(Owner, scheme);
                }

                PerformLayout();
                Invalidate(true);
                Update();
            }
        };
        row.Controls.Add(_colorScheme);
        group.Controls.Add(row);
        return group;
    }

    private Control BuildButtons()
    {
        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        Button save = new() { Text = "Save", AutoSize = true };
        save.Click += (_, _) => SaveSettings();
        AcceptButton = save;

        Button cancel = new() { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        CancelButton = cancel;

        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        return buttons;
    }

    protected override void OnFormClosed(FormClosedEventArgs eventArgs)
    {
        if (DialogResult != DialogResult.OK && Owner is not null)
        {
            ThemeManager.Apply(Owner, _originalColorScheme);
        }

        base.OnFormClosed(eventArgs);
    }

    private void SaveSettings()
    {
        Keys[] hotkeys =
        [
            _startHotKey.SelectedKey,
            _stopHotKey.SelectedKey,
            _decreaseHitBiasHotKey.SelectedKey,
            _increaseHitBiasHotKey.SelectedKey
        ];
        if (hotkeys.Any(static key => key == Keys.None) || hotkeys.Distinct().Count() != hotkeys.Length)
        {
            MessageBox.Show(
                this,
                "Each global hotkey must be set to a different key.",
                "Invalid Hotkeys",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Dictionary<int, IReadOnlyList<VirtualKey>> inputKeys = [];
        for (int lane = 0; lane < LaneNames.Length; lane++)
        {
            List<VirtualKey> keys = [];
            for (int slot = 0; slot < 2; slot++)
            {
                Keys selected = _laneKeys[lane, slot].SelectedKey;
                if (selected != Keys.None)
                {
                    VirtualKey virtualKey = (VirtualKey)(ushort)selected;
                    if (!keys.Contains(virtualKey))
                    {
                        keys.Add(virtualKey);
                    }
                }
            }

            if (keys.Count == 0)
            {
                MessageBox.Show(
                    this,
                    $"{LaneNames[lane]} must have at least one input key.",
                    "Invalid Inputs",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            inputKeys[lane] = keys;
        }

        Enum.TryParse(_colorScheme.SelectedItem?.ToString(), out ColorScheme colorScheme);
        Result = new BotSettings(
            (double)_hitBiasInput.Value,
            (double)_tapDurationInput.Value,
            (double)_holdReleaseGuardInput.Value,
            (int)_menuNavigationDelayInput.Value,
            hotkeys[0],
            hotkeys[1],
            hotkeys[2],
            hotkeys[3],
            inputKeys,
            colorScheme);
        DialogResult = DialogResult.OK;
        Close();
    }

    private static GroupBox CreateGroup(string text)
    {
        return new GroupBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 6)
        };
    }

    private static FlowLayoutPanel CreateRow()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(7),
            WrapContents = false
        };
    }

    private static TableLayoutPanel CreateKeyTable(int rows)
    {
        TableLayoutPanel table = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(7),
            ColumnCount = 2,
            RowCount = rows
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return table;
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 7, 3, 3)
        };
    }

    private static void ConfigureTimingInput(
        NumericUpDown input,
        decimal minimum,
        decimal maximum,
        double value)
    {
        input.DecimalPlaces = 1;
        input.Increment = 0.5M;
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Value = (decimal)Math.Clamp(value, (double)minimum, (double)maximum);
        input.Width = 100;
        input.Anchor = AnchorStyles.Left;
    }

    private static void AddNumberRow(
        TableLayoutPanel table,
        int row,
        string label,
        NumericUpDown input)
    {
        table.Controls.Add(CreateLabel(label), 0, row);
        table.Controls.Add(input, 1, row);
    }

    private static void AddKeyRow(
        TableLayoutPanel table,
        int row,
        string label,
        KeyCaptureTextBox input,
        Keys value)
    {
        table.Controls.Add(CreateLabel(label), 0, row);
        input.SelectedKey = value;
        table.Controls.Add(input, 1, row);
    }
}
