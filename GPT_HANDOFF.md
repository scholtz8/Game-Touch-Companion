# Actualización de regresión de localización — 2026-09-08

Estado: **FIX IMPLEMENTED / RE-TEST REQUIRED**. Evidencia del usuario: `dotnet test` ejecutó 199 pruebas, con 186 correctas, 12 omitidas y 1 error. Falló `LanguageStartupTests.DynamicMessagesRetainArgumentsAndTranslateErrorsWithoutChangingDraftData` porque la revisión editorial cambió `Dynamic100` de un texto con `Link blocked:` a `Link blocked. ...`, mientras el test seguía haciendo `Assert.StartsWith("Link blocked:", browser.Error)`. La app sí mostró el recurso nuevo esperado. Corrección aplicada: ambas aserciones EN/ES ahora usan `Assert.Equal(Localization.Get("Dynamic100"), browser.Error)`, por lo que la prueba sigue verificando traducción dinámica exacta sin duplicar el copy en el test. No se cambió lógica de producción. Reejecutar `dotnet test -c Release -p:Platform=x64`.

---

# Actualización de revisión de español — 2026-09-08

Estado: **IMPLEMENTED / BUILD NOT RUN IN THIS ENVIRONMENT**. Se revisó el copy español visible para eliminar redacción literal y tecnicismos innecesarios. Cambios de terminología principales: `Autoarranque` → `apertura automática`, `Rearmar` → `restablecer apertura automática`, `monitor` → `pantalla`, `foreground` → `primer plano`, y eliminación visible de `fallback`, `override`, `Configuration` y `null`. Se reescribieron ayudas, errores y estados para sonar naturales en una app Windows. Recursos: 326 claves ES/EN, XML PASS, placeholders idénticos. No se cambió la lógica ni `Localization.Catalog` porque sus cadenas fuente son identificadores exactos internos. Build/tests NOT RUN por ausencia de SDK .NET. Ver `docs/SPANISH_COPY_REVIEW_2026-09-08.md`.

---

# Actualización de revisión editorial en inglés — 2026-09-08

Estado: **IMPLEMENTED / STATIC VALIDATION PASS / BUILD NOT RUN IN THIS ENVIRONMENT**. Se revisaron las 326 cadenas de `Strings.en.resx` para eliminar traducciones literales y usar inglés natural de interfaz Windows. Cambios principales: `Configuration` → `Settings` en copy visible; terminología `display` consistente; `Rearm` visible → `Reset auto-launch`; ayuda de Personalization, navegador, perfiles, detección, bandeja y errores reescritos; placeholders y claves conservados. También se retiró del español `Ui018` la frase «(muestra aviso)» porque el diálogo de mismo monitor fue eliminado en la revisión UI previa. Validación: ambos RESX parsean como XML, 326 claves idénticas ES/EN y 0 diferencias de placeholders. `dotnet build/test`: NOT RUN porque este entorno no tiene SDK .NET. Ver `docs/ENGLISH_COPY_REVIEW_2026-09-08.md`.

---

# Actualización de revisión UI — 2026-09-08

Estado: **IMPLEMENTED / BUILD NOT RUN IN THIS ENVIRONMENT**. Solicitud: asegurar legibilidad Claro/Oscuro, mantener el texto de la barra siempre igual al cambiar tema y retirar cuadros pasivos de estado, dejando visibles únicamente errores. Cambios: `ToolbarAppearance.Content` fuerza texto blanco; contraste de la paleta existente verificado y estados deshabilitados reforzados; estados pasivos se ocultan y se registran con Serilog; Ajustes usa un único panel de error; confirmaciones destructivas de Perfiles quedan sin fondo; el warning modal de mismo monitor se reemplaza por log; `AppDialog` adopta tema. XAML parse PASS. `dotnet build/test`: NOT RUN porque este entorno no tiene SDK .NET. Próximo paso: ejecutar restore/build/test y smoke Claro/Oscuro en Windows/Codex, revisar las ocho pestañas y Companion con `ShowLabels` on/off.

---

# GPT HANDOFF

## 1. Fecha y hora

