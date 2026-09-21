using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace CaYaVidFit.Services;

public sealed class FfmpegInstaller
{
    // Official gyan.dev essentials build (Windows 64-bit).
    // URL may change; we also try winget first.
    public const string GyanEssentialsZipUrl =
        "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

    // Optional known SHA256 â€” if fetch of checksum page fails we warn instead of silent fail.
    public const string GyanChecksumPageUrl =
        "https://www.gyan.dev/ffmpeg/builds/release-version";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(30)
    };

    public async Task<(bool Ok, string Message)> TryWingetAsync(
        IProgress<string>? status,
        CancellationToken ct)
    {
        status?.Report("winget ile Gyan.FFmpeg deneniyorâ€¦ / Trying winget Gyan.FFmpegâ€¦");
        var winget = FindOnPath("winget.exe") ?? FindOnPath("winget");
        if (winget is null)
            return (false, "winget bulunamadÄ± / winget not found");

        var psi = new ProcessStartInfo
        {
            FileName = winget,
            Arguments = "install --id Gyan.FFmpeg -e --accept-package-agreements --accept-source-agreements",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var p = Process.Start(psi);
        if (p is null) return (false, "winget baÅŸlatÄ±lamadÄ± / could not start winget");

        var stdoutTask = p.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (p.ExitCode == 0)
            return (true, "winget kurulumu tamam / winget install OK. PATH'i yenilemek iÃ§in uygulamayÄ± yeniden baÅŸlatÄ±n.");

        return (false, $"winget exit {p.ExitCode}\n{stdout}\n{stderr}");
    }

    public async Task<(bool Ok, string Message)> DownloadEssentialsZipAsync(
        IProgress<(double? Percent, string Text)>? progress,
        CancellationToken ct)
    {
        var destDir = FfmpegLocator.LocalFfmpegDir;
        Directory.CreateDirectory(destDir);
        var zipPath = Path.Combine(destDir, "ffmpeg-release-essentials.zip");

        progress?.Report((0, "gyan.dev essentials indiriliyorâ€¦ / Downloadingâ€¦"));

        string? expectedSha = null;
        try
        {
            expectedSha = await TryFetchKnownShaAsync(ct);
        }
        catch (Exception ex)
        {
            progress?.Report((null, $"UyarÄ±: SHA alÄ±namadÄ± ({ex.Message}). Checksum doÄŸrulanamayacak.\nWarning: could not fetch SHA â€” zip will not be checksum-verified."));
        }

        try
        {
            using var response = await Http.GetAsync(GyanEssentialsZipUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength;
            await using var remote = await response.Content.ReadAsStreamAsync(ct);
            await using var local = File.Create(zipPath);
            var buffer = new byte[81920];
            long read = 0;
            int n;
            while ((n = await remote.ReadAsync(buffer, ct)) > 0)
            {
                await local.WriteAsync(buffer.AsMemory(0, n), ct);
                read += n;
                if (total is > 0)
                {
                    var pct = 100.0 * read / total.Value;
                    progress?.Report((pct, $"Ä°ndiriliyor / Downloading {pct:0.0}% ({read / 1e6:0.0}/{total.Value / 1e6:0.0} MB)"));
                }
                else
                {
                    progress?.Report((null, $"Ä°ndiriliyor / Downloading {read / 1e6:0.0} MBâ€¦"));
                }
            }
        }
        catch (Exception ex)
        {
            return (false, $"Ä°ndirme baÅŸarÄ±sÄ±z / Download failed: {ex.Message}\nURL: {GyanEssentialsZipUrl}");
        }

        if (expectedSha is not null)
        {
            progress?.Report((100, "SHA256 doÄŸrulanÄ±yorâ€¦ / Verifying SHA256â€¦"));
            var actual = await ComputeSha256Async(zipPath, ct);
            if (!string.Equals(actual, expectedSha, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(zipPath); } catch { /* ignore */ }
                return (false,
                    $"SHA256 uyuÅŸmadÄ± â€” zip silindi (sessiz devam yok).\n" +
                    $"Checksum mismatch â€” zip deleted (no silent continue).\n" +
                    $"expected={expectedSha}\nactual={actual}");
            }
        }
        else
        {
            progress?.Report((null, "UYARI: Bilinen SHA yok; zip doÄŸrulanmadan aÃ§Ä±lacak.\nWARNING: No known SHA; extracting without checksum verification."));
        }

        progress?.Report((null, "Zip aÃ§Ä±lÄ±yorâ€¦ / Extractingâ€¦"));
        try
        {
            ExtractEssentials(zipPath, destDir);
        }
        catch (Exception ex)
        {
            return (false, $"Zip aÃ§Ä±lamadÄ± / Extract failed: {ex.Message}");
        }

        var paths = FfmpegLocator.Locate();
        if (!paths.IsAvailable || paths.Source != "Local")
            return (false, "Zip aÃ§Ä±ldÄ± ama ffmpeg.exe / ffprobe.exe bulunamadÄ±.\nExtracted but binaries missing under ./ffmpeg/");

        var ok = await FfmpegLocator.VerifyAsync(paths, ct);
        return ok
            ? (true, $"Kurulum tamam / Installed to {destDir}")
            : (false, "Binaries found but -version failed.");
    }

    private static void ExtractEssentials(string zipPath, string destDir)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        // gyan layout: ffmpeg-*-essentials_build/bin/ffmpeg.exe
        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (name.EndsWith('/')) continue;
            string? targetName = null;
            if (name.EndsWith("/bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("ffmpeg.exe", StringComparison.OrdinalIgnoreCase) && name.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
                targetName = "ffmpeg.exe";
            else if (name.EndsWith("/bin/ffprobe.exe", StringComparison.OrdinalIgnoreCase) ||
                     name.EndsWith("ffprobe.exe", StringComparison.OrdinalIgnoreCase) && name.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
                targetName = "ffprobe.exe";

            if (targetName is null) continue;
            var outPath = Path.Combine(destDir, targetName);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            entry.ExtractToFile(outPath, overwrite: true);
        }
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(fs, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Best-effort: try to read checksums from gyan checksums page if available.
    /// Returns null if unknown â€” caller must warn.
    /// </summary>
    private static async Task<string?> TryFetchKnownShaAsync(CancellationToken ct)
    {
        // gyan publishes checksums at builds page; structure varies.
        // We try a well-known checksums file pattern; null = warn user.
        try
        {
            using var resp = await Http.GetAsync(
                "https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-release-essentials.zip.sha256", ct);
            if (!resp.IsSuccessStatusCode) return null;
            var text = (await resp.Content.ReadAsStringAsync(ct)).Trim();
            // format may be "<sha>  filename" or bare sha
            var token = text.Split(' ', '\t', '\r', '\n')[0].Trim();
            return token.Length == 64 ? token : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindOnPath(string fileName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var c = Path.Combine(dir.Trim('"'), fileName);
                if (File.Exists(c)) return c;
            }
            catch { /* ignore */ }
        }
        return null;
    }
}
