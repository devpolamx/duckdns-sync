using DuckDDNSSync.Core;

namespace DuckDDNSSync.Service;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Se relee en cada ciclo para tomar cambios guardados desde la app de escritorio
            // sin tener que reiniciar el servicio.
            var config = AppConfig.Load();

            if (config.IsValid)
            {
                var (success, message) = await DuckDnsClient.UpdateAsync(config.Domains, config.Token);
                var line = $"[{DateTime.Now:HH:mm:ss}] {(success ? "OK" : "ERROR")} - {message} (servicio)";
                AppLog.Append(line);
                logger.LogInformation("{Line}", line);
            }
            else
            {
                logger.LogInformation("Configuración incompleta (falta dominio o token), esperando...");
            }

            var interval = config.IntervalMinutes > 0 ? config.IntervalMinutes : 5;
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(interval), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
