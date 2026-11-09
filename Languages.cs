using System.Globalization;

namespace EnglishHelper;

sealed record AppLanguage(string Code, string Name, string Hello, string Example)
{
    public override string ToString() => Name;
}

static class Languages
{
    public static readonly AppLanguage[] All =
    [
        new("zh-CN", "简体中文", "你好", "你好，今天方便聊聊吗？"),
        new("zh-TW", "繁體中文", "你好", "你好，今天方便聊聊嗎？"),
        new("en", "English", "Hello", "Hi, do you have a moment to chat today?"),
        new("ja", "日本語", "こんにちは", "こんにちは、今日は少しお話しできますか？"),
        new("ko", "한국어", "안녕하세요", "안녕하세요, 오늘 잠깐 이야기할 수 있나요?"),
        new("fr", "Français", "Bonjour", "Bonjour, avez-vous un moment pour discuter aujourd’hui ?"),
        new("de", "Deutsch", "Hallo", "Hallo, haben Sie heute kurz Zeit für ein Gespräch?"),
        new("es", "Español", "Hola", "Hola, ¿tienes un momento para hablar hoy?"),
        new("it", "Italiano", "Ciao", "Ciao, hai un momento per parlare oggi?"),
        new("pt", "Português", "Olá", "Olá, tem um momento para conversar hoje?"),
        new("ru", "Русский", "Здравствуйте", "Здравствуйте, у вас есть минутка поговорить сегодня?"),
        new("uk", "Українська", "Вітаю", "Вітаю, чи маєте сьогодні хвилинку поговорити?"),
        new("ar", "العربية", "مرحبًا", "مرحبًا، هل لديك وقت قصير للدردشة اليوم؟"),
        new("hi", "हिन्दी", "नमस्ते", "नमस्ते, क्या आज आपके पास बात करने के लिए थोड़ा समय है?"),
        new("th", "ไทย", "สวัสดี", "สวัสดี วันนี้มีเวลาคุยสักครู่ไหม?"),
        new("vi", "Tiếng Việt", "Xin chào", "Xin chào, hôm nay bạn có chút thời gian trò chuyện không?"),
        new("id", "Bahasa Indonesia", "Halo", "Halo, apakah Anda punya waktu untuk mengobrol hari ini?"),
        new("tr", "Türkçe", "Merhaba", "Merhaba, bugün konuşmak için biraz vaktiniz var mı?"),
        new("nl", "Nederlands", "Hallo", "Hallo, heb je vandaag even tijd om te praten?"),
        new("pl", "Polski", "Cześć", "Cześć, masz dzisiaj chwilę na rozmowę?")
    ];
    public static bool IsSupported(string? code) => All.Any(x => x.Code == code);
    public static AppLanguage Get(string code) => All.FirstOrDefault(x => x.Code == code) ?? All[2];
    public static string SystemLanguage => Match(CultureInfo.CurrentUICulture.Name);
    public static string Match(string code)
    {
        if (code.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return code.Contains("TW", StringComparison.OrdinalIgnoreCase) || code.Contains("HK", StringComparison.OrdinalIgnoreCase) || code.Contains("Hant", StringComparison.OrdinalIgnoreCase) ? "zh-TW" : "zh-CN";
        return All.FirstOrDefault(x => x.Code == code.Split('-')[0])?.Code ?? "en";
    }
    // MyMemory has no detector: source guesses remain editable in the popup.
    public static string Guess(string text)
    {
        if (text.Any(c => c is >= '\u3040' and <= '\u30ff')) return "ja";
        if (text.Any(c => c is >= '\uac00' and <= '\ud7af')) return "ko";
        if (text.Any(Translator.IsChinese)) return "zh-CN";
        if (text.Any(c => "іїєґІЇЄҐ".Contains(c))) return "uk";
        if (text.Any(c => c is >= '\u0400' and <= '\u04ff')) return "ru";
        if (text.Any(c => c is >= '\u0600' and <= '\u06ff')) return "ar";
        if (text.Any(c => c is >= '\u0900' and <= '\u097f')) return "hi";
        if (text.Any(c => c is >= '\u0e00' and <= '\u0e7f')) return "th";
        return "en";
    }
}
