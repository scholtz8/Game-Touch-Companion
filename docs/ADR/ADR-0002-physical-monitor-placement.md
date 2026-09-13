# ADR-0002: Place the companion with Win32 physical monitor bounds

## Context

Phase 2 must place a borderless no-activate WPF window on monitors that may use different DPI scaling and negative desktop coordinates.

## Problem

WPF window properties use device-independent units, while `EnumDisplayMonitors` reports physical desktop pixels. Assigning Win32 bounds directly to `Window.Left`, `Top`, `Width`, and `Height` can misplace or resize the window under mixed DPI.

## Options

1. Convert every Win32 rectangle to WPF device-independent units.
2. Place and size the created HWND directly with `SetWindowPos` and `SWP_NOACTIVATE`.
3. Use WPF maximization and let Windows choose a monitor.

## Decision

Declare Per-Monitor V2 awareness and place the Companion HWND directly from the selected monitor's physical bounds through the centralized Native service. Continue using `SWP_NOACTIVATE` and do not maximize.

Persist only stable device names and user options in JSON. Re-enumerate bounds every time the app starts or receives `WM_DISPLAYCHANGE`; never persist coordinates as authoritative state.

## Consequences

Negative coordinates and mixed-DPI layouts remain in the same coordinate space as User32. Placement happens only after HWND creation. If a stored monitor disappears, selection logic must choose a safe current fallback and notify/log the change.
