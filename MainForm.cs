using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Security.Cryptography;
using System.Text;
using static FF14DiscordRelay.UiText;

namespace FF14DiscordRelay;

public sealed class MainForm : Form
{
    private const string RestrictedMessageTemplate = "[{name} say] : {message}";

    private static readonly Color PageBack = UiPalette.PageBack;
    private static readonly Color CardBack = UiPalette.CardBack;
    private static readonly Color Border = UiPalette.Border;
    private static readonly Color TextMain = UiPalette.TextMain;
    private static readonly Color TextMuted = UiPalette.TextMuted;
    private static readonly Color Accent = UiPalette.Accent;
    private static readonly Color AccentDark = UiPalette.AccentDark;
    private static readonly Color Success = UiPalette.Success;
    private static readonly Color Danger = UiPalette.Danger;

    private readonly SecureConfigStore _store;
    private AppConfig _config;
    private string? _password;
    private bool _hasSavedConfig;
    private bool _dirty;
    private bool _loadingControls;
    private bool _allowExit;
    private bool _restrictionsUnlocked;
    private bool _updatingChannelChecks;
    private bool _nicknameDetected;
    private bool _pausedForAct;
    private string _verifiedWebhookHash;
    private int _logoClickCount;
    private RelayService? _relay;
    private ToolStripMenuItem? _trayStatusMenuItem;
    private ToolStripMenuItem? _trayToggleRelayMenuItem;

    private readonly Label _saveStateLabel = StatusValueLabel();
    private readonly Label _actStateLabel = StatusValueLabel();
    private readonly Label _adminStateLabel = StatusValueLabel();
    private readonly Label _relayStateLabel = StatusValueLabel();

    private readonly Button _saveButton = PrimaryButton(K("7KCA7J6l"));
    private readonly Button _reloadButton = SecondaryButton(K("67aI65+s7Jik6riw"));
    private readonly Button _autoDetectButton = SecondaryButton(K("QUNUIOyekOuPmSDqsJDsp4A="));
    private readonly Button _startButton = PrimaryButton(K("7Iuc7J6R"));
    private readonly Button _stopButton = SecondaryButton(K("7KSR7KeA"));
    private readonly Button _browseActButton = SecondaryButton(K("7LC+6riw"));
    private readonly Button _testDiscordButton = SecondaryButton(K("7YWM7Iqk7Yq4"));

    private readonly TextBox _webhookBox = TextInput();
    private readonly TextBox _actFolderBox = TextInput();
    private readonly TextBox _nicknameBox = TextInput();
    private readonly TextBox _templateBox = TextInput();
    private readonly ComboBox _templatePresetBox = ComboInput();
    private readonly TextBox _processNamesBox = TextInput();

    private readonly CheckBox _ownOnlyBox = OptionBox(K("64K0IOuplOyLnOyngOunjCDsoITshqE="), true);
    private readonly CheckBox _disableMentionsBox = OptionBox(K("RGlzY29yZCDrqZjshZgg67mE7Zmc7ISx7ZmU"), true);
    private readonly CheckBox _splitLongBox = OptionBox(K("6ri0IOuplOyLnOyngCDsnpDrj5kg67aE7ZWg"), true);
    private readonly CheckBox _trayOnStartBox = OptionBox(K("7Iuc7J6R7ZWY66m0IO2KuOugiOydtOuhnCDsiKjquLDquLA="), true);
    private readonly CheckBox _saveLocalLogBox = OptionBox(K("66Gc7LusIOyghOyGoSDroZzqt7gg7KCA7J6l"), false);
    private readonly CheckBox _restrictionUnlockBox = OptionBox(K("7KCc7ZWcIO2VtOygnCDrqqjrk5w="), false);
    private readonly NumericUpDown _duplicateSecondsBox = new()
    {
        Minimum = 1,
        Maximum = 120,
        Width = 72,
        Value = 2,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = UiPalette.FieldBack,
        ForeColor = TextMain,
    };

    private readonly CheckedListBox _channelList = new()
    {
        CheckOnClick = true,
        IntegralHeight = false,
        BorderStyle = BorderStyle.None,
        Dock = DockStyle.Fill,
        BackColor = UiPalette.FieldBack,
        ForeColor = TextMain,
    };

