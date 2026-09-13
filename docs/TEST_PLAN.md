# Test Plan

## Seguimiento posterior al MVP

Fases 1–7 cerradas en el entorno reportado. Ver [BETA_VALIDATION.md](BETA_VALIDATION.md) para los criterios originales y el registro de compatibilidad futura. No se ha declarado Beta. Auditoría posterior: ZIP SHA256 sin cambios y 416 hashes/estructura de carpeta PASS; no se reejecutaron xUnit ni pruebas físicas.

## Automated

Resultado histórico Fase 6, 2026-09-07: restore/build PASS, 0 warnings/errors; 158 PASS normales (Core 122, Native 13, Integration 23), 0 FAIL, 4 SKIP. Escritorio opt-in: 3 PASS, 0 FAIL. Nuevo: preview proporcional/coordenadas extremas, guía acotada, borradores cancelados/descartados y fallo de guardado. Smoke con WPF real verifica siete pestañas, preview, diagnóstico, guía sin apertura ni activación de detección, revisión pendiente y selección de perfiles con borrador. Renders locales revisados a tamaño predeterminado; no prueban DPI físico ni foco. Detalle exacto en latest.txt. Resultados conservados de la implementación, no reejecutados para el cierre documental.

Resultado histórico Fase 5, 2026-09-06: build PASS sin advertencias/errores; suite normal 149 PASS, 0 FAIL, 4 SKIP (hardware y tres pruebas opt-in). Suite de escritorio ejecutada aparte: 3 PASS, 0 FAIL. Evidencia exacta en artifacts/test-results/latest.txt. Cierre manual de Fase 5 registrado el 2026-09-07; resultados automáticos no reejecutados para el cierre documental.

Fase 5 añade estado de detección, estabilidad/foreground, perfiles ambiguos, reinicios de proceso, exclusión de ventanas y rechazo de solapamiento. La enumeración real de ventanas necesita escritorio y ahora es opt-in junto al smoke WebView2; el sandbox no pudo enumerar (EnumWindows false sin código). La prueba automática de apertura usa juego/monitores simulados y WPF/WebView2 reales para verificar cancelación, revisión pendiente, monitor ausente/misma pantalla y preservación de browser preferences. No equivale al end-to-end con un juego real.

Fase 4: cobertura nueva de validación/serialización de perfiles, IDs requeridos/duplicados, versión futura, corrupción/cancelación, independencia del browser store, CRUD con fallo/concurrencia, aplicación de versión guardada y monitor exacto con revisión/override. Los resultados de Fase 3 siguientes son históricos; el resultado actual está en PROJECT_STATE y GPT_HANDOFF.

Resultado Fase 3, 2026-09-06: restore/build PASS; 117 PASS, 0 FAIL, 2 SKIP en la suite normal (Core 89, Native 13, Integration 15). Los omitidos son hardware físico y smoke de escritorio opt-in. Smoke WebView2 ejecutado aparte en escritorio: 1 PASS, 0 FAIL. No valida foco ni inyección táctil.

Cobertura nueva: normalización/rechazo de URLs y host virtual; JSON independiente, corrupto y cancelado; favoritos, home, toolbar, escrituras serializadas, avisos persistentes; navegación diferida e historial. Smoke real: páginas locales, atrás/adelante/recarga/inicio, favoritos, visibilidad de barra, rechazo de URL file desde el host, error de conexión loopback, recuperación y cierre temprano. Los controles se invocan programáticamente, no mediante touch real.

- Política URL HTTP/HTTPS.
- Estado foreground.
- Composición de estilos no-activate.
- `WM_MOUSEACTIVATE -> MA_NOACTIVATE`.
- Resolución y validación de selección de monitor.
- Fallback de monitor desaparecido y override del mismo monitor.
- Persistencia JSON atómica, archivo ausente y JSON inválido.
- Mapeo de `MONITORINFOEXW`, orden estable y coordenadas negativas.
- Cálculo de placement con `SWP_NOACTIVATE | SWP_NOZORDER`.
- Enumeración Win32 real del escritorio de Windows.

## Critical manual test 1

PASS exige 50 interacciones y cero cambios de foreground. Cualquier transición fuera de FocusProbe es FAIL.

Resultado 2026-09-05: **PASS (user-reported manual hardware test)**. FocusProbe mostró `Focus losses = 0` tras ejecutar el protocolo con Companion.

## Critical manual test 2

Registrar por separado tap, scroll, drag, pinch, links, buttons, JavaScript, back, forward y reload como PASS/FAIL/PARTIAL/NOT TESTED.

| Interacción | Resultado |
|---|---|
| Touch tap | PASS (user report) |
| Vertical scroll | PASS (user report) |
| Horizontal scroll | PASS (user report) |
| Drag | PASS (user report) |
| Pinch zoom | PASS (user report) |
| Hyperlinks | PASS (user report) |
| Buttons | PASS (user report) |
| JavaScript | PASS (user report) |
| Back | PASS (user report) |
| Forward | PASS (user report) |
| Reload | PASS (user report) |
| Pointer diagnostics | PASS — pointer type, ID and coordinates displayed |

