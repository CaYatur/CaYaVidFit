namespace CaYaVidFit.Models;

public enum EncodeModeKind
{
    PresetHigh,      // shrink ~95%
    PresetMedium,    // shrink ~82%
    PresetLow,       // shrink ~65%
    PresetQualityPlus, // grow ~125% / higher quality
    PresetUltra,       // grow ~160% / max quality budget
    TargetMb
}

public sealed class EncodeRequest
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public required EncodeModeKind Mode { get; init; }
    public double? TargetMb { get; init; }
    public bool StreamCopyIfFits { get; init; }
}

public sealed class EncodePlan
{
    public required string[] Arguments { get; init; }
    public required string DisplayCommand { get; init; }
    public bool IsStreamCopy { get; init; }
    public string? Warning { get; init; }
    public double? EstimatedVideoBitrateKbps { get; init; }
    public double? EstimatedAudioBitrateKbps { get; init; }
}

public sealed class EncodeProgressEventArgs : EventArgs
{
    public double? Percent { get; init; }
    public string? StatusText { get; init; }
    public TimeSpan? CurrentTime { get; init; }
}
