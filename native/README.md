# Breezy native Windows implementation

The executable is **Breezy.exe**. This implementation uses C++17, Win32, UI Automation COM, WinHTTP, DPAPI, SAPI, and GDI+. It contains no CLR entry point and does not bundle or load the .NET runtime. The executable imports Windows system libraries, including the universal CRT supplied with supported Windows 10/11 systems.

## Build

Use the Windows x64 distribution of [Zig 0.17.1](https://ziglang.org/download/). This version was used for the release build. Official archive SHA-256:

```text
b03f477a1943673e02aca0f570e8030dffd536ce1f45cbc4d3326ec31159562e
```

```powershell
$env:BREEZY_ZIG = 'C:\path\to\zig.exe'
powershell -ExecutionPolicy Bypass -File .\native\Build.ps1 -Check
```

Zig supplies the C/C++ compiler, Windows headers, import libraries, and static C++ runtime. A separate Visual Studio or .NET SDK installation is not required. Build caches and generated resources are stored under `artifacts`, outside the source tree.

`Build.ps1` converts the existing `UiText.cs` and `Languages.cs` catalogs to embedded JSON resources without compiling C#. Native-only labels are in `strings.json`. The existing PNG, ICO, and compact gzip dictionary are embedded into the executable.

## Verify

```powershell
Start-Process '.\artifacts\native\Breezy.exe' -ArgumentList '--native-test' -Wait
Start-Process '.\artifacts\native\Breezy.exe' -ArgumentList '--native-ui-test' -Wait
Start-Process '.\artifacts\native\Breezy.exe' -ArgumentList '--native-pack-test .\DictionaryPacks\ecdict-full-en-zh.json.gz' -Wait
powershell -File .\native\Verify-Portable.ps1 -Executable '.\artifacts\native\Breezy.exe'
```

Core tests cover Unicode, gzip integrity and compression, embedded dictionary data, settings migration, protected keys, native shortcut registration and conflict recovery, request construction, UTF-8 byte limits, and simulated service responses. UI tests use separate temporary preferences, a separate input-fixture process, loopback HTTP services, and a silent speech substitute. They exercise all eight interface languages, settings navigation, lookup, safe replacement, cancellation, model selection, and speech preferences. They do not use saved user credentials or call paid translation providers.

Reports and fixture screenshots are written alongside the test executable. The optional `--native-download-test` fetches only the public ECDICT license from GitHub to check real HTTPS release redirects.

## Package

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-Release.ps1 -Repository sx3111212/breezy-translate -Version 2.0.0
```

The release script copies only runtime files and documentation into the ZIP. Compiler caches, object files, generated resource source, test reports, and .NET assemblies are excluded. Expanded dictionary data is a separate release attachment.

## Compatibility and implementation

- Existing preferences, DPAPI-protected keys, AI profile IDs, and dictionaries remain under `%LOCALAPPDATA%\EnglishHelper`.
- UI Automation work runs on a separate MTA thread. Mouse hooks only post coordinates to the UI thread.
- Standard Windows Edit controls also use native selection and replacement messages. Password and read-only controls remain excluded from writing; changes in focus or original text prevent automatic replacement.
- Clipboard fallbacks retain the original OLE data object and restore it only while the clipboard still belongs to that operation.
- Translation requests use HTTPS, with HTTP permitted for local loopback endpoints. API requests do not follow redirects. Explicit dictionary downloads follow bounded HTTPS redirects without forwarding API credentials.
- Settings Save/Cancel applies to preferences. Dictionary import, deletion, and reload take effect immediately, as in the previous version.
- `--build-dictionary <csv> <json.gz>`, `--build-full-dictionary <csv> <json.gz>`, and `--install-dictionary <file>` retain the dictionary tooling workflows.

## Third-party sources

- [nlohmann/json v3.12.0](https://github.com/nlohmann/json/tree/v3.12.0), MIT.
- [miniz 3.0.2](https://github.com/richgel999/miniz/tree/3.0.2), upstream compression/decompression source and license retained.
- Zig and the linked libc++, libc++abi, libunwind, and MinGW runtime notices are in `vendor` and are included in the release.
- ECDICT retains its separate license under `Dictionary`.

No project-level open-source license has been selected. Third-party licenses apply to their respective components.
