using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace EnglishHelper;

static partial class UiTest
{
    static void UseStandardPreferences(string path)
    {
        UserPreferences.UseTestFile(path);
        UserPreferences.TrySave(new AppPreferences(InterfaceLanguage: "zh-CN", ReadTarget: "zh-CN", InputTarget: "en", HotkeysEnabled: false));
    }
    sealed class RecordedSpeech : ISpeechOutput
    {
        public List<string> Words { get; } = [];
        public int Stops { get; private set; }
        public bool Disposed { get; private set; }
        public void Speak(string text) => Words.Add(text);
        public void Stop() => Stops++;
        public void Dispose() => Disposed = true;
    }
    static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
    public static void RunWordViewTests()
    {
        string preferences = Path.Combine(AppContext.BaseDirectory, "word-test-" + Guid.NewGuid().ToString("N") + ".json");
        UseStandardPreferences(preferences);
        try
        {
            var recorded = new RecordedSpeech();
            using (var word = new ResultWindow("move", IntPtr.Zero, false, recorded))
            {
                word.Show(); word.Translate().GetAwaiter().GetResult(); Application.DoEvents();
                if (!word.IsCompactWordView || !word.PhoneticText.Contains('/' ) || !word.ResultText.Any(Translator.IsChinese)) throw new Exception("Compact word data missing");
                if (Descendants(word).Any(x => x.Text is "原文" or "使用译文" or "翻译")) throw new Exception("Sentence controls should not appear in word lookup");
                var toggle = Descendants(word).OfType<AutoReadSwitch>().Single();
                int toggleBottom = word.PointToClient(toggle.PointToScreen(new Point(0, toggle.Height))).Y;
                if (word.ClientSize.Height - toggleBottom > 35) throw new Exception("Auto-read footer has excessive empty space");
                if (word.AutoReadEnabled || recorded.Words.Count != 0) throw new Exception("Auto-read should default off");
                word.AutoReadEnabled = true;
                if (!recorded.Words.SequenceEqual(new[] { "move" })) throw new Exception("Enabling auto-read must pronounce current word");
                UserPreferences.ReloadForTest(); if (!UserPreferences.AutoRead) throw new Exception("Preference did not persist");
                word.Translate().GetAwaiter().GetResult();
                if (recorded.Words.Count != 1) throw new Exception("Repeated rendering pronounced the word twice");
                word.AutoReadEnabled = false;
                if (recorded.Stops != 1) throw new Exception("Disabling auto-read must stop playback");
                word.AutoReadEnabled = true; word.Close();
            }
            var nextSpeech = new RecordedSpeech();
            using (var next = new ResultWindow("generation", IntPtr.Zero, false, nextSpeech))
            {
                next.Show(); next.Translate().GetAwaiter().GetResult(); Application.DoEvents();
                if (!next.AutoReadEnabled || !nextSpeech.Words.SequenceEqual(new[] { "generation" })) throw new Exception("New lookup must honor saved auto-read preference");
                next.AutoReadEnabled = false; next.Close();
            }
            UserPreferences.ReloadForTest(); if (UserPreferences.AutoRead) throw new Exception("Disabled preference did not persist");
            var manualSpeech = new RecordedSpeech();
            using (var manual = new ResultWindow("computer", IntPtr.Zero, false, manualSpeech))
            {
                manual.Show(); manual.Translate().GetAwaiter().GetResult(); Application.DoEvents();
                if (manualSpeech.Words.Count != 0) throw new Exception("Disabled auto-read should stay silent");
                Descendants(manual).OfType<SpeakerButton>().Single().PerformClick();
                if (!manualSpeech.Words.SequenceEqual(new[] { "computer" })) throw new Exception("Manual speaker replay failed");
                manual.Close();
            }
            UserPreferences.SetAutoRead(true);
            var missingSpeech = new RecordedSpeech();
            using (var missing = new ResultWindow("qzxqzxunknown", IntPtr.Zero, false, missingSpeech))
            {
                missing.Translate().GetAwaiter().GetResult();
                if (missingSpeech.Words.Count != 0 || !(missing.ResultText + missing.FeedbackText).Contains("未收录")) throw new Exception("Missing entries must not auto-read");
            }
            using (var muted = new EnglishSpeech(0)) { muted.Speak("move"); muted.Stop(); }
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "word-view-test.txt"), "PASS: compact word/phonetic/definitions layout; default silent; toggle reads current word; saved preference applies to next lookup; no duplicate playback; disable stops playback; manual speaker replay; missing word stays silent; English SAPI request accepted at muted volume.");
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "word-view-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
        finally { if (File.Exists(preferences)) File.Delete(preferences); }
    }
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint from, uint to, bool attach);
    [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr window);
    static void FocusTestWindow(IntPtr window)
    {
        uint current = GetCurrentThreadId();
        uint foreground = Native.GetWindowThreadProcessId(Native.GetForegroundWindow(), out _);
        bool attached = current != foreground && AttachThreadInput(current, foreground, true);
        try { BringWindowToTop(window); Native.SetForegroundWindow(window); }
        finally { if (attached) AttachThreadInput(current, foreground, false); }
    }
    public static void RunFixture()
    {
        using var form = new Form { Text = UiText.T("appShort") + " · 独立输入框测试", Width = 500, Height = 260, TopMost = true };
        var editor = new TextBox { Name = "fixtureEditor", AccessibleName = "fixtureEditor", Text = "你好，很高兴认识你。", Multiline = true, Bounds = new Rectangle(20, 20, 440, 70) };
        var other = new TextBox { Name = "fixtureOther", AccessibleName = "fixtureOther", Text = "other field", Bounds = new Rectangle(20, 100, 440, 28) };
        var password = new TextBox { Name = "fixturePassword", AccessibleName = "fixturePassword", UseSystemPasswordChar = true, Text = "test-password", Bounds = new Rectangle(20, 140, 200, 28) };
        var readOnly = new TextBox { Name = "fixtureReadOnly", AccessibleName = "fixtureReadOnly", ReadOnly = true, Text = "generation", Bounds = new Rectangle(240, 140, 220, 28) };
        form.Controls.AddRange([editor, other, password, readOnly]);
        int clicks = 0;
        var outside = new Button { Name = "fixtureOutsideButton", AccessibleName = "fixtureOutsideButton", Text = "Outside click target", Bounds = new Rectangle(20, 180, 200, 28) };
        var counter = new Label { Name = "fixtureClickCount", Text = "Clicks: 0", Bounds = new Rectangle(240, 182, 220, 25) };
        outside.Click += (_, _) => counter.Text = "Clicks: " + ++clicks;
        form.Controls.AddRange([outside, counter]);
        form.Shown += (_, _) => { form.Activate(); editor.Focus(); editor.Select(editor.Text.Length, 0); };
        Application.Run(form);
    }

    static async Task TestWholeInput()
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(typeof(Program).Assembly.Location); start.ArgumentList.Add("--input-fixture");
        using var fixture = Process.Start(start) ?? throw new Exception("Cannot start fixture");
        try
        {
            for (int i = 0; i < 40; i++) { fixture.Refresh(); if (fixture.MainWindowHandle != IntPtr.Zero) break; await Task.Delay(100); }
            var hwnd = fixture.MainWindowHandle;
            if (hwnd == IntPtr.Zero) throw new Exception("Fixture did not open");
            FocusTestWindow(hwnd); await Task.Delay(200);
            var root = AutomationElement.FromHandle(hwnd);
            AutomationElement Find(string name) => root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name)) ?? throw new Exception("Missing fixture element " + name);
            var editor = Find("fixtureEditor"); var other = Find("fixtureOther");
            editor.SetFocus(); await Task.Delay(100);
            var editorText = (TextPattern)editor.GetCurrentPattern(TextPattern.Pattern);
            editorText.DocumentRange.FindText("你好", false, false).Select();
            var copied = await Native.Selection();
            if (copied != "你好") throw new Exception("Cross-process selection capture failed: " + copied);
            await Native.Paste("Hello");
            if (((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).Current.Value != "Hello，很高兴认识你。") throw new Exception("Cross-process selected replacement failed");
            ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).SetValue("你好，很高兴认识你。");
            var empty = editorText.DocumentRange;
            empty.MoveEndpointByRange(System.Windows.Automation.Text.TextPatternRangeEndpoint.Start, empty, System.Windows.Automation.Text.TextPatternRangeEndpoint.End); empty.Select();
            var input = InputTarget.Capture();
            if (input.Original != "你好，很高兴认识你。") throw new Exception("Whole-input capture failed");
            await input.Replace("Hello, nice to meet you.");
            if (((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).Current.Value != "Hello, nice to meet you.") throw new Exception("Whole-input replace failed");
            if (Clipboard.GetText() != "clipboard-test-marker") throw new Exception("Whole-input clipboard restoration failed");

            editor.SetFocus(); var changed = InputTarget.Capture();
            ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).SetValue("Edited while translating");
            bool rejected = false;
            try { await changed.Replace("Must not overwrite"); } catch { rejected = true; }
            if (!rejected || ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).Current.Value != "Edited while translating") throw new Exception("Changed-text protection failed");

            editor.SetFocus(); var switched = InputTarget.Capture(); other.SetFocus(); rejected = false;
            try { await switched.Replace("Must not overwrite other field"); } catch { rejected = true; }
            if (!rejected || ((ValuePattern)other.GetCurrentPattern(ValuePattern.Pattern)).Current.Value != "other field") throw new Exception("Focus-change protection failed");
            if (InputTarget.IsEditable(Find("fixturePassword"))) throw new Exception("Password must be excluded");
            if (InputTarget.IsEditable(Find("fixtureReadOnly"))) throw new Exception("Read-only must be excluded");
            await TestContextBubble(editor, Find("fixturePassword"), Find("fixtureReadOnly"), hwnd);
            await TestApplicationShortcuts(editor, hwnd);
        }
        finally { if (!fixture.HasExited) { fixture.CloseMainWindow(); await Task.Delay(200); } }
    }
    static async Task TestApplicationShortcuts(AutomationElement editor, IntPtr window)
    {
        var profile = UserPreferences.Current;
        var shortcuts = profile with { HotkeysEnabled = true, ReadHotkey = "Ctrl+Alt+Shift+F10", InputHotkey = "Ctrl+Alt+Shift+F11" };
        UserPreferences.TrySave(shortcuts);
        try
        {
            ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).SetValue("generation");
            FocusTestWindow(window); editor.SetFocus(); ((TextPattern)editor.GetCurrentPattern(TextPattern.Pattern)).DocumentRange.Select();
            using var app = new FloatingBar(); app.Show();
            await PressChord(shortcuts.ReadHotkey); await Task.Delay(500);
            if (app.CurrentResult is not { } result || !result.ResultText.Contains("一代")) throw new Exception("Application lookup shortcut did not open the dictionary");
            result.Close(); FocusTestWindow(window); editor.SetFocus();
            const string text = "Already in English.";
            ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).SetValue(text);
            var caret = ((TextPattern)editor.GetCurrentPattern(TextPattern.Pattern)).DocumentRange;
            caret.MoveEndpointByRange(System.Windows.Automation.Text.TextPatternRangeEndpoint.Start, caret, System.Windows.Automation.Text.TextPatternRangeEndpoint.End); caret.Select();
            await PressChord(shortcuts.InputHotkey); await Task.Delay(1050);
            if (!app.LastActionSucceeded || ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).Current.Value != text) throw new Exception("Application input shortcut did not complete");
        }
        finally { UserPreferences.TrySave(profile); }
    }
    static async Task TestContextBubble(AutomationElement editor, AutomationElement password, AutomationElement readOnly, IntPtr window)
    {
        ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).SetValue("generation");
        editor.SetFocus();
        var textPattern = (TextPattern)editor.GetCurrentPattern(TextPattern.Pattern);
        textPattern.DocumentRange.Select();
        var selected = ContextAnchor.FindSelection() ?? throw new Exception("Selection detection failed");
        if (selected.Text != "generation") throw new Exception("Selection snapshot mismatch");
        using var bubble = new FloatingBar(); bubble.Show();
        await Task.Delay(650);
        if (!bubble.Visible || !bubble.SelectionMode) throw new Exception("Automatic selection bubble did not appear");
        var box = bubble.BubbleScreenBounds;
        if (Math.Abs(box.Left - selected.Bounds.Right) > 65 || Math.Abs(box.Bottom - selected.Bounds.Top) > 65)
            throw new Exception($"Selection bubble too far away: {box}, selected {selected.Bounds}");
        if (Native.GetForegroundWindow() != window) throw new Exception("Context bubble stole focus");
        await bubble.ActivatePrimary();
        if (bubble.CurrentResult is not { } result || !result.ResultText.Contains("一代")) throw new Exception("Selection bubble did not open dictionary lookup directly");
        if (((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).Current.Value != "generation") throw new Exception("Word lookup modified editor");
        var work = Screen.FromRectangle(selected.Bounds).WorkingArea;
        if (!work.Contains(result.Bounds)) throw new Exception("Lookup panel extends off screen");
        result.Close(); FocusTestWindow(window); editor.SetFocus();
        var caret = textPattern.DocumentRange;
        caret.MoveEndpointByRange(System.Windows.Automation.Text.TextPatternRangeEndpoint.Start, caret, System.Windows.Automation.Text.TextPatternRangeEndpoint.End);
        caret.Select();
        await Task.Delay(650);
        if (!bubble.Visible || bubble.SelectionMode) throw new Exception("Input bubble did not attach after selection cleared");
        var field = editor.Current.BoundingRectangle; box = bubble.BubbleScreenBounds;
        if (Math.Abs(box.Right - field.Right) > 50 || Math.Abs(box.Top - field.Top) > 50) throw new Exception("Input bubble is not near the field corner");
        var anchored = bubble.Location;
        ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).SetValue("Still typing here");
        await Task.Delay(450);
        if (bubble.Location != anchored) throw new Exception("Input bubble moved while typing");
        password.SetFocus(); await Task.Delay(500);
        if (bubble.Visible) throw new Exception("Bubble should hide on password fields");
        readOnly.SetFocus(); ((TextPattern)readOnly.GetCurrentPattern(TextPattern.Pattern)).DocumentRange.Select();
        await Task.Delay(550);
        if (!bubble.Visible || !bubble.SelectionMode || ContextAnchor.FindSelection()?.Text != "generation") throw new Exception("Read-only selection bubble did not appear");
        var negativeScreen = new Rectangle(-1920, -1080, 1920, 1080);
        var nearEdge = ContextAnchor.PopupLocation(new Rectangle(-10, -15, 8, 10), new Size(448, 470), negativeScreen);
        if (!negativeScreen.Contains(new Rectangle(nearEdge, new Size(448, 470)))) throw new Exception("Negative-coordinate monitor placement failed");
    }
    public static void Run()
    {
        string preferences = Path.Combine(AppContext.BaseDirectory, "ui-test-" + Guid.NewGuid().ToString("N") + ".json");
        UseStandardPreferences(preferences);
        using var form = new Form { Text = UiText.T("appShort") + "自动验证", ShowInTaskbar = false, Opacity = 0 };
        form.Shown += async (_, _) =>
        {
            IDataObject? previous = null;
            try
            {
                previous = Native.SnapshotClipboard();
                Clipboard.SetText("clipboard-test-marker");
                form.Hide();
                await TestWholeInput();
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "ui-test.txt"), "PASS: focus and clipboard preservation; selection and full-input replacement; changed-text/focus protection; automatic bubble beside selected word; direct offline lookup; nearby result panel; input-corner attachment; stable placement while typing; password hiding; read-only selected text; multi-monitor placement.");
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "ui-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
            finally { try { if (previous != null) Clipboard.SetDataObject(previous, true); } catch { } if (File.Exists(preferences)) File.Delete(preferences); form.Close(); }
        };
        Application.Run(form);
    }
}
