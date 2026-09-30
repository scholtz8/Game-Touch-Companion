# Codex — Validate persistent monitor identity + Settings UX

## Scope

Execution and validation only.

Do **not** modify source code.
Do **not** refactor.
Do **not** commit.
Do **not** push.

This validation includes the previously implemented always-on detection changes because they have not yet been validated separately in this working tree.

If any required step fails, STOP and return the complete output. Do not fix the code.

---

## 1. Baseline

From the repository root:

```powershell
git branch --show-current
git status
dotnet --version
```

Record the current branch and pre-existing working-tree changes.

---

## 2. Restore + Release build

```powershell
dotnet restore

dotnet build -c Release -p:Platform=x64 --no-restore
```

Required:

- Build PASS
- 0 errors

Report all warnings.

If build fails, stop.

---

## 3. Always-on detection core regression

```powershell
dotnet test tests/GameTouchCompanion.Core.Tests/GameTouchCompanion.Core.Tests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~GameDetectionTests"
```

Required: 0 failed.

Report Passed / Failed / Skipped.

---

## 4. Application-settings migration and persistence

```powershell
dotnet test tests/GameTouchCompanion.Core.Tests/GameTouchCompanion.Core.Tests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~JsonApplicationSettingsStoreTests"
```

Required: 0 failed.

Important coverage:
- legacy `enableDetectionOnStartup` remains readable but disappears on save;
- persistent game/Companion monitor IDs round-trip;
- legacy GDI device names remain compatible.

Report Passed / Failed / Skipped.

---

## 5. Persistent monitor selection policy

```powershell
dotnet test tests/GameTouchCompanion.Core.Tests/GameTouchCompanion.Core.Tests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~MonitorSelectionServiceTests"
```

Required: 0 failed.

Important assertions:
- a stable monitor ID survives `DISPLAY1/2/3` renumbering;
- once a persistent ID exists, a reused `DISPLAYn` alias is never accepted as that physical display;
- legacy `DISPLAYn` references still resolve for one-time migration;
- same-display safety rules remain intact.

Report Passed / Failed / Skipped.

---

## 6. Native monitor mapping

```powershell
dotnet test tests/GameTouchCompanion.Native.Tests/GameTouchCompanion.Native.Tests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~MonitorProfileMapperTests"
```

Required: 0 failed.

Report Passed / Failed / Skipped.

Then run the real Windows monitor enumeration test:

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~Win32MonitorServiceTests"
```

Required: 0 failed.

For every connected display, confirm:
- DeviceName exists;
- IdentityKey exists;
- any available StableId is not the transient `\\.\DISPLAYn` GDI alias.

Do **not** change monitor topology during this command.

---

## 7. Profile monitor migration regression

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~ProfilesTests"
```

Required: 0 failed.

Important coverage:
- new profile monitor choices persist the stable identity;
- a disconnected saved identity is preserved;
- legacy `DISPLAYn` profile references migrate when resolvable;
- migration never overwrites a dirty profile draft.

Report Passed / Failed / Skipped.

---

## 8. Monitor topology/review regression

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~MonitorSelectionReviewTests"
```

Required: 0 failed.

Important coverage:
- reconnecting the same physical monitor clears review even if its `DISPLAYn` changed;
- a missing persistent target cannot silently become another monitor;
- legacy global selections require one explicit confirmation before persistent IDs are saved.

Report Passed / Failed / Skipped.

---

## 9. Touch-toolbar preference regression

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~BrowserViewModelTests"
```

Required: 0 failed.

Important coverage:
- runtime Show/Hide is session-only;
- the saved startup-toolbar preference is independent;
- opening a new Companion can reset runtime visibility to the saved preference;
- browser settings saves remain serialized safely.

Report Passed / Failed / Skipped.

---

