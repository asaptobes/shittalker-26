# Shit Talker 2026

**by: asaptobes** — based on the original **Shit Talker 1.2: by jaundice** (1999)

A Windows 11 remake of the classic prank text-to-speech soundboard. It keeps the original 640×480-era look and talks with the **real** 16-bit SoftVoice / Willow Pond DocTalker voice, not a modern substitute.

## Running it

Run **ShitTalker26.exe** and keep the whole folder together. The app needs `app\`, `runtime\` and `phrases.json` next to the exe.

Requirements: Windows 10/11 x64, .NET Framework 4.x (built into Windows), and the x86 Visual C++ runtime (needed by WineVDM).

## Using it

- **Phrase buttons:** click any button in Questions, Statements/responses, This:, or the two long lists to speak it straight away.
- **Hi, can I talk to … please?** Type a name in the black box. Click either button (or press Enter in the box) to ask for that person.
- **This is/ and I'm:** type your name in the second black box, then click to introduce yourself.
- **Jam radar!** speaks one of 135 random lines recovered from the original.
- **Random Insult** builds an insult from the original word tables.
- **Sicko noise** strings together four of the original vowel noises.
- **Quick Boxes:** type anything and click **Say it...** (or press Ctrl+Enter in the box).
- **Options → Custom buttons...** opens the 16 programmable buttons from v1.2. Click an empty button to set it up, or right-click any button to edit it.
- **Options → Voice settings...** opens the original voice controls (model, pitch, rate, gender and more).
- **Restart DocTalker** restarts the speech engine if it stops talking, just like the original. Unsaved voice tweaks are reset.

Quick Boxes and custom buttons are saved on exit to `%LOCALAPPDATA%\ShitTalker26\settings.json`. The previous copy is kept as `settings.json.bak`. On first launch, values from the original `app\prog.ini` are imported.

## Building

The source is a single file, `src\Program.cs` (WinForms, C# 5). Close the app first, then run this from the project folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build.ps1
```

This uses the .NET Framework compiler built into Windows, so no Visual Studio is needed. It writes `ShitTalker26.exe` into the project folder.

## How it works

The front end starts the original `DOC.EXE` hidden, through the portable WineVDM runtime. It then puts text into the engine's own text field and presses its Test button directly. No keystrokes are sent to other windows.

The phrase lists, random lines and generator tables in `phrases.json` were recovered from the original binary. Random choices use .NET's random generator, not the original VB3 sequence. `*random name*` substitution is not implemented.

## Credits & licensing

- **Original Shit-Talker 1.2:** jaundice (http://members.aol.com/meatsloth). Original readme: `app\README.TXT`.
- **Speech engine:** SoftVoice / Willow Pond DocTalker. Copyright notices remain in the binaries.
- **Compatibility runtime:** [WineVDM / otvdm v0.9.0](https://github.com/otya128/winevdm/releases/tag/v0.9.0). License in `runtime\otvdm-v0.9.0\LICENSE`.

The original application and voice binaries in `app\` are unchanged. Redistribution rights for them have not been established, so this is a personal project and not an official release.
