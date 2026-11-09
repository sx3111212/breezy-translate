using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;

namespace EnglishHelper;

static partial class UiTest
{
    sealed class BubbleCaptionPreview(bool selection, bool right) : ArtworkSurface
    {
        public Bitmap CapturePreview()
        {
            IsSelectionMode = selection; ExpandToRight = right;
            return RenderPreview(true);
        }
    }
    public static void RunSettingsNavigationTests()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "navigation-test-" + Guid.NewGuid().ToString("N") + ".json");
        UseStandardPreferences(path);
        try
        {
            var initial = UserPreferences.Current;
            if (new AppLinks(GitHubRepository: "example/bubble").ReleasePage != "https://github.com/example/bubble/releases" ||
                new AppLinks(GitHubRepository: "../bad?repo").ReleasePage != "" || AppLinks.IsWebUrl("file:///C:/test"))
                throw new Exception("Release link validation failed");
            using var owner = new Form();
            using var keys = new HotkeyManager(owner.Handle);
            using var settings = new SettingsWindow(keys);
            settings.Show(); Application.DoEvents();
            var navigation = (ListBox)settings.Controls.Find("SettingsNavigation", true).Single();
            var host = settings.Controls.Find("SettingsSectionHost", true).Single();
            var title = settings.Controls.Find("SettingsSectionTitle", true).Single();
            string[] sectionKeys = ["selectionFeature", "inputFeature", "service", "offlineDictionary", "generalSettings", "softwareUpdate", "donation"];
            var target = (ComboBox)settings.Controls.Find("ReadTarget", true).Single();
            target.SelectedItem = Languages.Get("ja");
            foreach (var language in UiText.InterfaceLanguages)
            {
                var selector = (ComboBox)settings.Controls.Find("InterfaceLanguage", true).Single();
                selector.SelectedItem = Languages.Get(language);
                if (settings.Text != UiText.T("appShort", language) + " · " + UiText.T("settings", language) ||
                    !Descendants(settings.Controls.Find("DonationPanel", true).Single()).OfType<Label>()
                        .Any(label => label.Text.StartsWith(UiText.T("appShort", language))))
                    throw new Exception("Localized short brand did not follow the draft language: " + language);
                for (int i = 0; i < sectionKeys.Length; i++)
                {
                    navigation.SelectedIndex = i; Application.DoEvents();
                    if (host.Controls.Cast<Control>().Count(c => c.Visible) != 1 ||
                        !settings.Controls.Find(sectionKeys[i] + "Section", true).Single().Visible ||
                        title.Text != UiText.T(sectionKeys[i], language) ||
                        navigation.GetItemText(navigation.Items[i]) != title.Text)
                        throw new Exception("Sidebar selection or localization failed: " + language + "/" + i);
                }
            }
            settings.Size = settings.MinimumSize; Application.DoEvents();
            if (navigation.Right > host.Parent!.Left || host.Width < 700)
                throw new Exception("Sidebar overlaps the parameter area at minimum size");
            if (((AppLanguage)target.SelectedItem!).Code != "ja" || UserPreferences.Current != initial)
                throw new Exception("Navigation lost draft settings or changed active preferences");
            settings.SelectSection(0);
            Descendants(settings).OfType<Button>().Single(x => x.Visible && x.Text == UiText.T("manageServices", "es")).PerformClick();
            if (navigation.SelectedIndex != 2) throw new Exception("Manage services did not select the service section");
            if (settings.Controls.Find("DictionaryManagement", true).Single().Parent != settings.Controls.Find("offlineDictionarySection", true).Single() ||
                navigation.Items.Count != 7 || sectionKeys[^1] != "donation") throw new Exception("Dictionary or General section ordering is incorrect");
            settings.Close();
            if (UserPreferences.Current != initial) throw new Exception("Closing settings saved a draft");
            var shortcuts = initial with { HotkeysEnabled = true, ReadHotkeyEnabled = true, InputHotkeyEnabled = true, ReadHotkey = "Ctrl+Alt+Shift+D", InputHotkey = "Ctrl+Alt+Shift+E" };
            if (ArtworkSurface.ShortcutCaption(true, shortcuts) != shortcuts.ReadHotkey || ArtworkSurface.ShortcutCaption(false, shortcuts) != shortcuts.InputHotkey ||
                ArtworkSurface.ShortcutCaption(true, shortcuts with { ReadHotkeyEnabled = false }) != UiText.T("shortcutDisabled", initial.InterfaceLanguage))
                throw new Exception("Bubble shortcut caption did not follow configured keys or enabled state");
            UserPreferences.TrySave(shortcuts);
            using (var bubble = new BubbleCaptionPreview(true, false))
            using (var image = bubble.CapturePreview()) image.Save(Path.Combine(AppContext.BaseDirectory, "bubble-shortcut-preview.png"));
            using (var bubble = new BubbleCaptionPreview(false, true))
            using (var image = bubble.CapturePreview()) image.Save(Path.Combine(AppContext.BaseDirectory, "input-bubble-shortcut-preview.png"));
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "settings-navigation-test.txt"), "PASS: seven sidebar sections; dictionary separated from General; eight interface languages; section titles; minimum window size; draft preservation; manage-service navigation; cancel without saving; bubble custom shortcuts and disabled state.");
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "settings-navigation-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
    public static void RunMultilingualSmoke()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "smoke-" + Guid.NewGuid().ToString("N") + ".json"); UseStandardPreferences(path);
        try
        {
            var japanese = MultilingualTranslator.Translate("你好", "ja", "zh-CN", writing: true).GetAwaiter().GetResult();
            var french = MultilingualTranslator.Translate("Hello", "fr", "en", writing: true).GetAwaiter().GetResult();
            if (!japanese.Text.Any(c => c is >= '\u3040' and <= '\u30ff') || !french.Text.Contains("Bonjour", StringComparison.OrdinalIgnoreCase)) throw new Exception("Multilingual smoke output failed");
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "multilingual-smoke.txt"), "PASS: MyMemory Chinese→Japanese: " + japanese.Text + "; English→French: " + french.Text + ". Azure was not called.");
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "multilingual-smoke.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
    [DllImport("user32.dll")] static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    sealed class HotkeyProbe : Form
    {
        public int Reads, Writes;
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x312)
            {
                if ((int)message.WParam == HotkeyManager.ReadId) Reads++;
                if ((int)message.WParam == HotkeyManager.InputId) Writes++;
            }
            base.WndProc(ref message);
        }
    }
    static async Task PressChord(string value)
    {
        HotkeyChord.TryParse(value, out var chord); var modifiers = new List<byte>();
        if ((chord.Modifiers & 2) != 0) modifiers.Add(0x11);
        if ((chord.Modifiers & 1) != 0) modifiers.Add(0x12);
        if ((chord.Modifiers & 4) != 0) modifiers.Add(0x10);
        foreach (var modifier in modifiers) keybd_event(modifier, 0, 0, UIntPtr.Zero);
        keybd_event((byte)chord.Key, 0, 0, UIntPtr.Zero); keybd_event((byte)chord.Key, 0, 2, UIntPtr.Zero);
        foreach (var modifier in modifiers.AsEnumerable().Reverse()) keybd_event(modifier, 0, 2, UIntPtr.Zero);
        await Task.Delay(170);
    }
    sealed class AzureFixture : HttpMessageHandler
    {
        public bool Received;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.RequestUri!.Query.Contains("to=ja") || request.RequestUri.Query.Contains("from=")) throw new Exception("Azure automatic source/target request mismatch");
            if (!request.Headers.Contains("Ocp-Apim-Subscription-Key") || !request.Headers.Contains("Ocp-Apim-Subscription-Region")) throw new Exception("Azure configuration headers missing");
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            if (!body.Contains("Bonjour")) throw new Exception("Azure request body mismatch"); Received = true;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[{\"detectedLanguage\":{\"language\":\"fr\",\"score\":1},\"translations\":[{\"text\":\"こんにちは\",\"to\":\"ja\"}]}]", Encoding.UTF8, "application/json") };
        }
    }
    public static void RunSettingsTests()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "settings-test-" + Guid.NewGuid().ToString("N") + ".json");
        UserPreferences.UseTestFile(path);
        using var host = new Form { Opacity = 0, ShowInTaskbar = false };
        host.Shown += async (_, _) =>
        {
            var originalCulture = CultureInfo.CurrentUICulture;
            try
            {
                host.Hide(); CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
                foreach (var culture in new[] { "zh-CN", "en-US", "ja-JP", "fr-FR", "de-DE", "es-ES", "ar-SA", "it-IT" })
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture); UserPreferences.ReloadForTest();
                    var fresh = UserPreferences.Current;
                    if (TranslationServices.Resolve(false, fresh, "A whole sentence.") != "MyMemory" || TranslationServices.Resolve(true, fresh) != "MyMemory" ||
                        fresh.ReadTarget != Languages.SystemLanguage || fresh.InputSourceLanguage != Languages.SystemLanguage || fresh.InputTarget != "en")
                        throw new Exception("Fresh multilingual defaults failed: " + culture);
                }
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");
                File.WriteAllText(path, "{\"AutoRead\":true}"); UserPreferences.ReloadForTest();
                var initial = UserPreferences.Current;
                if (!initial.AutoRead || initial.InterfaceLanguage != "ja" || initial.ReadTarget != "ja" || initial.InputTarget != "en") throw new Exception("System defaults or legacy migration failed");
                if (initial.InputSourceLanguage != "auto") throw new Exception("Legacy source behavior changed");
                var savedServices = initial with { ReadSentenceProvider = "Azure", InputProvider = "AI", InputSourceLanguage = "fr" };
                UserPreferences.TrySave(savedServices); UserPreferences.ReloadForTest();
                if (UserPreferences.Current.ReadSentenceProvider != "Azure" || UserPreferences.Current.InputProvider != "AI" || UserPreferences.Current.InputSourceLanguage != "fr") throw new Exception("Existing service or source selection overwritten by defaults");
                var configured = initial with { InterfaceLanguage = "en", ReadTarget = "fr", InputTarget = "de", HotkeysEnabled = false, ReadHotkey = "Ctrl+Alt+Shift+F10", InputHotkey = "Ctrl+Alt+Shift+F11" };
                if (!UserPreferences.TrySave(configured)) throw new Exception("Preferences could not be saved");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE"); UserPreferences.ReloadForTest();
                if (UserPreferences.Current.ReadTarget != "fr" || UserPreferences.Current.InterfaceLanguage != "en") throw new Exception("Saved preferences overwritten by OS language");
                UserPreferences.SetAutoRead(false); UserPreferences.ReloadForTest();
                if (UserPreferences.Current.ReadTarget != "fr" || UserPreferences.Current.InputTarget != "de") throw new Exception("Auto-read overwrote language settings");
                const string dummy = "test-only-not-a-real-api-key";
                var encrypted = SecretStorage.Protect(dummy);
                if (encrypted.Contains(dummy) || SecretStorage.Unprotect(encrypted) != dummy) throw new Exception("Credential encryption failed");
                using (var handler = new AzureFixture())
                using (var client = new HttpClient(handler))
                {
                    var response = await MultilingualTranslator.TranslateAzure("Bonjour", "auto", "ja", dummy, "test-region", client, CancellationToken.None);
                    if (!handler.Received || response.Text != "こんにちは" || response.Source != "fr") throw new Exception("Azure fixture response mismatch");
                }
                if (!HotkeyChord.TryParse("Ctrl+Alt+D", out _) || HotkeyChord.TryParse("D", out _) || HotkeyChord.TryParse("Ctrl+Alt+F12", out _)) throw new Exception("Shortcut validation failed");
                using (var main = new HotkeyProbe())
                using (var reservation = new HotkeyProbe())
                using (var keys = new HotkeyManager(main.Handle))
                using (var reserved = new HotkeyManager(reservation.Handle))
                {
                    main.Show(); FocusTestWindow(main.Handle);
                    var active = configured with { HotkeysEnabled = true };
                    if (!keys.TryApply(active, out var error)) throw new Exception(error);
                    await PressChord(active.ReadHotkey); await PressChord(active.InputHotkey);
                    if (main.Reads != 1 || main.Writes != 1) throw new Exception("Registered shortcuts did not dispatch distinct actions: read=" + main.Reads + ", input=" + main.Writes + ", focused=" + (Native.GetForegroundWindow() == main.Handle) + ", modifiers=" + Control.ModifierKeys);
                    var occupied = active with { ReadHotkey = "Ctrl+Alt+Shift+F8", InputHotkey = "Ctrl+Alt+Shift+F9" };
                    if (!reserved.TryApply(occupied, out error)) throw new Exception(error);
                    if (keys.TryApply(active with { ReadHotkey = occupied.ReadHotkey }, out _)) throw new Exception("Conflicting shortcut accepted");
                    await PressChord(active.ReadHotkey);
                    if (main.Reads != 2) throw new Exception("Conflict did not restore original shortcut");
                    if (keys.TryApply(active with { InputHotkey = active.ReadHotkey }, out _)) throw new Exception("Duplicate shortcuts accepted");
                    if (!keys.TryApply(active with { HotkeysEnabled = false }, out error)) throw new Exception(error);
                    await PressChord(active.ReadHotkey); if (main.Reads != 2) throw new Exception("Disabled shortcut still active");
                    main.Close();
                }
                using (var owner = new Form())
                using (var keys = new HotkeyManager(owner.Handle))
                {
                    keys.Suspend();
                    using var settings = new SettingsWindow(keys); settings.Show(); Application.DoEvents();
                    FocusTestWindow(settings.Handle); settings.SelectSection(0);
                    var keyBoxes = Descendants(settings).OfType<TextBox>().Where(x => x.Name is "ReadHotkey" or "InputHotkey").ToDictionary(x => x.Name);
                    // Enable the draft controls without activating global registrations while editing.
                    Descendants(settings).OfType<CheckBox>().Single(x => x.Name == "ReadHotkeyEnabled").Checked = true;
                    Descendants(settings).OfType<CheckBox>().Single(x => x.Name == "InputHotkeyEnabled").Checked = true;
                    keyBoxes["ReadHotkey"].Focus(); await PressChord("Ctrl+Alt+Shift+F6");
                    settings.SelectSection(1); keyBoxes["InputHotkey"].Focus(); await PressChord("Ctrl+Alt+Shift+F7");
                    if (keyBoxes["ReadHotkey"].Text != "Ctrl+Alt+Shift+F6" || keyBoxes["InputHotkey"].Text != "Ctrl+Alt+Shift+F7") throw new Exception("Shortcut recording controls failed");
                    Descendants(settings).OfType<CheckBox>().Single(x => x.Name == "ReadHotkeyEnabled").Checked = false;
                    Descendants(settings).OfType<CheckBox>().Single(x => x.Name == "InputHotkeyEnabled").Checked = false;
                    settings.SelectSection(0);
                    var combos = Descendants(settings).OfType<ComboBox>().ToDictionary(x => x.Name);
                    combos["ReadTarget"].SelectedItem = Languages.Get("ja"); combos["InputTarget"].SelectedItem = Languages.Get("es");
                    combos["InputSourceLanguage"].SelectedItem = Languages.Get("fr");
                    if (UserPreferences.Current.ReadTarget != "fr") throw new Exception("Unsaved preview changed active preferences");
                    if (Descendants(settings).OfType<ScenarioPreview>().Where(x => !x.IsInput).Any(x => x.TargetCode != "ja")) throw new Exception("Selected-text previews did not update");
                    if (Descendants(settings).OfType<ScenarioPreview>().Single(x => x.IsInput).TargetCode != "es") throw new Exception("Input preview did not update");
                    Descendants(settings).OfType<Button>().Single(x => x.Text == "Save & apply").PerformClick();
                    if (!settings.IsDisposed || UserPreferences.Current.ReadTarget != "ja" || UserPreferences.Current.InputTarget != "es" || UserPreferences.Current.InputSourceLanguage != "fr" || UserPreferences.Current.ReadHotkey != "Ctrl+Alt+Shift+F6" || UserPreferences.Current.InputHotkey != "Ctrl+Alt+Shift+F7") throw new Exception("Settings UI did not save independent languages and recorded shortcuts");
                }
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "settings-test.txt"), "PASS: OS-language defaults; legacy Auto-read migration; independent saved targets; UI draft previews; settings save; native shortcut dispatch, conflict rollback, duplicate rejection and disable; Windows per-user secret encryption; Azure auto-detection request/response fixture. No live Azure request was made.");
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "settings-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
            finally { CultureInfo.CurrentUICulture = originalCulture; if (File.Exists(path)) File.Delete(path); host.Close(); }
        };
        Application.Run(host);
    }
    public static void SaveSettingsPreview()
    {
        using var owner = new Form { Opacity = 0, ShowInTaskbar = false };
        using var keys = new HotkeyManager(owner.Handle);
        using var settings = new SettingsWindow(keys);
        var work = Screen.PrimaryScreen!.WorkingArea;
        using var background = new Form { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, TopMost = true, BackColor = Color.FromArgb(237, 241, 248), StartPosition = FormStartPosition.Manual,
            Size = new Size(settings.Width + 24, settings.Height + 24), Location = new Point(work.Left + (work.Width - settings.Width - 24) / 2, work.Top + (work.Height - settings.Height - 24) / 2) };
        background.Show(); settings.Owner = background; settings.TopMost = true; settings.StartPosition = FormStartPosition.Manual; settings.Location = new Point(background.Left + 12, background.Top + 12);
        settings.Show(); FocusTestWindow(settings.Handle); Application.DoEvents(); DwmFlush();
        void Capture(string name)
        {
            Application.DoEvents(); DwmFlush();
            using var image = new Bitmap(settings.Width, settings.Height);
            try
            {
                using var graphics = Graphics.FromImage(image);
                graphics.CopyFromScreen(settings.Location, Point.Empty, settings.Size);
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 6)
            {
                // A disconnected desktop cannot be captured; render the controls directly.
                settings.DrawToBitmap(image, new Rectangle(Point.Empty, settings.Size));
            }
            image.Save(Path.Combine(AppContext.BaseDirectory, name));
        }
        Capture("settings-preview.png");
        var animationClock = System.Diagnostics.Stopwatch.StartNew();
        while (animationClock.Elapsed.TotalSeconds < 6.1) { Application.DoEvents(); Thread.Sleep(15); }
        Capture("settings-animation-preview.png");
        settings.SelectSection(1);
        animationClock.Restart(); while (animationClock.Elapsed.TotalSeconds < 5.2) { Application.DoEvents(); Thread.Sleep(15); }
        Capture("input-settings-preview.png"); Capture("shortcuts-preview.png");
        settings.SelectSection(2); Capture("translation-services-preview.png");
        var category = (ComboBox)settings.Controls.Find("ServiceCategory", true)[0];
        category.SelectedItem = category.Items.Cast<ServiceChoice>().First(c => c.Code == "Azure"); Capture("azure-services-preview.png");
        category.SelectedItem = category.Items.Cast<ServiceChoice>().First(c => c.Code == "MyMemory");
        ((TextBox)settings.Controls.Find("MyMemoryEmail", true)[0]).UseSystemPasswordChar = true; Capture("mymemory-services-preview.png");
        settings.SelectSection(3); Capture("offline-dictionary-preview.png");
        settings.SelectSection(4); Capture("general-settings-preview.png");
        settings.SelectSection(5); Capture("software-update-preview.png");
        settings.SelectSection(6); Capture("donation-preview.png"); settings.Close(); background.Close();
    }
}
