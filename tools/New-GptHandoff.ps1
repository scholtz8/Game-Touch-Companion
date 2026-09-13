[CmdletBinding()]
param()

$repo = Split-Path -Parent $PSScriptRoot
$target = Join-Path $repo 'GPT_HANDOFF.generated.md'
$date = Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'
$branch = git -C $repo branch --show-current 2>&1 | Out-String
$commit = git -C $repo rev-parse --verify --quiet HEAD 2>$null | Out-String
if ($LASTEXITCODE -ne 0) { $commit = 'N/A - repository has no commits.' }
$status = git -C $repo status --short --branch 2>&1 | Out-String
$version = dotnet --version 2>&1 | Out-String
$info = dotnet --info 2>&1 | Out-String
$modified = git -C $repo status --short 2>&1 | Out-String
$stat = git -C $repo diff --stat 2>&1 | Out-String
$latestTest = Join-Path $repo 'artifacts\test-results\latest.txt'
$tests = if (Test-Path -LiteralPath $latestTest) { Get-Content -Raw -Encoding UTF8 -LiteralPath $latestTest } else { 'NOT RUN or no saved result' }

$content = @"
# GPT HANDOFF (GENERATED - merge with GPT_HANDOFF.md; does not overwrite manual analysis)

Generated: $date

## Git

Branch: $branch
Commit: $commit
Working tree:
``````text
$status
``````

## .NET

Version: $version
``````text
$info
``````

## Tests

``````text
$tests
``````

## Modified files

``````text
$modified
``````

## Diff stat

``````text
$stat
``````
"@
Set-Content -LiteralPath $target -Value $content -Encoding utf8
Write-Host "Generated $target. Manual GPT_HANDOFF.md was not overwritten."
