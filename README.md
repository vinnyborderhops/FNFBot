# Friday Night Funkin' Bot

A Windows Forms rhythm bot for Friday Night Funkin'. It reads song charts from a
local game installation and simulates configurable keyboard inputs.

## Features

- Discovers songs from a selected Friday Night Funkin' game folder
- Loads chart notes and sustain notes from JSON
- Supports Easy, Normal, Hard, Erect, and Nightmare charts
- Supports BF and Pico character chart variants when available
- Displays note count, scroll speed, BPM, and a predicted start delay
- Provides adjustable start delay and hit-bias timing
- Uses configurable global hotkeys while the game is focused
- Uses configurable lane keys, alternating between them for overlapping notes
- Includes an in-app settings editor with live theme previews
- Supports System, Light, and Dark color schemes, including dark title bars

## Requirements

- Windows 10 or Windows 11 (64-bit)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build from
  source
- A compatible Friday Night Funkin' installation with charts stored under:

  ```text
  assets/data/songs/<song-name>/
  ```

The application uses the Windows `SendInput` API and therefore does not support
macOS or Linux.

## Build

From this directory, run:

```powershell
dotnet restore
dotnet build FNFBot.sln
```

Run the application during development with:

```powershell
dotnet run --project FNFBot.WinForms.csproj
```

Create a release build with:

```powershell
dotnet publish FNFBot.WinForms.csproj -c Release -r win-x64 --self-contained false
```

## Usage

1. Start `FNFBOT.exe` or run the project with `dotnet run`.
2. Click **Browse** and select the Friday Night Funkin' game folder.
3. Click **Set Folder** to scan `assets/data/songs`.
4. Select a song and an available difficulty.
5. Click **Load Song**.
6. Set the delay before play, or use the predicted delay when BPM metadata is
   available.
7. Focus the game, open the pause menu during the loaded song, and press the
   configured start hotkey (`F1` by default). Press the stop hotkey (`F4` by
   default) to stop playback.

Click **Settings** to change hit bias, global hotkeys, lane inputs, or the color
scheme. Settings cannot be opened while a chart is playing.

Click a key field and press the desired key. Each lane has one required primary
key and one optional alternate key. Clear an alternate with Backspace, Delete,
or Escape. The four global hotkeys must be present and unique; modifier-only
keys and key combinations are not supported.

Theme changes are previewed in both the settings dialog and main window. Saving
applies all changes immediately, while cancelling restores the previous theme.
If new global hotkeys cannot be registered, the previous settings remain active.

When started, the bot presses Enter, moves down once, and confirms the restart.
Playback timing begins from that final confirmation, followed by the configured
delay.

### Controls

| Control | Action |
| --- | --- |
| `F1` (default) | Start the loaded chart |
| `F2` (default) | Decrease the start delay by 0.5 ms |
| `F3` (default) | Increase the start delay by 0.5 ms |
| **Stop** | Stop playback and release held keys |
| **-0.5 ms / +0.5 ms** | Adjust the hit bias |

The default lane keys are:

| Lane | Keys |
| --- | --- |
| Left | `A` / Left Arrow |
| Down | `S` / Down Arrow |
| Up | `W` / Up Arrow |
| Right | `D` / Right Arrow |

## Timing

**Delay before play** controls how long the bot waits after confirming the song
restart before it begins processing chart timestamps. When BPM metadata exists,
the application estimates this value as:

```text
500 + (300000 / BPM)
```

**Hit bias** shifts every chart event relative to its timestamp. Positive values
hit later and negative values hit earlier. The default is `+18 ms`; changes are
saved to:

```text
%LOCALAPPDATA%\FNFBot\settings.json
```

The same file also controls the global hotkeys and lane inputs. The complete
default configuration is created automatically on first launch:

