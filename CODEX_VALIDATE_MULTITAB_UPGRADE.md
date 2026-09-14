# Codex — Validation only for Multi-Tab Upgrade

## Scope

Do **not** redesign, refactor, or implement new features.

ChatGPT already implemented the upgrade. Your job is only to execute validation commands on the current working tree and report exact results.

Do not commit or push.

Do not modify source code automatically if a test/build fails. Stop and report the failure so it can be analyzed before any code change.

## 1. Inspect environment

From the repository root run:

```powershell
git status
dotnet --info
dotnet --version
```

Record the current branch and whether the tree already contains local changes.

## 2. Restore

Run:

```powershell
dotnet restore
```

Report:

- PASS or FAIL
- warnings
- errors

If restore fails, stop and report the full relevant error output. Do not change package versions.

## 3. Release build

Run:

```powershell
dotnet build -c Release -p:Platform=x64 --no-restore
```

Required result:

- 0 compile errors

Report warning count as well.

If build fails, stop. Do not edit code. Save the complete build output to:

```text
artifacts/validation/multitab-build.txt
```

and report the exact files/lines/errors.

## 4. Normal automated tests

Only if build passes, run:

```powershell
dotnet test -c Release -p:Platform=x64 --no-build
```

Report:

- total
- passed
- failed
- skipped
- duration

Intentional hardware/browser skips are acceptable. Failed tests are not acceptable.

If any test fails, do not modify code. Save the complete output to:

```text
artifacts/validation/multitab-tests.txt
```

and report every failing test and stack trace.

## 5. WebView2 desktop smoke tests

Only if the normal tests pass and WebView2 Runtime is installed:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter 'Category=BrowserRuntime'

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS
```

Report:

- passed
- failed
- skipped

Do not interpret a missing WebView2 Runtime as a code failure; report it separately.

## 6. Specific upgrade checks

Confirm from test results that these cases are covered and passing:

1. Existing schemaVersion 1 profiles migrate to schemaVersion 2.
2. Legacy URL becomes the primary tab.
3. Multiple tabs persist.
4. Tab order persists.
5. Primary tab persists.
6. Existing profile save/apply/delete behavior still passes.
7. Automatic game opening tests still pass.
8. Localization ES/EN resource tests still pass.
9. WebView2 smoke can create the Companion browser tab.

Do not add tests in this execution-only task.

## 7. Static repository check

Run:

```powershell
git diff --check
git status
```

Report trailing-whitespace/errors if any.

## 8. Do not run physical focus tests automatically

The following require the user and real hardware, so report them as:

```text
MANUAL HARDWARE TEST REQUIRED
```

- two displays;
- real touchscreen;
- game in foreground;
- tab switching by touch;
- profile switching by touch;
- temporary tab creation/close;
- 50+ touch interactions;
- zero unexpected game foreground loss.

## 9. Final report

Return exactly:

### Environment
- OS:
- .NET SDK:
- WebView2 Runtime:

### Restore
PASS / FAIL

### Build
PASS / FAIL
Warnings:
Errors:

### Normal tests
PASS / FAIL
Passed:
Failed:
Skipped:

### BrowserRuntime tests
PASS / FAIL / BLOCKED / NOT RUN
Passed:
Failed:
Skipped:

### Upgrade validation
- schema v1 -> v2 migration:
- multi-tab persistence:
- primary tab:
- tab ordering:
- automatic opening:
- localization:
- WebView2:

### Manual tests still required
List them.

### Git
- branch:
- status:

### Files produced
List validation logs only.

Do not make a commit.
Do not push.
Do not modify code in response to a failure.
