using System.Net;
using System.Net.Http;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace EnglishHelper;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Contains("--settings-navigation-test")) { UiTest.RunSettingsNavigationTests(); return; }
        if (args.Contains("--dictionary-settings-test"))
        {
            try { DictionarySettingsPanel.TestLayout(); }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "dictionary-settings-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
            return;
        }
        if (args.Contains("--dictionary-packs-test"))
        {
            try { LocalDictionary.TestPacks(); }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "dictionary-packs-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
            return;
        }
        if (args.Contains("--memory-email-test")) { UiTest.RunMemoryEmailTests(); return; }
        if (args.Contains("--animation-preview")) { UiTest.SaveAnimationPreview(); return; }
        if (args.Contains("--ai-test")) { UiTest.RunAiTests(); return; }
        if (args.Contains("--multilingual-smoke")) { UiTest.RunMultilingualSmoke(); return; }
        if (args.Contains("--settings-test")) { UiTest.RunSettingsTests(); return; }
        if (args.Contains("--settings-preview")) { UiTest.SaveSettingsPreview(); return; }
        if (args.Contains("--popup-test")) { UiTest.RunPopupTests(); return; }
        if (args.Contains("--word-view-test")) { UiTest.RunWordViewTests(); return; }
        if (args.Length == 2 && args[0] == "--install-dictionary")
        {
            int count = LocalDictionary.Install(args[1]);
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "dictionary-install.txt"), $"Installed {count} entries into {LocalDictionary.Folder}");
            return;
        }
        if (args.Length == 3 && args[0] is "--build-dictionary" or "--build-full-dictionary")
        {
            int count = LocalDictionary.Build(args[1], args[2], compact: args[0] == "--build-dictionary");
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "dictionary-build.txt"), $"Built {count} dictionary entries.");
            return;
        }
        if (args.Contains("--dictionary-test"))
        {
            try { LocalDictionary.Test(); }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "dictionary-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
            return;
        }
        if (args.Contains("--preview"))
        {
            using var bar = new FloatingBar();
            bar.Show(); Application.DoEvents();
            using var bitmap = bar.RenderPreview(false);
            bitmap.Save(Path.Combine(AppContext.BaseDirectory, "floating-preview.png"));
            using var expanded = bar.RenderPreview(true);
            expanded.Save(Path.Combine(AppContext.BaseDirectory, "floating-hover-preview.png"));
            bar.Close();
            UiTest.SavePopupPreviews();
            return;
        }
        if (args.Contains("--input-fixture")) { UiTest.RunFixture(); return; }
        if (args.Contains("--ui-test")) { UiTest.Run(); return; }
        if (args.Contains("--self-test"))
        {
            try
            {
                var longText = string.Concat(Enumerable.Repeat("你好，world😀。", 150));
                var chunks = Translator.Chunks(longText).ToArray();
                if (string.Concat(chunks) != longText || chunks.Any(x => Encoding.UTF8.GetByteCount(x) > 450))
                    throw new Exception("Chunk validation failed");
                var a = Translator.Translate("你好，很高兴认识你。", true).GetAwaiter().GetResult();
                var b = Translator.Translate("Good morning", false).GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(a) || !b.Any(Translator.IsChinese)) throw new Exception("Translation validation failed");
                var speech = Type.GetTypeFromProgID("SAPI.SpVoice") != null;
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test.txt"), $"PASS: UTF-8 chunks, Chinese→English: {a}, English→Chinese: {b}, speech available: {speech}");
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
            return;
        }
        using var mutex = new Mutex(true, "Local\\EnglishHelper.Desktop", out bool first);
        if (!first) { MessageBox.Show(UiText.T("alreadyRunning"), UiText.T("app")); return; }
        Application.Run(new FloatingBar());
    }
}

