namespace EnglishHelper;

sealed partial class ResultWindow : Form
{
    readonly RichTextBox source = new(), output = new();
    readonly Label status = new(), provider = new(), outputCaption = new();
    readonly Button translate = new RoundedButton(), replace = new RoundedButton(), copy = new RoundedButton(), direction = new RoundedButton();
    readonly IntPtr target;
    readonly string original;
    readonly bool writingMode;
    bool readingWord;
    string sourceCode = "auto", targetCode, detectedSource;
    readonly ISpeechOutput speech;
    readonly bool compactWord, allowAutoRead;
    readonly AutoReadSwitch autoRead = new();
    readonly Label wordPhonetic = new();
    readonly ToolTip hints = new();
    bool wordReady, automaticReadStarted;
    Rectangle? nearbyAnchor;
    bool working;
    bool internalFocusTransfer, dismissalReady, closingPopup;
    PopupDismissal? dismissal;
    ContextMenuStrip? languageMenu;
    readonly CancellationTokenSource translationCancellation = new();
    bool ownedResourcesDisposed;
    internal CancellationToken TranslationToken => translationCancellation.Token;
    internal string ResultText => output.Text;
    internal string FeedbackText => status.Text;
    internal ContextMenuStrip? CurrentLanguageMenu => languageMenu;
    internal bool IsCompactWordView => compactWord;
    internal string PhoneticText => wordPhonetic.Text;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal bool AutoReadEnabled { get => autoRead.Checked; set => autoRead.Checked = value; }

