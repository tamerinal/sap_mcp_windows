[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RfcSource,
    [Parameter(Mandatory)][string]$AdtSource,
    [Parameter(Mandatory)][string]$PyRfcSitePackages,
    [switch]$RefreshRuntimePins
)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskPayload = Join-Path $taskRoot 'payload'
$taskCache = Join-Path $taskPayload 'cache'
$taskPackages = Join-Path $taskPayload 'packages'
$taskStageRoot = Join-Path $taskRoot 'artifacts\payload-build'
New-Item -ItemType Directory -Force -Path $taskCache,$taskPackages,$taskStageRoot | Out-Null

function Get-TaskDownload([string]$Url, [string]$FileName, [string]$ExpectedHash = '') {
    $taskTarget = Join-Path $taskCache $FileName
    if (!(Test-Path -LiteralPath $taskTarget)) {
        Write-Host "Downloading $FileName"
        Invoke-WebRequest -Uri $Url -OutFile ($taskTarget + '.download')
        Move-Item -LiteralPath ($taskTarget + '.download') -Destination $taskTarget -Force
    }
    $taskHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $taskTarget).Hash.ToLowerInvariant()
    if ($ExpectedHash -and $taskHash -ne $ExpectedHash.ToLowerInvariant()) { throw "SHA256 mismatch: $FileName" }
    return $taskHash
}

