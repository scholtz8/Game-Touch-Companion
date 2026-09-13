# Win32 Interop

Los P/Invoke están exclusivamente en `GameTouchCompanion.Native/NativeMethods.cs`. El target inicial es x64, y los estilos usan `GetWindowLongPtrW`/`SetWindowLongPtrW` con `nint`. Los errores se consultan mediante `Marshal.GetLastPInvokeError()`.

APIs: `GetForegroundWindow`, `GetWindowLongPtrW`, `SetWindowLongPtrW`, `ShowWindow`, `SetWindowPos`, `EnumDisplayMonitors`, `GetMonitorInfoW`.

Constantes principales: `WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`, `WM_MOUSEACTIVATE`, `MA_NOACTIVATE`, `SW_SHOWNOACTIVATE`, `SWP_NOACTIVATE`, `WM_DISPLAYCHANGE`, `WM_DEVICECHANGE`, `WM_DPICHANGED`.

`MONITORINFOEXW.rcMonitor` se conserva en píxeles físicos. Width y Height se calculan como `Right - Left` y `Bottom - Top`; Left/Top pueden ser negativos. La ventana Companion se posiciona tras crear el HWND y vuelve a colocarse después de cambios DPI sin marcar `WM_DPICHANGED` como manejado.