## 10. Localization

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~LocalizationTests"
```

Required: 0 failed.

Confirm Spanish and English resources have identical keys and valid format placeholders.

---

## 11. Targeted BrowserRuntime UX/shell validation

Only if WebView2 Runtime is available:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~LanguageStartupTests.FirstChoicePersistsAndBothLanguagesRenderWithoutReinitializingSession|FullyQualifiedName~ShellLifecycleTests.HiddenStartupRestoreCloseToTrayExitAndSessionEndAreDistinct"

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS -ErrorAction SilentlyContinue
```

Required:
- no Detection tab;
- no old standalone Save-language button;
- one General Settings save button is present;
- detection remains active;
- tray contains Open, Rearm auto-launch and Exit;
- language changes live without recreating Companion/session;
- closing Settings to tray does not stop detection.

Report Passed / Failed / Skipped.

---

## 12. Targeted automatic-opening regression

Only if step 11 passes:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~AutomaticOpeningTests.DetectionAlwaysStartsWithoutOpeningCompanionWhenNoGameIsRunning|FullyQualifiedName~AutomaticOpeningTests.ClosingCompanionDismissesCurrentInstanceButRestartedGameCanOpenAgain|FullyQualifiedName~AutomaticOpeningTests.AutomaticPathRejectsUnsafeStatesAndOpensRealCompanionWithoutChangingBrowserPreferences"

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS -ErrorAction SilentlyContinue
```

Required:
- detection is always active;
- closing Companion dismisses only the current game process instance;
- restarting the game is eligible again;
- monitor/profile safety checks remain enforced;
- browser preferences are not modified by automatic opening.

Report Passed / Failed / Skipped.

---

## 13. Full normal suite

Only if all previous required steps pass:

```powershell
dotnet test -c Release -p:Platform=x64 --no-build
```

Report:

- Passed
- Failed
- Skipped
- Duration

Existing intentional hardware/WebView2 skips are acceptable.
Any failed test is not acceptable.

---

## 14. Full BrowserRuntime suite

Only if the normal suite passes and WebView2 Runtime is available:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "Category=BrowserRuntime"

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS -ErrorAction SilentlyContinue
```

Report Passed / Failed / Skipped.

---

## 15. Portable

Only if all tests pass:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\tools\Publish-Portable.ps1
```

Then validate the generated folder:

```powershell
.\tools\Test-Portable.ps1 -Folder "<GENERATED_PORTABLE_FOLDER>"
```

Required:
- PASS
- no user JSON/logs included
- existing TouchTestPage files are present.

Do not commit `artifacts/`.

---

## 16. Git/static validation

```powershell
git diff --check
git status
```

Report actual whitespace errors separately from normal LF/CRLF notices.

---

## Final report

Return exactly:

### Build
PASS / FAIL
Warnings:
Errors:

### GameDetectionTests
Passed:
Failed:
Skipped:

### JsonApplicationSettingsStoreTests
Passed:
Failed:
Skipped:

### MonitorSelectionServiceTests
Passed:
Failed:
Skipped:

### MonitorProfileMapperTests
Passed:
Failed:
Skipped:

### Win32MonitorServiceTests
Passed:
Failed:
Skipped:

### ProfilesTests
Passed:
Failed:
Skipped:

### MonitorSelectionReviewTests
Passed:
Failed:
Skipped:

### BrowserViewModelTests
Passed:
Failed:
Skipped:

### LocalizationTests
Passed:
Failed:
Skipped:

### Targeted BrowserRuntime UX/Shell
PASS / FAIL / BLOCKED
Passed:
Failed:
Skipped:

### Targeted Automatic Opening
PASS / FAIL / BLOCKED / NOT RUN
Passed:
Failed:
Skipped:

### Full normal suite
PASS / FAIL / NOT RUN
Passed:
Failed:
Skipped:

### Full BrowserRuntime
PASS / FAIL / BLOCKED / NOT RUN
Passed:
Failed:
Skipped:

### Portable
PASS / FAIL / NOT RUN

### Git
Branch:
Diff check:
Status:

### Code changes by Codex
None

Do not commit.
Do not push.
Do not modify code.
