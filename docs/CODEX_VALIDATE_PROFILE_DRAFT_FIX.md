# Codex — Validation of Profile Draft Fix

## Scope

This is an **execution-only** task.

Do not redesign, refactor, commit, push, or modify source code automatically.

The fix has already been implemented by ChatGPT. Your job is only to run the commands below and report exact results.

If anything fails, stop and report the failure verbatim.

---

## 1. Confirm environment

From the repository root:

```powershell
git status
dotnet --version
```

Report the current branch and whether the working tree contains the expected local changes.

---

## 2. Restore and build

```powershell
dotnet restore

dotnet build -c Release -p:Platform=x64 --no-restore
```

Required:

- build PASS
- 0 errors

If build fails, do not fix it. Return the exact compiler errors.

---

## 3. Run only ProfilesTests first

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~ProfilesTests"
```

The four previously failing tests must now pass:

```text
ProfilesTests.NewDraftAndFailedSaveKeepPendingChanges
ProfilesTests.SwitchingWithDirtyDraftRequiresExplicitDiscardOrCancel
ProfilesTests.ProfileEditorPersistsMultipleTabsOrderAndPrimarySelection
ProfilesTests.FailedStoragePreservesSavedVersionAndDraftAndBlocksConcurrentEdits
```

Also confirm the new regression test passes:

```text
ProfilesTests.InitializeCreatesCleanDefaultTabDraft
```

If any ProfilesTests test fails:

- STOP.
- Do not edit code.
- Return the full failing test name, assertion/exception, line and stack trace.

---

## 4. Run the full normal test suite

Only if the targeted ProfilesTests pass:

```powershell
dotnet test -c Release -p:Platform=x64 --no-build
```

Report:

- Total
- Passed
- Failed
- Skipped
- Duration

Intentional hardware/WebView2 skips are acceptable.
Any failed test is not acceptable.

If there is any failure, do not fix it. Return the exact failure.

---

## 5. Optional BrowserRuntime smoke

Only if the full normal suite passes and WebView2 Runtime is available:

```powershell
$env:GTC_RUN_WEBVIEW_TESTS = '1'

dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter 'Category=BrowserRuntime'

Remove-Item Env:\GTC_RUN_WEBVIEW_TESTS
```

Report PASS / FAIL / BLOCKED.

---

## 6. Final report

Return only:

### Build
PASS / FAIL
Warnings:
Errors:

### ProfilesTests
PASS / FAIL
Passed:
Failed:
Skipped:

### Full tests
PASS / FAIL / NOT RUN
Passed:
Failed:
Skipped:

### BrowserRuntime
PASS / FAIL / BLOCKED / NOT RUN

### Previously failing tests
- NewDraftAndFailedSaveKeepPendingChanges:
- SwitchingWithDirtyDraftRequiresExplicitDiscardOrCancel:
- ProfileEditorPersistsMultipleTabsOrderAndPrimarySelection:
- FailedStoragePreservesSavedVersionAndDraftAndBlocksConcurrentEdits:

### New regression test
- InitializeCreatesCleanDefaultTabDraft:

### Git
Branch:
Status:

Do not commit.
Do not push.
Do not modify code.
