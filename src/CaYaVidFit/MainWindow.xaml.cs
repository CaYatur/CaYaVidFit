using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CaYaVidFit.Models;
using CaYaVidFit.Services;
using Microsoft.Win32;

namespace CaYaVidFit;

public partial class MainWindow : Window
{
    private static readonly string[] VideoExtensions =
    [
        ".mp4", ".mkv", ".mov", ".avi", ".webm", ".m4v", ".wmv", ".flv", ".ts", ".mts", ".m2ts", ".mpg", ".mpeg"
    ];

    private FfmpegPaths _paths = FfmpegLocator.Locate();
    private VideoInfo? _info;
    private CancellationTokenSource? _cts;
    private EncodePlan? _plan;
    private bool _dropOverlayShown;

        public MainWindow()
    {
        InitializeComponent();
        ApplyLocalization();
        RefreshFfmpegStatus();
        Log(Loc.Ready);
    }

    private void ApplyLocalization()
    {
        Title = Loc.AppTitle;
        BtnRefreshFfmpeg.Content = Loc.Refresh;
        BtnInstallFfmpeg.Content = Loc.InstallFfmpeg;
        BtnBrowse.Content = Loc.Browse;
        if (BtnBrowseOut != null) BtnBrowseOut.Content = Loc.BrowseOut;
        TxtInput.ToolTip = Loc.DropHint;
        if (string.IsNullOrWhiteSpace(TxtInput.Text))
            TxtProbe.Text = Loc.NoVideo;
        RbHigh.Content = Loc.PresetHigh;
        RbMedium.Content = Loc.PresetMedium;
        RbLow.Content = Loc.PresetLow;
        if (RbQualityPlus != null) RbQualityPlus.Content = Loc.PresetQualityPlus;
        if (RbUltra != null) RbUltra.Content = Loc.PresetUltra;
        RbTarget.Content = Loc.TargetMb;
        ChkStreamCopy.Content = Loc.StreamCopy;
        BtnStart.Content = Loc.Start;
        BtnCancel.Content = Loc.Cancel;
        BtnCopyCmd.Content = Loc.CopyCmd;
        if (LblCommand != null) LblCommand.Text = Loc.CommandHeader;
        if (LblLog != null) LblLog.Text = Loc.LogHeader;
        if (TxtDropTitle != null) TxtDropTitle.Text = Loc.DropTitle;
        if (TxtDropHint != null) TxtDropHint.Text = Loc.DropZoneHint;
        if (LblOutput != null) LblOutput.Text = Loc.OutputLabel;
        if (LblFormat != null) LblFormat.Text = Loc.FormatLabel;
        if (TxtDropOverlayTitle != null) TxtDropOverlayTitle.Text = Loc.DropOverlayTitle;
        if (TxtDropOverlayHint != null) TxtDropOverlayHint.Text = Loc.DropOverlayHint;
        ApplyFormatComboTexts();
        if (FindName("TxtSubtitle") is System.Windows.Controls.TextBlock sub)
            sub.Text = Loc.Subtitle;
    }

    private void RefreshFfmpegStatus()
    {
        _paths = FfmpegLocator.Locate();
        if (_paths.IsAvailable)
        {
            FfmpegStatusText.Text = Loc.FfmpegOk(_paths.Source);
            FfmpegStatusText.Foreground = (Brush)FindResource("OkBrush");
            BtnInstallFfmpeg.IsEnabled = false;
        }
        else
        {
            FfmpegStatusText.Text = Loc.FfmpegMissing;
            FfmpegStatusText.Foreground = (Brush)FindResource("DangerBrush");
            BtnInstallFfmpeg.IsEnabled = true;
        }
        RebuildPlan();
    }

    private void BtnRefreshFfmpeg_Click(object sender, RoutedEventArgs e) => RefreshFfmpegStatus();