## Hardware matrix

| Test | Win11 | Win10 | Mouse | Touch | Resultado |
|---|---|---|---|---|---|
| NoActivate | PASS (user report) | NOT TESTED | NOT RECORDED | PASS (user report) | Focus losses = 0 |
| Tap | PASS (user report) | NOT TESTED | NOT RECORDED | PASS (user report) | PASS |
| Scroll | PASS (user report) | NOT TESTED | NOT RECORDED | PASS (user report) | PASS |
| Drag | PASS (user report) | NOT TESTED | NOT RECORDED | PASS (user report) | PASS |
| Pinch | PASS (user report) | NOT TESTED | N/A | PASS (user report) | PASS |
| WebView2 | PASS (user report) | NOT TESTED | NOT RECORDED | PASS (user report) | PASS |
| Foreground | PASS (user report) | NOT TESTED | NOT RECORDED | PASS (user report) | Focus losses = 0 |

## Game matrix

| Juego | Display mode | Input | Touch Companion | Focus | Resultado |
|---|---|---|---|---|---|
| Por elegir | Borderless | Controller | NOT TESTED | NOT TESTED | MANUAL REQUIRED |
| Por elegir | Windowed | Controller | NOT TESTED | NOT TESTED | MANUAL REQUIRED |
| Por elegir | Fullscreen | Controller | NOT TESTED | NOT TESTED | MANUAL REQUIRED |
| Por elegir | Exclusive Fullscreen | Controller | NOT TESTED | NOT TESTED | MANUAL REQUIRED |
| Por elegir | Borderless | Keyboard/Mouse | NOT TESTED | NOT TESTED | MANUAL REQUIRED |

Fase 5, 2026-09-07: usuario confirmó un juego real con control, URL correcta y foco conservado. Nombre y modo de pantalla no informados; por ello no se atribuye ese PASS a una fila de modo específico de esta matriz.

Probar además DPI 100/125/150/200 y ubicaciones derecha/izquierda/arriba/abajo, incluyendo coordenadas negativas.

## Regression matrix

| ID | Scenario | Status |
|---|---|---|
| BUG-001 | Companion becomes foreground after tap | PASS — user report, Focus losses 0 |
| BUG-002 | WebView2 child HWND activates parent | PASS — user report, Focus losses 0 |
| BUG-003 | Companion moves to unintended monitor | PASS — user report |
| BUG-004 | DPI scaling incorrect | NOT TESTED |
| BUG-005 | Disconnect notice overwritten by event burst | PASS — automated regression + manual retest |

## Phase 2 monitor matrix

| Test | Resultado | Evidencia requerida |
|---|---|---|
| Win32 enumeration | PASS | Integration test; 3 monitors detected on development machine |
| Default selection | PASS | Automated selection tests + smoke settings file |
| JSON persistence | PASS | Automated round-trip/overwrite tests + smoke write |
| Duplicate blocked | PASS | Automated |
| Explicit same-monitor override | PASS | Automated + user report |
| DISPLAY1 fullscreen | NOT TESTED | Visual hardware check |
| DISPLAY2 fullscreen with negative/mixed coordinates | PASS (user report) | Visual hardware check completed |
| DISPLAY3 fullscreen with negative coordinates | PASS (user report) | Visual hardware check completed |
| Persist after app restart | PASS (user report) | Selection retained |
| Resolution/orientation change | PASS (user report) | Topology refreshed correctly |
| Disconnect selected Companion monitor | PASS (user report) | Companion closed; persistent review banner confirmed |
| Reconnect monitor | PASS (user report) | Selector refreshed without crash |
| Mixed DPI 100/125/150/200 | NOT TESTED | Visual size, sharpness and touch alignment |
| Focus regression after Phase 2 | PASS (user report) | Focus losses = 0 |

## Phase 3 browser manual checklist

Estado: **PASS — validación manual global reportada por el usuario, 2026-09-06. Fase 3 COMPLETE.** Confirmó «Todo funciono como esperado», que Focus losses nunca aumentó y que las rutas bloqueadas no abrieron nada. Aclaró con captura que los avisos eran mensajes integrados en Configuration, no ventanas emergentes.

Alcance de evidencia: reporte global en respuesta al protocolo, sin desglose individual de sitios/acciones ni timeline nueva. La captura acredita los avisos, no el contador de foco. Los pasos siguientes se conservan para regresiones.

