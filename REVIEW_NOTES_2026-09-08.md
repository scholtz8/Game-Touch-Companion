# Revisión UI — 2026-09-08

Cambios aplicados por GPT sobre el ZIP recibido:

- El texto de la barra y su preview queda siempre blanco, independientemente del tema Claro/Oscuro de Configuración.
- Se verificó que la paleta base ya ofrece contraste alto; se aumentó la opacidad de controles deshabilitados para mejorar legibilidad.
- Se retiraron cuadros pasivos de estado/resumen de Inicio, Pantallas, Detección, Navegador y Perfiles.
- Los estados pasivos continúan en logs/diagnóstico; Browser, Profiles, Monitor y Detection registran cambios mediante Serilog.
- Ajustes oculta mensajes normales de éxito/información y muestra un único panel rojo únicamente cuando ocurre un error.
- Pantallas y Detección conservan panel rojo solo para errores reales.
- Navegador y Perfiles conservan únicamente sus paneles de error.
- Las confirmaciones de descarte/eliminación de perfiles siguen existiendo, pero sin caja coloreada de fondo.
- Se eliminó la confirmación modal del override de mismo monitor; al estar habilitado explícitamente, se continúa y se registra un Warning.
- Los diálogos modales de producción quedan reservados a errores (runtime WebView2, selección inválida y fallo fatal de inicio).
- AppDialog ahora respeta los colores del tema Claro/Oscuro.
- Se añadió una regresión para verificar que el texto de la barra siga blanco en ambos temas.

Validación realizada aquí:

- Parse XML de todos los XAML: PASS.
- Revisión estática de los cambios: PASS.
- `dotnet build/test`: NOT RUN; este entorno no tiene .NET SDK.

Antes de publicar o sustituir tu build actual, ejecutar en Windows/Codex:

```powershell
dotnet restore
dotnet build -c Release -p:Platform=x64
dotnet test -c Release -p:Platform=x64
$env:GTC_RUN_WEBVIEW_TESTS = '1'
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj -c Release -p:Platform=x64 --filter 'Category=BrowserRuntime'
Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS
```

Luego seguir la sección `Regresión de legibilidad y estados pasivos (2026-09-08)` de `docs/TEST_PLAN.md`.
