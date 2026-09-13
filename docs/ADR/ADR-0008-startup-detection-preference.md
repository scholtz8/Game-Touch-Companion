# ADR-0008 — Detección al abrir

## Context

El usuario solicita habilitar detección por defecto al abrir, además de inicio Windows/bandeja/idiomas/personalización. Se divide el trabajo según POST_MVP_PLAN.

## Problem

El toggle actual es solo de sesión. Guardar una preferencia no debe iniciar detección prematuramente ni perderse al guardar monitores/perfiles.

## Options

Guardar toggle de sesión directamente o separar preferencia de inicio y estado de sesión. Se elige separación.

## Decision

ApplicationSettings.EnableDetectionOnStartup bool, false por defecto, propiedad aditiva compatible con JSON anterior. MainWindowViewModel conserva el valor en todos los guardados de selección/perfil. Un cambio se confirma en memoria solo tras SaveAsync exitoso, bajo el gate existente desde UI. Settings corruptos/no inicializados bloquean edición.

MainWindow aplica la preferencia una única vez al terminar inicialización, con runtime, settings/perfiles/browser cargados y monitores válidos. Si falla un requisito, no activa; corregir no provoca un arranque diferido inesperado. Cerrar Companion o pausar sesión no modifica preferencia. No registra inicio Windows aún.

## Consequences

La regla anterior de siempre arrancar desactivado pasa a ser el valor predeterminado, no una imposición. Preferencia solo afecta próximos inicios; iconos/JSON previos se conservan. Idioma inicial y bandeja deberán respetar esta barrera de inicialización en las siguientes etapas. Sin nuevas dependencias o APIs Native.