1. Cierre la App anterior. En una terminal ejecute `dotnet run --project src/GameTouchCompanion.App/GameTouchCompanion.App.csproj`.
2. En Pantallas seleccione juego y monitor táctil Companion. En Navegador pulse Página de prueba local, Guardar como inicio y Añadir dirección a favoritos. Añada también `https://touch-test.local/second.html` como favorito. Abra Companion y deje la página local visible.
3. En otra terminal ejecute `dotnet run --project tools/FocusProbe/FocusProbe.csproj`. Colóquelo en el monitor de juego y déjelo foreground. Antes de contar, Focus losses debe ser 0; si hubo pérdidas durante preparación, cierre/reabra FocusProbe.
4. Sin volver a Configuration, complete al menos 50 acciones táctiles en Companion: taps, scroll, drag, pinch, enlaces entre ambas páginas, atrás/adelante, recarga, inicio, guardar/abrir favoritos y ocultar/mostrar barra. Compruebe que los controles permanecen accesibles y que el enlace de nueva ventana usa la misma vista.
5. Pulse SPACE varias veces: debe aumentar el contador de FocusProbe. Anote Focus losses y tome el resultado antes de cambiar a otra aplicación. PASS exige 0 pérdidas durante todo el intervalo y respuesta correcta a las acciones.
6. Fuera del intervalo de foco, cambie inicio/barra/favoritos, cierre y reabra App y verifique persistencia. Regrese al inicio local cuando termine.
7. En una prueba separada abra una página HTTP/HTTPS de confianza, sin credenciales, y pruebe scroll/enlaces con el mismo protocolo de foco. Para error/recovery prepare `http://127.0.0.1:1/` desde Configuration y compruebe error interno y regreso a inicio. No use login, upload ni selectores nativos aún.
8. Pruebe en la segunda página local los enlaces de esquema externo y descarga: no deben abrir otra aplicación ni un diálogo de guardado. Revalide desconexión del monitor Companion: cierre y banner de revisión en Pantallas.

| Comprobación | Estado Fase 3 |
|---|---|
| Protocolo de 50 acciones / foco / SPACE | PASS (reporte global); cero pérdidas explícitamente confirmado, sin conteo individual adjunto |
| Touch y toolbar, favoritos, ocultar/mostrar | PASS (reporte global del usuario) |
| Inicio/favoritos/barra tras reiniciar App | PASS (reporte global; store automatizado PASS) |
| Nueva ventana iniciada por usuario en misma vista | PASS (reporte global del usuario) |
| Sitio HTTP/HTTPS externo y foco | PASS (reporte global; sitio concreto no registrado) |
| Error interno, reintento/inicio con touch | PASS (reporte global; recuperación programática PASS) |
| Descarga/esquema externo sin diálogos emergentes | PASS explícito: no abrieron nada; avisos integrados confirmados con captura |
| Regresión hot-plug con navegador abierto | PASS (reporte global del usuario) |
| Archivo local no autorizado | PASS de bloqueo reportado: no abrió nada; tampoco mostró aviso |

Observación no bloqueante: la página de prueba promete avisos para todos los enlaces, pero el usuario no vio aviso para el archivo local. La no apertura quedó confirmada; la causa del bloqueo silencioso no fue instrumentada. No se modifica la política de seguridad por esta diferencia de feedback.

En futuras regresiones, reporte cada fila como PASS/FAIL/PARTIAL y el contador de foco. Si falla, adjunte `GPT_HANDOFF.md` y logs de diagnóstico; no incluya `browser.json` ni el perfil WebView2. Los `.trx` están en `artifacts/test-results`; `GPT_HANDOFF.generated.md` complementa, pero no reemplaza, el análisis manual.

## Phase 4 profiles manual checklist

Protocolo histórico de Fase 4. Al repetirlo sobre Fase 5, deja el toggle de Detección apagado para conservar la aplicación manual y el autoarranque inerte de esa prueba.

Estado: **PASS — reporte manual global del usuario, 2026-09-06. Fase 4 COMPLETE.** Respondió «funciona todo ok» al protocolo de Fase 4. Se registra su confirmación global, no una medición automática ni resultados individuales inventados. No aportó nueva captura/timeline o conteo de acciones. Los pasos se conservan como protocolo de regresión.

