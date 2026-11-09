# Breezy 2.0.2

## 简体中文

精简操作入口，明确区分两种跟随行为，继续保持 **小体积、解压即用、不需要 .NET**。

- 「设置 → 通用设置」新增「输入框跟随」「划词跟随」，默认均开启，可独立保存和关闭。
- 两个开关各有说明，旧配置没有这两项时自动启用。
- 单击托盘图标打开手动输入；右键菜单仅保留设置、软件更新、关于轻风、退出。
- 保留旧版风格的气泡动画、圆角阴影和结果卡片。
- 保留八种界面语言、二十种翻译目标、翻译服务、离线词典、快捷键及原有设置。

下载 **Breezy-win-x64.zip**，解压整个文件夹，双击 **Breezy.exe**。支持 Windows 10/11 x64，无需安装 .NET 或 VC++ 运行库。升级前先从托盘退出旧版。

可选扩展词典为 `ecdict-full-en-zh.json.gz`，在「设置 → 离线词典」导入；保留 `ECDICT-LICENSE.txt`。它提供英译简体中文单词释义，不是整句翻译模型。`SHA256SUMS.txt` 提供附件校验值。

已通过原生依赖检查及隔离桌面测试，覆盖多语言设置、查词、输入替换保护、模型编辑和朗读偏好。付费服务使用本机模拟接口验证。

捐赠入口暂未开通；项目暂未指定开源许可证，第三方许可说明随包提供。

---

## English

Simplifies the app’s entry points and separates the two follow behaviors while remaining **small, portable, and free of a .NET dependency**.

- Settings → General now has separate Follow input fields and Follow selected text switches, both enabled by default and saved independently.
- Each switch explains its behavior. Existing preferences without these fields receive enabled defaults.
- A single tray-icon click opens manual input. The context menu now contains only Settings, Software updates, About Breezy, and Exit.
- Retains the restored bubble animations, rounded corners, shadows, and result cards.
- Keeps eight interface languages, twenty translation targets, translation services, offline dictionaries, shortcuts, and existing preferences.

Download **Breezy-win-x64.zip**, extract the entire folder, and run **Breezy.exe**. Supports Windows 10/11 x64 without installing .NET or the Visual C++ Redistributable. Exit the old tray app before upgrading.

Choose English under Settings → General and save. All interface languages share the same package.

The optional `ecdict-full-en-zh.json.gz` pack can be imported under Settings → Offline dictionary; retain `ECDICT-LICENSE.txt`. It provides English-word definitions in Simplified Chinese, rather than sentence translation. `SHA256SUMS.txt` contains attachment checksums.

Native dependency checks and isolated desktop tests cover multilingual settings, lookup, safe input replacement, model editing, and speech preferences. Paid services were tested through local simulated endpoints.

A donation destination has not been configured. No project-level open-source license has been selected; third-party notices are included.
