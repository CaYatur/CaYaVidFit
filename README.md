# CaYaVidFit

<p align="center">
  <img src="docs/logo.png" alt="CaYaVidFit logo" width="128" height="128">
</p>

<p align="center">
  <strong>Windows video size fitter</strong> powered by <strong>ffmpeg</strong> (WPF / .NET 8).<br/>
  Drag-and-drop a video, pick a shrink or boost preset (or a <strong>target MB</strong>), choose an export format, then encode.<br/>
  Ships as a <strong>single-file</strong> self-contained <code>.exe</code> — no .NET install required on the target PC.
</p>

<p align="center">
  <a href="https://github.com/CaYatur/CaYaVidFit/releases"><img src="https://img.shields.io/github/v/release/CaYatur/CaYaVidFit?label=release" alt="Release"></a>
  <a href="https://github.com/CaYatur/CaYaVidFit/releases"><img src="https://img.shields.io/github/downloads/CaYatur/CaYaVidFit/total?label=downloads" alt="Downloads"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="MIT"></a>
  <a href="https://cayadev.com"><img src="https://img.shields.io/badge/brand-CaYaDev-C42021" alt="CaYaDev"></a>
</p>

<p align="center">
  <img src="docs/screenshot.png" alt="CaYaVidFit application screenshot (real English UI)" width="900">
</p>

> Turkish docs: [README.tr.md](README.tr.md) · Brand: [cayadev.com](https://cayadev.com)

---

## Download

Get the latest portable build from **[Releases](https://github.com/CaYatur/CaYaVidFit/releases)**.

1. Download `CaYaVidFit.exe`
2. Run it (Windows may show SmartScreen on first launch — *More info* → *Run anyway* if you trust the build)
3. If ffmpeg is missing, use **Install ffmpeg** in the app header

---

## What it does

CaYaVidFit wraps **ffmpeg / ffprobe** in a small desktop UI so you can:

- Shrink a video to a **guaranteed fraction of the original file size** (or smaller)
- Optionally **raise** size/quality budget with boost presets
- Hit an exact **target size in MB** with two-pass H.264 + AAC
- Keep the **same container as the input**, or pick MP4 / MKV / MOV / WEBM / AVI
- See the exact ffmpeg command and copy it

It is built for everyday “this file is too big for upload / chat / mail” workflows — not a full NLE.

---

## Features

### Input & output
- Large drop zone + **animated full-window drop overlay** when you drag a file over the app
- **Browse** for input and output paths
- Default output name: `*_fitted.<ext>` next to the source (extension follows the selected format)
- Supported inputs include MP4, MKV, MOV, AVI, WEBM, M4V, WMV, FLV, TS/MTS, MPEG, …

### Encode modes

| Mode | Behavior |
|------|----------|
| **Shrink High (~95%)** | Two-pass size cap ≈ 95% of original — best quality among shrink presets |
| **Shrink Medium (~82%)** | Two-pass size cap ≈ 82% of original (default) |
| **Shrink Low (~65%)** | Two-pass size cap ≈ 65% of original — smallest shrink preset |
| **Boost Quality+ (~125%)** | Two-pass budget ≈ 125% of original — more bits / higher quality |
| **Boost Ultra (~160%)** | Two-pass budget ≈ 160% of original |
| **Target MB** | Plan video/audio bitrates for a chosen megabyte target (two-pass H.264 + AAC) |

**Important:** Shrink presets are designed to **never grow** the file vs the original. Plain CRF-only encodes can inflate already-efficient sources; CaYaVidFit uses a **size-capped two-pass** plan instead.

### Stream copy
If the file is already under the target / the size budget is too tight for a sensible bitrate, the app can **stream-copy** (`-c copy`) instead of re-encoding (when the checkbox is enabled).

### Export format
- **Same as input** (default)
- Or force: **MP4**, **MKV**, **MOV**, **WEBM**, **AVI**

### UI / UX
- Dark CaYaDev palette (`#050505` / accent `#C42021`)
- Compact layout: Format + Output on one row; ffmpeg command + Log side by side
- Progress bar, **Cancel**, copyable command, timestamped log
- Window opens **centered** and clamped to the work area

### Language
- Default: **English**
- If Windows UI culture is Turkish (`tr-*`): Turkish strings automatically
- Optional override for testing / screenshots:

```powershell
$env:CAYAVIDFIT_LANG = "en-US"   # or "tr-TR"
.\CaYaVidFit.exe
```

---

## Requirements

- **Windows x64**
- **ffmpeg** + **ffprobe**
  - Detected from `PATH`, or from `./ffmpeg/` next to the app
  - Optional in-app install (winget / gyan.dev essentials zip)

---

## Quick start

1. Open `CaYaVidFit.exe`
2. Drop a video (or **Browse…**)
3. Pick a **Shrink** / **Boost** preset or **Target MB**
4. Choose **Format** if you need a specific container
5. Confirm **Output** path
6. Click **Start**

Cancel stops the running ffmpeg process.

---

## Build from source

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows)

### Run

```powershell
cd CaYaVidFit
dotnet run --project .\src\CaYaVidFit\CaYaVidFit.csproj
```

### Publish single-file (portable)

```powershell
dotnet publish .\src\CaYaVidFit\CaYaVidFit.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\dist
```

Or:

```powershell
.\tools\Publish.ps1
```

Output: `.\dist\CaYaVidFit.exe`

---

## Project layout

```
CaYaVidFit/
  CaYaVidFit.sln
  README.md / README.tr.md
  LICENSE
  docs/
    logo.png
    screenshot.png          # real English UI capture
  src/CaYaVidFit/
    App.xaml(.cs)
    MainWindow.xaml(.cs)
    Assets/logo.png, app.ico
    Models/                 # encode modes, video info, paths
    Services/               # probe, encode, bitrate, ffmpeg locate/install, Loc
    ffmpeg/.gitkeep         # optional local ffmpeg bundle folder
  tools/Publish.ps1
```

---

## Notes & limitations

- Primary encode path is **H.264 (libx264) + AAC** in a container you choose; exotic codecs/containers may need manual ffmpeg
- Two-pass encodes write temporary pass-log files under the system temp folder and clean them up afterward
- Target MB is a **plan** (bitrate math + mux overhead margin). Slight overshoot can still happen because of container overhead
- WEBM/AVI selection changes the output extension; codec pipeline remains the app’s H.264/AAC-oriented path unless you customize the command externally
- This is an MVP desktop tool — no GPU encode UI, no timeline editor, no batch queue yet

---

## Troubleshooting

| Issue | What to try |
|-------|-------------|
| App won’t start / silent exit | Check `CaYaVidFit_crash.txt` on the Desktop |
| `ffmpeg: missing` | Use **Install ffmpeg**, or put `ffmpeg.exe` + `ffprobe.exe` on PATH / in `./ffmpeg/` |
| Drop overlay stuck | Fixed in current builds — update from Releases; restart the app |
| Output larger than source on “Shrink” | Use a current Release; shrink modes are size-capped two-pass |
| SmartScreen warning | Expected for unsigned portable exes; verify you downloaded from this GitHub repo |

---

## License

MIT — see [LICENSE](LICENSE).

Made by [CaYaDev](https://cayadev.com) / Çağan Turgut.