2026-09-07, America/Santiago. Post-MVP etapa4, actualización de iconos y distribución de la barra.

## 2. Objetivo actual

Avanzar con personalización por autorización del usuario. Barra configurable con iconos vectoriales, controles de imagen transparentes y distribución fija implementada; temas globales de Configuration y rediseño integral pendientes. No inferir confirmación manual de idiomas de «continua con lo siguiente».

## 3. Fase actual

FASE 7 — COMPLETE. Fases 1–7 cerradas en el entorno reportado; compatibilidad ampliada futura.

## 4. Estado

Build Release PASS,187 normales PASS (11SKIP) y smoke de escritorio focalizado PASS. Etapa4 en curso: barra actualizada en código, pendiente manual. No nuevo ZIP ni versión.

## 5. Problema observado

La barra usaba glifos de texto, título y botones con fondo. ADR-0012 ahora documenta imágenes vectoriales transparentes, navegación fija a la izquierda, favoritos a la derecha, Inicio/título ocultos, recarga circular verde, cierre con cruz roja y control de barra con ojo. Una repetición paralela anterior falló dentro de WPF PackagePart; serialización de tests UI y reintento PASS, sin cambio del producto por ese fallo.

## 6. Comportamiento esperado

Preview local al editar. Guardar appearance.json y aplicar solo tras éxito, conservando Companion/URL/detección. Restablecer afecta borrador hasta Guardar. El texto junto a iconos es opcional y los tooltips permanecen localizados; no hay título visible; cerrar y recuperar barra siempre accesibles; mínimos44DIP y controles no enfocables.

## 7. Comportamiento real

Personalización bilingüe: cuatro paletas, tres densidades, texto opcional junto a iconos y caption hasta40 caracteres. Navegación está fija a la izquierda, favorito/favoritos a la derecha, e Inicio se colapsa. Template redondeado compartido preview/Companion. Temas completos claro/oscuro/sistema y diseño global aún no implementados.

## 8. Pasos exactos para reproducir

Ejecutar build actual, abrir Companion y Personalización. Editar solo cambia preview; Guardar aplica en vivo. Probar paletas/densidades, texto/iconos/caption y persistencia al reabrir. Verificar tooltips en modo solo icono, grupos izquierdo/derecho, ojo y cruz roja. Restablecer requiere Guardar. Ver TEST_PLAN etapa4. No usar ZIP histórico para estas opciones.

## 9. Entorno

Windows 11 x64, Windows reportado 10.0.26200.0; .NET SDK 10.0.400; app 0.7.0-dev; WebView2 SDK 1.0.4191.47 sin cambios. Último runtime observado en Fase 6: 152.0.4191.66. Publicación Release win-x64 self-contained. Smoke Release con juego/monitores simulados, WPF/WebView2 reales, datos aislados. Portable: PASS manual global del usuario. Máquina sin .NET/SDK: sin confirmación específica.

## 10. Git

Branch: master. Sin commits. Los archivos del proyecto permanecen untracked; no se creó commit ni se descartaron cambios del usuario.

## 11. Archivos modificados

Core AppearanceSettings/Store; App Appearance, AppearancePanel.xaml/.cs, CompanionWindow.Appearance.cs, ToolbarAppearance, TouchButtonResources y Localization object binding. MainWindow añade octava pestaña; App carga apariencia antes de MainWindow. RESX es/en ampliados. AppearanceSettingsTests/AppearanceTests y UI test collection. ADR-0012/PERSONALIZATION/estado/handoffs.

## 12. Cambios realizados

appearance.json independiente por usuario, validación presets/caption y reemplazo atómico. Falla de lectura conserva archivo y usa defaults; aviso indica que Guardar lo reemplaza explícitamente. Panel no publica borrador hasta persistir. Las imágenes vectoriales cambian en vivo con densidad/texto/idioma; navegación y favoritos conservan handlers, sin recreación de ventana ni activación.

## 13. Código relevante

Appearance.Current observable; Save_Click await Store.SaveAsync(draft) antes de Apply. Preview instancia local, template compartido. Companion suscribe eventos débiles y los quita al cerrar; IsClosing/dispatcher guard. Caption vacío usa recurso localizado, caption propio no se traduce.

