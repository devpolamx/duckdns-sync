using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace DuckDDNSSync
{
    public static class ServiceManager
    {
        private const string ServiceName = "DuckDDNSSync";

        public static bool IsInstalled()
        {
            try
            {
                return ServiceController.GetServices().Any(s => s.ServiceName == ServiceName);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsRunning()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                return sc.Status == ServiceControllerStatus.Running;
            }
            catch
            {
                return false;
            }
        }

        private static string? FindServiceExePath()
        {
            // El instalador pone el servicio en su propia subcarpeta para que nunca
            // comparta archivos (runtime self-contained, DuckDDNSSync.Core.dll, etc.)
            // con los de la interfaz. Como respaldo, también se busca junto al .exe
            // actual por si se está probando en local sin pasar por el instalador.
            var inServiceFolder = Path.Combine(AppContext.BaseDirectory, "Service", "DuckDDNSSync.Service.exe");
            if (File.Exists(inServiceFolder)) return inServiceFolder;

            var sameFolder = Path.Combine(AppContext.BaseDirectory, "DuckDDNSSync.Service.exe");
            return File.Exists(sameFolder) ? sameFolder : null;
        }

        public static (bool Success, string Message) InstallAndStart()
        {
            var exePath = FindServiceExePath();
            if (exePath == null)
                return (false, "No se encontró DuckDDNSSync.Service.exe junto a esta aplicación.");

            var script =
                $"sc create {ServiceName} binPath= \"{exePath}\" start= auto DisplayName= \"DuckDNS Sync\"\r\n" +
                $"sc start {ServiceName}\r\n";
            return RunElevated(script);
        }

        public static (bool Success, string Message) StopAndUninstall()
        {
            // "sc delete" deja el servicio "marcado para eliminar" un momento antes
            // de desaparecer del todo; si en ese lapso se vuelve a pedir quitarlo, ya
            // no existe y sc devuelve error 1060. No es una falla real, así que se
            // trata como éxito en vez de mostrarle ese código al usuario.
            if (!IsInstalled())
                return (true, "Ya estaba desinstalado.");

            var script =
                $"sc stop {ServiceName}\r\n" +
                $"sc delete {ServiceName}\r\n";
            return RunElevated(script);
        }

        // Se ejecuta como archivo .bat en vez de "cmd /c "comando con comillas""
        // porque el binPath y el DisplayName ya llevan comillas propias (por los
        // espacios en "Archivos de programa\IOSoluciones\..."), y cmd.exe anidando
        // comillas dentro de /c "..." las interpreta mal — terminaba corriendo
        // "sc start" contra un servicio que "sc create" nunca llegó a crear bien.
        private static (bool Success, string Message) RunElevated(string batchScript)
        {
            var batPath = Path.Combine(Path.GetTempPath(), $"duckddnssync_{Guid.NewGuid():N}.bat");
            try
            {
                File.WriteAllText(batPath, batchScript);

                var psi = new ProcessStartInfo
                {
                    FileName = batPath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
                using var process = Process.Start(psi);
                process!.WaitForExit();
                return process.ExitCode == 0
                    ? (true, "Listo.")
                    : (false, $"El comando terminó con código {process.ExitCode}.");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return (false, "Se canceló la solicitud de permisos de administrador.");
            }
            finally
            {
                try { File.Delete(batPath); } catch { }
            }
        }
    }
}
