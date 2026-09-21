namespace CaYaVidFit.Models;

public sealed class FfmpegPaths
{
    public required string Ffmpeg { get; init; }
    public required string Ffprobe { get; init; }
    public required string Source { get; init; } // PATH | Local | Missing
    public bool IsAvailable => Source != "Missing"
        && !string.IsNullOrWhiteSpace(Ffmpeg)
        && !string.IsNullOrWhiteSpace(Ffprobe);
}
