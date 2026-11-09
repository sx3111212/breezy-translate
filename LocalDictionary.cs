using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace EnglishHelper;

static partial class LocalDictionary
{
    public sealed record Entry(string Phonetic, string Translation);
    public sealed record ExpansionPack(string FileName, int EntryCount, string Error)
    {
        public bool IsLoaded => Error.Length == 0;
    }
    static IReadOnlyList<ExpansionPack> expansionPacks = Array.Empty<ExpansionPack>();
    public static IReadOnlyList<ExpansionPack> ExpansionPacks { get { _ = Count; return expansionPacks; } }
    static Lazy<Dictionary<string, Entry>> Entries = new(Load);
    static string? testFolder;
    public static string Folder => testFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EnglishHelper", "Dictionaries");
    public static string PackWarning { get; private set; } = "";
    public static int Count => Entries.Value.Count;
    public static void Reload() => Entries = new(Load);
    static readonly Regex Word = new("^[a-zA-Z]+(?:[-'][a-zA-Z]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public static string Normalize(string text) => text.Trim().Trim('.', ',', ':', ';', '!', '?', '"', '“', '”', '(', ')', '[', ']').ToLowerInvariant();
    public static bool IsWord(string text) => Word.IsMatch(Normalize(text));
    public static bool Contains(string text) => Entries.Value.ContainsKey(Normalize(text));
    public static string Lookup(string text)
    {
        var entry = GetEntry(text);
        return (string.IsNullOrWhiteSpace(entry.Phonetic) ? "" : "/" + entry.Phonetic + "/" + Environment.NewLine + Environment.NewLine) + entry.Translation.Replace("\\n", Environment.NewLine);
    }
    public static Entry GetEntry(string text)
    {
        var word = Normalize(text);
        if (!Entries.Value.TryGetValue(word, out var entry))
            throw new Exception("本地词典未收录这个单词。请检查拼写，或放入完整句子中翻译。未使用翻译记忆库猜测词义。");
        return entry;
    }
    static Dictionary<string, Entry> Load()
    {
        using var stream = typeof(LocalDictionary).Assembly.GetManifestResourceStream("EnglishHelper.Dictionary.english-zh.json.gz")
            ?? throw new Exception("本地词典文件缺失，请重新安装应用。");
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        var entries = JsonSerializer.Deserialize<Dictionary<string, Entry>>(gzip) ?? throw new Exception("无法读取本地词典。");
        PackWarning = "";
        var packs = new List<ExpansionPack>();
        if (Directory.Exists(Folder))
            foreach (var path in Directory.GetFiles(Folder, "*.json.gz").OrderBy(p => p, StringComparer.Ordinal))
                try
                {
                    var pack = ReadPack(path);
                    foreach (var entry in pack) entries[entry.Key] = entry.Value;
                    packs.Add(new(Path.GetFileName(path), pack.Count, ""));
                }
                catch (Exception ex)
                {
                    packs.Add(new(Path.GetFileName(path), 0, ex.Message));
                    PackWarning += Path.GetFileName(path) + ": " + ex.Message + Environment.NewLine;
                }
        expansionPacks = packs.AsReadOnly();
        return entries;
    }
    public static void DeletePack(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName ||
            !ExpansionPacks.Any(p => string.Equals(p.FileName, fileName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("请选择已安装的扩展包 / Select an installed expansion pack.");
        File.Delete(Path.Combine(Folder, fileName));
        Reload();
        _ = Count;
    }
    internal static Dictionary<string, Entry> ReadPack(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        var entries = JsonSerializer.Deserialize<Dictionary<string, Entry>>(gzip);
        if (entries == null || entries.Count == 0 || entries.Any(e => e.Key != Normalize(e.Key) || !IsWord(e.Key) || e.Value == null || string.IsNullOrWhiteSpace(e.Value.Translation)))
            throw new InvalidDataException("无效的英汉词典包 / Invalid English–Chinese dictionary pack.");
        return entries;
    }
    public static int Install(string source)
    {
        Directory.CreateDirectory(Folder);
        var name = Path.GetFileNameWithoutExtension(source);
        if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) name = name[..^5];
        var destination = Path.Combine(Folder, name + ".json.gz");
        var staging = Path.Combine(Folder, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            if (source.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) Build(source, staging, compact: false);
            else File.Copy(source, staging);
            int count = ReadPack(staging).Count;
            File.Move(staging, destination, overwrite: true);
            Reload();
            return count;
        }
        finally { if (File.Exists(staging)) File.Delete(staging); }
    }
    public static int Build(string source, string destination, bool compact = true)
    {
        using var csv = new TextFieldParser(source, Encoding.UTF8) { HasFieldsEnclosedInQuotes = true };
        csv.SetDelimiters(",");
        var header = csv.ReadFields() ?? throw new Exception("Dictionary header missing");
        int At(string name) => Array.IndexOf(header, name);
        int word = At("word"), phonetic = At("phonetic"), translation = At("translation"), bnc = At("bnc"), frq = At("frq"), collins = At("collins"), tag = At("tag");
        if (word < 0 || phonetic < 0 || translation < 0 || (compact && new[] { bnc, frq, collins, tag }.Any(i => i < 0))) throw new InvalidDataException("缺少 ECDICT CSV 字段 / Missing ECDICT CSV columns.");
        var result = new Dictionary<string, Entry>(StringComparer.Ordinal);
        while (!csv.EndOfData)
        {
            var row = csv.ReadFields();
            if (row == null || row.Length != header.Length) continue;
            string key = row[word].Trim().ToLowerInvariant();
            if (!Word.IsMatch(key) || string.IsNullOrWhiteSpace(row[translation])) continue;
            bool Ranked(int column, int max) => int.TryParse(row[column], out int rank) && rank > 0 && rank <= max;
            if (compact && !Ranked(bnc, 50000) && !Ranked(frq, 50000) && !Ranked(collins, 10) && string.IsNullOrWhiteSpace(row[tag])) continue;
            result.TryAdd(key, new Entry(row[phonetic], row[translation]));
        }
        using var file = File.Create(destination);
        using var gzip = new GZipStream(file, CompressionLevel.SmallestSize);
        JsonSerializer.Serialize(gzip, result);
        return result.Count;
    }
    public static void Test()
    {
        foreach (var word in new[] { "generation", "Generation.", "hello", "memory", "translate", "computer", "language" })
            if (!Lookup(word).Any(Translator.IsChinese)) throw new Exception("Missing Chinese definition: " + word);
        var generation = Lookup("generation");
        if (!generation.Contains('代') || generation.Contains("打出这样的球")) throw new Exception("Generation regression");
        if (Translator.Translate("generation", false).GetAwaiter().GetResult() != generation) throw new Exception("Offline lookup routing regression");
        if (IsWord("a whole sentence") || IsWord("你好")) throw new Exception("Sentence routing regression");
        bool missingRejected = false;
        try { Lookup("qzxqzxunknown"); } catch { missingRejected = true; }
        if (!missingRejected) throw new Exception("Unknown word must not be guessed");
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "dictionary-test.txt"), $"PASS: {Entries.Value.Count} offline entries; case/punctuation normalization; word/sentence routing; missing word rejection.\n\ngeneration\n{generation}");
    }
}