## 14. APIs Win32 involucradas

Sin nuevas APIs Win32/cambios Native de foco. Mantiene HWND/WebView2 y Focusable/IsTabStop false. Cierre y recuperar barra no son configurables ni se pueden ocultar.

## 15. Logs relevantes

Normal final: toolbar-final_net10.0_20260907205204.trx, ...205205.trx y ...205206.trx (187PASS). Escritorio focalizado: toolbar-appearance-desktop_net10.0_20260907205016.trx,1PASS. Fallo inicial por aserción obsoleta que contaba Inicio colapsado: ...204952.trx; no fue fallo del producto. El incidente paralelo histórico PackagePart se conserva. Sin nueva timeline física.

## 16. Resultado de build

Build Release PASS,0 warnings/errors. Sin paquetes nuevos/restore, ZIP/version intactos. Iconos vectoriales WPF, no dependientes de archivos raster adicionales.

## 17. Resultado de tests

187 normales PASS (Core143 Native15 Integration29),0FAIL,11SKIP. Smoke de escritorio focalizado1PASS,3s. Comprueba persistencia/reset/fallo, preview separado, iconos, grupos, Inicio oculto, tamaño/no-focusable, HWND/URL/detección retenidos. No es validación física multi-DPI/foco. UI tests siguen serializados tras fallo interno histórico de carga concurrente PackagePart.

## 18. Pruebas manuales

Fases 1/2: PASS reportado por el usuario en touch, fullscreen, persistencia, mismo monitor, cambios/reconexión y Focus losses = 0. BUG-005: aviso inicialmente sobrescrito por otra ráfaga; banner persistente corregido y confirmado por el usuario.

Fase 3: PASS (reporte manual global del usuario, 2026-09-06) tras recibir el protocolo de pruebas. Confirmó funcionamiento esperado y Focus losses sin aumentos. Descarga y aplicación externa no abrieron nada; mostraron avisos integrados. La captura muestra el mensaje «Enlace bloqueado: solo se permiten direcciones HTTP y HTTPS sin credenciales» en paneles de estado/error dentro de Configuration. El archivo local no autorizado no abrió nada ni mostró aviso: no se atribuye una causa interna sin evidencia y no se considera fallo del bloqueo. No se registró un conteo desglosado de acciones ni sitios. El PASS manual se basa en su reporte, no en el smoke automático.


Fase 4: PASS (reporte manual global del usuario, 2026-09-06): «funciona todo ok», en respuesta a las instrucciones de perfiles, persistencia, aplicación, validación y FocusProbe. Sin desglose por caso ni evidencia nueva del contador; el PASS se atribuye a su confirmación, no al smoke ni a las fases anteriores.

Fase 5: PASS manual reportado por el usuario el 2026-09-07. Indicó «me funciono todo bien», confirmó detección con un juego de su elección, apertura de la URL correspondiente y ausencia de pérdida de foco observada, incluso usando control. Después confirmó «ya lo probe y tambien me funciono» para cerrar Companion → Rearmar → activar detección → volver al juego → reapertura. No aportó nombre del juego, modo de pantalla, nueva timeline, captura del contador ni desglose de 50 acciones o casos negativos. El cierre se basa en su reporte global y esas confirmaciones explícitas, no en mediciones inferidas.

Fase 6: PASS manual global reportado por el usuario el 2026-09-07: «todo funcionando como corresponde», en respuesta a la entrega y protocolo de UX. Se registra su confirmación global, sin desglose por escenario, nueva timeline/captura del contador, conteo de acciones, juego/modo o escalas DPI. No se infieren mediciones exactas ni compatibilidad ampliada a partir de esa respuesta.

Fase 7: PASS manual global reportado por el usuario el 2026-09-07 tras recibir el ZIP y protocolo del portable: «funciona todo bien», reiterado como «todo funciono como corresponde». Se confirma funcionamiento del portable en su entorno. No se aportó desglose por caso, nueva timeline/captura del contador, conteo de acciones ni prueba explícita en equipo sin .NET/SDK o sin WebView2. Esas configuraciones quedan como compatibilidad futura, no bloquean el cierre en el entorno reportado.

