namespace CaYaVidFit.Models;

public sealed class VideoInfo
{
    public string Path { get; init; } = "";
    public double DurationSeconds { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public long FileSizeBytes { get; init; }
    public string? VideoCodec { get; init; }
    public string? AudioCodec { get; init; }
    public double? FrameRate { get; init; }
    public bool HasAudio { get; init; }

    public double FileSizeMb => FileSizeBytes / (1024.0 * 1024.0);

    public string ResolutionLabel => Width > 0 && Height > 0 ? $"{Width}×{Height}" : "?";

    public string DurationLabel
    {
        get
        {
            if (DurationSeconds <= 0) return "?";
            var t = TimeSpan.FromSeconds(DurationSeconds);
            return t.TotalHours >= 1
                ? t.ToString(@"h\:mm\:ss")
                : t.ToString(@"mm\:ss");
        }
    }

    public string SizeLabel => $"{FileSizeMb:0.##} MB";
}
