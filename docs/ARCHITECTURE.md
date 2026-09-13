# Architecture

## Apariencia post-MVP (etapa 4, primer bloque)

ADR-0012: AppearanceSettings/Store en Core validan y guardan appearance.json atómicamente. Appearance observable en App carga antes de MainWindow. AppearancePanel mantiene borrador/preview separados y solo publica tras guardado exitoso. Companion mantiene objetos Button y handlers; actualiza orden, bindings/brushes/densidades sin recrear HWND/WebView2. TouchButtonResources comparte template con preview. Cierre/recuperación quedan fuera del orden configurable. Sin cambios Native de foco ni dependencias nuevas. Temas globales futuros.

## Idiomas post-MVP (etapa 3 en curso)

ADR-0010 separa LanguagePreferenceStore en Core y recursos RESX en App. App elige idioma antes de construir MainWindow; cancelación termina sin detección. ADR-0011 sustituye reinicio por actualización en vivo: LanguageState observable, bindings XAML y renderizadores/LocalizedMessage preservan identidad y argumentos. Guardar publica el cambio solo después de persistir. No se recrean ventanas/WebView2 ni se altera detección; diagnóstico conserva muestra y hora. MonitorSelectionIssue conserva opcionalmente FormattableString sin alterar códigos/validación. Eventos diferidos de ComboBox solo actualizan su selector de origen y no aceptan nulos transitorios. Detalles en LOCALIZATION.md.

## Scope

La aplicación usa dos ventanas: `MainWindow` es activable para configuración y `CompanionWindow` es touch-first y no activable. Todo User32 está aislado en Native. No existe fallback `SetForegroundWindow`.

```text
┌─────────────────────────────┐
│ WPF Application             │
│ MainWindow / CompanionWindow│
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ Core                        │
│ Monitor models / selection │
│ JSON settings / state      │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│ Native                      │
│ User32 monitor + window APIs│
└─────────────────────────────┘

Configuration / Companion → BrowserViewModel → browser.json
CompanionWindow → WebView2 → HTTP(S) / packaged TouchTestPage
```

`CompanionWindow` aplica `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` tras crear su HWND, instala un `HwndSourceHook` y devuelve `MA_NOACTIVATE` para `WM_MOUSEACTIVATE`. `WS_EX_TRANSPARENT` no se usa. `ShowWindow(SW_SHOWNOACTIVATE)` y `SetWindowPos(SWP_NOACTIVATE)` refuerzan la presentación inicial.

El watcher de 40 ms existe solo en FocusProbe para diagnóstico. Producción no realiza polling agresivo.

## Monitor management

`Win32MonitorService` transforma `MONITORINFOEXW` en `MonitorProfile` de Core. Los `DisplayRect` son siempre píxeles físicos firmados del escritorio virtual. `MonitorSelectionService` aplica defaults, fallback por monitor desaparecido y la regla de no duplicar monitores salvo override explícito.

`JsonApplicationSettingsStore` persiste únicamente nombres de dispositivo y opciones; nunca considera coordenadas guardadas como fuente de verdad. La app reenumera en cada arranque y tras cambios de topología.

`Win32WindowPlacementService` usa `SetWindowPos` con `SWP_NOACTIVATE | SWP_NOZORDER`. No asigna bounds Win32 a las propiedades DIP de WPF y no usa `WindowState=Maximized`. El manifiesto declara `PerMonitorV2,PerMonitor`.

Si desaparece el monitor Companion, MainWindow cierra la ventana no-activate y crea un `SelectionReviewMessage` persistente separado de `MonitorStatus`. Nuevas ráfagas de topología no borran el aviso y `CanOpenCompanion` permanece false hasta que el usuario confirma o modifica la selección. No se abre un diálogo modal ni se activa Configuration Mode.

## Browser Companion (Phase 3)

