# Game Touch Companion

[English](README.md) | [Español](README.es.md)

## Licencia

Game Touch Companion se distribuye bajo la Licencia Pública General de GNU, versión 3 o posterior (`GPL-3.0-or-later`). Consulta el archivo [LICENSE](LICENSE) para ver el texto completo de la licencia.

Personalización (primer bloque): pestaña con paletas, tamaños, orden, iconos/etiquetas, título y preview de la barra. Guardar aplica a Companion abierto; Restablecer cambia primero el borrador. Ver [PERSONALIZATION](docs/PERSONALIZATION.md). Temas completos de Configuration y rediseño general siguen pendientes.

Idiomas (post-MVP): la primera apertura del build actual pregunta Español/English antes de iniciar la interfaz. En **Ajustes generales**, idioma, inicio con Windows y comportamiento de bandeja se editan como preferencias y se aplican juntos con **Guardar cambios**. El cambio de idioma se aplica en vivo sin reiniciar Companion ni la detección. Incluye guía, pantallas, diálogos, diagnóstico y estados propios. Páginas web, datos del usuario y mensajes técnicos externos no se traducen. Alcance y seguimiento en [LOCALIZATION](docs/LOCALIZATION.md).

## Ampliación actual: Windows y bandeja

En **Ajustes generales** puedes preparar idioma, inicio con Windows, iniciar oculto y cerrar Configuración a bandeja, y aplicar esas preferencias con un único **Guardar cambios**. El menú del icono permite abrir Configuración, **Rearmar apertura automática** y salir completamente. La detección automática ya no tiene interruptor ni preferencia de inicio: permanece activa mientras Game Touch Companion esté ejecutándose.

Usa la compilación actual desde una ruta estable: el ZIP histórico no incluye estas ampliaciones. Si mueves el EXE, registra explícitamente la nueva copia. No ejecutes dos copias simultáneas. Recuperación: salir de la copia actual y ejecutar `GameTouchCompanion.App.exe --show`. Protocolo manual en [TEST_PLAN](docs/TEST_PLAN.md), etapa 2 post-MVP.

Aplicación experimental para Windows 10/11 x64: permite interactuar con contenido WebView2 en un monitor secundario sin que la ventana Companion se convierta en foreground. La hipótesis se implementa con `WS_EX_NOACTIVATE`, `WM_MOUSEACTIVATE -> MA_NOACTIVATE` y presentación sin activación. No inyecta código, no modifica juegos y no instala drivers.

## Requisitos

- Windows 10/11 x64
- .NET 10 SDK
- Microsoft Edge WebView2 Evergreen Runtime
- Git
- Dos monitores y touch para la validación crítica manual

## Compilar y probar

```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet-cli'
dotnet restore
dotnet build
dotnet test
```

Ejecutar las dos aplicaciones:

```powershell
dotnet run --project tools/FocusProbe/FocusProbe.csproj
dotnet run --project src/GameTouchCompanion.App/GameTouchCompanion.App.csproj
```

FocusProbe muestra PID, HWND, foreground, mensajes de activación, pérdidas de foco y un contador de SPACE. La aplicación detecta los monitores mediante Win32, permite seleccionar monitores de juego y Companion, guarda esa selección en `%LOCALAPPDATA%\GameTouchCompanion\settings.json` y abre una ventana Companion fullscreen en los bounds físicos del monitor elegido. La página `tools/TouchTestPage/index.html` se sirve mediante un host virtual seguro de WebView2.

La misma pantalla se bloquea por defecto cuando existen alternativas. Para pruebas se puede habilitar explícitamente `Allow the same monitor for testing`, que muestra una advertencia antes de abrir Companion.

## Fase 3: navegador

1. En **Pantallas**, selecciona los monitores de juego y Companion.
2. En **Navegador**, escribe una URL completa (`https://…` o `http://…`) y pulsa **Abrir Companion**. **Ir** navega si está abierto o prepara la dirección para la siguiente apertura.
3. **Guardar como inicio** y **Añadir dirección a favoritos** conservan la dirección escrita. También puedes abrir o quitar favoritos.
4. En Companion usa Atrás, Adelante, Recargar, Inicio, Guardar y Favoritos. **Guardar** añade la página actual. **Ocultar barra** deja accesibles **Mostrar barra** y **Cerrar**.
5. **Página de prueba local** carga el test táctil; su enlace a la segunda página permite probar historial y recarga sin Internet.

