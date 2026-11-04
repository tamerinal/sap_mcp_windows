[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskFixture = Join-Path $taskRoot ('artifacts/publication-privacy-' + [guid]::NewGuid().ToString('N'))
$taskScripts = Join-Path $taskFixture 'scripts'
[IO.Directory]::CreateDirectory($taskScripts) | Out-Null
[IO.File]::Copy((Join-Path $taskRoot 'scripts/Test-Publication.ps1'), (Join-Path $taskScripts 'Test-Publication.ps1'))
& git -C $taskFixture init -q
if ($LASTEXITCODE -ne 0) { throw 'Could not initialize publication fixture' }
$taskSample = Join-Path $taskFixture 'README.md'
$taskCases = @(
    @{ Name = 'private class A address'; Text = (@(10, 23, 45, 67) -join '.'); Rejected = $true },
    @{ Name = 'private class B address'; Text = (@(172, 19, 2, 3) -join '.'); Rejected = $true },
    @{ Name = 'masked private subnet'; Text = (@('192', '168', '17', 'xxx') -join '.'); Rejected = $true },
    @{ Name = 'escaped user path'; Text = ('C:' + '\\Users\\' + 'fixture\\project'); Rejected = $true },
    @{ Name = 'reserved documentation example'; Text = 'https://sap.example.invalid / 192.0.2.10'; Rejected = $false },
    @{ Name = 'loopback fixture'; Text = '127.0.0.1 / ::1'; Rejected = $false }
)
foreach ($taskCase in $taskCases) {
    [IO.File]::WriteAllText($taskSample, $taskCase.Text, [Text.UTF8Encoding]::new($false))
    $taskRejected = $false
    try { & (Join-Path $taskScripts 'Test-Publication.ps1') -SourceOnly *> $null }
    catch { $taskRejected = $true }
    if ($taskRejected -ne $taskCase.Rejected) { throw ('Incorrect publication verdict: ' + $taskCase.Name) }
    Write-Output ('PASS publication privacy: ' + $taskCase.Name)
}