    private readonly TextBox _logBox = new()
    {
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        BorderStyle = BorderStyle.None,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 9F),
        BackColor = UiPalette.FieldBack,
        ForeColor = TextMain,
    };

    private readonly NotifyIcon _notifyIcon;

    public MainForm(SecureConfigStore store, AppConfig config, string? password, bool loadedFromDisk)
    {
        _store = store;
        var restrictionChanged = NormalizeConfigForRestrictions(config);
        _config = config;
        _verifiedWebhookHash = config.VerifiedWebhookHash;
        _password = password;
        _hasSavedConfig = loadedFromDisk;

        Text = AppIdentity.DisplayName;
        MinimumSize = new Size(1000, 740);
        Size = new Size(1100, 780);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        BackColor = PageBack;
        ForeColor = TextMain;
        Icon = UiImages.Icon;

        _notifyIcon = BuildNotifyIcon();
        Controls.Add(BuildLayout());
        PopulateControls(config);
        WireEvents();
        ApplyRestrictionMode();
        UpdateTrayState();

        var detection = ProcessHelper.DetectAct();
        ApplyActDetection(
            detection,
            fillFields: !loadedFromDisk || string.IsNullOrWhiteSpace(config.ActLogFolder),
            fillNickname: !loadedFromDisk || string.IsNullOrWhiteSpace(config.Nickname));

        SetDirty(!loadedFromDisk || restrictionChanged);
        AppendLog(loadedFromDisk ? K("7ISk7KCV7J2EIOu2iOufrOyZlOyKteuLiOuLpC4=") : K("7LWc7LSIIOyLpO2WieyeheuLiOuLpC4g7ISk7KCVIOyggOyepSDtm4Qg7Iuc7J6R7ZWgIOyImCDsnojsirXri4jri6Qu"));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray(K("7LC97J2EIOuLq+yngCDslYrqs6Ag7Yq466CI7J2066GcIOyIqOqyvOyKteuLiOuLpC4="));
            return;
        }

        _notifyIcon.Visible = false;
        _relay?.Dispose();
        base.OnFormClosing(e);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        QueueChannelListScroll();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeWindowBridge.ShowSettingsMessage)
        {
            ShowSettingsFromOtherInstance();
            return;
        }

        base.WndProc(ref m);
    }

    internal void ShowSettingsFromOtherInstance()
    {
        if (InvokeRequired)
        {
            BeginInvoke(ShowSettingsFromOtherInstance);
            return;
        }

        ShowFromTray();
        AppendLog(K("6riw7KG0IOyLpO2WiSDssL3snYQg7Je07JeI7Iq164uI64ukLg=="));
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = PageBack,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildStatusBar(), 0, 1);
        root.Controls.Add(BuildToolbar(), 0, 2);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 12, 0, 12),
            BackColor = PageBack,
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        body.Controls.Add(BuildSettingsPanel(), 0, 0);
        body.Controls.Add(BuildChannelPanel(), 1, 0);
        root.Controls.Add(body, 0, 3);

        root.Controls.Add(BuildLogPanel(), 0, 4);
        return root;
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(2, 0, 0, 16),
            BackColor = PageBack,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var iconBox = new IconPreview
        {
            Image = UiImages.DisplayImage,
            Width = 56,
            Height = 56,
            Cursor = Cursors.Default,
            Margin = new Padding(0, 0, 14, 0),
        };
        iconBox.Click += (_, _) => HandleLogoClick();

        var copy = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, RowCount = 2 };
        copy.BackColor = PageBack;
        copy.Controls.Add(new Label
        {
            Text = AppIdentity.DisplayName,
            AutoSize = true,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = TextMain,
            BackColor = PageBack,
            Margin = new Padding(0, 2, 0, 2),
        }, 0, 0);
        copy.Controls.Add(new Label
        {
            Text = K("QUNUIOyxhO2MhSDroZzqt7jsl5DshJwg7ISg7YOd7ZWcIOuCtCDrqpTsi5zsp4DrrLwgRGlzY29yZCBXZWJob29r7Jy866GcIOyghOuLrO2VqeuLiOuLpC4="),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = TextMuted,
            BackColor = PageBack,
            Margin = new Padding(1, 0, 0, 0),
        }, 0, 1);

        header.Controls.Add(iconBox, 0, 0);
        header.Controls.Add(copy, 1, 0);
        return header;
    }

    private Control BuildStatusBar()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            Padding = new Padding(0, 0, 0, 12),
            BackColor = PageBack,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        panel.Controls.Add(StatusCard(K("7ISk7KCV"), _saveStateLabel), 0, 0);
        panel.Controls.Add(StatusCard("ACT", _actStateLabel), 1, 0);
        panel.Controls.Add(StatusCard(K("6raM7ZWc"), _adminStateLabel), 2, 0);
        panel.Controls.Add(StatusCard(K("7KSR6rOE"), _relayStateLabel), 3, 0);
        return panel;
    }

    private Control BuildToolbar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 0, 0, 2),
            BackColor = PageBack,
        };
        bar.Controls.Add(_saveButton);
        bar.Controls.Add(_reloadButton);
        bar.Controls.Add(_autoDetectButton);
        bar.Controls.Add(_startButton);
        bar.Controls.Add(_stopButton);
        return bar;
    }

    private Control BuildSettingsPanel()
    {
        var card = SectionCard(K("7ZWE7IiYIOyEpOyglQ=="));
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 9,
            Padding = new Padding(0),
            BackColor = CardBack,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (var i = 0; i < 6; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddField(layout, 0, "Webhook", _webhookBox, _testDiscordButton);
        AddField(layout, 1, K("QUNUIOuhnOq3uCDtj7TrjZQ="), _actFolderBox, _browseActButton);
        AddField(layout, 2, K("64K0IOuLieuEpOyehA=="), _nicknameBox, null);
        ConfigureTemplatePresetBox();
        AddField(layout, 3, K("66mU7Iuc7KeAIO2YleyLnQ=="), _templatePresetBox, null);
        AddField(layout, 4, K("7KeB7KCRIOyeheugpQ=="), _templateBox, null);
        AddField(layout, 5, K("QUNUIO2UhOuhnOyEuOyKpA=="), _processNamesBox, null);

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = true,
            AutoScroll = false,
            MinimumSize = new Size(0, 96),
            Padding = new Padding(0, 3, 0, 0),
            BackColor = CardBack,
        };
        options.Controls.Add(_ownOnlyBox);
        options.Controls.Add(_disableMentionsBox);
        options.Controls.Add(_splitLongBox);
        options.Controls.Add(_trayOnStartBox);
        options.Controls.Add(_restrictionUnlockBox);
        layout.Controls.Add(FieldLabel(K("7Ji17IWY")), 0, 6);
        layout.SetColumnSpan(options, 2);
        layout.Controls.Add(options, 1, 6);

        layout.Controls.Add(FieldLabel(K("7KSR67O1IOuwqeyngA==")), 0, 7);
        var duplicatePanel = new FlowLayoutPanel { AutoSize = true, BackColor = CardBack };
        duplicatePanel.Controls.Add(_duplicateSecondsBox);
        duplicatePanel.Controls.Add(new Label
        {
            Text = K("7LSIIOuPmeyViCDqsJnsnYAg66mU7Iuc7KeAIOustOyLnA=="),
            AutoSize = true,
            Padding = new Padding(6, 6, 0, 0),
            ForeColor = TextMuted,
            BackColor = CardBack,
        });
        layout.Controls.Add(duplicatePanel, 1, 7);

        var hint = new Label
        {
            Text = K("7KCA7J6l65CY7KeAIOyViuydgCDshKTsoJXsnLzroZzripQg7Iuc7J6R7ZWgIOyImCDsl4bsirXri4jri6QuIEFDVCDsnpDrj5kg6rCQ7KeA64qUIOyLpO2WiSDspJHsnbggQUNU7JmAIEFDVCDshKTsoJUg7YyM7J287J2YIOuhnOq3uCDqsr3roZzrpbwg7ZWo6ruYIO2ZleyduO2VqeuLiOuLpC4="),
            AutoSize = false,
            Height = 58,
            Dock = DockStyle.Fill,
            ForeColor = TextMuted,
            BackColor = CardBack,
            Padding = new Padding(0, 14, 0, 0),
        };
        layout.SetColumnSpan(hint, 3);
        layout.Controls.Add(hint, 0, 8);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildChannelPanel()
    {
        var card = SectionCard(K("6rCA7KC47JisIOyxhO2MhSDqtazrtoQ="));
        foreach (var channel in ChannelCatalog.All)
            _channelList.Items.Add(channel, false);
        _channelList.DisplayMember = nameof(ChannelDefinition.DisplayName);
        card.Controls.Add(_channelList);
        return card;
    }

    private Control BuildLogPanel()
    {
        var card = SectionCard(K("7IOB7YOcIOuhnOq3uA=="));
        card.Controls.Add(_logBox);
        return card;
    }

    private NotifyIcon BuildNotifyIcon()
    {
        var menu = new ContextMenuStrip();
        _trayStatusMenuItem = new ToolStripMenuItem { Enabled = false };
        _trayToggleRelayMenuItem = new ToolStripMenuItem();
        _trayToggleRelayMenuItem.Click += (_, _) =>
        {
            if (_relay is null)
                _ = StartRelayAsync();
            else
                StopRelay();
        };

        menu.Items.Add(_trayStatusMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(K("7ISk7KCVIOyXtOq4sA=="), null, (_, _) => ShowFromTray());
        menu.Items.Add(_trayToggleRelayMenuItem);
        menu.Items.Add(K("7KKF66OM"), null, (_, _) =>
        {
            _allowExit = true;
            Close();
        });

        var icon = new NotifyIcon
        {
            Icon = UiImages.Icon,
            Text = AppIdentity.DisplayName,
            ContextMenuStrip = menu,
            Visible = true,
        };
        icon.DoubleClick += (_, _) => ShowFromTray();
        return icon;
    }

    private void WireEvents()
    {
        _saveButton.Click += (_, _) => SaveConfig();
        _reloadButton.Click += (_, _) => ReloadConfig();
        _autoDetectButton.Click += (_, _) => RunAutoDetect(fillFields: true);
        _browseActButton.Click += (_, _) => BrowseActFolder();
        _testDiscordButton.Click += async (_, _) => await TestWebhookAsync();
        _startButton.Click += async (_, _) => await StartRelayAsync();
        _stopButton.Click += (_, _) => StopRelay();

        foreach (var control in new Control[] { _webhookBox, _actFolderBox, _nicknameBox, _templateBox, _processNamesBox })
            control.TextChanged += (_, _) => SetDirty(true);
        _nicknameBox.TextChanged += (_, _) =>
        {
            if (!_loadingControls)
                _nicknameDetected = false;
        };
        _webhookBox.TextChanged += (_, _) => UpdateWebhookTestButton();
        _templatePresetBox.SelectedIndexChanged += (_, _) => ApplyTemplatePreset(markDirty: true);
        foreach (var box in GetRestrictableOptionBoxes())
            box.CheckedChanged += (_, _) => SetDirty(true);
        _restrictionUnlockBox.CheckedChanged += (_, _) =>
        {
            if (!_loadingControls)
                SetRestrictionUnlocked(_restrictionUnlockBox.Checked, markDirty: true);
        };
        _duplicateSecondsBox.ValueChanged += (_, _) => SetDirty(true);
        _channelList.ItemCheck += ChannelList_ItemCheck;
    }

    private void PopulateControls(AppConfig config)
    {
        _loadingControls = true;
        try
        {
            _webhookBox.Text = config.WebhookUrl;
            _actFolderBox.Text = config.ActLogFolder;
            _nicknameBox.Text = config.Nickname;
            _templateBox.Text = config.MessageTemplate;
            SelectTemplatePreset(config.MessageTemplate);
            _processNamesBox.Text = string.Join(", ", config.ActProcessNames);
            _ownOnlyBox.Checked = config.OwnMessagesOnly;
            _disableMentionsBox.Checked = config.DisableMentions;
            _splitLongBox.Checked = config.SplitLongMessages;
            _trayOnStartBox.Checked = config.StartMinimizedToTray;
            _saveLocalLogBox.Checked = false;
            _duplicateSecondsBox.Value = Math.Clamp(config.DuplicateWindowSeconds, 1, 120);

            var enabled = config.EnabledChannelCodes.Select(ChannelCatalog.NormalizeCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < _channelList.Items.Count; i++)
            {
                var channel = (ChannelDefinition)_channelList.Items[i];
                _channelList.SetItemChecked(i, enabled.Contains(channel.CodeHex));
            }
        }
        finally
        {
            _loadingControls = false;
        }

        QueueChannelListScroll();
    }

    private AppConfig ReadControls()
    {
        var config = new AppConfig
        {
            WebhookUrl = _webhookBox.Text.Trim(),
            VerifiedWebhookHash = IsWebhookVerified(_webhookBox.Text.Trim()) ? WebhookHash(_webhookBox.Text.Trim()) : "",
            ActLogFolder = _actFolderBox.Text.Trim(),
            Nickname = _nicknameBox.Text.Trim(),
            MessageTemplate = string.IsNullOrWhiteSpace(_templateBox.Text) ? "{message}" : _templateBox.Text,
            ActProcessNames = _processNamesBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            OwnMessagesOnly = _ownOnlyBox.Checked,
            DisableMentions = _disableMentionsBox.Checked,
            SplitLongMessages = _splitLongBox.Checked,
            StartMinimizedToTray = _trayOnStartBox.Checked,
            SaveLocalRelayLog = false,
            DuplicateWindowSeconds = (int)_duplicateSecondsBox.Value,
            EnabledChannelCodes = _channelList.CheckedItems.Cast<ChannelDefinition>().Select(x => x.CodeHex).ToList(),
        };

        NormalizeConfigForRestrictions(config);
        return config;
    }

    private void SaveConfig()
    {
        var next = ReadControls();
        var error = ValidateConfigForSave(next);
        if (error is not null)
        {
            MessageBox.Show(error, K("7ISk7KCVIOyggOyepQ=="), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_password is null)
        {
            using var create = new CreatePasswordForm(K("67mE67CA67KI7Zi4IOyDneyEsQ=="));
            if (create.ShowDialog(this) != DialogResult.OK)
                return;
            _password = create.Password;
        }

        try
        {
            _store.Save(next, _password);
            _config = next;
            _hasSavedConfig = true;
            SetDirty(false);
            UpdateWebhookTestButton();
            AppendLog(K("7ISk7KCV7J2EIOyggOyepe2WiOyKteuLiOuLpC4="));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{K("7ISk7KCV7J2EIOyggOyepe2VmOyngCDrqrvtlojsirXri4jri6Qu")}\n\n{ex.Message}", K("7ISk7KCVIOyggOyepQ=="), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReloadConfig()
    {
        if (!_store.Exists || _password is null)
        {
            MessageBox.Show(K("67aI65+s7JisIOyggOyepSDshKTsoJXsnbQg7JeG7Iq164uI64ukLg=="), K("67aI65+s7Jik6riw"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_dirty && MessageBox.Show(K("7KCA7J6l7ZWY7KeAIOyViuydgCDrs4Dqsr3sgqztla3snbQg7IKs65287KeR64uI64ukLiDqs4Tsho3tlaDquYzsmpQ/"), K("67aI65+s7Jik6riw"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            _config = _store.Load(_password);
            _verifiedWebhookHash = _config.VerifiedWebhookHash;
            PopulateControls(_config);
            SetDirty(false);
            UpdateWebhookTestButton();
            AppendLog(K("7ISk7KCV7J2EIOuLpOyLnCDrtojrn6zsmZTsirXri4jri6Qu"));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{K("7ISk7KCV7J2EIOu2iOufrOyYpOyngCDrqrvtlojsirXri4jri6Qu")}\n\n{ex.Message}", K("67aI65+s7Jik6riw"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BrowseActFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = K("QUNUIEZGWElWIOuhnOq3uCDtj7TrjZQg7ISg7YOd") };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _actFolderBox.Text = dialog.SelectedPath;
    }

    private async Task StartRelayAsync()
    {
        if (!_hasSavedConfig)
        {
            MessageBox.Show(K("7ISk7KCV7J2EIOyggOyepe2VnCDrkqQg7Iuc7J6R7ZWgIOyImCDsnojsirXri4jri6Qu"), K("7Iuc7J6R"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_dirty)
        {
            MessageBox.Show(K("67OA6rK97IKs7ZWt7J2EIOyggOyepe2VnCDrkqQg7Iuc7J6R7ZWgIOyImCDsnojsirXri4jri6Qu"), K("7Iuc7J6R"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var detection = ProcessHelper.DetectAct();
        ApplyActDetection(detection, fillFields: false, fillNickname: false);
        if (!detection.IsRunning)
        {
            MessageBox.Show(K("QUNU6rCAIOyLpO2WiSDspJHsnbTslrTslbwg7Iuc7J6R7ZWgIOyImCDsnojsirXri4jri6Qu"), K("QUNUIO2ZleyduA=="), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Directory.Exists(_config.ActLogFolder))
        {
            MessageBox.Show(K("QUNUIOuhnOq3uCDtj7TrjZTrpbwg7LC+7J2EIOyImCDsl4bsirXri4jri6QuIEFDVCDsnpDrj5kg6rCQ7KeA66W8IOuLpOyLnCDsi6TtlontlZjshLjsmpQu"), K("QUNUIOuhnOq3uA=="), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        TryRefreshDetectedNickname(save: true);

        if (string.IsNullOrWhiteSpace(_config.Nickname) || !_nicknameDetected)
        {
            MessageBox.Show(K("64K0IOuLieuEpOyehOydtCDsnpDrj5kg6rCQ7KeA65CY7KeAIOyViuyVhCDsi6TtlontlaAg7IiYIOyXhuyKteuLiOuLpC4gQUNU7JmAIOy6kOumre2EsCDroZzqt7jsnbgg7IOB7YOc66W8IO2ZleyduO2VtCDso7zshLjsmpQu"), K("64K0IOuLieuEpOyehA=="), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!_config.HasRequiredStartSettings)
        {
            MessageBox.Show(K("V2ViaG9vaywgQUNUIOuhnOq3uCDtj7TrjZQsIOuLieuEpOyehCwg7LGE7YyFIOq1rOu2hCDshKTsoJXsnYQg7ZmV7J247ZWY7IS47JqULg=="), K("7Iuc7J6R"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!await EnsureWebhookVerifiedBeforeStartAsync())
            return;

        _pausedForAct = false;
        _relay = new RelayService(_config, AppendLog, HandleRelayStateChanged, HandleRelayFatalError, HandleRelayNicknameChanged);
        _relay.Start();
        _relayStateLabel.Text = K("7Iuk7ZaJIOykkQ==");
        _relayStateLabel.ForeColor = Success;
        UpdateStateLabels();
        UpdateTrayState();
        AppendLog(K("7KSR6rOE66W8IOyLnOyeke2WiOyKteuLiOuLpC4="));

        if (_config.StartMinimizedToTray)
            HideToTray(K("7KSR6rOE6rCAIOyLpO2WiSDspJHsnoXri4jri6Qu"));
    }

    private void StopRelay()
    {
        _relay?.Dispose();
        _relay = null;
        _pausedForAct = false;
        UpdateStateLabels();
        UpdateTrayState();
        AppendLog(K("7KSR6rOE66W8IOykkeyngO2WiOyKteuLiOuLpC4="));
    }

    private void RunAutoDetect(bool fillFields)
    {
        ApplyActDetection(ProcessHelper.DetectAct(), fillFields, fillNickname: true);
    }

    private void ApplyActDetection(ActDetectionResult detection, bool fillFields, bool fillNickname)
    {
        _actStateLabel.Text = detection.IsRunning ? K("7Iuk7ZaJIOykkQ==") : K("6rCQ7KeAIOyViCDrkKg=");
        _actStateLabel.ForeColor = detection.IsRunning ? Success : Danger;
        _adminStateLabel.Text = ProcessHelper.IsAdministrator() ? K("6rSA66as7J6Q") : K("7J2867CYIOq2jO2VnA==");
        _adminStateLabel.ForeColor = ProcessHelper.IsAdministrator() ? Success : Danger;

        if (fillFields)
        {
            if (!string.IsNullOrWhiteSpace(detection.ProcessName))
                _processNamesBox.Text = detection.ProcessName;
            if (!string.IsNullOrWhiteSpace(detection.LogFolder))
                _actFolderBox.Text = detection.LogFolder;
        }

        if (fillNickname)
            AutoFillNickname(detection.LogFolder ?? _actFolderBox.Text.Trim());

        AppendLog(detection.Message);
        if (!string.IsNullOrWhiteSpace(detection.LogFolder))
            AppendLog($"{K("QUNUIOuhnOq3uCDtj7TrjZQ6IA==")}{detection.LogFolder}");
    }

    private void AutoFillNickname(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            return;

        var enabled = _channelList.CheckedItems.Cast<ChannelDefinition>().Select(x => x.CodeHex).ToArray();
        var candidates = ActLogParser.DetectNicknameCandidates(folder, enabled);
        var candidate = candidates.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            _nicknameDetected = false;
            AppendLog(K("64uJ64Sk7J6EIOyekOuPmSDqsJDsp4Ag7ZuE67O066W8IOywvuyngCDrqrvtlojsirXri4jri6Qu"));
            return;
        }

        if (!_nicknameBox.Text.Equals(candidate, StringComparison.Ordinal))
        {
            _nicknameBox.Text = candidate;
            AppendLog($"{K("64uJ64Sk7J6EIOyekOuPmSDsnoXroKU6IA==")}{candidate}");
        }
        _nicknameDetected = true;
    }

    private void TryRefreshDetectedNickname(bool save)
    {
        var folder = _actFolderBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            var detection = ProcessHelper.DetectAct();
            ApplyActDetection(detection, fillFields: string.IsNullOrWhiteSpace(folder), fillNickname: false);
            folder = detection.LogFolder ?? _actFolderBox.Text.Trim();
        }

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            _nicknameDetected = false;
            return;
        }

        var nickname = ActLogParser.DetectLatestPrimaryPlayerName(folder);
        if (string.IsNullOrWhiteSpace(nickname))
        {
            var enabled = _channelList.CheckedItems.Cast<ChannelDefinition>().Select(x => x.CodeHex).ToArray();
            nickname = ActLogParser.DetectNicknameCandidates(folder, enabled).FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(nickname))
        {
            _nicknameDetected = false;
            return;
        }

        ApplyRuntimeNickname(nickname, save);
    }

    private async Task TestWebhookAsync()
    {
        TryRefreshDetectedNickname(save: false);

        var temp = ReadControls();
        var error = ValidateReadyForManualTest(temp);
        if (error is not null)
        {
            MessageBox.Show(error, K("7YWM7Iqk7Yq4IOyghOyGoQ=="), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            await new DiscordWebhookClient().SendAsync(temp, BuildWebhookTestMessage(temp), CancellationToken.None);
            MarkWebhookVerified(temp.WebhookUrl, save: false);
            AppendLog(K("RGlzY29yZCDthYzsiqTtirgg7KCE7IahIOyEseqztQ=="));
            MessageBox.Show(K("7YWM7Iqk7Yq4IOuplOyLnOyngOulvCDsoITshqHtlojsirXri4jri6Qu"), K("7YWM7Iqk7Yq4IOyghOyGoQ=="), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            if (DiscordWebhookClient.IsMissingWebhook(ex))
            {
                ShowWebhookMissingError();
                return;
            }

            MessageBox.Show($"{K("7YWM7Iqk7Yq4IOyghOyGoSDsi6TtjKg=")}\n\n{ex.Message}", K("7YWM7Iqk7Yq4IOyghOyGoQ=="), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task<bool> EnsureWebhookVerifiedBeforeStartAsync()
    {
        if (IsWebhookVerified(_config.WebhookUrl))
            return true;

        AppendLog(K("V2ViaG9vayDqsoDspp3snbQg7ZWE7JqU7ZWp64uI64ukLiDthYzsiqTtirgg7KCE7Iah7J2EIOuovOyggCDsi6Ttlontlanri4jri6Qu"));

        try
        {
            await new DiscordWebhookClient().SendAsync(_config, BuildWebhookTestMessage(_config), CancellationToken.None);
            MarkWebhookVerified(_config.WebhookUrl, save: true);
            AppendLog(K("V2ViaG9vayDthYzsiqTtirjrpbwg7Ya16rO87ZaI7Iq164uI64ukLg=="));
            return true;
        }
        catch (Exception ex)
        {
            if (DiscordWebhookClient.IsMissingWebhook(ex))
                ShowWebhookMissingError();
            else
                MessageBox.Show($"{K("V2ViaG9vayDqsoDspp0g7Iuk7Yyo")}\n\n{ex.Message}", K("V2ViaG9vayDqsoDspp0g7Iuk7Yyo"), MessageBoxButtons.OK, MessageBoxIcon.Error);

            return false;
        }
    }

    private string? ValidateConfigForSave(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.WebhookUrl))
            return K("V2ViaG9vayBVUkzsnYQg7J6F66Cl7ZWY7IS47JqULg==");
        if (string.IsNullOrWhiteSpace(config.ActLogFolder))
            return K("QUNUIOuhnOq3uCDtj7TrjZTrpbwg7J6F66Cl7ZWY7IS47JqULg==");
        if (!Directory.Exists(config.ActLogFolder))
            return K("QUNUIOuhnOq3uCDtj7TrjZTqsIAg7KG07J6s7ZWY7KeAIOyViuyKteuLiOuLpC4=");
        if (config.EnabledChannelCodes.Count == 0)
            return K("7KCE7Iah7ZWgIOyxhO2MhSDqtazrtoTsnYQg7ZWY64KYIOydtOyDgSDshKDtg53tlZjshLjsmpQu");
        if (!_restrictionsUnlocked && config.EnabledChannelCodes.Count > 1)
            return K("7KCc7ZWcIOuqqOuTnOyXkOyEnOuKlCDssYTtjIUg6rWs67aE7J2EIDHqsJzrp4wg7ISg7YOd7ZWgIOyImCDsnojsirXri4jri6Qu");
        if (config.OwnMessagesOnly && string.IsNullOrWhiteSpace(config.Nickname))
            return K("64K0IOuplOyLnOyngOunjCDsoITshqHtlZjroKTrqbQg64K0IOuLieuEpOyehOydhCDsnoXroKXtlZjshLjsmpQu");
        return null;
    }

    private string? ValidateReadyForManualTest(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.WebhookUrl))
            return K("V2ViaG9vayBVUkzsnYQg7J6F66Cl7ZWY7IS47JqULg==");
        if (string.IsNullOrWhiteSpace(config.Nickname) || !_nicknameDetected)
            return K("64K0IOuLieuEpOyehOydtCDsnpDrj5kg6rCQ7KeA65CY7KeAIOyViuyVhCDthYzsiqTtirjtlaAg7IiYIOyXhuyKteuLiOuLpC4gQUNU7JmAIOy6kOumre2EsCDroZzqt7jsnbgg7IOB7YOc66W8IO2ZleyduO2VtCDso7zshLjsmpQu");
        return null;
    }

    private bool NormalizeConfigForRestrictions(AppConfig config)
    {
        if (_restrictionsUnlocked)
        {
            config.SaveLocalRelayLog = false;
            return false;
        }

        var changed = false;
        changed |= SetIfDifferent(() => config.MessageTemplate, value => config.MessageTemplate = value, RestrictedMessageTemplate);
        changed |= SetIfDifferent(() => config.OwnMessagesOnly, value => config.OwnMessagesOnly = value, true);
        changed |= SetIfDifferent(() => config.DisableMentions, value => config.DisableMentions = value, true);
        changed |= SetIfDifferent(() => config.SplitLongMessages, value => config.SplitLongMessages = value, true);
        changed |= SetIfDifferent(() => config.StartMinimizedToTray, value => config.StartMinimizedToTray = value, true);
        changed |= SetIfDifferent(() => config.SaveLocalRelayLog, value => config.SaveLocalRelayLog = value, false);

        var defaultProcessNames = AppConfig.CreateDefault().ActProcessNames;
        if (!config.ActProcessNames.SequenceEqual(defaultProcessNames, StringComparer.OrdinalIgnoreCase))
        {
            config.ActProcessNames = defaultProcessNames.ToList();
            changed = true;
        }

        if (config.EnabledChannelCodes.Count > 1)
        {
            config.EnabledChannelCodes = [config.EnabledChannelCodes[0]];
            changed = true;
        }
        else if (config.EnabledChannelCodes.Count == 0)
        {
            config.EnabledChannelCodes = [ChannelCatalog.CrossWorldLinkshell[0].CodeHex];
            changed = true;
        }

        return changed;

        static bool SetIfDifferent<T>(Func<T> get, Action<T> set, T value)
        {
            if (EqualityComparer<T>.Default.Equals(get(), value))
                return false;
            set(value);
            return true;
        }
    }

    private void ApplyRestrictionMode()
    {
        _loadingControls = true;
        try
        {
            if (!_restrictionsUnlocked)
            {
                _restrictionUnlockBox.Visible = false;
                _restrictionUnlockBox.Checked = false;
                foreach (var box in GetRestrictableOptionBoxes())
                {
                    box.Checked = true;
                    box.Enabled = false;
                }
                _saveLocalLogBox.Checked = false;
                SelectTemplatePreset(RestrictedMessageTemplate);
                _templatePresetBox.Enabled = false;
                _templateBox.Enabled = false;
                _templateBox.Text = RestrictedMessageTemplate;
                _processNamesBox.Text = string.Join(", ", AppConfig.CreateDefault().ActProcessNames);
                _processNamesBox.ReadOnly = true;
                _nicknameBox.ReadOnly = true;
                EnsureSingleChannelSelection();
            }
            else
            {
                _restrictionUnlockBox.Visible = true;
                _restrictionUnlockBox.Checked = true;
                foreach (var box in GetRestrictableOptionBoxes())
                    box.Enabled = true;
                _templatePresetBox.Enabled = true;
                _templateBox.Enabled = _templatePresetBox.SelectedItem is TemplatePreset { IsCustom: true };
                _processNamesBox.ReadOnly = false;
                _nicknameBox.ReadOnly = false;
            }
        }
        finally
        {
            _loadingControls = false;
        }

        QueueChannelListScroll();
    }

    private void EnsureSingleChannelSelection()
    {
        if (_restrictionsUnlocked || _channelList.Items.Count == 0)
            return;

        var checkedIndexes = Enumerable.Range(0, _channelList.Items.Count).Where(_channelList.GetItemChecked).ToArray();
        var keep = checkedIndexes.FirstOrDefault();
        if (checkedIndexes.Length == 0)
            keep = 0;

        for (var i = 0; i < _channelList.Items.Count; i++)
            _channelList.SetItemChecked(i, i == keep);

        QueueChannelListScroll();
    }

    private void QueueChannelListScroll()
    {
        if (IsDisposed || _channelList.IsDisposed)
            return;
        if (!IsHandleCreated || !_channelList.IsHandleCreated)
            return;

        BeginInvoke(ScrollChannelListToFirstChecked);
    }

    private void ScrollChannelListToFirstChecked()
    {
        for (var i = 0; i < _channelList.Items.Count; i++)
        {
            if (!_channelList.GetItemChecked(i))
                continue;

            _channelList.SelectedIndex = i;
            _channelList.TopIndex = i;
            return;
        }
    }

    private void ChannelList_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (_loadingControls || _updatingChannelChecks)
            return;

        if (!_restrictionsUnlocked)
        {
            if (e.NewValue == CheckState.Unchecked && _channelList.GetItemChecked(e.Index) && _channelList.CheckedItems.Count <= 1)
            {
                e.NewValue = CheckState.Checked;
                return;
            }

            if (e.NewValue == CheckState.Checked)
            {
                BeginInvoke(() =>
                {
                    _updatingChannelChecks = true;
                    try
                    {
                        for (var i = 0; i < _channelList.Items.Count; i++)
                        {
                            if (i != e.Index)
                                _channelList.SetItemChecked(i, false);
                        }
                    }
                    finally
                    {
                        _updatingChannelChecks = false;
                    }

                    SetDirty(true);
                });
                return;
            }
        }

        BeginInvoke(() => SetDirty(true));
    }

    private void HandleLogoClick()
    {
        if (_restrictionsUnlocked)
            return;

        _logoClickCount++;
        if (_logoClickCount < 5)
            return;

        SetRestrictionUnlocked(true, markDirty: true);
        AppendLog(K("7KCc7ZWcIO2VtOygnCDrqqjrk5zqsIAg7Zmc7ISx7ZmU65CY7JeI7Iq164uI64ukLg=="));
    }

    private void SetRestrictionUnlocked(bool unlocked, bool markDirty)
    {
        if (_restrictionsUnlocked == unlocked)
            return;

        _restrictionsUnlocked = unlocked;
        if (!unlocked)
            _logoClickCount = 0;

        ApplyRestrictionMode();
        if (markDirty)
            SetDirty(true);
    }

    private void SetDirty(bool dirty)
    {
        if (_loadingControls)
            return;

        _dirty = dirty;
        UpdateStateLabels();
    }

    private void UpdateStateLabels()
    {
        _saveStateLabel.Text = !_hasSavedConfig ? K("7KCA7J6lIO2VhOyalA==") : _dirty ? K("67OA6rK965Co") : K("7KCA7J6l65Co");
        _saveStateLabel.ForeColor = !_hasSavedConfig || _dirty ? Danger : Success;
        _saveButton.Visible = !_hasSavedConfig || _dirty;
        _reloadButton.Visible = _hasSavedConfig && _dirty;
        UpdateWebhookTestButton();

        if (_relay is null)
        {
            _relayStateLabel.Text = K("7KSR7KeA65Co");
            _relayStateLabel.ForeColor = TextMuted;
        }
        else if (_pausedForAct)
        {
            _relayStateLabel.Text = K("7J287Iuc7KCV7KeA");
            _relayStateLabel.ForeColor = TextMuted;
        }
        else
        {
            _relayStateLabel.Text = K("7Iuk7ZaJIOykkQ==");
            _relayStateLabel.ForeColor = Success;
        }

        _startButton.Visible = _relay is null && _hasSavedConfig && !_dirty;
        _stopButton.Visible = _relay is not null;
        ApplyRunningLock();
        UpdateTrayState();
    }

    private void ApplyRunningLock()
    {
        var running = _relay is not null;

        _webhookBox.ReadOnly = running;
        _actFolderBox.ReadOnly = running;
        _nicknameBox.ReadOnly = running || !_restrictionsUnlocked;
        _processNamesBox.ReadOnly = running || !_restrictionsUnlocked;

        _templatePresetBox.Enabled = !running && _restrictionsUnlocked;
        _templateBox.Enabled = !running && _restrictionsUnlocked && _templatePresetBox.SelectedItem is TemplatePreset { IsCustom: true };
        _browseActButton.Enabled = !running;
        _testDiscordButton.Enabled = !running;
        _autoDetectButton.Enabled = !running;
        _saveButton.Enabled = !running;
        _reloadButton.Enabled = !running;

        foreach (var box in GetRestrictableOptionBoxes())
            box.Enabled = !running && _restrictionsUnlocked;
        _restrictionUnlockBox.Enabled = !running && _restrictionsUnlocked;

        _duplicateSecondsBox.Enabled = !running;
        _channelList.Enabled = !running;
    }

    private void UpdateWebhookTestButton()
    {
        if (_testDiscordButton is null)
            return;

        _testDiscordButton.Visible = !IsWebhookVerified(_webhookBox.Text.Trim());
    }

    private bool IsWebhookVerified(string webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            return false;

        return string.Equals(_verifiedWebhookHash, WebhookHash(webhookUrl), StringComparison.Ordinal);
    }

    private void MarkWebhookVerified(string webhookUrl, bool save)
    {
        _verifiedWebhookHash = WebhookHash(webhookUrl);
        if (string.Equals(_config.WebhookUrl, webhookUrl, StringComparison.Ordinal))
            _config.VerifiedWebhookHash = _verifiedWebhookHash;
        UpdateWebhookTestButton();

        if (!save || _password is null)
            return;

        try
        {
            _store.Save(_config, _password);
            AppendLog(K("V2ViaG9vayDqsoDspp0g7IOB7YOc6rCAIOyggOyepeuQmOyXiOyKteuLiOuLpC4="));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{K("7ISk7KCV7J2EIOyggOyepe2VmOyngCDrqrvtlojsirXri4jri6Qu")}\n\n{ex.Message}", K("7ISk7KCVIOyggOyepQ=="), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string BuildWebhookTestMessage(AppConfig config)
    {
        return $"[{config.Nickname} say] {K("RkYxNCBEaXNjb3JkIFJlbGF5IO2FjOyKpO2KuCDrqZTsi5zsp4DsnoXri4jri6Qu")}";
    }

    private static string WebhookHash(string webhookUrl)
    {
        var normalized = webhookUrl.Trim();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private void HandleRelayFatalError(Exception ex)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => HandleRelayFatalError(ex));
            return;
        }

        StopRelay();
        ShowWebhookMissingError();
        AppendLog(K("V2ViaG9vayDsmKTrpZjroZwg7KSR6rOE66W8IOykkeyngO2WiOyKteuLiOuLpC4="));
    }

    private void HandleRelayStateChanged(RelayRuntimeState state)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => HandleRelayStateChanged(state));
            return;
        }

        _pausedForAct = state == RelayRuntimeState.PausedForAct;
        UpdateStateLabels();
    }

    private void HandleRelayNicknameChanged(string nickname)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => HandleRelayNicknameChanged(nickname));
            return;
        }

        ApplyRuntimeNickname(nickname, save: true);
    }

    private void ApplyRuntimeNickname(string nickname, bool save)
    {
        var clean = nickname.Trim();
        if (string.IsNullOrWhiteSpace(clean))
            return;

        var changed = !_config.Nickname.Equals(clean, StringComparison.OrdinalIgnoreCase)
            || !_nicknameBox.Text.Equals(clean, StringComparison.Ordinal);
        _nicknameDetected = true;
        if (!changed)
            return;

        _config.Nickname = clean;

        _loadingControls = true;
        try
        {
            _nicknameBox.Text = clean;
        }
        finally
        {
            _loadingControls = false;
        }

        AppendLog($"{K("64uJ64Sk7J6EIOyekOuPmSDqsLHsi6A6IA==")}{clean}");

        if (save && _hasSavedConfig && _password is not null)
        {
            try
            {
                _store.Save(_config, _password);
                AppendLog(K("64uJ64Sk7J6EIOuzgOqyveydhCDsoIDsnqXtlojsirXri4jri6Qu"));
            }
            catch (Exception ex)
            {
                AppendLog($"{K("64uJ64Sk7J6EIOuzgOqyvSDsoIDsnqUg7Iuk7YyoOiA=")}{ex.Message}");
            }
        }

        UpdateStateLabels();
    }

    private void ShowWebhookMissingError()
    {
        _config.VerifiedWebhookHash = "";
        _verifiedWebhookHash = "";
        if (_password is not null)
        {
            try
            {
                _store.Save(_config, _password);
            }
            catch
            {
                // The visible error is the missing webhook; a failed status clear should not hide it.
            }
        }

        UpdateWebhookTestButton();
        ShowFromTray();
        MessageBox.Show(K("V2ViaG9va+ydtCDsobTsnqztlZjsp4Ag7JWK6rGw64KYIOygkeq3vO2VoCDsiJgg7JeG7Iq164uI64ukLiDshKTsoJXsnYQg7ZmV7J247ZWY7IS47JqULg=="), K("V2ViaG9vayDqsoDspp0g7Iuk7Yyo"), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void ConfigureTemplatePresetBox()
    {
        if (_templatePresetBox.Items.Count > 0)
            return;

        _templatePresetBox.Items.Add(new TemplatePreset(K("64K07Jqp66eM"), "{message}", false));
        _templatePresetBox.Items.Add(new TemplatePreset(K("7LGE64SQICsg64K07Jqp"), "[{channel}] {message}", false));
        _templatePresetBox.Items.Add(new TemplatePreset(K("7J2066aEICsg64K07Jqp"), RestrictedMessageTemplate, false));
        _templatePresetBox.Items.Add(new TemplatePreset(K("7LGE64SQICsg7J2066aEICsg64K07Jqp"), "[{channel}] {name}: {message}", false));
        _templatePresetBox.Items.Add(new TemplatePreset(K("7KeB7KCRIOyeheugpQ=="), "", true));
        _templatePresetBox.DisplayMember = nameof(TemplatePreset.Name);
    }

    private void SelectTemplatePreset(string template)
    {
        ConfigureTemplatePresetBox();
        for (var i = 0; i < _templatePresetBox.Items.Count; i++)
        {
            if (_templatePresetBox.Items[i] is TemplatePreset preset && !preset.IsCustom && preset.Template == template)
            {
                _templatePresetBox.SelectedIndex = i;
                _templateBox.Enabled = false;
                return;
            }
        }

        _templatePresetBox.SelectedIndex = _templatePresetBox.Items.Count - 1;
        _templateBox.Enabled = true;
    }

    private void ApplyTemplatePreset(bool markDirty)
    {
        if (_templatePresetBox.SelectedItem is not TemplatePreset preset)
            return;

        _templateBox.Enabled = preset.IsCustom;
        if (!preset.IsCustom)
            _templateBox.Text = preset.Template;

        if (markDirty)
            SetDirty(true);
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void HideToTray(string message)
    {
        Hide();
        ShowInTaskbar = false;
        _notifyIcon.Icon = UiImages.Icon;
    }

    private void ShowFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private void UpdateTrayState()
    {
        if (_notifyIcon is null)
            return;

        var running = _relay is not null;
        var stateText = !running ? K("7KSR7KeA65Co") : _pausedForAct ? K("7J287Iuc7KCV7KeA") : K("7Iuk7ZaJIOykkQ==");
        _notifyIcon.Text = $"{AppIdentity.DisplayName} - {stateText}";

        if (_trayStatusMenuItem is not null)
            _trayStatusMenuItem.Text = !running ? K("7IOB7YOcOiDspJHsp4DrkKg=") : _pausedForAct ? K("7IOB7YOcOiDsnbzsi5zsoJXsp4A=") : K("7IOB7YOcOiDsi6Ttlokg7KSR");
        if (_trayToggleRelayMenuItem is not null)
            _trayToggleRelayMenuItem.Text = running ? K("7KSR6rOEIOykkeyngA==") : K("7KSR6rOEIOyLnOyekQ==");
    }

    private CheckBox[] GetRestrictableOptionBoxes()
    {
        return [_ownOnlyBox, _disableMentionsBox, _splitLongBox, _trayOnStartBox];
    }

    private static CardPanel SectionCard(string title)
    {
        return new CardPanel
        {
            Title = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 44, 18, 18),
            Margin = new Padding(0, 0, 12, 0),
            BackColor = PageBack,
        };
    }

    private static Control StatusCard(string title, Label value)
    {
        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Height = 72,
            Padding = new Padding(16, 11, 16, 10),
            Margin = new Padding(0, 0, 10, 0),
            BackColor = PageBack,
        };

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = CardBack };
        layout.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 8.5F),
            BackColor = CardBack,
        }, 0, 0);
        layout.Controls.Add(value, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Label StatusValueLabel()
    {
        return new Label
        {
            Text = "-",
            AutoSize = true,
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = TextMain,
            Padding = new Padding(0, 2, 0, 0),
            BackColor = CardBack,
        };
    }

    private static Label FieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = CardBack,
        };
    }

    private static TextBox TextInput()
    {
        return new TextBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = UiPalette.FieldBack,
            ForeColor = TextMain,
            Height = 26,
        };
    }

    private static ComboBox ComboInput()
    {
        return new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = UiPalette.FieldBack,
            ForeColor = TextMain,
        };
    }

    private static CheckBox OptionBox(string text, bool isChecked)
    {
        return new CheckBox
        {
            Text = text,
            AutoSize = true,
            Checked = isChecked,
            ForeColor = TextMain,
            Margin = new Padding(0, 0, 14, 6),
            BackColor = CardBack,
        };
    }

    private static Button PrimaryButton(string text)
    {
        var button = BaseButton(text);
        button.BackColor = Accent;
        button.ForeColor = Color.FromArgb(32, 25, 15);
        if (button is RoundedButton rounded)
            rounded.BorderColor = Color.FromArgb(217, 141, 26);
        return button;
    }

    private static Button SecondaryButton(string text)
    {
        var button = BaseButton(text);
        button.BackColor = Color.FromArgb(255, 248, 234);
        button.ForeColor = AccentDark;
        if (button is RoundedButton rounded)
            rounded.BorderColor = Color.FromArgb(232, 198, 132);
        return button;
    }

    private static Button BaseButton(string text)
    {
        return new RoundedButton
        {
            Text = text,
            Width = 116,
            Height = 36,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand,
        };
    }

    private static void AddField(TableLayoutPanel layout, int row, string label, Control input, Control? action)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(FieldLabel(label), 0, row);
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 2, 8, 8);
        layout.Controls.Add(input, 1, row);
        if (action is not null)
        {
            action.Height = 30;
            action.Width = 92;
            action.Margin = new Padding(0, 1, 0, 8);
            layout.Controls.Add(action, 2, row);
        }
    }

    private sealed record TemplatePreset(string Name, string Template, bool IsCustom);

    private sealed class CardPanel : Panel
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title { get; set; } = "";

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;

            using var path = RoundedRect(rect, 12);
            using var fill = new SolidBrush(CardBack);
            using var border = new Pen(Border);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);

            if (!string.IsNullOrWhiteSpace(Title))
            {
                using var titleFont = new Font("Segoe UI", 10.5F, FontStyle.Bold);
                using var titleBrush = new SolidBrush(TextMain);
                e.Graphics.DrawString(Title, titleFont, titleBrush, new PointF(18, 15));
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
