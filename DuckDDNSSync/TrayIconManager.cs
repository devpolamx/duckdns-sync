using System.Diagnostics;
using System.Windows.Forms;

namespace DuckDDNSSync
{
    public class TrayIconManager : IDisposable
    {
        private readonly NotifyIcon _icon;
        private readonly ToolStripMenuItem _startupItem;

        public event Action? OpenRequested;
        public event Action? SyncNowRequested;
        public event Action? ExitRequested;
        public event Action<bool>? StartupToggled;

        public TrayIconManager(bool startupEnabled)
        {
            var menu = new ContextMenuStrip();

            var openItem = new ToolStripMenuItem("Abrir");
            openItem.Click += (_, _) => OpenRequested?.Invoke();

            var syncItem = new ToolStripMenuItem("Sincronizar ahora");
            syncItem.Click += (_, _) => SyncNowRequested?.Invoke();

            _startupItem = new ToolStripMenuItem("Iniciar con Windows") { CheckOnClick = true, Checked = startupEnabled };
            _startupItem.Click += (_, _) => StartupToggled?.Invoke(_startupItem.Checked);

            var exitItem = new ToolStripMenuItem("Salir");
            exitItem.Click += (_, _) => ExitRequested?.Invoke();

            menu.Items.Add(openItem);
            menu.Items.Add(syncItem);
            menu.Items.Add(_startupItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            _icon = new NotifyIcon
            {
                // El .exe ya lleva ducky_icon.ico embebido (ApplicationIcon); se reutiliza
                // en vez de cargar el archivo aparte.
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule!.FileName) ?? System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "DuckDNS Sync",
                ContextMenuStrip = menu
            };
            _icon.DoubleClick += (_, _) => OpenRequested?.Invoke();
        }

        public void SetStartupChecked(bool value) => _startupItem.Checked = value;

        public void SetStatus(string text) => _icon.Text = Truncate($"DuckDNS Sync - {text}", 63);

        private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max);

        public void Dispose()
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
    }
}
