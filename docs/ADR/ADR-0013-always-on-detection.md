# ADR-0013 — Detección automática siempre activa

## Context

La detección por sesión y la preferencia `EnableDetectionOnStartup` añadían dos estados que el usuario debía coordinar. Además, cerrar Companion pausaba toda la detección para evitar una reapertura inmediata, lo que impedía detectar después otra ejecución del mismo juego u otro juego configurado.

## Decision

La detección se inicia automáticamente después de cargar monitores, navegador y perfiles, y permanece activa mientras Game Touch Companion siga ejecutándose. Se eliminan el interruptor de sesión, la preferencia `EnableDetectionOnStartup` y la acción de pausar/reanudar desde la bandeja. El inicio con Windows sigue siendo una preferencia separada.

Una instancia de juego se identifica en memoria por nombre de ejecutable, PID y hora de inicio del proceso. Después de una apertura automática queda atendida. Si el usuario cierra Companion, se marca como descartada una sola instancia: primero la instancia asociada que abrió/reutilizó Companion; si Companion fue abierto manualmente y no existe asociación, se usa la última instancia configurada observada en primer plano. Esa instancia no vuelve a abrir Companion automáticamente, pero la detección continúa activa y otros juegos no quedan bloqueados.

Los estados atendido/descartado no se persisten en disco. Cuando una instancia termina se elimina del estado del detector. Una nueva ejecución del mismo juego, incluso si Windows reutiliza un PID, tiene una hora de inicio distinta y puede abrir Companion normalmente. Otro juego configurado tampoco queda bloqueado por una instancia descartada.

`Rearmar` limpia explícitamente los estados atendidos/descartados para permitir otro intento con una instancia todavía abierta. Como ya no existe una pestaña dedicada de Detección, esta acción vive en **Diagnóstico** y en el menú de bandeja. Los bloqueos temporales de apertura no detienen la detección; se vuelven a evaluar con un pequeño intervalo de reintento.

## Consequences

`settings.json` deja de guardar `EnableDetectionOnStartup`. JSON antiguos que contengan la propiedad siguen siendo compatibles porque el deserializador ignora propiedades desconocidas; el siguiente guardado elimina el campo obsoleto.

El cierre de Companion provocado por pérdida del monitor seleccionado no se considera un descarte del usuario. El cierre completo de Game Touch Companion cancela el bucle de detección y descarta naturalmente todo el estado temporal.

ADR-0008 queda supersedido en lo relativo a la preferencia de detección al iniciar. ADR-0009 queda supersedido en lo relativo a pausar/reanudar detección desde la bandeja y a pausar la detección al cerrar Companion.