1. Ejecuta `dotnet run --project src/GameTouchCompanion.App/GameTouchCompanion.App.csproj` y configura las pantallas habituales.
2. En Perfiles crea «Prueba A» con URL `https://touch-test.local/index.html`, ejecutable `Game.exe`, monitor Companion existente y Autoarranque marcado. Guarda. No debe arrancar ninguna aplicación ni abrirse Companion automáticamente.
3. Crea «Prueba B» con URL `https://touch-test.local/second.html`, monitor vacío y Autoarranque desmarcado. Guarda, selecciona A, modifica/guarda su nombre; confirma que no se duplica.
4. Cierra y reabre App. Deben conservarse ambos perfiles y sus campos. Aplicar A con Companion cerrado debe preparar su URL/monitor sin abrirlo; ábrelo desde Pantallas. Aplica B con Companion abierto: debe navegar a la segunda página. Inicio y favoritos previos deben seguir intactos.
5. Cambia la URL del borrador sin guardar y pulsa Aplicar: debe usar la versión guardada. Un perfil con URL `file:///C:/Windows/win.ini` o ejecutable con ruta/argumentos debe rechazarse al guardar.
6. Guarda un perfil con monitor inexistente y aplícalo: aviso interno, sin navegar ni mover Companion a un fallback del perfil. Prueba el monitor del juego sin override: debe bloquearse. Tras desconectar el monitor Companion, verifica cierre/banner y que el perfil no pueda saltarse la revisión en Pantallas.
7. Elimina solamente un perfil de prueba: Cancelar debe conservarlo; Confirmar eliminación debe quitarlo también tras reiniciar. No uses perfiles valiosos para esta prueba.
8. Aplica un perfil válido y vuelve a dejar FocusProbe foreground en el monitor del juego. Con contador inicial 0, realiza 50 interacciones touch en Companion y verifica SPACE y cero pérdidas. No cuentes como regresión el cambio deliberado a Configuration para editar/aplicar.

Resultado manual del protocolo: PASS global reportado por el usuario; sin desglose por CRUD/persistencia, aplicación, borrador, validación, monitor/revisión, autoarranque y foco. El smoke programático cubre perfil guardado → URL → WebView2, no la edición visual ni el hot-plug de este flujo. Al cierre se repitieron build (0 advertencias/errores) y suite normal: 136 PASS, 0 FAIL, 2 SKIP. Windows 10, otras escalas DPI, juegos reales y controles nativos siguen en la matriz futura.

## Phase 5 detection manual checklist

Estado: **PASS — reporte manual global del usuario, 2026-09-07. Fase 5 cerrada.**

Fase 5: PASS manual reportado por el usuario el 2026-09-07. Indicó «me funciono todo bien», confirmó detección con un juego de su elección, apertura de la URL correspondiente y ausencia de pérdida de foco observada, incluso usando control. Después confirmó «ya lo probe y tambien me funciono» para cerrar Companion → Rearmar → activar detección → volver al juego → reapertura. No aportó nombre del juego, modo de pantalla, nueva timeline, captura del contador ni desglose de 50 acciones o casos negativos. El cierre se basa en su reporte global y esas confirmaciones explícitas, no en mediciones inferidas.

El checklist siguiente se conserva como protocolo de regresión; no representa un desglose de resultados aportado por el usuario.

1. Cierra las instancias anteriores. Ejecuta `dotnet run --project src/GameTouchCompanion.App/GameTouchCompanion.App.csproj`. Verifica que Detección está desactivada al arrancar.
2. Selecciona juego y Companion en monitores distintos. Crea/guarda un perfil «FocusProbe detección», ejecutable `FocusProbe.exe`, URL `https://touch-test.local/index.html`, monitor Companion correcto y Autoarranque marcado. Debe haber solo un perfil habilitado para ese ejecutable. No abras Companion manualmente.
3. Activa Detección. En otra terminal ejecuta `dotnet run --project tools/FocusProbe/FocusProbe.csproj`; déjalo foreground y completamente dentro del monitor del juego. Tras dos muestras estables (habitualmente 2–4 s más inicialización WebView2), Companion debe abrirse automáticamente en la otra pantalla.
4. FocusProbe debe conservar `Focus losses = 0` desde su arranque y recibir SPACE. Sin volver a Configuration, realiza 50 acciones touch en Companion. Anota el contador antes de cambiar de aplicación. El contador de Detección es muestreado y no reemplaza esta evidencia.
5. Comprueba después el panel Detección: nombre del perfil, PID, HWND y estado; cambiar deliberadamente a Configuration hace que el juego deje de ser foreground y no es por sí mismo un fallo.
6. Cierra Companion: la detección debe pausarse sin reapertura. Reactivarla sin Rearmar no debe repetir la instancia ya atendida; Rearmar y volver al juego sí permite otro intento. Al detener detección, no debe aparecer una apertura tardía.
7. Con detección activada, termina el juego: el estado detectado debe limpiarse; Companion existente debe permanecer disponible. Reinicia el juego: instancia nueva elegible tras estabilidad. Hazlo como prueba separada del intervalo de foco anterior.
8. Desmarca Autoarranque: detectar no debe navegar/abrir. Duplica un perfil habilitado del mismo ejecutable: aviso de ambigüedad, sin selección arbitraria. Corrige y Rearma. Aplica un perfil manualmente: detección pausada.
9. Prueba monitor inexistente, mismo monitor (incluso con override), ventana del juego solapada con target y revisión de desconexión: aviso interno y sin apertura automática. Reconecta/confirma/Rearma para reintentar. Mantén archivos personales intactos; usa perfiles de prueba.
10. Repite con un juego real elegido por ti y registra nombre del ejecutable, modo windowed/borderless/fullscreen, monitores, resultado y timeline. No se garantiza compatibilidad con procesos protegidos, launchers o fullscreen exclusivo.

