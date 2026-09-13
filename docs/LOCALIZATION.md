# Idiomas — etapa 3 implementada, pendiente manual final

## Comportamiento actual

Español e inglés para etiquetas, guía/Next y sus tres pasos, esquema/roles de pantallas, diálogos propios, diagnóstico, estados de navegador/perfiles/detección y bandeja. Guardar en Ajustes aplica inmediatamente; no requiere reiniciar. ADR-0011 sustituye el reinicio propuesto inicialmente en ADR-0010.

La preferencia se guarda atómicamente en `%LOCALAPPDATA%\GameTouchCompanion\language.json`, separada de monitores/perfiles/navegador. Se admite es/en. Ausencia o valor no admitido abre selector bilingüe; JSON ilegible/malformado falla sin sobrescribirse. App no crea MainWindow ni activa detección/bandeja hasta aceptar y guardar la elección inicial. Cancelar termina. Esto precede --startup y --show.

En Ajustes se guarda primero; solo tras éxito se publica el idioma. Fallo conserva idioma activo anterior y muestra mensaje. El cambio conserva HWND de Configuration y Companion, WebView2, página/URL, borradores, confirmaciones de perfiles, paso de guía, selección de pantallas y detección. No captura nueva muestra de diagnóstico: conserva valores/hora y solo actualiza presentación.

## Implementación

Resources/Strings.resx es español/fallback; Strings.en.resx comparte claves. TrExtension devuelve un Binding de una vía a LanguageState observable. Ui000–Ui116 son claves estables, no renumerarlas. Localization.Catalog relaciona fuentes exactas/plantillas con claves dinámicas; no hace búsqueda/reemplazo en textos renderizados.

LocalizedMessage conserva identidad de mensajes y argumentos para los ViewModels. Renderizadores de textos code-behind se reevalúan al cambiar idioma. MonitorSelectionIssue conserva opcionalmente la plantilla/argumentos sin cambiar código, severidad ni lógica de validación. Nombres de dispositivos y perfiles no se traducen. Selector de pantallas usa plantilla visual localizada. Bandeja cambia etiquetas sin recrear icono/callbacks y libera suscripción al salir.

AppDialog traduce cuerpo/título y botones Sí/No/Aceptar de los avisos propios; No es la opción predeterminada del aviso de pruebas. El selector inicial y fallo fatal previo a cultura permanecen bilingües. No se traducen páginas web, URLs, nombres del usuario, logs técnicos, identificadores/códigos ni mensajes externos de Windows/WebView2.

## Verificación

Build Release PASS sin advertencias/errores. 181 pruebas normales PASS (Core137, Native15, Integration29), 10 SKIP (hardware y 9 escritorio opt-in). Escritorio: 9 PASS. TRX finales en artifacts/test-results/latest.txt.

Cobertura: paridad/no-vacíos y formatos de recursos, persistencia/cancelación/fallos; cambio en vivo es/en con Companion/WebView2 reales, identidad de HWND/URL, borrador y confirmación conservados, detección activa, misma marca temporal de diagnóstico, idioma anterior ante fallo de escritura y diálogo con botones traducidos. Menú real de bandeja verificado tras cambiar idioma. Pruebas con archivos aislados, sin tocar language.json ni registro Windows reales del usuario.

Renders revisados: guía, pantallas, diagnóstico en inglés, y barra/estado de Companion tras cambio en vivo. El render de WPF no captura los píxeles del contenido WebView2 (airspace); la navegación real se verifica mediante estado/URL. No representa prueba física de foco ni multi-DPI.

## Incidencia detectada y corregida

La prueba ampliada encontró un evento diferido de selección que leía ambos ComboBox antes de que ambos estuvieran listos, borrando la selección de Companion. La prueba se detuvo en un aviso modal y dejó un error secundario de hilo. Ahora SelectedItem es OneWay: el manejador persiste solo el control de origen, ignora nulos transitorios y cambios sin efecto. El cambio de idioma reenvía notificaciones al dispatcher propietario si es necesario. Regresión final aprobada.

## Pendiente

Confirmación manual del build actual: cambiar idiomas con Companion abierto, recorrer guía/pantallas/diálogos/diagnóstico, comprobar persistencia al siguiente arranque y repetir interacción touch/juego/FocusProbe. No se hereda evidencia de foco de etapas anteriores. Personalización y nuevo portable siguen después; ZIP anterior sin estas ampliaciones.
