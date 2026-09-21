using CaYaVidFit.Models;

namespace CaYaVidFit.Services;

/// <summary>
/// Target-MB two-pass H.264 + AAC bitrate planning.
/// Audio floor ~96 kbps AAC; container overhead ~2%.
/// </summary>
public static class BitrateCalculator
{
    public const double DefaultAudioKbps = 96.0;
    public const double MinVideoKbps = 50.0;
    public const double MuxOverheadFactor = 0.98; // leave ~2% for container

    public static (bool Ok, double VideoKbps, double AudioKbps, string? Error) PlanTargetMb(
        VideoInfo info, double targetMb)
    {
        if (targetMb <= 0)
            return (false, 0, 0, "Hedef MB sıfırdan büyük olmalı. / Target MB must be > 0.");

        if (info.DurationSeconds <= 0.5)
            return (false, 0, 0, "Süre okunamadı veya çok kısa. / Duration missing or too short.");

        var targetBits = targetMb * 1024.0 * 1024.0 * 8.0 * MuxOverheadFactor;
        var totalKbps = targetBits / info.DurationSeconds / 1000.0;

        var audioKbps = info.HasAudio ? DefaultAudioKbps : 0.0;
        var videoKbps = totalKbps - audioKbps;

        if (videoKbps < MinVideoKbps)
        {
            var audioOnlyMb = (audioKbps * 1000.0 * info.DurationSeconds / 8.0) / (1024.0 * 1024.0);
            var floorMb = audioOnlyMb / MuxOverheadFactor + 0.05;
            return (false, videoKbps, audioKbps,
                $"Hedef çok küçük. Ses-only taban ≈ {floorMb:0.##} MB (AAC {audioKbps:0} kbps). " +
                $"En az ~{floorMb + 0.2:0.##} MB deneyin.\n" +
                $"Target too small. Audio-only floor ≈ {floorMb:0.##} MB. Try at least ~{floorMb + 0.2:0.##} MB.");
        }

        return (true, videoKbps, audioKbps, null);
    }

    /// <summary>CRF presets: High≈18, Medium≈23, Low≈28 (libx264).</summary>
    public static int CrfForPreset(EncodeModeKind mode) => mode switch
    {
        EncodeModeKind.PresetHigh => 18,
        EncodeModeKind.PresetMedium => 23,
        EncodeModeKind.PresetLow => 28,
        EncodeModeKind.PresetQualityPlus => 18,
        EncodeModeKind.PresetUltra => 15,
        _ => 23
    };

    public static string PresetLabel(EncodeModeKind mode) => mode switch
    {
        EncodeModeKind.PresetHigh => "High — under original (~95%)",
        EncodeModeKind.PresetMedium => "Medium — under original (~82%)",
        EncodeModeKind.PresetLow => "Low — under original (~65%)",
        EncodeModeKind.PresetQualityPlus => "Quality+ — above original (~125%)",
        EncodeModeKind.PresetUltra => "Ultra — above original (~160%)",
        EncodeModeKind.TargetMb => "Target MB — two-pass H.264+AAC",
        _ => mode.ToString()
    };
}
