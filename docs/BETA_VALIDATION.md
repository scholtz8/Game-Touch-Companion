# Validación para Beta

## Estado

Preparación del registro: 2026-09-07. Fases 1–7 cerradas con los reportes del usuario; portable 0.7.0-dev funcional en su entorno. No se solicita repetir esas validaciones. Aún no hay evidencia desglosada suficiente para declarar Beta según el brief.

## Criterios del proyecto

- Cinco juegos probados.
- Dos configuraciones de hardware.
- Windows 11 probado; Windows 10 cuando haya equipo disponible.
- Al menos 50 interacciones por sesión y 30 minutos de prueba.
- Juego conserva foreground, touch funciona y no hay crashes ni bucles de activación. Registrar cualquier fallo y su contexto.

Las dos configuraciones son equipos/topologías de hardware descritos, no dos copias del ZIP. Registrar el modo de pantalla real; no atribuir un resultado borderless a fullscreen exclusivo. No es necesario compartir nombres de usuario, URLs privadas, cookies ni perfiles JSON.

## Registro de sesiones

El juego ya confirmado en Fase 5 y el portable confirmado en Fase 7 conservan su PASS global. Nombre del juego, duración y configuración detallada no fueron proporcionados; no se inventan para rellenar la matriz. Se pueden incorporar aquí si el usuario conserva esos datos.

| Sesión | Juego / ejecutable | Hardware | Windows / modo de pantalla | Duración | Acciones touch | Foco / estabilidad | Resultado |
|---|---|---|---|---|---|---|---|
| 1 | Por registrar | — | — | — | — | — | Sin registro individual |
| 2 | Por registrar | — | — | — | — | — | Sin registro individual |
| 3 | Por registrar | — | — | — | — | — | Sin registro individual |
| 4 | Por registrar | — | — | — | — | — | Sin registro individual |
| 5 | Por registrar | — | — | — | — | — | Sin registro individual |

Para cada hardware: usar etiqueta A/B, GPU, monitores/resoluciones, escalas DPI, posiciones relativas, pantalla táctil e input de juego. Los números de serie no son necesarios.

## Pruebas adicionales de compatibilidad

- Equipo/VM sin .NET/SDK, con WebView2: abrir portable. No desinstalar runtimes del equipo habitual.
- Entorno desechable sin WebView2: comprobar aviso y recuperación tras instalarlo.
- DPI 100/125/150/200 y topologías izquierda/derecha/arriba/abajo cuando estén disponibles.
- Windows 10 y fullscreen exclusivo: registrar limitaciones o NOT TESTED, sin inferir compatibilidad universal.

Estos casos no reabren las fases ya cerradas; documentan el alcance futuro. El criterio Beta y sus evidencias se revisan antes de etiquetar una release como Beta.

## Cómo avanzar

Usar el ZIP existente, cerrar otras copias y registrar las próximas sesiones de juego habituales. No ejecutar pruebas destructivas ni modificar controles de seguridad. Si falla foco, anotar hora, modo, acción y HWND/timeline cuando esté disponible; no introducir hooks o restauración forzada como solución automática.

El agente puede mantener el registro y analizar resultados, pero no simular juegos, touch físico, otro hardware o duraciones no realizadas. Instalador, firma digital y auto-update son decisiones futuras, no requisitos pendientes del MVP.
