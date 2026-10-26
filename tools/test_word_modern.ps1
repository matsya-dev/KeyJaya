<#
.SYNOPSIS
Tests Modern Comments in a newly owned Word document with the running development resident.
.DESCRIPTION
Compile-only by default. AllowDesktop requires the running resident to match the
current application build by SHA-256. Creates local scratch content only. Uses
owned editor input and the resident's normal Windows shortcut dispatch; does not
relax injected-key filtering, alter configuration, install, or publish anything.
The view-switch fixture currently requires Word's English Comments button label.
#>
param([switch]$AllowDesktop, [ValidateSet('All','Panel','PanelReply')][string]$Scope = 'All', [switch]$ManualPanel)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $projectRoot 'build-common.ps1')
$run = Join-Path $projectRoot ('out/modern-word-comments/native-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$runner = Join-Path $run 'ModernWordCommentTests.exe'
$arguments = @('/nologo', '/platform:x64', '/codepage:65001', '/r:System.Web.Extensions.dll', '/main:KeyJaya.ModernWordCommentTests', ('/out:' + $runner)) + $references + $runtime + (Join-Path $projectRoot 'tests/integration/ModernWordCommentTests.cs')
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Modern Comments test compilation failed.' }
if (!$AllowDesktop) { Write-Output ('Compiled only: ' + $runner); return }
$resident = @(Get-CimInstance Win32_Process -Filter "name='keyjaya-app.exe'")
if ($resident.Count -ne 1) { throw 'Exactly one tested development resident is required.' }
if (!$resident[0].CommandLine.Contains('--diagnostic')) { throw 'Run the tested development resident in diagnostic mode so conversion settlement can be verified.' }
$built = Join-Path $projectRoot 'out/app/engine/keyjaya-app.exe'
if ((Get-FileHash -LiteralPath $built -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $resident[0].ExecutablePath -Algorithm SHA256).Hash) { throw 'Running resident differs from the current app build; no document was opened.' }
$panelMode = if ($ManualPanel) { 'manual' } else { 'automatic' }
& $runner $projectRoot (Join-Path $run 'scratch') ([string]$resident[0].ProcessId) $Scope $panelMode 2>&1 | Tee-Object -FilePath (Join-Path $run 'results.txt')
if ($LASTEXITCODE -ne 0) { throw ('Modern Comments check failed; evidence: ' + $run) }
Write-Output ('Evidence: ' + $run)
