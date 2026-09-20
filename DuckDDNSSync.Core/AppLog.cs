using System.IO;

namespace DuckDDNSSync.Core
{
    public static class AppLog
    {
        // %ProgramData%: el servicio (SYSTEM) y la app de escritorio comparten el mismo log.
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DuckDDNSSync", "log.txt");

        public static List<string> ReadToday()
        {
            try
            {
                DiscardIfStale();
                if (File.Exists(LogPath))
                    return File.ReadAllLines(LogPath).ToList();
            }
            catch
            {
                // el log en disco es solo de apoyo, no debe tumbar la app
            }
            return new List<string>();
        }

        public static void Append(string line)
        {
            try
            {
                DiscardIfStale();
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch
            {
                // el log en disco es solo de apoyo, no debe tumbar la app
            }
        }

        private static void DiscardIfStale()
        {
            if (File.Exists(LogPath) && File.GetLastWriteTime(LogPath).Date != DateTime.Now.Date)
                File.Delete(LogPath);
        }
    }
}