    private async void BtnInstallFfmpeg_Click(object sender, RoutedEventArgs e)
    {
        BtnInstallFfmpeg.IsEnabled = false;
        var installer = new FfmpegInstaller();
        var status = new Progress<string>(s =>
        {
            TxtStatus.Text = s;
            Log(s);
        });

        try
        {
            var (wingetOk, wingetMsg) = await installer.TryWingetAsync(status, CancellationToken.None);
            Log(wingetMsg);
            RefreshFfmpegStatus();
            if (_paths.IsAvailable)
            {
                MessageBox.Show(this, wingetMsg, "ffmpeg", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var choice = MessageBox.Show(this, Loc.WingetFailAsk, Loc.InstallTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (choice != MessageBoxResult.Yes)
            {
                MessageBox.Show(this,
                    "ffmpeg olmadan encode yapılamaz.\nCannot encode without ffmpeg.",
                    "CaYaVidFit", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlProgress = new Progress<(double? Percent, string Text)>(p =>
            {
                if (p.Percent is double pct) Progress.Value = pct;
                TxtStatus.Text = p.Text;
                Log(p.Text);
            });

            var (ok, msg) = await installer.DownloadEssentialsZipAsync(dlProgress, CancellationToken.None);
            Log(msg);
            RefreshFfmpegStatus();
            MessageBox.Show(this, msg, ok ? "OK" : "Hata / Error",
                MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            Log(Loc.InstallError(ex.Message));
            MessageBox.Show(this, ex.Message, "ffmpeg", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnInstallFfmpeg.IsEnabled = !_paths.IsAvailable;
            Progress.Value = 0;
        }
    }

    private void BtnBrowseOut_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = Loc.IsTurkish ? "Çıktı dosyası" : "Output file",
            Filter = "MP4|*.mp4|All|*.*",
            FileName = string.IsNullOrWhiteSpace(TxtOutput.Text)
                ? "output_fitted.mp4"
                : System.IO.Path.GetFileName(TxtOutput.Text)
        };
        var initDir = !string.IsNullOrWhiteSpace(TxtOutput.Text)
            ? System.IO.Path.GetDirectoryName(TxtOutput.Text)
            : (!string.IsNullOrWhiteSpace(TxtInput.Text) ? System.IO.Path.GetDirectoryName(TxtInput.Text) : null);
        if (!string.IsNullOrWhiteSpace(initDir) && System.IO.Directory.Exists(initDir))
            dlg.InitialDirectory = initDir!;
        if (dlg.ShowDialog(this) == true)
        {
            TxtOutput.Text = dlg.FileName;
            RebuildPlan();
        }
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Video seç / Select video",
            Filter = "Video|*.mp4;*.mkv;*.mov;*.avi;*.webm;*.m4v;*.wmv;*.flv;*.ts;*.mts;*.m2ts;*.mpg;*.mpeg|All|*.*"
        };
        if (dlg.ShowDialog(this) == true)
            _ = LoadInputAsync(dlg.FileName);
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            ShowDropOverlay(true);
        }
        else
        {
            e.Effects = DragDropEffects.None;
            ForceHideDropOverlay();
        }
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        ForceHideDropOverlay();
        e.Handled = true;
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;
        var path = files[0];
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (!VideoExtensions.Contains(ext))
        {
            MessageBox.Show(this, $"Desteklenmeyen uzanti / Unsupported extension: {ext}",
                "CaYaVidFit", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _ = LoadInputAsync(path);
    }

    private async Task LoadInputAsync(string path)
    {
        ForceHideDropOverlay();
        TxtInput.Text = path;
        TxtOutput.Text = PreferredOutputPath(path);
        TxtProbe.Text = "ffprobe çalışıyor… / probing…";
        TxtProbe.Foreground = (Brush)FindResource("MutedBrush");

        if (!_paths.IsAvailable)
        {
            TxtProbe.Text = "ffprobe yok — önce ffmpeg kurun.\nffprobe missing — install ffmpeg first.";
            TxtProbe.Foreground = (Brush)FindResource("DangerBrush");
            return;
        }

        try
        {
            var probe = new ProbeService(_paths);
            _info = await probe.ProbeAsync(path);
            TxtProbe.Text =
                $"Süre / Duration: {_info.DurationLabel}   |   " +
                $"Çözünürlük / Resolution: {_info.ResolutionLabel}   |   " +
                $"Boyut / Size: {_info.SizeLabel}\n" +
                $"Video: {_info.VideoCodec ?? "?"}   Audio: {(_info.HasAudio ? _info.AudioCodec ?? "?" : "yok / none")}   " +
                $"FPS: {_info.FrameRate?.ToString("0.##") ?? "?"}";
            TxtProbe.Foreground = (Brush)FindResource("FgBrush");
            Log($"Probe OK: {_info.DurationLabel}, {_info.ResolutionLabel}, {_info.SizeLabel}");
            RebuildPlan();
        }
        catch (Exception ex)
        {
            _info = null;
            TxtProbe.Text = Loc.ProbeError(ex.Message);
            TxtProbe.Foreground = (Brush)FindResource("DangerBrush");
            Log(ex.Message);
        }
    }


    private void ApplyFormatComboTexts()
    {
        if (CmbFormat is null) return;
        foreach (var item in CmbFormat.Items.OfType<ComboBoxItem>())
        {
            var tag = item.Tag?.ToString() ?? "";
            item.Content = tag switch
            {
                "same" => Loc.FormatSame,
                ".mp4" => "MP4",
                ".mkv" => "MKV",
                ".mov" => "MOV",
                ".webm" => "WEBM",
                ".avi" => "AVI",
                _ => item.Content
            };
        }
    }

    private string? SelectedFormatExt()
    {
        if (CmbFormat?.SelectedItem is ComboBoxItem ci)
        {
            var tag = ci.Tag?.ToString() ?? "same";
            if (tag == "same") return null;
            return tag;
        }
        return null;
    }

    private string PreferredOutputPath(string inputPath)
    {
        return EncodeService.ResolveOutputPath(inputPath, SelectedFormatExt());
    }

    private void CmbFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized) return;
        if (!string.IsNullOrWhiteSpace(TxtInput?.Text))
            TxtOutput.Text = PreferredOutputPath(TxtInput.Text);
        RebuildPlan();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Fit work area and true-center (avoids off-screen bottom on short displays)
        var wa = SystemParameters.WorkArea;
        MaxWidth = wa.Width;
        MaxHeight = wa.Height;
        if (Width > wa.Width - 20) Width = Math.Max(MinWidth, wa.Width - 20);
        if (Height > wa.Height - 20) Height = Math.Max(MinHeight, wa.Height - 20);
        Left = wa.Left + (wa.Width - Width) / 2;
        Top = wa.Top + (wa.Height - Height) / 2;
    }


    private void ShowDropOverlay(bool show)
    {
        if (DropOverlay is null) return;
        if (show)
        {
            if (_dropOverlayShown) return;
            _dropOverlayShown = true;

            // Clear any leftover clocks
            DropOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            if (DropOverlay.Child is Border innerShow)
                innerShow.BeginAnimation(UIElement.OpacityProperty, null);

            DropOverlay.Opacity = 0;
            DropOverlay.Visibility = Visibility.Visible;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140))
            {
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            DropOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            return;
        }

        // Hide — always force-clear, never depend on Completed alone
        if (!_dropOverlayShown && DropOverlay.Visibility != Visibility.Visible)
            return;
        _dropOverlayShown = false;

        DropOverlay.BeginAnimation(UIElement.OpacityProperty, null);
        if (DropOverlay.Child is Border innerHide)
            innerHide.BeginAnimation(UIElement.OpacityProperty, null);

        DropOverlay.Opacity = 0;
        DropOverlay.Visibility = Visibility.Collapsed;
    }

    private void ForceHideDropOverlay()
    {
        _dropOverlayShown = true; // ensure hide path runs
        ShowDropOverlay(false);
    }

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            ShowDropOverlay(true);
        }
        e.Handled = true;
    }

