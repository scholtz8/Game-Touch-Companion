# ADR-0010 — Idioma persistente antes del arranque

## Context

Etapas post-MVP 1 y 2 validadas. El usuario solicita español e inglés y elección inicial. La interfaz actual mezcla ambos idiomas.

## Problem

El diálogo inicial debe preceder detección, HWND de Companion y ocultación. Cambiar textos durante una partida no debe recrear ventanas ni cambiar foco.

## Options

Recursos por cultura con reinicio frente a bindings globales en vivo. Preferencia en settings.json frente a archivo independiente.

## Decision

Recursos RESX español (fallback) e inglés, claves compartidas y extensión XAML. Idioma por usuario en language.json, separado de selección de monitores: sus guardados no pueden borrar el idioma. Escritura atómica mediante archivo temporal. Solo es/en; valor ausente o no admitido pide elección. JSON ilegible/fallo de lectura no se sobrescribe silenciosamente.

App usa cierre explícito durante el diálogo inicial; cancelar termina sin detección ni registro Windows. Solo después de guardar y aplicar idioma construye MainWindow y restaura OnMainWindowClose. El diálogo bilingüe funciona sin cultura elegida. Ajustes permite guardar próximo idioma y requiere reinicio manual, sin recrear Companion ni alterar sesión.

Migración incremental: primero persistencia, diálogo y etiquetas estáticas; después estados, validaciones y errores dinámicos. No declarar etapa completa ni inglés integral mientras queden mensajes propios sin migrar. No traducir páginas web, URLs, perfiles del usuario, códigos técnicos ni mensajes externos del sistema.

## Consequences

Instalaciones anteriores muestran selector una vez porque no tienen language.json. Primera ejecución significa por usuario de Windows, no por equipo ni por carpeta portable. Sin detección automática del idioma del sistema, dependencias nuevas, reinicio automático ni activación desde Companion. Pruebas con rutas aisladas; datos reales del usuario intactos.
