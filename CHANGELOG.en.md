# Breezy Translate

## 2.0.1

- Restored the translucent capsule bubble, smooth hover expansion, loading ring, and success feedback using native C++.
- Restored compact white result cards, rounded corners and shadows, display-scaled typography, phonetics, speaker buttons, and the Auto-read switch.
- Restored the pale settings sidebar, rounded buttons, and continuous demonstrations with pause and replay.
- Aligned all seven settings pages and restored dictionary columns, email clearing, and expandable release notes.
- Moved temporary model editing into an owned popup. Applying a model prepares the next retry without changing saved defaults.
- Remains portable as `Breezy.exe`, with no .NET dependency. Expanded dictionary data stays separate.

## 2.0.0

- Migrated to a native C++ Windows app named `Breezy.exe`, without bundled .NET, WinForms, or WPF runtimes.
- Retained selected-text lookup, whole-input translation, eight interface languages, twenty translation targets, custom shortcuts, local demonstrations, speech, and Auto-read.
- Retained MyMemory, Azure, compatible AI services and Claude Messages, multiple profiles, model lists, and temporary model overrides in result cards.
- Existing preferences, DPAPI-protected keys, and offline dictionaries remain compatible. Expanded dictionary data is still downloaded separately.
- Added native dependency inspection and regression tests for standard input fields, full dictionary packs, settings, focus protection, cancellation, and model/speech behavior.

## 1.0.0

- Introduced the Breezy Translate branding and software-update/about pages.
- Provided selection and input translation with configurable services, dictionaries, languages, and shortcuts.
