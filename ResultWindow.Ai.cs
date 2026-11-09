namespace EnglishHelper;

sealed partial class ResultWindow
{
    AiProfile? selectedAi;
    string selectedProvider = "MyMemory";
    ContextMenuStrip? aiMenu;
    Button? aiChoice, aiRetry;
    internal ContextMenuStrip? CurrentAiMenu => aiMenu;
    const int AiRowHeight = 38;
    Control BuildAiChoices()
    {
        var row = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        selectedProvider = TranslationServices.Resolve(writingMode, text: source.Text);
        selectedAi = selectedProvider == "AI" ? TranslationServices.Profile(writingMode, source.Text) : null;
        aiChoice = new RoundedButton { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 252), ForeColor = Color.FromArgb(95, 111, 151), Font = new Font(Font.FontFamily, 8), Name = "ResultAiChoice" };
        aiRetry = new RoundedButton { Dock = DockStyle.Right, Width = 65, Text = UiText.T("retranslate"), BackColor = Color.White, ForeColor = Color.FromArgb(86, 103, 219), Name = "ResultAiRetry" };
        hints.SetToolTip(aiChoice, "切换服务或模型；只影响本次结果 / Change service or model for this result");
        aiChoice.Click += (_, _) => OpenAiMenu(); aiRetry.Click += async (_, _) => await Translate();
        row.Controls.AddRange([aiChoice, aiRetry]); UpdateAiChoice(); return row;
    }
    void UpdateAiChoice()
    {
        if (aiChoice != null) aiChoice.Text = (selectedAi?.ToString() ?? TranslationServices.Name(selectedProvider)) + "  ▾";
    }
    void OpenAiMenu()
    {
        if (working || aiChoice == null) return;
        aiMenu?.Dispose(); var menu = aiMenu = new ContextMenuStrip { ShowCheckMargin = true };
        foreach (var service in TranslationServices.Available(writingMode, LocalDictionary.IsWord(source.Text), UserPreferences.Current.InterfaceLanguage, []).Where(c => !c.Code.StartsWith("ai:")))
        {
            var item = new ToolStripMenuItem(service.Name) { Checked = selectedAi == null && selectedProvider == service.Code };
            item.Click += (_, _) => { selectedAi = service.Code == "AI" ? AiTranslation.Default : null; selectedProvider = service.Code; UpdateAiChoice(); }; menu.Items.Add(item);
        }
        foreach (var profile in UserPreferences.Current.AiProfiles ?? [])
        {
            var item = new ToolStripMenuItem(profile.ToString()) { Checked = selectedAi?.Id == profile.Id };
            item.Click += (_, _) => { selectedAi = profile; selectedProvider = "AI"; UpdateAiChoice(); };
            menu.Items.Add(item);
        }
        menu.Items.Add(new ToolStripSeparator());
        var settings = new ToolStripMenuItem(UiText.T("settings") + "…");
        settings.Click += (_, _) => { Close(); Application.OpenForms.OfType<FloatingBar>().FirstOrDefault()?.OpenSettings(2); };
        menu.Items.Add(settings);
        if (selectedAi != null)
        {
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem(UiText.T("aiModel")) { Enabled = false });
            var entry = new ToolStripTextBox { Text = selectedAi.Model, AutoSize = false, Width = 270 };
            menu.Items.Add(entry);
            void ApplyModel() { if (!string.IsNullOrWhiteSpace(entry.Text)) { selectedAi = selectedAi with { Model = entry.Text.Trim() }; UpdateAiChoice(); menu.Close(); } }
            entry.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ApplyModel(); } };
            var apply = new ToolStripMenuItem(UiText.T("aiApplyModel")) { Name = "ApplyModel" }; apply.Click += (_, _) => ApplyModel(); menu.Items.Add(apply);
            var load = new ToolStripMenuItem(UiText.T("aiLoad")); menu.Items.Add(load);
            load.DropDownItems.Add(new ToolStripMenuItem("Loading…") { Enabled = false });
            // Keep this menu open while the asynchronous list request runs.
            load.DropDownOpening += async (_, _) =>
            {
                if (load.Tag != null || selectedAi == null) return;
                load.Tag = true;
                try
                {
                    var models = await AiTranslation.Models(selectedAi, translationCancellation.Token);
                    if (IsDisposed || menu.IsDisposed) return;
                    load.DropDownItems.Clear();
                    foreach (var modelName in models)
                    {
                        var item = new ToolStripMenuItem(modelName) { Checked = selectedAi.Model == modelName };
                        item.Click += (_, _) => { selectedAi = selectedAi with { Model = modelName }; UpdateAiChoice(); }; load.DropDownItems.Add(item);
                    }
                    load.Text = models.Length + " models ▸";
                    if (models.Length == 0) load.DropDownItems.Add(new ToolStripMenuItem("请手动填写 / Enter manually") { Enabled = false });
                }
                catch (Exception) { if (!IsDisposed && !menu.IsDisposed) { load.DropDownItems.Clear(); load.DropDownItems.Add(new ToolStripMenuItem("请手动填写 / Enter manually") { Enabled = false }); ReportStatus("无法获取模型列表，请手动填写 / Enter model manually."); } }
            };
        }
        menu.Closed += (_, _) => { if (!IsDisposed) BeginInvoke(() => { menu.Dispose(); if (aiMenu == menu) aiMenu = null; }); };
        menu.Show(aiChoice, new Point(0, aiChoice.Height));
    }
}
