<#
.SYNOPSIS
Compiles and runs the registered, noninteractive C# regression suites.
.DESCRIPTION
Each invocation gets a fresh fixture beneath out/tests. Source defaults are reset
between suites. Nothing opens a working document, starts resident hooks, modifies
the installed profile, or builds an installer. Results are always a JSON array.
.PARAMETER Suite
Run only the named registered suites.
.PARAMETER Full
Run every suite in tests/suites.json, including the large corpus checks.
#>
param(
    [string[]]$Suite = @(),
    [switch]$Full
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
. (Join-Path $root 'build-common.ps1')
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

$output = Join-Path $root 'out/tests'
$fixture = Join-Path $output ('fixture-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output, $fixture -Force | Out-Null
foreach ($folder in @('assets', 'data')) {
    Copy-Item -LiteralPath (Join-Path $root $folder) -Destination $fixture -Recurse -Force
}
$inputs = Join-Path $output 'inputs'
New-Item -ItemType Directory -Path $inputs -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $root 'tests/fixtures') -File |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $inputs -Force }
Copy-Item -LiteralPath (Join-Path $inputs 'english-guard.txt') -Destination (Join-Path $fixture 'data/english-guard.txt') -Force

$results = @()
foreach ($name in $names) {
    New-Item -ItemType Directory -Path (Join-Path $fixture 'config') -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $root 'defaults') -File |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $fixture 'config') -Force }
    Copy-Item -LiteralPath (Join-Path $root 'assets/short-trigger-defaults.yml') -Destination (Join-Path $fixture 'config/short-triggers.yml') -Force

    $runner = Join-Path $output ($name + '.exe')
    $compilerArguments = @(
        '/nologo', '/optimize+', '/platform:x64', '/codepage:65001',
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
        $compilerArguments += Join-Path $root 'tests/contracts/WordNavigationTests.cs'
        $compilerArguments += Join-Path $root 'tests/contracts/WordTransactionReuseTests.cs'
    }
    if ($name -eq 'ExcelCellTests') {
        $compilerArguments += Join-Path $root 'tests/contracts/ExcelTransactionTests.cs'
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
    & $compiler @compilerArguments
    if ($LASTEXITCODE -ne 0) { throw ($name + ' compilation failed') }

    $extra = @()
    if ($name -in @('StringentTests', 'CopiedConversionTests', 'FontCyclingTests')) {
        $extra = @((Join-Path $inputs 'expanded-en_US-words.txt'))
    }
    if ($name -eq 'SanskritTests') { $extra = @((Join-Path $inputs 'english-prose.txt')) }
    if ($name -eq 'ScriptFeaturesTests') { $extra = @((Join-Path $inputs 'script-reference-passages.tsv')) }
    if ($name -eq 'ToggleTests') { $extra = @((Join-Path $inputs 'contraction-keys.txt')) }
    if ($name -eq 'SelectionTests') { $extra = @((Join-Path $inputs 'editor-contracts.json')) }

    $result = & $runner $fixture @extra 2>&1
    $exitCode = $LASTEXITCODE
    $result | Set-Content -LiteralPath (Join-Path $output ($name + '.txt')) -Encoding UTF8
    $results += @{ suite = $name; exit_code = $exitCode; output = ($result -join "`n") }
    ConvertTo-Json -InputObject @($results) -Depth 4 |
        Set-Content -LiteralPath (Join-Path $output 'results.json') -Encoding UTF8
    Write-Output ($name + ': ' + ($result -join "`n"))
    if ($exitCode -ne 0) { throw ($name + ' failed') }
}
Write-Output ('Passed ' + $names.Count + ' isolated automated suites. Results: ' + (Join-Path $output 'results.json'))
