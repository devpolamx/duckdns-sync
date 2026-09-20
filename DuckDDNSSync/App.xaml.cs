using System.IO;
using System.Windows;
using System.Windows.Threading;
using DuckDDNSSync.Core;
using Wpf.Ui.Appearance;

namespace DuckDDNSSync
{
    public partial class App : System.Windows.Application
    {
        private Mutex? _mutex;

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex) LogCrash(ex);
            };
            DispatcherUnhandledException += (_, args) =>
            {
                LogCrash(args.Exception);
                ShowCrash(args.Exception);
                args.Handled = true;
                Shutdown(1);
            };

            try
            {
                base.OnStartup(e);

                _mutex = new Mutex(true, "DuckDDNSSync_SingleInstance", out var createdNew);
                if (!createdNew)
                {
                    Shutdown();
                    return;
                }

                var config = AppConfig.Load();
                if (config.DarkTheme.HasValue)
                    ApplicationThemeManager.Apply(config.DarkTheme.Value ? ApplicationTheme.Dark : ApplicationTheme.Light);
                else
                    ApplicationThemeManager.ApplySystemTheme();

                var startMinimized = e.Args.Contains("--minimized");
                var window = new MainWindow(startMinimized);
                if (!startMinimized)
                {
                    window.Show();
                }
            }
            catch (Exception ex)
            {
                LogCrash(ex);
                ShowCrash(ex);
                Shutdown(1);
            }
        }

        private static void ShowCrash(Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"DuckDNS Sync no pudo iniciar:\n\n{ex.GetType().Name}: {ex.Message}\n\nEl detalle completo quedó en:\n%ProgramData%\\DuckDDNSSync\\crash.log",
                "DuckDNS Sync - Error al iniciar", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static void LogCrash(Exception ex)
        {
            try
            {
                var path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "DuckDDNSSync", "crash.log");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n\n");
            }
            catch
            {
                // si ni siquiera se puede escribir el log de crash, no hay más remedio
            }
        }
    }
}
