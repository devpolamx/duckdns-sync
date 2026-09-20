# DuckDDNSSync

Servicio ligero para sincronizar la IP pública con DuckDNS.

Descripción
-----------
DuckDDNSSync es una pequeña aplicación y servicio en .NET que mantiene actualizado automáticamente un registro en DuckDNS con la IP pública de la máquina donde se ejecuta. Está diseñada para ejecutarse como un worker service (.NET 10) o como una aplicación de escritorio ligera según la configuración del proyecto en esta solución.

Características
---------------
- Consulta periódica de la IP pública.
- Actualización automática del registro DuckDNS cuando cambia la IP.
- Configurable (dominio, token, intervalo).
- Ligera y fácil de desplegar en Windows o en entornos compatibles con .NET 10.

Requisitos
----------
- .NET 10 SDK (para compilar y ejecutar el worker service).
- Conexión a Internet.
- Una cuenta y un dominio en DuckDNS (https://www.duckdns.org/).

Instalación y uso
------------------
1. Clona el repositorio:

   git clone https://github.com/devpolamx/duckdns-sync.git

2. Configura los valores necesarios (token y dominio). Normalmente están en el archivo de configuración del proyecto (appsettings.json o similar). Valores típicos:

   {
	 "DuckDnsToken": "tu_token_aqui",
	 "DuckDnsDomain": "tu_subdominio",
	 "UpdateIntervalMinutes": 5
   }

3. Compila y ejecuta el proyecto con dotnet / Visual Studio.

   dotnet build
   dotnet run --project DuckDDNSSync

4. Para producción, ejecutar como servicio/worker según las instrucciones del proyecto.

Configuración
-------------
- DuckDnsToken: Token proporcionado por DuckDNS.
- DuckDnsDomain: Nombre del subdominio a actualizar (sin .duckdns.org).
- UpdateIntervalMinutes: Intervalo en minutos entre comprobaciones de IP.

Contribuciones
---------------
Las contribuciones son bienvenidas: reporta issues, abre pull requests o propone mejoras.

Licencia
--------
Proyecto con licencia abierta. Revisa el fichero LICENSE si existe.

Apoya el proyecto
-----------------
❤️ GitHub Sponsors: https://github.com/sponsors/devpolamx
☕ Invítame un café: https://www.paypal.me/poladevmx
☕ Buy Me A Coffee: https://buymeacoffee.com/cpoladiazd

Contacto
-------
Repositorio: https://github.com/devpolamx/duckdns-sync
