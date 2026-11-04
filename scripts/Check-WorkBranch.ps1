[CmdletBinding()]
param(
    [string]$ExpectedBranch,
    [string]$RepositoryPath = (Split-Path $PSScriptRoot -Parent),
    [switch]$RequireClean
)
$ErrorActionPreference = 'Stop'

function Invoke-WorkGit {
    param([string[]]$GitArguments)
    # Keep stderr warnings separate: line-ending warnings are not changed files/conflicts.
    $taskOutput = @(& git -C $RepositoryPath @GitArguments)
    if ($LASTEXITCODE -ne 0) {
        throw ('Git check failed: ' + ($GitArguments -join ' ') + '. ' + ($taskOutput -join "`n"))
    }
    return $taskOutput
}

if ([string]::IsNullOrWhiteSpace($ExpectedBranch)) {
    throw 'Pass -ExpectedBranch with the branch assigned to this task. No files or branches were changed.'
}
$null = Invoke-WorkGit -GitArguments @('rev-parse', '--show-toplevel')
$taskBranch = (Invoke-WorkGit -GitArguments @('branch', '--show-current')) -join ''
if ([string]::IsNullOrWhiteSpace($taskBranch)) {
    throw 'Detached HEAD: switch to a task branch before making changes.'
}
if ($taskBranch -in @('YAPAY_ZEKA', 'main', 'master')) {
    throw ('Protected integration branch: ' + $taskBranch + '. Create a separate task branch before making changes.')
}
if ($taskBranch -cne $ExpectedBranch) {
    throw ('Wrong task branch. Expected: ' + $ExpectedBranch + '; actual: ' + $taskBranch + '.')
}
$taskConflicts = @(Invoke-WorkGit -GitArguments @('diff', '--name-only', '--diff-filter=U'))
if ($taskConflicts.Count -gt 0) { throw 'Unresolved merge conflicts: resolve them before starting work.' }
foreach ($taskState in @('MERGE_HEAD', 'CHERRY_PICK_HEAD', 'REVERT_HEAD', 'rebase-merge', 'rebase-apply', 'sequencer')) {
    $taskStatePath = (Invoke-WorkGit -GitArguments @('rev-parse', '--path-format=absolute', '--git-path', $taskState)) -join ''
    if (Test-Path -LiteralPath $taskStatePath) {
        throw ('Unfinished Git operation: ' + $taskState + '. Finish it before starting work.')
    }
}
$taskChanges = @(Invoke-WorkGit -GitArguments @('status', '--porcelain=v1', '--untracked-files=normal'))
if ($RequireClean -and $taskChanges.Count -gt 0) {
    throw 'Working tree is not clean. Preserve existing work and isolate the new task; nothing was changed.'
}
$taskCommit = (Invoke-WorkGit -GitArguments @('rev-parse', '--short', 'HEAD')) -join ''
Write-Output ('PASS work branch: ' + $taskBranch + '; commit: ' + $taskCommit + '; changed entries: ' + $taskChanges.Count)
Write-Output 'Read-only check. Task ownership, remote freshness and GitHub protections require separate verification.'