`BrowserViewModel` comparte navegación pendiente, página actual, historial, inicio, favoritos y estado. `BrowserSettingsPanel` permite escribir direcciones en Configuration; Companion proporciona controles táctiles no focusables y dirección de solo lectura. Conserva la última petición mientras Companion está cerrado o inicializándose.

`JsonBrowserSettingsStore` valida y persiste inicio, favoritos y barra en `%LOCALAPPDATA%\GameTouchCompanion\browser.json`, separado de la selección de monitores. Usa un temporal en el mismo directorio y reemplazo atómico. El view model serializa guardados. Un JSON corrupto no se sobrescribe y bloquea la edición persistente; si falla un guardado se indica que los cambios solo están en memoria.

`BrowserUrlPolicy` admite HTTP/HTTPS absoluto sin credenciales. El host virtual `https://touch-test.local` admite como entradas `index.html` y `second.html`, con mapeo `Deny`. Se validan navegación, redirects y frames; los frames vacíos `about:blank`/`about:srcdoc` se permiten. Los enlaces de nueva ventana iniciados por el usuario se abren en la misma vista después del callback.

Se cancelan popups automáticos, descargas, permisos, esquemas externos, autenticación HTTP y selección de certificados. Los certificados inválidos nunca se aceptan. Se deshabilitan host objects, mensajes web, diálogos JavaScript predeterminados, menús contextuales, devtools, aceleradores de navegador, autocompletado y guardado de contraseñas. Estas restricciones no prueban el foco de todos los sitios o controles nativos.

WebView2 conserva su perfil en `%LOCALAPPDATA%\GameTouchCompanion\WebView2`. Ni el perfil ni `browser.json` se exportan con diagnósticos. Los logs propios registran eventos y códigos, no direcciones, títulos o contenido. Todo acceso al navegador se realiza en el dispatcher WPF; el cierre desuscribe eventos y dispone WebView2, y las continuaciones comprueban si la ventana se cerró. Los errores de carga ofrecen reintento/inicio internos; la salida del proceso principal solicita cerrar y reabrir Companion. Native y sus APIs no-activate no cambian.

Decisión: [ADR-0003](ADR/ADR-0003-browser-companion.md). Referencias primarias: [seguridad WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security) y [modelo de hilos](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/threading-model).

## Game profiles (Phase 4)

`GameProfile` y `GameProfileDocument` son records Core; `GameProfileValidation` normaliza identidad/nombre/proceso/URL/monitor y exige las invariantes no-activate. `JsonGameProfileStore` guarda una colección schemaVersion 1 en `profiles.json`, independiente de monitor/browser settings. IDs, nombre y URL son obligatorios al deserializar; versiones desconocidas, duplicados y documentos inválidos no se reescriben al cargar. No se migra ni descarta una versión futura.

`ProfilesViewModel` separa el borrador de la colección guardada. Solo confirma creación/edición/eliminación en memoria después del guardado atómico; bloquea nuevas operaciones mientras está ocupado. `ProfilesPanel` añade edición básica en Configuration y confirmación de eliminación interna. En Fase 6 el descarte al cambiar de perfil/Nuevo requiere confirmación integrada. Apply utiliza el perfil guardado.

`MainWindow.ApplyProfileAsync` toma el gate de settings, refresca topología con el lifecycle de desconexión existente y pide a `MainWindowViewModel.ApplyProfileMonitorAsync` validar/aplicar la preferencia exacta. Un monitor ausente o revisión pendiente bloquea la aplicación; no hay fallback silencioso del perfil. La preferencia aplicada actualiza settings.json. A continuación reposiciona Companion si está abierto y solicita la URL a BrowserViewModel sin cambiar home/favoritos. Configuration se deshabilita durante la operación; Companion cerrado permanece cerrado.

En la entrega de Fase 4, AutoLaunch era solo metadato; Fase 5 lo consume bajo activación explícita de Detección. ProcessName nunca se ejecuta. No se exportan perfiles con los diagnósticos. Decisión original: [ADR-0004](ADR/ADR-0004-game-profiles.md).

