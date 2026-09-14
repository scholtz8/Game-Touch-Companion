# Codex — Validate Tab Close + Keyboard Size UX

## Scope

Execution only.

Do not modify source code.
Do not refactor.
Do not commit.
Do not push.

ChatGPT already implemented the change.

## 1. Build

From the repository root:

```powershell
dotnet build -c Release -p:Platform=x64
```

Required:
- PASS
- 0 errors

Report warnings.

If build fails, STOP and return the complete compiler/XAML error. Do not fix it.

## 2. Localization tests

```powershell
dotnet test tests/GameTouchCompanion.IntegrationTests/GameTouchCompanion.IntegrationTests.csproj `
  -c Release `
  -p:Platform=x64 `
  --no-build `
  --filter "FullyQualifiedName~LocalizationTests|FullyQualifiedName~LanguageStartupTests"
```

Required:
- 0 failed

This verifies the new `Ui172` resource is consistent in ES/EN.

## 3. Full normal suite

Only if steps 1–2 pass:

```powershell
dotnet test -c Release -p:Platform=x64 --no-build
```

Report:
- Passed
- Failed
- Skipped

No BrowserRuntime run is required for this change because WebView2 navigation/bridge logic was not changed.

## 4. Git checks

```powershell
git diff --check
git status
```

Report whitespace issues and current branch/status.

## Final report

Return:

### Build
PASS / FAIL
Warnings:
Errors:

### Localization
Passed:
Failed:
Skipped:

### Full normal suite
PASS / FAIL / NOT RUN
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
