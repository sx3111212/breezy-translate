using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace EnglishHelper;

static partial class UiTest
{
    sealed class AiFixture : IDisposable
    {
        readonly HttpListener listener = new();
        readonly Task server;
        public readonly string Url;
        public readonly List<string> Requests = [];
        public AiFixture()
        {
            var portProbe = new TcpListener(IPAddress.Loopback, 0); portProbe.Start();
            int port = ((IPEndPoint)portProbe.LocalEndpoint).Port; portProbe.Stop();
            Url = "http://127.0.0.1:" + port + "/v1"; listener.Prefixes.Add("http://127.0.0.1:" + port + "/"); listener.Start();
            server = Task.Run(async () =>
            {
                try
                {
                    while (listener.IsListening)
                    {
                        var context = await listener.GetContextAsync();
                        string response;
                        if (context.Request.Url!.AbsolutePath == "/v1/models") response = "{\"data\":[{\"id\":\"model-a\"},{\"id\":\"model-b\"}]}";
                        else
                        {
                            using var reader = new StreamReader(context.Request.InputStream);
                            using var body = JsonDocument.Parse(await reader.ReadToEndAsync());
                            var model = body.RootElement.GetProperty("model").GetString()!;
                            var messages = body.RootElement.GetProperty("messages");
                            bool anthropic = context.Request.Url.AbsolutePath == "/v1/messages";
                            if (anthropic)
                            {
                                if (context.Request.Headers["x-api-key"] != "fixture-key" || context.Request.Headers["anthropic-version"] != "2023-06-01" ||
                                    messages.GetArrayLength() != 1 || messages[0].GetProperty("role").GetString() != "user" || !body.RootElement.TryGetProperty("system", out _) || body.RootElement.GetProperty("max_tokens").GetInt32() <= 0)
                                    throw new Exception("Claude native request/authentication mismatch");
                            }
                            else if (messages[0].GetProperty("role").GetString() != "system" || messages[1].GetProperty("role").GetString() != "user" || context.Request.Headers["Authorization"] != "Bearer fixture-key")
                                throw new Exception("AI request role/authentication mismatch");
                            lock (Requests) Requests.Add(model);
                            response = anthropic
                                ? JsonSerializer.Serialize(new { stop_reason = "end_turn", content = new[] { new { type = "thinking", text = "Hidden reasoning" }, new { type = "text", text = "译文：" + model } } })
                                : JsonSerializer.Serialize(new { choices = new[] { new { finish_reason = "stop", message = new { content = "译文：" + model } } } });
                        }
                        var bytes = Encoding.UTF8.GetBytes(response); context.Response.ContentType = "application/json";
                        await context.Response.OutputStream.WriteAsync(bytes); context.Response.Close();
                    }
                }
                catch (Exception) when (!listener.IsListening) { }
            });
        }
        public void Dispose() { listener.Stop(); listener.Close(); server.GetAwaiter().GetResult(); }
    }
    sealed class AiErrorFixture(string response, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(response) });
    }
    sealed class MemoryFixture : HttpMessageHandler
    {
        public bool Received;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.Host != "api.mymemory.translated.net" || !Uri.UnescapeDataString(request.RequestUri.Query).Contains("langpair=en|fr")) throw new Exception("Sentence MyMemory request mismatch");
            Received = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"responseStatus\":200,\"responseData\":{\"translatedText\":\"Bonjour, mon ami.\"}}") });
        }
    }
    public static void RunAiTests()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "ai-test-" + Guid.NewGuid().ToString("N") + ".json");
        UseStandardPreferences(path);
        using var host = new Form { ShowInTaskbar = false, Opacity = 0 };
        host.Shown += async (_, _) =>
        {
            try
            {
                using var fixture = new AiFixture();
                var a = new AiProfile("a", "Service A", fixture.Url, "model-a", SecretStorage.Protect("fixture-key"));
                var b = a with { Id = "b", Name = "Service B", Model = "model-b" };
                var claude = a with { Id = "claude", Name = "Claude fixture", Protocol = "Anthropic" };
                if ((await AiTranslation.Translate("Hello", "auto", "zh-CN", claude)).Text != "译文：model-a") throw new Exception("Claude native response parsing failed");
                if ((await AiTranslation.Models(claude)).Length != 2) throw new Exception("Claude model-list parsing failed");
                if (AiTranslation.Endpoint(fixture.Url + "/messages", "models").AbsolutePath != "/v1/models") throw new Exception("Claude endpoint normalization failed");
                foreach (var name in new[] { "Kimi", "Gemini", "Claude", "豆包", "腾讯混元", "Ollama" })
                    if (!AiPreset.All.Any(p => p.Name.Contains(name))) throw new Exception("Missing requested provider preset: " + name);
                if (AiTranslation.Endpoint(fixture.Url + "/chat/completions", "models").AbsolutePath != "/v1/models") throw new Exception("Endpoint normalization failed");
                foreach (var bad in new[] { "http://example.com/v1", "https://user:pass@example.com", "https://example.com?key=secret", "file:///C:/api" })
                {
                    bool rejected = false; try { AiTranslation.Endpoint(bad, "models"); } catch { rejected = true; }
                    if (!rejected) throw new Exception("Unsafe URL accepted");
                }
                if ((await AiTranslation.Models(a)).Length != 2) throw new Exception("Model listing failed");
                if (!UserPreferences.TrySave(UserPreferences.Current with { Provider = "AI", ReadProvider = "AI", ReadSentenceProvider = "AI", AiProfiles = [a, b], DefaultAiProfileId = b.Id })) throw new Exception("AI profile save failed");
                UserPreferences.ReloadForTest();
                if (UserPreferences.Current.Provider != "AI" || AiTranslation.Default?.Id != b.Id || File.ReadAllText(path).Contains("fixture-key")) throw new Exception("Default or encrypted persistence failed");
                if ((await MultilingualTranslator.Translate("Hello", "zh-CN")).Text != "译文：model-b") throw new Exception("Default routing failed");
                if ((await MultilingualTranslator.Translate("Hello", "zh-CN", aiProfile: a)).Text != "译文：model-a" || AiTranslation.Default?.Id != b.Id) throw new Exception("Per-request override leaked into default");
                UserPreferences.TrySave(UserPreferences.Current with { ReadProvider = "Offline", InputProvider = "AI" });
                int beforeOffline; lock (fixture.Requests) beforeOffline = fixture.Requests.Count;
                if ((await MultilingualTranslator.Translate("move", "zh-CN")).Service != "ECDICT") throw new Exception("Reading offline override ignored");
                if (fixture.Requests.Count != beforeOffline) throw new Exception("Offline word sent network request");
                bool offlineRejected = false;
                try { await MultilingualTranslator.Translate("A full sentence.", "zh-CN", providerOverride: "Offline"); } catch (Exception ex) when (ex.Message == UiText.T("offlineLimit")) { offlineRejected = true; }
                if (!offlineRejected || fixture.Requests.Count != beforeOffline) throw new Exception("Unsupported offline request silently used AI");
                UserPreferences.TrySave(UserPreferences.Current with { ReadSentenceProvider = "MyMemory" });
                using (var memory = new MemoryFixture())
                using (var client = new HttpClient(memory))
                {
                    var result = await MultilingualTranslator.Translate("Hello, my friend.", "fr", "en", serviceClient: client);
                    if (!memory.Received || result.Service != "MyMemory" || result.Text != "Bonjour, mon ami.") throw new Exception("Sentence MyMemory selection ignored");
                }
                UserPreferences.TrySave(UserPreferences.Current with { ReadSentenceProvider = "Azure", ProtectedAzureKey = SecretStorage.Protect("fixture-azure-key"), AzureRegion = "test-region" });
                using (var azure = new AzureFixture())
                using (var client = new HttpClient(azure))
                {
                    var result = await MultilingualTranslator.Translate("Bonjour, mon ami.", "ja", serviceClient: client);
                    if (!azure.Received || result.Service != "Azure Translator" || result.Text != "こんにちは") throw new Exception("Sentence Azure selection ignored");
                }
                UserPreferences.TrySave(UserPreferences.Current with { ReadSentenceProvider = "AI" });
                if ((await MultilingualTranslator.Translate("Hello", "zh-CN", writing: true)).Text != "译文：model-b") throw new Exception("Input AI override ignored");
                UserPreferences.TrySave(UserPreferences.Current with { ReadProvider = "AI", InputProvider = "Offline" });
                UserPreferences.ReloadForTest();
                if ((await MultilingualTranslator.Translate("move", "zh-CN")).Text != "译文：model-b" || (await MultilingualTranslator.Translate("move", "zh-CN", writing: true)).Text != "译文：model-b") throw new Exception("Legacy input offline setting did not migrate to AI");
                UserPreferences.TrySave(UserPreferences.Current with { ReadProvider = "AI", InputProvider = "AI", ReadWordAiProfileId = a.Id, ReadSentenceAiProfileId = b.Id, InputAiProfileId = a.Id });
                if ((await MultilingualTranslator.Translate("move", "zh-CN")).Text != "译文：model-a" || (await MultilingualTranslator.Translate("A full sentence.", "zh-CN")).Text != "译文：model-b" ||
                    (await MultilingualTranslator.Translate("Hello", "zh-CN", writing: true)).Text != "译文：model-a") throw new Exception("Per-feature model selection ignored");
                UserPreferences.TrySave(UserPreferences.Current with { ReadWordAiProfileId = "", ReadSentenceAiProfileId = "", InputAiProfileId = "" });
                using (var panel = new AiSettingsPanel(UserPreferences.Current))
                {
                    var model = (ComboBox)panel.Controls.Find("AiModel", true)[0]; model.Text = "edited-model";
                    ((ComboBox)panel.Controls.Find("AiProfiles", true)[0]).SelectedIndex = 0;
                    var exported = panel.Export(true);
                    if (exported.Profiles.Single(p => p.Id == b.Id).Model != "edited-model" || AiTranslation.Default?.Model != "model-b") throw new Exception("Settings draft isolation failed");
                    ((TextBox)panel.Controls.Find("AiBaseUrl", true)[0]).Text = fixture.Url + "/changed";
                    if (panel.Export(true).Profiles.Single(p => p.Id == a.Id).ProtectedKey.Length != 0) throw new Exception("Credential followed changed endpoint");
                    ((ComboBox)panel.Controls.Find("AiPreset", true)[0]).SelectedItem = AiPreset.All.Single(p => p.Name == "Claude");
                    ((TextBox)panel.Controls.Find("AiApiKey", true)[0]).Text = "fixture-key"; model.Text = "fixture-model";
                    if (panel.Export(true).Profiles.Single(p => p.Id == a.Id).Protocol != "Anthropic") throw new Exception("Claude preset protocol not saved");
                }
                using (var errorClient = new HttpClient(new AiErrorFixture("secret response fixture-key", HttpStatusCode.Unauthorized)))
                {
                    try { await AiTranslation.Translate("Hello", "auto", "zh-CN", a, client: errorClient); throw new Exception("HTTP error accepted"); }
                    catch (Exception ex) when (ex.Message.Contains("HTTP 401") && !ex.Message.Contains("fixture-key")) { }
                }
                using (var truncatedClient = new HttpClient(new AiErrorFixture("{\"choices\":[{\"finish_reason\":\"length\",\"message\":{\"content\":\"partial\"}}]}")))
                {
                    bool rejected = false; try { await AiTranslation.Translate("Hello", "auto", "zh-CN", a, client: truncatedClient); } catch { rejected = true; }
                    if (!rejected) throw new Exception("Truncated result accepted");
                }
                using (var truncatedClient = new HttpClient(new AiErrorFixture("{\"stop_reason\":\"max_tokens\",\"content\":[{\"type\":\"text\",\"text\":\"partial\"}]}")))
                {
                    bool rejected = false; try { await AiTranslation.Translate("Hello", "auto", "zh-CN", claude, client: truncatedClient); } catch { rejected = true; }
                    if (!rejected) throw new Exception("Truncated Claude response accepted");
                }
                using (var keys = new HotkeyManager(host.Handle))
                using (var settings = new SettingsWindow(keys))
                {
                    settings.Show(); settings.SelectSection(2); FocusTestWindow(settings.Handle); await Task.Delay(120); DwmFlush();
                    using var image = new Bitmap(settings.Width, settings.Height);
                    using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(settings.Location, Point.Empty, settings.Size);
                    image.Save(Path.Combine(AppContext.BaseDirectory, "ai-settings-preview.png")); settings.Close();
                }
                using (var keys = new HotkeyManager(host.Handle))
                using (var settings = new SettingsWindow(keys))
                {
                    settings.Show(); settings.SelectSection(0); FocusTestWindow(settings.Handle);
                    var readPage = settings.Controls.Find("ReadTarget", true)[0].Parent;
                    var inputPage = settings.Controls.Find("InputTarget", true)[0].Parent;
                    if (readPage == inputPage || settings.Controls.Find("ReadHotkey", true)[0].Parent != readPage || settings.Controls.Find("ReadProvider", true)[0].Parent != readPage ||
                        settings.Controls.Find("InputHotkey", true)[0].Parent != inputPage || settings.Controls.Find("InputProvider", true)[0].Parent != inputPage)
                        throw new Exception("Feature settings are not grouped together");
                    var readingService = (ComboBox)settings.Controls.Find("ReadProvider", true)[0];
                    readingService.SelectedItem = readingService.Items.Cast<ServiceChoice>().Single(c => c.Code == "Offline");
                    var writingService = (ComboBox)settings.Controls.Find("InputProvider", true)[0];
                    writingService.SelectedItem = writingService.Items.Cast<ServiceChoice>().Single(c => c.Code == "AI");
                    var sentenceService = (ComboBox)settings.Controls.Find("SentenceProvider", true)[0];
                    if (!sentenceService.Items.Cast<ServiceChoice>().Select(c => c.Code).SequenceEqual(writingService.Items.Cast<ServiceChoice>().Select(c => c.Code)) || readingService.Items.Cast<ServiceChoice>().Any(c => c.Code is "MyMemory" or "Azure")) throw new Exception("Sentence and input service lists differ");
                    sentenceService.SelectedItem = sentenceService.Items.Cast<ServiceChoice>().Single(c => c.Code == "ai:a");
                    if (UserPreferences.Current.ReadProvider != "AI") throw new Exception("Draft operation service leaked");
                    Descendants(settings).OfType<Button>().Single(x => x.Text == UiText.T("save")).PerformClick();
                    if (!settings.IsDisposed || TranslationServices.Resolve(false, text: "move") != "Offline" || TranslationServices.Resolve(false, text: "A sentence.") != "AI" || TranslationServices.Resolve(true) != "AI" || UserPreferences.Current.ReadSentenceAiProfileId != "a") throw new Exception("Settings operation service save failed");
                }
                foreach (var code in new[] { "MyMemory", "Azure" })
                {
                    using var keys = new HotkeyManager(host.Handle); using var settings = new SettingsWindow(keys);
                    settings.Show(); settings.SelectSection(0);
                    var selector = (ComboBox)settings.Controls.Find("SentenceProvider", true)[0];
                    selector.SelectedItem = selector.Items.Cast<ServiceChoice>().Single(c => c.Code == code);
                    var general = settings.Controls.Find("InterfaceLanguage", true)[0].Parent!;
                    if (Descendants(general).OfType<TextBox>().Any() || Descendants(general).OfType<ComboBox>().Count() != 1) throw new Exception("General settings contains service configuration");
                    settings.SelectSection(2);
                    var category = (ComboBox)settings.Controls.Find("ServiceCategory", true)[0];
                    category.SelectedItem = category.Items.Cast<ServiceChoice>().Single(c => c.Code == "Azure");
                    var azureKey = settings.Controls.Find("AzureApiKey", true)[0];
                    if (!azureKey.Visible || azureKey.Parent!.Parent == general || settings.Controls.Find("AiModel", true)[0].Visible) throw new Exception("Azure not grouped in translation services");
                    Descendants(settings).OfType<Button>().Single(x => x.Text == UiText.T("save")).PerformClick();
                    UserPreferences.ReloadForTest();
                    if (!settings.IsDisposed || UserPreferences.Current.ReadSentenceProvider != code || TranslationServices.Resolve(false, text: "A sentence.") != code) throw new Exception("Sentence service selection did not persist");
                }
                UserPreferences.TrySave(UserPreferences.Current with { ReadProvider = "AI", InputProvider = "AI", ReadSentenceAiProfileId = "", ReadSentenceProvider = "AI" });
                using (var main = new HotkeyProbe())
                using (var reserve = new HotkeyProbe())
                using (var third = new HotkeyProbe())
                using (var activeKeys = new HotkeyManager(main.Handle))
                using (var reservedKeys = new HotkeyManager(reserve.Handle))
                using (var thirdKeys = new HotkeyManager(third.Handle))
                {
                    var shortcuts = UserPreferences.Current with { HotkeysEnabled = true, ReadHotkeyEnabled = false, InputHotkeyEnabled = true,
                        ReadHotkey = "Ctrl+Alt+Shift+F8", InputHotkey = "Ctrl+Alt+Shift+F9" };
                    if (!activeKeys.TryApply(shortcuts, out var message)) throw new Exception(message);
                    var reserved = shortcuts with { ReadHotkeyEnabled = true, InputHotkeyEnabled = false };
                    if (!reservedKeys.TryApply(reserved, out message)) throw new Exception("Disabled reading shortcut was reserved: " + message);
                    if (activeKeys.TryApply(shortcuts with { ReadHotkeyEnabled = true }, out _)) throw new Exception("Reading shortcut conflict not detected");
                    var other = shortcuts with { ReadHotkeyEnabled = false };
                    if (thirdKeys.TryApply(other, out _)) throw new Exception("Input shortcut lost after reading conflict rollback");
                    if (!activeKeys.TryApply(shortcuts with { HotkeysEnabled = false }, out _) || !thirdKeys.TryApply(other, out _)) throw new Exception("Input shortcut not released when disabled");
                }
                foreach (var text in new[] { "move", "A sentence to translate." })
                {
                    using var popup = new ResultWindow(text, IntPtr.Zero, false, new RecordedSpeech(), false);
                    popup.Show(); FocusTestWindow(popup.Handle); await popup.Translate();
                    if (popup.ResultText != "译文：model-b") throw new Exception("Popup did not honor default AI");
                    ((Button)popup.Controls.Find("ResultAiChoice", true)[0]).PerformClick();
                    await Task.Delay(60);
                    if (popup.IsDisposed || popup.CurrentAiMenu == null) throw new Exception("Service menu dismissed popup");
                    var choice = popup.CurrentAiMenu.Items.OfType<ToolStripMenuItem>().Single(x => (x.Text ?? "").StartsWith("Service A")); choice.PerformClick(); popup.CurrentAiMenu?.Close();
                    await Task.Delay(60); await popup.Translate();
                    if (popup.IsDisposed || popup.ResultText != "译文：model-a" || AiTranslation.Default?.Id != "b") throw new Exception("Popup service override failed");
                    ((Button)popup.Controls.Find("ResultAiChoice", true)[0]).PerformClick();
                    var menu = popup.CurrentAiMenu!; menu.Items.OfType<ToolStripTextBox>().Single().Text = "manual-model";
                    menu.Items.OfType<ToolStripMenuItem>().Single(x => x.Name == "ApplyModel").PerformClick();
                    await Task.Delay(60); await popup.Translate();
                    if (popup.ResultText != "译文：manual-model") throw new Exception("Popup model override failed");
                    ((Button)popup.Controls.Find("ResultAiChoice", true)[0]).PerformClick();
                    var modelsMenu = popup.CurrentAiMenu!;
                    var load = modelsMenu.Items.OfType<ToolStripMenuItem>().Single(x => x.Text == UiText.T("aiLoad"));
                    load.ShowDropDown();
                    for (int attempt = 0; attempt < 50 && !load.Text!.StartsWith("2 models"); attempt++) await Task.Delay(20);
                    if (popup.IsDisposed || !load.Text!.StartsWith("2 models")) throw new Exception("Popup model listing failed");
                    load.DropDownItems.OfType<ToolStripMenuItem>().Single(x => x.Text == "model-b").PerformClick(); modelsMenu.Close();
                    await Task.Delay(60); await popup.Translate();
                    if (popup.ResultText != "译文：model-b") throw new Exception("Popup model-list selection failed");
                    if (text == "move" && string.IsNullOrWhiteSpace(popup.PhoneticText)) throw new Exception("Word IPA lost");
                    if (text == "move")
                    {
                        FocusTestWindow(popup.Handle); Application.DoEvents(); await Task.Delay(120); DwmFlush();
                        using var image = new Bitmap(popup.Width, popup.Height);
                        using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(popup.Location, Point.Empty, popup.Size);
                        image.Save(Path.Combine(AppContext.BaseDirectory, "ai-word-preview.png"));
                    }
                    ((Button)popup.Controls.Find("ResultAiChoice", true)[0]).PerformClick();
                    var offlineMenu = popup.CurrentAiMenu!;
                    if (text == "move")
                    {
                        offlineMenu.Items.OfType<ToolStripMenuItem>().Single(i => i.Text == UiText.T("offlineMode")).PerformClick(); offlineMenu.Close();
                        await Task.Delay(60); await popup.Translate();
                        if (popup.ResultText == "译文：model-b") throw new Exception("Popup offline selection ignored");
                    }
                    else
                    {
                        if (offlineMenu.Items.OfType<ToolStripMenuItem>().Any(i => i.Text == UiText.T("offlineMode")) || !offlineMenu.Items.OfType<ToolStripMenuItem>().Any(i => i.Text == "MyMemory") || !offlineMenu.Items.OfType<ToolStripMenuItem>().Any(i => i.Text == "Azure Translator")) throw new Exception("Sentence popup missing translation services");
                        offlineMenu.Close();
                    }
                    popup.Close();
                }
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "ai-test.txt"), "PASS: matching sentence/input service options; MyMemory/Azure/AI sentence routing and persistence; unified translation service settings; general settings language only; independent shortcuts; word dictionary and AI profiles/model switching. Mock/local fixtures only; no paid API calls.");
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "ai-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
            finally { if (File.Exists(path)) File.Delete(path); host.Close(); }
        };
        Application.Run(host);
    }
}
