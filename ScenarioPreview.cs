using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace EnglishHelper;

sealed class ScenarioPreview : Control
{
    public readonly bool IsInput;
    public string TargetCode = "en", UiLanguage = "zh-CN", Shortcut = "Ctrl+Alt+D", SourceCode = "auto";
    readonly Bitmap bubble;
    readonly System.Windows.Forms.Timer timer = new() { Interval = 33 };
    readonly Stopwatch clock = new();
    readonly Button pause = new RoundedButton(), replay = new RoundedButton();
    double position;
    bool paused;
    const double Duration = 8.8;
    static readonly Color Ink = Color.FromArgb(61, 77, 115), Muted = Color.FromArgb(143, 155, 181), Accent = Color.FromArgb(94, 116, 222);

    public ScenarioPreview(bool input)
    {
        IsInput = input;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        using var stream = typeof(ScenarioPreview).Assembly.GetManifestResourceStream("EnglishHelper.Assets.translation-orb.png")!;
        using var image = new Bitmap(stream); bubble = new Bitmap(image);
        foreach (var button in new[] { pause, replay })
        {
            button.Size = new Size(28, 26); button.BackColor = Color.White; button.ForeColor = Muted;
            button.Font = new Font("Segoe UI", 11); Controls.Add(button);
        }
        pause.Text = "Ⅱ"; pause.Name = "PauseDemo"; replay.Text = "↺"; replay.Name = "ReplayDemo";
        pause.Click += (_, _) => { paused = !paused; UpdatePlayback(); Invalidate(); };
        replay.Click += (_, _) => { position = 0; paused = false; UpdatePlayback(); Invalidate(); };
        timer.Tick += (_, _) => { position = (position + clock.Elapsed.TotalSeconds) % Duration; clock.Restart(); Invalidate(); };
    }
    void UpdatePlayback()
    {
        pause.Text = paused ? "▶" : "Ⅱ";
        if (Visible && IsHandleCreated && !paused && !IsDisposed) { clock.Restart(); timer.Start(); }
        else { timer.Stop(); clock.Stop(); }
    }
    protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); UpdatePlayback(); }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); UpdatePlayback(); }
    protected override void OnHandleDestroyed(EventArgs e) { timer.Stop(); clock.Stop(); base.OnHandleDestroyed(e); }
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (pause != null) { pause.Location = new Point(Width - 76, 7); replay.Location = new Point(Width - 43, 7); }
    }
    internal void SeekPreview(double seconds) { paused = true; position = seconds % Duration; UpdatePlayback(); Invalidate(); }
    static float Progress(double value, double start, double end) => (float)Math.Clamp((value - start) / (end - start), 0, 1);
    static PointF Between(PointF a, PointF b, float progress)
    {
        progress = progress * progress * (3 - 2 * progress);
        return new(a.X + (b.X - a.X) * progress, a.Y + (b.Y - a.Y) * progress);
    }
    static void Card(Graphics g, Rectangle rectangle, Color fill, Color border, int radius = 10)
    {
        using var shape = RoundedButton.Rounded(rectangle, radius); using var brush = new SolidBrush(fill); using var pen = new Pen(border);
        g.FillPath(brush, shape); g.DrawPath(pen, shape);
    }
    static void DrawLabel(Graphics g, string text, Font font, Rectangle rectangle, Color color, TextFormatFlags align = TextFormatFlags.Left)
        => TextRenderer.DrawText(g, text, font, rectangle, color, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | align);
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics; g.Clear(Parent?.BackColor ?? Color.FromArgb(246, 248, 253));
        g.SmoothingMode = SmoothingMode.AntiAlias; g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        Card(g, new Rectangle(0, 0, Width - 1, Height - 1), Color.White, Color.FromArgb(221, 229, 244), 14);
        using var body = new Font("Microsoft YaHei UI", 13);
        using var small = new Font("Microsoft YaHei UI", 8);
        pause.AccessibleName = UiText.T(paused ? "playDemo" : "pauseDemo", UiLanguage); replay.AccessibleName = UiText.T("replayDemo", UiLanguage);
        if (IsInput) DrawInput(g, body, small); else DrawReading(g, body, small);
    }
    int DrawReading(Graphics g, Font body, Font small)
    {
        string word = TargetCode == "en" ? Languages.Get("ja").Hello : "Hello";
        int wordWidth = TextRenderer.MeasureText(word, body, Size.Empty, TextFormatFlags.NoPadding).Width;
        int rowTop = (Height - 58) / 2;
        var origin = new PointF(24, rowTop + 17); var end = new PointF(24 + wordWidth, rowTop + 17);
        var bubblePoint = new PointF(24 + wordWidth + 25, rowTop + 5);
        int popupX = Math.Max((int)bubblePoint.X + 42, Width / 2 - 10);
        var speakerPoint = new PointF(Width - 45, rowTop + 26);
        float selection = Progress(position, .8, 2.2);
        if (selection > 0)
        {
            using var fill = new SolidBrush(Color.FromArgb(224, 232, 255));
            g.FillRectangle(fill, 20, rowTop + 2, (wordWidth + 9) * selection, 34);
        }
        DrawLabel(g, word, body, new Rectangle(20, rowTop, wordWidth + 14, 36), Ink);
        if (position >= 2.2) g.DrawImage(bubble, bubblePoint.X - 17, bubblePoint.Y - 17, 34, 34);
        if (position >= 3.5)
        {
            Card(g, new Rectangle(popupX, rowTop - 10, Width - popupX - 19, 78), Color.FromArgb(247, 249, 255), Color.FromArgb(223, 231, 248));
            DrawLabel(g, word + "  →  " + Languages.Get(TargetCode).Name, small, new Rectangle(popupX + 10, rowTop - 6, Width - popupX - 56, 19), Muted);
            DrawLabel(g, Languages.Get(TargetCode).Hello, body, new Rectangle(popupX + 10, rowTop + 16, Width - popupX - 70, 32), Ink);
            DrawSpeaker(g, speakerPoint, position >= 5.5);
        }
        PointF cursor = position < 2.2 ? Between(origin, end, Progress(position, .8, 2.2))
            : position < 3.5 ? Between(end, bubblePoint, Progress(position, 2.2, 3.1))
            : Between(bubblePoint, speakerPoint, Progress(position, 4.2, 5.2));
        DrawCursor(g, cursor, position, 3.2, 5.3);
        return position < 2.2 ? 1 : position < 3.5 ? 2 : position < 5.3 ? 3 : 4;
    }
    int DrawInput(Graphics g, Font body, Font small)
    {
        string fromCode = SourceCode != "auto" ? SourceCode : UiLanguage == TargetCode ? TargetCode == "en" ? "zh-CN" : "en" : UiLanguage;
        string from = Languages.Get(fromCode).Example;
        string to = Languages.Get(TargetCode).Example;
        bool complete = position >= 4.7;
        var field = new Rectangle(18, (Height - 74) / 2, Width - 37, 74);
        Card(g, field, complete ? Color.FromArgb(246, 252, 250) : Color.FromArgb(251, 252, 255),
            complete ? Color.FromArgb(185, 223, 209) : Color.FromArgb(212, 222, 246), 8);
        var graphemes = StringInfo.ParseCombiningCharacters(from);
        int count = (int)(graphemes.Length * Progress(position, .7, 2.6));
        string typed = count == 0 ? "" : count == graphemes.Length ? from : from[..graphemes[count]];
        var content = new Rectangle(field.Left + 10, field.Top + 10, field.Width - 66, field.Height - 20);
        DrawLabel(g, complete ? to : typed, body, content, Ink);
        if (!complete && position < 2.7 && (int)(position * 3) % 2 == 0)
        {
            int x = Math.Min(content.Right - 2, content.Left + TextRenderer.MeasureText(typed, body, Size.Empty, TextFormatFlags.NoPadding).Width + 3);
            using var caret = new Pen(Accent); g.DrawLine(caret, x, content.Top + 3, x, content.Bottom - 3);
        }
        var button = new PointF(field.Right - 25, field.Top + 20);
        if (position > .5) g.DrawImage(bubble, button.X - 17, button.Y - 17, 34, 34);
        if (position >= 3.7 && !complete)
        {
            using var spinner = new Pen(Accent, 2); g.DrawArc(spinner, button.X - 18, button.Y - 18, 36, 36, (float)(position * 260 % 360), 245);
        }
        var start = new PointF(field.Left + 56, field.Top + 28);
        var cursor = position < 2.7 ? start : Between(start, button, Progress(position, 2.7, 3.5));
        DrawCursor(g, cursor, position, .25, 3.55);
        return position < 2.7 ? 1 : position < 3.7 ? 2 : position < 4.7 ? 3 : 4;
    }
    static void DrawCursor(Graphics g, PointF point, double time, params double[] clicks)
    {
        foreach (double click in clicks)
        {
            double age = time - click;
            if (age < 0 || age > .55) continue;
            float radius = 6 + (float)age * 22;
            using var ring = new Pen(Color.FromArgb((int)(160 * (1 - age / .55)), Accent), 2);
            g.DrawEllipse(ring, point.X - radius, point.Y - radius, radius * 2, radius * 2);
        }
        using var cursor = new GraphicsPath(); cursor.AddPolygon([point, new(point.X + 1, point.Y + 18), new(point.X + 6, point.Y + 13), new(point.X + 10, point.Y + 21), new(point.X + 14, point.Y + 19), new(point.X + 10, point.Y + 11), new(point.X + 17, point.Y + 10)]);
        using var outline = new Pen(Color.White, 2.6f) { LineJoin = LineJoin.Round }; g.DrawPath(outline, cursor);
        using var fill = new SolidBrush(Color.FromArgb(55, 72, 108)); g.FillPath(fill, cursor);
    }
    static void DrawSpeaker(Graphics g, PointF point, bool playing)
    {
        using var pen = new Pen(playing ? Accent : Muted, 1.6f) { LineJoin = LineJoin.Round };
        float x = point.X - 8, y = point.Y - 7;
        g.DrawPolygon(pen, new PointF[] { new(x, y + 4), new(x + 4, y + 4), new(x + 8, y), new(x + 8, y + 16), new(x + 4, y + 12), new(x, y + 12) });
        g.DrawArc(pen, x + 7, y + 3, 9, 10, -60, 120);
        if (playing) g.DrawArc(pen, x + 7, y - 1, 15, 18, -55, 110);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Dispose(); clock.Stop(); bubble.Dispose(); }
        base.Dispose(disposing);
    }
}
