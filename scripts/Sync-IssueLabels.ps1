[CmdletBinding()]
param([string]$Repository = 'tamerinal/sap_mcp_windows')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskLabels = Get-Content -Raw -LiteralPath (Join-Path $taskRoot '.github/labels.json') | ConvertFrom-Json
foreach ($taskLabel in $taskLabels) {
    & gh label create $taskLabel.name --repo $Repository --color $taskLabel.color --description $taskLabel.description --force
    if ($LASTEXITCODE -ne 0) { throw ('Could not synchronize label: ' + $taskLabel.name) }
}
Write-Host ('Synchronized ' + $taskLabels.Count + ' labels. Other repository labels were preserved.')
