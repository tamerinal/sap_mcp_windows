[CmdletBinding()]
param([Parameter(Mandatory)][string]$PreviousReleaseDirectory)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskVersion = ([xml](Get-Content -Raw -LiteralPath (Join-Path $taskRoot 'Directory.Build.props'))).Project.PropertyGroup.Version
$taskRegistry = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{950456B8-B9F0-443B-9CA9-A4F5FD067290}_is1'
if (Test-Path -LiteralPath $taskRegistry) { throw 'Existing installation detected; upgrade fixture must not alter it.' }
if (Test-Path -LiteralPath (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'SapMcpManager/config/settings.json')) { throw 'Existing manager settings detected; use an isolated user profile.' }
$taskArtifacts = [IO.Path]::GetFullPath((Join-Path $taskRoot 'artifacts'))
$taskPreviousHashes = Get-Content -Raw -LiteralPath (Join-Path $PreviousReleaseDirectory 'SHA256SUMS.json') | ConvertFrom-Json
$taskResults = @()
foreach ($taskMode in @('Online','Offline')) {
    $taskOldName = 'SAP-MCP-Manager-0.1.0-preview.14-Windows-x64-' + $taskMode + '.exe'
    $taskOld = Join-Path $PreviousReleaseDirectory $taskOldName
    $taskExpected = @($taskPreviousHashes | Where-Object fileName -eq $taskOldName)
    if ($taskExpected.Count -ne 1 -or (Get-FileHash -LiteralPath $taskOld -Algorithm SHA256).Hash -ne $taskExpected[0].Hash) { throw 'Previous release installer hash mismatch' }
    $taskFixture = [IO.Path]::GetFullPath((Join-Path $taskArtifacts ('upgrade-' + $taskMode + '-' + [guid]::NewGuid().ToString('N'))))
    if (!$taskFixture.StartsWith($taskArtifacts + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid upgrade fixture path' }
    $taskApp = Join-Path $taskFixture 'app'; $taskData = Join-Path $taskFixture 'user-data'
    New-Item -ItemType Directory -Force -Path $taskData | Out-Null
    $taskConfig = Join-Path $taskData 'settings.json'; $taskSecret = Join-Path $taskData 'fixture.password.dpapi'
    ConvertTo-SecureString 'fictional upgrade fixture password' -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -LiteralPath $taskSecret -Encoding utf8NoBOM
    $taskCertificate = Join-Path $taskData 'public-ca.cer'
    $taskRsa = [Security.Cryptography.RSA]::Create(2048)
    try {
        $taskRequest = [Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=fictional.example.invalid',$taskRsa,[Security.Cryptography.HashAlgorithmName]::SHA256,[Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $taskCert = $taskRequest.CreateSelfSigned([DateTimeOffset]::UtcNow.AddDays(-1),[DateTimeOffset]::UtcNow.AddDays(1))
        try { [IO.File]::WriteAllBytes($taskCertificate,$taskCert.Export([Security.Cryptography.X509Certificates.X509ContentType]::Cert)) } finally { $taskCert.Dispose() }
    } finally { $taskRsa.Dispose() }
    $taskSettings = @{
        schemaVersion=1;uiMode='advanced';uiLanguage='en';dataDirectory=$taskData;credentialsFile=(Join-Path $taskData 'mapping.json');secretsDirectory=$taskData
        nodeExecutable=(Join-Path $taskData 'node.exe');pythonExecutable=(Join-Path $taskData 'python.exe');powerShellExecutable=(Get-Process -Id $PID).Path
        packageDirectory=(Join-Path $taskData 'packages');hostExecutable=(Join-Path $taskApp 'SapMcpHost.exe')
        rfc=@{selected=$false;logDirectory=(Join-Path $taskData 'rfc-logs')};adt=@{selected=$false;logDirectory=(Join-Path $taskData 'adt-logs')}
        profiles=@(@{id='fixture';name='Fixture';targetKey='fictional.example.invalid:02';user='DEMO';client='100';language='EN';secretRef=$taskSecret;adtTimeoutSeconds=37;adtAutoDiscovery=$true;adtCertificates=@(@{file=$taskCertificate;sha256=(Get-FileHash -LiteralPath $taskCertificate -Algorithm SHA256).Hash})})
        activeProfiles=@{'fictional.example.invalid:02'='fixture'}
    }
    $taskSettings | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $taskConfig -Encoding utf8NoBOM
    '{"fixture":"synthetic mapping; no network access"}' | Set-Content -LiteralPath $taskSettings.credentialsFile -Encoding utf8NoBOM
    try {
        foreach ($taskPhase in @('previous','current')) {
            $taskInstaller = if ($taskPhase -eq 'previous') { $taskOld } else { Join-Path $taskRoot ('artifacts/installers/SAP-MCP-Manager-' + $taskVersion + '-Windows-x64-' + $taskMode + '.exe') }
            $taskProcess = Start-Process -FilePath $taskInstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS','/COMPONENTS=manager,adt',('/DIR="' + $taskApp + '"')) -WindowStyle Hidden -PassThru
            if (!$taskProcess.WaitForExit(60000)) { $taskProcess.Kill($true); throw 'Upgrade installation timed out' }
            if ($taskProcess.ExitCode -ne 0) { throw 'Upgrade installation failed' }
            $taskRegistration = Get-ItemProperty -LiteralPath $taskRegistry
            $taskExpectedVersion = if ($taskPhase -eq 'previous') { '0.1.0-preview.14' } else { $taskVersion }
            if ($taskRegistration.DisplayVersion -ne $taskExpectedVersion -or $taskRegistration.InstallLocation.TrimEnd('\') -ne $taskApp) { throw 'Upgrade installation identity/version mismatch' }
            $taskOutput = Join-Path $taskFixture ($taskPhase + '-diagnose.private.json')
            $taskDiagnosis = Start-Process -FilePath (Join-Path $taskApp 'SapMcpHost.exe') -ArgumentList @('--diagnose','--config',('"' + $taskConfig + '"')) -RedirectStandardOutput $taskOutput -RedirectStandardError (Join-Path $taskFixture ($taskPhase + '-diagnose.private.log')) -WindowStyle Hidden -PassThru
            if (!$taskDiagnosis.WaitForExit(30000)) { $taskDiagnosis.Kill($true); throw 'Upgrade settings diagnosis timed out' }
            if ($taskDiagnosis.ExitCode -notin @(0,2) -or (Get-Content -Raw -LiteralPath $taskOutput | ConvertFrom-Json).version -ne $taskExpectedVersion) { throw 'Installed Host could not read fixture settings' }
            if ($taskPhase -eq 'previous') {
                $taskMarker = Join-Path $taskApp 'user-preservation-marker.txt'; 'synthetic user marker' | Set-Content -LiteralPath $taskMarker
                $taskBefore = @(Get-ChildItem -LiteralPath $taskData -File | Get-FileHash -Algorithm SHA256); $taskMarkerHash = (Get-FileHash -LiteralPath $taskMarker -Algorithm SHA256).Hash
            }
        }
        foreach ($taskFile in $taskBefore) { if ((Get-FileHash -LiteralPath $taskFile.Path -Algorithm SHA256).Hash -ne $taskFile.Hash) { throw 'Upgrade changed fixture user data' } }
        if ((Get-FileHash -LiteralPath $taskMarker -Algorithm SHA256).Hash -ne $taskMarkerHash) { throw 'Upgrade changed the user marker' }
        $taskPlain = ConvertTo-SecureString ([IO.File]::ReadAllText($taskSecret).Trim())
        if ([Net.NetworkCredential]::new('', $taskPlain).Password -ne 'fictional upgrade fixture password') { throw 'Upgrade changed DPAPI credentials' }
        $taskResults += @{mode=$taskMode;from='0.1.0-preview.14';to=$taskVersion;status='passed';settingsProfilesCertificatesAndSecretsPreserved=$true}
        Write-Output ('PASS ' + $taskMode + ' preview.14 upgrade: installation identity, version, settings, profile, certificate and DPAPI preservation')
    } finally {
        $taskUninstaller = Join-Path $taskApp 'unins000.exe'
        if (Test-Path -LiteralPath $taskUninstaller) {
            $taskUninstall = Start-Process -FilePath $taskUninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -PassThru -Wait
            if ($taskUninstall.ExitCode -ne 0) { throw 'Upgrade fixture uninstall failed' }
        }
    }
}
$taskResults | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskArtifacts 'upgrade-results.json') -Encoding utf8NoBOM
