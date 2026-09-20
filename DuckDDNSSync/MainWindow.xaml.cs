using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using DuckDDNSSync.Core;
using Wpf.Ui.Appearance;

namespace DuckDDNSSync
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly AppConfig _config;
        private readonly SyncScheduler _scheduler = new();
        private readonly TrayIconManager _tray;
        private readonly ObservableCollection<string> _domainTags = new();
        private readonly List<string> _logLines = new();
        private readonly DispatcherTimer _logPollTimer = new() { Interval = TimeSpan.FromSeconds(5) };
        private bool _isExiting;
        private bool _loadingTheme;
        private bool _loadingServiceCheckbox;

        public MainWindow() : this(false) { }

        public MainWindow(bool startMinimized)
        {
            InitializeComponent();

            AppTitleBar.MinimizeActionOverride = (_, window) => window.Hide();

            _config = AppConfig.Load();

            DomainsItemsControl.ItemsSource = _domainTags;
            DomainsItemsControl.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler(RemoveDomainTag_Click));
            foreach (var domain in SplitAndTrim(_config.Domains))
            {
                _domainTags.Add(domain);
            }

            TokenTextBox.Text = _config.Token;
            IntervalComboBox.SelectedValue = _config.IntervalMinutes.ToString();
            if (IntervalComboBox.SelectedItem == null) IntervalComboBox.SelectedIndex = 0;
            StartupCheckBox.IsChecked = _config.StartWithWindows;

            _loadingTheme = true;
            DarkThemeToggle.IsChecked = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
            _loadingTheme = false;

            _tray = new TrayIconManager(StartupManager.IsEnabled());
            _tray.OpenRequested += ShowAndActivate;
            _tray.SyncNowRequested += async () => await SyncNowAsync();
            _tray.ExitRequested += ExitApplication;
            _tray.StartupToggled += SetStartup;

            _logLines.AddRange(AppLog.ReadToday());
            RenderLog();

            // Si el servicio de Windows corre en paralelo, escribe en el mismo
            // log.txt compartido; esto refleja esas líneas aquí sin que el usuario
            // tenga que cerrar y reabrir la ventana para verlas.
            _logPollTimer.Tick += (_, _) => PollExternalLog();
            _logPollTimer.Start();

            // Antes de cada tick automático se reconfirma en vivo si el servicio de
            // Windows real ya está corriendo (aunque el estado mostrado en la UI
            // todavía no se haya actualizado), para no duplicar sincronizaciones.
            // No afecta al botón "Sincronizar ahora": ese sigue siendo manual.
            _scheduler.Tick += async () =>
            {
                if (ServiceManager.IsRunning())
                {
                    _scheduler.Stop();
                    return;
                }
                await SyncNowAsync();
            };
            if (!_config.IsValid)
            {
                AppendLog("Configura dominio y token para iniciar la sincronización.");
            }
            RefreshServiceStatus();

            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_isExiting) return;
            e.Cancel = true;
            Hide();
        }

        private void MinimizeMenuItem_Click(object sender, RoutedEventArgs e) => Hide();

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => ExitApplication();

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow { Owner = this }.ShowDialog();
        }

        private void ShowAndActivate()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            _isExiting = true;
            _logPollTimer.Stop();
            _scheduler.Stop();
            _tray.Dispose();
            Close();
            System.Windows.Application.Current.Shutdown();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var interval = int.Parse((string)IntervalComboBox.SelectedValue);
            CommitDomainInput();

            _config.Domains = string.Join(",", _domainTags);
            _config.Token = TokenTextBox.Text.Trim();
            _config.IntervalMinutes = interval;
            _config.StartWithWindows = StartupCheckBox.IsChecked == true;
            _config.Save();

            SetStartup(_config.StartWithWindows);
            AppendLog(_config.IsValid ? "Configuración guardada." : "Configuración guardada, pero falta dominio o token.");
            RefreshServiceStatus();
        }

        private void RefreshServiceStatus()
        {
            var installed = ServiceManager.IsInstalled();
            var running = installed && ServiceManager.IsRunning();

            _loadingServiceCheckbox = true;
            ServiceCheckBox.IsChecked = installed;
            _loadingServiceCheckbox = false;

            ServiceStatusText.Text = running
                ? "El servicio de Windows está activo: sincroniza aunque no haya sesión iniciada."
                : installed
                    ? "El servicio está instalado pero detenido."
                    : "Sincroniza aunque no haya una sesión de Windows iniciada (pedirá permisos de administrador).";

            // Evita sincronizar dos veces (la app y el servicio) al mismo tiempo.
            if (running)
            {
                _scheduler.Stop();
            }
            else if (_config.IsValid)
            {
                _scheduler.Start(TimeSpan.FromMinutes(_config.IntervalMinutes));
            }
        }

        private async void ServiceCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_loadingServiceCheckbox) return;

            var wantService = ServiceCheckBox.IsChecked == true;
            ServiceCheckBox.IsEnabled = false;
            ServiceStatusText.Text = wantService ? "Instalando el servicio..." : "Quitando el servicio...";

            var (success, message) = wantService
                ? await Task.Run(ServiceManager.InstallAndStart)
                : await Task.Run(ServiceManager.StopAndUninstall);

            // "sc start"/"sc delete" devuelven el control un instante antes de que el
            // servicio termine la transición (y "sc delete" en particular lo deja un
            // momento "marcado para eliminar" antes de desaparecer del todo). Se
            // espera a la condición correcta según la acción para no mostrar un
            // estado que ya quedó viejo.
            if (success)
            {
                for (var i = 0; i < 10; i++)
                {
                    var installed = ServiceManager.IsInstalled();
                    var running = installed && ServiceManager.IsRunning();
                    var reachedTarget = wantService ? running : !installed;
                    if (reachedTarget) break;
                    await Task.Delay(300);
                }
            }

            AppendLog(wantService
                ? (success ? "Servicio de Windows instalado e iniciado." : $"No se pudo instalar el servicio: {message}")
                : (success ? "Servicio de Windows detenido y quitado." : $"No se pudo quitar el servicio: {message}"));

            ServiceCheckBox.IsEnabled = true;
            RefreshServiceStatus();
        }

        private async void SyncNowButton_Click(object sender, RoutedEventArgs e) => await SyncNowAsync();

        private void DomainInputTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.OemComma)
            {
                e.Handled = true;
                CommitDomainInput();
            }
        }

        private void CommitDomainInput()
        {
            var text = DomainInputTextBox.Text;
            DomainInputTextBox.Clear();
            foreach (var domain in SplitAndTrim(text))
            {
                if (!_domainTags.Contains(domain, StringComparer.OrdinalIgnoreCase))
                    _domainTags.Add(domain);
            }
        }

        private static IEnumerable<string> SplitAndTrim(string text)
        {
            foreach (var part in text.Split(','))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0) yield return trimmed;
            }
        }

        private void RemoveDomainTag_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.Button { Tag: string domain })
                _domainTags.Remove(domain);
        }

        private void DonationLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button { Tag: string url })
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void DarkThemeToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_loadingTheme) return;

            var dark = DarkThemeToggle.IsChecked == true;
            ApplicationThemeManager.Apply(dark ? ApplicationTheme.Dark : ApplicationTheme.Light);

            _config.DarkTheme = dark;
            _config.Save();
        }

        private async Task SyncNowAsync()
        {
            if (!_config.IsValid)
            {
                AppendLog("No se puede sincronizar: falta dominio o token.");
                return;
            }

            var (success, message) = await DuckDnsClient.UpdateAsync(_config.Domains, _config.Token);
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            AppendLog($"[{timestamp}] {(success ? "OK" : "ERROR")} - {message}");

            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Última sincronización: {timestamp} - {(success ? "OK" : message)}";
                _tray.SetStatus(success ? "sincronizado" : "error");
            });
        }

        private void SetStartup(bool enabled)
        {
            if (enabled) StartupManager.Enable(); else StartupManager.Disable();
            _tray.SetStartupChecked(enabled);
            StartupCheckBox.IsChecked = enabled;
        }

        private void AppendLog(string line)
        {
            AppLog.Append(line);
            Dispatcher.Invoke(() =>
            {
                _logLines.Add(line);
                if (_logLines.Count > 200) _logLines.RemoveAt(0);
                RenderLog();
            });
        }

        private void RenderLog()
        {
            LogTextBox.Text = string.Join(Environment.NewLine, _logLines);
            LogTextBox.ScrollToEnd();
        }

        private void PollExternalLog()
        {
            var onDisk = AppLog.ReadToday();
            if (onDisk.Count == _logLines.Count) return;

            _logLines.Clear();
            _logLines.AddRange(onDisk);
            if (_logLines.Count > 200) _logLines.RemoveRange(0, _logLines.Count - 200);
            RenderLog();
        }
    }
}