Los casos individuales sin resultado explícito permanecen sin desglose documentado; no se asigna PASS individual ni conteo exacto a partir del reporte global. Si falla, adjunta GPT_HANDOFF.md y logs de diagnóstico (sin perfiles, URLs privadas ni cookies). No introducir SetForegroundWindow, hooks ni desactivar protecciones como respuesta automática a un fallo.

## Phase 6 UX manual checklist

Estado: **PASS — reporte manual global del usuario, 2026-09-07. Fase 6 cerrada.**

Fase 6: PASS manual global reportado por el usuario el 2026-09-07: «todo funcionando como corresponde», en respuesta a la entrega y protocolo de UX. Se registra su confirmación global, sin desglose por escenario, nueva timeline/captura del contador, conteo de acciones, juego/modo o escalas DPI. No se infieren mediciones exactas ni compatibilidad ampliada a partir de esa respuesta.

El checklist siguiente se conserva para regresiones; no representa un desglose de resultados individuales. Otras escalas DPI, Windows 10 y compatibilidad ampliada siguen pendientes.

1. Ejecuta `dotnet run --project src/GameTouchCompanion.App/GameTouchCompanion.App.csproj`. Inicio debe mostrar la guía, sin abrir Companion ni activar detección. Ve a Pantallas, revisa roles/posición/resolución en el esquema y vuelve a Inicio para avanzar. Los números del esquema no tienen por qué coincidir con Windows.
2. Completa los tres pasos y reinicia la guía. No debe abrir Companion, cambiar perfiles/URL ni activar detección. Selección inválida, una sola pantalla o revisión pendiente deben bloquear avanzar/terminar; no deben borrar el aviso. La guía no deshace lo guardado en otros paneles.
3. Crea dos perfiles de prueba. Modifica A sin guardar y selecciona B/Nuevo: debe aparecer confirmación integrada. Conservar borrador mantiene campos y selección de A. Descartar y continuar carga B o un borrador nuevo. Guardar limpia el indicador; Aplicar sigue usando el perfil guardado. No uses perfiles valiosos para pruebas de eliminación.
4. En el editor elige un monitor conectado y pulsa Usar monitor elegido: cambia solo el borrador. Usar selección actual deja vacío el campo. Una preferencia ausente debe permanecer visible hasta cambiarla explícitamente; Guardar y reiniciar verifica persistencia. Salir sin guardar descarta el borrador.
5. En Ajustes cambia Mostrar barra táctil: debe sincronizarse con Navegador y persistir al reiniciar. NoActivate y restauración de foco no ofrecen opciones inseguras. Inicio/favoritos y perfiles previos se conservan.
6. En Diagnóstico revisa versión 0.6.0-dev, runtime, monitores/revisión y muestra manual timestamp/HWND. Con detección pausada, sus últimas muestras pueden estar antiguas. Consultar/actualizar diagnóstico no activa detección ni abre Companion; Configuration sí recibe foco por interacción deliberada.
7. Redimensiona Configuration hasta su mínimo. Comprueba scroll y acceso a botones inferiores en Pantallas/Perfiles. Cambia distribución/orientación/desconecta el monitor de prueba: esquema y selector se actualizan, Companion se cierra si corresponde y banner de revisión sigue bloqueando. DPI 100/125/150/200 requiere revisión física aparte.
8. Repite regresión de foco: prepara perfil local, activa Detección y ejecuta `dotnet run --project tools/FocusProbe/FocusProbe.csproj` en el monitor del juego. Déjalo foreground, espera apertura y realiza 50 acciones touch/scroll/drag/pinch/toolbar en Companion; SPACE debe llegar al probe y Focus losses permanecer 0. No vuelvas a Configuration durante el intervalo.
9. Repite con tu juego/control y la URL correspondiente; comprueba cierre → Rearmar → activar → volver al juego → reapertura. Reporta PASS/FAIL/PARTIAL y contador/timeline si falla. Para futuras regresiones, registrar los resultados nuevos por separado del PASS global de cierre.

## Phase 7 portable manual checklist

Estado: **PASS — validación manual global del usuario, 2026-09-07. Fase 7 cerrada.**

Fase 7: PASS manual global reportado por el usuario el 2026-09-07 tras recibir el ZIP y protocolo del portable: «funciona todo bien», reiterado como «todo funciono como corresponde». Se confirma funcionamiento del portable en su entorno. No se aportó desglose por caso, nueva timeline/captura del contador, conteo de acciones ni prueba explícita en equipo sin .NET/SDK o sin WebView2. Esas configuraciones quedan como compatibilidad futura, no bloquean el cierre en el entorno reportado.