Inicio y favoritos se guardan en `%LOCALAPPDATA%\GameTouchCompanion\browser.json`. La preferencia de iniciar Companion con la barra táctil visible se guarda desde **Personalización**; las pantallas siguen en `settings.json`. La última página no se convierte automáticamente en inicio. Si `browser.json` está corrupto, se conserva sin sobrescribir y se bloquean los cambios persistentes hasta corregirlo y reiniciar. Los errores de página ofrecen **Reintentar** e **Ir a inicio** dentro de Companion.

Prueba opcional con WebView2 real, ejecutada en una sesión de escritorio normal (abre y cierra Companion con perfil aislado; no mide foco/touch):

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj -p:Platform=x64 --filter 'Category=BrowserRuntime'
Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS
```

Consulta la [prueba física de Fase 3](docs/TEST_PLAN.md#phase-3-browser-manual-checklist). Fases 2 y 3 cerradas: el usuario confirmó el protocolo de Fase 3, cero pérdidas de foco y avisos integrados de bloqueo. El checklist se conserva para regresiones; no garantiza compatibilidad con todos los sitios o juegos.

## Fase 4: perfiles

La pestaña **Perfiles** permite crear, editar, guardar, aplicar y eliminar perfiles con múltiples pestañas y una página principal. Se conservan en `%LOCALAPPDATA%\GameTouchCompanion\profiles.json` (schemaVersion 2), sin modificar favoritos/inicio al guardar. Los documentos schemaVersion 1 se migran automáticamente al formato multi-pestaña.

1. Pulsa **Nuevo perfil** y escribe nombre, URL HTTP/HTTPS del mapa y, si corresponde, el nombre del ejecutable (`Game.exe`, sin ruta ni argumentos).
2. Deja la pantalla Companion sin preferencia para usar la selección actual, o elige una pantalla conectada y pulsa **Usar monitor elegido**. La aplicación guarda una identidad persistente del destino de Windows en vez de depender de `\\.\DISPLAY1/2/3`; si esa pantalla se desconecta, el perfil conserva su identidad y no la sustituye silenciosamente por otra.
3. Pulsa **Guardar**. **Aplicar perfil guardado** cambia la selección actual de monitor y prepara/navega la URL. Si Companion está cerrado, ábrelo desde Pantallas o Navegador; aplicar no lo abre por sí solo.
4. **Eliminar…** solicita confirmación dentro de la pestaña antes de borrar del JSON. No cambia la página ya abierta. Seleccionar otro perfil o Nuevo con cambios pendientes ofrece **Conservar borrador** o **Descartar y continuar**. Aplicar siempre usa la versión guardada. Los borradores no se persisten al cerrar la aplicación.

**La detección automática permanece activa mientras Game Touch Companion esté ejecutándose.** No hay interruptor de sesión ni preferencia separada de “detección al iniciar”. Cada perfil decide si debe abrir Companion mediante su opción de apertura automática. La aplicación no ejecuta juegos ni configura el inicio de Windows, `NoActivate` permanece obligatorio y aplicar un perfil manualmente no detiene la detección.

Si falta el monitor o hay revisión pendiente tras una desconexión, no se aplica el perfil: revisa Pantallas o edita su preferencia. La aplicación respeta el permiso de mismo monitor para pruebas. Un JSON inválido o de versión desconocida se conserva y bloquea edición hasta corregirlo y reiniciar. Ante fallo de guardado, el borrador queda disponible y la colección guardada no cambia.

Fase 4 cerrada con confirmación manual global del usuario. El [protocolo manual](docs/TEST_PLAN.md#phase-4-profiles-manual-checklist) se conserva para regresiones. Los perfiles pueden contener URLs privadas; no adjuntes `profiles.json` a diagnósticos.

## Fase 5: detección y apertura automática

1. Guarda un perfil con el ejecutable exacto (`FocusProbe.exe` sirve para la primera prueba), una URL y Autoarranque marcado. Usa un monitor Companion distinto del juego.
2. La **Detección** se inicia automáticamente con Game Touch Companion y permanece activa mientras la aplicación siga ejecutándose, incluso si Configuración está oculta en la bandeja. No hay que activarla manualmente.
3. Abre el juego tú mismo y déjalo foreground. Tras dos muestras estables, separadas por unos 2 segundos, se aplica el perfil y se abre Companion sin activarlo. En segundo plano solo se muestra diagnóstico.
4. Más de un perfil con apertura automática para el mismo ejecutable bloquea la selección. Un monitor ausente, revisión pendiente, WebView2 no disponible o una pantalla que cubra el juego también bloquean la apertura, pero la detección continúa activa y vuelve a evaluar la situación.
5. Una instancia se identifica por ejecutable + PID + hora de inicio. Tras una apertura automática queda **atendida**. Si cierras Companion mientras juegas, esa instancia queda **descartada** y no vuelve a abrir Companion automáticamente. Si cierras y vuelves a abrir el juego, o abres otro juego configurado, se trata como una nueva instancia y puede abrir Companion normalmente. **Rearmar** permite explícitamente otro intento con la instancia actual.

El estado técnico de detección se consulta en **Diagnóstico**, junto con perfil detectado, PID, HWND, bounds y primer plano. **Rearmar apertura automática** está disponible allí y en la bandeja. El contador de transiciones se muestrea cada 2 segundos: puede omitir pérdidas breves y **no sustituye FocusProbe**. No se fuerza ni recupera el foco.

La apertura automática exige monitores separados incluso si habilitaste el override de pruebas. La detección es por nombre de ejecutable, no por ruta/firma: no prueba identidad o seguridad. Ventanas auxiliares, minimizadas, invisibles o vacías se excluyen; launchers, juegos protegidos, escritorios virtuales y fullscreen exclusivo requieren pruebas específicas. No se elevan permisos para inspeccionar procesos inaccesibles.

Fase 5 cerrada el 2026-09-07 con validación manual reportada de detección, URL correcta, foco conservado con control y rearme/reapertura. Evidencia y protocolo de regresión en el [checklist manual](docs/TEST_PLAN.md#phase-5-detection-manual-checklist). Las pruebas opt-in de escritorio del comando anterior incluyen enumeración, navegador real y apertura con juego/monitores simulados y datos aislados.

## Fase 6: configuración guiada

La pestaña **Inicio** ofrece una guía de tres pasos: pantallas, contenido y prueba. Vuelve a Inicio para avanzar después de configurar cada panel. Requiere dos monitores distintos y sin revisión pendiente; el override de una sola pantalla sigue disponible para pruebas manuales desde Pantallas, pero no completa la guía. Terminar/reiniciar la guía no abre Companion ni deshace ajustes guardados; la detección automática continúa funcionando en segundo plano.

**Pantallas** incluye un esquema proporcional del escritorio con números, roles y coordenadas, también negativas. Los números son locales al esquema, no necesariamente los identificadores de Windows. No identifica automáticamente cuál monitor es táctil ni modifica la posición real.

**Perfiles** muestra ejecutables, cambios sin guardar y confirmación al cambiar de borrador. El selector de monitores solo copia la elección al borrador al pulsar el botón; Guardar sigue siendo explícito.

**Ajustes generales** reúne idioma, inicio con Windows y comportamiento de bandeja bajo un único **Guardar cambios**. La preferencia de inicio de la barra táctil vive en **Personalización**. **Diagnóstico** muestra versión/runtime, selección/revisión, muestra manual de primer plano, últimas muestras de detección y la acción **Rearmar apertura automática**. Touch y pérdidas exactas no se inventan: usa TouchTestPage/FocusProbe.

Versión 0.6.0-dev. Fase 6 cerrada el 2026-09-07 con reporte manual global «todo funcionando como corresponde», sin desglose ni nueva timeline. Evidencia y protocolo de regresión en [validación manual Fase 6](docs/TEST_PLAN.md#phase-6-ux-manual-checklist). No incluye packaging de Fase 7.

## Fase 7: distribución portable

Publicar carpeta Release win-x64 con .NET incluido y generar ZIP/hashes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Publish-Portable.ps1
```

