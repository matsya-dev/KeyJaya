<#
.SYNOPSIS
Tests Modern Comments in a newly owned Word document with the running development resident.
.DESCRIPTION
Compile-only by default. AllowDesktop requires the running resident to match the
current application build by SHA-256. Creates local scratch content only. Uses
owned editor input and the resident's normal Windows shortcut dispatch; does not
relax injected-key filtering, alter configuration, install, or publish anything.
Control discovery uses fresh owned native hosts, supported UIA actions and
localized Office labels where available. SetupViews needs no diagnostic restart.
#>
param([switch]$AllowDesktop, [ValidateSet('All','Panel','PanelReply','ContextualNew','ContextualAll','ContextualRecovery','SetupOnly','SetupViews')][string]$Scope = 'All', [switch]$ManualPanel,
    [string]$ResidentRoot = '', [string]$ResidentAssembly = '', [ValidateRange(15,600)][int]$TimeoutSeconds = 180)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $projectRoot 'build-common.ps1')
$run = Join-Path $projectRoot ('out/modern-word-comments/native-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$runner = Join-Path $run 'ModernWordCommentTests.exe'
$arguments = @('/nologo', '/platform:x64', '/codepage:65001', '/langversion:5', '/r:System.Web.Extensions.dll', '/r:System.Management.dll', '/main:KeyJaya.ModernWordCommentTests', ('/out:' + $runner)) + $references + $runtime + @((Join-Path $projectRoot 'tests/integration/ModernWordCommentTests.cs'),(Join-Path $projectRoot 'tests/integration/ModernWordSetup.cs'),(Join-Path $projectRoot 'tests/integration/ModernWordPanel.cs'),(Join-Path $projectRoot 'tests/contracts/ModernWordSetupTests.cs'))
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Modern Comments test compilation failed.' }
$selfTest = Start-Process -FilePath $runner -ArgumentList '--self-test' -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $run 'self-tests.txt') -RedirectStandardError (Join-Path $run 'self-tests-stderr.txt')
$null = $selfTest.Handle
if (!$selfTest.WaitForExit(10000)) { $selfTest.Kill(); throw 'Comment fixture self-tests timed out.' }
if ($selfTest.ExitCode -ne 0) { throw ('Comment fixture self-tests failed; inspect ' + $run) }
Get-Content -LiteralPath (Join-Path $run 'self-tests.txt') -Encoding UTF8
$scopeProbe = Start-Process -FilePath $runner -ArgumentList @('--scope-info',$Scope) -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $run 'scope.json') -RedirectStandardError (Join-Path $run 'scope-stderr.txt')
$null = $scopeProbe.Handle
if (!$scopeProbe.WaitForExit(5000)) { $scopeProbe.Kill(); throw 'Comment scope probe timed out.' }
if ($scopeProbe.ExitCode -ne 0) { throw 'Comment scope probe failed.' }
$scopeInfo = Get-Content -LiteralPath (Join-Path $run 'scope.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($scopeInfo.scope -ine $Scope -or $scopeInfo.needs_settlement -isnot [bool]) { throw 'Invalid comment scope metadata.' }
$Scope = $scopeInfo.scope
$denial = Start-Process -FilePath $runner -ArgumentList '--not-authorized' -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $run 'denied-stdout.txt') -RedirectStandardError (Join-Path $run 'denied-stderr.txt')
$null = $denial.Handle
if (!$denial.WaitForExit(5000)) { $denial.Kill(); throw 'The default-deny probe did not finish.' }
if ($denial.ExitCode -ne 2) { throw 'Modern Comments runner failed its default-deny check.' }
if (!$AllowDesktop) { Write-Output ('Compiled only: ' + $runner); return }
$resident = @(Get-CimInstance Win32_Process -Filter "name='keyjaya-app.exe'" |
    Where-Object { $_.CommandLine -notmatch '(?:^|\s)--(?:watch-word-focus|capture-clipboard)(?:\s|$)' })
if ($resident.Count -ne 1) { throw 'Exactly one tested development resident is required.' }
# The resident's queue probe intentionally expires after fifteen minutes. An
# expired probe returns zero too, which must not be mistaken for an idle queue.
# SetupOnly/SetupViews cannot dispatch conversions or use this probe.
if ($scopeInfo.needs_settlement) {
    if (!$resident[0].CommandLine.Contains('--diagnostic')) { throw 'Conversion tests require a diagnostic resident for verified settlement.' }
    $diagnosticDeadline = ([datetime]$resident[0].CreationDate).ToUniversalTime().AddMinutes(15)
    if ($diagnosticDeadline -le [DateTime]::UtcNow.AddSeconds($TimeoutSeconds + 5)) { throw 'The resident diagnostic window cannot cover this conversion test. No document was opened or app restarted; use a freshly authorized diagnostic copy after the current trial, or SetupOnly/SetupViews.' }
}
$built = if ($ResidentAssembly) { [IO.Path]::GetFullPath($ResidentAssembly) } else { Join-Path $projectRoot 'out/app/engine/keyjaya-app.exe' }
if (!(Test-Path -LiteralPath $built -PathType Leaf)) { throw 'The explicitly tested resident assembly is missing.' }
if ((Get-FileHash -LiteralPath $built -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $resident[0].ExecutablePath -Algorithm SHA256).Hash) { throw 'Running resident differs from the current app build; no document was opened.' }
$panelMode = if ($ManualPanel) { 'manual' } else { 'automatic' }
$profileRoot = if ($ResidentRoot) { [IO.Path]::GetFullPath($ResidentRoot) } else { Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'KeyJaya' }
$nativeArguments = @('--desktop',$projectRoot,(Join-Path $run 'scratch'),[string]$resident[0].ProcessId,$Scope,$panelMode,$profileRoot)
$quoted = @($nativeArguments | ForEach-Object { '"' + $_ + '"' })
$process = Start-Process -FilePath $runner -ArgumentList $quoted -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $run 'results.txt') -RedirectStandardError (Join-Path $run 'stderr.txt')
$null = $process.Handle
$started = $process.StartTime
$clock = [Diagnostics.Stopwatch]::StartNew()
while (!$process.WaitForExit(500)) {
    if ($clock.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
        $current = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
        if ($null -ne $current -and $current.Path -eq $runner -and $current.StartTime -eq $started) { Stop-Process -Id $process.Id -Force }
        throw ('Modern Comments check timed out; only its test runner was stopped. A scratch Word window may remain for review. Evidence: ' + $run)
    }
}
Get-Content -LiteralPath (Join-Path $run 'results.txt') -Encoding UTF8
if ($process.ExitCode -ne 0) { throw ('Modern Comments check failed; evidence: ' + $run) }
Write-Output ('Evidence: ' + $run)
