# ADR-0014 — Identidad persistente de pantallas

## Context

Los nombres GDI `\\.\DISPLAY1`, `\\.\DISPLAY2`, etc. identifican una salida dentro de la topología activa de Windows, pero su numeración puede cambiar cuando se conectan, desconectan, habilitan o deshabilitan pantallas. Persistir ese alias como identidad de una pantalla podía hacer que un perfil terminara apuntando a otro monitor físico después de un cambio de topología.

## Decision

Game Touch Companion separa **identidad persistente** de **alias GDI actual**.

`Win32MonitorService` sigue usando `EnumDisplayMonitors`/`GetMonitorInfoW` para geometría y obtiene además, mediante `EnumDisplayDevicesW`, la identidad de dispositivo/interfaz que Windows expone para el monitor asociado al `\\.\DISPLAYn` actual. Esa identidad normalizada se guarda como `StableId`; el nombre de monitor expuesto por Windows se usa como `FriendlyName`.

`MonitorProfile` conserva:

- `StableId`: identidad preferida para persistencia y comparación;
- `DeviceName`: alias GDI actual, necesario para interoperabilidad/diagnóstico de la sesión;
- `FriendlyName`: nombre visible si Windows lo proporciona;
- bounds, working area y estado de primaria.

`settings.json` guarda `GameMonitorId` y `CompanionMonitorId`. Los antiguos `GameMonitorDeviceName`/`CompanionMonitorDeviceName` se mantienen como datos de compatibilidad y alias de la sesión, pero una vez existe un ID persistente **nunca** se hace fallback al antiguo `DISPLAYn` si ese ID falta: ese alias podría haber sido reasignado a otra pantalla.

Los perfiles guardan la misma identidad persistente en `CompanionMonitor`. Los perfiles legacy que todavía contienen un `DISPLAYn` se migran automáticamente cuando ese alias puede resolverse de forma inequívoca durante el arranque. Si no puede resolverse, el valor se conserva sin sustitución destructiva.

Si una pantalla persistida está desconectada, la selección deseada se conserva y la apertura de Companion queda bloqueada. La aplicación puede mostrar una selección de fallback solo como referencia de UI, pero no la persiste ni la autoriza mediante una simple confirmación. El usuario debe reconectar la pantalla original o seleccionar explícitamente otra pantalla, lo que guarda su nueva identidad.

## Consequences

Cambiar la numeración `DISPLAY1/2/3` sin cambiar la identidad de dispositivo ya no desconfigura la selección global ni los perfiles. El nombre visible de la pantalla, resolución y un tag corto derivado del ID ayudan a distinguir monitores similares sin exponer una ruta extensa en la UI.

El ID utilizado es una **identidad de dispositivo/interfaz proporcionada por Windows**, no una promesa de serial físico inmutable. Windows puede presentar una identidad diferente si cambia suficientemente la ruta de hardware (por ejemplo GPU, dock, adaptador o determinados cambios de puerto). En ese caso la aplicación conserva el ID anterior como no disponible y exige una reselección explícita en vez de adivinar.

La identidad completa puede aparecer en logs para diagnóstico local. La UI normal muestra solo nombre amigable, geometría y un tag hash corto.