El checklist se conserva como protocolo de regresión, no como desglose de resultados individuales. Automático: restore/publish/build Release PASS; 158 tests normales PASS, 4 SKIP; 3 pruebas de escritorio Release PASS. ZIP extraído: 416 hashes y comprobaciones x64/runtime/contenido PASS. El smoke corre desde bin de tests, no desde artifacts/releases; la ejecución del portable se acredita por el reporte manual separado. Resultados automáticos conservados, no reejecutados para el cierre.

1. Cierra todas las instancias de desarrollo/Companion. Extrae el ZIP completo de artifacts/releases en una carpeta nueva fuera de bin/obj, idealmente con espacios en la ruta. No ejecutes desde el ZIP ni copies solo el EXE.
2. Ejecuta GameTouchCompanion.App.exe sin terminal, SDK ni privilegios de administrador. Inicio/Diagnóstico deben mostrar 0.7.0-dev y runtime WebView2. Detección apagada; no abrir Companion hasta elegirlo.
3. Abre la página local, navega a second.html y prueba toolbar. Comprueba que perfiles/inicio/favoritos previos se conservan y que cerrar/reabrir mantiene los guardados. Las copias de desarrollo y portable comparten LocalAppData; no las uses simultáneamente.
4. Configura pantallas distintas y repite FocusProbe: apertura manual/automática, 50 acciones touch/scroll/drag/pinch, SPACE y Focus losses = 0 durante el intervalo. Repite juego/control y cierre/rearme, desconexión/banner.
5. En un equipo/VM de prueba sin SDK ni .NET instalado, pero con WebView2 Evergreen, extrae y abre el EXE. No desinstales runtimes de tu equipo principal para esta prueba. Registrar versión de Windows y resultado; si no hay equipo, dejar NOT TESTED.
6. En entorno desechable sin WebView2, comprobar aviso claro y recuperación después de instalar Evergreen desde Microsoft. No desinstalar WebView2 del equipo habitual. Registrar NOT TESTED si no está disponible.
7. Actualización: cerrar aplicación, extraer en carpeta nueva y abrir. No mezclar DLL; conservar versión anterior. No subir ni compartir datos de LocalAppData. El hash comprueba integridad, no firma/autoría.

La publicación no cumple por sí sola el criterio Beta de cinco juegos, dos configuraciones y sesiones de 30 minutos. Fases 1–6 conservan reportes históricos, no prueban este paquete.

## Post-MVP etapa 1: detección al abrir

Automático 2026-09-07: restore/build Release PASS; 160 pruebas normales PASS, 0FAIL, 5SKIP (hardware +4 escritorio opt-in). Escritorio:4PASS. Cobertura: JSON anterior defaultfalse, roundtriptrue, conservación al refrescar/seleccionar/aplicar perfil, fallo de guardado y carga WPF que activa una sola vez sin abrir si no hay juego. No prueba foco físico.

Manual PASS global del usuario: «funciona perfecto». Checklist conservado para regresiones; no se aportó nueva timeline de foco.

1. Ejecuta la compilación actual (el ZIP antiguo no incluye la opción). En Ajustes marca Activar detección automáticamente al abrir; sesión actual no debe cambiar.
2. Cierra y reabre App: Detección marcada si configuración/runtime/perfiles/browser están disponibles. Sin juego no debe abrir Companion.
3. Inicia tu juego/perfil o FocusProbe, conserva foreground y comprueba apertura normal/foco. Cierra Companion: detección se pausa, preferencia sigue marcada. Reiniciar App vuelve a aplicarla.
4. Desmarca preferencia, reinicia: detección apagada. Cambia monitores/aplica perfil y comprueba que la preferencia guardada no se pierde.
5. La preferencia de detección es independiente del inicio Windows añadido en etapa 2. Idiomas/personalización siguen pendientes.

## Post-MVP etapa 2: inicio con Windows y bandeja

Automático 2026-09-07: build Release sin advertencias/errores; 169 normales PASS (130 Core, 15 Native, 24 Integration), 8 SKIP (hardware y 7 escritorio opt-in). Escritorio: 7 PASS. Registro probado exclusivamente mediante backend en memoria; no se modificó el inicio real del usuario. Se verifican comandos/ruta anterior, salida y cierre de sesión simulados, inicio oculto, recuperación explícita, fallo/ausencia de bandeja e icono real. No equivalen a cerrar sesión de Windows ni a medir foco físico.

Manual PASS global reportado por el usuario el 2026-09-07: «funciono todo como deberia». Etapa 2 cerrada en su entorno. Sin desglose por escenario, nueva timeline ni contador de foco; no se infieren mediciones exactas ni compatibilidad ampliada. Checklist conservado para regresiones:

