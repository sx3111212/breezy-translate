using System.Globalization;
using System.Runtime.InteropServices;

namespace EnglishHelper;

interface ISpeechOutput : IDisposable
{
    void Speak(string text);
    void Stop();
}
interface ILanguageSpeechOutput : ISpeechOutput { void Speak(string text, string language); }

sealed class EnglishSpeech(int volume = 100) : ILanguageSpeechOutput
{
    dynamic? voice;
    string voiceLanguage = "";
    void Initialize(string requestedLanguage)
    {
        string primary = requestedLanguage.Split('-')[0];
        if (voice != null && voiceLanguage == primary) return;
        if (voice != null) Dispose();
        dynamic candidate = Activator.CreateInstance(Type.GetTypeFromProgID("SAPI.SpVoice") ?? throw new Exception("未找到 Windows 语音组件。"))!;
        try
        {
            dynamic tokens = candidate.GetVoices();
            try
            {
                for (int index = 0; index < tokens.Count; index++)
                {
                    dynamic token = tokens.Item(index);
                    try
                    {
                        string language = token.GetAttribute("Language");
                        bool matches = language.Split(';').Any(x =>
                        {
                            if (!int.TryParse(x, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int id)) return false;
                            try { return CultureInfo.GetCultureInfo(id).TwoLetterISOLanguageName == primary; } catch { return false; }
                        });
                        if (!matches) continue;
                        candidate.Voice = token; candidate.Volume = volume; voice = candidate; voiceLanguage = primary; return;
                    }
                    finally { Marshal.FinalReleaseComObject(token); }
                }
                throw new Exception(UiText.T("missingVoice") + " " + Languages.Get(requestedLanguage).Name);
            }
            finally { Marshal.FinalReleaseComObject(tokens); }
        }
        catch { Marshal.FinalReleaseComObject(candidate); throw; }
    }
    public void Speak(string text) => Speak(text, "en");
    public void Speak(string text, string language) { Initialize(language); voice!.Speak(text, 19); }
    public void Stop() { if (voice != null) voice.Speak("", 3); }
    public void Dispose()
    {
        if (voice == null) return;
        try { Stop(); } finally { Marshal.FinalReleaseComObject(voice); voice = null; }
    }
}
