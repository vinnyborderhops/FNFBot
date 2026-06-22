using System.Globalization;
using FNFBot.Core;
using FNFBot.Interop;
using FNFBot.Services;

namespace FNFBot.UI;

public sealed class MainForm : Form
{
    private const int WindowMessageHotKey = 0x0312;
    private const int StartHotKeyId = 1;
    private const int DecreaseHitBiasHotKeyId = 2;
    private const int IncreaseHitBiasHotKeyId = 3;
    private const int StopHotKeyId = 4;
    private const string NoSongsText = "No songs found";
    private const string ModsHeaderText = "Mods";

    private readonly TextBox _folderTextBox = new();
    private readonly ListBox _songListBox = new();
    private readonly FlowLayoutPanel _difficultyPanel = new();
    private readonly TextBox _delayInput = new();
    private readonly Button _usePredictedDelayButton = new();
    private readonly Label _hitBiasValueLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Label _infoLabel = new();
    private readonly Label _songStatsLabel = new();
    private readonly Button _loadButton = new();
    private readonly Button _startButton = new();
    private readonly Button _stopButton = new();
    private readonly Button _settingsButton = new();
    private readonly List<RadioButton> _difficultyButtons = [];

    private RhythmBot? _bot;
    private GlobalHotKeyManager? _hotKeys;
    private BotSettings _settings;
    private string _selectedDifficulty = "normal";
    private double? _predictedDelayMs;
    private double _hitBiasMs = RhythmBot.DefaultHitBiasMs;

    public MainForm()
    {
        _settings = UserSettings.Load(RhythmBot.DefaultHitBiasMs);
        _hitBiasMs = _settings.HitBiasMs;

        Text = "\U0001F3AE Friday Night Funkin' Bot";
        ClientSize = new Size(600, 780);
        MinimumSize = new Size(616, 500);
        MaximumSize = new Size(616, Screen.FromControl(this).WorkingArea.Height);
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 9F);

