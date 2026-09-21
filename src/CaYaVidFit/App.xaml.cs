using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace CaYaVidFit;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
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
        catch { /* ignore */ }
    }
}
