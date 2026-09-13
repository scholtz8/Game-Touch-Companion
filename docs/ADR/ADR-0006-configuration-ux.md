# ADR-0006 — Configuration UX

## Context

Fases 1–5 cerradas por reporte manual; Fase 6 exige setup wizard, monitor preview, profile manager, settings y diagnostics screen. MainWindow sigue activable, Companion sigue no-activate.

## Problem

La configuración está dispersa, las coordenadas de monitor no son visuales, los borradores se descartan al cambiar de perfil y Rearmar no explica suficientemente su relación con la pausa.

## Options

1. Reescribir ventanas y stores con un framework UI: más riesgo y dependencias.
2. Añadir guía integrada y controles sobre servicios existentes, manteniendo contratos y archivos JSON.

## Decision

Elegir 2. Asistente de tres pasos en una pestaña inicial, con enlaces a Pantallas y Perfiles/Navegador y resumen final. Es una guía repetible, no una transacción ni una marca persistida de onboarding; los cambios se guardan en sus paneles existentes. No abre Companion ni activa detección al terminar.

Vista esquemática de pantallas en Configuration calculada en Core con una transformación uniforme de píxeles físicos a espacio de preview. Coordenadas negativas admitidas; no se usa para placement de ventanas ni cambia DPI/posición real. Roles indicados mediante texto además de color.

Gestor de perfiles conserva borradores: cambiar perfil/Nuevo con cambios pendientes requiere descartar explícitamente o cancelar, mediante aviso integrado. Añadir selector de monitores conectados para copiar al borrador sin reemplazar silenciosamente preferencias ausentes. Stores y validación no cambian.

Ajustes muestran políticas fijas y accesos a opciones existentes. Diagnóstico reúne versión/runtime, selección/revisión y muestras existentes de detección; actualización explícita de foreground sin nuevo timer. No exporta datos ni ejecuta scripts; exportación avanzada queda fuera de esta entrega. Contadores no instrumentados se etiquetan como no disponibles, nunca cero inferido.

## Consequences

Sin dependencias, P/Invoke, hooks ni cambios de CompanionWindow. No cambia AutoLaunch ni protección de monitor. Las confirmaciones de borrador modifican deliberadamente la UX de Fase 4; se añaden tests de cancelación/descarte/guardado fallido. La guía no promete rollback de cambios guardados. Se requiere prueba física de navegación/preview/DPI y regresión de foco; el éxito histórico de Fase 5 no valida automáticamente Fase 6.
