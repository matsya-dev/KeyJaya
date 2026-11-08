<#
.SYNOPSIS
Compiles and runs the registered, noninteractive C# regression suites.
.DESCRIPTION
Each invocation gets an owned workspace beneath out/w. Fixtures are recreated
and removed between suites; evidence remains in out/reports and out/tests.
Nothing opens a working document, starts resident hooks, modifies
the installed profile, or builds an installer. Results are always a JSON array.
.PARAMETER Suite
Run only the named registered suites.
.PARAMETER Full
Run every suite in tests/suites.json, including the large corpus checks.
.PARAMETER KeepArtifacts
Keep the full scratch workspace instead of removing regenerable fixtures.
#>
param(
    [string[]]$Suite = @(),
    [switch]$Full,
    [switch]$KeepArtifacts
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
. (Join-Path $root 'build-common.ps1')
. (Join-Path $root 'tools/workspace.ps1')
$registry = Get-Content -LiteralPath (Join-Path $root 'tests/suites.json') -Raw | ConvertFrom-Json
$available = [string[]]$registry
if ($Suite.Count -gt 0) {
    $names = $Suite
} elseif ($Full) {
    $names = $available
} else {
    $names = @(
        'WordSnapshotTests', 'InDesignFindSyntaxTests', 'SingleLetterTests',
        'CompoundCaretTests', 'SlashWordTests', 'NumberCtrlTests', 'TriggerEditorTests',
        'BookStyleTests', 'ScriptLanguageDetectionTests', 'ItransTests', 'HindiTests',
        'CorrectionRaceTests', 'ShortcutSettingsTests', 'NativeKeyTests', 'ToggleTests'
    )
}
foreach ($name in $names) {
    if ($name -notin $available) { throw ('Unknown or interactive test suite: ' + $name) }
}

$output = Assert-KjOutputPath $root (Join-Path $root 'out/tests')
New-Item -ItemType Directory -Path $output -Force | Out-Null
$work = New-KjWork $root 'tests' -KeepArtifacts:$KeepArtifacts
$success = $false
$results = @()
try {
    Write-KjJson (Join-Path $output 'results.json') @($results)
    Write-KjJson (Join-Path $output 'latest.json') @{ evidence=$work.Evidence; workspace=$work.Path; suites=@($names) }
    foreach ($name in $names) {
        # Inspection mode still needs a fresh fixture for each suite. Keep the
        # component short enough for nested .NET Framework distribution tests.
        $suitePath = if ($KeepArtifacts) { Join-Path $work.Path ('s' + $results.Count.ToString('00')) } else { $work.Path }
        $fixture = Join-Path $suitePath 'fixture'
        $inputs = Join-Path $suitePath 'inputs'
        $runner = Join-Path $suitePath ($name + '.exe')
        $suitePassed = $false
        $recorded = $false
        try {
            New-Item -ItemType Directory -Path $fixture, $inputs -Force | Out-Null
            foreach ($folder in @('assets','data')) {
                Copy-Item -LiteralPath (Join-Path $root $folder) -Destination $fixture -Recurse -Force
            }
            Get-ChildItem -LiteralPath (Join-Path $root 'tests/fixtures') -File |
                ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $inputs -Force }
            New-Item -ItemType Directory -Path (Join-Path $fixture 'config') -Force | Out-Null
            Get-ChildItem -LiteralPath (Join-Path $root 'defaults') -File |
                ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $fixture 'config') -Force }
            Copy-Item -LiteralPath (Join-Path $root 'assets/short-trigger-defaults.yml') -Destination (Join-Path $fixture 'config/short-triggers.yml') -Force
            Copy-Item -LiteralPath (Join-Path $inputs 'english-guard.txt') -Destination (Join-Path $fixture 'data/english-guard.txt') -Force
            $compilerArguments = @(
                '/nologo', '/optimize+', '/platform:x64', '/codepage:65001', '/langversion:5',
                '/r:System.Web.Extensions.dll'
            ) + $references + @(
                ('/main:KeyJaya.' + $name)
                ('/out:' + $runner)
            ) + $runtime + (Join-Path $root ('tests/csharp/' + $name + '.cs'))
            if ($name -eq 'SelectionTests') {
                $compilerArguments += Join-Path $root 'tests/contracts/EditorContracts.cs'
            }
            if ($name -eq 'WordSnapshotTests') {
                $compilerArguments += Join-Path $root 'tests/contracts/WordApplicationTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/WordCommentFocusTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/ModernWordSetupTests.cs'
                $compilerArguments += Join-Path $root 'tests/integration/ModernWordSetup.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/FocusMonitorTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/InputHookThreadTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/WordNavigationTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/WordSearchFieldTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/WordTransactionReuseTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/WordNativeTimingTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/WordRevisionEnumerationTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/WordRevisionBatchTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/WordFormattingCycleTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/WordPerformanceContracts.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/WordBatchingTests.cs'
                $compilerArguments += Join-Path $root 'tests/integration/WordPerformanceSupport.cs'
            }
            if ($name -eq 'ExcelCellTests') {
                $compilerArguments += Join-Path $root 'tests/contracts/ExcelTransactionTests.cs'
            }
            if ($name -eq 'NativeKeyTests') {
                $compilerArguments += Join-Path $root 'tests/contracts/NativeEditTests.cs'
                $compilerArguments += Join-Path $root 'tests/contracts/ClipboardSnapshotTests.cs'
            }
            if ($name -eq 'ShortcutSettingsTests') {
                $compilerArguments += Join-Path $root 'tests/contracts/WordShortcutTests.cs'
            }
            if ($name -eq 'BookStyleTests') {
                $compilerArguments += Join-Path $root 'tests/contracts/ControlItalicTests.cs'
            }
            if ($name -eq 'StandaloneTests') {
                $compilerArguments += @('src/DistributionFiles.cs', 'src/InstallTransaction.cs', 'tests/contracts/DistributionTests.cs') |
                    ForEach-Object { Join-Path $root $_ }
            }
            Set-KjInputs $work (@($work.Inputs) + @($compilerArguments | Where-Object { $_ -like '*.cs' }) + @((Join-Path $root 'test.ps1'),(Join-Path $root 'build-common.ps1'),(Join-Path $root 'data/manifest.json')))
            $compiled = Invoke-KjLogged $work $compiler $compilerArguments ($name + '-compiler.txt')
            if ($compiled.ExitCode -ne 0) { throw ($name + ' compilation failed: ' + $compiled.Output) }
            $extra = @()
            if ($name -in @('StringentTests', 'CopiedConversionTests', 'FontCyclingTests')) {
                $extra = @((Join-Path $inputs 'expanded-en_US-words.txt'))
            }
            if ($name -eq 'SanskritTests') { $extra = @((Join-Path $inputs 'english-prose.txt')) }
            if ($name -eq 'ScriptFeaturesTests') { $extra = @((Join-Path $inputs 'script-reference-passages.tsv')) }
            if ($name -eq 'ToggleTests') { $extra = @((Join-Path $inputs 'contraction-keys.txt')) }
            if ($name -eq 'SelectionTests') { $extra = @((Join-Path $inputs 'editor-contracts.json')) }
            if ($name -eq 'WordSnapshotTests') { $extra = @((Join-Path $inputs 'word-performance.json')) }
            Push-Location -LiteralPath $suitePath
            try { $run = Invoke-KjLogged $work $runner (@($fixture) + $extra) ($name + '.txt') }
            finally { Pop-Location }
            $results += @{ suite=$name; exit_code=$run.ExitCode; output=$run.Output }
            $recorded = $true
            Copy-Item -LiteralPath (Join-Path $work.Evidence ($name + '.txt')) -Destination (Join-Path $output ($name + '.txt')) -Force
            Write-Output ($name + ': ' + $run.Output)
            if ($run.ExitCode -ne 0) { throw ($name + ' failed') }
            $suitePassed = $true
        } catch {
            if (-not $recorded) { $results += @{ suite=$name; exit_code=1; output=$_.ToString() } }
            throw
        } finally {
            Write-KjJson (Join-Path $work.Evidence 'results.json') @($results)
            Write-KjJson (Join-Path $output 'results.json') @($results)
            if ($suitePassed -and -not $KeepArtifacts) { Clear-KjScratch $work }
        }
    }
    $success = $true
    Write-Output ('Passed ' + $names.Count + ' isolated automated suites. Evidence: ' + $work.Evidence)
} finally {
    Complete-KjWork $work $success
}