$taskLockPath = Join-Path $taskPayload 'sources.lock.json'
if ((Test-Path -LiteralPath $taskLockPath) -and !$RefreshRuntimePins) {
    $taskPins = Get-Content -Raw -LiteralPath $taskLockPath | ConvertFrom-Json -AsHashtable
} else {
    $taskNodeIndex = Invoke-RestMethod 'https://nodejs.org/dist/index.json'
    $taskNode = $taskNodeIndex | Where-Object { $_.version -like 'v24.*' -and $_.lts } | Select-Object -First 1
    $taskNodeFile = 'node-' + $taskNode.version + '-win-x64.zip'
    $taskChecksums = (Invoke-WebRequest ('https://nodejs.org/dist/' + $taskNode.version + '/SHASUMS256.txt')).Content
    $taskNodeHash = ($taskChecksums -split "`n" | Where-Object { $_ -match ('\s' + [regex]::Escape($taskNodeFile) + '$') } | Select-Object -First 1) -split '\s+' | Select-Object -First 1
    if (!$taskNodeHash) { throw 'Node.js official checksum missing' }
    $taskPythonRelease = Invoke-RestMethod 'https://api.github.com/repos/astral-sh/python-build-standalone/releases/latest'
    $taskPython = $taskPythonRelease.assets | Where-Object { $_.name -match '^cpython-3\.11\..*x86_64-pc-windows-msvc-install_only_stripped.tar.gz$' } | Select-Object -First 1
    if (!$taskPython -or !$taskPython.digest) { throw 'Python asset or GitHub SHA256 digest missing' }
    $taskVcUrls = @{
        'vc-runtime' = 'https://aka.ms/vs/17/release/vc_redist.x64.exe'
        'vc2013' = 'https://aka.ms/highdpimfc2013x64enu'
    }
    $taskPins = @{
        schemaVersion = 1
        node = @{ version = $taskNode.version.TrimStart('v'); fileName = $taskNodeFile; url = 'https://nodejs.org/dist/' + $taskNode.version + '/' + $taskNodeFile; sha256 = $taskNodeHash; format = 'zip'; stripPrefix = 'node-' + $taskNode.version + '-win-x64'; source = 'Node.js official distribution and SHASUMS256.txt' }
        python = @{ version = ($taskPython.name -split '\+')[0].Replace('cpython-',''); fileName = $taskPython.name; url = $taskPython.browser_download_url; sha256 = $taskPython.digest.Replace('sha256:',''); format = 'tar.gz'; stripPrefix = 'python'; source = 'astral-sh/python-build-standalone ' + $taskPythonRelease.tag_name }
    }
    foreach ($taskId in $taskVcUrls.Keys) {
        $taskFileName = $taskId + '-x64.exe'
        $taskResolvedUrl = (Invoke-WebRequest -Uri $taskVcUrls[$taskId] -Method Head).BaseResponse.RequestMessage.RequestUri.AbsoluteUri
        $taskHash = Get-TaskDownload $taskResolvedUrl $taskFileName
        $taskSignature = Get-AuthenticodeSignature -LiteralPath (Join-Path $taskCache $taskFileName)
        if ($taskSignature.Status -ne 'Valid' -or $taskSignature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') { throw "Microsoft signature invalid: $taskId" }
        $taskVersion = (Get-Item -LiteralPath (Join-Path $taskCache $taskFileName)).VersionInfo.ProductVersion
        $taskPins[$taskId] = @{ version = $taskVersion; fileName = $taskFileName; url = $taskResolvedUrl; sha256 = $taskHash; format = 'exe'; stripPrefix = ''; source = 'Microsoft signed Visual C++ runtime' }
    }
    $taskPins | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $taskLockPath -Encoding utf8NoBOM
}

$taskAssets = [System.Collections.Generic.List[object]]::new()
$taskDependencyNotices = [System.Collections.Generic.List[object]]::new()
foreach ($taskId in @('node','python','vc-runtime','vc2013')) {
    $taskPin = $taskPins[$taskId]
    $null = Get-TaskDownload $taskPin.url $taskPin.fileName $taskPin.sha256
    $taskAsset = @{ id = $taskId }
    foreach ($taskKey in $taskPin.Keys) { $taskAsset[$taskKey] = $taskPin[$taskKey] }
    $taskAssets.Add($taskAsset)
}

foreach ($taskPair in @(@{id='rfc';source=$RfcSource},@{id='adt';source=$AdtSource})) {
    if (!(Test-Path -LiteralPath (Join-Path $taskPair.source 'package-lock.json'))) { throw "Missing MCP source: $($taskPair.source)" }
    $taskBuild = Join-Path $taskStageRoot ($taskPair.id + '-' + [guid]::NewGuid().ToString('N'))
    $taskOutput = Join-Path $taskBuild 'package'
    New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
    foreach ($taskName in @('src','python','package.json','package-lock.json','tsconfig.json')) {
        $taskSource = Join-Path $taskPair.source $taskName
        if (Test-Path -LiteralPath $taskSource) { Copy-Item -LiteralPath $taskSource -Destination $taskBuild -Recurse }
    }
    # Distribution-only adaptation; source repositories remain untouched.
    $taskConfigPath = Join-Path $taskBuild 'src/config.ts'
    $taskConfig = Get-Content -Raw -LiteralPath $taskConfigPath
    $taskDefaults = @{
        DEFAULT_LANDSCAPE_FILE = '""'
        DEFAULT_CREDENTIALS_FILE = '""'
        DEFAULT_PYTHON_EXECUTABLE = '"python"'
        DEFAULT_SECRET_STORE_FILE = '""'
        DEFAULT_RFC_MCP_ARGS = '[]'
        DEFAULT_RFC_MCP_CWD = 'process.cwd()'
    }
    $taskAdaptations = @()
    foreach ($taskConstant in $taskDefaults.Keys) {
        $taskPattern = '(?ms)(export const ' + $taskConstant + '\s*=).*?;'
        if ($taskConfig -match $taskPattern) {
            $taskConfig = [regex]::Replace($taskConfig, $taskPattern, ('$1 ' + $taskDefaults[$taskConstant] + ';'))
            $taskAdaptations += $taskConstant
        }
    }
    Set-Content -LiteralPath $taskConfigPath -Value $taskConfig -Encoding utf8NoBOM
    Push-Location $taskBuild
    try {
        & npm.cmd ci --ignore-scripts --no-audit --no-fund
        if ($LASTEXITCODE -ne 0) { throw 'npm ci failed' }
        & npm.cmd run build
        if ($LASTEXITCODE -ne 0) { throw 'MCP TypeScript build failed' }
        & npm.cmd prune --omit=dev --ignore-scripts --no-audit --no-fund
        if ($LASTEXITCODE -ne 0) { throw 'npm prune failed' }
    } finally { Pop-Location }
    foreach ($taskName in @('dist','python','node_modules','package.json','package-lock.json','LICENSE','NOTICE')) {
        $taskSource = Join-Path $taskBuild $taskName
        if (Test-Path -LiteralPath $taskSource) { Copy-Item -LiteralPath $taskSource -Destination $taskOutput -Recurse }
    }
    Copy-Item -LiteralPath (Join-Path $taskRoot 'LICENSE') -Destination $taskOutput -Force
    $taskDependencyLock = Get-Content -Raw -LiteralPath (Join-Path $taskOutput 'package-lock.json') | ConvertFrom-Json -AsHashtable
    foreach ($taskDependencyPath in $taskDependencyLock.packages.Keys | Where-Object { $_ -like 'node_modules/*' }) {
        $taskDependencyDirectory = Join-Path $taskOutput $taskDependencyPath
        $taskDependencyJson = Join-Path $taskDependencyDirectory 'package.json'
        if (!(Test-Path -LiteralPath $taskDependencyJson)) { continue }
        $taskDependency = Get-Content -Raw -LiteralPath $taskDependencyJson | ConvertFrom-Json
        $taskNoticeFiles = @(Get-ChildItem -LiteralPath $taskDependencyDirectory -File | Where-Object { $_.Name -match '^(LICENSE|LICENCE|NOTICE|COPYING)' } | ForEach-Object { $taskDependencyPath + '/' + $_.Name })
        $taskDependencyNotices.Add(@{ component = $taskPair.id; name = $taskDependency.name; version = $taskDependency.version; license = $taskDependency.license; noticeFiles = $taskNoticeFiles })
    }
    $taskMetadata = Get-Content -Raw -LiteralPath (Join-Path $taskBuild 'package.json') | ConvertFrom-Json
    $taskCommit = (& git -C $taskPair.source rev-parse HEAD).Trim()
    $taskDirty = [bool](& git -C $taskPair.source status --porcelain)
    $taskVersion = $taskMetadata.version + '+' + $taskCommit.Substring(0,7) + $(if ($taskDirty) { '.snapshot' } else { '' })
    @{ repository = $(if ($taskPair.id -eq 'rfc') {'tamerinal/SAP_RFC_MCP'} else {'tamerinal/sap_adt_mcp'}); commit = $taskCommit; includesLocalChanges = $taskDirty; version = $taskVersion; distributionAdaptations = $taskAdaptations; adaptationReason = 'Remove machine-specific fallback paths; managed host provides explicit environment configuration.' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskOutput 'build-info.json') -Encoding utf8NoBOM
    $taskFileName = 'sap-' + $taskPair.id + '-' + $taskVersion + '.zip'
    $taskArchive = Join-Path $taskPackages $taskFileName
    if (Test-Path -LiteralPath $taskArchive) { Remove-Item -LiteralPath $taskArchive }
    [System.IO.Compression.ZipFile]::CreateFromDirectory($taskOutput, $taskArchive, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    $taskAssets.Add(@{id=$taskPair.id;version=$taskVersion;fileName=$taskFileName;sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $taskArchive).Hash.ToLowerInvariant();url='';format='zip';stripPrefix='';source='Private MCP source snapshot';capabilities=@{nativeLogPolicy='false';diagnose='false';stdio='true'}})
}

$taskPyRfcDirectory = Join-Path $PyRfcSitePackages 'pyrfc'
$taskPyRfcInfo = Get-ChildItem -LiteralPath $PyRfcSitePackages -Directory -Filter 'pyrfc-*.dist-info' | Select-Object -First 1
if (!$taskPyRfcInfo -or !(Test-Path -LiteralPath (Join-Path $taskPyRfcDirectory '_cyrfc.cp311-win_amd64.pyd'))) { throw 'A verified cp311 win_amd64 PyRFC build is required. See docs/MCP-RFC-DEVELOPMENT-REQUEST.md.' }
$taskPyStage = Join-Path $taskStageRoot ('pyrfc-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $taskPyStage | Out-Null
Copy-Item -LiteralPath $taskPyRfcDirectory -Destination $taskPyStage -Recurse
Copy-Item -LiteralPath $taskPyRfcInfo.FullName -Destination $taskPyStage -Recurse
$taskNative = Join-Path $taskPyStage 'pyrfc/_cyrfc.cp311-win_amd64.pyd'
$taskOriginalNativeHash = (Get-FileHash -LiteralPath $taskNative -Algorithm SHA256).Hash.ToLowerInvariant()
# Strip only CodeView's debug-symbol pathname, never executable code or SDK references.
# The approved local binary embeds its build user's temporary PDB path.
$taskNativeBytes = [IO.File]::ReadAllBytes($taskNative)
$taskNativeStream = [IO.MemoryStream]::new($taskNativeBytes, $false)
$taskPe = [System.Reflection.PortableExecutable.PEReader]::new($taskNativeStream)
try {
    foreach ($taskDebug in $taskPe.ReadDebugDirectory()) {
        if ($taskDebug.Type -eq [System.Reflection.PortableExecutable.DebugDirectoryEntryType]::CodeView) {
            $taskCodeView = $taskPe.ReadCodeViewDebugDirectoryData($taskDebug)
            if ($taskCodeView.Path -match '^[A-Za-z]:[\\/]') {
                $taskPathOffset = $taskDebug.DataPointer + 24 # RSDS, GUID, age
                $taskPathLength = [Text.Encoding]::UTF8.GetByteCount($taskCodeView.Path)
                if ($taskPathLength -lt 9 -or $taskPathOffset + $taskPathLength -ge $taskNativeBytes.Length) { throw 'Invalid CodeView debug record' }
                [Array]::Clear($taskNativeBytes, $taskPathOffset, $taskPathLength)
                [Text.Encoding]::ASCII.GetBytes('pyrfc.pdb').CopyTo($taskNativeBytes, $taskPathOffset)
            }
        }
    }
} finally { $taskPe.Dispose(); $taskNativeStream.Dispose() }
[IO.File]::WriteAllBytes($taskNative, $taskNativeBytes)
Get-ChildItem -LiteralPath $taskPyStage -Recurse -File -Filter '*.pdb' | ForEach-Object { Remove-Item -LiteralPath $_.FullName }
@{ sourceCommit = '5d4a20a5aee37accf32fc44ed8d668bd69332169'; originalNativeSha256 = $taskOriginalNativeHash; distributionNativeSha256 = (Get-FileHash -LiteralPath $taskNative -Algorithm SHA256).Hash.ToLowerInvariant(); distributionAdaptations = @('Omit PDB symbols', 'Neutralize CodeView debug-symbol pathname; executable code unchanged') } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskPyStage 'build-info.json') -Encoding utf8NoBOM
Get-ChildItem -LiteralPath $taskPyStage -Directory -Recurse -Filter '__pycache__' | ForEach-Object { $taskRemove = [IO.Path]::GetFullPath($_.FullName); if (!$taskRemove.StartsWith([IO.Path]::GetFullPath($taskPyStage) + [IO.Path]::DirectorySeparatorChar)) { throw 'Invalid clean-up path' }; Remove-Item -LiteralPath $taskRemove -Recurse -Force }
$taskPyVersion = ($taskPyRfcInfo.Name -replace '^pyrfc-','' -replace '\.dist-info$','')
$taskPyFile = 'pyrfc-' + $taskPyVersion + '-cp311-win_amd64.zip'
$taskPyArchive = Join-Path $taskPackages $taskPyFile
if (Test-Path -LiteralPath $taskPyArchive) { Remove-Item -LiteralPath $taskPyArchive }
[System.IO.Compression.ZipFile]::CreateFromDirectory($taskPyStage, $taskPyArchive, [System.IO.Compression.CompressionLevel]::Optimal, $false)
$taskProvenance = Get-Content -Raw -LiteralPath (Join-Path $taskPyRfcInfo.FullName 'direct_url.json') | ConvertFrom-Json
$taskAssets.Add(@{id='pyrfc';version=$taskPyVersion;fileName=$taskPyFile;sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $taskPyArchive).Hash.ToLowerInvariant();url='';format='zip';stripPrefix='';source='SAP-archive/PyRFC commit ' + $taskProvenance.vcs_info.commit_id})

foreach ($taskArchive in Get-ChildItem -LiteralPath $taskPackages -Filter '*.zip') {
    $taskZip = [System.IO.Compression.ZipFile]::OpenRead($taskArchive.FullName)
    try {
        foreach ($taskEntry in $taskZip.Entries) {
            if ($taskEntry.FullName -match '(?i)(sapnwrfc|libsapucum|icudt\d*|icuin\d*|icuuc\d*)\.(dll|so|dylib)$|\.password\.dpapi$|(^|/)sap_credentials\.json$') { throw "Forbidden SAP SDK or secret file in package: $($taskEntry.FullName)" }
        }
    } finally { $taskZip.Dispose() }
}
@{schemaVersion=1;version='0.1.0-preview.1';assets=$taskAssets} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $taskPayload 'catalog.json') -Encoding utf8NoBOM
$taskDependencyNotices | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $taskPayload 'dependency-notices.json') -Encoding utf8NoBOM
Write-Host 'Payload prepared. No SAP SDK binaries or user credential files were included.'
