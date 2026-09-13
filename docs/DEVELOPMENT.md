# Development

Apariencia: ADR-0012/PERSONALIZATION.md. Estado observable y archivo separado; preview local nunca publica hasta guardar. Mantener Button/handlers y HWND de Companion, Focusable/IsTabStop false y mínimo44 DIP. TouchButtonResources compartido entre preview y barra; no aplicar estilos globales de Configuration sobre Companion. Tests usan AppearanceSmoke aislado y colección no paralela para estado global; restauran apariencia al terminar.

BrowserRuntimeTests, AutomaticOpeningTests y ShellLifecycleTests también comparten la colección no paralela de UI: WPF PackagePart falló de forma intermitente cargando recursos desde varios STA concurrentes. Esta serialización es del harness, no del producto; conservar TRX del fallo y de los reintentos.

Idiomas: ver ADR-0010/0011 y LOCALIZATION.md. Recursos neutrales españoles y satélite en/Strings; no omitir la carpeta en al publicar/copiar el build. TrExtension crea bindings observables, y Ajustes aplica en vivo solo tras guardar. LocalizedMessage conserva plantillas/argumentos; no usar cadenas traducidas como identificadores de lógica ni traducir substrings de datos del usuario. Tests usan rutas aisladas y colección no paralela cuando cambian Localization.Language; restauran el idioma al terminar. Etapa 3 pendiente de validación manual final.

## Shell post-MVP

App inicia MainWindow explícitamente antes de Show. `--startup` evita activación inicial; `--show` fuerza mostrar Configuración. UseWindowsForms se limita a NotifyIcon; WPF mantiene DPI del manifiesto y se suprime únicamente WFO0003 (ADR-0009). No iniciar un loop Forms ni cambiar DPI global.

Pruebas de registro inyectan IStartupValueStore: nunca registrar inicio real desde tests. ShellLifecycleTests requiere escritorio opt-in, usa datos aislados y comprueba recuperación/salida/fallo de bandeja e icono real. El inicio de sesión Windows y foco con juego requieren validación manual separada.

Instale .NET 10 SDK y WebView2 Evergreen Runtime. Use `DOTNET_CLI_HOME` dentro del repositorio en entornos restringidos. Tras cada cambio relevante ejecute restore, build y test. Las pruebas dependientes de touch/monitores deben permanecer separadas y marcadas `MANUAL HARDWARE TEST REQUIRED`.

Antes de cambios arquitectónicos, cree un ADR con Context, Problem, Options, Decision y Consequences. Ante fallos o decisiones no triviales, actualice el handoff autocontenido.

La selección se guarda en `%LOCALAPPDATA%\GameTouchCompanion\settings.json`. Para repetir el comportamiento de primera ejecución, mueva ese archivo a una ubicación de respaldo en lugar de borrarlo. Los bounds enumerados son píxeles físicos y solo deben llegar a la ventana a través del servicio Win32; no deben asignarse directamente a propiedades WPF que usan DIPs.

## Portable Release

`powershell -NoProfile -ExecutionPolicy Bypass -File tools/Publish-Portable.ps1` publica y empaqueta sin limpiar destinos anteriores. Requiere red autorizada para restore/runtime packs y auditoría NuGet. Ante fallo conserva carpeta parcial y no genera ZIP. No modificar esas carpetas para producir otra versión: repetir script.

`tools/Test-Portable.ps1 -Folder <carpeta>` verifica el artefacto sin arrancarlo. Ver docs/PORTABLE.md y ADR-0007. El build/test Release de solución es separado del publish RID; ejecutar comandos con las opciones correctas y no atribuir resultados de bin a ejecución del portable.
