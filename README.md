# ShitTalker 26 — Original Voice Edition

Run **ShitTalker26.exe**. Keep this entire folder together. This is the first native Windows 11 frontend, using the original 16-bit SoftVoice/Willow Pond DocTalker engine through portable WineVDM 0.9.0.

## Use

- Type text and click **Speak**, or press **Ctrl+Enter**.
- Click an original phrase to speak it immediately. The field above the buttons filters the list.
- **Random original line** selects one of 135 recovered original lines.
- **Random insult** selects a lead-in, two descriptions, and a noun from the original tables, joins them with spaces, and speaks the result.
- **Sicko noise** joins four randomly selected original vowel fragments with no spaces, then speaks them through the original engine.
- **Quick boxes** provides five saved text fields.
- **16 custom buttons**: click an empty button to configure it; right-click an existing button to edit it.
- **Voice settings** opens the original voice controls (model, pitch, rate, contour, gender, quality). Save named voices in that dialog as needed.
- **Stop / restart voice** interrupts speech by restarting this application's engine. Unsaved voice adjustments reset.

Quick boxes and custom buttons save on exit to `%LOCALAPPDATA%\ShitTalker26\settings.json`. The previous settings are retained as `settings.json.bak`. On first launch, original `app\prog.ini` values are imported.

## What is preserved

The supplied original binaries are unchanged. Speech uses the real `DOC.EXE`, `SVTTS.DLL`, and `SVTRAN.DLL`, not a modern substitute voice. The original voice was audibly confirmed on this computer. The frontend controls the engine's text and Test controls directly instead of sending keystrokes to whatever window has focus.

64 phrase buttons and 135 random lines were recovered from the original binary. The insult and sicko-noise generators use recovered VB3 string tables and concatenation structure, including short entries and duplicates. Selection uses .NET's random generator; the original VB3 seed sequence and numeric rounding probabilities have not been reproduced. Name substitution remains unimplemented. The original 640×480 interface has been replaced with a resizable native interface.

## Requirements and provenance

Tested on this x64 Windows 11 computer with the existing .NET Framework 4.x. WineVDM requires the x86 Visual C++ runtime. No system-wide Win16 file association or compatibility driver is installed.

- Original application: Shit-Talker 1.2 by jaundice, supplied by the user. Original readme: `app\README.TXT`.
- Original speech engine: SoftVoice / Willow Pond. Existing copyright notices remain in the binaries.
- Compatibility runtime: https://github.com/otya128/winevdm/releases/tag/v0.9.0. License is included under `runtime\otvdm-v0.9.0\LICENSE`; corresponding upstream source is available at that tag.

This folder is a local prototype, not a public redistribution package. Redistribution rights for the old application and voice binaries have not been established.

## Build

Source is in `src\Program.cs`. From the workspace root run `tools\build.ps1` using the Windows .NET Framework compiler. Close ShitTalker 26 before rebuilding. Phrase extraction is in `tools\extract_phrases.py`.