Etapa 1 post-MVP: PASS global del usuario («funciona perfecto»). Sin desglose ni nueva timeline; no se extrapola a etapa 2.

Etapa 2 post-MVP: PASS manual global del usuario el 2026-09-07: «funciono todo como deberia», en respuesta a entrega de inicio Windows/bandeja y protocolo. Cierre en el entorno reportado; sin desglose por escenario, timeline, contador ni mediciones exactas inferidas.

Etapa3: usuario reportó textos faltantes y pidió actualización en vivo. Corregido en código; no hay confirmación manual final de esta entrega ni nuevas mediciones de foco.

Etapa4: usuario autorizó continuar al siguiente punto. No aportó validación manual de la barra nueva; pruebas con hardware pendientes.

## 19. Foreground timeline

Fase 1 previamente registrada:
```text
22:28:58.327 0x0000000000170F2E FOCUS PROBE (Losses=0)
22:29:41.365 0x0000000000040BEA OTHER (posterior al intervalo informado)
```

Fases 2 y 3: Focus losses = 0 reportado por el usuario. Para Fase 3 no se adjuntó una nueva timeline; la captura aportada corresponde a los avisos de bloqueo.

Fase 5: ausencia de pérdida de foco observada reportada por el usuario; no se adjuntó nueva timeline ni contador. No se infiere una medición del muestreo de 2 s.

Fase 6: no se adjuntó nueva timeline ni captura del contador. La confirmación global no permite atribuir un conteo exacto de pérdidas o acciones.

Fase 7: no se adjuntó nueva timeline ni contador; el reporte global no se convierte en mediciones exactas.

## 20. Hipótesis

NullReferenceException dentro de PackagePart en una repetición paralela es consistente con carga WPF concurrente entre STA de pruebas. Tests UI comparten colección no paralela; conservar fallo/reintentos, no atribuirlo a foco físico.

## 21. Alternativas consideradas

ADR-0012: presets contrastados y densidades acotadas frente a RGB/tamaños arbitrarios. Orden por presets frente a editor libre. Archivo separado evita que guardar browser/monitores borre apariencia. Temas de toda Configuration en siguiente bloque.

## 22. Ventajas y desventajas

Cambios visuales en vivo, preview independiente y recuperación siempre accesible. Opciones acotadas: no RGB libre, tipografía arbitraria ni texto personalizado de cada botón todavía. Caption40 caracteres; mínimos44DIP.

## 23. Riesgos

Manual de idiomas y barra pendiente, no inferido. Temas globales/rediseño completo pendientes. Tamaños físicos/DPI/touch/foco requieren hardware. Render WPF omite contenido WebView2 por airspace, URL real sí probada. No copias simultáneas.

## 24. Decisión requerida a GPT

Ninguna decisión bloqueante. Validar barra y continuar siguiente bloque de temas/diseño; no cerrar etapa4 ni publicar portable como entrega integrada todavía.

## 25. Preguntas

Sin preguntas bloqueantes. El usuario autorizó avanzar, no dio un nuevo PASS manual global de idiomas.

## 26. Próximo paso propuesto por Codex

Validar Personalización/Guardar/Restablecer con Companion y juego/FocusProbe. Continuar temas claro/oscuro/sistema y rediseño de Configuration; después entrega portable integrada.

## 27. Diff resumido

Primer bloque de personalización implementado/verificado, estilos de barra compartidos y documentación. Ocho pestañas. Tests WPF agrupados para evitar concurrencia de recursos. Sin commits/paquetes/ZIP/version nuevos.

## 28. Dependencias

Sin nuevas dependencias. WPF templates/bindings y JsonSerializer del framework. Windows Forms permanece solo NotifyIcon, supresión WFO0003 histórica conservada.

## 29. Información adicional

Fases1–7 y post-MVP1–2 cerradas históricamente. Idiomas implementado/manual pendiente; etapa4 primer bloque implementado con siguiente bloque visual pendiente. Portable histórico no incluye ampliaciones. Sin certificación Beta.
