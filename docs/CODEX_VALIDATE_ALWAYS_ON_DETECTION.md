# Codex — Validate always-on automatic detection

## Scope

Execution/validation only.

Do **not** modify source code.
Do **not** refactor.
Do **not** commit.
Do **not** push.

If any required step fails, stop and return the complete output. Do not fix the code.

---

## 1. Baseline

From the repository root:

```powershell
git branch --show-current
git status
dotnet --version
```

Record the branch and pre-existing working-tree changes.

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

## 3. Core detection-state tests

```powershell
dotnet test tests/GameTouchCompanion.Core.Tests/GameTouchCompanion.Core.Tests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~GameDetectionTests"
```

Required: 0 failed.

Important coverage:
- attended/dismissed identity uses executable + PID + process start time;
- changing profile does not make the same process instance auto-open again;
- restarting the same executable creates a new eligible instance;
- dismissing one game does not block another configured game;
- ended instances are pruned from in-memory state.

Report Passed / Failed / Skipped.

---

## 4. Application-settings migration test

```powershell
dotnet test tests/GameTouchCompanion.Core.Tests/GameTouchCompanion.Core.Tests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~JsonApplicationSettingsStoreTests"
```

Required: 0 failed.

Confirm the legacy `enableDetectionOnStartup` JSON property:
- is tolerated when reading old settings;
- is no longer represented by ApplicationSettings;
- disappears on the next settings save.

Report Passed / Failed / Skipped.

---

## 5. Profiles regression tests

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~ProfilesTests"
```

Required: 0 failed.

Report Passed / Failed / Skipped.

---

## 6. Targeted BrowserRuntime: always-on detection

Only if WebView2 Runtime is available:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~AutomaticOpeningTests.DetectionAlwaysStartsWithoutOpeningCompanionWhenNoGameIsRunning|FullyQualifiedName~AutomaticOpeningTests.ClosingCompanionDismissesCurrentInstanceButRestartedGameCanOpenAgain"

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS -ErrorAction SilentlyContinue
```

Required:

- detection starts automatically without a session checkbox;
- no Companion opens when no configured game is running;
- after auto-opening a game, manually closing Companion does **not** stop detection;
- the same game instance does not immediately reopen Companion;
- a restarted game instance can auto-open Companion again.

Report Passed / Failed / Skipped.

If this step fails, stop.

---

## 7. Shell/tray BrowserRuntime regression

Only if step 6 passes:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~ShellLifecycleTests|FullyQualifiedName~LanguageStartupTests"

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS -ErrorAction SilentlyContinue
```

Required:
- detection remains active while Settings is hidden to tray;
- tray no longer contains a detection pause/resume command;
- tray still contains Open, Rearm/Reset auto-launch, and Exit;
- closing the full application disposes the tray/detection lifecycle normally;
- live ES/EN switching remains valid.

Report Passed / Failed / Skipped.

---

## 8. Existing automatic-opening safety regression

Only if previous BrowserRuntime steps pass:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~AutomaticOpeningTests.AutomaticPathRejectsUnsafeStatesAndOpensRealCompanionWithoutChangingBrowserPreferences"

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS -ErrorAction SilentlyContinue
```

Required: PASS.

This verifies that always-on detection did not bypass the existing monitor/profile/foreground safety checks.

---

## 9. Full normal suite

Only if all previous required steps pass:

```powershell
dotnet test -c Release -p:Platform=x64 --no-build
```

Report:
- Passed
- Failed
- Skipped
- Duration

Failed tests are not acceptable.
Existing intentional hardware/WebView2 skips are acceptable.

---

## 10. Full BrowserRuntime suite

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

## 11. Static Git validation

```powershell
git diff --check
git status
```

Report LF/CRLF notices separately from actual whitespace errors.

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

### ProfilesTests
Passed:
Failed:
Skipped:

### Always-on BrowserRuntime
PASS / FAIL / BLOCKED
Passed:
Failed:
Skipped:

### Shell/Tray BrowserRuntime
PASS / FAIL / BLOCKED / NOT RUN
Passed:
Failed:
Skipped:

### Automatic-opening safety regression
PASS / FAIL / BLOCKED / NOT RUN

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

### Git
Branch:
Diff check:
Status:

### Code changes by Codex
None

Do not commit.
Do not push.
Do not modify code.
