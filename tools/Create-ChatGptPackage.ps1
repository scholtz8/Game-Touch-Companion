[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$RepositoryRoot = (Resolve-Path $RepositoryRoot).Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $RepositoryRoot 'artifacts/chatgpt-handoff'
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$StagingDirectory = Join-Path $OutputDirectory 'GameTouchCompanion_Current'
$ZipPath = Join-Path $OutputDirectory 'GameTouchCompanion_Current.zip'

if (Test-Path -LiteralPath $StagingDirectory) { Remove-Item -LiteralPath $StagingDirectory -Recurse -Force }
if (Test-Path -LiteralPath $ZipPath) { Remove-Item -LiteralPath $ZipPath -Force }
New-Item -ItemType Directory -Path $StagingDirectory -Force | Out-Null

$excludedDirectories = @(
    '.git', '.vs', '.idea', '.vscode', 'bin', 'obj', 'artifacts', 'publish',
    'TestResults', 'coverage', '.dotnet', '.dotnet-cli', '.nuget', 'packages',
    'EBWebView', 'WebView2', 'User Data', 'Cache', 'Code Cache', 'GPUCache'
)
$excludedFileNames = @(
    'settings.json', 'browser.json', 'profiles.json', 'language.json', 'appearance.json',
    'GPT_HANDOFF.generated.md', 'README_EN.md'
)
$excludedExtensions = @('.zip', '.7z', '.rar', '.log', '.dmp', '.mdmp', '.etl', '.trace', '.tmp', '.temp', '.bak', '.old', '.pfx', '.p12', '.key', '.pem')

function Test-IncludedPath([object]$Item) {
    $relative = Get-RelativePath $Item.FullName
    $parts = $relative -split '[\\/]'
    foreach ($part in $parts) {
        if ($excludedDirectories -contains $part) { return $false }
    }
    if ($Item.Name -in $excludedFileNames) { return $false }
    if ($Item -is [IO.FileInfo] -and $excludedExtensions -contains $Item.Extension.ToLowerInvariant()) { return $false }
    if ($Item.Name -match '^(\.env)(\..*)?$' -or $Item.Name -in @('secrets.json', 'credentials.json')) { return $false }
    return $true
}

function Get-RelativePath([string]$Path) {
    $rootUri = [Uri]::new(($RepositoryRoot.TrimEnd('\\') + '\\'))
    $pathUri = [Uri]::new($Path)
    return [Uri]::UnescapeDataString($rootUri.MakeRelativeUri($pathUri).ToString()).Replace('/', '\\')
}

$rootFiles = @(
    '.gitignore', 'README.md', 'README.es.md', 'CHANGELOG.md', 'LICENSE',
    'GameTouchCompanion.sln', 'Directory.Build.props', 'Directory.Packages.props',
    'global.json', 'GPT_HANDOFF.md', 'PROJECT_STATE.md', 'CONTRIBUTING.md', 'SECURITY.md'
)
foreach ($relativePath in $rootFiles) {
    $source = Join-Path $RepositoryRoot $relativePath
    if (Test-Path -LiteralPath $source -PathType Leaf) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $StagingDirectory $relativePath) -Force
    }
}

foreach ($directoryName in @('src', 'tests', 'tools', 'docs', 'assets', '.github')) {
    $sourceDirectory = Join-Path $RepositoryRoot $directoryName
    if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) { continue }
    Copy-Item -LiteralPath $sourceDirectory -Destination $StagingDirectory -Recurse -Force
}

foreach ($item in @(Get-ChildItem -LiteralPath $StagingDirectory -Recurse -Force | Sort-Object FullName -Descending)) {
    if ($item.Name -in $excludedFileNames -or
        $excludedExtensions -contains $item.Extension.ToLowerInvariant() -or
        $item.Name -match '^(\.env)(\..*)?$' -or
        $item.Name -in @('secrets.json', 'credentials.json') -or
        (($item.PSIsContainer) -and ($item.Name -in $excludedDirectories))) {
        Remove-Item -LiteralPath $item.FullName -Recurse -Force
    }
}

$secretPattern = 'BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY|\bapi[_-]?key\b\s*[:=]|\bclient[_-]?secret\b\s*[:=]|\bpassword\b\s*[:=]|\btoken\b\s*[:=]'
$secretHits = Get-ChildItem -LiteralPath $StagingDirectory -Recurse -File -Force |
    Where-Object { $_.Extension -notin @('.png', '.ico', '.dll', '.exe', '.pdb') } |
    Select-String -Pattern $secretPattern -CaseSensitive:$false -ErrorAction SilentlyContinue
if ($secretHits) { throw 'Potential secret pattern found in the package; package creation stopped.' }

Compress-Archive -Path (Join-Path $StagingDirectory '*') -DestinationPath $ZipPath -CompressionLevel Optimal
$files = @(Get-ChildItem -LiteralPath $StagingDirectory -Recurse -File -Force)
$rootFolders = @(Get-ChildItem -LiteralPath $StagingDirectory -Directory -Force | Select-Object -ExpandProperty Name)
[PSCustomObject]@{
    ZipPath = $ZipPath
    SizeBytes = (Get-Item -LiteralPath $ZipPath).Length
    FileCount = $files.Count
    RootFolders = ($rootFolders -join ', ')
}