## Game detection (Phase 5)

`WindowsGameDetectionSource` consulta procesos configurados (nombre/PID/StartTime) y dispone cada wrapper Process. `IProcessWindowService`/`Win32ProcessWindowService` enumeran top-level HWND/PID, visibilidad, estado minimizado, owner, tool style, bounds y foreground. No leen títulos, argumentos ni memoria. No usan MainWindowHandle como única fuente. EnumWindows falla cerrado si la sesión no permite enumerar; no eleva permisos. [EnumWindows](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows) y [GetWindowThreadProcessId](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowthreadprocessid) documentan la enumeración y asociación con PID.

`GameDetectionTracker` es una máquina de estados Core testeable sin GUI. Prefiere ventana foreground elegible; sin ella muestra el candidato más grande de forma determinista, pero no lo abre. Requiere un único perfil AutoLaunch y dos capturas foreground estables. Recuerda intentos por perfil/PID/inicio del proceso; HWND nuevo o foreground alternante no reabre una instancia atendida. Rearmar limpia ese registro. Las ventanas tool/owned/minimizadas/invisibles/vacías no son elegibles. WS_VISIBLE no garantiza que una ventana esté descubierta: por ello se exige foreground al actuar. [IsWindowVisible](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindowvisible).

`MainWindow.Detection.cs` realiza capturas fuera del dispatcher con una espera secuencial de 2 s. Stop cancela la tarea; un nuevo ciclo espera que termine el anterior. El toggle es opt-in y no persistente. Aplicar manualmente/ cerrar Companion pausa la detección; salir del juego solo limpia el estado detectado. Los errores de captura consecutivos no inundan el log.

La apertura automática usa el gate de settings, valida el perfil vigente, PID/StartTime/HWND/foreground/tamaño y topología, respeta revisión de monitores y rechaza target sobre el juego (incluso con override). Revalida después del guardado y antes de abrir, sin await ni diálogo entre la última comprobación y ShowWithoutActivation. Nunca llama al botón manual que presenta advertencias modales. Si foreground cambia durante el guardado, no abre; la selección de monitor ya guardada puede permanecer aplicada. Las carreras externas posteriores a la última comprobación no se pueden eliminar con polling: se requiere prueba física.

MainWindow admite inyección interna de servicios/almacenes para un smoke con datos aislados. Este prueba la ruta automática con juego y monitores simulados, WPF/WebView2 reales; no es evidencia de foco físico. El contador UI de transiciones es muestreado, no mide pérdidas breves. Decisión y límites: [ADR-0005](ADR/ADR-0005-game-detection.md).

## Configuration UX (Phase 6)

MainWindow añade Inicio, Ajustes y Diagnóstico sin cambiar CompanionWindow/Native. SetupGuide mantiene solo un índice de navegación (0–2); no persiste onboarding ni escribe stores. MainWindow.Ux verifica selección válida, dos pantallas distintas y ausencia de revisión antes de avanzar/terminar. La guía enlaza paneles existentes, que conservan sus propios guardados; no es una transacción ni abre Companion/activa detección.

MonitorPreviewLayout en Core convierte bounds físicos a coordenadas esquemáticas con escala uniforme y centrado, usando double antes de sumar extremos para evitar overflow. Admite coordenadas negativas, excluye rectángulos vacíos y rechaza viewports no finitos. Canvas solo dibuja; jamás usa esos valores para placement. Sus números son índices locales, roles en texto/color y leyenda accesible. Los eventos de colección/selección y tamaño repintan; sin timer nuevo.

ProfilesViewModel compara borrador con perfil guardado. Una selección pendiente conserva el perfil actual hasta ConfirmDiscard; CancelDiscard conserva campos y selección. Guardar exitoso limpia el estado pendiente; fallo conserva el borrador. ListBox WPF se prueba también, no solo el view model. El selector de monitor copia el nombre solo por botón; un monitor ausente no borra el dato guardado. Cerrar la aplicación sigue descartando borradores no guardados.

