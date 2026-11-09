using System.IO;
using System.Net;
using System.Net.Http;

namespace EnglishHelper;

static partial class UiTest
{
    sealed class EmailTransport(string provider, string email = "") : HttpMessageHandler
    {
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var uri = request.RequestUri!;
            var parameters = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2)).ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p.Length == 2 ? p[1] : ""));
            string response;
            if (provider == "MyMemory")
            {
                if (uri.Host != "api.mymemory.translated.net" || request.Method != HttpMethod.Get ||
                    email.Length == 0 && parameters.ContainsKey("de") || email.Length > 0 && (!parameters.TryGetValue("de", out var supplied) || supplied != email))
                    throw new Exception("MyMemory contact parameter mismatch");
                if (email.Contains('+') && !uri.Query.Contains("%2B")) throw new Exception("Email plus sign was not encoded");
                response = "{\"responseStatus\":200,\"responseData\":{\"translatedText\":\"Bonjour.\"}}";
            }
            else
            {
                var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                if (parameters.ContainsKey("de") || uri.AbsoluteUri.Contains("example.invalid") && uri.Host != "ai.example.invalid" ||
                    body.Contains("reader+tag@example.invalid") || request.Headers.ToString().Contains("reader+tag@example.invalid"))
                    throw new Exception("Contact email leaked to another provider");
                response = provider == "Azure"
                    ? "[{\"detectedLanguage\":{\"language\":\"en\"},\"translations\":[{\"text\":\"Bonjour.\",\"to\":\"fr\"}]}]"
                    : "{\"choices\":[{\"finish_reason\":\"stop\",\"message\":{\"content\":\"Bonjour.\"}}]}";
            }
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) };
        }
    }
    public static void RunMemoryEmailTests()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "memory-email-" + Guid.NewGuid().ToString("N") + ".json");
        UseStandardPreferences(path);
        using var host = new Form { ShowInTaskbar = false, Opacity = 0 };
        host.Shown += async (_, _) =>
        {
            try
            {
                const string contact = "reader+tag@example.invalid";
                using (var transport = new EmailTransport("MyMemory"))
                using (var client = new HttpClient(transport))
                {
                    await MultilingualTranslator.Translate("Hello, friend.", "fr", "en", serviceClient: client);
                    if (transport.Calls != 1) throw new Exception("Anonymous translation failed");
                }
                void SelectMemory(SettingsWindow settings)
                {
                    settings.Show(); settings.SelectSection(2);
                    var selector = (ComboBox)settings.Controls.Find("ServiceCategory", true)[0];
                    selector.SelectedItem = selector.Items.Cast<ServiceChoice>().Single(c => c.Code == "MyMemory");
                }
                using (var keys = new HotkeyManager(host.Handle))
                using (var settings = new SettingsWindow(keys))
                {
                    SelectMemory(settings); ((TextBox)settings.Controls.Find("MyMemoryEmail", true)[0]).Text = contact; settings.Close();
                    if (UserPreferences.Current.MyMemoryEmail != "") throw new Exception("Cancelled email was saved");
                }
                using (var keys = new HotkeyManager(host.Handle))
                using (var settings = new SettingsWindow(keys))
                {
                    SelectMemory(settings); var email = (TextBox)settings.Controls.Find("MyMemoryEmail", true)[0];
                    var save = Descendants(settings).OfType<Button>().Single(b => b.Text == UiText.T("save"));
                    email.Text = "not-an-email"; save.PerformClick();
                    if (settings.IsDisposed || UserPreferences.Current.MyMemoryEmail != "" || !Descendants(settings).OfType<Label>().Any(l => l.Text == UiText.T("emailInvalid"))) throw new Exception("Invalid contact email accepted or error hidden");
                    email.Text = "  " + contact + "  "; save.PerformClick();
                    UserPreferences.ReloadForTest();
                    if (!settings.IsDisposed || UserPreferences.Current.MyMemoryEmail != contact) throw new Exception("Email save/reload failed");
                }
                using (var transport = new EmailTransport("MyMemory", contact))
                using (var client = new HttpClient(transport))
                {
                    await MultilingualTranslator.Translate("Hello, friend.", "fr", "en", writing: true, serviceClient: client);
                    await MultilingualTranslator.Translate(string.Concat(Enumerable.Repeat("Hello, friend. ", 90)), "fr", "en", serviceClient: client);
                    if (transport.Calls < 3) throw new Exception("Email was not checked across chunked translation requests");
                }
                UserPreferences.TrySave(UserPreferences.Current with { ProtectedAzureKey = SecretStorage.Protect("fixture-key") });
                using (var transport = new EmailTransport("Azure"))
                using (var client = new HttpClient(transport))
                    await MultilingualTranslator.Translate("Hello, friend.", "fr", "en", providerOverride: "Azure", serviceClient: client);
                using (var transport = new EmailTransport("AI"))
                using (var client = new HttpClient(transport))
                    await AiTranslation.Translate("Hello, friend.", "en", "fr", new AiProfile("fixture", "AI", "https://ai.example.invalid/v1", "fixture-model", SecretStorage.Protect("fixture-key")), client: client);
                using (var transport = new EmailTransport("MyMemory", contact))
                using (var client = new HttpClient(transport))
                {
                    await MultilingualTranslator.Translate("move", "zh-CN", serviceClient: client);
                    if (transport.Calls != 0) throw new Exception("Offline dictionary sent a contact email");
                }
                using (var keys = new HotkeyManager(host.Handle))
                using (var settings = new SettingsWindow(keys))
                {
                    SelectMemory(settings);
                    if (((TextBox)settings.Controls.Find("MyMemoryEmail", true)[0]).Text != contact) throw new Exception("Saved email not editable");
                    ((Button)settings.Controls.Find("ClearMyMemoryEmail", true)[0]).PerformClick();
                    if (UserPreferences.Current.MyMemoryEmail != contact) throw new Exception("Unsaved clear changed active email");
                    Descendants(settings).OfType<Button>().Single(b => b.Text == UiText.T("save")).PerformClick();
                    UserPreferences.ReloadForTest();
                    if (!settings.IsDisposed || UserPreferences.Current.MyMemoryEmail != "") throw new Exception("Cleared email persisted");
                }
                using (var transport = new EmailTransport("MyMemory"))
                using (var client = new HttpClient(transport))
                    await MultilingualTranslator.Translate("Hello, friend.", "fr", "en", serviceClient: client);
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "memory-email-test.txt"), "PASS: optional email validation, cancellation, save/reload, clear, anonymous omission, URL encoding and chunked requests; email excluded from Azure, AI and offline dictionary. Mock transport only; no contact email sent externally.");
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "memory-email-test.txt"), "FAIL: " + ex); Environment.ExitCode = 1; }
            finally { if (File.Exists(path)) File.Delete(path); host.Close(); }
        };
        Application.Run(host);
    }
}
