[CmdletBinding()]
param([switch]$NoBuild, [string]$ResultsDirectory = '')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskDotnet = Join-Path $taskRoot '.tools/dotnet/dotnet.exe'
if (!(Test-Path -LiteralPath $taskDotnet)) { $taskDotnet = 'dotnet' }
if ([IO.Path]::IsPathFullyQualified($taskDotnet)) { $env:DOTNET_ROOT = Split-Path $taskDotnet -Parent }
if (!$ResultsDirectory) { $ResultsDirectory = Join-Path $taskRoot 'artifacts/test-results' }
New-Item -ItemType Directory -Force -Path $ResultsDirectory | Out-Null
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
Push-Location $taskRoot
try {
    if (!$NoBuild) {
        & $taskDotnet restore SapMcpManager.slnx --locked-mode
        if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed' }
        & $taskDotnet build SapMcpManager.slnx -c Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'Test build failed' }
    }
    & $taskDotnet test tests/SapMcp.Tests/SapMcp.Tests.csproj -c Release --no-build --logger trx --results-directory $ResultsDirectory
    if ($LASTEXITCODE -ne 0) { throw 'Unit tests failed' }
    # Invoke xUnit v3 directly. The VSTest -assemblyInfo subprocess can receive a stray
    # shutdown message instead of pure JSON (upstream xunit/xunit#3576).
    $taskUiReport = Join-Path $ResultsDirectory 'ui.trx'
    & $taskDotnet exec tests/SapMcp.UiTests/bin/Release/net10.0/SapMcp.UiTests.dll -noLogo -noAutoReporters -failSkips -trx $taskUiReport
    if ($LASTEXITCODE -ne 0) { throw 'Headless UI tests failed' }
    [xml]$taskReport = Get-Content -LiteralPath $taskUiReport -Raw
    $taskCounters = $taskReport.TestRun.ResultSummary.Counters
    if ([int]$taskCounters.total -le 0 -or [int]$taskCounters.failed -ne 0 -or [int]$taskCounters.passed -ne [int]$taskCounters.total) { throw 'UI report must contain executed, passing tests' }
    Write-Output ('PASS headless UI report: ' + $taskCounters.passed + ' tests, none skipped.')
} finally { Pop-Location }
