using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace EnglishHelper;

sealed record TranslationResult(string Text, string Source, string Target, string Service);

static class MultilingualTranslator
{
    static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(20) };
    public static async Task<TranslationResult> Translate(string text, string target, string source = "auto", CancellationToken cancellation = default, AiProfile? aiProfile = null, bool useConfiguredService = true, bool writing = false, string? providerOverride = null, HttpClient? serviceClient = null)
    {
        cancellation.ThrowIfCancellationRequested();
        if (text.Length > 3000) throw new Exception("请每次选取不超过 3,000 个字符。");
        if (!Languages.IsSupported(target) || source != "auto" && !Languages.IsSupported(source)) throw new Exception("不支持所选语言。");
        if (source != "auto" && source == target) return new(text, source, target, "");
        var settings = UserPreferences.Current;
        var client = serviceClient ?? Client;
        var provider = providerOverride ?? (useConfiguredService ? TranslationServices.Resolve(writing, settings, text) : "MyMemory");
        if (!writing && LocalDictionary.IsWord(text) && provider is not ("AI" or "Offline")) provider = "Offline";
        if (aiProfile != null || provider == "AI")
            return await AiTranslation.Translate(text, source, target, aiProfile ?? TranslationServices.Profile(writing, text, settings) ?? throw new Exception(UiText.T("configureAiFirst")), cancellation);
        if (provider == "Offline")
        {
            source = source == "auto" ? Languages.Guess(text) : source;
            if (source == target) return new(text, source, target, UiText.T("offline"));
            if (source == "en" && target == "zh-CN" && LocalDictionary.IsWord(text)) return new(LocalDictionary.Lookup(text), source, target, "ECDICT");
            throw new Exception(UiText.T("offlineLimit"));
        }
        if (provider == "Azure")
        {
            var key = SecretStorage.Unprotect(settings.ProtectedAzureKey);
            if (string.IsNullOrWhiteSpace(key)) throw new Exception("请先在设置的翻译服务页填写 Azure 配置。");
            return await TranslateAzure(text, source, target, key, settings.AzureRegion, client, cancellation);
        }
        source = source == "auto" ? Languages.Guess(text) : source;
        if (source == target) return new(text, source, target, "MyMemory");
        if (source == "en" && target == "zh-CN" && LocalDictionary.IsWord(text)) return new(LocalDictionary.Lookup(text), source, target, "ECDICT");
        var email = MyMemoryContact.Validate(settings.MyMemoryEmail);
        var translated = new List<string>();
        foreach (var chunk in Translator.Chunks(text))
        {
            var url = "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(chunk) + "&langpair=" + Uri.EscapeDataString(source + "|" + target);
            if (email.Length > 0) url += "&de=" + Uri.EscapeDataString(email);
            using var response = await client.GetAsync(url, cancellation).ConfigureAwait(false); response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false));
            var root = json.RootElement;
            if (root.GetProperty("responseStatus").ToString() != "200") throw new Exception("当前翻译服务不可用、语言对不受支持或额度已用完。");
            var result = WebUtility.HtmlDecode(root.GetProperty("responseData").GetProperty("translatedText").GetString());
            if (string.IsNullOrWhiteSpace(result)) throw new Exception("服务未返回译文。"); translated.Add(result);
        }
        return new(string.Join(target.StartsWith("zh") || target is "ja" or "ko" ? "" : " ", translated), source, target, "MyMemory");
    }
    static string AzureCode(string code) => code switch { "zh-CN" => "zh-Hans", "zh-TW" => "zh-Hant", _ => code };
    internal static async Task<TranslationResult> TranslateAzure(string text, string source, string target, string key, string region, HttpClient client, CancellationToken cancellation)
    {
        var url = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=" + Uri.EscapeDataString(AzureCode(target));
        if (source != "auto") url += "&from=" + Uri.EscapeDataString(AzureCode(source));
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Ocp-Apim-Subscription-Key", key);
        if (!string.IsNullOrWhiteSpace(region)) request.Headers.Add("Ocp-Apim-Subscription-Region", region);
        request.Content = new StringContent(JsonSerializer.Serialize(new[] { new { Text = text } }), Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, cancellation).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) throw new Exception("Azure 翻译请求失败（HTTP " + (int)response.StatusCode + "），请检查配置及用量。");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation).ConfigureAwait(false));
        var item = json.RootElement[0]; var result = item.GetProperty("translations")[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(result)) throw new Exception("服务未返回译文。");
        if (item.TryGetProperty("detectedLanguage", out var detected)) source = Languages.Match(detected.GetProperty("language").GetString() ?? "en");
        return new(result, source, target, "Azure Translator");
    }
}
