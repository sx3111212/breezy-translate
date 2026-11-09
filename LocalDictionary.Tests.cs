using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace EnglishHelper;

static partial class LocalDictionary
{
    public static void TestPacks()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "pack-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        testFolder = Path.Combine(root, "installed"); Reload();
        try
        {
            int baseline = Count;
            string source = Path.Combine(root, "fixture.csv");
            File.WriteAllText(source, "word,phonetic,translation,bnc,frq,collins,tag\nqzxrareword,,罕见词,0,0,0,\nhello,,你好（扩展）,1,1,1,\n");
            string compact = Path.Combine(root, "compact.json.gz"), full = Path.Combine(root, "fixture.json.gz");
            if (Build(source, compact) != 1 || Build(source, full, compact: false) != 2) throw new Exception("Frequency filtering regression");
            if (Install(full) != 2 || !Lookup("Qzxrareword.").Contains("罕见词") || Lookup("hello") != "你好（扩展）") throw new Exception("Pack merge/normalization regression");
            if (Count != baseline + 1) throw new Exception("Duplicate entries must merge");
            byte[] installed = File.ReadAllBytes(Path.Combine(Folder, "fixture.json.gz"));
            File.WriteAllText(full, "corrupted");
            bool rejected = false;
            try { Install(full); } catch { rejected = true; }
            if (!rejected || !File.ReadAllBytes(Path.Combine(Folder, "fixture.json.gz")).SequenceEqual(installed)) throw new Exception("Invalid replacement damaged installed pack");
            using (var file = File.Create(full))
            using (var gzip = new GZipStream(file, CompressionLevel.SmallestSize))
                JsonSerializer.Serialize(gzip, new Dictionary<string, Entry> { ["hello"] = new("", "你好（更新）") });
            Install(full);
            if (Contains("qzxrareword") || Lookup("hello") != "你好（更新）") throw new Exception("Pack replacement reload failed");
            Install(source);
            if (!Contains("qzxrareword")) throw new Exception("CSV import failed");
            File.WriteAllText(Path.Combine(Folder, "broken.json.gz"), "invalid");
            Reload();
            if (!Contains("hello") || PackWarning.Length == 0) throw new Exception("Corrupt pack must retain base dictionary and show warning");
            if (ExpansionPacks.Count != 2 || !ExpansionPacks.Single(p => p.FileName == "fixture.json.gz").IsLoaded ||
                ExpansionPacks.Single(p => p.FileName == "fixture.json.gz").EntryCount != 2 || ExpansionPacks.Single(p => p.FileName == "broken.json.gz").IsLoaded)
                throw new Exception("Pack metadata does not describe loaded and failed packages");
            using (var panel = new DictionarySettingsPanel())
            {
                var list = (ListView)panel.Controls.Find("DictionaryPacks", true).Single();
                if (list.Items.Count != 2 || list.Items.Cast<ListViewItem>().Any(i => i.Tag is not ExpansionPack))
                    throw new Exception("Dictionary package UI did not show installed packages");
            }
            bool invalidDeleteRejected = false;
            try { DeletePack("../fixture.json.gz"); } catch (InvalidOperationException) { invalidDeleteRejected = true; }
            if (!invalidDeleteRejected || !File.Exists(Path.Combine(Folder, "fixture.json.gz"))) throw new Exception("Deletion accepted a path outside the pack list");
            DeletePack("broken.json.gz");
            if (PackWarning.Length > 0 || ExpansionPacks.Count != 1) throw new Exception("Failed pack deletion did not refresh metadata");
            DeletePack("fixture.json.gz");
            if (Count != baseline || Contains("qzxrareword") || ExpansionPacks.Count != 0) throw new Exception("Pack deletion did not restore built-in entries");
            foreach (var path in Directory.GetFiles(Folder)) File.Delete(path);
            Reload();
            if (Count != baseline || Contains("qzxrareword")) throw new Exception("Manual removal reload failed");
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "dictionary-packs-test.txt"), "PASS: full/compact build; pack merge and normalization; CSV import; invalid replacement preservation; loaded/failed package list and entry counts; invalid deletion path rejection; failed and loaded pack deletion; immediate built-in fallback; reload after removal. Isolated fixtures, no network requests.");
        }
        finally { testFolder = null; Reload(); Directory.Delete(root, recursive: true); }
    }
}
