# Codex — Validate Runtime Test Fix 2

## Important

Execution only.

Do not modify source code.
Do not refactor.
Do not commit.
Do not push.

If anything fails, return the complete failing test output to the user.

## 1. Build

From the repository root:

```powershell
dotnet build -c Release -p:Platform=x64
```

Report PASS/FAIL and warnings/errors.

## 2. Re-run the profile tests

Confirm the previous profile-draft fix is still green:

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~ProfilesTests"
```

Report Passed / Failed / Skipped.

## 3. Run only the two BrowserRuntime tests involved in this fix

Enable the opt-in WebView2 tests:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'
```

Then run:

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~BrowserRuntimeTests.LocalNavigationErrorsAndLifecycleWorkWithRealWebView2|FullyQualifiedName~AutomaticOpeningTests.AutomaticPathRejectsUnsafeStatesAndOpensRealCompanionWithoutChangingBrowserPreferences"
```

Then always clear the variable:

```powershell
Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS -ErrorAction SilentlyContinue
```

Expected:

```text
LocalNavigationErrorsAndLifecycleWorkWithRealWebView2: PASS
AutomaticPathRejectsUnsafeStatesAndOpensRealCompanionWithoutChangingBrowserPreferences: PASS
```

If either fails:
- STOP.
- Do not change code.
- Return the complete assertion/exception, line number and stack trace.

## 4. Full normal suite

Only if steps 1–3 pass:

```powershell
dotnet test -c Release -p:Platform=x64 --no-build
```

Report:
- Total
- Passed
- Failed
- Skipped
- Duration

## 5. Full BrowserRuntime suite

Only if the normal suite passes:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "Category=BrowserRuntime"

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS -ErrorAction SilentlyContinue
```

Report:
- Passed
- Failed
- Skipped

## Final response format

### Build
PASS / FAIL

### ProfilesTests
Passed:
Failed:
Skipped:

### Targeted BrowserRuntime
- LocalNavigationErrorsAndLifecycleWorkWithRealWebView2:
- AutomaticPathRejectsUnsafeStatesAndOpensRealCompanionWithoutChangingBrowserPreferences:

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

### Code changes
None performed by Codex.

Do not commit.
Do not push.
