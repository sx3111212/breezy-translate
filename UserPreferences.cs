using System.IO;
using System.Text.Json;

namespace EnglishHelper;

sealed record AppPreferences(bool AutoRead = false, string InterfaceLanguage = "", string ReadTarget = "", string InputTarget = "en", bool HotkeysEnabled = true,
    string ReadHotkey = "Ctrl+Alt+D", string InputHotkey = "Ctrl+Alt+E", string Provider = "MyMemory", string AzureRegion = "", string ProtectedAzureKey = "",
    AiProfile[]? AiProfiles = null, string DefaultAiProfileId = "", string ReadProvider = "", string InputProvider = "",
    string ReadWordAiProfileId = "", string ReadSentenceAiProfileId = "", string InputAiProfileId = "",
    bool ReadHotkeyEnabled = true, bool InputHotkeyEnabled = true, string ReadSentenceProvider = "MyMemory", string InputSourceLanguage = "auto", string MyMemoryEmail = "");

static class UserPreferences
{
    static string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnglishHelper", "settings.json");
    static AppPreferences? cached;
    public static event Action? Changed;
    public static AppPreferences Current => cached ??= Load();
    public static bool AutoRead => Current.AutoRead;
    static AppPreferences Load()
    {
        AppPreferences value;
        try { value = JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(settingsPath)) ?? new(InputSourceLanguage: Languages.SystemLanguage); }
        catch { value = new(InputSourceLanguage: Languages.SystemLanguage); }
        return Normalize(value);
    }
    static AppPreferences Normalize(AppPreferences value) => value with
    {
        InterfaceLanguage = string.IsNullOrWhiteSpace(value.InterfaceLanguage) ? UiText.SystemLanguage : UiText.Supports(value.InterfaceLanguage) ? value.InterfaceLanguage : "en",
        ReadTarget = Languages.IsSupported(value.ReadTarget) ? value.ReadTarget : Languages.SystemLanguage,
        InputTarget = Languages.IsSupported(value.InputTarget) ? value.InputTarget : "en",
        Provider = TranslationServices.IsSupported(value.Provider) ? value.Provider : "MyMemory",
        ReadProvider = value.ReadProvider == "AI" || string.IsNullOrEmpty(value.ReadProvider) && value.Provider == "AI" ? "AI" : "Offline",
        InputProvider = TranslationServices.IsSupported(value.InputProvider) ? value.InputProvider : "",
        ReadSentenceProvider = value.ReadSentenceProvider is "AI" or "Azure" or "MyMemory" ? value.ReadSentenceProvider : "MyMemory",
        InputSourceLanguage = value.InputSourceLanguage == "auto" || Languages.IsSupported(value.InputSourceLanguage) ? value.InputSourceLanguage : Languages.SystemLanguage,
        MyMemoryEmail = (value.MyMemoryEmail ?? "").Trim(),
        AiProfiles = value.AiProfiles ?? []
    };
    public static bool TrySave(AppPreferences value)
    {
        value = Normalize(value);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            var temporary = settingsPath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, settingsPath, true); cached = value;
        }
        catch { return false; }
        Changed?.Invoke(); return true;
    }
    public static bool SetAutoRead(bool enabled) => TrySave(Current with { AutoRead = enabled });
    internal static void UseTestFile(string path) { settingsPath = path; cached = null; }
    internal static void ReloadForTest() => cached = null;
}
