# Breezy

[简体中文](README.md) | [English](README.en.md)

For Windows 10/11 x64. Extract the entire folder and run **Breezy.exe**. No .NET or other runtime installation is required.

## Getting started

- Select text and click the nearby bubble, or press Ctrl+Alt+D, to look up a word or translate a sentence.
- Write in a supported input field and click the bubble, or press Ctrl+Alt+E, to translate the entire field.
- Right-click the bubble or tray icon for Settings. Choose your languages, services, shortcuts, and interface language there.
- Use Exit in the tray menu to quit. Closing a result popup keeps the app running.

MyMemory is the default online service and requires an internet connection. The built-in offline dictionary supports English words to Simplified Chinese only. AI services, Azure, and local Ollama can be configured separately with your own accounts or models.

For an English interface, open Settings → General, select English, and save. All interface languages use the same download.

Exit the old tray app before upgrading. Existing preferences and dictionary packs remain under `%LOCALAPPDATA%\EnglishHelper` and can be reused.

## Help and feedback

- [Full English guide](https://github.com/sx3111212/breezy-translate/blob/main/README.en.md)
- [Releases](https://github.com/sx3111212/breezy-translate/releases/latest)
- [Report an issue](https://github.com/sx3111212/breezy-translate/issues)

Online translation sends requested text directly to your selected service. Offline word lookup stays local. Selection and input-field compatibility varies between applications; check your service and language settings before use.

The `Licenses` folder contains notices for the dictionary, JSON, compression, and C++ runtime components. No project-level open-source license has been selected.
