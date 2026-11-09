# Offline English–Chinese dictionaries

Source: ECDICT, https://github.com/skywind3000/ECDICT (MIT). Retain the included ECDICT-LICENSE.txt when distributing packs.

The app embeds english-zh.json.gz: 58,222 single English words with Chinese definitions. Its compact build retains ranked, Collins-rated or exam-tagged entries.

## Optional expanded pack

../DictionaryPacks/ecdict-full-en-zh.json.gz contains 399,980 single-word entries with Chinese definitions, including unranked entries. It is about 9.5 MiB and is not embedded or copied into app output. This expands word coverage; it is not the entire ECDICT database of phrases, names and entries without Chinese definitions.

Settings → General settings → Offline dictionaries provides:
- Import app-format .json.gz packs or ECDICT .csv files.
- Download and install from an HTTPS direct file URL.
- Open dictionary folder.
- Reload after manually adding/removing files.

Packs live in %LOCALAPPDATA%\EnglishHelper\Dictionaries. Installation applies immediately, independently of preference Save/Cancel. External packs merge over the built-in dictionary in ordinal filename order; later files override earlier definitions. A same-named import replaces its previous pack only after validation. Invalid packs are skipped with a warning in Settings. Lookups remain offline.

Only English words → Simplified Chinese are supported. This does not provide offline sentence translation, stemming, MDict, StarDict or arbitrary JSON support.

## Distribution

Upload DictionaryPacks/ecdict-full-en-zh.json.gz and ECDICT-LICENSE.txt as GitHub Release assets. Users can download and import the file, or paste the release asset's HTTPS direct URL in Settings. No repository URL is hard-coded; users trigger downloads explicitly.

## Build and verify

The current app is native C++. Build it with the toolchain described in [native/README.md](../native/README.md). No .NET runtime is used.

```powershell
Start-Process '.\artifacts\native\Breezy.exe' -ArgumentList '--build-dictionary .\obj\ecdict-complete.csv .\Dictionary\english-zh.json.gz' -Wait
Start-Process '.\artifacts\native\Breezy.exe' -ArgumentList '--build-full-dictionary .\obj\ecdict-complete.csv .\DictionaryPacks\ecdict-full-en-zh.json.gz' -Wait
Start-Process '.\artifacts\native\Breezy.exe' -ArgumentList '--native-test' -Wait
Start-Process '.\artifacts\native\Breezy.exe' -ArgumentList '--native-pack-test .\DictionaryPacks\ecdict-full-en-zh.json.gz' -Wait
```

For local installation via CLI: `Breezy.exe --install-dictionary <pack-or-csv>`. Restart an older running app after upgrading.