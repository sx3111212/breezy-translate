namespace EnglishHelper;

sealed class SettingsWindow : Form
{
    readonly HotkeyManager hotkeys;
    readonly AppPreferences previous;
    readonly ComboBox uiLanguage = Selector(), readLanguage = Selector(), inputLanguage = Selector();
    readonly ComboBox inputSource = Selector();
    readonly ComboBox readProvider = Selector(), sentenceProvider = Selector(), inputProvider = Selector();
    readonly ComboBox serviceCategory = Selector();
    readonly TextBox readKey = new(), inputKey = new(), apiKey = new() { UseSystemPasswordChar = true }, region = new();
    readonly CheckBox readEnabled = new(), inputEnabled = new();
    readonly Label feedback = new();
    readonly ListBox navigation = new();
    readonly Panel sectionHost = new();
    readonly Label sectionTitle = new();
    readonly List<Panel> sections = new();
    readonly string[] sectionKeys = ["selectionFeature", "inputFeature", "service", "offlineDictionary", "generalSettings", "softwareUpdate", "donation"];
    readonly AppInfoPanel updates = new(false), donation = new(true);
    readonly ScenarioPreview reading = new(false), writing = new(true);
    readonly Dictionary<Control, string> textKeys = new();
    readonly AiSettingsPanel aiSettings;
    readonly Panel azureSettings = new();
    readonly Label serviceInfo = new();
    readonly Panel memorySettings = new();
    readonly TextBox memoryEmail = new() { MaxLength = 254, PlaceholderText = "you@example.com" };
    readonly DictionarySettingsPanel dictionaries = new();
    readonly Label readMemoryHint = new(), inputMemoryHint = new();
    string DraftLanguage => (uiLanguage.SelectedItem as AppLanguage)?.Code ?? previous.InterfaceLanguage;

