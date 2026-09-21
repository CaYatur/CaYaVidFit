# CaYaVidFit

Windows video size fitter powered by **ffmpeg** (WPF / .NET 8).

Drag-and-drop a video, pick a shrink or boost preset (or a **target MB**), choose an export format, then encode. Ships as a **single-file** self-contained `.exe`.

> Turkish: [README.tr.md](README.tr.md) · Brand: [cayadev.com](https://cayadev.com)

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
- ffmpeg / ffprobe (app can install them)

## Run (portable)

```powershell
# published single-file exe (after publish below)
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

## Project layout

```
CaYaVidFit/
  CaYaVidFit.sln
  README.md / README.tr.md
  LICENSE
  src/CaYaVidFit/          # WPF app
    Assets/logo.png, app.ico
    Models/, Services/
```

## License

MIT — see [LICENSE](LICENSE).
