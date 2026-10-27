[CmdletBinding()]
param([switch]$SkipTests, [switch]$SkipPayload, [string]$InnoCompiler = '', [string]$RfcSource, [string]$AdtSource, [string]$PyRfcSitePackages)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskVersion = ([xml](Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'Directory.Build.props'))).Project.PropertyGroup.Version
$taskDotnet = Join-Path $taskRoot '.tools\dotnet\dotnet.exe'
if (!(Test-Path -LiteralPath $taskDotnet)) { $taskDotnet = 'dotnet' }
if ([IO.Path]::IsPathFullyQualified($taskDotnet)) { $env:DOTNET_ROOT = Split-Path $taskDotnet -Parent }
Push-Location $taskRoot
try {
    if (!$SkipPayload) {
        if (!$RfcSource -or !$AdtSource -or !$PyRfcSitePackages) { throw 'Pass explicit MCP source and PyRFC paths, or use -SkipPayload with a prepared catalog.' }
        & (Join-Path $PSScriptRoot 'Prepare-Payload.ps1') -RfcSource $RfcSource -AdtSource $AdtSource -PyRfcSitePackages $PyRfcSitePackages
    }
    if (!$SkipTests) { & $taskDotnet test SapMcpManager.slnx -c Release -v minimal; if ($LASTEXITCODE -ne 0) { throw 'Tests failed' } }
    $taskPublish = Join-Path $taskRoot 'artifacts\publish'
    $taskPublish = [IO.Path]::GetFullPath($taskPublish)
    $taskArtifactsRoot = [IO.Path]::GetFullPath((Join-Path $taskRoot 'artifacts'))
    if (!$taskPublish.StartsWith($taskArtifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid publish output path' }
    if (Test-Path -LiteralPath $taskPublish) { Remove-Item -LiteralPath $taskPublish -Recurse -Force }
    & $taskDotnet publish src/SapMcp.Desktop/SapMcp.Desktop.csproj -c Release -r win-x64 --self-contained true -o $taskPublish -p:PublishSingleFile=false
    if ($LASTEXITCODE -ne 0) { throw 'Desktop publish failed' }
    & $taskDotnet publish src/SapMcp.Host/SapMcp.Host.csproj -c Release -r win-x64 --self-contained true -o $taskPublish -p:PublishSingleFile=false
    if ($LASTEXITCODE -ne 0) { throw 'Host publish failed' }
    $taskPublishPayload = Join-Path $taskPublish 'payload'
    New-Item -ItemType Directory -Force -Path $taskPublishPayload | Out-Null
    Copy-Item -LiteralPath (Join-Path $taskRoot 'payload\catalog.json') -Destination $taskPublishPayload -Force
    Copy-Item -LiteralPath (Join-Path $taskRoot 'payload\dependency-notices.json') -Destination $taskPublishPayload -Force
    Copy-Item -LiteralPath (Join-Path $taskRoot 'payload\packages') -Destination $taskPublishPayload -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $taskRoot 'docs') -Destination $taskPublish -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $taskRoot 'LICENSE'),(Join-Path $taskRoot 'THIRD-PARTY-NOTICES.md') -Destination $taskPublish -Force
    # Runtime NuGet packages can copy native debug symbols; they are not needed to run.
    Get-ChildItem -LiteralPath $taskPublish -Recurse -File -Filter '*.pdb' | ForEach-Object { Remove-Item -LiteralPath $_.FullName }
    & (Join-Path $PSScriptRoot 'Test-Publication.ps1')
    if (!$InnoCompiler) { $InnoCompiler = Join-Path $taskRoot '.tools\inno\ISCC.exe' }
    if (!(Test-Path -LiteralPath $InnoCompiler)) { throw 'Inno Setup 6.7.3 compiler required. Pass -InnoCompiler or install in .tools/inno.' }
    foreach ($taskOffline in @('0','1')) {
        & $InnoCompiler /Qp ('/DBuildRoot=' + $taskPublish) ('/DPayloadRoot=' + (Join-Path $taskRoot 'payload')) ('/DOffline=' + $taskOffline) (Join-Path $taskRoot 'installer\SapMcpManager.iss')
        if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
    }
    Get-ChildItem -LiteralPath (Join-Path $taskRoot 'artifacts\installers') -Filter ("SAP-MCP-Manager-$taskVersion-Windows-x64-*.exe") | Get-FileHash -Algorithm SHA256 | Select-Object @{Name='fileName';Expression={Split-Path $_.Path -Leaf}},Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskRoot 'artifacts\installers\SHA256SUMS.json') -Encoding utf8NoBOM
} finally { Pop-Location }
