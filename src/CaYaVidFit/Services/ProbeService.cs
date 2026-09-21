using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CaYaVidFit.Models;

namespace CaYaVidFit.Services;

public sealed class ProbeService
{
    private readonly FfmpegPaths _paths;

    public ProbeService(FfmpegPaths paths) => _paths = paths;

    public async Task<VideoInfo> ProbeAsync(string inputPath, CancellationToken ct = default)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Video bulunamadÄ± / Video not found", inputPath);
        if (!_paths.IsAvailable)
            throw new InvalidOperationException("ffprobe bulunamadÄ± / ffprobe not found");

        var args = new[]
        {
            "-v", "quiet",
            "-print_format", "json",
            "-show_format",
            "-show_streams",
            inputPath
        };

        var (exit, stdout, stderr) = await ProcessRunner.RunAsync(_paths.Ffprobe, args, ct);
        if (exit != 0)
            throw new InvalidOperationException($"ffprobe hata / failed (exit {exit}): {stderr}");

        using var doc = JsonDocument.Parse(stdout);
        var root = doc.RootElement;

        double duration = 0;
        long size = new FileInfo(inputPath).Length;
        if (root.TryGetProperty("format", out var format))
        {
            if (format.TryGetProperty("duration", out var d) &&
                double.TryParse(d.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var dv))
                duration = dv;
            if (format.TryGetProperty("size", out var sz) &&
                long.TryParse(sz.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                size = parsed;
        }

        int w = 0, h = 0;
        string? vcodec = null, acodec = null;
        double? fps = null;
        bool hasAudio = false;

        if (root.TryGetProperty("streams", out var streams) && streams.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in streams.EnumerateArray())
            {
                var codecType = s.TryGetProperty("codec_type", out var ctEl) ? ctEl.GetString() : null;
                if (codecType == "video" && w == 0)
                {
                    if (s.TryGetProperty("width", out var wi)) w = wi.GetInt32();
                    if (s.TryGetProperty("height", out var hi)) h = hi.GetInt32();
                    if (s.TryGetProperty("codec_name", out var cn)) vcodec = cn.GetString();
                    if (s.TryGetProperty("avg_frame_rate", out var afr))
                        fps = ParseFraction(afr.GetString());
                    if (duration <= 0 && s.TryGetProperty("duration", out var sd) &&
                        double.TryParse(sd.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var sdv))
                        duration = sdv;
                }
                else if (codecType == "audio")
                {
                    hasAudio = true;
                    if (acodec is null && s.TryGetProperty("codec_name", out var acn))
                        acodec = acn.GetString();
                }
            }
        }

        return new VideoInfo
        {
            Path = inputPath,
            DurationSeconds = duration,
            Width = w,
            Height = h,
            FileSizeBytes = size,
            VideoCodec = vcodec,
            AudioCodec = acodec,
            FrameRate = fps,
            HasAudio = hasAudio
        };
    }

    private static double? ParseFraction(string? s)
    {
        if (string.IsNullOrWhiteSpace(s) || s == "0/0") return null;
        var parts = s.Split('/');
        if (parts.Length == 2 &&
            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var a) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var b) &&
            b != 0)
            return a / b;
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            return v;
        return null;
    }
}

internal static class ProcessRunner
{
    public static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        string fileName, IEnumerable<string> args, CancellationToken ct,
        Action<string>? onStderrLine = null,
        Action<string>? onStdoutLine = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var stdoutDone = new TaskCompletionSource();
        var stderrDone = new TaskCompletionSource();

        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) { stdoutDone.TrySetResult(); return; }
            stdout.AppendLine(e.Data);
            onStdoutLine?.Invoke(e.Data);
        };
        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) { stderrDone.TrySetResult(); return; }
            stderr.AppendLine(e.Data);
            onStderrLine?.Invoke(e.Data);
        };

        if (!p.Start()) throw new InvalidOperationException("Process start failed");
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        await using var reg = ct.Register(() =>
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { /* ignore */ }
        });

        await p.WaitForExitAsync(CancellationToken.None);
        await Task.WhenAll(stdoutDone.Task, stderrDone.Task);
        ct.ThrowIfCancellationRequested();
        return (p.ExitCode, stdout.ToString(), stderr.ToString());
    }

    public static string QuoteArg(string arg)
    {
        if (arg.Length == 0) return "\"\"";
        if (arg.Contains('"') || arg.Contains(' ') || arg.Contains('\t'))
            return "\"" + arg.Replace("\"", "\\\"") + "\"";
        return arg;
    }

    public static string FormatCommand(string exe, IEnumerable<string> args) =>
        string.Join(" ", new[] { QuoteArg(exe) }.Concat(args.Select(QuoteArg)));
}
