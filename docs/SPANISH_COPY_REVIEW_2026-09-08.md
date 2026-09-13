# Revisión editorial del español — 2026-09-08

## Objetivo

Revisar la redacción visible en español para que suene natural en una aplicación de Windows, evitando traducciones literales, tecnicismos innecesarios y terminología inconsistente. No se modifican claves de recursos, comportamiento, arquitectura ni placeholders de formato.

## Criterios aplicados

- Español neutro y directo, adecuado para una interfaz de escritorio.
- Instrucciones breves en segunda persona cuando el usuario debe actuar.
- `pantalla` como término visible, en lugar de alternar entre `monitor` y `pantalla`.
- `apertura automática` en lugar de `Autoarranque`, porque la función abre Companion y no inicia el juego.
- `restablecer apertura automática` en lugar de `Rearmar`.
- `primer plano` en lugar de `foreground` en copy visible.
- Eliminación de `fallback`, `override`, `null` y otros anglicismos cuando no aportan valor al usuario.
- Se conservan nombres técnicos propios cuando son necesarios: `NoActivate`, `WebView2 Runtime`, `FocusProbe`, nombres de archivos JSON y `DIP`.
- Se mantienen `Companion`, `Detección`, `Pantallas`, `Navegador`, `Perfiles`, `Ajustes`, `Personalización` y `Diagnóstico` como términos de producto/interfaz.

## Ejemplos

| Antes | Después |
|---|---|
| Prepara tu Companion | Configura tu Companion |
| Rearmar instancias y reiniciar contador muestreado | Restablecer apertura automática y contador de muestras |
| Activar detección y autoarranque en esta sesión | Activar detección y apertura automática durante esta sesión |
| Actualizar muestra de foreground | Actualizar muestra de ventana en primer plano |
| Monitor Companion (vacío = selección actual) | Pantalla de Companion (vacío = selección actual) |
| Monitores conectados para el perfil | Pantallas disponibles para este perfil |
| Sin fallback de restauración de foco | No se usa ningún método alternativo para restaurar el foco |
| Vista previa del borrador | Vista previa |
| Restablecer borrador | Restablecer cambios |

## Alcance

- `Resources/Strings.resx`: revisión editorial extensa de la interfaz, ayudas, estados, errores y diagnósticos.
- 326 claves ES/EN conservadas.
- Ninguna cadena española vacía.
- Placeholders de formato ES/EN conservados sin cambios.
- XML de ambos RESX válido.

## Términos literales retirados del copy español visible

- `Configuration`
- `foreground`
- `fallback`
- `override`
- `Autoarranque`
- `Rearmar`
- `monitor`
- `null`

Las cadenas fuente internas usadas como identificadores exactos por `Localization.Catalog` no se modifican porque no representan el texto final mostrado al usuario; siguen resolviéndose a las claves de recursos revisadas.

## Validación pendiente

Este entorno no dispone del SDK de .NET, por lo que `dotnet build` y `dotnet test` no se ejecutaron aquí. Ejecutar en Windows:

```powershell
dotnet restore
dotnet build -c Release -p:Platform=x64
dotnet test -c Release -p:Platform=x64
```

Después recorrer visualmente las ocho pestañas en español y comprobar que los textos largos no produzcan recortes en DPI 100 %, 125 %, 150 % y 200 %.