Ajustes comparte BrowserViewModel.SetToolbarVisibleAsync y sus errores persistentes; no duplica almacenes. Diagnóstico consulta foreground con Capture([]), que en producción evita enumerar procesos/ventanas, al abrir la pestaña o pulsar actualizar. Reutiliza la última muestra de detección y etiqueta su posible antigüedad; no convierte el muestreo en contador exacto. No lee títulos, URLs, cookies ni exporta datos. Touch no instrumentado y foco exacto se indican explícitamente no disponibles.

Decisión: [ADR-0006](ADR/ADR-0006-configuration-ux.md). Datos/pruebas de UI aislados en DetectionSmoke; renders de Configuration no incluyen datos personales. Fase 6 cerrada el 2026-09-07 por validación manual global del usuario; alcance y límites de evidencia en TEST_PLAN. Compatibilidad ampliada sigue pendiente.

## Portable deployment (Phase 7)

Portable.pubxml publica Release win-x64 self-contained, sin single-file, trimming ni ReadyToRun. TouchTestPage declara CopyToPublishDirectory explícito. Publish-Portable.ps1 genera carpeta única, valida runtime/archivos, añade PORTABLE.md, manifiesto SHA256 y ZIP con hash externo. Test-Portable.ps1 verifica hashes/archivos adicionales, JSON/logs privados comunes, PE AMD64 de app/runtime/loader, runtimeconfig self-contained y contenido requerido. Ningún script ejecuta el EXE ni accede a LocalAppData.

ADR-0007 conserva los datos en LocalAppData; portable no significa datos junto al EXE. WebView2 Evergreen es externo y .NET incluido debe actualizarse republicando. Paquete sin firma, no publicado en servidor y no Beta certificada. Smoke Release de integración y validación estructural del ZIP son evidencias diferentes del reporte manual del portable. Fase 7 cerrada el 2026-09-07 con confirmación global del usuario en su entorno; compatibilidad ampliada futura.

## Post-MVP: preferencia de detección al abrir

ADR-0008 sustituye la regla de sesión siempre apagada: EnableDetectionOnStartup en settings.json tiene default false; es independiente del toggle de sesión. MainWindowViewModel conserva el valor al normalizar monitores y aplicar perfiles, y solo confirma el cambio después de guardar. UI usa settingsOperationGate y muestra fallo integrado sin cambiar sesión. Settings no cargados bloquean edición.

La inicialización de MainWindow se ejecuta una sola vez; tras cargar settings, perfiles/browser y comprobar runtime/selección válida, activa el toggle si la preferencia lo pide. Si los requisitos fallan no hay reactivación tardía implícita. Pausa/cierre Companion no borra preferencia. La futura selección inicial de idioma debe preceder esta activación; plan en POST_MVP_PLAN.md.

## Post-MVP: Windows y bandeja

ADR-0009 define HKCU Run por usuario, con EXE actual entre comillas y `--startup`; solo se escribe por acción explícita. Native encapsula un único valor y permite backend de pruebas. El registro es fuente de verdad; JSON conserva StartMinimizedToTray y CloseToTray (false por defecto).

App reemplaza StartupUri por inicialización explícita antes de Show. MainWindow crea HWND oculto para recibir topología y solo permanece oculta con configuración válida e icono disponible. TrayService usa NotifyIcon de Windows Forms, pero WPF conserva el ciclo de vida y DPI del manifiesto; se suprime únicamente WFO0003. No hay nuevas dependencias NuGet. FindWindowW comprueba presencia de Shell_TrayWnd, no garantiza visibilidad del icono bajo cualquier política.

Cerrar Configuración puede ocultarla; Salir y SessionEnding liberan recursos y Companion. Recuperar desde bandeja activa Configuración deliberadamente. Detección y Companion conservan NoActivate. `--show` permite arranque visible de recuperación; no hay IPC ni protección de instancia única.
