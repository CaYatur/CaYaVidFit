using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using CaYaVidFit.Models;

namespace CaYaVidFit.Services;

public sealed class EncodeService
{
    private static readonly Regex TimeRegex = new(@"time=(\d+):(\d+):(\d+[.,]\d+)", RegexOptions.Compiled);
    private static readonly Regex ProgressOutRegex = new(@"^(out_time_ms|out_time_us|progress|speed)=", RegexOptions.Compiled);

    private readonly FfmpegPaths _paths;

    public EncodeService(FfmpegPaths paths) => _paths = paths;

    public EncodePlan BuildPlan(VideoInfo info, EncodeRequest req)
    {
        if (!_paths.IsAvailable)
            throw new InvalidOperationException("ffmpeg bulunamadÄ± / ffmpeg not found");

        // Already fits target â†’ stream copy or skip
        if (req.Mode == EncodeModeKind.TargetMb && req.TargetMb is double tmb
            && info.FileSizeMb <= tmb + 0.01)
        {
            if (req.StreamCopyIfFits)
            {
                var copyArgs = BuildStreamCopyArgs(req.InputPath, req.OutputPath);
                return new EncodePlan
                {
                    Arguments = copyArgs,
                    DisplayCommand = ProcessRunner.FormatCommand(_paths.Ffmpeg, copyArgs),
                    IsStreamCopy = true,
                    Warning = $"Girdi zaten â‰¤ {tmb:0.##} MB ({info.SizeLabel}). Yeniden kodlama yok â€” stream copy.\n" +
                              $"Input already â‰¤ {tmb:0.##} MB. Skipping re-encode â€” stream copy."
                };
            }

            return new EncodePlan
            {
                Arguments = Array.Empty<string>(),
                DisplayCommand = "(skip â€” no encode)",
                IsStreamCopy = false,
                Warning = $"Girdi zaten â‰¤ hedef ({info.SizeLabel} â‰¤ {tmb:0.##} MB). Ä°ÅŸlem atlandÄ±.\n" +
                          $"Input already within target. Skipped."
            };
        }

        if (req.Mode == EncodeModeKind.TargetMb)
        {
            var target = req.TargetMb ?? throw new ArgumentException("TargetMb required");
            var (ok, vBitrate, aBitrate, err) = BitrateCalculator.PlanTargetMb(info, target);
            if (!ok)
                throw new InvalidOperationException(err ?? "Hedef geÃ§ersiz / Invalid target");

            var args = BuildTwoPassArgs(req.InputPath, req.OutputPath, vBitrate, aBitrate, pass: 1);
            // Display shows both passes conceptually; actual run does pass1 then pass2
            var display = BuildTwoPassDisplay(req.InputPath, req.OutputPath, vBitrate, aBitrate);
            return new EncodePlan
            {
                Arguments = args, // pass 1 first; EncodeAsync runs both
                DisplayCommand = display,
                IsStreamCopy = false,
                EstimatedVideoBitrateKbps = vBitrate,
                EstimatedAudioBitrateKbps = aBitrate
            };
        }

        // Presets must NEVER grow the file vs original size.
        // Two-pass with a size budget below the source; if bitrate floor fails, stream-copy.
        var fraction = req.Mode switch
        {
            EncodeModeKind.PresetHigh => 0.95,
            EncodeModeKind.PresetMedium => 0.82,
            EncodeModeKind.PresetLow => 0.65,
            EncodeModeKind.PresetQualityPlus => 1.25,
            EncodeModeKind.PresetUltra => 1.60,
            _ => 0.82
        };
        var budgetMb = Math.Max(0.05, info.FileSizeMb * fraction);
        var (okP, vBitrateP, aBitrateP, errP) = BitrateCalculator.PlanTargetMb(info, budgetMb);
        if (!okP)
        {
            if (req.StreamCopyIfFits)
            {
                var copyArgs = BuildStreamCopyArgs(req.InputPath, req.OutputPath);
                return new EncodePlan
                {
                    Arguments = copyArgs,
                    DisplayCommand = ProcessRunner.FormatCommand(_paths.Ffmpeg, copyArgs),
                    IsStreamCopy = true,
                    Warning = Loc.PresetTooTightCopy(info.SizeLabel, budgetMb)
                };
            }
            throw new InvalidOperationException(errP ?? "Preset size budget too tight.");
        }

        var displayP = BuildTwoPassDisplay(req.InputPath, req.OutputPath, vBitrateP, aBitrateP);
        var pass1P = BuildTwoPassArgs(req.InputPath, req.OutputPath, vBitrateP, aBitrateP, pass: 1);
        return new EncodePlan
        {
            Arguments = pass1P,
            DisplayCommand = displayP,
            IsStreamCopy = false,
            EstimatedVideoBitrateKbps = vBitrateP,
            EstimatedAudioBitrateKbps = aBitrateP,
            Warning = Loc.PresetSizeCap(info.SizeLabel, budgetMb, BitrateCalculator.CrfForPreset(req.Mode))
        };
    }

