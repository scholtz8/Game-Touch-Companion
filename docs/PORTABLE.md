# Game Touch Companion — distribución portable

Versión de desarrollo 0.7.0-dev, Windows x64. No es todavía una Beta certificada.

## Ejecutar

1. Extrae el ZIP completo en una carpeta local. No ejecutes desde el ZIP ni copies solo el EXE.
2. Requiere Microsoft Edge WebView2 Evergreen Runtime. .NET está incluido; no necesitas SDK ni instalar .NET por separado.
3. Abre GameTouchCompanion.App.exe, sin ejecutar como administrador. Mantén TouchTestPage y todos los DLL junto a la aplicación.
4. Configura las pantallas y perfiles. La detección permanece desactivada al arrancar.

WebView2 se obtiene desde Microsoft: https://developer.microsoft.com/microsoft-edge/webview2/
No se incluye ni instala automáticamente. El comportamiento en una máquina sin WebView2 requiere prueba manual; la app conserva su aviso de runtime ausente.

## Datos y actualizaciones

Portable se refiere a los binarios. Configuración, perfiles, logs y caché WebView2 permanecen en `%LOCALAPPDATA%\GameTouchCompanion`; se comparten con ejecuciones de desarrollo del mismo usuario. No ejecutes dos copias a la vez. Cierra la app antes de actualizar y extrae cada versión en una carpeta nueva, sin mezclar DLL. Conserva la carpeta anterior para volver atrás; no se garantiza compatibilidad de datos con versiones futuras.

Eliminar la carpeta extraída no borra tus datos. No compartas LocalAppData, browser.json, profiles.json ni cookies. El paquete no contiene esos archivos personales.

## Integridad y límites

El ZIP tiene un archivo .sha256 externo; SHA256SUMS.txt contiene hashes de archivos internos (no de sí mismo). Permiten comprobar integridad, no identidad del autor: el paquete no tiene firma digital. Si Windows muestra una advertencia de procedencia, verifica origen y hash; no desactives protecciones del sistema.

Windows 11 x64 es el objetivo; Windows 10 y otros modos/hardware requieren pruebas. WebView2 depende de sus requisitos de sistema. No hay instalador, auto-update, registro de inicio de Windows ni ejecución de juegos. Mantén al juego foreground y valida foco con tu hardware.
