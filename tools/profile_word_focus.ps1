<#
.SYNOPSIS
Opt-in, metadata-only Word focus timings; never reads document text or sends input.
.DESCRIPTION
Prepare a disposable Word document with its body focused, then explicitly permit
-AllowDesktop. The baseline is the retained nonresident 1.9.7 library from the
Acrobat investigation; Current is freshly compiled from development sources.
No resident is started, profile loaded, text changed, or clipboard accessed.
#>
param([ValidateSet('Baseline','Current')][string]$Mode = 'Current',
    [switch]$AllowDesktop, [ValidateRange(3,30)][int]$Samples = 12,
    [ValidateRange(0,60)][int]$WaitSeconds = 0)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $projectRoot 'build-common.ps1')
if ($Mode -eq 'Current') {
    $library = Join-Path $projectRoot 'out/reliability/KeyJaya.dll'
    New-Item -ItemType Directory -Path (Split-Path $library) -Force | Out-Null
    $arguments = @('/nologo','/optimize+','/platform:x64','/codepage:65001','/langversion:5',
        '/target:library','/r:System.Web.Extensions.dll',('/out:' + $library)) + $references + $runtime
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Timing library compilation failed.' }
} else {
    $library = Join-Path $projectRoot 'out/acrobat-search/KeyJaya.dll'
    if (!(Test-Path -LiteralPath $library)) { throw 'Retained baseline library missing; do not substitute a different build.' }
}
if (!$AllowDesktop) { Write-Output 'Timing library ready; no windows inspected.'; exit 0 }
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
[void][Reflection.Assembly]::LoadFrom($library)
$clock = [Diagnostics.Stopwatch]::StartNew()
do {
    $owner = [KeyJaya.Native]::GetForegroundWindow()
    $focus = $null
    if ([KeyJaya.WordPasteOptions]::IsWord($owner)) {
        $focus = [Windows.Automation.AutomationElement]::FocusedElement
        if ($focus -and $focus.Current.HasKeyboardFocus -and
            ($focus.Current.AutomationId -eq 'Body' -or
             $focus.Current.ControlType -eq [Windows.Automation.ControlType]::Document)) { break }
    }
    if ($clock.Elapsed.TotalSeconds -ge $WaitSeconds) { Write-Output 'Word body is not focused; no timings collected.'; exit 0 }
    Start-Sleep -Milliseconds 250
} while ($true)
$identity = [KeyJaya.Native]::FocusIdentity()
$values = @()
for ($sample = 0; $sample -lt $Samples; $sample++) {
    if ($owner -ne [KeyJaya.Native]::GetForegroundWindow() -or
        ![KeyJaya.EditGuard]::SameFocus($identity,[KeyJaya.Native]::FocusIdentity())) { throw 'Focus changed; timings discarded.' }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $valid = [KeyJaya.WordPasteOptions]::DocumentFocused($owner)
    $watch.Stop()
    if (!$valid) { throw 'Body focus was not verified; timings discarded.' }
    $values += $watch.Elapsed.TotalMilliseconds
}
$sorted = @($values | Sort-Object)
[pscustomobject]@{ mode=$Mode; samples=$Samples; first_ms=$values[0]
    median_ms=$sorted[[int][Math]::Floor(($Samples-1)/2)]; max_ms=$sorted[-1]
    assembly_sha256=(Get-FileHash -LiteralPath $library -Algorithm SHA256).Hash
    scope='Word focus metadata only; no document text or keyboard input'
} | ConvertTo-Json
