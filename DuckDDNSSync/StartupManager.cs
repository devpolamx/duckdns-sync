using System.Diagnostics;
using Microsoft.Win32;

namespace DuckDDNSSync
{
    public static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "DuckDDNSSync";

        public static bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(ValueName) is string;
        }

        public static void Enable()
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            var exePath = Process.GetCurrentProcess().MainModule!.FileName;
            key.SetValue(ValueName, $"\"{exePath}\" --minimized");
        }

        public static void Disable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            key?.DeleteValue(ValueName, false);
        }
    }
}
