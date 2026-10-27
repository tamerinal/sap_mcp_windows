[CmdletBinding()]
param([Parameter(Mandatory)][string]$AdtSource)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskSource = [IO.Path]::GetFullPath($AdtSource)
$taskPayload = Join-Path $taskRoot 'payload'
$taskCatalog = Get-Content -Raw -LiteralPath (Join-Path $taskPayload 'catalog.json') | ConvertFrom-Json -AsHashtable
$taskExisting = @($taskCatalog.assets | Where-Object { $_.id -ne 'adt' })
foreach ($taskAsset in $taskExisting) {
    $taskFile = @(Join-Path $taskPayload ('packages/' + $taskAsset.fileName); Join-Path $taskPayload ('cache/' + $taskAsset.fileName)) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (!$taskFile -or (Get-FileHash -LiteralPath $taskFile -Algorithm SHA256).Hash -ne $taskAsset.sha256) { throw "Existing pinned asset unavailable or changed: $($taskAsset.id)" }
}
$taskStage = Join-Path $taskRoot ('artifacts/adt-payload-' + [guid]::NewGuid().ToString('N'))
$taskBuild = Join-Path $taskStage 'build'; $taskOutput = Join-Path $taskStage 'package'
New-Item -ItemType Directory -Force -Path $taskBuild,$taskOutput | Out-Null
foreach ($taskName in @('src','package.json','package-lock.json','tsconfig.json')) { Copy-Item -LiteralPath (Join-Path $taskSource $taskName) -Destination $taskBuild -Recurse }
Push-Location $taskBuild
try {
    & npm.cmd ci --ignore-scripts --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'ADT dependency restore failed' }
    & npm.cmd run build
    if ($LASTEXITCODE -ne 0) { throw 'ADT build failed' }
    & npm.cmd prune --omit=dev --ignore-scripts --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw 'ADT production prune failed' }
} finally { Pop-Location }
foreach ($taskName in @('dist','node_modules','package.json','package-lock.json')) { Copy-Item -LiteralPath (Join-Path $taskBuild $taskName) -Destination $taskOutput -Recurse }
$taskLicense = Join-Path $taskSource 'LICENSE'
if (!(Test-Path -LiteralPath $taskLicense)) { $taskLicense = Join-Path $taskRoot 'LICENSE' }
Copy-Item -LiteralPath $taskLicense -Destination (Join-Path $taskOutput 'LICENSE')
$taskCommit = (& git -C $taskSource rev-parse HEAD).Trim()
$taskDirty = [bool](& git -C $taskSource status --porcelain)
$taskMetadata = Get-Content -Raw -LiteralPath (Join-Path $taskSource 'package.json') | ConvertFrom-Json
$taskVersion = $taskMetadata.version + '+' + $taskCommit.Substring(0,7) + $(if ($taskDirty) { '.snapshot' } else { '' })
$taskSourceManifest = @(& git -C $taskSource ls-files --cached --others --exclude-standard | Where-Object { $_ -like 'src/*' -or $_ -in @('package.json','package-lock.json','tsconfig.json') } | ForEach-Object { @{file=$_;sha256=(Get-FileHash -LiteralPath (Join-Path $taskSource $_) -Algorithm SHA256).Hash.ToLowerInvariant()} })
@{repository='tamerinal/sap_adt_mcp';commit=$taskCommit;version=$taskVersion;includesLocalChanges=$taskDirty;connectionPolicyVersion=1;distributionAdaptations=@();sourceManifest=$taskSourceManifest} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskOutput 'build-info.json') -Encoding utf8NoBOM
$taskNotices = @(Get-Content -Raw -LiteralPath (Join-Path $taskPayload 'dependency-notices.json') | ConvertFrom-Json -AsHashtable | Where-Object { $_.component -ne 'adt' })
$taskLock = Get-Content -Raw -LiteralPath (Join-Path $taskOutput 'package-lock.json') | ConvertFrom-Json -AsHashtable
foreach ($taskKey in $taskLock.packages.Keys | Where-Object { $_ -like 'node_modules/*' }) {
    $taskDirectory = Join-Path $taskOutput $taskKey
    if (!(Test-Path -LiteralPath (Join-Path $taskDirectory 'package.json'))) { continue }
    $taskDependency = Get-Content -Raw -LiteralPath (Join-Path $taskDirectory 'package.json') | ConvertFrom-Json
    $taskFiles = @(Get-ChildItem -LiteralPath $taskDirectory -File | Where-Object { $_.Name -match '^(LICENSE|LICENCE|NOTICE|COPYING)' } | ForEach-Object { $taskKey + '/' + $_.Name })
    $taskNotices += @{component='adt';name=$taskDependency.name;version=$taskDependency.version;license=$taskDependency.license;noticeFiles=$taskFiles}
}
$taskFileName = 'sap-adt-' + $taskVersion + '.zip'; $taskArchive = Join-Path $taskPayload ('packages/' + $taskFileName)
if (Test-Path -LiteralPath $taskArchive) { throw 'Immutable ADT archive already exists; choose a new source version/commit.' }
[IO.Compression.ZipFile]::CreateFromDirectory($taskOutput,$taskArchive,[IO.Compression.CompressionLevel]::Optimal,$false)
$taskAsset = @{id='adt';version=$taskVersion;fileName=$taskFileName;sha256=(Get-FileHash -LiteralPath $taskArchive -Algorithm SHA256).Hash.ToLowerInvariant();url='';format='zip';stripPrefix='';source='Private ADT release';capabilities=@{stdio='true';nativeLogPolicy='false';diagnose='false';connectionPolicyVersion='1'}}
$taskCatalog.assets = @($taskExisting) + @($taskAsset)
$taskCatalog.version = ([xml](Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'Directory.Build.props'))).Project.PropertyGroup.Version
$taskCatalog | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $taskPayload 'catalog.json') -Encoding utf8NoBOM
$taskNotices | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $taskPayload 'dependency-notices.json') -Encoding utf8NoBOM
& (Join-Path $PSScriptRoot 'Test-Publication.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Publication validation failed' }
Write-Host 'ADT updated; RFC/PyRFC/runtime assets preserved byte-for-byte.'
