using System.Diagnostics;
using System.IO;
using CaYaVidFit.Models;

namespace CaYaVidFit.Services;

public static class FfmpegLocator
{
    public static string AppDirectory =>
        AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static string LocalFfmpegDir => Path.Combine(AppDirectory, "ffmpeg");

    public static FfmpegPaths Locate()
    {
        var pathFfmpeg = FindOnPath("ffmpeg.exe") ?? FindOnPath("ffmpeg");
        var pathFfprobe = FindOnPath("ffprobe.exe") ?? FindOnPath("ffprobe");
        if (!string.IsNullOrEmpty(pathFfmpeg) && !string.IsNullOrEmpty(pathFfprobe)
            && File.Exists(pathFfmpeg) && File.Exists(pathFfprobe))
        {
            return new FfmpegPaths
            {
                Ffmpeg = pathFfmpeg,
                Ffprobe = pathFfprobe,
                Source = "PATH"
            };
        }

        var localFfmpeg = Path.Combine(LocalFfmpegDir, "ffmpeg.exe");
        var localFfprobe = Path.Combine(LocalFfmpegDir, "ffprobe.exe");
        // Also accept nested bin/ layout from gyan essentials zip
        if (!File.Exists(localFfmpeg))
            localFfmpeg = FindUnder(LocalFfmpegDir, "ffmpeg.exe") ?? localFfmpeg;
        if (!File.Exists(localFfprobe))
            localFfprobe = FindUnder(LocalFfmpegDir, "ffprobe.exe") ?? localFfprobe;

        if (File.Exists(localFfmpeg) && File.Exists(localFfprobe))
        {
            return new FfmpegPaths
            {
                Ffmpeg = localFfmpeg,
                Ffprobe = localFfprobe,
                Source = "Local"
            };
        }

        return new FfmpegPaths
        {
            Ffmpeg = "",
            Ffprobe = "",
            Source = "Missing"
        };
    }

    public static async Task<bool> VerifyAsync(FfmpegPaths paths, CancellationToken ct = default)
    {
        if (!paths.IsAvailable) return false;
        try
        {
            var ff = await RunVersionAsync(paths.Ffmpeg, ct);
            var fp = await RunVersionAsync(paths.Ffprobe, ct);
            return ff && fp;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> RunVersionAsync(string exe, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = "-version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Process start failed");
        await p.WaitForExitAsync(ct);
        return p.ExitCode == 0;
    }

    private static string? FindOnPath(string fileName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim('"'), fileName);
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            }
            catch { /* ignore bad PATH entries */ }
        }
        return null;
    }

    private static string? FindUnder(string root, string fileName)
    {
        if (!Directory.Exists(root)) return null;
        try
        {
            return Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
