namespace EnglishHelper;

sealed record ServiceChoice(string Code, string Name)
{
    public override string ToString() => Name;
}
static class TranslationServices
{
    public static bool IsSupported(string value) => value is "MyMemory" or "Azure" or "AI" or "Offline";
    public static string Resolve(bool writing, AppPreferences? settings = null, string? text = null)
    {
        settings ??= UserPreferences.Current;
        if (!writing) return LocalDictionary.IsWord(text ?? "") ? settings.ReadProvider == "AI" ? "AI" : "Offline" : settings.ReadSentenceProvider;
        var operation = IsSupported(settings.InputProvider) ? settings.InputProvider : settings.Provider;
        return operation == "Offline" ? "AI" : operation;
    }
    public static AiProfile? Profile(bool writing, string text, AppPreferences? settings = null)
    {
        settings ??= UserPreferences.Current;
        var id = writing ? settings.InputAiProfileId : LocalDictionary.IsWord(text) ? settings.ReadWordAiProfileId : settings.ReadSentenceAiProfileId;
        if (string.IsNullOrEmpty(id)) id = settings.DefaultAiProfileId;
        return (settings.AiProfiles ?? []).FirstOrDefault(p => p.Id == id);
    }
    public static ServiceChoice[] Available(bool writing, bool word, string language, AiProfile[] profiles)
    {
        var choices = new List<ServiceChoice>();
        if (!writing && word) choices.Add(new("Offline", UiText.T("offlineMode", language)));
        choices.Add(new("AI", UiText.T("defaultAi", language)));
        choices.AddRange(profiles.Select(p => new ServiceChoice("ai:" + p.Id, p.ToString())));
        if (writing || !word) choices.AddRange(Configurable(language).Where(c => c.Code != "AI"));
        return choices.ToArray();
    }
    public static ServiceChoice[] Configurable(string language) => [new("AI", UiText.T("aiServices", language)), new("Azure", "Azure Translator"), new("MyMemory", "MyMemory")];
    public static ServiceChoice[] Choices(string language, bool inherit = false)
    {
        ServiceChoice[] choices = [new("Offline", UiText.T("offlineMode", language)), new("MyMemory", "MyMemory · " + UiText.T("online", language)),
            new("Azure", "Azure Translator"), new("AI", UiText.T("aiServices", language))];
        return inherit ? [new("", UiText.T("followService", language)), .. choices] : choices;
    }
    public static string Name(string code) => Choices(UserPreferences.Current.InterfaceLanguage).FirstOrDefault(c => c.Code == code)?.Name ?? code;
}
