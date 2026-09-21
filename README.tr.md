# CaYaVidFit

<p align="center">
  <img src="docs/logo.png" alt="CaYaVidFit logo" width="128" height="128">
</p>

<p align="center">
  <strong>Windows video boyut ayarlayıcı</strong> — <strong>ffmpeg</strong> (WPF / .NET 8).<br/>
  Videoyu sürükleyip bırakın, küçültme veya artırma preset’i (veya <strong>hedef MB</strong>) seçin, formatı belirleyin, kodlayın.<br/>
  <strong>Tek dosya</strong> self-contained <code>.exe</code> — hedef PC’de .NET kurulumu gerekmez.
</p>

<p align="center">
  <img src="docs/screenshot.png" alt="CaYaVidFit ekran görüntüsü" width="900">
</p>

<p align="center">
  <a href="https://github.com/CaYatur/CaYaVidFit/releases"><img src="https://img.shields.io/github/v/release/CaYatur/CaYaVidFit?label=release" alt="Release"></a>
  <a href="https://github.com/CaYatur/CaYaVidFit/releases"><img src="https://img.shields.io/github/downloads/CaYatur/CaYaVidFit/total?label=downloads" alt="Downloads"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="MIT"></a>
  <a href="https://cayadev.com"><img src="https://img.shields.io/badge/brand-CaYaDev-C42021" alt="CaYaDev"></a>
</p>


> English: [README.md](README.md) · Marka: [cayadev.com](https://cayadev.com)

## İndirme

En güncel taşınabilir sürüm: **[Releases](https://github.com/CaYatur/CaYaVidFit/releases)**.

1. `CaYaVidFit.exe` indirin
2. Çalıştırın (SmartScreen çıkarsa ve kaynağa güveniyorsanız *Ek bilgi* → *Yine de çalıştır*)
3. ffmpeg yoksa üstteki **ffmpeg Kur** ile kurun

## Ne işe yarar?

Günlük “dosya yükleme / sohbet / mail için çok büyük” senaryoları için:

- Orijinal boyuta göre **garanti küçültme** (shrink)
- İsteğe bağlı **kalite / boyut artırma** (boost)
- **Hedef MB** ile iki geçişli H.264 + AAC
- **Aynı konteyner** veya MP4 / MKV / MOV / WEBM / AVI
- Tam ffmpeg komutunu görüp kopyalama

Tam bir video editörü değildir.

## Özellikler (özet)

| Mod | Davranış |
|-----|----------|
| Küçült Yüksek (~%95) | Boyut tavanı ≈ orijinalin %95’i |
| Küçült Orta (~%82) | Varsayılan |
| Küçült Düşük (~%65) | En agresif küçültme preset’i |
| Artır Kalite+ (~%125) | Bütçe ≈ %125 |
| Artır Ultra (~%160) | Bütçe ≈ %160 |
| Hedef MB | Seçilen MB için bitrate planı + iki geçiş |

Küçültme preset’leri **orijinalden büyük üretmemek** için tasarlandı (boyut tavanlı iki geçiş). Düz CRF bazen dosyayı büyütebilir; bu yüzden burada kullanılmaz.

Diğerleri: sürükle-bırak overlay, girdi/çıktı Gözat, stream copy seçeneği, CaYaDev koyu tema, EN/TR (OS diline göre), `CAYAVIDFIT_LANG` ile zorla dil.

```powershell
$env:CAYAVIDFIT_LANG = "tr-TR"
.\CaYaVidFit.exe
```

## Gereksinimler

- Windows x64
- ffmpeg + ffprobe (PATH veya `./ffmpeg/`; uygulama kurabilir)

## Kaynaktan derleme

```powershell
cd CaYaVidFit
dotnet run --project .\src\CaYaVidFit\CaYaVidFit.csproj

dotnet publish .\src\CaYaVidFit\CaYaVidFit.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\dist
```

veya `.\tools\Publish.ps1`

## Sorun giderme

- Sessiz kapanma → Masaüstünde `CaYaVidFit_crash.txt`
- ffmpeg eksik → uygulama içi kurulum veya PATH
- Küçültme büyütüyorsa → Releases’ten güncel sürümü alın

## Lisans

MIT — [LICENSE](LICENSE).

[CaYaDev](https://cayadev.com) / Çağan Turgut.
