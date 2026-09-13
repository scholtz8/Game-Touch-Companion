# ADR-0007 — Portable win-x64

## Context

Fases 1–6 cerradas; Fase 7 exige Release win-x64 self-contained en carpeta, sin instalador ni single-file inicial.

## Problem

Distribuir todos los binarios y contenido local sin depender del SDK o runtime .NET instalado y sin incluir perfiles privados. WebView2 Evergreen sigue siendo requisito externo.

## Options

Framework-dependent, self-contained folder o single-file/installer. Se elige carpeta self-contained conforme al alcance original.

## Decision

Perfil Portable.pubxml explícito: Release, win-x64, self-contained, sin single-file, trimming ni ReadyToRun. Script publica en directorio único dentro de artifacts/releases, comprueba archivos/runtimeconfig, incluye instrucciones de distribución, genera manifiesto SHA256 y ZIP. Nunca mezcla con una publicación anterior ni copia datos de LocalAppData. No instala runtimes, firma, sube artefactos o registra startup.

## Consequences

Actualización de cierre (2026-09-07): el usuario confirmó el portable y se cerró Fase 7 en su entorno. La prueba en equipo sin .NET/SDK o sin WebView2 pasa a compatibilidad futura; no se da por ejecutada ni bloquea ese cierre. Este acuerdo matiza el criterio previo de cierre indicado abajo. Ver BETA_VALIDATION.md para la siguiente validación.

Referencias primarias: [publicación .NET](https://learn.microsoft.com/en-us/dotnet/core/deploying/) y [distribución WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution). Self-contained incluye .NET, no convierte WebView2 en runtime incluido ni evita requisitos nativos del sistema. Las actualizaciones de seguridad de .NET requieren republicar la carpeta; no se heredan automáticamente del runtime global.

Carpeta mayor por runtime .NET incluido. Requiere WebView2 Evergreen instalado aparte. Portable describe los binarios, no los datos: settings/logs/WebView2 permanecen en LocalAppData. ZIP sin firma; hashes detectan cambios pero no autentican editor. Debe probarse extraído en otro directorio y en equipo sin .NET/SDK, con regresión manual de foco antes de cerrar Fase 7. No declarar Beta por publicar.
