[CmdletBinding()]
param([string]$SdkDirectory = '', [string]$PublishDirectory = '')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskDotnet = Join-Path $taskRoot '.tools\dotnet\dotnet.exe'
if (!(Test-Path -LiteralPath $taskDotnet)) { $taskDotnet = 'dotnet' }
if (!$PublishDirectory) { $PublishDirectory = Join-Path $taskRoot 'artifacts\publish' }
$taskVerification = Join-Path $taskRoot ('artifacts\verification\run-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
& $taskDotnet run --project (Join-Path $taskRoot 'tests\SapMcp.Integration\SapMcp.Integration.csproj') -c Release -- $PublishDirectory (Join-Path $taskRoot 'payload') $taskVerification $SdkDirectory
if ($LASTEXITCODE -ne 0) { throw 'Release verification failed' }
Copy-Item -LiteralPath (Join-Path $taskVerification 'report.json') -Destination (Join-Path $taskRoot 'artifacts\verification\report.json') -Force
Write-Host "Verification results: $taskVerification"
