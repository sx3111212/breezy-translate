# 轻风翻译 / Breezy Translate v1.0.0

Windows 悬浮翻译工具：划词查词，输入即译。

- 选中文字后点击气泡查看译文，或翻译当前输入框。
- 支持 MyMemory、用户自行配置的 Azure 和兼容的 AI 服务。
- 内置离线英汉词典；支持单独导入扩展词典包。
- 八种界面语言、自定义全局快捷键、Windows 本地英语发音。
- 设置中提供版本发布页和可配置的自愿捐赠入口。

## 下载与运行

下载 `BreezyTranslate-1.0.0-win-x64.zip`，解压整个文件夹，运行 `EnglishHelper.exe`。适用于 Windows x64，包含 .NET 运行时。

可选扩展词典：下载 `ecdict-full-en-zh.json.gz`，在「设置 → 离线词典」导入；请保留 `ECDICT-LICENSE.txt`。词典只提供英语单词到简体中文的释义。

`SHA256SUMS.txt` 提供附件的 SHA-256 校验值。在线翻译会将待翻译文本发送到所选服务，使用前请核对服务设置。不同软件的划词与输入框兼容性可能不同。

---

## English

Breezy Translate is a floating translation tool for Windows: look up selected words, translate sentences, and translate entire input fields.

- Click the bubble next to selected text, or translate the focused input field.
- Use MyMemory, your own Azure Translator account, or a compatible AI service.
- Look up English words offline with the bundled English–Chinese dictionary; import an optional expanded pack separately.
- Choose from eight interface languages, customize global shortcuts, and listen with Windows' local English voices.
- Open the releases page from Settings. Donations are optional; a donation destination has not been configured yet.

### Download and run

Download `BreezyTranslate-1.0.0-win-x64.zip`, extract the entire folder, and run `EnglishHelper.exe`. The package is for Windows x64 and includes the .NET runtime.

**English interface:** open Settings → General, select English as the interface language, and save. The same download supports all interface languages.

For the optional expanded dictionary, download `ecdict-full-en-zh.json.gz` and import it under Settings → Offline dictionary. Retain `ECDICT-LICENSE.txt`. The dictionary provides English-word definitions in Simplified Chinese only.

`SHA256SUMS.txt` contains SHA-256 checksums for the release attachments. Online translation sends the requested text to your selected service. Selection and input-field compatibility varies between applications.

Read the [English user guide](https://github.com/sx3111212/breezy-translate/blob/main/README.en.md) for setup, services, privacy, and limitations.
