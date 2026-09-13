[CmdletBinding()]
param()

$ErrorActionPreference = 'Continue'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path $repo 'artifacts\diagnostics'
New-Item -ItemType Directory -Force -Path $output | Out-Null

Get-ComputerInfo -Property WindowsProductName,WindowsVersion,OsBuildNumber,OsArchitecture |
    Out-File (Join-Path $output 'system.txt') -Encoding utf8
dotnet --info 2>&1 | Out-File (Join-Path $output 'dotnet-info.txt') -Encoding utf8
git -C $repo status --short --branch 2>&1 | Out-File (Join-Path $output 'git-status.txt') -Encoding utf8
git -C $repo diff --no-ext-diff 2>&1 | Out-File (Join-Path $output 'git-diff.txt') -Encoding utf8

$testResult = Join-Path $repo 'artifacts\test-results\latest.txt'
if (Test-Path -LiteralPath $testResult) { Copy-Item -LiteralPath $testResult -Destination (Join-Path $output 'test-results.txt') -Force }
else { 'NOT RUN or no saved result' | Out-File (Join-Path $output 'test-results.txt') -Encoding utf8 }

$logFolder = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'GameTouchCompanion\Logs'
$appLog = Get-ChildItem -LiteralPath $logFolder -Filter 'app-*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$focusLog = Get-ChildItem -LiteralPath $logFolder -Filter 'focus-probe-*.log' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($appLog) { Copy-Item -LiteralPath $appLog.FullName -Destination (Join-Path $output 'app-log.txt') -Force } else { 'No app log found' | Out-File (Join-Path $output 'app-log.txt') }
if ($focusLog) { Copy-Item -LiteralPath $focusLog.FullName -Destination (Join-Path $output 'foreground-log.txt') -Force } else { 'No foreground log found' | Out-File (Join-Path $output 'foreground-log.txt') }

try {
    Get-CimInstance Win32_DesktopMonitor -ErrorAction Stop | Select-Object Name,DeviceID,ScreenWidth,ScreenHeight,Status |
        Format-List | Out-File (Join-Path $output 'monitors.txt') -Encoding utf8
}
catch {
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.Screen]::AllScreens | Select-Object DeviceName,Bounds,WorkingArea,Primary |
        Format-List | Out-File (Join-Path $output 'monitors.txt') -Encoding utf8
}
Write-Host "Diagnostics written to $output"
