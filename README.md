# CaYaVidFit

<p align="center">
  <img src="docs/logo.png" alt="CaYaVidFit logo" width="128" height="128">
</p>

<p align="center">
  <strong>Windows video size fitter</strong> powered by <strong>ffmpeg</strong> (WPF / .NET 8).<br/>
  Drag-and-drop a video, pick a shrink or boost preset (or a <strong>target MB</strong>), choose an export format, then encode.<br/>
  Ships as a <strong>single-file</strong> self-contained <code>.exe</code>.
</p>

<p align="center">
  <a href="https://github.com/CaYatur/CaYaVidFit/releases"><img src="https://img.shields.io/github/v/release/CaYatur/CaYaVidFit?label=release" alt="Release"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="MIT"></a>
  <a href="https://cayadev.com"><img src="https://img.shields.io/badge/brand-CaYaDev-C42021" alt="CaYaDev"></a>
</p>

<p align="center">
  <img src="docs/screenshot.png" alt="CaYaVidFit application screenshot" width="900">
</p>

> Turkish docs: [README.tr.md](README.tr.md) · Brand: [cayadev.com](https://cayadev.com)

## Download

Get the latest portable build from **[Releases](https://github.com/CaYatur/CaYaVidFit/releases)**.

## Features

- Auto-detect **ffmpeg** (PATH or `./ffmpeg/`); optional install via winget / gyan.dev zip
- Visible drop zone + **animated full-window drop overlay**
- Browse input and **output** paths
- **Shrink presets** — always stay under the original size (~95% / ~82% / ~65%) via size-capped two-pass
- **Boost presets** — Quality+ (~125%) and Ultra (~160%) to raise size/quality budget
- **Target MB** two-pass H.264 + AAC (shared radio group so it can be turned off)
- Stream-copy when already under target / budget too tight
- **Export format**: same as input (default), or MP4 / MKV / MOV / WEBM / AVI
- Compact dark UI (CaYaDev palette), bilingual **EN / TR** (follows OS UI language)
- Progress, cancel, copyable ffmpeg command, log panel

## Language

UI defaults to **English**. If the Windows display language is Turkish (`tr-*`), the UI uses Turkish automatically.

## Requirements

- Windows x64
- ffmpeg / ffprobe (the app can install them)

## Run (portable)

```powershell
# from a Release download, or after publish:
.\dist\CaYaVidFit.exe
```

From source:

```powershell
cd CaYaVidFit
dotnet run --project .\src\CaYaVidFit\CaYaVidFit.csproj
```

## Publish single-file

```powershell
dotnet publish .\src\CaYaVidFit\CaYaVidFit.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\dist
```

Or: `.\tools\Publish.ps1`

## Project layout

```
CaYaVidFit/
  CaYaVidFit.sln
  README.md / README.tr.md
  LICENSE
  docs/logo.png, docs/screenshot.png
  src/CaYaVidFit/          # WPF app
    Assets/logo.png, app.ico
    Models/, Services/
  tools/Publish.ps1
```

## License

MIT — see [LICENSE](LICENSE).