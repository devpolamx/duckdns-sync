# DuckDNS Sync

Aplicación ligera para Windows que mantiene actualizado un dominio de [DuckDNS](https://www.duckdns.org/) con la IP pública del equipo — con interfaz de escritorio y, opcionalmente, un servicio de Windows para que siga sincronizando aunque no haya una sesión iniciada.

## Descripción

DuckDNS Sync tiene dos partes:

- **Interfaz de escritorio** (WPF, con ícono en la bandeja del sistema): para configurar el dominio, el token y el intervalo, ver el log de actividad, y activar el autoarranque.
- **Servicio de Windows** (opcional, se activa desde un checkbox en la propia app): corre en segundo plano sin necesidad de una sesión de Windows iniciada — pensado para servidores.

Ambas comparten la misma configuración (`%ProgramData%\DuckDDNSSync\config.json`) y el mismo log del día (`%ProgramData%\DuckDDNSSync\log.txt`).

## Características

- Uno o varios dominios DuckDNS a la vez.
- Intervalo de sincronización configurable (5 a 60 minutos).
- Tema claro/oscuro (sigue el del sistema por defecto).
- Autoarranque con Windows (minimizado a la bandeja).
- Servicio de Windows opcional, para sincronizar sin sesión iniciada.
- Log del día, copiable, visible desde la app.

## Requisitos

Hay dos builds, según el Windows del equipo donde se instale:

| Build | Compatible con | Notas |
|---|---|---|
| **.NET 10** (recomendado) | Windows 10, 11, Windows Server 2016 en adelante | Self-contained: no requiere instalar el runtime de .NET aparte. |
| **.NET Framework 4.8** | Windows 7 SP1, 8, 8.1 en adelante | El instalador incluye el instalador offline de .NET Framework 4.8 por si el equipo no lo trae. Por ahora solo la interfaz, sin servicio de Windows. |

Ambos instaladores incluyen el Visual C++ Redistributable si hace falta. Se necesita, además, una cuenta y un dominio en [DuckDNS](https://www.duckdns.org/).

## Instalación

### Como usuario final

1. Descarga el instalador que corresponda a tu Windows (ver tabla de arriba).
2. Ejecútalo (pedirá permisos de administrador).
3. Abre **DuckDNS Sync**, ve a la pestaña **Configuración** y llena dominio(s), token e intervalo.
4. Guarda. Si quieres que sincronice sin sesión iniciada (por ejemplo en un servidor), activa **Iniciar como servicio de Windows**.

### Para desarrollo

```
git clone https://github.com/devpolamx/duckdns-sync.git
cd duckdns-sync
dotnet build DuckDDNSSync.slnx
```

Para generar los instaladores, revisa los scripts en `installer/` (requieren [Inno Setup](https://jrsoftware.org/isinfo.php)).

## Configuración

Toda la configuración se hace desde la interfaz — no hace falta editar archivos a mano:

- **Dominio(s)**: el nombre antes de `.duckdns.org` (uno o varios).
- **Token**: el token de tu cuenta DuckDNS.
- **Intervalo**: cada cuánto sincroniza (minutos).

## Contribuciones

Las contribuciones son bienvenidas: reporta issues, abre pull requests o propone mejoras.

## Licencia

Proyecto con licencia abierta. Revisa el archivo LICENSE si existe.

## Apoya el proyecto

- ☕ [Buy Me a Coffee](https://buymeacoffee.com/cpoladiazd)
- 💲 [PayPal](https://www.paypal.me/poladevmx)
- ⭐ [GitHub Sponsors](https://github.com/sponsors/devpolamx)

## Contacto

- Autor: Carlos A. Pola Díaz — [LinkedIn](https://www.linkedin.com/in/devpolamx/)
- Repositorio: https://github.com/devpolamx/duckdns-sync
