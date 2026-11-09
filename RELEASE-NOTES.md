# Breezy 2.0.1

## 简体中文

恢复旧版界面风格，继续保持 **小体积、解压即用、不需要 .NET**。

- 恢复透明胶囊气泡、平滑悬停展开、加载圆环与成功反馈。
- 恢复紧凑的白色结果卡片、圆角阴影、音标、发音按钮和自动朗读开关。
- 恢复浅色设置侧栏、圆角按钮和带暂停/重播的连续操作动画，对齐七个设置页。
- 保留八种界面语言、二十种翻译目标、翻译服务、离线词典、快捷键及原有设置。

下载 **Breezy-win-x64.zip**，解压整个文件夹，双击 **Breezy.exe**。支持 Windows 10/11 x64，无需安装 .NET 或 VC++ 运行库。升级前先从托盘退出旧版。

可选扩展词典为 `ecdict-full-en-zh.json.gz`，在「设置 → 离线词典」导入；保留 `ECDICT-LICENSE.txt`。它提供英译简体中文单词释义，不是整句翻译模型。`SHA256SUMS.txt` 提供附件校验值。

已通过原生依赖检查及隔离桌面测试，覆盖多语言设置、查词、输入替换保护、模型编辑和朗读偏好。付费服务使用本机模拟接口验证。

捐赠入口暂未开通；项目暂未指定开源许可证，第三方许可说明随包提供。

---

## English

Restores the previous visual style while remaining **small, portable, and free of a .NET dependency**.

- Restores the translucent capsule bubble, smooth hover expansion, loading ring, and success feedback.
- Restores compact white result cards, rounded corners and shadows, phonetics, speaker buttons, and the Auto-read switch.
- Restores the pale settings sidebar, rounded buttons, and continuous demonstrations with pause/replay across the seven settings pages.
- Keeps eight interface languages, twenty translation targets, translation services, offline dictionaries, shortcuts, and existing preferences.

Download **Breezy-win-x64.zip**, extract the entire folder, and run **Breezy.exe**. Supports Windows 10/11 x64 without installing .NET or the Visual C++ Redistributable. Exit the old tray app before upgrading.

Choose English under Settings → General and save. All interface languages share the same package.

The optional `ecdict-full-en-zh.json.gz` pack can be imported under Settings → Offline dictionary; retain `ECDICT-LICENSE.txt`. It provides English-word definitions in Simplified Chinese, rather than sentence translation. `SHA256SUMS.txt` contains attachment checksums.

Native dependency checks and isolated desktop tests cover multilingual settings, lookup, safe input replacement, model editing, and speech preferences. Paid services were tested through local simulated endpoints.

A donation destination has not been configured. No project-level open-source license has been selected; third-party notices are included.
