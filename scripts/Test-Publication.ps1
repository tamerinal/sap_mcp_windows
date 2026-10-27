[CmdletBinding()]
param([switch]$SourceOnly)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskFindings = [Collections.Generic.List[string]]::new()
# These patterns identify local workspace/user paths, not public GitHub owner names.
$taskPersonal = '(?i)([A-Z]:[\\/]+Users[\\/]+(?!\.{3})[^\\/\s"'']+|[A-Z]:[\\/]+Codex[\\/]|[A-Z]:[\\/]+OneDrive[\\/]|\.openclaw[\\/])'
$taskForbiddenFile = '(?i)(^|/)(sapnwrfc|libsapucum|icudt\d*|icuin\d*|icuuc\d*)\.(dll|so|dylib)$|\.password\.dpapi$|(^|/)(SAPUILandscape\.xml|sap_credentials\.json|settings\.json)$|\.pdb$'
$taskCredential = '(?i)-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----\s+[A-Za-z0-9+/]{32,}|gh[pousr]_[A-Za-z0-9]{30,}'
Push-Location $taskRoot
try {
    $taskFiles = @(& git ls-files --cached --others --exclude-standard) | Sort-Object -Unique
    if ($LASTEXITCODE -ne 0) { throw 'Git source inventory failed' }
    foreach ($taskRelative in $taskFiles) {
        if (!(Test-Path -LiteralPath $taskRelative -PathType Leaf)) { continue }
        if ($taskRelative -match $taskForbiddenFile) { $taskFindings.Add('Forbidden source file: ' + $taskRelative) }
        if ([IO.Path]::GetExtension($taskRelative) -in @('.cs','.axaml','.json','.md','.txt','.ps1','.iss','.yml','.props','.csproj','.xml','.example')) {
            $taskText = [IO.File]::ReadAllText((Join-Path $taskRoot $taskRelative))
            if ($taskText -match $taskPersonal) { $taskFindings.Add('Machine-specific path in source: ' + $taskRelative) }
            if ($taskText -match $taskCredential) { $taskFindings.Add('Potential credential in source: ' + $taskRelative) }
        }
    }
    if (!$SourceOnly) {
        $taskPublish = Join-Path $taskRoot 'artifacts/publish'
        if (Test-Path -LiteralPath $taskPublish) {
            foreach ($taskFile in Get-ChildItem -LiteralPath $taskPublish -Recurse -File) {
                if ($taskFile.Name -match $taskForbiddenFile) { $taskFindings.Add('Forbidden publish file: ' + $taskFile.Name) }
                if ($taskFile.Extension -in @('.dll','.exe','.json','.txt','.md')) {
                    $taskText = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($taskFile.FullName))
                    if ($taskText -match $taskPersonal) { $taskFindings.Add('Machine-specific path in publish file: ' + $taskFile.Name) }
                }
            }
        }
        $taskCatalog = Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'payload/catalog.json') | ConvertFrom-Json
        foreach ($taskAsset in $taskCatalog.assets | Where-Object { $_.id -in @('rfc','adt','pyrfc') }) {
            $taskArchive = Join-Path $taskRoot ('payload/packages/' + $taskAsset.fileName)
            if ((Get-FileHash -LiteralPath $taskArchive -Algorithm SHA256).Hash -ne $taskAsset.sha256) { throw "Catalog hash mismatch: $($taskAsset.id)" }
            $taskZip = [IO.Compression.ZipFile]::OpenRead($taskArchive)
            try {
                foreach ($taskEntry in $taskZip.Entries) {
                    if ($taskEntry.FullName -match $taskForbiddenFile) { $taskFindings.Add('Forbidden package file: ' + $taskAsset.id + '/' + $taskEntry.FullName) }
                    if ($taskEntry.Length -eq 0) { continue }
                    $taskStream = $taskEntry.Open(); $taskMemory = [IO.MemoryStream]::new()
                    try { $taskStream.CopyTo($taskMemory); $taskBytes = $taskMemory.ToArray() } finally { $taskStream.Dispose(); $taskMemory.Dispose() }
                    $taskText = [Text.Encoding]::UTF8.GetString($taskBytes)
                    if ($taskText -match $taskPersonal) { $taskFindings.Add('Machine-specific path in package: ' + $taskAsset.id + '/' + $taskEntry.FullName) }
                    if ($taskText -match $taskCredential) { $taskFindings.Add('Potential credential in package: ' + $taskAsset.id + '/' + $taskEntry.FullName) }
                }
            } finally { $taskZip.Dispose() }
        }
    }
    if ($taskFindings.Count) { throw ($taskFindings -join "`n") }
    Write-Host 'PASS publication scan: no forbidden files, machine-specific paths or detected credential patterns.'
    Write-Host 'Pattern scanning complements review; it cannot prove the absence of every possible secret.'
} finally { Pop-Location }
