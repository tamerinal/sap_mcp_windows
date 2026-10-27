[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskRegistry = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{950456B8-B9F0-443B-9CA9-A4F5FD067290}_is1'
if (Test-Path -LiteralPath $taskRegistry) { throw 'An existing SAP MCP Manager installation is registered. Run installer smoke tests in a clean VM/user profile to preserve it.' }
$taskArtifacts = [IO.Path]::GetFullPath((Join-Path $taskRoot 'artifacts'))
$taskResults = [System.Collections.Generic.List[object]]::new()
$taskScenarios = @(
    @{ Mode='Online'; Name='AdtOnly'; Components='manager,adt'; Rfc=$false; Adt=$true },
    @{ Mode='Offline'; Name='Both'; Components='manager,rfc,adt'; Rfc=$true; Adt=$true },
    @{ Mode='Offline'; Name='RfcOnly'; Components='manager,rfc'; Rfc=$true; Adt=$false },
    @{ Mode='Online'; Name='Empty'; Components='manager'; Reject=$true }
)
foreach ($taskScenario in $taskScenarios) {
    $taskMode = $taskScenario.Mode
    $taskInstaller = Join-Path $taskRoot ('artifacts\installers\SAP-MCP-Manager-0.1.0-preview.1-Windows-x64-' + $taskMode + '.exe')
    $taskDestination = [IO.Path]::GetFullPath((Join-Path $taskArtifacts ('installer-smoke\' + $taskMode + '-' + $taskScenario.Name)))
    if (!$taskDestination.StartsWith($taskArtifacts + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid test destination' }
    $taskLog = Join-Path $taskArtifacts ('installer-smoke-' + $taskMode + '-' + $taskScenario.Name + '.log')
    $taskProcess = Start-Process -FilePath $taskInstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS',('/COMPONENTS=' + $taskScenario.Components),('/DIR="' + $taskDestination + '"'),('/LOG="' + $taskLog + '"')) -WindowStyle Hidden -PassThru
    if (!$taskProcess.WaitForExit(60000)) { $taskProcess.Kill($true); throw "Installer timed out: $taskMode $($taskScenario.Name)" }
    if ($taskScenario.Reject) {
        if ($taskProcess.ExitCode -eq 0 -or (Test-Path -LiteralPath (Join-Path $taskDestination 'SapMcpManager.exe'))) { throw 'Empty component selection was accepted' }
        $taskResults.Add(@{mode=$taskMode;scenario=$taskScenario.Name;status='passed';rejected=$true})
        Write-Host 'PASS empty component selection rejected before installation'
        continue
    }
    if ($taskProcess.ExitCode -ne 0) { throw "Installer failed: $taskMode ($($taskProcess.ExitCode)); $taskLog" }
    try {
        foreach ($taskFile in @('SapMcpManager.exe','SapMcpHost.exe','coreclr.dll','payload\catalog.json','setup-selection.json')) {
            if (!(Test-Path -LiteralPath (Join-Path $taskDestination $taskFile))) { throw "Installed file missing: $taskFile" }
        }
        $taskSelection = Get-Content -Raw -LiteralPath (Join-Path $taskDestination 'setup-selection.json') | ConvertFrom-Json
        if ($taskSelection.rfc -ne $taskScenario.Rfc -or $taskSelection.adt -ne $taskScenario.Adt) { throw 'Installer component selection was not persisted' }
        $taskCatalog = Get-Content -Raw -LiteralPath (Join-Path $taskDestination 'payload\catalog.json') | ConvertFrom-Json
        foreach ($taskAsset in $taskCatalog.assets) {
            $taskFile = Join-Path $taskDestination ('payload\packages\' + $taskAsset.fileName)
            if (!(Test-Path -LiteralPath $taskFile)) { $taskFile = Join-Path $taskDestination ('payload\cache\' + $taskAsset.fileName) }
            $taskMustExist = $taskMode -eq 'Offline' -or $taskAsset.id -in @('rfc','adt','pyrfc')
            if ($taskMustExist) {
                if (!(Test-Path -LiteralPath $taskFile)) { throw "Installer asset missing: $($taskAsset.id)" }
                if ((Get-FileHash -Algorithm SHA256 -LiteralPath $taskFile).Hash -ne $taskAsset.sha256) { throw "Installed package hash mismatch: $($taskAsset.id)" }
            }
        }
        $taskResults.Add(@{mode=$taskMode;scenario=$taskScenario.Name;status='passed';selfContained=$true;hashesVerified=$true})
        Write-Host "PASS $taskMode $($taskScenario.Name): self-contained files, selected components and package hashes"
    } finally {
        $taskUninstaller = Join-Path $taskDestination 'unins000.exe'
        if (Test-Path -LiteralPath $taskUninstaller) {
            $taskUninstall = Start-Process -FilePath $taskUninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -PassThru -Wait
            if ($taskUninstall.ExitCode -ne 0) { throw "Uninstall failed: $taskMode" }
            if (Test-Path -LiteralPath (Join-Path $taskDestination 'setup-selection.json')) { throw 'Uninstall left installer selection metadata behind' }
        }
    }
}
$taskResults | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskArtifacts 'installer-smoke-results.json') -Encoding utf8NoBOM