static class Translator
{
    static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(20) };
    public static bool IsChinese(char c) => c is >= '\u3400' and <= '\u9fff';
    public static IEnumerable<string> Chunks(string text)
    {
        var chunk = new StringBuilder();
        int bytes = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (bytes + rune.Utf8SequenceLength > 450) { yield return chunk.ToString(); chunk.Clear(); bytes = 0; }
            chunk.Append(rune.ToString()); bytes += rune.Utf8SequenceLength;
            if (bytes > 200 && "。！？.!?\n".Contains(rune.ToString())) { yield return chunk.ToString(); chunk.Clear(); bytes = 0; }
        }
        if (chunk.Length > 0) yield return chunk.ToString();
    }
    public static async Task<string> Translate(string text, bool toEnglish, CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        if (text.Length > 3000) throw new Exception("请每次选取不超过 3000 个字符。");
        if (!toEnglish && LocalDictionary.IsWord(text)) return LocalDictionary.Lookup(text);
        var results = new List<string>();
        foreach (var chunk in Chunks(text))
        {
            cancellation.ThrowIfCancellationRequested();
            var pair = toEnglish ? "zh-CN|en" : "en|zh-CN";
            var url = "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(chunk) + "&langpair=" + Uri.EscapeDataString(pair);
            using var response = await Client.GetAsync(url, cancellation).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false));
            var root = json.RootElement;
            if (root.GetProperty("responseStatus").ToString() != "200")
                throw new Exception("翻译服务暂时不可用或已达到免费额度。" + root.GetProperty("responseDetails").ToString());
            var translated = WebUtility.HtmlDecode(root.GetProperty("responseData").GetProperty("translatedText").GetString());
            if (string.IsNullOrWhiteSpace(translated)) throw new Exception("服务未返回译文，请重试。");
            results.Add(translated);
        }
        return string.Join(toEnglish ? " " : "", results);
    }
}

static class Native
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
    [DllImport("user32.dll")] static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public UNION data; }
    [StructLayout(LayoutKind.Explicit)] struct UNION { [FieldOffset(0)] public KEYBDINPUT keyboard; [FieldOffset(0)] public MOUSEINPUT mouse; }
    [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort key, scan; public uint flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int x, y; public uint mouseData, flags, time; public UIntPtr extra; }
    static INPUT Key(ushort key, bool up = false) => new() { type = 1, data = new UNION { keyboard = new KEYBDINPUT { key = key, flags = up ? 2u : 0 } } };
    public static void Ctrl(ushort key)
    {
        INPUT[] inputs = [Key(0x11), Key(key), Key(key, true), Key(0x11, true)];
        if (SendInput(4, inputs, Marshal.SizeOf<INPUT>()) != 4) throw new Exception("无法操作此窗口，请检查目标软件是否以管理员权限运行。");
    }
    public static async Task<string> Selection()
    {
        var previous = SnapshotClipboard();
        uint before = GetClipboardSequenceNumber(), copied = before;
        try
        {
            Ctrl(0x43);
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(40);
                copied = GetClipboardSequenceNumber();
                if (copied != before) return Clipboard.ContainsText() ? Clipboard.GetText() : "";
            }
            return "";
        }
        finally
        {
            if (copied != before && copied == GetClipboardSequenceNumber())
                try { if (previous != null) Clipboard.SetDataObject(previous, true); else Clipboard.Clear(); } catch { /* Selection still usable when clipboard owner exits. */ }
        }
    }
    public static async Task Paste(string text)
    {
        var previous = SnapshotClipboard();
        Clipboard.SetText(text);
        var ours = GetClipboardSequenceNumber();
        try { Ctrl(0x56); await Task.Delay(700); }
        finally
        {
            if (ours == GetClipboardSequenceNumber())
                try { if (previous != null) Clipboard.SetDataObject(previous, true); else Clipboard.Clear(); } catch { }
        }
    }
    public static IDataObject? SnapshotClipboard()
    {
        var data = Clipboard.GetDataObject();
        if (data == null) return null;
        var snapshot = new DataObject();
        foreach (var format in data.GetFormats(false))
        {
            try
            {
                var value = data.GetData(format, false);
                if (value != null) snapshot.SetData(format, false, value);
            }
            catch { /* Some application-specific formats cannot be materialized. */ }
        }
        return snapshot;
    }
}

