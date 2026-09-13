# Troubleshooting

## Personalización de barra

- Cambiar controles solo cambia preview; Guardar aplica. Restablecer borrador también requiere Guardar.
- Si aparece error leyendo appearance.json, se usan defaults y el archivo se conserva. Respáldalo antes de corregirlo; Guardar desde Personalización reemplaza explícitamente su contenido.
- Título máximo40 caracteres sin controles, presets cerrados. Fallo de guardado no altera apariencia activa. Cerrar/recuperar barra no se pueden quitar.
- Los colores no cambian páginas web ni todavía el tema completo de Configuration. No confundir este primer bloque con cierre de etapa4.

## Idioma

- Guardar idioma actualiza la interfaz inmediatamente. Si falla el guardado, conserva el idioma anterior y muestra aviso. No necesita reinicio ni cerrar a bandeja.
- Instalaciones anteriores preguntan una vez por usuario de Windows. language.json es independiente de settings.json/perfiles; no borres estos últimos para repetir el selector.
- Si language.json es ilegible o está dañado, el arranque falla y no lo sobrescribe. Con App cerrada, conserva un respaldo antes de corregirlo o moverlo para elegir otra vez.
- Guía/Next, roles de pantallas, diagnóstico, diálogos y estados propios se traducen. Contenido web, nombres/URLs del usuario, códigos y mensajes externos del sistema no se modifican. Si un texto propio sigue en el otro idioma, registra panel, acción y captura para corregir la clave.

## Inicio Windows y bandeja (post-MVP)

- App oculta: doble clic en su icono o menú Abrir Configuración. Para salir usa Salir completamente, no cerrar a bandeja. Si necesitas recuperación, sal de cualquier copia y ejecuta `GameTouchCompanion.App.exe --show`.
- Inicio Windows no funciona: comprobar estado en Ajustes y Administrador de tareas. El EXE debe permanecer en la ruta registrada. Tras moverlo, usa Registrar esta copia explícitamente. Desmarcar elimina solo la entrada GameTouchCompanion del usuario actual.
- La posición del icono visible/oculto depende de Windows. Si no hay bandeja disponible al iniciar, Configuración se muestra. No ejecutar copias simultáneas; no hay IPC de instancia única.
- El portable histórico no incluye estas opciones. Usa el build actual; no mezcles sus DLL con el ZIP anterior.

- **No abre automáticamente:** activa Detección en esta sesión, guarda un único perfil Autoarranque para el ejecutable exacto y deja la ventana de juego foreground durante dos muestras. Revisa monitor/revisión y el aviso persistente. Tras corregir un intento fallido pulsa Rearmar.
- **Cerré Companion y no vuelve:** por diseño el cierre pausa Detección y la instancia atendida no se reabre. Reactiva y Rearma explícitamente.
- **EnumWindows falla en tests:** requiere sesión de escritorio; el sandbox puede devolver false sin código. No se eleva la aplicación ni se fuerzan permisos. Ejecuta las pruebas opt-in desde una sesión normal.
- **Contador de Detección distinto de FocusProbe:** Detección muestrea cada 2 s y puede omitir cambios breves. Para aprobar foco usa siempre FocusProbe y un intervalo sin cambios deliberados a Configuration.

- **WebView2 Runtime missing:** instale Microsoft Edge WebView2 Evergreen Runtime y reinicie la app.
- **Companion toma foreground:** capture la timeline de FocusProbe, HWND, PID y logs; no active fallbacks agresivos.
- **Página local no carga:** confirme que `TouchTestPage/index.html` está junto al ejecutable publicado.
- **SDK equivocado:** ejecute `dotnet --list-sdks`; se requiere 10.x.
- **NuGet bloqueado:** permita acceso HTTPS a `api.nuget.org` y repita `dotnet restore`.
- **Monitor guardado ya no existe:** la app selecciona un fallback, cierra Companion si estaba abierto y muestra un banner persistente. Confirme la selección actual o cambie el selector antes de reabrir.
- **Selección no persiste:** revise permisos y JSON en `%LOCALAPPDATA%\GameTouchCompanion\settings.json`; JSON corrupto se informa y no se sobrescribe silenciosamente.
- **Fullscreen desalineado con DPI mixto:** confirme el manifiesto PerMonitorV2 y capture bounds/logs; no convierta manualmente los píxeles Win32 en DIPs.
- **Favoritos/inicio no se guardan:** revise el aviso persistente y `%LOCALAPPDATA%\GameTouchCompanion\browser.json`. Si está corrupto, haga copia antes de corregirlo y reinicie; no se sobrescribe automáticamente. Si falla el guardado, los cambios solo están en memoria hasta un guardado exitoso.
- **No abre desde Navegador:** revise la selección y el banner de la pestaña Pantallas. El botón nuevo respeta el bloqueo de revisión del monitor.
- **Página falla:** use Reintentar o Ir a inicio. Solo se admiten URLs HTTP/HTTPS completas sin credenciales. Una descarga, permiso o aplicación externa se bloquea por diseño.
- **Proceso WebView2 terminado:** cierre y vuelva a abrir Companion. No desactive el sandbox de Chromium, certificados u otras protecciones. El smoke de escritorio requiere una sesión normal; se observó cierre de WebView2 en el entorno restringido de pruebas.
- **Build incremental BG1002 por BAML faltante:** se observó tras cambios XAML; `dotnet build GameTouchCompanion.sln --no-restore -t:Rebuild` regeneró los artefactos y compiló sin errores. No borre código ni preferencias.

## Fase 6: guía y borradores

- La guía no avanza: requiere selección válida, monitores distintos y ninguna revisión pendiente. Confirma Pantallas; el override de una pantalla solo permite pruebas manuales fuera de la guía.
- Terminar guía no abre nada: es intencional. Usa Abrir Companion o activa Detección explícitamente. Reiniciar guía no restaura ni borra settings.
- Al elegir otro perfil no cambia el editor: hay un borrador pendiente. Conservar borrador cancela el cambio; Descartar y continuar lo confirma. Guardar primero es otra opción. Los borradores no sobreviven al cierre de la app.
- Elegir monitor del desplegable no modifica la preferencia: pulsa Usar monitor elegido y después Guardar. Esto evita reemplazar automáticamente un monitor desconectado.
- Diagnóstico muestra muestras antiguas: la detección está pausada o no ha completado otra captura. Actualizar muestra manual consulta solo foreground; no reactiva el sondeo ni mide pérdidas exactas.
- El esquema no coincide con numeración de Windows: usa índices locales y representa bounds/roles. No detecta qué pantalla tiene touch. La leyenda muestra nombres y coordenadas reales.
- Restore NU1900 en sandbox: la consulta de vulnerabilidades de NuGet requiere red. Reintenta con acceso autorizado; no desactives auditoría ni ignores el error como evidencia de build.

## Portable (Fase 7)

- Falta DLL o TouchTestPage: extraer el ZIP entero, no copiar solo EXE ni mezclar versiones. Ejecutar Test-Portable.ps1 para comprobar integridad.
- Runtime WebView2 ausente: .NET incluido no incluye Evergreen. Consultar PORTABLE.md e instalar desde Microsoft por decisión del usuario; no hay instalador automático.
- Aparecen perfiles anteriores: esperado; portable y desarrollo comparten LocalAppData. Cerrar una copia antes de abrir la otra.
- Advertencia de procedencia: paquete sin firma. Verificar origen/hash; no desactivar seguridad del sistema. Hash no autentica al editor.