```json
{
  "hitBiasMs": 18,
  "hotkeys": {
    "start": "F1",
    "stop": "F4",
    "decreaseDelay": "F2",
    "increaseDelay": "F3"
  },
  "inputs": {
    "left": [ "A", "Left" ],
    "down": [ "S", "Down" ],
    "up": [ "W", "Up" ],
    "right": [ "D", "Right" ]
  },
  "colorScheme": "System"
}
```

Key names are case-insensitive and use the Windows Forms
[`Keys`](https://learn.microsoft.com/dotnet/api/system.windows.forms.keys)
names. `LeftArrow`, `RightArrow`, `UpArrow`, and `DownArrow` are also accepted
as aliases. Common punctuation can be written directly, including `.`, `/`,
`,`, `;`, `'`, `[`, `]`, `\`, `-`, `=`, and backtick. Shifted variants such
as `?`, `>`, `:`, and `+` map to the same physical keys. For example, the
inputs below configure a `Z`, `X`, `.`, `/` layout:

```json
"inputs": {
  "left": [ "Z" ],
  "down": [ "X" ],
  "up": [ "." ],
  "right": [ "/" ]
}
```

Each lane must contain at least one key; multiple keys let the bot handle
overlapping notes in that lane. The settings UI supports up to two keys per
lane, while manually edited JSON may contain additional valid keys.

`hitBiasMs` must be finite; the settings editor allows values from `-1000.0` to
`1000.0` in 0.5 ms increments. `colorScheme` accepts `System`, `Light`, or
`Dark`, with `System` as the default. System mode follows the current Windows
app theme when the theme is applied.

Restart the application after manually editing the JSON file. Property and key
names are case-insensitive. Unknown keys are ignored, invalid or empty lane
settings fall back to their defaults, and an unknown color scheme falls back to
`System`.

Timing can vary with the game build, display latency, audio latency, and system
load, so calibration may still be necessary.

## Supported Chart Layout

Songs are discovered when the following base chart exists:

```text
assets/data/songs/<song-name>/<song-name>-chart.json
```

Additional recognized files include:

```text
<song-name>-chart-erect.json
<song-name>-chart-bf.json
<song-name>-chart-pico.json
```

The loader expects the newer chart structure with a top-level `notes` object and
note entries containing:

```json
{
  "t": 1250,
  "d": 0,
  "l": 250
}
```

- `t`: note time in milliseconds
- `d`: direction from `0` (left) through `3` (right)
- `l`: optional sustain length in milliseconds

Opponent notes or other entries with a direction greater than `3` are ignored.

## Project Structure

```text
CLI/       Experimental command-line interface code
Core/      Bot orchestration and playback
Interop/   Windows hotkey and keyboard interop
Models/    Chart, note, and scheduled-event models
Services/  Chart loading, timing, settings, and input simulation
UI/        Windows Forms user interface
```

The current application entry point launches the Windows Forms interface. The
code in `CLI/` is not wired into the executable. Theme handling and the settings
dialog live in `UI/ThemeManager.cs` and `UI/SettingsForm.cs`; configuration
loading and validation live in `Services/UserSettings.cs`.

## Troubleshooting

- **No songs found:** Confirm that the selected folder contains
  `assets/data/songs` and that each song has its expected base chart file.
- **A difficulty is missing:** The difficulty is shown only when its chart file
  contains a matching property under `notes`.
- **Hotkeys do not work:** Another application may already have registered the
  configured keys. Close the conflicting program and reopen Settings or restart
  FNFBot.
- **Settings will not save:** Ensure all four hotkeys are different and every
  lane has at least one input key.
- **The theme preview remained after cancelling:** Close the settings dialog
  normally; the main window restores the saved theme when the dialog closes.
- **The game ignores inputs:** Run the bot at the same privilege level as the
  game. Windows can block simulated input from a lower-privileged process.
- **Notes are consistently early or late:** Adjust hit bias in 0.5 ms steps.
- **The whole chart is offset:** Adjust the start delay or try the predicted
  delay.

## Disclaimer

This project automates gameplay by simulating keyboard input. Use it only where
automation is permitted. It is not affiliated with the Friday Night Funkin'
developers.
