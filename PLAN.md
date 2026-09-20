# Plan: DuckDDNSSync

Cliente de sincronización para Duck DNS (estilo No-IP DUC): configura dominio(s) y token, sincroniza la IP a intervalos, corre en segundo plano con icono en la bandeja del sistema, se puede activar/desactivar el autoarranque con Windows, y se puede abrir la ventana de configuración en cualquier momento.

## Decisión de arquitectura

Una única app WPF (ya existe el proyecto base en `DuckDDNSSync/`, .NET 10, WPF). Sin Windows Service separado: el mismo ejecutable corre minimizado a la bandeja y hace el trabajo de fondo. El autoarranque se logra con una entrada en `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (no requiere admin, no requiere instalador). Esto es lo mismo que hace el cliente real de No-IP.

Si en el futuro se necesita que sincronice sin sesión de usuario iniciada (antes del login), ahí sí se justifica un Windows Service real — no antes.

## Componentes

1. **Modelo de configuración** (`AppConfig`)
   - Dominio(s) Duck DNS (ej. `midominio` de `midominio.duckdns.org`)
   - Token de Duck DNS
   - Intervalo de sincronización (minutos)
   - Flag: iniciar con Windows (sí/no)
   - Guardado como JSON en `%AppData%\DuckDDNSSync\config.json`

2. **Servicio de sincronización** (`DuckDnsClient`)
   - Un método: `UpdateAsync(domains, token)` → `GET https://www.duckdns.org/update?domains={d}&token={t}&ip=`
   - Duck DNS detecta la IP pública automáticamente si `ip=` va vacío, así que no hace falta resolverla a mano
   - Se interpreta la respuesta (`OK`/`KO`) para logging y estado en la UI

3. **Bucle de fondo** (`System.Threading.Timer` o `PeriodicTimer`)
   - Dispara `UpdateAsync` cada N minutos según config
   - Corre mientras la app viva (en bandeja o con ventana abierta)

4. **Icono de bandeja** (`System.Windows.Forms.NotifyIcon` referenciado desde el proyecto WPF)
   - Doble clic → abre/restaura `MainWindow`
   - Menú contextual: Abrir, Sincronizar ahora, Iniciar con Windows (checkbox), Salir
   - Cerrar la ventana (X) minimiza a bandeja en vez de terminar el proceso; "Salir" del menú sí termina

5. **Autoarranque** (`StartupManager`)
   - Activar: escribe ruta del `.exe` (con flag `--minimized`) en `HKCU\...\Run`
   - Desactivar: borra esa entrada
   - Usa `Microsoft.Win32.Registry` (stdlib de .NET, sin dependencias nuevas)

6. **UI** (`MainWindow.xaml`, ya existe el esqueleto)
   - Campos: dominio, token, intervalo
   - Checkbox "Iniciar con Windows"
   - Botón "Guardar" / "Sincronizar ahora"
   - Estado: última sincronización, resultado (OK/error), próxima sincronización
   - Log simple en un `TextBox`/`ListBox` (últimas N líneas, en memoria — no hace falta archivo de log para esta escala)

## Flujo

```
Inicio app (normal o --minimized por autoarranque)
  → carga config.json (o crea uno por defecto)
  → si hay dominio+token válidos, arranca el timer de sync
  → si --minimized, no muestra MainWindow, solo icono de bandeja
  → doble clic en icono → muestra MainWindow
```

## Estructura de archivos (dentro de `DuckDDNSSync/`)

```
DuckDDNSSync.csproj      (agregar referencia a System.Windows.Forms para NotifyIcon)
App.xaml / App.xaml.cs   (parseo de --minimized, single-instance)
MainWindow.xaml/.cs      (UI de configuración + estado)
AppConfig.cs             (modelo + load/save JSON)
DuckDnsClient.cs         (llamada HTTP a Duck DNS)
SyncScheduler.cs         (timer que llama a DuckDnsClient)
StartupManager.cs        (alta/baja en registro Run)
TrayIconManager.cs       (NotifyIcon + menú contextual)
```

## Fuera de alcance (por ahora)

- Windows Service real / instalador MSI — no se justifica para este caso de uso
- Resolución manual de IP pública — Duck DNS ya la detecta
- Múltiples proveedores DDNS (No-IP, Cloudflare, etc.) — solo Duck DNS
- Logging a archivo persistente / rotación de logs
- Cifrado del token en disco (queda en JSON plano en `%AppData%` del usuario; es el mismo nivel de protección que usa el cliente oficial)

## Pasos de implementación

1. `AppConfig` con load/save JSON
2. `DuckDnsClient` con `HttpClient` (usar `IHttpClientFactory` no hace falta, un `HttpClient` estático basta para esta escala)
3. `SyncScheduler` con `PeriodicTimer`
4. `MainWindow`: formulario + binding a `AppConfig` + botones
5. `TrayIconManager` con `NotifyIcon`, menú contextual, minimizar a bandeja al cerrar
6. `StartupManager`: alta/baja en registro, checkbox conectado
7. Manejo de `--minimized` en `App.xaml.cs` para el arranque automático
8. Prueba manual: configurar dominio real de Duck DNS, verificar respuesta `OK` y que el registro DNS se actualice
