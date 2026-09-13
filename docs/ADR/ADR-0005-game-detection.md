# ADR-0005: Sampled game detection and guarded automatic opening

## Context

Phase 4 passed automatic and user-reported manual checks. Phase 5 adds process detection, HWND identification and automatic profile loading. Existing profiles can already contain AutoLaunch=true, so enabling the feature must be visible and deliberate.

## Decision

Use a session-only detection toggle, off at startup, and sequential polling every two seconds. Capture only process identity/name/start time for configured executable names; dispose Process wrappers. Enumerate top-level HWNDs with EnumWindows, obtain PID/visibility/minimized state/owner/style/rectangle and foreground, without titles, command lines or process memory. Query work runs off the WPF dispatcher; results return to UI. No hooks or new dependencies.

Match executable basenames case-insensitively. Prefer the eligible foreground window; otherwise show a deterministic diagnostic candidate. Owned/tool/minimized/empty/invisible windows are not launch candidates. Do not trust MainWindowHandle as the sole source. Automatic action requires exactly one AutoLaunch profile for the detected process, its eligible window foreground for two consecutive captures, and fresh PID/start-time/HWND/foreground validation before opening. Multiple enabled profiles for the same process block automatic choice. Background matches are shown but do not navigate/open.

Attempt once per profile ID and process instance (PID + start timestamp). Changing HWND, closing Companion, hot-plug and switching foreground do not cause repeated automatic opening. An explicit Rearm button or a new process instance permits another attempt. Failed attempts remain blocked with a persistent explanation until rearmed. Stopping detection cancels queued work; manually applying a profile pauses detection. A process exit clears detected state but does not close the user's browsing window.

Reuse monitor/browser/no-activate services, not the manual button path with modal warnings. Automatic mode requires distinct game/Companion screens even when the testing override is enabled; it also rejects a target intersecting the detected game's rectangle. Respect existing monitor review and exact profile monitor selection. Never activate/restore the game, execute games or modify Windows startup. Expose PID/HWND/foreground and sampled focus transitions; at two seconds this is diagnostic sampling, not a substitute for FocusProbe's physical zero-loss test.

## Consequences and alternatives

Session opt-in avoids silently activating preexisting test preferences. Foreground/stability gating defers launch for background games and short-lived splash windows; this limitation is explicit. Basename matching does not establish executable trust and may match a different application with the same name. Inaccessible/exited processes are skipped; no elevation fallback. Exclusive fullscreen, launchers, multiple windows, virtual desktops and protected processes require manual compatibility checks. No custom SetForegroundWindow/SendInput/global hook is introduced.

## Primary references

- [EnumWindows](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows): top-level desktop windows.
- [GetWindowThreadProcessId](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowthreadprocessid): HWND-to-PID mapping.
- [IsWindowVisible](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindowvisible): WS_VISIBLE is not proof of unobscured content.
- [GetWindowRect](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect): screen bounds and DPI considerations.
- [Process.StartTime](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.starttime): instance timestamp; access can fail.