sealed class FloatingBar : ArtworkSurface
{
    readonly NotifyIcon tray;
    readonly Icon trayArtwork;
    readonly ToolTip hints = new() { AutoPopDelay = 10000, InitialDelay = 400 };
    readonly System.Windows.Forms.Timer follow = new() { Interval = 350 };
    readonly System.Windows.Forms.Timer selectionDelay = new() { Interval = 120 };
    readonly MouseObserver mouseObserver;
    readonly HotkeyManager hotkeys;
    SettingsWindow? settingsWindow;
    readonly Dictionary<ToolStripItem, string> menuText = new();
    readonly System.Windows.Forms.Timer reset = new() { Interval = 2600 };
    bool busy;
    bool followInput = true;
    bool inputAtBottom;
    SelectedText? selection;
    Point? releasedAt;
    IntPtr lastExternalWindow;
    internal bool SelectionMode => IsSelectionMode;
    internal bool LastActionSucceeded => IsSuccessful;
    internal ResultWindow? CurrentResult => result;
    ResultWindow? result;
    Point dragStart;
    Point dragLocation;
    bool dragging;
    bool resourcesDisposed;
    public FloatingBar()
    {
        StartPosition = FormStartPosition.Manual;
        var area = Screen.PrimaryScreen!.WorkingArea; Location = new Point(area.Right - Width - 32, area.Top + 160);
        Font = new Font("Microsoft YaHei UI", 9);
        hints.SetToolTip(this, "选字时点击查词；输入框内点击翻译整句。拖动固定位置，右键设置。");
        var menu = new ContextMenuStrip();
        var settingsItem = menu.Items.Add(UiText.T("settings"), null, (_, _) => OpenSettings()); menuText[settingsItem] = "settings";
        menu.Items.Add(new ToolStripSeparator());
        var followItem = new ToolStripMenuItem("自动靠近选中文字和输入框") { Checked = true, CheckOnClick = true };
        followItem.Click += (_, _) => { followInput = followItem.Checked; if (followInput) RefreshContext(); };
        menu.Opening += (_, _) => followItem.Checked = followInput;
        menu.Items.Add(followItem);
        menuText[followItem] = "follow";
        var bottomItem = new ToolStripMenuItem("输入框按钮放在右下角") { CheckOnClick = true };
        bottomItem.Click += (_, _) => { inputAtBottom = bottomItem.Checked; RefreshContext(); };
        menu.Items.Add(bottomItem);
        menuText[bottomItem] = "bottom";
        var lookupItem = menu.Items.Add(UiText.T("readKey"), null, async (_, _) => await CaptureText()); menuText[lookupItem] = "readKey";
        var manualItem = menu.Items.Add(UiText.T("manual"), null, (_, _) => OpenManual()); menuText[manualItem] = "manual";
        var helpItem = menu.Items.Add(UiText.T("help"), null, (_, _) => MessageBox.Show(UiText.T("readAction") + "\n" + UserPreferences.Current.ReadHotkey + "\n\n" + UiText.T("writeAction") + "\n" + UserPreferences.Current.InputHotkey + "\n\n" + UiText.T("readingRule"), UiText.T("app")));
        menuText[helpItem] = "help";
        var exitItem = menu.Items.Add(UiText.T("exit"), null, (_, _) => Close()); menuText[exitItem] = "exit";
        ContextMenuStrip = menu;
        using (var stream = typeof(FloatingBar).Assembly.GetManifestResourceStream("EnglishHelper.Assets.translation-orb.ico")
            ?? throw new InvalidOperationException("Missing tray icon artwork."))
        using (var icon = new Icon(stream, SystemInformation.SmallIconSize)) trayArtwork = (Icon)icon.Clone();
        Icon = trayArtwork;
        tray = new NotifyIcon { Icon = trayArtwork, Text = UiText.T("app"), Visible = true, ContextMenuStrip = menu };
        tray.DoubleClick += (_, _) => OpenManual();
        hotkeys = new HotkeyManager(Handle);
        if (!hotkeys.TryApply(UserPreferences.Current, out var shortcutError)) tray.ShowBalloonTip(4500, UiText.T("settings"), shortcutError, ToolTipIcon.Info);
        UserPreferences.Changed += ApplyPreferences;
        ApplyPreferences();
        follow.Tick += (_, _) => RefreshContext(); follow.Start();
        selectionDelay.Tick += (_, _) => { selectionDelay.Stop(); var point = releasedAt; releasedAt = null; RefreshContext(point); };
        mouseObserver = new MouseObserver();
        mouseObserver.ButtonChanged += (point, down) =>
        {
            if (!IsHandleCreated || IsDisposed) return;
            BeginInvoke(() => ObservePointer(point, down));
        };
        reset.Tick += (_, _) => { reset.Stop(); IsSuccessful = false; RefreshArtwork(); };
    }
    void ApplyPreferences()
    {
        if (IsDisposed) return;
        Text = UiText.T("app"); tray.Text = UiText.T("app");
        AccessibleName = UiText.T("bubbleAccessible");
        foreach (var item in menuText) item.Key.Text = UiText.T(item.Value);
        UpdateBubbleHint();
        RefreshArtwork();
    }
    void UpdateBubbleHint()
    {
        var preferences = UserPreferences.Current;
        hints.SetToolTip(this, UiText.T(IsSelectionMode ? "readKey" : "writeKey") + " → " +
            Languages.Get(IsSelectionMode ? preferences.ReadTarget : preferences.InputTarget).Name +
            "\n" + ArtworkSurface.ShortcutCaption(IsSelectionMode, preferences));
    }
    internal void OpenSettings(int section = 0)
    {
        if (settingsWindow is { IsDisposed: false }) { settingsWindow.SelectSection(section); settingsWindow.Activate(); return; }
        result?.Close(); hotkeys.Suspend(); settingsWindow = new SettingsWindow(hotkeys);
        settingsWindow.FormClosed += (_, _) =>
        {
            settingsWindow = null;
            if (!resourcesDisposed && !hotkeys.Resume(UserPreferences.Current, out var error)) tray.ShowBalloonTip(4500, UiText.T("settings"), error, ToolTipIcon.Info);
        };
        settingsWindow.SelectSection(section); settingsWindow.Show();
    }
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x312 && ((int)message.WParam == HotkeyManager.ReadId || (int)message.WParam == HotkeyManager.InputId))
        {
            int action = (int)message.WParam;
            BeginInvoke(async () =>
            {
                if (busy || settingsWindow is { IsDisposed: false } || !await HotkeyManager.WaitForModifiers()) return;
                if (IsDisposed || busy || settingsWindow is { IsDisposed: false }) return;
                result?.Close();
                if (action == HotkeyManager.ReadId) await CaptureText(); else await TranslateInput();
            });
            return;
        }
        base.WndProc(ref message);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !resourcesDisposed)
        {
            resourcesDisposed = true; UserPreferences.Changed -= ApplyPreferences; hotkeys?.Dispose(); settingsWindow?.Close(); mouseObserver?.Dispose(); selectionDelay.Dispose(); follow.Dispose(); reset.Dispose(); hints.Dispose();
            result?.Close(); tray?.Dispose(); trayArtwork?.Dispose();
        }
        base.Dispose(disposing);
    }
    void ObservePointer(Point point, bool down)
    {
        if (!followInput || busy || ContextMenuStrip?.Visible == true) return;
        if (Native.GetAncestor(Native.WindowFromPoint(point), 2) == Handle) return;
        if (down) { selectionDelay.Stop(); selection = null; Hide(); }
        else { releasedAt = point; selectionDelay.Stop(); selectionDelay.Start(); }
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left) { dragStart = MousePosition; dragLocation = Location; dragging = false; }
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.Button != MouseButtons.Left) return;
        var offset = new Point(MousePosition.X - dragStart.X, MousePosition.Y - dragStart.Y);
        if (Math.Abs(offset.X) + Math.Abs(offset.Y) > 6) dragging = true;
        if (dragging) { followInput = false; Location = new Point(dragLocation.X + offset.X, dragLocation.Y + offset.Y); }
    }
    protected override async void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || dragging || busy) return;
        await ActivatePrimary();
    }
    internal async Task ActivatePrimary()
    {
        if (busy) return;
        if (IsSelectionMode) await CaptureText(); else await TranslateInput();
    }
    internal void RefreshContext(Point? selectionEnd = null)
    {
        if (IsDisposed || !followInput || busy || PointerOverArtwork || ContextMenuStrip?.Visible == true || (Native.GetAsyncKeyState(1) & 0x8000) != 0) return;
        try
        {
            var foreground = Native.GetForegroundWindow();
            Native.GetWindowThreadProcessId(foreground, out uint process);
            if (process == Environment.ProcessId) { Hide(); return; }
            lastExternalWindow = foreground;
            selection = ContextAnchor.FindSelection(selectionEnd);
            if (selection != null)
            {
                IsSelectionMode = true;
                var center = ContextAnchor.SelectionCenter(selection.Bounds, BubbleScreenBounds.Width);
                PositionBubble(center, selection.Bounds);
                UpdateBubbleHint();
                return;
            }
            var element = InputTarget.FocusedEditor();
            if (element == null) { Hide(); return; }
            var rect = element.Current.BoundingRectangle;
            if (rect.IsEmpty || rect.Width < 1 || rect.Height < 1 || element.Current.IsOffscreen) { Hide(); return; }
            var input = Rectangle.FromLTRB((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
            IsSelectionMode = false;
            PositionBubble(ContextAnchor.InputCenter(input, BubbleScreenBounds.Width, inputAtBottom), input);
            UpdateBubbleHint();
        }
        catch { selection = null; Hide(); }
    }
    void PositionBubble(Point center, Rectangle anchor)
    {
        var work = Screen.FromRectangle(anchor).WorkingArea;
        center = ContextAnchor.ClampCenter(center, BubbleScreenBounds.Width, work);
        ExpandToRight = center.X - work.Left < Width - BubbleScreenBounds.Width / 2;
        int offsetX = (int)((ExpandToRight ? 24f : 172f) / 196 * Width);
        Location = new Point(center.X - offsetX, center.Y - Height / 2);
        if (!Visible) Show();
        RefreshArtwork();
    }
    async Task TranslateInput()
    {
        if (busy) return;
        busy = true; reset.Stop(); IsWorking = true; IsSuccessful = false; RefreshArtwork();
        string? translated = null;
        InputTarget? input = null;
        try
        {
            input = InputTarget.Capture();
            translated = (await MultilingualTranslator.Translate(input.Original, UserPreferences.Current.InputTarget, UserPreferences.Current.InputSourceLanguage, writing: true)).Text;
            if (IsDisposed) return;
            await input.Replace(translated);
            IsSuccessful = true; reset.Start();
        }
        catch (Exception ex)
        {
            if (IsDisposed) return;
            if (translated != null && input != null)
            {
                result?.Close(); result = new ResultWindow(input.Original, IntPtr.Zero, true);
                result.SetResult(translated, ex.Message); result.Show();
            }
            else MessageBox.Show(UiText.ErrorMessage(ex), UiText.T("app"));
        }
        finally { busy = false; IsWorking = false; if (!IsDisposed) RefreshArtwork(); }
    }
    async Task CaptureText()
    {
        if (busy) return;
        busy = true; IsWorking = true; RefreshArtwork();
        try
        {
            var target = Native.GetForegroundWindow();
            Native.GetWindowThreadProcessId(target, out uint process);
            if (process == Environment.ProcessId && lastExternalWindow != IntPtr.Zero)
            {
                Native.SetForegroundWindow(lastExternalWindow); await Task.Delay(100); target = Native.GetForegroundWindow();
            }
            Point? selectionPoint = selection == null ? null : new Point(selection.Bounds.Left + 1, selection.Bounds.Top + selection.Bounds.Height / 2);
            var captured = ContextAnchor.FindSelection(selectionPoint);
            var selected = captured != null && captured.Window == target ? captured.Text : await Native.Selection();
            if (string.IsNullOrWhiteSpace(selected)) { MessageBox.Show(UiText.T("noSelection"), UiText.T("app")); return; }
            result?.Close();
            result = new ResultWindow(selected, target, false);
            result.PlaceNear(captured?.Bounds ?? BubbleScreenBounds);
            result.Show();
            await result.Translate();
        }
        catch (Exception ex) { MessageBox.Show(UiText.ErrorMessage(ex), UiText.T("app")); }
        finally { busy = false; IsWorking = false; if (!IsDisposed) RefreshArtwork(); }
    }
    void OpenManual() { result?.Close(); result = new ResultWindow("", IntPtr.Zero, true); result.Show(); }
}

