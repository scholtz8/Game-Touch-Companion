# Changelog

## Unreleased — Touch browser UX

### Added
- Browser-style tab captions now follow the loaded website `document.title` with a host/new-tab fallback.
- Each Companion tab has its own close button and new tabs open on a packaged blank page.
- Editable Companion address bar with touch-friendly navigation; bare domains are promoted to HTTPS.
- Built-in bottom touch keyboard for the Companion address bar and editable HTML fields.

### Changed
- Hiding the Companion toolbar now hides the tab strip as well.
- Companion tabs use a substantially more compact browser-like size.
- Profile tab configuration no longer asks the user to maintain a separate tab name; website titles are used at runtime.
- Web messages are enabled only for the token-validated touch-keyboard visibility bridge.


## Unreleased

### Added

- Companion con múltiples pestañas: cada perfil puede guardar hasta 20 páginas, definir orden y pestaña principal; el Companion conserva un WebView2 independiente por pestaña creado de forma lazy para mantener el estado al alternar.
- Cambio de perfil directamente desde Companion mediante panel táctil in-window, sin volver a Configuración. Cambiar de perfil reemplaza la sesión de pestañas actual por la definición guardada.
- Pestañas temporales desde Companion con creación/cierre durante la sesión sin modificar el perfil guardado.
- Migración automática de `profiles.json` schemaVersion 1 a schemaVersion 2 con `tabs` y `primaryTabId`, conservando la URL anterior como pestaña principal.
- Logging persistente reforzado: carpeta `Logs` junto al ejecutable cuando es escribible, fallback a `%LOCALAPPDATA%`, rotación diaria, límite de 10 MB por archivo y retención de 14 archivos. Diagnóstico añade acceso para abrir la carpeta de logs.

- Post-MVP etapa 1: preferencia persistente EnableDetectionOnStartup, false por defecto, aplicada una vez tras inicialización válida. Pausar/cerrar Companion no borra la preferencia. Plan de ampliaciones en docs/POST_MVP_PLAN.md; Windows/bandeja/idiomas/personalización siguen pendientes.

- Iconos aportados por el usuario: paquete completo conservado en assets/branding, ICO multirresolución en el ejecutable y ventana Configuration. PNG/WindowsAssets reservados para uso posterior; no se modifica el portable ya entregado.

- Fase 7: perfil Portable Release win-x64 self-contained sin single-file/trimming; scripts de publicación/ZIP/SHA256 y validación de carpeta extraída (arquitectura x64, runtime, contenido y hashes).
- Instrucciones de distribución/actualización y privacidad; versión 0.7.0-dev. Paquete sin datos personales ni instalador. Fase 7 cerrada documentalmente el 2026-09-07 por confirmación manual global del portable: «funciona todo bien», reiterada por el usuario. Compatibilidad ampliada futura; sin nueva timeline/desglose. ZIP y versión conservados; build/tests no reejecutados.

- Fase 6: guía integrada de tres pasos, preview proporcional de monitores con roles/coordenadas, pestañas Ajustes y Diagnóstico local, explicación de pausa/rearme.
- Gestor de perfiles con selector de monitores conectados y confirmación para conservar/descartar borradores al cambiar de perfil o crear uno nuevo.
- Tests de geometría de preview, navegación de guía, protección de borradores y smoke WPF de siete pestañas sin apertura/activación implícita.

- Fase 5: detección opt-in por sesión cada 2 s, metadatos de proceso y enumeración de ventanas top-level por PID; diagnóstico de HWND/foreground sin títulos ni memoria de procesos.
- Estado Core para selección estable, ambigüedad, instancias atendidas y Rearmar; apertura automática bajo revisión de monitor y comprobación fresca de foreground.
- Pruebas de detección y smoke de la ruta automática con datos aislados; cancelación, bloqueo de revisión, monitor ausente y pantalla del juego.

- Fase 4: GameProfile y documento JSON versionado, validación de identidad/proceso/URL/invariantes de foco y almacenamiento atómico en profiles.json.
- Pestaña Perfiles con borrador, guardar, aplicar versión guardada y eliminación confirmada. Fallos de guardado conservan el perfil previo.
- Aplicación manual con preferencia exacta de monitor y respeto al bloqueo de revisión; AutoLaunch se guarda sin detección ni ejecución hasta Fase 5.

- Fase 3: URL HTTP/HTTPS en Configuration y controles táctiles de historial, recarga, inicio y favoritos en Companion.
- Preferencias del navegador en `browser.json`, con validación, guardado atómico y aviso persistente de carga/guardado fallido.
- Barra ocultable con recuperación/cierre accesibles, segunda página local y errores WebView2 con reintento/inicio.
- Política de navegación, manejo de nueva ventana en la misma vista y bloqueo de esquemas externos, descargas y permisos.
- Pruebas de política, persistencia, BrowserViewModel y smoke opt-in con WebView2 real y perfil aislado.

