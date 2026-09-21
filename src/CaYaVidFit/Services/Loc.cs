using System.Globalization;

namespace CaYaVidFit.Services;

/// <summary>UI strings: English default; Turkish when OS UI culture is tr-*.</summary>
public static class Loc
{
    public static bool IsTurkish { get; } =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", StringComparison.OrdinalIgnoreCase);

    public static string T(string en, string tr) => IsTurkish ? tr : en;

    public static string AppTitle => T("CaYaVidFit - Video Size Fitter", "CaYaVidFit - Video Boyut Ayarlayıcı");
    public static string Subtitle => T("Fit video size with ffmpeg", "ffmpeg ile video boyutunu ayarla");
    public static string Refresh => T("Refresh", "Yenile");
    public static string InstallFfmpeg => T("Install ffmpeg", "ffmpeg Kur");
    public static string Browse => T("Browse...", "Gözat...");
        public static string DropTitle => T("Drop video here", "Videoyu buraya bırak");
    public static string DropZoneHint => T("or click Browse — MP4, MKV, MOV, AVI, WEBM…", "veya Gözat’a tıkla — MP4, MKV, MOV, AVI, WEBM…");
    public static string OutputLabel => T("Output:", "Çıktı:");
    public static string DropHint => T("Drag-drop or Browse", "Sürükle-bırak veya Gözat");
    public static string NoVideo => T("No video selected", "Video seçilmedi");
    public static string PresetHigh => T("Shrink High — under original (~95%)", "Küçült Yüksek — orijinalden küçük (~%95)");
    public static string PresetMedium => T("Shrink Medium — under original (~82%)", "Küçült Orta — orijinalden küçük (~%82)");
    public static string PresetLow => T("Shrink Low — under original (~65%)", "Küçült Düşük — orijinalden küçük (~%65)");
    public static string TargetMb => T("Target MB (two-pass H.264+AAC):", "Hedef MB (iki geçiş H.264+AAC):");
    public static string StreamCopy => T("Stream copy if already under target / budget too tight", "Hedefin altındaysa veya bütçe yetmezse stream copy");
    public static string Start => T("Start", "Başlat");
    public static string Cancel => T("Cancel", "İptal");
    public static string CopyCmd => T("Copy command", "Komutu kopyala");
    public static string Ready => T("CaYaVidFit ready. Drop a video or Browse.", "CaYaVidFit hazır. Video sürükleyin veya Gözat.");
    public static string FfmpegMissing => T("ffmpeg: missing", "ffmpeg: eksik");
    public static string FfmpegOk(string src) => T($"ffmpeg: {src}", $"ffmpeg: {src}");
    public static string ProbeError(string m) => T("Probe error: ", "Probe hatası: ") + m;
    public static string InvalidTarget => T("(invalid target MB)", "(geçersiz hedef MB)");
    public static string WingetFailAsk => T(
        "winget failed or ffmpeg still missing.\nDownload gyan.dev essentials zip into ./ffmpeg/ ?",
        "winget başarısız veya ffmpeg hâlâ yok.\ngyan.dev essentials zip ./ffmpeg/ altına indirilsin mi?");
    public static string InstallTitle => T("Install ffmpeg", "ffmpeg kur");
    public static string InstallError(string m) => T("Install error: ", "Kurulum hatası: ") + m;

    public static string PresetSizeCap(string srcLabel, double budgetMb, int crfHint) => T(
        $"Size-capped two-pass: budget {budgetMb:0.##} MB (source {srcLabel}). Plain CRF can grow files.",
        $"Boyut tavanlı iki geçiş: bütçe {budgetMb:0.##} MB (kaynak {srcLabel}). Düz CRF dosyayı büyütebilir.");
    public static string PresetTooTightCopy(string srcLabel, double budgetMb) => T(
        $"Budget {budgetMb:0.##} MB too tight for {srcLabel}; stream-copying instead of growing.",
        $"Bütçe {budgetMb:0.##} MB, {srcLabel} için çok sıkı; büyütmek yerine stream copy.");
    public static string BrowseOut => T("Browse...", "Gözat...");

    public static string PresetQualityPlus => T("Boost Quality+ — above original (~125%)", "Artır Kalite+ — orijinalden büyük (~%125)");
    public static string PresetUltra => T("Boost Ultra — above original (~160%)", "Artır Ultra — orijinalden büyük (~%160)");
    public static string FormatLabel => T("Format:", "Format:");
    public static string FormatSame => T("Same as input", "İçe aktarılan ile aynı");
    public static string DropOverlayTitle => T("Drop to import", "Bırak — içe aktar");
    public static string DropOverlayHint => T("Release to load this video", "Videoyu yüklemek için bırakın");

    public static string CommandHeader => T("ffmpeg command", "ffmpeg komutu");
    public static string LogHeader => T("Log", "Günlük");
}