    public ResultWindow(string text, IntPtr targetWindow, bool writing, ISpeechOutput? speechOutput = null, bool allowAutoRead = true)
    {
        speech = speechOutput ?? new EnglishSpeech(); this.allowAutoRead = allowAutoRead;
        target = targetWindow; original = text; writingMode = writing;
        targetCode = writing ? UserPreferences.Current.InputTarget : UserPreferences.Current.ReadTarget;
        if (writing) sourceCode = UserPreferences.Current.InputSourceLanguage;
        selectedProvider = TranslationServices.Resolve(writing, text: text);
        readingWord = LocalDictionary.IsWord(text);
        detectedSource = Languages.Guess(text);
        compactWord = !writing && targetCode == "zh-CN" && LocalDictionary.IsWord(text) && LocalDictionary.Contains(text);
        Text = UiText.T("app"); ClientSize = new Size(448, 470); FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen; ShowInTaskbar = false; TopMost = true;
        Font = new Font("Microsoft YaHei UI", 10); BackColor = Color.White;
        AutoScaleMode = AutoScaleMode.Dpi; DoubleBuffered = true;
        KeyPreview = true; KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { e.Handled = true; Close(); } };
        if (compactWord) { BuildWordView(text); return; }
        BuildSentenceView(text);
    }
    void BuildWordView(string text)
    {
        ClientSize = new Size(360, 300); BackColor = Color.White; source.Text = text;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18, 9, 18, 10), RowCount = 6, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 47));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, AiRowHeight));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        var header = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        var word = new Label { Text = LocalDictionary.Normalize(text), Font = new Font("Segoe UI", 21), ForeColor = Color.FromArgb(29, 43, 63), Location = new Point(0, 0), Height = 43, AutoEllipsis = true };
        var close = new RoundedButton { Text = "×", Font = new Font("Segoe UI", 18), BackColor = Color.White, ForeColor = Color.FromArgb(160, 167, 181), Size = new Size(29, 31), AccessibleName = "关闭查词" };
        header.Controls.AddRange([word, close]);
        header.Resize += (_, _) => { word.Width = Math.Max(1, header.Width - 38); close.Location = new Point(header.Width - 29, 4); };
        header.Paint += (_, e) => { using var line = new Pen(Color.FromArgb(232, 235, 241)); e.Graphics.DrawLine(line, 0, header.Height - 1, header.Width, header.Height - 1); };
        close.Click += (_, _) => Close();
        foreach (Control grip in new Control[] { header, word })
        {
            Point origin = Point.Empty, window = Point.Empty;
            grip.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { origin = MousePosition; window = Location; } };
            grip.MouseMove += (_, e) => { if (e.Button == MouseButtons.Left) Location = new Point(window.X + MousePosition.X - origin.X, window.Y + MousePosition.Y - origin.Y); };
        }
        layout.Controls.Add(header, 0, 0);
        var pronunciation = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        wordPhonetic.Text = "…"; wordPhonetic.AutoSize = true; wordPhonetic.Font = new Font("Segoe UI", 10);
        wordPhonetic.ForeColor = Color.FromArgb(122, 146, 178); wordPhonetic.Margin = new Padding(0, 7, 5, 0);
        var speaker = new SpeakerButton { Margin = new Padding(0, 1, 0, 0) }; speaker.Click += (_, _) => Speak();
        hints.SetToolTip(speaker, UiText.T("pronounce")); pronunciation.Controls.AddRange([wordPhonetic, speaker]); layout.Controls.Add(pronunciation, 0, 1);
        output.BorderStyle = BorderStyle.None; output.BackColor = Color.White; output.ForeColor = Color.FromArgb(38, 65, 98);
        output.Font = new Font(Font.FontFamily, 11); output.Dock = DockStyle.Fill; output.Margin = new Padding(0, 6, 0, 4);
        output.ReadOnly = true; output.DetectUrls = false; output.ScrollBars = RichTextBoxScrollBars.Vertical; output.AccessibleName = "单词释义";
        layout.Controls.Add(output, 0, 2);
        status.AutoSize = true; status.MaximumSize = new Size(322, 0); status.Font = new Font(Font.FontFamily, 8);
        status.ForeColor = Color.FromArgb(140, 117, 117); status.Margin = new Padding(0, 2, 0, 3); status.Visible = false; layout.Controls.Add(status, 0, 3);
        var footer = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        ConfigureAutoRead(); autoRead.Location = new Point(0, 2);
        provider.Text = "离线词典"; provider.AutoSize = true; provider.Font = new Font(Font.FontFamily, 8); provider.ForeColor = Color.FromArgb(180, 188, 202);
        hints.SetToolTip(provider, "词义与音标来自 ECDICT，本地离线查询。");
        footer.Controls.AddRange([autoRead, provider]); footer.Resize += (_, _) => provider.Location = new Point(footer.Width - provider.PreferredWidth, 8);
        layout.Controls.Add(BuildAiChoices(), 0, 4); layout.Controls.Add(footer, 0, 5); Controls.Add(layout);
    }
    void ReportStatus(string message)
    {
        status.Text = message;
        status.Visible = !string.IsNullOrWhiteSpace(message);
    }
    void FillWordView()
    {
        var entry = LocalDictionary.GetEntry(source.Text);
        wordPhonetic.Text = string.IsNullOrWhiteSpace(entry.Phonetic) ? UiText.T("noPhonetic") : "/" + entry.Phonetic + "/";
        output.Text = entry.Translation.Replace("\\n", Environment.NewLine);
        provider.Text = UiText.T("offline");
        wordReady = true; ReportStatus("");
        FitWordView();
        if (nearbyAnchor is Rectangle anchor) PlaceNear(anchor);
        if (autoRead.Checked && allowAutoRead && !automaticReadStarted)
        {
            automaticReadStarted = true; Speak();
        }
    }
    void FitWordView()
    {
        int measured = TextRenderer.MeasureText(output.Text, output.Font, new Size(ClientSize.Width - 42, 1000), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height;
        ClientSize = new Size(ClientSize.Width, Math.Clamp(measured + 146, 240, 440) + AiRowHeight);
        if (provider.Parent != null) { provider.Location = new Point(provider.Parent.Width - provider.PreferredWidth, 8); provider.Visible = provider.Left > autoRead.Right + 7; }
    }
    void StyleButton(Button button, int width, bool primary)
    {
        button.Size = new Size(width, 33); button.Margin = new Padding(0, 0, 8, 0);
        button.BackColor = primary ? Color.FromArgb(86, 103, 219) : Color.FromArgb(232, 237, 250);
        button.ForeColor = primary ? Color.White : Color.FromArgb(84, 98, 148); button.Font = new Font(Font.FontFamily, 9);
    }
    void UpdateDirection() => direction.Text = (sourceCode == "auto" ? selectedProvider is "Azure" or "AI" ? UiText.T("auto") : Languages.Get(detectedSource).Name + "?" : Languages.Get(sourceCode).Name) + "  →  " + Languages.Get(targetCode).Name;
    void OpenLanguageMenu()
    {
        languageMenu?.Dispose(); var menu = languageMenu = new ContextMenuStrip();
        var sourceItem = new ToolStripMenuItem(UiText.T("source")); var targetItem = new ToolStripMenuItem(UiText.T("target"));
        var automatic = new ToolStripMenuItem(UiText.T("auto")) { Checked = sourceCode == "auto" };
        automatic.Click += (_, _) => { sourceCode = "auto"; detectedSource = Languages.Guess(source.Text); UpdateDirection(); copy.Enabled = replace.Enabled = false; }; sourceItem.DropDownItems.Add(automatic);
        foreach (var language in Languages.All)
        {
            var sourceChoice = new ToolStripMenuItem(language.Name) { Checked = sourceCode == language.Code };
            sourceChoice.Click += (_, _) => { sourceCode = language.Code; detectedSource = sourceCode; UpdateDirection(); copy.Enabled = replace.Enabled = false; };
            sourceItem.DropDownItems.Add(sourceChoice);
            var targetChoice = new ToolStripMenuItem(language.Name) { Checked = targetCode == language.Code };
            targetChoice.Click += (_, _) => { targetCode = language.Code; UpdateDirection(); copy.Enabled = replace.Enabled = false; };
            targetItem.DropDownItems.Add(targetChoice);
        }
        menu.Items.AddRange([sourceItem, targetItem]); menu.Closed += (_, _) => { if (!IsDisposed) BeginInvoke(() => { menu.Dispose(); if (languageMenu == menu) languageMenu = null; }); };
        menu.Show(direction, new Point(0, direction.Height));
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e); PopupChrome.Apply(Handle);
    }
    protected override void OnShown(EventArgs e)
    {
        UiText.Apply(this);
        if (compactWord && provider.Parent != null)
        {
            provider.Location = new Point(provider.Parent.ClientSize.Width - provider.PreferredWidth, 8);
            provider.Visible = provider.Left > autoRead.Right + 7;
        }
        base.OnShown(e);
        dismissal ??= new PopupDismissal(this, RequestDismissal);
        BeginInvoke(() => dismissalReady = true);
    }
    protected override void OnDeactivate(EventArgs e) { base.OnDeactivate(e); RequestDismissal(); }
    protected override void OnFormClosing(FormClosingEventArgs e) { closingPopup = true; base.OnFormClosing(e); }
    void RequestDismissal()
    {
        if (!dismissalReady || internalFocusTransfer || closingPopup || IsDisposed || Disposing || !Visible || languageMenu?.Visible == true || aiMenu?.Visible == true) return;
        closingPopup = true; Close();
    }
    public void PlaceNear(Rectangle anchor)
    {
        nearbyAnchor = anchor;
        StartPosition = FormStartPosition.Manual;
        Location = ContextAnchor.PopupLocation(anchor, Size, Screen.FromRectangle(anchor).WorkingArea);
    }
    public void SetResult(string translation, string message)
    {
        if (compactWord) { output.Text = translation; ReportStatus(message); return; }
        ApplySentenceResult(translation, message);
    }
    public async Task Translate()
    {
        if (working || IsDisposed) return;
        if (string.IsNullOrWhiteSpace(source.Text)) { status.Text = "请先输入文字。"; return; }
        if (!writingMode && readingWord != LocalDictionary.IsWord(source.Text))
        {
            readingWord = LocalDictionary.IsWord(source.Text); selectedProvider = TranslationServices.Resolve(false, text: source.Text);
            selectedAi = selectedProvider == "AI" ? TranslationServices.Profile(false, source.Text) : null; UpdateAiChoice();
        }
        if (compactWord && selectedAi == null && selectedProvider is "MyMemory" or "Offline")
        {
            try { FillWordView(); }
            catch (Exception ex) { output.Text = UiText.ErrorMessage(ex); wordPhonetic.Text = UiText.T("noPhonetic"); wordReady = false; }
            return;
        }
        working = true; translate.Enabled = replace.Enabled = copy.Enabled = false; source.Enabled = direction.Enabled = false; output.Clear(); ReportStatus("正在翻译…");
        if (aiChoice != null) aiChoice.Enabled = false;
        if (aiRetry != null) aiRetry.Enabled = false;
        try
        {
            var translation = await MultilingualTranslator.Translate(source.Text, targetCode, sourceCode, translationCancellation.Token, selectedAi, writing: writingMode, providerOverride: selectedProvider);
            if (IsDisposed) return;
            detectedSource = translation.Source;
            if (compactWord)
            {
                var entry = LocalDictionary.GetEntry(source.Text);
                wordPhonetic.Text = string.IsNullOrWhiteSpace(entry.Phonetic) ? UiText.T("noPhonetic") : "/" + entry.Phonetic + "/";
                output.Text = translation.Text; provider.Text = translation.Service; wordReady = true; ReportStatus("");
                hints.SetToolTip(provider, translation.Service); FitWordView();
                if (autoRead.Checked && allowAutoRead) Speak();
                return;
            }
            bool dictionary = translation.Service == "ECDICT";
            ApplySentenceResult(dictionary ? translation.Text.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine) : translation.Text);
            output.ReadOnly = dictionary; replace.Visible = target != IntPtr.Zero && !dictionary; replace.Enabled = target != IntPtr.Zero && !dictionary;
            outputCaption.Text = UiText.T("translation"); UpdateDirection();
        }
        catch (Exception ex) { if (!IsDisposed) ReportStatus(UiText.ErrorMessage(ex)); }
        finally { working = false; if (!IsDisposed) { translate.Enabled = source.Enabled = direction.Enabled = true; if (aiChoice != null) aiChoice.Enabled = true; if (aiRetry != null) aiRetry.Enabled = true; } }
    }
    async Task Replace()
    {
        if (working || string.IsNullOrWhiteSpace(output.Text)) return;
        working = true; replace.Enabled = false; internalFocusTransfer = true;
        try
        {
            Hide();
            if (!Native.SetForegroundWindow(target)) throw new Exception("无法返回原窗口，请重新选中文字并查询。");
            await Task.Delay(180);
            if (Native.GetForegroundWindow() != target) throw new Exception("原窗口没有获得焦点，请重新选中文字。");
            var current = await Native.Selection();
            if (current != original) throw new Exception("原来的选区已改变，请重新选中文字并翻译。");
            if (Native.GetForegroundWindow() != target) throw new Exception("当前窗口已改变，已取消替换。");
            await Native.Paste(output.Text); Close();
        }
        catch (Exception ex) { Show(); ReportStatus(ex.Message); }
        finally { working = false; internalFocusTransfer = false; if (!IsDisposed) replace.Enabled = true; }
    }
    void Speak()
    {
        var text = compactWord ? LocalDictionary.Normalize(source.Text) : writingMode ? output.Text : source.Text;
        if (string.IsNullOrWhiteSpace(text)) { ReportStatus("请先获取英文内容。"); return; }
        try
        {
            if (speech is ILanguageSpeechOutput multilingual) multilingual.Speak(text, compactWord ? "en" : writingMode ? targetCode : detectedSource);
            else speech.Speak(text);
            ReportStatus("");
        }
        catch (Exception ex) { ReportStatus(UiText.ErrorMessage(ex)); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !ownedResourcesDisposed)
        {
            ownedResourcesDisposed = true; translationCancellation.Cancel(); translationCancellation.Dispose();
            dismissal?.Dispose();
            languageMenu?.Dispose();
            aiMenu?.Dispose();
            try { speech?.Dispose(); } catch { }
            hints.Dispose();
            if (compactWord) { source.Dispose(); translate.Dispose(); replace.Dispose(); copy.Dispose(); direction.Dispose(); }
        }
        base.Dispose(disposing);
    }
}
