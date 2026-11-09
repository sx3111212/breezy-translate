namespace EnglishHelper;

sealed partial class ResultWindow
{
    TableLayoutPanel? sentenceLayout;
    string? translatedSource;
    void BuildSentenceView(string text)
    {
        ClientSize = new Size(420, 340); BackColor = Color.White;
        var layout = sentenceLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18, 10, 18, 10), ColumnCount = 1, RowCount = 7 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, AiRowHeight));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var header = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        StyleButton(direction, 295, false); direction.BackColor = Color.White; direction.Font = new Font(Font.FontFamily, 10);
        direction.Location = new Point(-8, 0); UpdateDirection();
        direction.Click += (_, _) => { if (!working) OpenLanguageMenu(); };
        var close = new RoundedButton { Text = "×", Font = new Font("Segoe UI", 18), BackColor = Color.White, ForeColor = Color.FromArgb(160, 167, 181), Size = new Size(29, 31), AccessibleName = "关闭翻译" };
        close.Click += (_, _) => Close(); header.Controls.AddRange([direction, close]);
        header.Resize += (_, _) => close.Location = new Point(header.Width - 29, 0);
        AttachDrag(header); layout.Controls.Add(header, 0, 0);

        ConfigureSentenceText(source, 12); source.Text = text; source.MaxLength = 3000; source.AccessibleName = "原句";
        source.Margin = new Padding(0, 2, 0, 7); layout.Controls.Add(source, 0, 1);
        source.TextChanged += (_, _) =>
        {
            if (!working && source.Text.Replace("\r\n", "\n") != translatedSource)
            { copy.Enabled = replace.Enabled = false; wordReady = false; }
        };

        var tools = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        tools.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(232, 235, 241)); e.Graphics.DrawLine(pen, 0, 0, tools.Width, 0); };
        var speaker = new SpeakerButton { Location = new Point(0, 3) }; speaker.Click += (_, _) => Speak(); hints.SetToolTip(speaker, UiText.T("pronounce"));
        outputCaption.Text = "译文"; outputCaption.AutoSize = true; outputCaption.Font = new Font(Font.FontFamily, 8);
        outputCaption.ForeColor = Color.FromArgb(150, 160, 180); outputCaption.Location = new Point(37, 8);
        StyleButton(translate, 58, false); translate.Text = "翻译"; translate.BackColor = Color.White; translate.AccessibleName = "翻译句子";
        translate.Click += async (_, _) => await Translate(); tools.Controls.AddRange([speaker, outputCaption, translate]);
        tools.Resize += (_, _) => translate.Location = new Point(tools.Width - 58, 1); layout.Controls.Add(tools, 0, 2);

        ConfigureSentenceText(output, 11); output.AccessibleName = "译文"; output.Margin = new Padding(0, 5, 0, 5); layout.Controls.Add(output, 0, 3);
        status.AutoSize = true; status.MaximumSize = new Size(382, 0); status.Font = new Font(Font.FontFamily, 8);
        status.ForeColor = Color.FromArgb(140, 117, 117); status.Margin = new Padding(0, 2, 0, 3); status.Visible = false; layout.Controls.Add(status, 0, 4);
        var footer = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        ConfigureAutoRead(); autoRead.Location = new Point(0, 3);
        var actions = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Width = 200, Height = 34, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        StyleButton(replace, 83, false); replace.Text = "使用译文"; replace.Enabled = false; replace.Visible = target != IntPtr.Zero;
        replace.BackColor = Color.White; replace.Margin = Padding.Empty; replace.Click += async (_, _) => await Replace();
        StyleButton(copy, 52, false); copy.Text = "复制"; copy.Enabled = false; copy.BackColor = Color.White; copy.Margin = Padding.Empty;
        copy.Click += (_, _) => { try { if (!string.IsNullOrEmpty(output.Text)) Clipboard.SetText(output.Text); } catch { ReportStatus("剪贴板正忙，请重试。"); } };
        actions.Controls.AddRange([replace, copy]); footer.Controls.AddRange([autoRead, actions]); footer.Resize += (_, _) => actions.Location = new Point(footer.Width - actions.Width, 0);
        layout.Controls.Add(BuildAiChoices(), 0, 5); layout.Controls.Add(footer, 0, 6); Controls.Add(layout);
        hints.SetToolTip(output, "译文由所选翻译服务提供，请核对含义。");
        hints.SetToolTip(source, "在这里输入或编辑要翻译的句子。");
    }
    void ConfigureSentenceText(RichTextBox box, float size)
    {
        box.BorderStyle = BorderStyle.None; box.BackColor = Color.White; box.ForeColor = Color.FromArgb(38, 65, 98);
        box.Font = new Font(Font.FontFamily, size); box.Dock = DockStyle.Fill; box.Multiline = true;
        box.ScrollBars = RichTextBoxScrollBars.Vertical; box.DetectUrls = false;
    }
    void AttachDrag(Control control)
    {
        Point origin = Point.Empty, window = Point.Empty;
        control.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { origin = MousePosition; window = Location; } };
        control.MouseMove += (_, e) => { if (e.Button == MouseButtons.Left) Location = new Point(window.X + MousePosition.X - origin.X, window.Y + MousePosition.Y - origin.Y); };
    }
    void FitSentenceView()
    {
        if (sentenceLayout == null) return;
        int Measure(string value, Font font) => TextRenderer.MeasureText(value, font, new Size(ClientSize.Width - 40, 1000), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height;
        int sourceHeight = Math.Clamp(Measure(source.Text, source.Font) + 10, 55, 125);
        int outputHeight = Math.Clamp(Measure(output.Text, output.Font) + 15, 90, 230);
        sentenceLayout.RowStyles[1].Height = sourceHeight;
        ClientSize = new Size(ClientSize.Width, Math.Clamp(sourceHeight + outputHeight + 146, 300, 510) + AiRowHeight);
        if (nearbyAnchor is Rectangle anchor) PlaceNear(anchor);
    }
    void ConfigureAutoRead()
    {
        autoRead.Checked = UserPreferences.AutoRead; autoRead.Name = "AutoReadSwitch";
        autoRead.AccessibleName = compactWord ? "自动朗读单词" : "自动朗读英文";
        hints.SetToolTip(autoRead, UiText.T("autoReadHelp"));
        autoRead.CheckedChanged += (_, _) =>
        {
            bool saved = UserPreferences.SetAutoRead(autoRead.Checked);
            if (!autoRead.Checked) { try { speech.Stop(); } catch { } }
            else if (wordReady && allowAutoRead) { automaticReadStarted = true; Speak(); }
            if (!saved) ReportStatus("本次已生效，但无法保存朗读设置。");
        };
    }
    void ApplySentenceResult(string value, string message = "")
    {
        translatedSource = source.Text.Replace("\r\n", "\n");
        output.Text = value; copy.Enabled = true; replace.Visible = target != IntPtr.Zero; replace.Enabled = target != IntPtr.Zero;
        wordReady = !string.IsNullOrWhiteSpace(writingMode ? output.Text : source.Text); ReportStatus(message); FitSentenceView();
        if (autoRead.Checked && allowAutoRead && wordReady) { automaticReadStarted = true; Speak(); }
    }
}
