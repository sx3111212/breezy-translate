# Breezy 2.0.0

## 简体中文

**小体积、解压即用、不需要 .NET。**

- 改为 C++ 原生 Windows 应用，启动文件统一为 **`Breezy.exe`**。
- 保留划词查词、整段输入框翻译、八种界面语言和二十种翻译目标。
- 保留 MyMemory、Azure、兼容 AI / Claude Messages 服务、多个配置、模型列表及卡片内临时模型切换。
- 保留内置离线词典、扩展词典导入/下载/删除、全局快捷键、示例动画和本机语音/自动朗读。
- 沿用已有配置、DPAPI 加密密钥和离线词典目录；翻译期间原文或焦点变化时保留译文，不覆盖输入。

### 下载与运行

下载 **Breezy-win-x64.zip**，解压整个文件夹，双击 **Breezy.exe**。适用于 Windows 10/11 x64，无需安装 .NET 或 VC++ 运行库。升级前先从托盘退出旧版。

扩展词典为可选附件 `ecdict-full-en-zh.json.gz`，在设置的「离线词典」页导入；保留附带的 `ECDICT-LICENSE.txt`。它只提供英语单词到简体中文的释义，不是离线整句翻译模型。

界面语言在「设置 → 通用设置」中切换，所有语言共用同一个包。`SHA256SUMS.txt` 提供附件校验值。

已验证原生依赖、核心服务协议、完整词典、多语言窗口、模型切换、朗读偏好、快捷键冲突恢复、输入与焦点变化保护和取消行为。付费服务通过本机模拟接口测试，实际可用性和额度仍取决于用户账户。在线翻译直接请求用户选择的服务。

捐赠入口暂未开通，项目暂未指定开源许可证；第三方许可说明随包提供。

---

## English

**Small, portable, and free of a .NET dependency.**

- Rebuilt as a native C++ Windows application named **Breezy.exe**.
- Retains selected-word lookup, whole-input translation, eight interface languages, and twenty translation targets.
- Retains MyMemory, Azure, compatible AI / Claude Messages services, multiple profiles, model lists, and temporary model overrides in result cards.
- Retains offline dictionaries and pack management, global shortcuts, local demonstrations, system speech, and Auto-read.
- Reuses existing preferences, DPAPI-protected keys, and dictionaries. Changes in input text or focus prevent automatic replacement while preserving the translation.

### Download and run

Download **Breezy-win-x64.zip**, extract the entire folder, and run **Breezy.exe**. Supports Windows 10/11 x64 without installing .NET or the Visual C++ Redistributable. Exit the old tray app before upgrading.

The optional expanded dictionary is `ecdict-full-en-zh.json.gz`. Import it under Settings → Offline dictionary and retain `ECDICT-LICENSE.txt`. It provides English-word definitions in Simplified Chinese, rather than offline sentence translation.

For an English interface, choose English under Settings → General and save. All interface languages use the same package. `SHA256SUMS.txt` contains release checksums.

Validation covers native dependencies, service protocols, full dictionary data, multilingual windows, models, speech preferences, shortcut conflict recovery, safe input replacement, focus changes, and cancellation. Paid services were tested with local simulated endpoints; actual availability and quotas depend on the user's account. Online translation requests go directly to the selected service.

A donation destination has not been configured and no project-level open-source license has been selected. Third-party license notices are included.

[English guide](https://github.com/sx3111212/breezy-translate/blob/main/README.en.md)