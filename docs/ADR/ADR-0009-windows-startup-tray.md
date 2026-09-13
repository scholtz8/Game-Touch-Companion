# ADR-0009 — Inicio de sesión y bandeja

## Context

Etapa 1 validada por el usuario («funciona perfecto»). Siguiente etapa: inicio Windows, iniciar oculto y cerrar a bandeja, con icono del usuario.

## Problem

Registrar inicio debe ser explícito/reversible por usuario; ocultar no puede impedir salir o cerrar sesión. Los controles de bandeja son acciones deliberadas, no interacciones no-activate de Companion.

## Options

Tarea programada, Startup shortcut o HKCU Run; NotifyIcon del framework frente a implementar Shell_NotifyIcon. Se eligen HKCU Run y NotifyIcon para reducir código de interoperabilidad.

## Decision

Native encapsula lectura/escritura de un único valor GameTouchCompanion en HKCU Software/Microsoft/Windows/CurrentVersion/Run. No se modifica HKLM, StartupApproved ni entradas de otras aplicaciones. Valor: EXE actual entre comillas y --startup. Validar ruta absoluta/existente/nombre de app y límite de 260 caracteres. Otro valor/ruta no se reemplaza sin acción explícita; al arrancar solo leer. Registro es fuente de verdad, no se duplica bool en JSON. Windows puede deshabilitar/demorar inicio por políticas/Administrador de tareas.

StartMinimizedToTray y CloseToTray son bool JSON, false por defecto, preservados en todos los guardados. NotifyIcon usa framework Windows Forms (UseWindowsForms, sin nuevo NuGet), icono Resource e interfaz inyectable para tests. MainWindow conserva WPF y Companion intacto. App inicia explícitamente la ventana: inicializa sin Show, crea HWND oculto para seguir recibiendo topología, y solo omite Show si preferencias, shell/bandeja e inicialización son válidos. Error mantiene acceso a Configuration. --startup evita activación inicial si debe mostrarse por error/opciones.

Menú: abrir Configuration, activar/pausar detección, Rearmar y Salir. Show/Activate de Configuration solo al pedirlo explícitamente. Cerrar a bandeja conserva Companion/detección; cerrar Companion sigue pausando. Salir y cierre de sesión ignoran CloseToTray y liberan icono, watchers y Companion. No se registra startup durante pruebas automatizadas: backend inyectado/memoria. Registro real solo al marcar ajuste.

## Consequences

Build inicial detectó WFO0003 al añadir UseWindowsForms: el analizador pide mover DPI del manifiesto a inicialización Forms. Esta aplicación sigue siendo WPF; se conserva el manifiesto PerMonitorV2 validado y se suprime únicamente WFO0003 en App, sin llamar SetHighDpiMode ni Application.Run de Forms. [Diagnóstico y supresión documentada](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/compiler-messages/wfo0003). La consulta de shell usa [FindWindowW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-findwindoww) con clase Shell_TrayWnd, sin buscar títulos. Comprobar el shell y Visible reduce riesgo de ocultación sin bandeja, no certifica visibilidad real del icono en todo shell/política.

Ruta portable queda fija en el registro; mover/actualizar exige elegir explícitamente la copia nueva. El usuario puede usar --show para recuperar Configuration aunque tenga inicio oculto. Windows decide visibilidad del icono y puede reiniciar Explorer; NotifyIcon maneja recreación, pero shell real requiere validación manual. No añadir IPC/múltiples instancias en esta etapa; no ejecutar copias simultáneas. Idioma inicial de etapa 3 deberá preceder ocultación/detección.

Fuentes: [Run/RunOnce](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys), [NotifyIcon](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon?view=windowsdesktop-10.0).
