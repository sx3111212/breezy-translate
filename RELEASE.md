# GitHub 原生版发布

仓库：[sx3111212/breezy-translate](https://github.com/sx3111212/breezy-translate)。从 v2.0 起默认发布 C++ 原生版，启动文件为 `Breezy.exe`。历史 v1.0.0 的 .NET 便携包继续保留在原来的 Release 中。

## 构建与验证

使用 Zig 0.17.1 Windows x64 工具链。无需安装 .NET SDK；工具链来源及校验值见 [native/README.md](native/README.md)。

```powershell
$env:BREEZY_ZIG = 'C:\path\to\zig.exe'
powershell -ExecutionPolicy Bypass -File .\scripts\Build-Release.ps1 -Repository sx3111212/breezy-translate -Version 2.0.0
```

输出目录为 `artifacts\release-2.0.0`。脚本拒绝覆盖已经存在的同版本目录。主包 `Breezy-win-x64.zip` 只包含原生程序、配置、双语说明、更新日志和第三方许可证，不包含 .NET、编译器、对象文件或测试报告。完整词典作为独立附件提供。

发布前运行窗口和完整词典测试，查看 `artifacts\native` 中的报告：

```powershell
Start-Process '.\artifacts\native\Breezy.exe' -ArgumentList '--native-ui-test' -Wait
Start-Process '.\artifacts\native\Breezy.exe' -ArgumentList '--native-pack-test .\DictionaryPacks\ecdict-full-en-zh.json.gz' -Wait
```

如果已经验证了 `artifacts\native\Breezy.exe`，可以给打包脚本增加 `-UseExistingBuild`，使用版本号匹配的已构建程序；仍会检查 PE 类型、CLR 目录及导入依赖。

## 推送和发布

在本人完成 `gh auth login` 后，先提交源码并推送，再创建附带四个附件的 Release 草稿。上传完成后核对附件大小和 SHA-256，再公开版本。

```powershell
git add .
git commit -m "Release native Breezy 2.0.0"
git push origin main
gh release create v2.0.0 --repo sx3111212/breezy-translate --target main --draft --title "Breezy 2.0.0" --notes-file .\RELEASE-NOTES.md .\artifacts\release-2.0.0\Breezy-win-x64.zip .\artifacts\release-2.0.0\ecdict-full-en-zh.json.gz .\artifacts\release-2.0.0\ECDICT-LICENSE.txt .\artifacts\release-2.0.0\SHA256SUMS.txt
gh release edit v2.0.0 --repo sx3111212/breezy-translate --draft=false --latest
```

固定入口：

- 发布页：`https://github.com/sx3111212/breezy-translate/releases/latest`
- Windows x64 主包：`https://github.com/sx3111212/breezy-translate/releases/latest/download/Breezy-win-x64.zip`
- 扩展词典：`https://github.com/sx3111212/breezy-translate/releases/latest/download/ecdict-full-en-zh.json.gz`

`app-links.json` 已填入真实仓库与发布页地址。捐赠主页尚未开通，`DonationUrl` 保留空值；开通后再同步 README、软件配置及 `.github/FUNDING.yml`。不填写未开通的 GitHub Sponsors 地址。

项目暂未选择开源许可证，不添加项目级 `LICENSE`；保留词典及其他第三方组件的许可说明。旧版 .NET 构建脚本另存于 `scripts/Build-Legacy-Release.ps1`，不用于当前默认发布。