# ADR-0004: Game profiles, manual application and independent persistence

## Context

Phase 3 passed automatic and user-reported hardware checks. Phase 4 requires game profiles, map URL, monitor preference and an auto-launch setting. Process detection and automatic profile loading belong to Phase 5.

## Decision

Store a versioned collection in LocalApplicationData/GameTouchCompanion/profiles.json, independent of browser.json and settings.json. Use Core records, strict validation and atomic replacement, without new dependencies or SQLite. Invalid/corrupt/future-schema documents block writes and remain untouched. IDs are stable; edits and deletes update the visible collection only after a successful save. Deletion requires inline confirmation.

Provide a basic Configuration tab to create/edit/delete and manually apply saved profiles. Each has a name, optional executable basename, HTTP(S) map URL, optional Companion monitor and AutoLaunch preference. Empty monitor means use current selection. AutoLaunch requires an executable name, is stored only, and does not launch games, register Windows startup or detect processes. NoActivate must remain true and RestoreGameFocusFallback false; these are not editable switches.

Applying a profile validates the current monitor topology and existing review/same-monitor rules before changing navigation. Missing preferred monitor blocks application rather than silently choosing a fallback. Apply the monitor through MainWindowViewModel under the existing settings gate, then queue/navigate the map URL through BrowserViewModel; do not overwrite browser home or favorites. Closed Companion stays closed until explicitly opened with the existing controls. No active-profile identity is persisted or inferred after manual browser/monitor changes.

Draft changes require Save; choosing another profile/New discards the draft with an explicit UI explanation. Apply uses the selected saved profile, never an unsaved draft. The UI does not edit while a storage/application operation is running. Profile file content and process names/URLs are not logged or exported by diagnostics.

## Alternatives / consequences

Merging profiles with existing settings would broaden the migration and risk overwrites. A separate versioned document preserves existing contracts. Runtime monitor selection is persisted when a profile is applied, but its URL remains a navigation request, not a new browser home. A basic editor is necessary to validate profiles now; richer profile-manager UX remains Phase 6. No new Win32 API, hook, JavaScript injection or focus workaround is introduced. New manual application/monitor tests are required before closing Phase 4.