    private void Window_DragLeave(object sender, DragEventArgs e)
    {
        // DragLeave also fires when crossing child elements; only hide if outside window.
        var pos = e.GetPosition(this);
        const double pad = 2;
        if (pos.X < pad || pos.Y < pad || pos.X > ActualWidth - pad || pos.Y > ActualHeight - pad)
            ForceHideDropOverlay();
        e.Handled = true;
    }

    private void Mode_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized) return;
        if (TxtTargetMb is not null && RbTarget is not null)
            TxtTargetMb.IsEnabled = RbTarget.IsChecked == true;
        RebuildPlan();
    }
    private void TxtTargetMb_TextChanged(object sender, TextChangedEventArgs e) { if (IsInitialized) RebuildPlan(); }

    private EncodeModeKind CurrentMode()
    {
        if (RbHigh.IsChecked == true) return EncodeModeKind.PresetHigh;
        if (RbLow.IsChecked == true) return EncodeModeKind.PresetLow;
        if (RbQualityPlus?.IsChecked == true) return EncodeModeKind.PresetQualityPlus;
        if (RbUltra?.IsChecked == true) return EncodeModeKind.PresetUltra;
        if (RbTarget.IsChecked == true) return EncodeModeKind.TargetMb;
        return EncodeModeKind.PresetMedium;
    }

    private void RebuildPlan()
    {
        // XAML IsChecked fires Mode_Changed before all controls exist — guard hard.
        if (!IsInitialized || TxtInput is null || TxtCommand is null || TxtStatus is null || TxtOutput is null || ChkStreamCopy is null)
            return;
        if (RbHigh is null || RbLow is null || RbTarget is null || TxtTargetMb is null || RbMedium is null)
            return;

        if (_info is null || string.IsNullOrWhiteSpace(TxtInput.Text) || !_paths.IsAvailable)
        {
            TxtCommand.Text = "";
            _plan = null;
            return;
        }

        try
        {
            double? target = null;
            if (CurrentMode() == EncodeModeKind.TargetMb)
            {
                if (!double.TryParse(TxtTargetMb.Text.Replace(',', '.'),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var t) || t <= 0)
                {
                    TxtCommand.Text = Loc.InvalidTarget;
                    _plan = null;
                    return;
                }
                target = t;
            }

            var outPath = string.IsNullOrWhiteSpace(TxtOutput.Text)
                ? PreferredOutputPath(TxtInput.Text)
                : TxtOutput.Text;

            var req = new EncodeRequest
            {
                InputPath = TxtInput.Text,
                OutputPath = outPath,
                Mode = CurrentMode(),
                TargetMb = target,
                StreamCopyIfFits = ChkStreamCopy.IsChecked == true
            };

            var enc = new EncodeService(_paths);
            _plan = enc.BuildPlan(_info, req);
            TxtCommand.Text = _plan.DisplayCommand;
            if (_plan.Warning is not null)
                TxtStatus.Text = _plan.Warning.Split('\n')[0];
            else if (_plan.EstimatedVideoBitrateKbps is double vb)
                TxtStatus.Text = $"Plan: video ≈ {vb:0} kbps, audio ≈ {_plan.EstimatedAudioBitrateKbps:0} kbps";
            else
                TxtStatus.Text = BitrateCalculator.PresetLabel(CurrentMode());
        }
        catch (Exception ex)
        {
            _plan = null;
            TxtCommand.Text = "";
            TxtStatus.Text = ex.Message.Split('\n')[0];
            Log(ex.Message);
        }
    }

    private void BtnCopyCmd_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCommand.Text)) return;
        try
        {
            Clipboard.SetText(TxtCommand.Text);
            TxtStatus.Text = "Komut panoya kopyalandı / Copied to clipboard";
        }
        catch (Exception ex)
        {
            Log("Clipboard: " + ex.Message);
        }
    }

    private async void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (_info is null || _plan is null)
        {
            MessageBox.Show(this, "Önce geçerli bir video ve mod seçin.\nSelect a video and valid mode first.",
                "CaYaVidFit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!_paths.IsAvailable)
        {
            MessageBox.Show(this, "ffmpeg eksik / missing", "CaYaVidFit",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Rebuild in case output path edited
        RebuildPlan();
        if (_plan is null) return;

        if (_plan.Warning is not null && _plan.Arguments.Length == 0 && !_plan.IsStreamCopy)
        {
            MessageBox.Show(this, _plan.Warning, "CaYaVidFit", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_plan.IsStreamCopy && _plan.Warning is not null)
        {
            var r = MessageBox.Show(this, _plan.Warning + "\n\nDevam / Continue?", "Stream copy",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;
        }

        double? target = null;
        if (CurrentMode() == EncodeModeKind.TargetMb &&
            double.TryParse(TxtTargetMb.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var t))
            target = t;

        var req = new EncodeRequest
        {
            InputPath = TxtInput.Text,
            OutputPath = string.IsNullOrWhiteSpace(TxtOutput.Text)
                ? PreferredOutputPath(TxtInput.Text)
                : TxtOutput.Text,
            Mode = CurrentMode(),
            TargetMb = target,
            StreamCopyIfFits = ChkStreamCopy.IsChecked == true
        };

        _cts = new CancellationTokenSource();
        SetBusy(true);
        Progress.Value = 0;
        Log("Encode başlıyor…\n" + _plan.DisplayCommand);

        var progress = new Progress<EncodeProgressEventArgs>(p =>
        {
            if (p.Percent is double pct) Progress.Value = pct;
            if (!string.IsNullOrEmpty(p.StatusText)) TxtStatus.Text = p.StatusText!;
        });

        try
        {
            var enc = new EncodeService(_paths);
            // rebuild plan with final req
            _plan = enc.BuildPlan(_info, req);
            await enc.EncodeAsync(_info, req, _plan, progress, _cts.Token);

            var outSize = File.Exists(req.OutputPath) ? new FileInfo(req.OutputPath).Length / (1024.0 * 1024.0) : 0;
            var msg = $"Bitti / Done → {req.OutputPath}\nÇıktı boyutu / Output size: {outSize:0.##} MB";
            if (target is double tm && outSize > tm + 0.15)
                msg += $"\nUyarı: hedef {tm:0.##} MB aşıldı (mux/overhead). / Warning: slightly over target.";
            Log(msg);
            TxtStatus.Text = msg.Split('\n')[0];
            MessageBox.Show(this, msg, "CaYaVidFit", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            TxtStatus.Text = "İptal edildi / Cancelled";
            Log("Cancelled");
        }
        catch (Exception ex)
        {
            TxtStatus.Text = "Hata / Error: " + ex.Message.Split('\n')[0];
            Log(ex.ToString());
            MessageBox.Show(this, ex.Message, "Encode hatası", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        TxtStatus.Text = "İptal isteniyor… / Cancelling…";
    }

    private void SetBusy(bool busy)
    {
        BtnStart.IsEnabled = !busy;
        BtnCancel.IsEnabled = busy;
        BtnBrowse.IsEnabled = !busy;
        if (BtnBrowseOut != null) BtnBrowseOut.IsEnabled = !busy;
        RbHigh.IsEnabled = !busy;
        RbMedium.IsEnabled = !busy;
        RbLow.IsEnabled = !busy;
        if (RbQualityPlus != null) RbQualityPlus.IsEnabled = !busy;
        if (RbUltra != null) RbUltra.IsEnabled = !busy;
        if (CmbFormat != null) CmbFormat.IsEnabled = !busy;
        RbTarget.IsEnabled = !busy;
        TxtTargetMb.IsEnabled = !busy && RbTarget.IsChecked == true;
    }

    private void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        TxtLog.Text = string.IsNullOrEmpty(TxtLog.Text) ? line : TxtLog.Text + "\n" + line;
    }

    private void DropZone_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Controls.Button) return;
        if (e.OriginalSource is System.Windows.Controls.TextBox) return;
        BtnBrowse_Click(sender, e);
    }
}
