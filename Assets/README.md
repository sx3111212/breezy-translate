# Floating translation button artwork

The v2.0.2 General settings page is captured in `native-general-preview.png` and `native-general-preview.en.png`, with both follow switches enabled by default.

Native v2.0.1 screenshots: `native-dictionary-preview.png`, `native-selection-preview.png`, `native-selection-preview.en.png`, and `native-sentence-preview.png` capture the real C++ windows over a plain test backdrop, including DWM rounding and shadows. `native-floating-hover-preview.png` renders the same per-pixel-alpha surface used by the live capsule. All use isolated UI-test preferences and the existing bubble artwork; they are not generated mockups. Previews without the `native-` prefix are historical references.

Generated using the built-in `image_gen.imagegen` tool with `transparent_background: true`.

- Production image: `translation-orb.png`. Embedded into the executable; no dependency on the original generation folder.
- Tray-specific artwork: `translation-tray.png`, a more saturated, shadow-free adaptation of the same bubble for tiny tray slots. Generated with built-in image generation; its prompt is in `tray-generation-prompt.md`.
- Tray and executable icon: `translation-orb.ico`, derived from `translation-tray.png` with tight optical framing and transparent frames at 16, 20, 24, 32, 40, 48, 64, 128 and 256 pixels. Rebuild it with `powershell -File Assets/Build-Icon.ps1`.
- Rendered interface previews: `floating-preview.png` (resting) and `floating-hover-preview.png` (hover).
- Compact dictionary panel preview: `dictionary-preview.png`, showing the word, phonetic transcription, definitions, speaker button and saved Auto-read switch. UI surfaces are implemented in native Windows code; the original glass-bubble artwork is reused.
- Matching sentence popup preview: `sentence-preview.png`. Both popup previews capture Windows-composited corners and shadows over a plain test background.
- Settings previews: `settings-preview.png` demonstrates independent reading/input targets; `shortcuts-preview.png` shows recording and enabling global hotkeys. These are screenshots of native controls, not generated mockups.
- Windows per-pixel alpha preserves the image's transparent edges and soft shadow. Hover enlargement, reveal animation, labels, progress ring and completion badge are rendered by the application.

## Final generation prompt

Use case: stylized-concept. Asset type: production PNG artwork for a small floating desktop translation button. Create ONE exquisitely polished, compact, rounded chat-bubble orb: a smooth iridescent glass speech bubble with a clear white overlapping speech-bubble motif in its center, suggesting bilingual conversation. Refined soft 3D product rendering, luminous pearlescent edges, restrained blue-violet and mint reflections, beautifully balanced form, subtle depth and a very soft short shadow. Front-facing near-orthographic view. A confident simple silhouette and bold center motif that remain readable at 48 pixels. Center the single object, let it occupy about 85 percent of a square canvas, with generous unclipped transparent space around its edges. The background must be genuinely transparent with an alpha channel, including outside the soft shadow. No words, no letters, no typography, no watermark, no surrounding UI, no multiple options, no square tile or rectangular background.
