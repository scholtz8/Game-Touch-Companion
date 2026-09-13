# ADR-0011 — Cambio de idioma en vivo

## Context

El usuario detectó textos dinámicos pendientes y solicita aplicar idioma al guardar, sin reiniciar. Sustituye el requisito de reinicio de ADR-0010.

## Decision

Guardar primero language.json; solo tras éxito publicar cambio observable. Recursos XAML usan bindings de una vía, no strings resueltos una vez. Mensajes conservan identidad/argumentos o renderizador, sin traducir URLs, nombres de perfiles ni contenido web. ViewModels notifican solo propiedades de presentación; guía y preview se redibujan sin cambiar selección, paso ni borradores. Diagnóstico conserva la muestra y hora originales; cambiar idioma no vuelve a consultar el escritorio. No recrear ventanas/WebView2, navegar, reiniciar detección ni rearmar instancias.

Bandeja actualiza etiquetas conservando icono y callbacks. Suscripción liberada al disponer; WPF usa eventos débiles/bindings. Si el guardado falla, cultura activa y preferencia anterior permanecen. El selector bilingüe de primera ejecución conserva su orden antes de MainWindow.

## Consequences

Pruebas de cambio en vivo, identidad de HWND/Companion, borrador/URL/detección, formatos, errores y regresión de escritorio. Diálogos nativos ya abiertos son modales: no se puede guardar un cambio detrás de ellos; siguientes aperturas usan idioma nuevo. Botones nativos de MessageBox pueden seguir el idioma de Windows, no son textos propios.
