# English copy review — 2026-09-08

A full editorial pass was applied to `Resources/Strings.en.resx` to make the English UI read like native Windows application copy instead of a literal translation from Spanish.

## Editorial rules

- Prefer **Settings** over **Configuration** for the main app window.
- Use **display** consistently for Windows display selection; keep technical Win32 terminology only where it helps diagnostics.
- Use **the Companion** in explanatory sentences, while short button labels may remain `Open Companion`, `Close Companion`, etc.
- Replace literal/technical `Rearm instances` wording in user-facing English with **Reset auto-launch** / **Reset auto-launch state**.
- Prefer direct user actions: `Select`, `Open`, `Fix`, `Check`, `Enable`.
- Keep `NoActivate`, `WebView2 Runtime`, `foreground`, `schemaVersion`, and similar terms only where the feature is genuinely technical.
- Preserve every resource key and every composite-format placeholder.
- Keep button labels short and explanations sentence-cased.

## Examples

| Before | Revised |
|---|---|
| Game Touch Companion — Configuration | Game Touch Companion — Settings |
| Design the Companion toolbar. Preview does not change the session... | Customize the Companion toolbar. Preview changes do not affect the current session until you select Save... |
| Configuration theme | Settings theme |
| Show text with icons (when off, text appears on hover) | Show labels next to icons (when off, labels appear on hover) |
| Draft preview | Preview |
| Reset draft | Reset changes |
| Rearm instances | Reset auto-launch |
| Browser prepared. Open Companion to browse. | Browser ready. Open the Companion to start browsing. |
| Profile applied and Companion opened without activation. | Profile applied and the Companion opened without taking focus. |

## Validation performed here

- `Strings.resx` and `Strings.en.resx` contain the same 326 keys: PASS.
- All resource values are nonempty by inspection/parsing: PASS.
- Composite-format placeholders match between Spanish and English for every key: PASS.
- Both `.resx` files parse as valid XML: PASS.
- Build/tests: NOT RUN in this environment because the .NET SDK is unavailable.

## Small Spanish consistency correction

`Ui018` was changed from `Permitir la misma pantalla para pruebas (muestra aviso)` to `Permitir la misma pantalla para pruebas`, because the same-display confirmation dialog was intentionally removed in the previous UI cleanup.
