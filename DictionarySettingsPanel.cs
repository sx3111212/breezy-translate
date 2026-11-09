using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace EnglishHelper;

sealed class DictionarySettingsPanel : Panel
{
    readonly Label title = new(), status = new(), hint = new();
    readonly TextBox folder = new() { ReadOnly = true }, url = new();
    readonly RoundedButton open = new(), import = new(), download = new(), refresh = new(), delete = new();
    readonly ListView packs = new() { Name = "DictionaryPacks", View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, ShowItemToolTips = true };
    readonly CancellationTokenSource lifetime = new();
    string language = "en";
    bool busy;
    string T(string zh, string en) => language.StartsWith("zh") ? zh : en;

    public DictionarySettingsPanel()
    {
        Name = "DictionaryManagement";
        title.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        Add(title, 0, 0, 680, 30);
        Add(status, 0, 36, 680, 54); status.AutoEllipsis = true;
        packs.Columns.Add("", 380); packs.Columns.Add("", 130); packs.Columns.Add("", 140);
        Add(packs, 0, 98, 680, 132);
        Add(delete, 450, 238, 230, 34); delete.Name = "DeleteDictionaryPack"; delete.Enabled = false;
        packs.SelectedIndexChanged += (_, _) => delete.Enabled = !busy && packs.SelectedItems.Count == 1;
        delete.Click += (_, _) =>
        {
            if (busy || packs.SelectedItems.Count != 1 || packs.SelectedItems[0].Tag is not LocalDictionary.ExpansionPack pack) return;
            if (MessageBox.Show(this,
                T($"删除扩展包“{pack.FileName}”？删除后立即生效，内置词典仍可使用。", $"Delete expansion pack '{pack.FileName}'? This applies immediately. The built-in dictionary remains available."),
                delete.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try { LocalDictionary.DeletePack(pack.FileName); UpdateStatus(); }
            catch (Exception ex) { status.Text = T("删除失败：", "Deletion failed: ") + ex.Message; }
        };
        Add(folder, 0, 286, 680, 28); folder.Text = LocalDictionary.Folder;
        Add(open, 0, 324, 200, 34); Add(import, 212, 324, 230, 34); Add(refresh, 454, 324, 226, 34);
        Add(url, 0, 374, 460, 30); Add(download, 474, 372, 206, 34);
        Add(hint, 0, 416, 680, 112);
        open.Name = "OpenDictionaryFolder"; import.Name = "ImportDictionary"; download.Name = "DownloadDictionary";
        refresh.Name = "ReloadDictionaries"; url.Name = "DictionaryDownloadUrl";
        open.Click += (_, _) =>
        {
            try { Directory.CreateDirectory(LocalDictionary.Folder); Process.Start(new ProcessStartInfo(LocalDictionary.Folder) { UseShellExecute = true }); }
            catch (Exception ex) { status.Text = ex.Message; }
        };
        refresh.Click += (_, _) => { LocalDictionary.Reload(); UpdateStatus(); };
        import.Click += async (_, _) =>
        {
            using var picker = new OpenFileDialog { Filter = "English–Chinese dictionary (*.json.gz;*.csv)|*.json.gz;*.csv", Multiselect = false };
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            await Run(async () => await Task.Run(() => LocalDictionary.Install(picker.FileName)));
        };
        download.Click += async (_, _) =>
        {
            if (!Uri.TryCreate(url.Text.Trim(), UriKind.Absolute, out var address) || address.Scheme != "https")
            { status.Text = T("请输入 HTTPS 词典文件直链。", "Enter an HTTPS direct link to a dictionary file."); return; }
            string filename = Path.GetFileName(address.LocalPath);
            if (!filename.EndsWith(".json.gz", StringComparison.OrdinalIgnoreCase) && !filename.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            { status.Text = T("链接必须指向 .json.gz 词典包或 ECDICT .csv 文件。", "Link must point to a .json.gz pack or ECDICT .csv file."); return; }
            await Run(async () =>
            {
                var temp = Path.Combine(Path.GetTempPath(), "EnglishHelper-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(temp);
                var path = Path.Combine(temp, filename);
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                    using var response = await client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead, lifetime.Token);
                    response.EnsureSuccessStatusCode();
                    if (response.RequestMessage?.RequestUri?.Scheme != "https") throw new IOException("HTTPS required.");
                    const long limit = 150 * 1024 * 1024;
                    if (response.Content.Headers.ContentLength > limit) throw new IOException("Dictionary download exceeds 150 MB.");
                    await using (var source = await response.Content.ReadAsStreamAsync(lifetime.Token))
                    await using (var target = File.Create(path))
                    {
                        var buffer = new byte[81920]; long total = 0; int read;
                        while ((read = await source.ReadAsync(buffer, lifetime.Token)) != 0)
                        {
                            total += read;
                            if (total > limit) throw new IOException("Dictionary download exceeds 150 MB.");
                            await target.WriteAsync(buffer.AsMemory(0, read), lifetime.Token);
                        }
                    }
                    lifetime.Token.ThrowIfCancellationRequested();
                    await Task.Run(() => LocalDictionary.Install(path));
                }
                finally
                {
                    if (File.Exists(path)) File.Delete(path);
                    Directory.Delete(temp);
                }
            });
        };
        SetLanguage("en");
    }
    void Add(Control control, int x, int y, int width, int height)
    {
        control.Bounds = new Rectangle(x, y, width, height);
        if (control is RoundedButton) { control.BackColor = Color.White; control.ForeColor = Color.FromArgb(86, 103, 219); }
        Controls.Add(control);
    }
    public void SetLanguage(string value)
    {
        language = value;
        title.Text = T("离线词典（英语 → 简体中文）", "Offline dictionaries (English → Simplified Chinese)");
        open.Text = T("打开词典文件夹", "Open dictionary folder");
        import.Text = T("导入词典文件…", "Import dictionary…");
        refresh.Text = T("重新加载词典", "Reload dictionaries");
        download.Text = T("下载并安装", "Download & install");
        delete.Text = T("删除选中的扩展包", "Delete selected pack");
        packs.Columns[0].Text = T("扩展包文件", "Expansion pack file");
        packs.Columns[1].Text = T("词条数", "Entries");
        packs.Columns[2].Text = T("加载状态", "Load status");
        url.PlaceholderText = T("GitHub Releases 词典文件的 HTTPS 直链", "HTTPS direct link to a GitHub Releases dictionary asset");
        hint.Text = T("保留内置小词典，可另外导入扩展词库。支持本应用的 .json.gz 包和 ECDICT .csv；不支持 MDict 等其他格式。\n下载、导入或删除立即生效；取消设置不会撤销这些操作。删除扩展包后仍可使用内置词典。\n手动增删文件后请点“重新加载词典”。", "Add app-format .json.gz packs or ECDICT .csv files; formats such as MDict are unsupported.\nImport, download and deletion apply immediately; Cancel does not undo them. The built-in dictionary remains available after deletion.\nAfter manually changing files, click Reload dictionaries.");
        if (!busy) UpdateStatus();
    }
    void UpdateStatus()
    {
        try
        {
            int count = LocalDictionary.Count;
            var installed = LocalDictionary.ExpansionPacks;
            int loaded = installed.Count(p => p.IsLoaded);
            status.Text = T($"当前可查 {count:N0} 个词条；已加载 {loaded} 个扩展包。", $"{count:N0} available entries; {loaded} expansion packs loaded.");
            if (LocalDictionary.PackWarning.Length > 0) status.Text += "\n" + T("部分词典未加载，详情请查看列表。", "Some packs could not load; see the list for details.");
            string? selected = (packs.SelectedItems.Count == 1 ? packs.SelectedItems[0].Tag as LocalDictionary.ExpansionPack : null)?.FileName;
            packs.BeginUpdate();
            try
            {
                packs.Items.Clear();
                foreach (var pack in installed)
                {
                    var item = new ListViewItem(pack.FileName) { Tag = pack, ToolTipText = pack.Error, Selected = pack.FileName == selected };
                    item.SubItems.Add(pack.IsLoaded ? pack.EntryCount.ToString("N0") : "—");
                    item.SubItems.Add(pack.IsLoaded ? T("已加载", "Loaded") : T("加载失败", "Failed to load"));
                    packs.Items.Add(item);
                }
            }
            finally { packs.EndUpdate(); }
            delete.Enabled = !busy && packs.SelectedItems.Count == 1;
        }
        catch (Exception ex) { status.Text = ex.Message; }
    }
    async Task Run(Func<Task> operation)
    {
        busy = true; import.Enabled = download.Enabled = refresh.Enabled = delete.Enabled = false;
        status.Text = T("正在处理词典，请稍候…", "Processing dictionary, please wait…");
        try { await operation(); if (!IsDisposed) UpdateStatus(); }
        catch (Exception ex) { if (!IsDisposed) status.Text = T("安装失败：", "Installation failed: ") + ex.Message; }
        finally
        {
            busy = false;
            if (!IsDisposed) { import.Enabled = download.Enabled = refresh.Enabled = true; delete.Enabled = packs.SelectedItems.Count == 1; }
        }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { lifetime.Cancel(); lifetime.Dispose(); }
        base.Dispose(disposing);
    }
    internal static void TestLayout()
    {
        using var form = new Form { ClientSize = new Size(740, 570) };
        using var panel = new DictionarySettingsPanel { Bounds = new Rectangle(20, 20, 700, 530) };
        form.Controls.Add(panel);
        form.Show(); Application.DoEvents();
        foreach (string language in new[] { "zh-CN", "en", "zh-TW", "ja" })
        {
            panel.SetLanguage(language);
            foreach (Control child in panel.Controls)
                if (!panel.ClientRectangle.Contains(child.Bounds)) throw new Exception("Dictionary control is clipped: " + child.Name);
            if (!panel.status.Text.Contains(LocalDictionary.Count.ToString("N0"))) throw new Exception("Dictionary count is not visible");
            if (panel.packs.Items.Count != LocalDictionary.ExpansionPacks.Count || panel.delete.Enabled)
                throw new Exception("Expansion pack list or empty selection state is incorrect");
        }
        panel.SetLanguage("zh-CN");
        if (panel.packs.Items.Count > 0)
        {
            panel.packs.Items[0].Selected = true; Application.DoEvents();
            if (!panel.delete.Enabled) throw new Exception("Selecting an expansion pack did not enable deletion");
        }
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(Path.Combine(AppContext.BaseDirectory, "dictionary-settings-preview.png"));
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "dictionary-settings-test.txt"), "PASS: dictionary controls fit panel; language refresh; installed entry count and package list; deletion button requires a selected pack; offscreen rendering. No desktop focus or network needed.");
    }
}
