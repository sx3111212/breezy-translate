# Breezy

[简体中文](README.md) | [English](README.en.md)

适用于 Windows 10/11 x64。解压整个文件夹，双击 **Breezy.exe** 即可使用，无需安装 .NET 或其他运行环境。

## 开始使用

- 划词：选中文字后点击附近的气泡，或按 Ctrl+Alt+D。
- 输入框：写好内容后点击气泡，或按 Ctrl+Alt+E，翻译整个输入框。
- 设置：右键气泡或托盘图标，选择「设置」。可修改目标语言、翻译服务、快捷键和界面语言。
- 手动输入：单击托盘图标即可打开。
- 跟随：在「设置 → 通用设置」分别开关「输入框跟随」「划词跟随」，默认均开启。
- 退出：在托盘图标的右键菜单中选择「退出」。关闭结果窗口不会退出程序。

默认在线服务为 MyMemory，需要网络。内置离线词典仅支持英语单词译成简体中文。AI、Azure 或本机 Ollama 可按需要自行配置。

切换英文界面：设置 → 通用设置 → English，然后保存。所有界面语言共用此下载包。

升级前请先退出旧版托盘程序，再运行新版。配置和词典继续保存在 `%LOCALAPPDATA%\EnglishHelper`，已有设置可继续使用。

## 说明与反馈

- [完整使用说明](https://github.com/sx3111212/breezy-translate)
- [版本更新](https://github.com/sx3111212/breezy-translate/releases/latest)
- [问题反馈](https://github.com/sx3111212/breezy-translate/issues)

在线翻译会把请求的文本直接发送到所选服务；离线查词在本机完成。不同软件对取词和输入框操作的支持情况不同。请在设置中核对服务与语言。

`Licenses` 文件夹包含词典、JSON、压缩及 C++ 运行库的许可说明。项目本身暂未指定开源许可证。
