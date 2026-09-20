using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace DuckDDNSSync
{
    public partial class AboutWindow : Wpf.Ui.Controls.FluentWindow
    {
        public AboutWindow()
        {
            InitializeComponent();

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            VersionText.Text = version == null ? "" : $"Versión {version.ToString(3)}";

            var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            CopyrightText.Text = copyright ?? "";
        }

        private void LinkClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button { Tag: string url })
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