        BuildUi();
        ThemeManager.Apply(this, _settings.ColorScheme);
        SetFolder();
        RefreshSongs();
    }

    protected override void OnHandleCreated(EventArgs eventArgs)
    {
        base.OnHandleCreated(eventArgs);

        RegisterHotKeys(_settings, true);
    }

    protected override void OnHandleDestroyed(EventArgs eventArgs)
    {
        _hotKeys?.Dispose();
        _hotKeys = null;
        base.OnHandleDestroyed(eventArgs);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WindowMessageHotKey)
        {
            switch (message.WParam.ToInt32())
            {
                case StartHotKeyId:
                    BeginInvoke(new Action(StartBot));
                    break;
                case DecreaseHitBiasHotKeyId:
                    BeginInvoke(new Action(() => AdjustHitBias(-0.1)));
                    break;
                case IncreaseHitBiasHotKeyId:
                    BeginInvoke(new Action(() => AdjustHitBias(0.1)));
                    break;
                case StopHotKeyId:
                    BeginInvoke(new Action(StopBot));
                    break;
            }
        }

        base.WndProc(ref message);
    }

    private void BuildUi()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10),
            ColumnCount = 1,
            RowCount = 7
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        TableLayoutPanel header = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0, 0, 0, 10)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        Label title = new()
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Text = "\U0001F3AE Friday Night Funkin' Bot",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 0)
        };
        header.Controls.Add(title, 0, 0);

        _settingsButton.Text = "\u2699 Settings";
        _settingsButton.AutoSize = true;
        _settingsButton.Anchor = AnchorStyles.Right;
        _settingsButton.Click += (_, _) => OpenSettings();
        header.Controls.Add(_settingsButton, 1, 0);
        root.Controls.Add(header, 0, 0);

        root.Controls.Add(BuildFolderGroup(), 0, 1);
        root.Controls.Add(BuildSongGroup(), 0, 2);
        root.Controls.Add(BuildDifficultyGroup(), 0, 3);
        root.Controls.Add(BuildDelayGroup(), 0, 4);
        root.Controls.Add(BuildControlsGroup(), 0, 5);
        root.Controls.Add(BuildInfoGroup(), 0, 6);
    }

    private Control BuildFolderGroup()
    {
        GroupBox group = CreateGroup("Game Folder");
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(7)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = "Path:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 7, 3, 3)
        }, 0, 0);

        _folderTextBox.ReadOnly = true;
        _folderTextBox.Dock = DockStyle.Fill;
        _folderTextBox.Text = Environment.CurrentDirectory;
        layout.Controls.Add(_folderTextBox, 1, 0);

        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        Button browseButton = new() { Text = "\U0001F4C1 Browse", AutoSize = true };
        browseButton.Click += (_, _) => BrowseFolder();
        Button setFolderButton = new() { Text = "\u2705 Set Folder", AutoSize = true };
        setFolderButton.Click += (_, _) => SetFolder();
        buttons.Controls.Add(browseButton);
        buttons.Controls.Add(setFolderButton);
        layout.Controls.Add(buttons, 1, 1);

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildSongGroup()
    {
        GroupBox group = CreateGroup("Song Selection");
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(7)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label
        {
            Text = "Available Songs:",
            AutoSize = true,
            Margin = new Padding(3, 3, 3, 5)
        }, 0, 0);

        _songListBox.Dock = DockStyle.Fill;
        _songListBox.DrawMode = DrawMode.OwnerDrawFixed;
        _songListBox.IntegralHeight = false;
        _songListBox.DrawItem += DrawSongListItem;
        _songListBox.SelectedIndexChanged += (_, _) => OnSongSelected();
        layout.Controls.Add(_songListBox, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private Control BuildDifficultyGroup()
    {
        GroupBox group = CreateGroup("Difficulty");
        _difficultyPanel.Dock = DockStyle.Top;
        _difficultyPanel.AutoSize = false;
        _difficultyPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _difficultyPanel.Padding = new Padding(7);
        _difficultyPanel.WrapContents = true;
        _difficultyPanel.SizeChanged += (_, _) => ResizeDifficultyPanel();
        group.Controls.Add(_difficultyPanel);
        UpdateDifficulties([]);
        return group;
    }

    private Control BuildDelayGroup()
    {
        GroupBox group = CreateGroup("Delay Settings");
        FlowLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(7),
            WrapContents = false
        };

        FlowLayoutPanel delayRow = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        delayRow.Controls.Add(new Label
        {
            Text = "Delay before play (ms):",
            AutoSize = true,
            Margin = new Padding(3, 7, 3, 3)
        });

        _delayInput.Text = "3000";
        _delayInput.Width = 100;
        delayRow.Controls.Add(_delayInput);

        _usePredictedDelayButton.Text = "Use Predicted";
        _usePredictedDelayButton.AutoSize = true;
        _usePredictedDelayButton.Enabled = false;
        _usePredictedDelayButton.Click += (_, _) => UsePredictedDelay();
        delayRow.Controls.Add(_usePredictedDelayButton);
        layout.Controls.Add(delayRow);

        FlowLayoutPanel hitBiasRow = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        hitBiasRow.Controls.Add(new Label
        {
            Text = "Hit bias:",
            AutoSize = true,
            Margin = new Padding(3, 7, 3, 3)
        });

        Button decreaseHitBiasButton = new() { Text = "-0.5 ms", AutoSize = true };
        decreaseHitBiasButton.Click += (_, _) => AdjustHitBias(-0.5);
        hitBiasRow.Controls.Add(decreaseHitBiasButton);

        _hitBiasValueLabel.AutoSize = false;
        _hitBiasValueLabel.Width = 65;
        _hitBiasValueLabel.TextAlign = ContentAlignment.MiddleCenter;
        _hitBiasValueLabel.Margin = new Padding(3, 7, 3, 3);
        UpdateHitBiasLabel();
        hitBiasRow.Controls.Add(_hitBiasValueLabel);

        Button increaseHitBiasButton = new() { Text = "+0.5 ms", AutoSize = true };
        increaseHitBiasButton.Click += (_, _) => AdjustHitBias(0.5);
        hitBiasRow.Controls.Add(increaseHitBiasButton);
        layout.Controls.Add(hitBiasRow);

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildControlsGroup()
    {
        GroupBox group = CreateGroup("Controls");
        FlowLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(7),
            WrapContents = false
        };

        SetStatus("Status: Ready", Color.Green);
        _statusLabel.AutoSize = true;
        layout.Controls.Add(_statusLabel);

        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        _loadButton.Text = "\U0001F4DD Load Song";
        _loadButton.AutoSize = true;
        _loadButton.Click += (_, _) => LoadSong();
        _startButton.Text =
            $"\u25B6\uFE0F  Start ({KeyNames.ToDisplayName(_settings.StartHotKey)})";
        _startButton.AutoSize = true;
        _startButton.Click += (_, _) => StartBot();
        _stopButton.Text =
            $"\u23F9\uFE0F  Stop ({KeyNames.ToDisplayName(_settings.StopHotKey)})";
        _stopButton.AutoSize = true;
        _stopButton.Enabled = false;
        _stopButton.Click += (_, _) => StopBot();
        buttons.Controls.Add(_loadButton);
        buttons.Controls.Add(_startButton);
        buttons.Controls.Add(_stopButton);
        layout.Controls.Add(buttons);
        group.Controls.Add(layout);
        return group;
    }

    private Control BuildInfoGroup()
    {
        GroupBox group = CreateGroup("Current Song Info");
        FlowLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(7),
            WrapContents = false
        };

        _infoLabel.Text = "No song loaded";
        SetSemanticColor(_infoLabel, ThemeManager.SemanticMuted);
        _infoLabel.AutoSize = true;
        layout.Controls.Add(_infoLabel);

        _songStatsLabel.Text = "Scroll Speed: --    BPM: --\r\nPredicted Delay: -- ms";
        SetSemanticColor(_songStatsLabel, ThemeManager.SemanticMuted);
        _songStatsLabel.AutoSize = true;
        _songStatsLabel.Margin = new Padding(3, 4, 3, 3);
        layout.Controls.Add(_songStatsLabel);
        group.Controls.Add(layout);
        return group;
    }

    private static GroupBox CreateGroup(string text)
    {
        return new GroupBox
        {
            Text = text,
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 5)
        };
    }

    private void BrowseFolder()
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Select Game Folder",
            UseDescriptionForTitle = true,
            SelectedPath = _folderTextBox.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void SetFolder()
    {
        string folder = _folderTextBox.Text;
        if (!Directory.Exists(folder))
        {
            MessageBox.Show(
                this,
                "Invalid folder path",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        _bot = new RhythmBot(3000, folder, _settings.InputKeys)
        {
            HitBiasMs = _hitBiasMs,
            TapDurationMs = _settings.TapDurationMs,
            HoldReleaseGuardMs = _settings.HoldReleaseGuardMs,
            MenuNavigationDelayMs = _settings.MenuNavigationDelayMs
        };
        _bot.PlaybackFinished += OnPlaybackFinished;
        RefreshSongs();
        SetStatus("Status: Folder set \u2713", Color.Green);
    }

    private void RefreshSongs()
    {
        if (_bot is null)
        {
            return;
        }

        _songListBox.Items.Clear();
        IReadOnlyList<SongLocation> songs = _bot.ListSongLocations();
        if (songs.Count == 0)
        {
            _songListBox.Items.Add(NoSongsText);
            return;
        }

        foreach (SongLocation song in songs.Where(static song => !song.IsMod))
        {
            _songListBox.Items.Add(new SongListItem(song));
        }

        SongLocation[] modSongs = songs.Where(static song => song.IsMod).ToArray();
        if (modSongs.Length > 0)
        {
            _songListBox.Items.Add(new SongListHeader(ModsHeaderText));
            foreach (SongLocation song in modSongs)
            {
                _songListBox.Items.Add(new SongListItem(song));
            }
        }
    }

    private void OnSongSelected()
    {
        if (_songListBox.SelectedItem is SongListHeader)
        {
            _songListBox.ClearSelected();
            return;
        }

        if (_songListBox.SelectedItem is not SongListItem songItem)
        {
            return;
        }

        SongLocation song = songItem.Location;
        _infoLabel.Text = $"Selected: {song.DisplayName}";
        SetSemanticColor(_infoLabel, ThemeManager.SemanticText);
        SetEmptyStats();

        UpdateDifficulties(ChartLoader.AvailableDifficulties(song.Name, song.BasePath));
    }

    private void UpdateDifficulties(IReadOnlyList<string> difficulties)
    {
        foreach (RadioButton button in _difficultyButtons)
        {
            button.Dispose();
        }

        _difficultyButtons.Clear();
        _difficultyPanel.Controls.Clear();

        if (difficulties.Count == 0)
        {
            _selectedDifficulty = string.Empty;
            ResizeDifficultyPanel();
            return;
        }

        if (!difficulties.Contains(_selectedDifficulty, StringComparer.Ordinal))
        {
            _selectedDifficulty = difficulties.Contains("normal", StringComparer.Ordinal)
                ? "normal"
                : difficulties[0];
        }

        foreach (string difficulty in difficulties)
        {
            RadioButton button = new()
            {
                Text = ChartLoader.GetDisplayName(difficulty),
                Tag = difficulty,
                AutoSize = true,
                Checked = difficulty == _selectedDifficulty,
                Margin = new Padding(7, 3, 7, 3)
            };
            button.CheckedChanged += (_, _) =>
            {
                if (button.Checked)
                {
                    _selectedDifficulty = (string)button.Tag;
                }
            };
            _difficultyButtons.Add(button);
            _difficultyPanel.Controls.Add(button);
        }

        ResizeDifficultyPanel();
    }

    private void ResizeDifficultyPanel()
    {
        int availableWidth = _difficultyPanel.ClientSize.Width;
        if (availableWidth <= 0)
        {
            return;
        }

        int preferredHeight = _difficultyPanel
            .GetPreferredSize(new Size(availableWidth, 0))
            .Height;
        if (_difficultyPanel.Height != preferredHeight)
        {
            _difficultyPanel.Height = preferredHeight;
        }
    }

    private void LoadSong()
    {
        if (_bot is null)
        {
            MessageBox.Show(this, "No bot initialized", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (_songListBox.SelectedItem is not SongListItem songItem)
        {
            MessageBox.Show(this, "Please select a song", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SongLocation song = songItem.Location;
        string difficulty = _selectedDifficulty;
        if (_bot.LoadSong(song, difficulty))
        {
            int noteCount = _bot.CurrentChart!.Notes.Count;
            _infoLabel.Text = $"Loaded: {song.DisplayName} - {difficulty} ({noteCount} notes)";
            SetSemanticColor(_infoLabel, ThemeManager.SemanticSuccess);

            double? scrollSpeed = ChartLoader.GetScrollSpeed(_bot.CurrentChart, difficulty);
            double? bpm = MetadataLoader.LoadBpm(song.Name, difficulty, song.BasePath);
            SetSongStats(scrollSpeed, bpm);
            SetStatus("Status: Song loaded, ready to play", Color.Green);
        }
        else
        {
            MessageBox.Show(this, $"Failed to load {song.DisplayName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus("Status: Failed to load song", Color.Red);
        }
    }

    private void DrawSongListItem(object? sender, DrawItemEventArgs eventArgs)
    {
        if (eventArgs.Index < 0)
        {
            return;
        }

        object item = _songListBox.Items[eventArgs.Index];
        bool isHeader = item is SongListHeader;
        Color backColor = isHeader
            ? _songListBox.BackColor
            : eventArgs.BackColor;
        Color foreColor = isHeader
            ? ThemeManager.GetSemanticColor(_settings.ColorScheme, ThemeManager.SemanticMuted)
            : eventArgs.ForeColor;
        Font font = isHeader
            ? new Font(eventArgs.Font ?? _songListBox.Font, FontStyle.Bold)
            : eventArgs.Font ?? _songListBox.Font;

        using SolidBrush backgroundBrush = new(backColor);
        eventArgs.Graphics.FillRectangle(backgroundBrush, eventArgs.Bounds);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            item.ToString(),
            font,
            new Rectangle(
                eventArgs.Bounds.Left + 4,
                eventArgs.Bounds.Top,
                eventArgs.Bounds.Width - 8,
                eventArgs.Bounds.Height),
            foreColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

        if (!isHeader)
        {
            eventArgs.DrawFocusRectangle();
        }

        if (isHeader)
        {
            font.Dispose();
        }
    }

    private void StartBot()
    {
        if (_bot?.CurrentSong is null)
        {
            MessageBox.Show(this, "Please load a song first", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!double.TryParse(
                _delayInput.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double delay) ||
            !double.IsFinite(delay) ||
            delay < 0)
        {
            MessageBox.Show(this, "Delay must be a non-negative number", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _bot.DelayBeforePlayMs = delay;
        SetStatus("Status: Bot running...", Color.Orange);
        _loadButton.Enabled = false;
        _startButton.Enabled = false;
        _stopButton.Enabled = true;
        _bot.OnF1Pressed();
    }

    private void StopBot()
    {
        if (_bot is null)
        {
            return;
        }

        _bot.IsRunning = false;
        SetStatus("Status: Stopped", Color.Red);
        _loadButton.Enabled = true;
        _startButton.Enabled = true;
        _stopButton.Enabled = false;
    }

    private void OnPlaybackFinished(bool completed)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(new Action(() =>
        {
            SetStatus(completed ? "Status: Chart complete" : "Status: Stopped", completed ? Color.Green : Color.Red);
            _loadButton.Enabled = true;
            _startButton.Enabled = true;
            _stopButton.Enabled = false;
        }));
    }

    private void AdjustDelay(double amountMs)
    {
        double currentDelay = double.TryParse(
            _delayInput.Text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double parsedDelay) &&
            double.IsFinite(parsedDelay)
            ? parsedDelay
            : 3500;
        double newDelay = Math.Max(0, currentDelay + amountMs);
        _delayInput.Text = FormatDelay(newDelay);

        if (_bot is not null)
        {
            _bot.DelayBeforePlayMs = newDelay;
        }

        SetStatus($"Status: Delay set to {FormatDelay(newDelay)}ms", Color.Blue);
        Console.WriteLine($"Delay adjusted to {FormatDelay(newDelay)}ms");
    }

    private void AdjustHitBias(double amountMs)
    {
        _hitBiasMs = Math.Clamp(
            Math.Round(_hitBiasMs + amountMs, 1, MidpointRounding.AwayFromZero),
            -1000,
            1000);
        if (_bot is not null)
        {
            _bot.HitBiasMs = _hitBiasMs;
        }

        _settings = _settings with { HitBiasMs = _hitBiasMs };
        UserSettings.Save(_settings);
        UpdateHitBiasLabel();
        SetStatus($"Status: Hit bias set to {FormatSignedMilliseconds(_hitBiasMs)}", Color.Blue);
        Console.WriteLine($"Hit bias adjusted to {FormatSignedMilliseconds(_hitBiasMs)}");
    }

    private void UpdateHitBiasLabel()
    {
        _hitBiasValueLabel.Text = FormatSignedMilliseconds(_hitBiasMs);
    }

    private void UsePredictedDelay()
    {
        if (!_predictedDelayMs.HasValue)
        {
            return;
        }

        double predictedDelay = _predictedDelayMs.Value;
        _delayInput.Text = FormatDelay(predictedDelay);
        if (_bot is not null)
        {
            _bot.DelayBeforePlayMs = predictedDelay;
        }

        SetStatus($"Status: Using predicted delay of {FormatDelay(predictedDelay)}ms", Color.Blue);
    }

    private void SetSongStats(double? scrollSpeed, double? bpm)
    {
        if (bpm.HasValue)
        {
            double predictedDelay = 500 + (300000 / bpm.Value);
            _predictedDelayMs = predictedDelay;
            _usePredictedDelayButton.Enabled = true;
            string predictedScrollText = scrollSpeed.HasValue ? FormatNumber(scrollSpeed.Value) : "--";
            _songStatsLabel.Text =
                $"Scroll Speed: {predictedScrollText}    BPM: {FormatNumber(bpm.Value)}\r\n" +
                $"Predicted Delay: {FormatDelay(predictedDelay)} ms";
            SetSemanticColor(_songStatsLabel, ThemeManager.SemanticText);
            return;
        }

        _predictedDelayMs = null;
        _usePredictedDelayButton.Enabled = false;
        string scrollText = scrollSpeed.HasValue ? FormatNumber(scrollSpeed.Value) : "--";
        string bpmText = bpm.HasValue ? FormatNumber(bpm.Value) : "--";
        _songStatsLabel.Text =
            $"Scroll Speed: {scrollText}    BPM: {bpmText}\r\nPredicted Delay: -- ms";
        SetSemanticColor(_songStatsLabel, ThemeManager.SemanticMuted);
    }

    private void SetEmptyStats()
    {
        _predictedDelayMs = null;
        _usePredictedDelayButton.Enabled = false;
        _songStatsLabel.Text = "Scroll Speed: --    BPM: --\r\nPredicted Delay: -- ms";
        SetSemanticColor(_songStatsLabel, ThemeManager.SemanticMuted);
    }

    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = text;
        string semantic = color == Color.Green
            ? ThemeManager.SemanticSuccess
            : color == Color.Red
                ? ThemeManager.SemanticError
                : color == Color.Blue
                    ? ThemeManager.SemanticInfo
                    : color == Color.Orange
                        ? ThemeManager.SemanticWarning
                        : ThemeManager.SemanticText;
        SetSemanticColor(_statusLabel, semantic);
    }

    private void OpenSettings()
    {
        if (_bot?.IsRunning == true)
        {
            MessageBox.Show(
                this,
                "Stop playback before changing settings.",
                "Settings",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _hotKeys?.Dispose();
        _hotKeys = null;

        using SettingsForm dialog = new(_settings);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is not BotSettings updated)
        {
            RegisterHotKeys(_settings, true);
            return;
        }

        if (!RegisterHotKeys(updated, true))
        {
            RegisterHotKeys(_settings, false);
            return;
        }

        _settings = updated;
        _hitBiasMs = updated.HitBiasMs;
        if (_bot is not null)
        {
            _bot.HitBiasMs = updated.HitBiasMs;
            _bot.TapDurationMs = updated.TapDurationMs;
            _bot.HoldReleaseGuardMs = updated.HoldReleaseGuardMs;
            _bot.MenuNavigationDelayMs = updated.MenuNavigationDelayMs;
            _bot.InputKeys = updated.InputKeys;
        }

        UserSettings.Save(updated);
        UpdateHitBiasLabel();
        _startButton.Text = $"\u25B6\uFE0F  Start ({KeyNames.ToDisplayName(updated.StartHotKey)})";
        _stopButton.Text = $"\u23F9\uFE0F  Stop ({KeyNames.ToDisplayName(updated.StopHotKey)})";
        ThemeManager.Apply(this, updated.ColorScheme);
        SetStatus("Status: Settings saved", Color.Blue);
    }

    private bool RegisterHotKeys(BotSettings settings, bool showError)
    {
        _hotKeys?.Dispose();
        GlobalHotKeyManager hotKeys = new(Handle);
        try
        {
            hotKeys.Register(StartHotKeyId, settings.StartHotKey);
            hotKeys.Register(StopHotKeyId, settings.StopHotKey);
            hotKeys.Register(DecreaseHitBiasHotKeyId, settings.DecreaseHitBiasHotKey);
            hotKeys.Register(IncreaseHitBiasHotKeyId, settings.IncreaseHitBiasHotKey);
            _hotKeys = hotKeys;
            return true;
        }
        catch (Exception exception)
        {
            hotKeys.Dispose();
            _hotKeys = null;
            if (showError)
            {
                MessageBox.Show(
                    this,
                    $"Could not register global hotkeys: {exception.Message}",
                    "Hotkey Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return false;
        }
    }

    private void SetSemanticColor(Control control, string semantic)
    {
        control.Tag = semantic;
        control.ForeColor = ThemeManager.GetSemanticColor(_settings.ColorScheme, semantic);
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("G", CultureInfo.InvariantCulture);
    }

    private static string FormatDelay(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatSignedMilliseconds(double value)
    {
        string sign = value >= 0 ? "+" : string.Empty;
        return $"{sign}{FormatDelay(value)} ms";
    }

    private sealed record SongListItem(SongLocation Location)
    {
        public override string ToString()
        {
            return Location.IsMod ? $"  {Location.ModName} / {Location.Name}" : Location.Name;
        }
    }

    private sealed record SongListHeader(string Text)
    {
        public override string ToString()
        {
            return Text;
        }
    }
}
