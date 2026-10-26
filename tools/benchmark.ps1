<#
.SYNOPSIS
Measures conversion-engine speed in a fresh, isolated configuration.
.DESCRIPTION
Compiles only a noninteractive runner. Never starts resident hooks, changes the
clipboard, launches an editor, builds an installer, or reads installed settings.
Reports first requests, warm percentiles and engine initialization separately.
.PARAMETER Iterations
Warm samples per case. At least 20; default 301.
.PARAMETER Check
Fail when a warm p95 exceeds the checked-in 150 ms engine budget.
#>
param(
    [ValidateRange(20, 10000)][int]$Iterations = 301,
    [switch]$Check
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $root 'build-common.ps1')
$output = Join-Path $root ('out/benchmarks/run-' + [Guid]::NewGuid().ToString('N'))
$fixture = Join-Path $output 'fixture'
New-Item -ItemType Directory -Path $fixture -Force | Out-Null
foreach ($name in @('assets', 'data')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination $fixture -Recurse
}
Copy-Item -LiteralPath (Join-Path $root 'defaults') -Destination (Join-Path $fixture 'config') -Recurse
Copy-Item -LiteralPath (Join-Path $root 'assets/short-trigger-defaults.yml') -Destination (Join-Path $fixture 'config/short-triggers.yml')
$runner = Join-Path $output 'ConversionBenchmarks.exe'
$arguments = @('/nologo', '/optimize+', '/platform:x64', '/codepage:65001', '/r:System.Web.Extensions.dll') + $references + @(
    '/main:KeyJaya.ConversionBenchmarks'
    ('/out:' + $runner)
) + $runtime + (Join-Path $root 'tests/benchmarks/ConversionBenchmarks.cs')
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Benchmark compilation failed' }
$report = Join-Path $output 'results.json'
& $runner $fixture $report (Join-Path $root 'tests/benchmarks/budgets.json') $Iterations
$code = $LASTEXITCODE
if ($code -ne 0 -and $code -ne 2) { throw 'Benchmark correctness or execution failed' }
$results = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$columns = @(
    'id'
    @{ Label = 'first_request_ms'; Expression = { [double]$_.first_request_ms }; FormatString = 'F3' }
    @{ Label = 'median_ms'; Expression = { [double]$_.median_ms }; FormatString = 'F3' }
    @{ Label = 'p95_ms'; Expression = { [double]$_.p95_ms }; FormatString = 'F3' }
    @{ Label = 'p99_ms'; Expression = { [double]$_.p99_ms }; FormatString = 'F3' }
)
$results.cases | Sort-Object p95_ms -Descending | Format-Table -Property $columns -AutoSize
Write-Output ('Report: ' + $report)
if ($Check -and $code -eq 2) { throw 'Conversion-engine speed budget exceeded; inspect the report' }