Cada ejecución crea una carpeta nueva en `artifacts/releases`, sin sobrescribir la anterior. El ZIP contiene los binarios, TouchTestPage, instrucciones PORTABLE.md y SHA256SUMS.txt. No contiene perfiles ni datos de LocalAppData. WebView2 Evergreen requiere instalación aparte. Sin single-file, trimming, instalador, firma o auto-update.

Extrae el ZIP completo y ejecuta `GameTouchCompanion.App.exe`, no `dotnet run`. Cierra previamente otras copias: los datos se comparten en LocalAppData. Ver [instrucciones portable](docs/PORTABLE.md). Para comprobar una carpeta extraída:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-Portable.ps1 -Folder 'C:\ruta\carpeta-extraida'
```

Fase 7 cerrada el 2026-09-07 con confirmación manual global del portable en el entorno del usuario. Las pruebas sin .NET/SDK o sin WebView2 y compatibilidad ampliada quedan para el futuro. El smoke Release es evidencia separada del reporte manual; no se declara Beta.

## Limitaciones generales

- El focus/touch real, scroll, drag y pinch requieren prueba manual con hardware después de cambios de topología o DPI.
- Una ventana no-activate limita navegación por teclado, lectores de pantalla e inputs de texto; la configuración permanece en una ventana normal.
- Fullscreen exclusivo puede comportarse distinto de windowed/borderless.
- `\\.\DISPLAY1/2/3` se consideran alias temporales de la sesión. La selección persistente usa la identidad de dispositivo/interfaz que Windows expone para cada pantalla y resuelve dinámicamente el alias GDI actual. Un cambio importante de ruta de hardware (por ejemplo otro adaptador, dock, GPU o ciertos cambios de puerto) puede hacer que Windows presente una identidad distinta y requerir reselección.

## Siguiente paso: validación Beta

Fases 1–7 completadas en el entorno reportado. El [registro Beta](docs/BETA_VALIDATION.md) organiza la compatibilidad pendiente: cinco juegos, dos configuraciones de hardware y sesiones de 30 minutos con 50+ interacciones. No exige repetir las validaciones globales ya confirmadas, pero sí registrar evidencia específica antes de etiquetar Beta.

## Diagnóstico y handoff

Companion bloquea descargas, permisos de sitios, aplicaciones externas, popups automáticos, autenticación HTTP y selección de certificados. Subida de archivos, selectores nativos, login por formularios, DRM y sitios de terceros aún no están validados y podrían afectar el foco. No introducir credenciales durante estas pruebas.

El perfil WebView2 puede conservar cookies/cache en `%LOCALAPPDATA%\GameTouchCompanion\WebView2`. Ni este perfil ni `browser.json` se incluyen en los diagnósticos.

```powershell
./tools/CollectDiagnostics.ps1
./tools/New-GptHandoff.ps1
```

Los logs diarios quedan en `%LOCALAPPDATA%\GameTouchCompanion\Logs`. No hay telemetría externa.

Si PowerShell bloquea el script local, puedes generar el complemento sin cambiar la política permanente:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/New-GptHandoff.ps1
```

Comparte `GPT_HANDOFF.md` para el análisis; `GPT_HANDOFF.generated.md` solo aporta entorno/Git/resultados automáticos.


### Navegación táctil en Companion

Companion usa pestañas compactas similares a un navegador cuyo texto sigue el título de cada sitio, permite cerrar cada pestaña y crear pestañas temporales en blanco, incorpora una barra de direcciones editable por touch y un teclado táctil integrado en la zona inferior para campos de texto web sin abrir una ventana de teclado externa. Al ocultar la barra de navegación también se oculta la barra de pestañas.
