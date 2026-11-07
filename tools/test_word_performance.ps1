<#
.SYNOPSIS
Prepares and self-tests repeatable Word body comparisons; desktop execution is opt-in.
.DESCRIPTION
Default: compile two C# 5 runners and validate fixtures/reporting without Office,
UIA inspection, hooks, clipboard or installed settings. AllowDesktop creates one
new exclusively owned Word process and scratch document, alternating baseline
and candidate on identical inputs with Track Changes off/on. It verifies text,
formatting, retained selection/caret, one-step undo and earlier undo history
outside the timed operation.
No resident or installer is started. Existing documents are never enumerated/read.
.PARAMETER BaselineWordSelection
Optional earlier WordSelection.cs. All other sources and fixtures are identical.
Without it, the comparison is a same-source calibration, not a speedup claim.
.PARAMETER AllowDesktop
Requires separate permission and an idle keyboard and mouse. Uses its own Word foreground
window; aborts if focus changes. Native Ctrl hardware/paste UI/comments are separate.
.PARAMETER TimeoutSeconds
Bounds the helper lifetime. On timeout stops only its own test helper, never Word
or another app; any remaining scratch Word window is left for inspection.
#>
param(
    [switch]$AllowDesktop,
    [string]$BaselineWordSelection = '',
    [string[]]$Cases = @(),
    [ValidateRange(3,31)][int]$Iterations = 5,
    [ValidateRange(1,3)][int]$Warmups = 1,
    [ValidateRange(30,600)][int]$TimeoutSeconds = 360
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $root 'build-common.ps1')
$output = Join-Path $root 'out/word-performance-tests'
$build = Join-Path $output 'build'
New-Item -ItemType Directory -Path $output,$build -Force | Out-Null
foreach ($folder in @((Join-Path $root 'out'),$output,$build)) {
    if ((Get-Item -LiteralPath $folder).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Test output must not be linked.' }
}
$lock = [IO.File]::Open((Join-Path $output 'runner.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try {
    $run = Join-Path $output ('run-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $run | Out-Null
    $selection = Join-Path $root 'src/WordSelection.cs'
    $baseline = if ($BaselineWordSelection) { (Resolve-Path -LiteralPath $BaselineWordSelection).ProviderPath } else { $selection }
    if ([IO.Path]::GetExtension($baseline) -ne '.cs' -or !(Test-Path -LiteralPath $baseline -PathType Leaf)) { throw 'Baseline must be a WordSelection.cs source file.' }
    $baselineCopy = Join-Path $build 'baseline-WordSelection.cs'
    Copy-Item -LiteralPath $baseline -Destination $baselineCopy -Force
    Copy-Item -LiteralPath $baselineCopy -Destination (Join-Path $run 'WordSelection.baseline.cs')
    Copy-Item -LiteralPath $selection -Destination (Join-Path $run 'WordSelection.candidate.cs')
    $sameSource = (Get-FileHash -LiteralPath $baselineCopy -Algorithm SHA256).Hash -eq (Get-FileHash -LiteralPath $selection -Algorithm SHA256).Hash
    $runner = Join-Path $build 'WordPerformanceTests.exe'
    $baselineLibrary = Join-Path $build 'WordPerformanceBaseline.dll'
    $testSources = @('tests/integration/NativeWordBindingTests.cs','tests/integration/WordPerformanceTests.cs','tests/integration/WordPerformanceSupport.cs','tests/contracts/WordPerformanceContracts.cs') | ForEach-Object { Join-Path $root $_ }
    $common = @('/nologo','/optimize+','/platform:x64','/codepage:65001','/langversion:5','/r:System.Web.Extensions.dll') + $references
    $arguments = $common + @('/target:exe','/main:KeyJaya.WordPerformanceTests',('/out:' + $runner)) + $runtime + $testSources
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Current Word comparison compilation failed.' }
    $baselineInputs = @($runtime | ForEach-Object { if ([IO.Path]::GetFileName($_) -eq 'WordSelection.cs') { $baselineCopy } else { $_ } })
    $arguments = $common + @('/target:library',('/out:' + $baselineLibrary)) + $baselineInputs + $testSources
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Baseline Word adapter is incompatible with this comparison fixture.' }
    $fixture = Join-Path $root 'tests/fixtures/word-performance.json'
    $sources = @($runtime + $testSources | ForEach-Object { [pscustomobject]@{ relative = $_.Substring($root.Length + 1); sha256 = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash } })
    $manifest = [pscustomobject]@{
        schema = 1
        same_adapter_source = $sameSource
        baseline_adapter_sha256 = (Get-FileHash -LiteralPath $baselineCopy -Algorithm SHA256).Hash
        candidate_adapter_sha256 = (Get-FileHash -LiteralPath $selection -Algorithm SHA256).Hash
        baseline_binary_sha256 = (Get-FileHash -LiteralPath $baselineLibrary -Algorithm SHA256).Hash
        candidate_binary_sha256 = (Get-FileHash -LiteralPath $runner -Algorithm SHA256).Hash
        fixture_sha256 = (Get-FileHash -LiteralPath $fixture -Algorithm SHA256).Hash
        iterations = $Iterations; warmups = $Warmups; selected_cases = $Cases; sources = $sources
        coverage = 'Known-output body discovery/selection, native transaction and continuation finalization; excludes engine/mode selection, resident dispatch, physical Ctrl, OS input replay, paste UI and Modern Comments'
    }
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $run 'preparation.json') -Encoding UTF8
    & $runner '--self-test' $root $fixture (Join-Path $run 'self-tests.json')
    if ($LASTEXITCODE -ne 0) { throw 'Word comparison self-tests failed.' }
    $denial = Start-Process -FilePath $runner -ArgumentList '--not-authorized' -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $run 'denied-stdout.txt') -RedirectStandardError (Join-Path $run 'denied-stderr.txt')
    # Windows PowerShell 5.1 can lose ExitCode after a timed wait unless the
    # process handle has been acquired before the child exits.
    $null = $denial.Handle
    if (!$denial.WaitForExit(5000)) { $denial.Kill(); throw 'The default-deny probe did not finish.' }
    if ($denial.ExitCode -ne 2) { throw 'The native runner failed its default-deny entry-point check.' }
    if (!$AllowDesktop) {
        Write-Output ('Prepared and self-tested. Word was not started. Evidence: ' + $run)
        return
    }
    $report = Join-Path $run 'results.json'
    $sourceMode = if ($sameSource) { 'same' } else { 'different' }
    $caseSelection = if ($Cases.Count) { $Cases -join ',' } else { 'all' }
    $nativeArguments = @('--desktop',$root,$fixture,$baselineLibrary,$report,[string]$Iterations,[string]$Warmups,$sourceMode,$caseSelection)
    # Windows paths cannot contain a double quote; these arguments contain no
    # trailing directory separator and need only outer quoting for spaces.
    $quoted = @($nativeArguments | ForEach-Object { '"' + $_ + '"' })
    $process = Start-Process -FilePath $runner -ArgumentList $quoted -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $run 'stdout.txt') -RedirectStandardError (Join-Path $run 'stderr.txt')
    $null = $process.Handle
    $started = $process.StartTime
    $clock = [Diagnostics.Stopwatch]::StartNew()
    while (!$process.WaitForExit(500)) {
        if ($clock.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
            $current = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($null -ne $current -and $current.Path -eq $runner -and $current.StartTime -eq $started) { Stop-Process -Id $process.Id -Force }
            throw ('Word comparison timed out; only its test helper was stopped. A scratch Word window may remain for review. Evidence: ' + $run)
        }
    }
    if ($process.ExitCode -ne 0) { throw ('Word comparison failed; inspect ' + $run) }
    $results = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    if ($results.status -ne 'passed') { throw 'Incomplete native Word comparison.' }
    if ($sameSource) { Write-Output 'Same-source calibration: timing differences measure variability; no speedup claim.' }
    $results.scenarios | Format-Table id,tracking,baseline_median_ms,candidate_median_ms,median_improvement_percent -AutoSize
    Write-Output ('Validated native Word comparison. Evidence: ' + $run)
} finally { $lock.Dispose() }
