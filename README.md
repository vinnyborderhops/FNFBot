# FNFBOT

FNFBOT is a Windows/.NET 8 bot for Friday Night Funkin' charts. It loads Funkin chart JSON from a game folder, schedules the chart's note timings, and sends keyboard input to the focused game window with Win32 `SendInput`.

The primary application is a WinForms UI. There is also a debug console/CLI surface in `CLI/CliApplication.cs`; see [Debug And CLI Notes](#debug-and-cli-notes) before removing or rewriting it.

## What It Does

- Scans a Funkin game folder for songs in `assets/data/songs`.
- Also scans mods in `mods/<mod name>/data/songs`.
- Loads Funkin chart files such as `song-chart.json`, `song-chart-erect.json`, `song-chart-bf.json`, and `song-chart-pico.json`.
- Supports normal, Erect/Nightmare, BF, and Pico difficulty naming as implemented in `Services/ChartLoader.cs`.
- Filters chart notes to Funkin `d = 0..3`, where Funkin's chart data defines `0 = left`, `1 = down`, `2 = up`, `3 = right`, and `floor(d / 4)` chooses the strumline. In the current Funkin source, strumline `0` is the player strumline.
- Schedules tap notes and hold notes, including a small hold-release guard.
- Uses configurable lane inputs, global hotkeys, hit bias, tap duration, hold-release guard, and color scheme.
- Saves settings to `%LOCALAPPDATA%\FNFBot\settings.json`.

## Requirements

- Windows
- .NET 8 SDK to build or run from source
- A Friday Night Funkin' game/source folder with `assets/data/songs`
- The game window focused when playback starts

This project targets `net8.0-windows`, uses WinForms, registers global hotkeys through `user32.dll`, and sends keyboard input through `SendInput`, so it is intentionally Windows-only.

## Quick Start

1. Build or run the app:

   ```bat
   dotnet run --project FNFBot.WinForms.csproj -c Debug
   ```

2. In the app, set the game folder to the Funkin root, for example the folder that contains `assets/data/songs`.

3. Select a song and difficulty.

4. Click `Load Song`.

5. Put Friday Night Funkin' in focus during gameplay.

6. Start the bot with the Start button or the configured global hotkey. The default start hotkey is `F1`.

The bot opens the pause menu, selects `Restart Song`, confirms it, waits for the game's restart/countdown timing, then begins sending inputs from the loaded chart.

## Controls

Default global hotkeys:

| Action | Default |
| --- | --- |
| Start | `F1` |
| Stop | `F4` |
| Decrease hit bias | `F2` |
| Increase hit bias | `F3` |

Default lane inputs:

| Lane | Primary | Alternate |
| --- | --- | --- |
| Left | `A` | `Left Arrow` |
| Down | `S` | `Down Arrow` |
| Up | `W` | `Up Arrow` |
| Right | `D` | `Right Arrow` |

Alternate inputs matter because dense charts can reuse the same direction before the previous key-up has happened. `PlaybackTimeline` rotates through available keys per lane so overlapping same-direction notes have a better chance of being represented cleanly.

## Settings

Settings are saved at:

```text
%LOCALAPPDATA%\FNFBot\settings.json
```

If the file does not exist, `Services/UserSettings.cs` creates it with the default settings on app startup. Delete the file to regenerate defaults.

The generated default file is:

```json
{
  "hitBiasMs": 22.5,
  "tapDurationMs": 50,
  "holdReleaseGuardMs": 35,
  "menuNavigationDelayMs": 250,
  "hotkeys": {
    "start": "F1",
    "stop": "F4",
    "decreaseHitBias": "F2",
    "increaseHitBias": "F3"
  },
  "inputs": {
    "left": [
      "A",
      "Left"
    ],
    "down": [
      "S",
      "Down"
    ],
    "up": [
      "W",
      "Up"
    ],
    "right": [
      "D",
      "Right"
    ]
  },
  "colorScheme": "System"
}
```

Settings fields:

| Field | Default | Notes |
| --- | --- | --- |
| `hitBiasMs` | `22.5` | Shifts every note after restart delay. Positive is later, negative is earlier. The Settings window allows `-1000` to `1000`. |
| `tapDurationMs` | `50` | Key hold duration for tap notes. The Settings window allows `1` to `1000`. |
| `holdReleaseGuardMs` | `35` | Extra time added before releasing hold notes. The Settings window allows `0` to `1000`. |
| `menuNavigationDelayMs` | `250` | Delay between pause-menu navigation inputs before playback starts. Loaded values are clamped to `50` through `1000`. |
| `hotkeys.start` | `F1` | Global hotkey that starts playback after a song is loaded. |
| `hotkeys.stop` | `F4` | Global hotkey that stops playback. |
| `hotkeys.decreaseHitBias` | `F2` | Global hotkey that decreases hit bias by `0.1 ms`. |
| `hotkeys.increaseHitBias` | `F3` | Global hotkey that increases hit bias by `0.1 ms`. |
| `inputs.left` | `["A", "Left"]` | Primary and alternate keys for left-lane notes. |
| `inputs.down` | `["S", "Down"]` | Primary and alternate keys for down-lane notes. |
| `inputs.up` | `["W", "Up"]` | Primary and alternate keys for up-lane notes. |
| `inputs.right` | `["D", "Right"]` | Primary and alternate keys for right-lane notes. |
| `colorScheme` | `System` | Accepts `System`, `Light`, or `Dark`. |

Key names use WinForms `Keys` names, with punctuation aliases handled by `Services/KeyNames.cs`. Arrow keys may be written as `Left`, `Right`, `Up`, and `Down`; the loader also accepts `LeftArrow`, `RightArrow`, `UpArrow`, and `DownArrow`.

`Delay before play` is not saved in `settings.json`. It is a per-run value in the main window and defaults to `3000 ms` when a new bot instance is created.

## Timing Model

The timing controls are split because they compensate for different things.

### Delay Before Play

`Delay before play` is the delay from the bot's final restart-confirmation `Enter` key-down to chart time `0`.

The bot's restart sequence in `Core/RhythmBot.cs` is:

1. Press `Enter` to open the pause menu.
2. Wait the configured menu navigation delay (`250 ms` by default).
3. Press `Down` to move from `Resume` to `Restart Song`.
4. Wait the configured menu navigation delay (`250 ms` by default).
5. Press `Enter` to confirm restart.
6. Start an internal stopwatch at that final `Enter` key-down.

The menu navigation delay only affects how quickly the bot reaches the final
restart-confirmation press. It is intentionally separate from `Delay before play`,
so making menu navigation faster does not change the song countdown delay formula.

The default delay is `3000 ms`, but the UI can predict a delay from the song BPM. This prediction is based on the current Funkin restart/countdown flow:

- `PlayState` uses a `vwooshDelay` of `0.5 s` before starting the countdown.
- `Countdown.performCountdown()` initializes the conductor at `-5` beats.
- After the initial `before` state, the countdown runs five beat-length ticks: 3, 2, 1, go, after.
- The song starts when the conductor reaches chart time `0`, adjusted by Funkin's combined offsets.

That gives this app's predicted delay formula:

```text
predictedDelayMs = 500 + (5 * 60000 / bpm)
predictedDelayMs = 500 + (300000 / bpm)
```

The UI reads BPM from the first entry in the song metadata `timeChanges` array. If the metadata is missing or cannot be parsed, the predicted delay button stays disabled.

### Hit Bias

`Hit bias` shifts every scheduled note relative to its chart time after the restart delay has elapsed:

```text
actualPressTime = restartConfirmTime + DelayBeforePlayMs + note.t + HitBiasMs
```

Positive hit bias presses later. Negative hit bias presses earlier.

The default is `+22.5 ms`. This is deliberately inside Funkin's current PBOT1 `sick` range rather than exactly at zero. In the referenced Funkin scoring code, PBOT1 judgements are:

| Judgement | Absolute timing |
| --- | --- |
| Perfect score cap | `< 5 ms` |
| Sick | `<= 45 ms` |
| Good | `<= 90 ms` |
| Bad | `<= 135 ms` |
| Shit | `<= 160 ms` |
| Miss | `> 160 ms` |

Funkin judges note timing from the difference between `Conductor.instance.songPosition` and the note's chart time, with input latency compensation applied before scoring. Since FNFBOT is external to the game and goes through Windows input, a small late bias can be more stable than trying to land exactly on `0 ms`.

### Tap Duration And Hold Release Guard

`PlaybackTimeline` turns every note into a key-down and key-up event.

- Tap notes default to a `50 ms` key hold.
- Hold notes release at `note.l + HoldReleaseGuardMs`.
- The default hold-release guard is `35 ms`.

The guard exists so the key is not released exactly at the charted sustain length, where scheduler jitter or game-frame timing could make a hold appear dropped slightly early.

## Supported Chart Layout

The chart loader expects the current Funkin JSON layout:

```json
{
  "scrollSpeed": {
    "normal": 1.6
  },
  "notes": {
    "normal": [
      { "t": 1000, "d": 0 },
      { "t": 1250, "d": 1, "l": 500 }
    ]
  }
}
```

Relevant fields:

- `t`: chart time in milliseconds.
- `d`: combined strumline/direction integer. The bot currently keeps only `0..3` and maps those to left/down/up/right.
- `l`: optional hold length in milliseconds.
- `scrollSpeed`: displayed in the UI for the selected difficulty.

Difficulty lookup is file-aware:

- `song-chart.json`: `easy`, `normal`, `hard`
- `song-chart-erect.json`: `erect`, `nightmare`
- `song-chart-bf.json`: `bf-easy`, `bf-normal`, `bf-hard`
- `song-chart-pico.json`: `pico-easy`, `pico-normal`, `pico-hard`

The metadata loader uses matching metadata suffixes:

- default: `song-metadata.json`
- erect/nightmare: `song-metadata-erect.json`
- BF/Pico variants: `song-metadata-bf.json` or `song-metadata-pico.json`

## Project Structure

```text
CLI/                 Debug console command surface
Core/                RhythmBot orchestration and playback
Interop/             Win32 virtual keys, SendInput support, global hotkeys
Models/              Chart and scheduled event models
Services/            Chart loading, song discovery, settings, timeline, input
UI/                  WinForms main window, settings window, theme helpers
Program.cs           Application entry point
FNFBot.WinForms.csproj
```

Important files:

- `Core/RhythmBot.cs`: loads songs, navigates restart, starts playback, waits for scheduled chart events.
- `Services/ChartLoader.cs`: understands Funkin chart file names, difficulty names, note filtering, and scroll speed.
- `Services/PlaybackTimeline.cs`: builds ordered key-down/key-up events and chooses alternate keys for overlaps.
- `Services/UserSettings.cs`: loads/saves settings and defines defaults.
- `UI/MainForm.cs`: main UI, folder selection, song loading, predicted delay, hotkey handling.
- `UI/SettingsForm.cs`: timing, hotkeys, lane inputs, and appearance settings.
- `CLI/CliApplication.cs`: console commands for listing/loading songs, changing delay, showing status, toggling debug output, and simulating `F1`.

## Debug And CLI Notes

`Program.cs` currently launches the WinForms UI:

```csharp
ApplicationConfiguration.Initialize();
Application.Run(new MainForm());
```

That can make `CLI/CliApplication.cs` look unused at first glance. Do not judge it from `Program.cs` alone. The project file changes the executable subsystem by configuration:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <OutputType>Exe</OutputType>
</PropertyGroup>

<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <OutputType>WinExe</OutputType>
</PropertyGroup>
```

So Debug builds are console-subsystem executables and Release builds are windowed executables. The CLI class is compiled into the app and is the existing console/debug command surface. In this snapshot, it is not the default entry point because `Main()` still opens `MainForm`; if a future change wants a real debug REPL, wire `CliApplication.Run()` from `Program.cs` with a clear Debug-only switch instead of creating a second CLI implementation.

CLI commands already implemented:

```text
list
load <song>
delay <ms>
status
debug [on|off]
f1
quit
```

Set `FNFBOT_CLI_DEBUG=1` to enable CLI debug diagnostics on startup.

## Building

Debug run:

```bat
dotnet run --project FNFBot.WinForms.csproj -c Debug
```

Release build:

```bat
dotnet build FNFBot.sln -c Release
```

Framework-dependent Windows x64 publish:

```bat
dotnet publish FNFBot.WinForms.csproj -c Release -r win-x64 --self-contained false
```

## Troubleshooting

### No Songs Found

Check that the selected folder is the Funkin root folder. It should contain:

```text
assets/data/songs/<song>/<song>-chart.json
```

Mods are detected under:

```text
mods/<mod name>/data/songs/<song>/<song>-chart.json
```

### Song Loads But Timing Is Late Or Early

Use `Hit bias` for small timing nudges after the restart delay is basically correct.

- If notes are being hit late, decrease hit bias.
- If notes are being hit early, increase hit bias.
- Start with small changes, such as `0.5 ms` to `2 ms`.

Use `Delay before play` for large alignment problems where every note is offset by the same amount because the bot started playback before or after the game reached chart time `0`.

### Start Hotkey Does Nothing

- Make sure a song is loaded first.
- Make sure global hotkey registration succeeded.
- Change conflicting hotkeys in Settings if another app owns `F1`, `F2`, `F3`, or `F4`.
- Keep the game window focused so the injected key events go to Funkin.

### Notes Drop During Dense Sections

Check lane input settings. Each lane can have a primary and alternate key. When two notes of the same direction overlap or occur too close together, `PlaybackTimeline` can rotate between configured keys for that lane.

### Holds Release Too Early

Increase `Hold release guard (ms)` in Settings. The default is `35 ms`.

## Reference Notes From Funkin Source

The timing details above were checked against the Funkin source tree:

- `source/funkin/play/PauseSubState.hx`: `Restart Song` sets `PlayState.instance.needsReset = true`.
- `source/funkin/play/PlayState.hx`: restart reset uses `vwooshDelay = 0.5`, then calls `Countdown.performCountdown()`.
- `source/funkin/play/Countdown.hx`: countdown begins at `-5` beats and runs five beat-length timer ticks.
- `source/funkin/Conductor.hx`: `combinedOffset` is `instrumentalOffset + formatOffset + globalOffset`.
- `source/funkin/play/scoring/Scoring.hx`: PBOT1 scoring and judgement thresholds.
- `source/funkin/data/song/SongData.hx`: note `d` uses `0..3` for direction and `floor(d / 4)` for strumline.

## Disclaimer

This project automates gameplay by simulating keyboard input. It is not affiliated with the Friday Night Funkin' developers. I do not indorse using for a competitive advantage.
