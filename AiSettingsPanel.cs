namespace EnglishHelper;

sealed class AiSettingsPanel : Panel
{
    readonly List<AiProfile> profiles;
    readonly ComboBox saved = new() { DropDownStyle = ComboBoxStyle.DropDownList }, preset = new() { DropDownStyle = ComboBoxStyle.DropDownList }, model = new(), protocol = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly TextBox name = new(), address = new(), key = new() { UseSystemPasswordChar = true };
    readonly Label feedback = new();
    readonly CheckBox defaultChoice = new();
    readonly Dictionary<Control, (string Zh, string En)> captions = new();
    readonly CancellationTokenSource lifetime = new();
    readonly Button add, remove, fetch, test;
    int current = -1;
    bool refreshing;
    string interfaceLanguage = "en";
    string defaultId;
    public AiSettingsPanel(AppPreferences preferences)
    {
        profiles = [.. preferences.AiProfiles ?? []]; defaultId = preferences.DefaultAiProfileId;
        AutoScroll = true;
        AddLabel("已保存的配置", "Saved configurations", 0);
        Put(saved, 160, 0, 360); saved.Name = "AiProfiles";
        add = AddButton("添加", "Add", 530, 0, 60); remove = AddButton("删除", "Delete", 595, 0, 60);
        add.Name = "AiAdd"; remove.Name = "AiRemove"; preset.Name = "AiPreset"; defaultChoice.Name = "AiDefault";
        AddLabel("服务预设", "Provider preset", 44); Put(preset, 160, 44, 330); preset.Items.AddRange(AiPreset.All);
        protocol.Items.AddRange(["Chat", "Claude Messages"]); protocol.Name = "AiProtocol"; protocol.AccessibleName = "API protocol"; protocol.DropDownWidth = 220; Put(protocol, 500, 44, 155);
        AddLabel("配置名称", "Configuration name", 88); Put(name, 160, 88, 495); name.Name = "AiName";
        AddLabel("接口地址", "Base URL", 132); Put(address, 160, 132, 495); address.Name = "AiBaseUrl";
        AddLabel("API Key", "API Key", 176); Put(key, 160, 176, 495); key.Name = "AiApiKey";
        key.PlaceholderText = "留空保留已保存密钥 / Leave blank to keep saved key";
        AddLabel("模型名称", "Model", 220); Put(model, 160, 220, 350); model.Name = "AiModel";
        fetch = AddButton("获取模型", "Load models", 520, 220, 135);
        captions[defaultChoice] = ("设为默认 AI 配置", "Use as default AI configuration"); Put(defaultChoice, 160, 264, 350);
        test = AddButton("测试翻译", "Test translation", 520, 264, 135);
        var hint = AddLabel("选择服务 → 填写密钥 → 获取或填写模型 → 保存。\n测试翻译会发送 Hello，可能产生少量费用。密钥仅在本机加密保存。\n自定义支持 Chat Completions 兼容接口，本机服务可不填密钥。", "Choose provider → enter key → load or enter model → save.\nTest sends Hello and may incur a small charge. Keys are encrypted locally.\nCustom endpoints must support Chat Completions; local keys are optional.", 310);
        hint.Size = new Size(655, 78);
        Put(feedback, 0, 395, 655); feedback.Height = 50; feedback.ForeColor = Color.FromArgb(137, 89, 89);
        saved.SelectedIndexChanged += (_, _) =>
        {
            if (refreshing) return;
            StoreDraft(); current = saved.SelectedIndex; LoadDraft();
        };
        address.TextChanged += (_, _) =>
        {
            if (refreshing || current < 0 || address.Text == profiles[current].BaseUrl) return;
            // A saved credential must never follow a profile to a different endpoint.
            profiles[current] = profiles[current] with { ProtectedKey = "" }; key.Clear();
            key.PlaceholderText = UiText.T("aiKey", interfaceLanguage);
        };
        preset.SelectedIndexChanged += (_, _) =>
        {
            if (refreshing || preset.SelectedItem is not AiPreset item) return;
            if (address.Text != item.Url) { address.Text = item.Url; key.Clear(); model.Items.Clear(); model.Text = ""; }
            protocol.SelectedIndex = item.Protocol == "Anthropic" ? 1 : 0;
            if (!string.IsNullOrEmpty(item.Url)) name.Text = item.Name;
            feedback.Text = item.Note;
        };
        add.Click += (_, _) =>
        {
            StoreDraft(); var p = new AiProfile(Guid.NewGuid().ToString("N"), "New AI", AiPreset.All[0].Url, "");
            profiles.Add(p); Refill(profiles.Count - 1);
        };
        remove.Click += (_, _) =>
        {
            if (current < 0) return;
            if (profiles[current].Id == defaultId) defaultId = "";
            profiles.RemoveAt(current); Refill(profiles.Count - 1);
        };
        defaultChoice.CheckedChanged += (_, _) =>
        {
            if (refreshing || current < 0) return;
            if (defaultChoice.Checked) defaultId = profiles[current].Id;
            else if (defaultId == profiles[current].Id) defaultId = "";
        };
        fetch.Click += async (_, _) => await Run(async p =>
        {
            var models = await AiTranslation.Models(p, lifetime.Token);
            if (IsDisposed) return;
            var previous = model.Text; model.Items.Clear(); model.Items.AddRange(models);
            model.Text = previous.Length > 0 ? previous : models.FirstOrDefault() ?? "";
            feedback.Text = models.Length + " models. 可手动填写其他模型 / Manual entry is also supported.";
        });
        test.Click += async (_, _) => await Run(async p =>
        {
            var result = await AiTranslation.Translate("Hello", "en", "zh-CN", p, lifetime.Token);
            if (!IsDisposed) feedback.Text = "✓ " + result.Text;
        });
        Refill(profiles.FindIndex(p => p.Id == defaultId)); SetLanguage(preferences.InterfaceLanguage);
    }
    void Put(Control control, int x, int y, int width) { control.Bounds = new Rectangle(x, y, width, 30); Controls.Add(control); }
    Label AddLabel(string zh, string en, int y)
    {
        var label = new Label { ForeColor = Color.FromArgb(81, 96, 131) }; captions[label] = (zh, en); Put(label, 0, y + 4, 155); return label;
    }
    Button AddButton(string zh, string en, int x, int y, int width)
    {
        var button = new Button { FlatStyle = FlatStyle.Flat, BackColor = Color.White }; captions[button] = (zh, en); Put(button, x, y, width); return button;
    }
    public void SetLanguage(string language)
    {
        interfaceLanguage = language;
        foreach (var item in captions)
        {
            string textKey = item.Value.En switch
            {
                "Saved configurations" => "aiSaved", "Provider preset" => "aiPreset", "Configuration name" => "aiName",
                "Base URL" => "aiAddress", "API Key" => "aiKey", "Model" => "aiModel", "Use as default AI configuration" => "aiDefault",
                "Load models" => "aiLoad", "Test translation" => "aiTest", "Add" => "aiAdd", "Delete" => "aiDelete", _ => "aiHint"
            };
            item.Key.Text = UiText.T(textKey, language);
        }
        key.PlaceholderText = UiText.T(current >= 0 && profiles[current].ProtectedKey.Length > 0 ? "keySaved" : "aiKey", language);
    }
    void StoreDraft()
    {
        if (current < 0 || current >= profiles.Count) return;
        var old = profiles[current];
        profiles[current] = old with { Name = name.Text.Trim(), BaseUrl = address.Text.Trim(), Model = model.Text.Trim(),
            ProtectedKey = key.Text.Length == 0 ? old.ProtectedKey : SecretStorage.Protect(key.Text.Trim()), Protocol = protocol.SelectedIndex == 1 ? "Anthropic" : "ChatCompletions" };
        refreshing = true; saved.Items[current] = profiles[current]; refreshing = false;
    }
    void Refill(int index)
    {
        refreshing = true; saved.Items.Clear(); saved.Items.AddRange(profiles.Cast<object>().ToArray());
        current = profiles.Count == 0 ? -1 : Math.Clamp(index, 0, profiles.Count - 1); saved.SelectedIndex = current;
        refreshing = false; LoadDraft();
    }
    void LoadDraft()
    {
        refreshing = true;
        var p = current >= 0 ? profiles[current] : null;
        name.Text = p?.Name ?? ""; address.Text = p?.BaseUrl ?? ""; model.Items.Clear(); model.Text = p?.Model ?? ""; key.Clear();
        preset.SelectedItem = AiPreset.All.FirstOrDefault(x => x.Url == p?.BaseUrl) ?? AiPreset.All[^1];
        protocol.SelectedIndex = p?.Protocol == "Anthropic" ? 1 : 0;
        defaultChoice.Checked = p != null && p.Id == defaultId;
        foreach (var control in new Control[] { name, address, key, model, preset, protocol, defaultChoice, remove, fetch, test }) control.Enabled = p != null;
        feedback.Text = (preset.SelectedItem as AiPreset)?.Note ?? ""; refreshing = false;
        SetLanguage(interfaceLanguage);
    }
    public (AiProfile[] Profiles, string DefaultId) Export(bool required)
    {
        StoreDraft();
        foreach (var p in profiles)
        {
            if (string.IsNullOrWhiteSpace(p.Name)) throw new Exception("请填写配置名称 / Configuration name is required.");
            AiTranslation.Validate(p);
        }
        if (required && !profiles.Any(p => p.Id == defaultId)) throw new Exception("请添加 AI 配置并勾选设为默认 / Select a default AI configuration.");
        return (profiles.ToArray(), defaultId);
    }
    public AiProfile[] Snapshot() { StoreDraft(); return profiles.ToArray(); }
    async Task Run(Func<AiProfile, Task> action)
    {
        try
        {
            StoreDraft(); if (current < 0) return;
            Enabled = false; feedback.Text = "连接中… / Connecting…";
            await action(profiles[current]);
        }
        catch (Exception ex) { if (!IsDisposed) feedback.Text = ex is OperationCanceledException ? "请求已取消或超时 / Cancelled or timed out." : ex is System.Net.Http.HttpRequestException ? "连接失败，请检查网络和地址 / Connection failed." : ex.Message; }
        finally { if (!IsDisposed) Enabled = true; }
    }
    protected override void Dispose(bool disposing) { if (disposing) { lifetime.Cancel(); lifetime.Dispose(); } base.Dispose(disposing); }
}