    public SettingsWindow(HotkeyManager hotkeys)
    {
        this.hotkeys = hotkeys; previous = UserPreferences.Current;
        ClientSize = new Size(1000, Math.Min(700, Screen.PrimaryScreen!.WorkingArea.Height - 60)); MinimumSize = new Size(960, 600);
        MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterScreen; ShowInTaskbar = false;
        Font = new Font("Microsoft YaHei UI", 9); BackColor = Color.White;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, ColumnCount = 2, RowCount = 1 };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 196)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        navigation.Name = "SettingsNavigation"; navigation.Dock = DockStyle.Fill; navigation.Margin = new Padding(0, 0, 12, 0);
        navigation.BorderStyle = BorderStyle.None; navigation.BackColor = Color.FromArgb(246, 248, 253);
        navigation.Font = new Font(Font.FontFamily, 10); navigation.DrawMode = DrawMode.OwnerDrawFixed;
        navigation.ItemHeight = 48; navigation.IntegralHeight = false; navigation.HorizontalScrollbar = true;
        navigation.AccessibleName = UiText.T("settings", previous.InterfaceLanguage);
        navigation.DrawItem += DrawNavigationItem;
        navigation.Items.AddRange(sectionKeys);
        var detail = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, ColumnCount = 1, RowCount = 2 };
        detail.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sectionTitle.Name = "SettingsSectionTitle"; sectionTitle.Dock = DockStyle.Fill; sectionTitle.Margin = new Padding(24, 0, 0, 0);
        sectionTitle.Font = new Font(Font.FontFamily, 13, FontStyle.Bold); sectionTitle.ForeColor = Color.FromArgb(52, 65, 96);
        sectionTitle.TextAlign = ContentAlignment.MiddleLeft;
        sectionHost.Name = "SettingsSectionHost"; sectionHost.Dock = DockStyle.Fill; sectionHost.Margin = Padding.Empty;
        detail.Controls.Add(sectionTitle, 0, 0); detail.Controls.Add(sectionHost, 0, 1);
        body.Controls.Add(navigation, 0, 0); body.Controls.Add(detail, 1, 0); root.Controls.Add(body, 0, 0);
        var readTab = Page("selectionFeature"); var inputTab = Page("inputFeature"); var serviceTab = Page("service");
        var dictionaryTab = Page("offlineDictionary"); var generalTab = Page("generalSettings");
        var updateTab = Page("softwareUpdate"); var donationTab = Page("donation");
        updates.Dock = DockStyle.Fill; updateTab.Controls.Add(updates);
        donation.Dock = DockStyle.Fill; donationTab.Controls.Add(donation);
        AddLabel(serviceTab, "serviceType", 24, 28); serviceCategory.Name = "ServiceCategory"; Place(serviceTab, serviceCategory, 220, 24, 340, 30);
        aiSettings = new AiSettingsPanel(previous); Place(serviceTab, aiSettings, 24, 70, 720, 446);
        azureSettings.Name = "AzureConfiguration"; Place(serviceTab, azureSettings, 24, 70, 720, 210);
        memorySettings.Name = "MyMemoryConfiguration"; Place(serviceTab, memorySettings, 24, 70, 720, 400);
        serviceInfo.Name = "ServiceInformation"; serviceInfo.ForeColor = Color.FromArgb(81, 96, 131); Place(memorySettings, serviceInfo, 0, 0, 680, 48); textKeys[serviceInfo] = "memoryIntro";
        var quota = new Label { ForeColor = Color.FromArgb(81, 96, 131) }; textKeys[quota] = "memoryQuota"; Place(memorySettings, quota, 0, 65, 680, 65);
        AddLabel(memorySettings, "contactEmail", 0, 155); memoryEmail.Name = "MyMemoryEmail"; memoryEmail.Text = previous.MyMemoryEmail; Place(memorySettings, memoryEmail, 196, 151, 340, 30);
        var clearEmail = new RoundedButton { Name = "ClearMyMemoryEmail", BackColor = Color.White, ForeColor = Color.FromArgb(86, 103, 219) };
        textKeys[clearEmail] = "clearEmail"; Place(memorySettings, clearEmail, 546, 151, 110, 31); clearEmail.Click += (_, _) => memoryEmail.Clear();
        var privacy = new Label { ForeColor = Color.FromArgb(121, 133, 158) }; textKeys[privacy] = "memoryEmailNotice"; Place(memorySettings, privacy, 0, 206, 680, 120);
        var quotaLink = new LinkLabel { AutoSize = false, LinkColor = Color.FromArgb(86, 103, 219) }; textKeys[quotaLink] = "memoryQuotaLink"; Place(memorySettings, quotaLink, 0, 342, 400, 30);
        quotaLink.LinkClicked += (_, _) => { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://mymemory.translated.net/doc/usagelimits.php") { UseShellExecute = true }); } catch { feedback.Text = UiText.T("actionFailed", DraftLanguage); } };
        serviceCategory.SelectedIndexChanged += (_, _) => UpdateServiceEditor();

        uiLanguage.Name = "InterfaceLanguage"; readLanguage.Name = "ReadTarget"; inputLanguage.Name = "InputTarget";
        inputSource.Name = "InputSourceLanguage";
        readProvider.Name = "ReadProvider"; sentenceProvider.Name = "SentenceProvider"; inputProvider.Name = "InputProvider";
        readKey.Name = "ReadHotkey"; inputKey.Name = "InputHotkey";
        uiLanguage.Items.AddRange(Languages.All.Where(x => UiText.Supports(x.Code)).Cast<object>().ToArray());
        readLanguage.Items.AddRange(Languages.All.Cast<object>().ToArray()); inputLanguage.Items.AddRange(Languages.All.Cast<object>().ToArray());
        Choose(uiLanguage, previous.InterfaceLanguage); Choose(readLanguage, previous.ReadTarget); Choose(inputLanguage, previous.InputTarget);
        inputSource.Items.Add(new AppLanguage("auto", UiText.T("sourceGuess"), "", ""));
        inputSource.Items.AddRange(Languages.All.Cast<object>().ToArray()); Choose(inputSource, previous.InputSourceLanguage);

        Place(readTab, reading, 24, 22, 720, 170);
        AddLabel(readTab, "targetLanguage", 24, 222); Place(readTab, readLanguage, 220, 218, 310, 30);
        AddLabel(readTab, "wordService", 24, 266); Place(readTab, readProvider, 220, 262, 500, 30);
        AddLabel(readTab, "sentenceService", 24, 310); Place(readTab, sentenceProvider, 220, 306, 310, 30); ManageButton(readTab, 540, 306, false);
        ConfigureMemoryHint(readTab, readMemoryHint);
        AddLabel(readTab, "shortcuts", 24, 378); Place(readTab, readKey, 220, 374, 260, 32); Place(readTab, readEnabled, 500, 376, 220, 28);

        Place(inputTab, writing, 24, 22, 720, 170);
        AddLabel(inputTab, "source", 24, 222); Place(inputTab, inputSource, 220, 218, 310, 30);
        AddLabel(inputTab, "targetLanguage", 24, 266); Place(inputTab, inputLanguage, 220, 262, 310, 30);
        AddLabel(inputTab, "service", 24, 310); Place(inputTab, inputProvider, 220, 306, 310, 30); ManageButton(inputTab, 540, 306, true);
        ConfigureMemoryHint(inputTab, inputMemoryHint);
        AddLabel(inputTab, "shortcuts", 24, 378); Place(inputTab, inputKey, 220, 374, 260, 32); Place(inputTab, inputEnabled, 500, 376, 220, 28);
        sentenceProvider.SelectedIndexChanged += (_, _) => UpdateMemoryHints(); inputProvider.SelectedIndexChanged += (_, _) => UpdateMemoryHints();
        ConfigureKeyCapture(readKey, previous.ReadHotkey); ConfigureKeyCapture(inputKey, previous.InputHotkey);
        readEnabled.Name = "ReadHotkeyEnabled"; inputEnabled.Name = "InputHotkeyEnabled";
        textKeys[readEnabled] = textKeys[inputEnabled] = "enableShortcut";
        readEnabled.Checked = previous.HotkeysEnabled && previous.ReadHotkeyEnabled; inputEnabled.Checked = previous.HotkeysEnabled && previous.InputHotkeyEnabled;
        readKey.Enabled = readEnabled.Checked; inputKey.Enabled = inputEnabled.Checked;
        readEnabled.CheckedChanged += (_, _) => readKey.Enabled = readEnabled.Checked;
        inputEnabled.CheckedChanged += (_, _) => inputKey.Enabled = inputEnabled.Checked;

        AddLabel(generalTab, "interface", 24, 34); Place(generalTab, uiLanguage, 220, 30, 310, 30);
        Place(dictionaryTab, dictionaries, 24, 22, 700, 530);
        apiKey.Name = "AzureApiKey"; region.Name = "AzureRegion";
        AddLabel(azureSettings, "apiKey", 0, 8); Place(azureSettings, apiKey, 196, 4, 460, 30);
        apiKey.PlaceholderText = !string.IsNullOrEmpty(previous.ProtectedAzureKey) ? UiText.T("keySaved") : "API Key";
        AddLabel(azureSettings, "resourceRegion", 0, 58); Place(azureSettings, region, 196, 54, 460, 30); region.Text = previous.AzureRegion; region.PlaceholderText = "eastasia";

        var footer = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        feedback.ForeColor = Color.FromArgb(157, 89, 89); feedback.Font = new Font(Font.FontFamily, 8); feedback.Bounds = new Rectangle(0, 8, 470, 48); footer.Controls.Add(feedback);
        var cancel = new RoundedButton { BackColor = Color.FromArgb(237, 241, 248), ForeColor = Color.FromArgb(95, 111, 151), Size = new Size(90, 34) };
        var save = new RoundedButton { BackColor = Color.FromArgb(89, 110, 222), ForeColor = Color.White, Size = new Size(130, 34) };
        textKeys[cancel] = "cancel"; textKeys[save] = "save"; footer.Controls.AddRange([cancel, save]);
        footer.Resize += (_, _) => { save.Location = new Point(footer.Width - 130, 17); cancel.Location = new Point(footer.Width - 230, 17); feedback.Width = Math.Max(200, footer.Width - 245); };
        cancel.Click += (_, _) => Close(); save.Click += (_, _) => Save(); root.Controls.Add(footer, 0, 1); Controls.Add(root);
        uiLanguage.SelectedIndexChanged += (_, _) => RefreshText();
        readLanguage.SelectedIndexChanged += (_, _) => RefreshPreviews(); inputLanguage.SelectedIndexChanged += (_, _) => RefreshPreviews();
        inputSource.SelectedIndexChanged += (_, _) => RefreshPreviews();
        navigation.SelectedIndexChanged += (_, _) => { ShowSection(); RefreshServices(); };
        navigation.SelectedIndex = 0;
        RefreshText();
    }
    void ManageButton(Control parent, int x, int y, bool input)
    {
        var button = new RoundedButton { BackColor = Color.White, ForeColor = Color.FromArgb(86, 103, 219) };
        textKeys[button] = "manageServices"; Place(parent, button, x, y, 180, 31);
        button.Click += (_, _) =>
        {
            var selected = (input ? inputProvider : sentenceProvider).SelectedItem as ServiceChoice;
            SelectSection(2);
            var category = selected?.Code is "Azure" or "MyMemory" ? selected.Code : "AI";
            serviceCategory.SelectedItem = serviceCategory.Items.Cast<ServiceChoice>().First(c => c.Code == category);
        };
    }
    void UpdateServiceEditor()
    {
        var code = (serviceCategory.SelectedItem as ServiceChoice)?.Code ?? "AI";
        aiSettings.Visible = code == "AI"; azureSettings.Visible = code == "Azure"; memorySettings.Visible = code == "MyMemory";
    }
    void ConfigureMemoryHint(Control parent, Label label)
    {
        label.Font = new Font(Font.FontFamily, 8); label.ForeColor = Color.FromArgb(121, 133, 158); label.AutoEllipsis = true;
        textKeys[label] = "memoryQuotaHint"; Place(parent, label, 220, 339, 500, 23);
    }
    void UpdateMemoryHints()
    {
        readMemoryHint.Visible = (sentenceProvider.SelectedItem as ServiceChoice)?.Code == "MyMemory";
        inputMemoryHint.Visible = (inputProvider.SelectedItem as ServiceChoice)?.Code == "MyMemory";
    }
    internal void SelectSection(int index) => navigation.SelectedIndex = index;
    void ShowSection()
    {
        int selected = navigation.SelectedIndex;
        for (int i = 0; i < sections.Count; i++) sections[i].Visible = i == selected;
        if (selected >= 0)
        {
            sections[selected].BringToFront();
            sectionTitle.Text = UiText.T(sectionKeys[selected], DraftLanguage);
        }
        navigation.Invalidate();
    }
    void DrawNavigationItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(selected ? Color.FromArgb(228, 234, 252) : navigation.BackColor);
        e.Graphics.FillRectangle(background, e.Bounds);
        if (selected)
        {
            using var accent = new SolidBrush(Color.FromArgb(89, 110, 222));
            e.Graphics.FillRectangle(accent, e.Bounds.Left, e.Bounds.Top + 12, 3, e.Bounds.Height - 24);
        }
        var bounds = new Rectangle(e.Bounds.Left + 16, e.Bounds.Top, e.Bounds.Width - 24, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, navigation.GetItemText(navigation.Items[e.Index]), e.Font, bounds,
            selected ? Color.FromArgb(63, 82, 185) : Color.FromArgb(81, 96, 131),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        e.DrawFocusRectangle();
    }
    static ComboBox Selector() => new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Color.White, DropDownWidth = 510 };
    static void Choose(ComboBox selector, string code) => selector.SelectedItem = selector.Items.Cast<AppLanguage>().FirstOrDefault(x => x.Code == code);
    Panel Page(string key)
    {
        var page = new Panel { Name = key + "Section", Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true, Visible = false };
        sections.Add(page); sectionHost.Controls.Add(page); return page;
    }
    void AddLabel(Control parent, string key, int x, int y)
    {
        var label = new Label { ForeColor = Color.FromArgb(81, 96, 131) }; textKeys[label] = key; Place(parent, label, x, y, 190, 30);
    }
    static void Place(Control parent, Control child, int x, int y, int width, int height) { child.Bounds = new Rectangle(x, y, width, height); parent.Controls.Add(child); }
    void ConfigureKeyCapture(TextBox box, string initial)
    {
        box.ReadOnly = true; box.ShortcutsEnabled = false; box.BackColor = Color.White; box.Font = new Font("Segoe UI", 12); box.Text = initial;
        box.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.Escape) return;
            e.SuppressKeyPress = true; e.Handled = true;
            if (HotkeyChord.TryParse(HotkeyChord.FromKey(e), out var chord)) box.Text = chord.ToString();
        };
    }
    static string ChoiceCode(ComboBox selector, string initial) => (selector.SelectedItem as ServiceChoice)?.Code ?? initial;
    static string ProfileCode(string provider, string id) => provider == "AI" && id.Length > 0 ? "ai:" + id : provider;
    static string ProfileId(string code) => code.StartsWith("ai:") ? code[3..] : "";
    void RefreshServices()
    {
        var profiles = aiSettings.Snapshot();
        void Fill(ComboBox selector, bool input, bool word, string initial)
        {
            var code = ChoiceCode(selector, initial);
            selector.Items.Clear(); selector.Items.AddRange(TranslationServices.Available(input, word, DraftLanguage, profiles));
            selector.SelectedItem = selector.Items.Cast<ServiceChoice>().FirstOrDefault(c => c.Code == code) ?? selector.Items.Cast<ServiceChoice>().First(c => c.Code == "AI");
        }
        Fill(readProvider, false, true, ProfileCode(previous.ReadProvider, previous.ReadWordAiProfileId));
        Fill(sentenceProvider, false, false, ProfileCode(previous.ReadSentenceProvider, previous.ReadSentenceAiProfileId));
        Fill(inputProvider, true, false, ProfileCode(TranslationServices.Resolve(true, previous), previous.InputAiProfileId));
    }
    void RefreshText()
    {
        Text = UiText.T("appShort", DraftLanguage) + " · " + UiText.T("settings", DraftLanguage);
        foreach (var entry in textKeys) entry.Key.Text = UiText.T(entry.Value, DraftLanguage);
        navigation.AccessibleName = UiText.T("settings", DraftLanguage);
        for (int i = 0; i < sectionKeys.Length; i++) navigation.Items[i] = UiText.T(sectionKeys[i], DraftLanguage);
        ShowSection();
        int sourceIndex = inputSource.SelectedIndex;
        inputSource.Items[0] = new AppLanguage("auto", UiText.T("sourceGuess", DraftLanguage), "", ""); inputSource.SelectedIndex = sourceIndex;
        var category = (serviceCategory.SelectedItem as ServiceChoice)?.Code ?? "AI";
        serviceCategory.Items.Clear(); serviceCategory.Items.AddRange(TranslationServices.Configurable(DraftLanguage));
        serviceCategory.SelectedItem = serviceCategory.Items.Cast<ServiceChoice>().First(c => c.Code == category);
        aiSettings.SetLanguage(DraftLanguage); RefreshServices(); RefreshPreviews(); UpdateServiceEditor();
        dictionaries.SetLanguage(DraftLanguage);
        updates.SetLanguage(DraftLanguage); donation.SetLanguage(DraftLanguage);
    }
    void RefreshPreviews()
    {
        if (readLanguage.SelectedItem is not AppLanguage read || inputLanguage.SelectedItem is not AppLanguage input) return;
        reading.TargetCode = read.Code; writing.TargetCode = input.Code;
        writing.SourceCode = (inputSource.SelectedItem as AppLanguage)?.Code ?? "auto";
        reading.UiLanguage = writing.UiLanguage = DraftLanguage; reading.Invalidate(); writing.Invalidate();
    }
    void Save()
    {
        try
        {
            RefreshServices();
            var wordService = ChoiceCode(readProvider, "Offline"); var sentenceService = ChoiceCode(sentenceProvider, "AI"); var inputService = ChoiceCode(inputProvider, "AI");
            var ai = aiSettings.Export(false);
            var candidate = previous with { AutoRead = UserPreferences.AutoRead, InterfaceLanguage = DraftLanguage, ReadTarget = ((AppLanguage)readLanguage.SelectedItem!).Code,
                InputTarget = ((AppLanguage)inputLanguage.SelectedItem!).Code, HotkeysEnabled = readEnabled.Checked || inputEnabled.Checked,
                ReadHotkeyEnabled = readEnabled.Checked, InputHotkeyEnabled = inputEnabled.Checked, ReadHotkey = readKey.Text, InputHotkey = inputKey.Text,
                ReadProvider = wordService == "Offline" ? "Offline" : "AI", InputProvider = inputService.StartsWith("ai:") ? "AI" : inputService,
                ReadSentenceProvider = sentenceService.StartsWith("ai:") ? "AI" : sentenceService,
                InputSourceLanguage = ((AppLanguage)inputSource.SelectedItem!).Code,
                ReadWordAiProfileId = ProfileId(wordService), ReadSentenceAiProfileId = ProfileId(sentenceService), InputAiProfileId = ProfileId(inputService),
                AiProfiles = ai.Profiles, DefaultAiProfileId = ai.DefaultId, AzureRegion = region.Text.Trim(), MyMemoryEmail = MyMemoryContact.Validate(memoryEmail.Text) };
            if (!string.IsNullOrWhiteSpace(apiKey.Text))
            {
                if (apiKey.Text.Trim().Any(char.IsWhiteSpace)) throw new Exception(UiText.T("apiKey", DraftLanguage) + ": invalid format.");
                candidate = candidate with { ProtectedAzureKey = SecretStorage.Protect(apiKey.Text.Trim()) };
            }
            if ((candidate.InputProvider == "Azure" || candidate.ReadSentenceProvider == "Azure") && string.IsNullOrWhiteSpace(candidate.ProtectedAzureKey)) throw new Exception(UiText.T("requiredKey", DraftLanguage));
            if (!hotkeys.TryApply(candidate, out var error)) { feedback.Text = error; return; }
            if (!UserPreferences.TrySave(candidate)) { hotkeys.TryApply(previous, out _); hotkeys.Suspend(); feedback.Text = UiText.T("saveFailed", DraftLanguage); return; }
            Close();
        }
        catch (Exception ex) { feedback.Text = ex.Message; }
    }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); PopupChrome.Apply(Handle); }
}
