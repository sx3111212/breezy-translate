using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace EnglishHelper;

// Keep public links separate from user preferences and never invent release/payment destinations.
sealed record AppLinks(string ProjectUrl = "", string ReleasesUrl = "", string DonationUrl = "", string GitHubRepository = "")
{
    public string ReleasePage => IsWebUrl(ReleasesUrl) ? ReleasesUrl :
        System.Text.RegularExpressions.Regex.IsMatch(GitHubRepository, @"^[a-zA-Z0-9_.-]+/[a-zA-Z0-9_.-]+$")
            ? "https://github.com/" + GitHubRepository + "/releases" : "";
    public static AppLinks Load()
    {
        try { return JsonSerializer.Deserialize<AppLinks>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "app-links.json"))) ?? new(); }
        catch { return new(); }
    }
    public static bool IsWebUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}

sealed class AppInfoPanel : Panel
{
    readonly bool donation;
    readonly AppLinks links = AppLinks.Load();
    readonly Label introduction = new(), details = new(), status = new();
    readonly RoundedButton action = new(), secondary = new();
    readonly LinkLabel project = new();
    readonly RichTextBox notes = new();
    string language = "zh-CN";
    string T(string zh, string en) => language.StartsWith("zh") ? zh : en;
    internal static string CurrentVersion => typeof(AppInfoPanel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public AppInfoPanel(bool donation)
    {
        this.donation = donation;
        Name = donation ? "DonationPanel" : "SoftwareUpdatePanel";
        BackColor = Color.White; AutoScroll = true;
        foreach (var label in new[] { introduction, details, status }) label.ForeColor = Color.FromArgb(81, 96, 131);
        Place(introduction, 24, 22, 650, 65);
        Place(details, 24, 97, 650, 54);
        Place(status, 24, 162, 650, 65);
        action.Name = donation ? "OpenDonation" : "CheckForUpdates";
        action.BackColor = Color.FromArgb(89, 110, 222); action.ForeColor = Color.White;
        secondary.BackColor = Color.FromArgb(237, 241, 248); secondary.ForeColor = Color.FromArgb(63, 82, 185);
        Place(action, 24, 242, 175, 38); Place(secondary, 215, 242, 175, 38);
        project.LinkColor = Color.FromArgb(86, 103, 219);
        Place(project, 24, 302, 650, 30);
        notes.Name = "UpdateReleaseNotes"; notes.ReadOnly = true; notes.BorderStyle = BorderStyle.None;
        notes.BackColor = Color.FromArgb(246, 248, 253); notes.ForeColor = Color.FromArgb(52, 65, 96);
        notes.Font = new Font("Microsoft YaHei UI", 9); notes.DetectUrls = false;
        Place(notes, 24, 342, 650, 155); notes.Visible = false;
        action.Click += (_, _) => OpenLink(donation ? links.DonationUrl : links.ReleasePage);
        secondary.Click += (_, _) => { notes.Visible = !notes.Visible; SetLanguage(language); };
        project.LinkClicked += (_, _) => OpenLink(links.ProjectUrl);
        SetLanguage(language);
    }
    void Place(Control control, int x, int y, int width, int height)
    {
        control.Bounds = new Rectangle(x, y, width, height); Controls.Add(control);
    }
    public void SetLanguage(string value)
    {
        language = value;
        introduction.Text = donation
            ? UiText.T("aboutIntro", language)
            : T("在这里查看当前版本和更新日志，或前往版本发布页获取新版。", "View your installed version and release notes, or visit the releases page for a newer version.");
        details.Text = donation
            ? T("感谢每一份支持，也感谢你的使用与反馈。\n捐赠完全自愿，不影响现有功能的使用。", "Thank you for your support and feedback.\nDonating is optional and does not affect existing features.")
            : T("当前版本：", "Current version: ") + CurrentVersion + "\nWindows · " + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
        bool available = AppLinks.IsWebUrl(donation ? links.DonationUrl : links.ReleasePage);
        status.Name = donation ? "DonationStatus" : "UpdateStatus";
        status.Text = donation
            ? available ? T("点击下方按钮，在浏览器中打开捐赠页面。", "Open the donation page in your browser below.") : T("捐赠通道暂未配置，感谢你的支持。", "A donation destination has not been configured. Thank you for your support.")
            : available ? T("更新来自 GitHub Releases。点击下方按钮打开发布页，对照当前版本查看。", "Updates are published on GitHub Releases. Open the releases page and compare it with your current version.") : T("尚未发布到 GitHub，暂时无法检查新版本。\n发布后将在此提供 GitHub Releases 入口。", "The project has not been published to GitHub yet; updates cannot be checked.\nA GitHub Releases link will be available after publication.");
        action.Enabled = available;
        action.Text = donation ? T("捐赠支持 ♥", "Donate ♥") : T("前往版本发布页", "View releases");
        secondary.Visible = !donation;
        secondary.Text = notes.Visible ? T("收起更新日志", "Hide release notes") : T("查看更新日志", "View release notes");
        project.Enabled = AppLinks.IsWebUrl(links.ProjectUrl);
        project.Text = project.Enabled ? T("项目主页：", "Project: ") + links.ProjectUrl : T("项目主页暂未配置", "Project website not configured");
        try { notes.Text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "CHANGELOG.md")); }
        catch { notes.Text = T("暂无本地更新日志。", "No local release notes available."); }
    }
    void OpenLink(string url)
    {
        if (!AppLinks.IsWebUrl(url)) return;
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { status.Text = T("无法打开浏览器，请稍后重试。", "Could not open the browser. Please try again."); }
    }
}
