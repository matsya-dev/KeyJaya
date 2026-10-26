<#
.SYNOPSIS
Tests production setup rollback without building or executing an installer.
.DESCRIPTION
Embeds a source-data fixture in a test-only library, then invokes Install with
test=true. Writes only beneath a fresh out/ directory and the setup diagnostic
log directory. Never registers startup, launches a resident, or opens documents.
#>
param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $projectRoot 'build-common.ps1')
$run = Join-Path $projectRoot ('out/setup-transaction/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$payload = Join-Path $run 'fixture.zip'
Add-Type -AssemblyName System.IO.Compression
$stream = [IO.File]::Create($payload)
$archive = New-Object IO.Compression.ZipArchive($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    $files = @((Join-Path $projectRoot 'VERSION'), (Join-Path $projectRoot 'assets/short-trigger-defaults.yml'), (Join-Path $projectRoot 'assets/distribution-exclusions.txt'))
    $files += Get-ChildItem -LiteralPath (Join-Path $projectRoot 'data') -File |
        Where-Object { $_.Extension -in @('.dat', '.idx', '.hash', '.tsv') } | ForEach-Object { $_.FullName }
    foreach ($file in $files) {
        $relative = $file.Substring($projectRoot.Length + 1).Replace('\', '/')
        $entry = $archive.CreateEntry($relative)
        $output = $entry.Open()
        $input = [IO.File]::OpenRead($file)
        try { $input.CopyTo($output) } finally { $input.Dispose(); $output.Dispose() }
    }
} finally { $archive.Dispose(); $stream.Dispose() }

# Library target has no installer entry point. Use the exact setup source list
# shared with setup-build.ps1, plus this narrowly scoped integration suite.
$library = Join-Path $run 'SetupTransactionTests.dll'
$compilerArguments = @('/nologo', '/target:library', '/platform:x64', '/codepage:65001', '/r:System.Web.Extensions.dll', '/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', ('/r:' + (Join-Path $framework 'System.IO.Compression.dll')), ('/resource:' + $payload + ',payload.zip'), ('/out:' + $library)) + $setupSources + (Join-Path $projectRoot 'tests/integration/SetupRetirementTests.cs')
& $compiler @compilerArguments
if ($LASTEXITCODE -ne 0) { throw 'Setup test library compilation failed.' }
$runnerSource = Join-Path $run 'Runner.cs'
[IO.File]::WriteAllText($runnerSource, 'class Runner { [System.STAThread] static int Main(string[] args) { return KeyJaya.SetupRetirementTests.Run(args[0], args[1]); } }')
$runner = Join-Path $run 'Runner.exe'
& $compiler '/nologo' '/platform:x64' ('/r:' + $library) ('/out:' + $runner) $runnerSource
if ($LASTEXITCODE -ne 0) { throw 'Setup test runner compilation failed.' }
& $runner $projectRoot (Join-Path $run 'fixtures') 2>&1 | Tee-Object -FilePath (Join-Path $run 'results.txt')
if ($LASTEXITCODE -ne 0) { throw 'Setup retirement rollback test failed.' }
Write-Output ('Evidence: ' + $run)
