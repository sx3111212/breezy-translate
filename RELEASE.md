# GitHub 发布

计划仓库名：`breezy-translate`。首次版本：`v1.0.0`。本文件中的计划不代表已经上线。

当前 `app-links.json` 已按已连接账号 `sx3111212/breezy-translate` 预填项目和发布页地址。仓库和 Release 创建前，这些地址尚不可用；如果更换账号或仓库名，请一起修改。

## 首次发布

1. 安装 GitHub CLI，并运行 `gh auth login --hostname github.com --git-protocol https --web` 完成本人登录。不要把密码或令牌写进项目。
2. 在 GitHub 上创建公开仓库，或使用 `gh repo create OWNER/breezy-translate --public --description "轻风翻译 / Breezy Translate — Windows floating translator"`。
3. 将 `app-links.json` 的 `GitHubRepository` 填为真实的 `OWNER/breezy-translate`；`ProjectUrl` 填为仓库 HTTPS 地址；`ReleasesUrl` 填为该地址加 `/releases/latest`。
4. 如果已有捐赠主页，填入 `DonationUrl`，在 README 增加同一链接，并创建 `.github/FUNDING.yml`，内容为 `custom: ['真实的 HTTPS 捐赠链接']`。没有收款主页时保留空值。不要直接填写尚未开通的 GitHub Sponsors 地址。
5. 选择项目许可证；词典自带的 MIT 许可证必须保留。
6. 在项目目录打包：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-Release.ps1 -Repository OWNER/breezy-translate -Version 1.0.0
```

发布包携带 .NET 运行时，面向 Windows x64。解压整个文件夹，运行 `EnglishHelper.exe`；请勿只复制 exe。`app-links.json` 可在发布后单独修改。扩展词典单独下载后到设置导入。

7. 检查待提交文件，提交源码，推送：

```powershell
git init -b main
git add .
git diff --cached --stat
git commit -m "Initial release of Breezy Translate"
git remote add origin https://github.com/OWNER/breezy-translate.git
gh auth setup-git
git push -u origin main
```

8. 上传完整附件后再公开版本：

```powershell
gh release create v1.0.0 --repo OWNER/breezy-translate --target main --draft --title "轻风翻译 / Breezy Translate v1.0.0" --notes-file .\RELEASE-NOTES.md .\artifacts\release-1.0.0\BreezyTranslate-1.0.0-win-x64.zip .\artifacts\release-1.0.0\ecdict-full-en-zh.json.gz .\artifacts\release-1.0.0\ECDICT-LICENSE.txt .\artifacts\release-1.0.0\SHA256SUMS.txt
gh release edit v1.0.0 --repo OWNER/breezy-translate --draft=false --latest
```

下载地址在仓库名确定后即可预先写好，无需先上传文件：

- 发布页：`https://github.com/OWNER/breezy-translate/releases/latest`
- 主程序：`https://github.com/OWNER/breezy-translate/releases/latest/download/BreezyTranslate-1.0.0-win-x64.zip`（后续版本需更新文件名）
- 扩展词典：`https://github.com/OWNER/breezy-translate/releases/latest/download/ecdict-full-en-zh.json.gz`

实际发布后检查这些地址再对外推广。自愿捐赠不保证收入。
