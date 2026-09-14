[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$releaseRoot = Join-Path $repo 'artifacts/releases'
$releaseName = 'GameTouchCompanion-win-x64-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$output = Join-Path $releaseRoot $releaseName
New-Item -ItemType Directory -Path $output | Out-Null
$project = Join-Path $repo 'src/GameTouchCompanion.App/GameTouchCompanion.App.csproj'
& dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishProfile=Portable -o $output
if ($LASTEXITCODE -ne 0) { throw "Publish failed; partial output retained at $output. No ZIP created." }
foreach ($relative in @('GameTouchCompanion.App.exe','GameTouchCompanion.App.dll','GameTouchCompanion.App.runtimeconfig.json',
    'coreclr.dll','hostfxr.dll','hostpolicy.dll','PresentationFramework.dll','Microsoft.Web.WebView2.Wpf.dll',
    'TouchTestPage/index.html','TouchTestPage/second.html','TouchTestPage/blank.html')) {
    if (!(Test-Path -LiteralPath (Join-Path $output $relative) -PathType Leaf)) { throw "Missing published file: $relative" }
}
if (!(Get-ChildItem -LiteralPath $output -Recurse -Filter WebView2Loader.dll)) { throw 'WebView2Loader.dll missing.' }
$runtime = Get-Content -Raw -LiteralPath (Join-Path $output 'GameTouchCompanion.App.runtimeconfig.json') | ConvertFrom-Json
if (!$runtime.runtimeOptions.includedFrameworks -or $runtime.runtimeOptions.framework -or $runtime.runtimeOptions.frameworks) {
    throw 'Runtime configuration is not self-contained.'
}
Copy-Item -LiteralPath (Join-Path $repo 'docs/PORTABLE.md') -Destination (Join-Path $output 'PORTABLE.md')
$hashLines = Get-ChildItem -LiteralPath $output -File -Recurse | Sort-Object FullName | ForEach-Object {
    $relative = $_.FullName.Substring($output.Length + 1).Replace('\','/')
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash, $relative
}
$hashLines | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding UTF8
$zip = Join-Path $releaseRoot ($releaseName + '.zip')
Compress-Archive -LiteralPath $output -DestinationPath $zip -CompressionLevel Optimal
('{0}  {1}' -f (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash, [IO.Path]::GetFileName($zip)) |
    Set-Content -LiteralPath ($zip + '.sha256') -Encoding ASCII
Write-Host "Portable folder: $output"
Write-Host "ZIP: $zip"
Write-Host 'Structural checks passed; extracted desktop/hardware validation still required.'
