using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace EnglishHelper;

static partial class UiTest
{
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
    static async Task ClickAt(Point point)
    {
        SetCursorPos(point.X, point.Y); await Task.Delay(40);
        mouse_event(2, 0, 0, 0, UIntPtr.Zero); mouse_event(4, 0, 0, 0, UIntPtr.Zero);
        await Task.Delay(180);
    }
    static Point Center(Control control) => control.PointToScreen(new Point(control.Width / 2, control.Height / 2));
    public static void RunPopupTests()
    {
        string preferences = Path.Combine(AppContext.BaseDirectory, "popup-test-" + Guid.NewGuid().ToString("N") + ".json");
        UseStandardPreferences(preferences);
        using var host = new Form { ShowInTaskbar = false, Opacity = 0 };
        host.Shown += async (_, _) =>
        {
            var cursor = Cursor.Position; var clipboard = Native.SnapshotClipboard();
            try
            {
                host.Hide(); await TestPopupInteractions();
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "popup-test.txt"), "PASS: word and sentence popups keep inside clicks; actual outside clicks dismiss both; clicked target still receives click; popup closes on focus loss; no binary corner region; shared sentence styling and Auto-read; replacing selected text survives internal focus transfer.");
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "popup-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
            finally
            {
                SetCursorPos(cursor.X, cursor.Y);
                try { if (clipboard != null) Clipboard.SetDataObject(clipboard, true); } catch { }
                if (File.Exists(preferences)) File.Delete(preferences); host.Close();
            }
        };
        Application.Run(host);
    }
    static async Task TestPopupInteractions()
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(typeof(Program).Assembly.Location); start.ArgumentList.Add("--input-fixture");
        using var fixture = Process.Start(start) ?? throw new Exception("Cannot start fixture");
        try
        {
            for (int i = 0; i < 40; i++) { fixture.Refresh(); if (fixture.MainWindowHandle != IntPtr.Zero) break; await Task.Delay(100); }
            var window = fixture.MainWindowHandle; if (window == IntPtr.Zero) throw new Exception("Fixture did not open");
            FocusTestWindow(window);
            var root = AutomationElement.FromHandle(window);
            AutomationElement Find(string name) => root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name)) ?? throw new Exception("Missing fixture " + name);
            var target = Find("fixtureOutsideButton"); var count = Find("Clicks: 0"); var editor = Find("fixtureEditor");
            var rect = target.Current.BoundingRectangle;
            var outside = new Point((int)(rect.Left + rect.Width / 2), (int)(rect.Top + rect.Height / 2));
            var spoken = new RecordedSpeech();
            using (var word = new ResultWindow("move", IntPtr.Zero, false, spoken))
            {
                word.Show(); word.Translate().GetAwaiter().GetResult(); FocusTestWindow(word.Handle); await Task.Delay(150);
                var pendingTranslation = word.TranslationToken;
                if (word.Region != null) throw new Exception("Word popup should use compositor corners, not a binary region");
                await ClickAt(Center(Descendants(word).OfType<SpeakerButton>().Single()));
                if (word.IsDisposed || !spoken.Words.SequenceEqual(new[] { "move" })) throw new Exception("Inside word click dismissed popup or did not play");
                if (word.Bounds.Contains(outside)) throw new Exception("Fixture overlaps popup");
                await ClickAt(outside);
                if (!word.IsDisposed || count.Current.Name != "Clicks: 1" || !spoken.Disposed || !pendingTranslation.IsCancellationRequested)
                    throw new Exception("Word outside-click dismissal or click-through failed: disposed=" + word.IsDisposed + "; count=" + count.Current.Name + "; speechDisposed=" + spoken.Disposed + "; cancelled=" + pendingTranslation.IsCancellationRequested);
            }
            const string sentenceText = "We should test sentence popups.";
            var sentenceSpeech = new RecordedSpeech();
            using (var sentence = new ResultWindow(sentenceText, IntPtr.Zero, false, sentenceSpeech))
            {
                sentence.SetResult("我们应该测试句子弹窗。", ""); sentence.Show(); FocusTestWindow(sentence.Handle); await Task.Delay(150);
                if (sentence.Region != null || sentence.BackColor != Color.White || Descendants(sentence).OfType<RichTextBox>().Count() != 2) throw new Exception("Sentence popup styling regression");
                var languageButton = Descendants(sentence).OfType<Button>().Single(x => x.Text.Contains("→"));
                languageButton.PerformClick(); Application.DoEvents();
                var menu = sentence.CurrentLanguageMenu ?? throw new Exception("Language picker did not open");
                var sourceChoices = (ToolStripMenuItem)menu.Items[0];
                sourceChoices.DropDownItems.OfType<ToolStripMenuItem>().Single(x => x.Text == "Français").PerformClick();
                if (!languageButton.Text.Contains("Français") || sentence.IsDisposed) throw new Exception("Source-language selection dismissed popup or did not apply");
                sourceChoices.DropDownItems.OfType<ToolStripMenuItem>().First().PerformClick(); menu.Close(); Application.DoEvents();
                var translated = Descendants(sentence).OfType<RichTextBox>().Single(x => x.AccessibleName == "译文");
                await ClickAt(Center(translated));
                if (sentence.IsDisposed) throw new Exception("Inside sentence click should keep popup open");
                await ClickAt(Center(Descendants(sentence).OfType<AutoReadSwitch>().Single()));
                if (sentence.IsDisposed || !sentenceSpeech.Words.SequenceEqual(new[] { sentenceText })) throw new Exception("Sentence Auto-read failed");
                await ClickAt(outside);
                if (!sentence.IsDisposed || count.Current.Name != "Clicks: 2") throw new Exception("Sentence outside-click dismissal or click-through failed");
            }
            using (var focus = new ResultWindow("generation", IntPtr.Zero, false, new RecordedSpeech()))
            {
                focus.Show(); focus.Translate().GetAwaiter().GetResult(); FocusTestWindow(focus.Handle); await Task.Delay(150);
                FocusTestWindow(window); editor.SetFocus(); await Task.Delay(200);
                if (!focus.IsDisposed) throw new Exception("Focus loss did not dismiss popup");
            }
            UserPreferences.SetAutoRead(false);
            editor.SetFocus(); ((TextPattern)editor.GetCurrentPattern(TextPattern.Pattern)).DocumentRange.Select();
            using (var replacement = new ResultWindow("你好，很高兴认识你。", window, true, new RecordedSpeech()))
            {
                replacement.SetResult("Hello, nice to meet you.", ""); replacement.Show(); FocusTestWindow(replacement.Handle); await Task.Delay(150);
                var use = Descendants(replacement).OfType<Button>().Single(x => x.Text == "使用译文");
                if (!use.Enabled) throw new Exception("Prepared translation must remain usable when popup is shown");
                await ClickAt(Center(use)); await Task.Delay(1100);
                var actual = ((ValuePattern)editor.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
                if (!replacement.IsDisposed || actual != "Hello, nice to meet you.")
                    throw new Exception("Dismissal interrupted replacement focus transfer: disposed=" + replacement.IsDisposed + "; editor=" + actual + "; labels=" + string.Join(" | ", Descendants(replacement).OfType<Label>().Select(x => x.Text)));
            }
        }
        finally { if (!fixture.HasExited) { fixture.CloseMainWindow(); await Task.Delay(200); } }
    }
}