    public async Task EncodeAsync(
        VideoInfo info,
        EncodeRequest req,
        EncodePlan plan,
        IProgress<EncodeProgressEventArgs>? progress,
        CancellationToken ct)
    {
        if (plan.Arguments.Length == 0 && plan.Warning is not null && !plan.IsStreamCopy)
        {
            progress?.Report(new EncodeProgressEventArgs { Percent = 100, StatusText = plan.Warning });
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(req.OutputPath)!);

        // Two-pass whenever we planned bitrates (Target MB or size-capped presets)
        if (!plan.IsStreamCopy && plan.EstimatedVideoBitrateKbps is double)
        {
            var v = plan.EstimatedVideoBitrateKbps ?? 0;
            var a = plan.EstimatedAudioBitrateKbps ?? 0;
            var passLog = Path.Combine(Path.GetTempPath(), "cayavidfit_ffmpeg2pass");

            progress?.Report(new EncodeProgressEventArgs { Percent = 0, StatusText = "Pass 1/2..." });
            var pass1 = BuildTwoPassArgs(req.InputPath, req.OutputPath, v, a, pass: 1, passLog);
            await RunFfmpegAsync(pass1, info.DurationSeconds, progress, 0, 50, ct);

            progress?.Report(new EncodeProgressEventArgs { Percent = 50, StatusText = "Pass 2/2..." });
            var pass2 = BuildTwoPassArgs(req.InputPath, req.OutputPath, v, a, pass: 2, passLog);
            await RunFfmpegAsync(pass2, info.DurationSeconds, progress, 50, 50, ct);

            CleanupPassLog(passLog);
        }
        else
        {
            await RunFfmpegAsync(plan.Arguments, info.DurationSeconds, progress, 0, 100, ct);
        }

        progress?.Report(new EncodeProgressEventArgs { Percent = 100, StatusText = "Tamam / Done" });
    }

    private async Task RunFfmpegAsync(
        string[] args,
        double durationSec,
        IProgress<EncodeProgressEventArgs>? progress,
        double percentBase,
        double percentSpan,
        CancellationToken ct)
    {
        void OnLine(string line)
        {
            // -progress pipe:2 style
            if (line.StartsWith("out_time_ms=", StringComparison.Ordinal))
            {
                if (long.TryParse(line["out_time_ms=".Length..], out var ms) && durationSec > 0)
                {
                    var pct = Math.Clamp(ms / 1000.0 / durationSec * percentSpan + percentBase, 0, percentBase + percentSpan);
                    progress?.Report(new EncodeProgressEventArgs
                    {
                        Percent = pct,
                        CurrentTime = TimeSpan.FromMilliseconds(ms),
                        StatusText = $"encode {pct:0.0}%"
                    });
                }
                return;
            }
            if (line.StartsWith("out_time_us=", StringComparison.Ordinal))
            {
                if (long.TryParse(line["out_time_us=".Length..], out var us) && durationSec > 0)
                {
                    var pct = Math.Clamp(us / 1_000_000.0 / durationSec * percentSpan + percentBase, 0, percentBase + percentSpan);
                    progress?.Report(new EncodeProgressEventArgs
                    {
                        Percent = pct,
                        CurrentTime = TimeSpan.FromTicks(us * 10),
                        StatusText = $"encode {pct:0.0}%"
                    });
                }
                return;
            }

            var m = TimeRegex.Match(line);
            if (m.Success && durationSec > 0)
            {
                var h = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                var min = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                var secStr = m.Groups[3].Value.Replace(',', '.');
                var sec = double.Parse(secStr, CultureInfo.InvariantCulture);
                var t = h * 3600 + min * 60 + sec;
                var pct = Math.Clamp(t / durationSec * percentSpan + percentBase, 0, percentBase + percentSpan);
                progress?.Report(new EncodeProgressEventArgs
                {
                    Percent = pct,
                    CurrentTime = TimeSpan.FromSeconds(t),
                    StatusText = $"encode {pct:0.0}%"
                });
            }
        }

        // Prefer -progress on stderr (pipe:2) for reliable parsing
        var fullArgs = new List<string> { "-y", "-hide_banner", "-progress", "pipe:2" };
        // strip leading -y if present in plan
        foreach (var a in args)
        {
            if (a == "-y") continue;
            fullArgs.Add(a);
        }

        var (exit, _, stderr) = await ProcessRunner.RunAsync(
            _paths.Ffmpeg, fullArgs, ct, onStderrLine: OnLine);

        if (ct.IsCancellationRequested)
            throw new OperationCanceledException(ct);

        if (exit != 0)
            throw new InvalidOperationException($"ffmpeg exit {exit}\n{TrimErr(stderr)}");
    }

