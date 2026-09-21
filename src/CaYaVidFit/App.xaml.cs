using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace CaYaVidFit;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var lang = Environment.GetEnvironmentVariable("CAYAVIDFIT_LANG");
        if (!string.IsNullOrWhiteSpace(lang))
        {
            try
            {
                var culture = new CultureInfo(lang);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
            catch
            {
                // ignore invalid culture
            }
        }

        DispatcherUnhandledException += (_, args) =>
        {
            TryLog(args.Exception);
            MessageBox.Show(args.Exception.Message, "CaYaVidFit", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) TryLog(ex);
        };

        base.OnStartup(e);
    }

    static void TryLog(Exception ex)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "CaYaVidFit_crash.txt");
            File.WriteAllText(path, DateTime.Now + "\n" + ex);
        }
        catch
        {
            // ignore
        }
    }
}
