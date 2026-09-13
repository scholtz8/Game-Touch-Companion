# Security policy

## Scope

Game Touch Companion is an experimental Windows application that uses WPF, WebView2, and Win32 APIs to display web content on a secondary display without intentionally taking foreground focus from a game. It does not inject code into games, modify game memory, install drivers, or provide a remote service.

## Reporting a vulnerability

Please do not open a public issue for a suspected security vulnerability. Contact the repository owner privately through the security contact mechanism configured on GitHub. Include a concise description, affected version or commit, reproduction steps, and impact. Do not include passwords, tokens, private URLs, user profiles, browser data, or other personal information.

If no private GitHub security contact is available, open a minimal issue asking for a private reporting channel without disclosing vulnerability details.

## Sensitive data

Local settings, browser preferences, profiles, logs, and WebView2 user data are stored outside the repository under `%LOCALAPPDATA%\\GameTouchCompanion`. Profiles may contain private URLs. Remove or redact this data before sharing diagnostics.

The project does not require credentials or signing certificates to build. Never commit secrets, certificates, WebView2 profiles, generated archives, or diagnostic output.

## Supported versions

Security fixes are applied to the current development branch. Because the project is experimental, older commits and unreleased builds may not receive backports.