    private static string TrimErr(string s)
    {
        var lines = s.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join('\n', lines.TakeLast(12));
    }

    private static string[] BuildCrfArgs(string input, string output, int crf) =>
    [
        "-y",
        "-i", input,
        "-c:v", "libx264",
        "-preset", "medium",
        "-crf", crf.ToString(CultureInfo.InvariantCulture),
        "-c:a", "aac",
        "-b:a", "128k",
        "-movflags", "+faststart",
        output
    ];

    private static string[] BuildStreamCopyArgs(string input, string output) =>
    [
        "-y",
        "-i", input,
        "-c", "copy",
        "-movflags", "+faststart",
        output
    ];

    private string[] BuildTwoPassArgs(string input, string output, double videoKbps, double audioKbps, int pass, string? passLog = null)
    {
        passLog ??= Path.Combine(Path.GetTempPath(), "cayavidfit_ffmpeg2pass");
        var v = ((int)Math.Round(videoKbps)).ToString(CultureInfo.InvariantCulture) + "k";
        var a = ((int)Math.Round(audioKbps)).ToString(CultureInfo.InvariantCulture) + "k";

        if (pass == 1)
        {
            return
            [
                "-y",
                "-i", input,
                "-c:v", "libx264",
                "-b:v", v,
                "-pass", "1",
                "-passlogfile", passLog,
                "-an",
                "-f", "mp4",
                "NUL"
            ];
        }

        var args = new List<string>
        {
            "-y",
            "-i", input,
            "-c:v", "libx264",
            "-b:v", v,
            "-pass", "2",
            "-passlogfile", passLog,
            "-movflags", "+faststart"
        };
        if (audioKbps > 0)
        {
            args.AddRange(["-c:a", "aac", "-b:a", a]);
        }
        else
        {
            args.Add("-an");
        }
        args.Add(output);
        return args.ToArray();
    }

    private string BuildTwoPassDisplay(string input, string output, double videoKbps, double audioKbps)
    {
        var p1 = BuildTwoPassArgs(input, output, videoKbps, audioKbps, 1);
        var p2 = BuildTwoPassArgs(input, output, videoKbps, audioKbps, 2);
        return "# Pass 1\n" + ProcessRunner.FormatCommand(_paths.Ffmpeg, p1)
               + "\n# Pass 2\n" + ProcessRunner.FormatCommand(_paths.Ffmpeg, p2);
    }

    private static void CleanupPassLog(string passLog)
    {
        foreach (var f in new[] { passLog + "-0.log", passLog + "-0.log.mbtree", passLog + ".log", passLog + ".log.mbtree" })
        {
            try { if (File.Exists(f)) File.Delete(f); } catch { /* ignore */ }
        }
    }

    public static string DefaultOutputPath(string inputPath)
    {
        var dir = Path.GetDirectoryName(inputPath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(inputPath);
        var ext = Path.GetExtension(inputPath);
        if (string.IsNullOrEmpty(ext)) ext = ".mp4";
        // Always write .mp4 for re-encodes; keep ext for stream-copy callers who override
        return Path.Combine(dir, $"{name}_fitted{ext}");
    }

    public static string DefaultFittedMp4(string inputPath)
    {
        var dir = Path.GetDirectoryName(inputPath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(inputPath);
        return Path.Combine(dir, $"{name}_fitted.mp4");
    }

    public static string ResolveOutputPath(string inputPath, string? preferredExt)
    {
        var dir = Path.GetDirectoryName(inputPath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(inputPath);
        var ext = string.IsNullOrWhiteSpace(preferredExt)
            ? Path.GetExtension(inputPath)
            : preferredExt;
        if (string.IsNullOrWhiteSpace(ext)) ext = ".mp4";
        if (!ext.StartsWith('.')) ext = "." + ext;
        ext = ext.ToLowerInvariant();
        // Keep container-friendly defaults
        if (ext is not (".mp4" or ".mkv" or ".mov" or ".webm" or ".avi" or ".m4v"))
            ext = ".mp4";
        return Path.Combine(dir, $"{name}_fitted{ext}");
    }
}