- Bootstrap .NET 10 con proyectos App, Core, Native y tres proyectos de tests.
- FocusProbe con timeline, mensajes Win32 y contador de teclado.
- CompanionWindow no-activate con WebView2 y página táctil local.
- Logging local, scripts de diagnóstico y documentación de Fase 0/1.
- Detección Win32 de monitores mediante `EnumDisplayMonitors` y `GetMonitorInfoW`.
- Selector separado para monitor del juego y monitor Companion.
- Persistencia JSON atómica de selección en `%LOCALAPPDATA%`.
- Fullscreen por bounds físicos con soporte de coordenadas negativas y DPI Per-Monitor V2.
- Reconciliación tras `WM_DISPLAYCHANGE`/`WM_DEVICECHANGE` y reposicionamiento tras `WM_DPICHANGED`.
- Cierre seguro de Companion si desaparece su monitor seleccionado.

### Fixed

- Corregido el estado inicial del editor de perfiles tras el upgrade multi-tab: ahora siempre crea una pestaña inicial limpia al cargar, conserva un baseline explícito del borrador y distingue correctamente entre borrador limpio, cambios pendientes y fallos de guardado. Esto evita colecciones de pestañas vacías y falsos positivos de cambios sin guardar.
- Corregida la regresión del test de localización tras la revisión editorial: `LanguageStartupTests` ya no depende del antiguo prefijo `Link blocked:` / `Enlace bloqueado:` y compara el mensaje dinámico contra el recurso `Dynamic100` activo. No cambia comportamiento de producción.

### Changed

- UX de pestañas refinada: el botón `×` ahora vive dentro del mismo recuadro de cada pestaña, con apariencia más cercana a navegadores de escritorio.
- Teclado táctil aumentado por defecto y nuevo control `A+`/`A−` para alternar un tamaño ampliado durante la sesión.
- Revisión editorial completa del español visible: redacción más natural, `pantalla` como término consistente, `Autoarranque` → `apertura automática`, `Rearmar` → `restablecer apertura automática`, `foreground` → `primer plano`, y eliminación de anglicismos como `fallback`/`override` en la interfaz. Se simplificaron ayudas, estados, errores, perfiles, detección, bandeja y Personalización sin cambiar claves ni placeholders.
- English UI copy received a full editorial pass for natural Windows-style phrasing rather than literal Spanish translation. `Configuration` is now `Settings` in user-facing English, display terminology is consistent, `Rearm` is presented as `Reset auto-launch`, and Personalization/help/error copy was simplified. Resource keys and format placeholders are unchanged.
- The Spanish same-display testing label no longer says it shows a warning, matching the previously removed confirmation dialog.
- Revisión visual Claro/Oscuro: contraste de la paleta existente verificado, opacidad de controles deshabilitados aumentada y etiquetas de la barra/preview fijadas a blanco para que no hereden el texto oscuro del tema Claro.
- Estados pasivos de Inicio, Pantallas, Detección, Navegador, Perfiles y Ajustes ya no ocupan cuadros visibles; se registran en logs. Ajustes conserva un panel visible solo para errores. Confirmaciones destructivas de Perfiles mantienen la confirmación sin caja de fondo.
- El override explícito de mismo monitor ya no abre confirmación modal; se registra como warning y continúa. Los diálogos modales de producción quedan reservados a errores. `AppDialog` adopta los colores del tema de Configuración.

- Versión 0.6.0-dev. Restore/build PASS; 158 pruebas normales PASS y 3 de escritorio PASS. Fase 6 cerrada documentalmente el 2026-09-07 con reporte manual global «todo funcionando como corresponde», sin resultados individuales ni nueva timeline. Pruebas automáticas conservadas de implementación, no reejecutadas para el cierre. Sin dependencias nuevas ni cambios en Native/CompanionWindow. Fase 7 no iniciada.

- Versión 0.5.0-dev: AutoLaunch ahora funciona al activar Detección. Aplicación manual/cierre de Companion la pausan; el modo automático exige pantallas distintas aun con override. Fase 5 cerrada documentalmente el 2026-09-07: usuario confirma detección con juego real, URL correcta, foco conservado incluso con control y rearme/reapertura. Sin nueva timeline ni desglose por caso. Fases 1–5 cerradas; Fase 6 no iniciada. Código y resultados automáticos sin cambios en este cierre.

- Versión 0.4.0-dev; Fase 4 cerrada el 2026-09-06 con reporte manual global «funciona todo ok». Build limpio y 136 tests PASS al cierre (2 omitidos intencionalmente). Fases 1/2/3 permanecen cerradas. Sin nuevas dependencias.

- Versión 0.3.0-dev: 117 tests normales PASS (2 omitidos intencionalmente); smoke WebView2 de escritorio 1 PASS. Fase 3 cerrada tras PASS manual reportado por el usuario: cero pérdidas de foco, bloqueos correctos y avisos integrados confirmados con captura. Fase 2 permanece cerrada.

- Fase 1 validada manualmente: cero pérdidas de foreground y todas las interacciones de la página WebView2 respondieron correctamente.
- Versión de desarrollo avanzada a Fase 2; 36 tests automáticos PASS, incluida enumeración Win32 real y regresión del aviso persistente.

### Fixed

- BUG-005: el aviso de monitor Companion desconectado ahora vive en un banner independiente, sobrevive ráfagas posteriores de `WM_DEVICECHANGE` y bloquea la reapertura hasta confirmar la selección fallback.
- BUG-005 validado manualmente: el banner permanece visible hasta confirmar la selección.

### Known issues

- DPI mixto 100/125/150/200, Windows 10 y juegos/modos de pantalla reales aún no probados.
