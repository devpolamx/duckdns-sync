using System.IO;
using System.Text.Json;

namespace DuckDDNSSync.Core
{
    public class AppConfig
    {
        public string Domains { get; set; } = "";
        public string Token { get; set; } = "";
        public int IntervalMinutes { get; set; } = 5;
        public bool StartWithWindows { get; set; } = false;
        public bool? DarkTheme { get; set; } = null;

        public bool IsValid => !string.IsNullOrWhiteSpace(Domains)
            && !string.IsNullOrWhiteSpace(Token)
            && IntervalMinutes > 0;

        // %ProgramData% (no %AppData%): tanto el servicio de Windows (corre como SYSTEM,
        // sin perfil de usuario) como la app de escritorio deben leer/escribir el mismo archivo.
        private static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DuckDDNSSync", "config.json");

        public static AppConfig Load()
        {
            try
            {
                MigrateFromUserProfileIfNeeded();
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                    if (cfg != null) return cfg;
                }
            }
            catch
            {
                // config corrupta o ilegible: se usa una nueva
            }
            return new AppConfig();
        }

        public void Save()
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }

        // Migra el config.json de versiones anteriores (guardado en %AppData% del usuario)
        // la primera vez que se ejecuta con la nueva ruta compartida en %ProgramData%.
        private static void MigrateFromUserProfileIfNeeded()
        {
            if (File.Exists(ConfigPath)) return;

            var oldPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DuckDDNSSync", "config.json");
            if (!File.Exists(oldPath)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.Copy(oldPath, ConfigPath);
        }
    }
}
