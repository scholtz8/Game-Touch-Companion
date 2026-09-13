# Focus Model

Windows mantiene una única ventana foreground. Companion no intenta compartir ese estado: recibe interacción sin activarse. El criterio central es que `GetForegroundWindow()` siga devolviendo el HWND de FocusProbe/juego durante toda la interacción.

La prueba automatizada solo valida composición de flags y procesamiento de mensajes. El comportamiento de WPF, touch y los child HWND de WebView2 necesita hardware real. Si otro HWND obtiene foreground, detener Fase 2, identificar HWND/PID/parent y actualizar `GPT_HANDOFF.md`.
