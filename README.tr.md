# CaYaVidFit

**ffmpeg** ile çalışan Windows video boyut ayarlayıcı (WPF / .NET 8).

Videoyu sürükleyip bırakın, küçültme veya artırma preset’i (veya **hedef MB**) seçin, dışa aktarma formatını belirleyin ve kodlayın. **Tek dosya** self-contained `.exe` olarak yayınlanır.

> English: [README.md](README.md) · Marka: [cayadev.com](https://cayadev.com)

## Özellikler

- **ffmpeg** otomatik bulma (PATH veya `./ffmpeg/`); isteğe bağlı kurulum (winget / gyan.dev)
- Görünür bırakma alanı + **animasyonlu tam pencere overlay**
- Girdi ve **çıktı** için Gözat
- **Küçültme preset’leri** — orijinal boyuttan büyük olmaz (~%95 / ~%82 / ~%65), boyut tavanlı iki geçiş
- **Artırma preset’leri** — Kalite+ (~%125) ve Ultra (~%160)
- **Hedef MB** iki geçiş H.264 + AAC (radyo grubu ile kapatılabilir)
- Hedefin altındaysa / bütçe yetmezse stream copy
- **Dışa aktarma formatı**: varsayılan içe aktarılan ile aynı; MP4 / MKV / MOV / WEBM / AVI
- Kompakt koyu arayüz (CaYaDev renkleri), çift dil **EN / TR** (OS diline göre)
- İlerleme, iptal, kopyalanabilir ffmpeg komutu, günlük

## Dil

Varsayılan **İngilizce**. Windows arayüz dili Türkçe (`tr-*`) ise arayüz Türkçe açılır.

## Gereksinimler

- Windows x64
- ffmpeg / ffprobe (uygulama kurabilir)

## Çalıştırma

```powershell
.\dist\CaYaVidFit.exe
```

Kaynaktan:

```powershell
cd CaYaVidFit
dotnet run --project .\src\CaYaVidFit\CaYaVidFit.csproj
```

## Tek dosya yayın

```powershell
dotnet publish .\src\CaYaVidFit\CaYaVidFit.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\dist
```

## Lisans

MIT — [LICENSE](LICENSE).
