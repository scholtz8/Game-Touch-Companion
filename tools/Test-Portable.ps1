[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference = 'Stop'
$packageRoot = (Resolve-Path -LiteralPath $Folder).Path.TrimEnd('\')
$prefix = $packageRoot + '\'
$manifest = Join-Path $packageRoot 'SHA256SUMS.txt'
$expected = @{}
foreach ($line in Get-Content -LiteralPath $manifest -Encoding UTF8) {
    if ($line -notmatch '^([A-Fa-f0-9]{64})  (.+)$') { throw 'Invalid manifest line.' }
    $hash = $Matches[1]; $relative = $Matches[2]
    $target = [IO.Path]::GetFullPath((Join-Path $packageRoot $relative))
    if (!$target.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Manifest path outside package.' }
    if ($expected.ContainsKey($target)) { throw 'Duplicate manifest path.' }
    $expected[$target] = $true
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $hash) { throw "Hash mismatch: $relative" }
}
foreach ($file in Get-ChildItem -LiteralPath $packageRoot -Recurse -File) {
    if ($file.FullName -ne $manifest -and !$expected.ContainsKey($file.FullName)) { throw "Unlisted file: $($file.Name)" }
    if ($file.Name -in @('profiles.json','browser.json','settings.json') -or $file.Extension -eq '.log') { throw 'User data found in package.' }
}
foreach ($name in @('GameTouchCompanion.App.exe','coreclr.dll','hostfxr.dll','hostpolicy.dll')) {
    $bytes = [IO.File]::ReadAllBytes((Join-Path $packageRoot $name))
    $pe = [BitConverter]::ToInt32($bytes, 0x3c)
    if ([BitConverter]::ToUInt32($bytes, $pe) -ne 0x4550 -or [BitConverter]::ToUInt16($bytes, $pe + 4) -ne 0x8664) { throw "Not an AMD64 PE: $name" }
}
$loader = Join-Path $packageRoot 'WebView2Loader.dll'
if (!(Test-Path -LiteralPath $loader)) { $loader = Join-Path $packageRoot 'runtimes/win-x64/native/WebView2Loader.dll' }
$bytes = [IO.File]::ReadAllBytes($loader)
$pe = [BitConverter]::ToInt32($bytes, 0x3c)
if ([BitConverter]::ToUInt16($bytes, $pe + 4) -ne 0x8664) { throw 'WebView2 loader is not AMD64.' }
$runtime = Get-Content -Raw -LiteralPath (Join-Path $packageRoot 'GameTouchCompanion.App.runtimeconfig.json') | ConvertFrom-Json
if (!$runtime.runtimeOptions.includedFrameworks -or $runtime.runtimeOptions.framework -or $runtime.runtimeOptions.frameworks) { throw 'Not self-contained.' }
foreach ($name in @('TouchTestPage/index.html','TouchTestPage/second.html','PORTABLE.md','PresentationFramework.dll','Microsoft.Web.WebView2.Wpf.dll')) {
    if (!(Test-Path -LiteralPath (Join-Path $packageRoot $name) -PathType Leaf)) { throw "Missing: $name" }
}
Write-Host "PASS: $($expected.Count) hashes, no unlisted files/user JSON/logs, AMD64 app/runtime/loader, self-contained config and required content."
Write-Host 'This test does not launch the app or validate hardware/WebView2 on a clean machine.'