1. Cierra otras copias y ejecuta la compilación actual, no el ZIP histórico. En Ajustes, activa Iniciar oculto y Cerrar Configuración a bandeja. Cierra Configuración: debe ocultarse sin detener Companion/detección. Doble clic al icono debe recuperar la misma sesión.
2. Prueba el menú: activar/pausar, Rearmar y Salir completamente. Salir debe cerrar también Companion y quitar el icono. Cerrar solo Companion sigue pausando detección sin borrar preferencias.
3. Abre de nuevo: con configuración válida debe iniciar oculto. Para recuperar aun con esa preferencia usa `GameTouchCompanion.App.exe --show`, después de salir de cualquier copia anterior. Si falta configuración válida o bandeja, debe mostrar Configuración.
4. Desde una ruta estable del EXE actual marca Iniciar con Windows. Comprueba el estado «Esta copia está registrada». Guarda tu trabajo antes de cerrar sesión y volver a entrar: debe arrancar según preferencias; la detección automática sigue dependiendo de su propio ajuste. Desmarcar inicio Windows debe quitar únicamente el registro de esta app.
5. Si cambias de carpeta, cierra la copia anterior y usa Registrar esta copia para reemplazar explícitamente la ruta. No ejecutar dos copias simultáneas. Windows puede deshabilitar/retrasar inicio; revisar Administrador de tareas. No se requiere administrador.
6. Repite detección/apertura y touch con juego/control/FocusProbe. Abrir Configuración desde bandeja es una acción deliberada que sí puede tomar foco: sepárala del intervalo de prueba NoActivate de Companion. Registra resultados, sin atribuir el PASS de etapas anteriores a esta ampliación.

Windows controla si el icono queda dentro del área de iconos ocultos. Idiomas y personalización siguen pendientes; no hay nuevo portable publicado.

## Post-MVP etapa 3: primer bloque histórico

Automático: 179 normales PASS, 9 SKIP (hardware + 8 escritorio opt-in); 8 escritorio PASS. Pruebas aisladas: lectura/guardado es/en, cancelación, idioma ausente/no admitido, JSON roto y fallo de escritura, paridad de recursos, omitir selector al tener preferencia, diálogo real y renders es/en. No se modificó language.json del usuario ni el registro Windows.

Prueba manual del primer bloque (pendiente):

1. Sal de todas las copias y ejecuta el build actual. Si aún no existe language.json, debe aparecer Idioma / Language incluso con inicio oculto/detección configurados. Cierra el diálogo sin guardar: no debe abrir Companion ni quedar icono de bandeja.
2. Reabre, elige English y guarda: las etiquetas de pestañas, botones de navegador/perfiles/Companion y menú de bandeja deben estar en inglés. Se permite aún texto dinámico sin traducir; el inventario pendiente está en LOCALIZATION.md.
3. Sal completamente y reabre: no debe volver a pedir idioma y se conservan opciones de detección/bandeja. La preferencia está separada de monitores y perfiles.
4. En Settings elige Español y Save language. La sesión actual no debe cambiar idioma ni foco por un reinicio automático. Sal completamente y reabre: etiquetas en español. Repite hacia inglés.
5. Para repetir primera elección, con App cerrada mueve solo language.json a un respaldo; no borres settings.json ni perfiles. No es necesario modificar el registro de Windows. Si un archivo de idioma está dañado, se informa fallo de inicio y se conserva; recupera el respaldo.

No cerrar etapa 3 hasta completar mensajes dinámicos y regresión correspondiente. Sin nuevo ZIP ni certificación de foco/DPI.

## Post-MVP etapa 3: actualización en vivo — protocolo vigente

Sustituye la instrucción de reiniciar y la limitación de mensajes sin traducir del primer bloque. Usuario reportó textos faltantes en Next/guía, preview, diálogos y diagnóstico; no fue una aprobación manual de idiomas. Esos puntos se migraron y se añadió aplicación inmediata según ADR-0011.

Automático: build Release PASS;181 normales PASS,10SKIP;9 escritorio PASS. Tests conservan HWND, Companion, URL, borrador/confirmación, detección y hora de diagnóstico al cambiar; verifican fallo de guardado, mensajes con argumentos y bandeja real. Renders revisados en inglés. Pruebas físicas de foco/DPI no realizadas.

Manual pendiente:

