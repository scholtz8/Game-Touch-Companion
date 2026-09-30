# Codex — Validate Touch Browser UX Upgrade

## Scope

Execution only.

Do **not** modify source code.
Do **not** refactor.
Do **not** commit.
Do **not** push.

ChatGPT already implemented the upgrade. If any command fails, stop and return the complete error output.

---

## 1. Environment

From the repository root:

```powershell
git status
git branch --show-current
dotnet --version
```

Record the current branch and working-tree state.

---

## 2. Restore and build

```powershell
dotnet restore

dotnet build -c Release -p:Platform=x64 --no-restore
```

Required:

- Build PASS
- 0 compile errors

Warnings must be reported.

If build fails, stop and return the exact compiler errors and line numbers. Do not fix them.

---

## 3. Core URL-policy tests

Run:

```powershell
dotnet test tests/GameTouchCompanion.Core.Tests/GameTouchCompanion.Core.Tests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~BrowserUrlPolicyTests"
```

Confirm that the packaged blank page URL is accepted and the existing unsafe URL cases remain rejected.

Report Passed / Failed / Skipped.

---

## 4. Profiles regression tests

Run:

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~ProfilesTests"
```

This must confirm that removing the editable tab-name field from the UI did not break profile persistence/migration.

Report Passed / Failed / Skipped.

---

## 5. Targeted WebView2 runtime test

Only if WebView2 Runtime is installed:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~BrowserRuntimeTests.LocalNavigationErrorsAndLifecycleWorkWithRealWebView2"

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS -ErrorAction SilentlyContinue
```

Required assertions include:

- WebView2 initializes.
- Host objects remain disabled.
- Web messages are enabled for the token-validated touch-keyboard bridge.
- `window.__gtcKeyboard` exists in the local test page.
- Normal browser lifecycle/navigation still passes.

If it fails, stop and return the complete assertion/exception/stack trace. Do not modify code.

---

## 6. Full normal suite

Only if previous steps pass:

```powershell
dotnet test -c Release -p:Platform=x64 --no-build
```

Report:

- Total
- Passed
- Failed
- Skipped
- Duration

Intentional hardware/WebView2-gated skips are acceptable.
Failed tests are not acceptable.

---

## 7. Full BrowserRuntime suite

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

## 8. Portable structural validation

Only if all tests pass:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

.\tools\Publish-Portable.ps1
```

Use the generated portable folder with:

```powershell
.\tools\Test-Portable.ps1 -Folder "<GENERATED_PORTABLE_FOLDER>"
```

Confirm that `TouchTestPage/blank.html` is included alongside `index.html` and `second.html`.

Do not commit generated `artifacts/`.

---

## 9. Static Git check

```powershell
git diff --check
git status
```

Report any whitespace errors.

---

## Final report

Return exactly:

### Build
PASS / FAIL
Warnings:
Errors:

### BrowserUrlPolicyTests
Passed:
Failed:
Skipped:

### ProfilesTests
Passed:
Failed:
Skipped:

### Targeted BrowserRuntime
PASS / FAIL / BLOCKED
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
Blank page packaged: YES / NO

### Git
Branch:
Status:
Diff check:

### Code changes by Codex
None

Do not commit.
Do not push.
Do not modify code.
