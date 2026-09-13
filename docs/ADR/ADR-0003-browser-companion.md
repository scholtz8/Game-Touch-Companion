# ADR-0003: Browser state and navigation in Companion Mode

## Context

Phase 2 passed the user's monitor and focus checks. Phase 3 adds HTTP/HTTPS navigation, home, favorites and browser errors to the existing no-activate WebView2 host.

## Problem

Typing a URL requires the activatable configuration window. Companion must provide touch navigation without requiring keyboard foreground. Browser preferences must survive monitor refreshes, which currently rewrite settings.json. Web navigation must not silently launch other applications or expose arbitrary local files.

## Options

1. Put keyboard URL entry and every setting in the no-activate window.
2. Share a browser view model between configuration (URL entry/favorite management) and Companion (touch navigation/read-only current address).
3. Rewrite the monitor persistence model to accommodate all browser fields.

## Decision

Use a shared BrowserViewModel with keyboard URL editing in Configuration and touch controls in Companion. Store home, toolbar preference and favorites in a separate browser.json under LocalApplicationData. Keep the established monitor settings.json contract intact. The browser view model serializes writes and reports validation/save errors inline.

Use the existing stable WebView2 dependency. Serve packaged HTML through the existing virtual host with an allowlist of local entry pages. Validate HTTP/HTTPS navigation and redirects, reject credentials in URLs, arbitrary file access and external application schemes. User-initiated new-window links navigate in the same view; script popups, downloads and permission prompts are blocked in Companion Mode. No custom host objects or injected external JavaScript are added. Browser process/profile data lives in LocalApplicationData, separate from diagnostics.

Keep WebView2 certificate protections. Error/retry/home controls remain within the no-activate window. Routine logs contain event names/status codes, not browsing URLs, page titles, credentials or page content. Browser favorites/settings are intentionally local user data and excluded from diagnostic exports.

## Consequences

The user enters URLs during configuration, then returns foreground to FocusProbe/game for touch interactions. Browser controls and favorites remain usable without toggling window activation. Hiding the toolbar leaves a recoverable show-controls button. The two JSON files have independent ownership. Auth flows, downloads and site permission prompts are limited in Companion Mode; their compatibility requires manual testing and is not silently bypassed. External websites and touch behavior must be revalidated in Phase 3.

## Primary references

- [Microsoft WebView2 security guidance](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/security).
- [Microsoft WebView2 threading model](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/threading-model).
- [NewWindowRequested.Handled](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2newwindowrequestedeventargs.handled).