1. Ejecuta el build actual (no ZIP histórico). Con Companion abierto, en Ajustes guarda English: sin cerrar nada deben cambiar pestañas, botones, estados, barra y menú de bandeja. La URL y la detección deben conservarse.
2. Recorre los tres pasos de Home: Next y Finish guide en inglés. Cambia a Español estando en un paso intermedio: conserva el paso y cambia los textos. No abre ventanas ni activa detección desde la guía.
3. Displays: roles Game/Companion/Available, leyenda, Primary y avisos traducidos. Cambia idioma varias veces: no se pierde ninguno de los monitores seleccionados. Confirma que seleccionar otra pantalla manualmente y el permiso de pruebas siguen guardándose.
4. Profiles: deja un borrador y muestra confirmación de descarte/eliminación; cambia idioma desde Settings y vuelve. Textos traducidos, borrador/confirmación conservados. Cancela si no deseas eliminar datos.
5. Diagnostics: toma una muestra y cambia idioma sin actualizarla. Textos traducidos; misma hora/valores. Prueba bloqueo de URL/descarga y verifica mensajes integrados, sin ventanas externas desde Companion.
6. Guarda Español/English y vuelve a abrir la app para verificar persistencia; no es necesario reiniciar para aplicar. Si un guardado falla, debe conservar el idioma anterior y mostrar aviso.
7. Repite navegación touch con juego/control/FocusProbe después de regresar al juego. Configuration toma foco deliberadamente y los diálogos modales de producción quedan reservados a errores: excluye esas acciones del intervalo NoActivate. No atribuir mediciones exactas sin evidencia nueva.

No traducir nombres/URLs del usuario, páginas web ni códigos/mensajes técnicos externos. Versión/ZIP sin republicar; etapa pendiente de confirmación manual final.

## Post-MVP etapa 4: barra personalizable — primer bloque

Automático: build Release PASS;187 normales PASS,11SKIP;10 escritorio PASS. 12 combinaciones de paleta/densidad, preview sin mutación, guardar/restablecer/fallo, controles no enfocables >=44 DIP, orden y caption, HWND/URL/detección conservados. Datos aislados, sin modificar appearance.json real. La suite de pantallas/idiomas ahora recorre ocho pestañas.

Manual pendiente:

1. Abre el build actual y Personalización. Cambia paleta/tamaño/orden/etiquetas/título: solo cambia preview. Con Companion abierto, pulsa Guardar: cambia su barra sin reiniciar ni modificar página/detección.
2. Prueba las cuatro paletas y tres tamaños con touch. Cerrar y recuperar barra deben seguir accesibles aunque ocultes etiquetas/barra. El título largo debe recortarse, no empujar fuera esos controles.
3. Cambia es/en con un borrador de apariencia: etiquetas de Personalización/preview cambian; el título propio no se traduce. Guarda y reabre la app para verificar persistencia de apariencia.
4. Restablecer borrador no debe cambiar Companion hasta Guardar. Tras guardar, vuelve a la paleta/tamaño/orden predeterminados.
5. Usa juego/control/FocusProbe después de volver al juego; separa la edición en Configuration del intervalo NoActivate. Registra pérdida de foco si aparece. No inferir mediciones del smoke automático.

Siguiente bloque: temas de Configuration y rediseño general. Etapa4 en curso; idiomas no recibió nueva confirmación manual al autorizar este avance.
# Regresión de guía y temas (2026-09-08)

En Inicio comprobar los tres pasos simultáneos y accesos a Pantallas, Navegador, Perfiles y Detección; no debe aparecer Reiniciar/Siguiente/Atrás. Cambiar la selección de monitores y volver a Inicio: no debe aparecer un cuadro pasivo de resumen; el estado se conserva en logs/diagnóstico. En ambos temas revisar las ocho pestañas, desplegables abiertos, selección de listas y foco/hover de botones. AppearanceTests genera capturas con datos aislados. La prueba física de foco sigue siendo independiente.


## Regresión de legibilidad y estados pasivos (2026-09-08)

Automático requerido en Windows antes de publicar:

```powershell
dotnet restore
dotnet build -c Release -p:Platform=x64
dotnet test -c Release -p:Platform=x64
$env:GTC_RUN_WEBVIEW_TESTS = '1'
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj -c Release -p:Platform=x64 --filter 'Category=BrowserRuntime'
Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS
```

Manual:

1. Tema Claro + `Mostrar texto junto a los iconos`: en preview y Companion, Atrás/Adelante/Recargar/Guardar/Favoritos deben conservar texto blanco sobre la barra, sin heredar el texto oscuro de Configuración. Repetir en Oscuro.
2. Revisar Inicio, Pantallas, Detección, Navegador y Perfiles: no deben aparecer cuadros pasivos de estado tipo «Navegador preparado», «Perfiles listos» o el aviso amarillo normal de monitores.
3. Ajustes: guardar idioma, preferencias de detección, bandeja o inicio de Windows correctamente no debe añadir mensajes de éxito visibles. Provocar un fallo controlado en un entorno de prueba debe mostrar únicamente el panel rojo de error.
4. Pantallas/Detección/Navegador/Perfiles: los errores reales deben seguir visibles en panel rojo; los estados normales deben quedar solo en logs/Diagnóstico.
5. Activar explícitamente «mismo monitor para pruebas» y abrir Companion: no debe aparecer confirmación modal adicional; debe continuar y dejar un warning en el log.
6. Confirmar que las confirmaciones de descarte/eliminación de Perfiles siguen funcionando, pero sin una caja coloreada alrededor.
7. Repetir con controles deshabilitados en Claro/Oscuro y verificar que el texto siga legible.
