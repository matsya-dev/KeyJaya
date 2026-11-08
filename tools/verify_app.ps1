<#
.SYNOPSIS
Checks the compiled application through a noninteractive conversion probe.
.DESCRIPTION
Loads a private copy of the assembly in an owned out/w workspace. Never calls the
resident entry point, installs the app, opens documents, or registers hooks.
#>
param([switch]$KeepArtifacts)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $root 'build-common.ps1')
. (Join-Path $root 'tools/workspace.ps1')

$app = Join-Path $root 'out/app'
$assembly = Join-Path $app 'engine/keyjaya-app.exe'
if (!(Test-Path -LiteralPath $assembly)) {
    throw 'Build the app before running this probe.'
}
$work = New-KjWork $root 'app-probe' -KeepArtifacts:$KeepArtifacts
$success = $false
try {
$output = $work.Path
Set-KjInputs $work @($assembly,(Join-Path $root 'tests/RepositoryBuildProbe.cs'),(Join-Path $root 'tools/verify_app.ps1'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
Copy-Item -LiteralPath $assembly -Destination (Join-Path $output 'keyjaya-app.exe') -Force

$runner = Join-Path $output 'RepositoryBuildProbe.exe'
$compilerArguments = @(
    '/nologo', '/optimize+', '/platform:x64', '/codepage:65001'
    ('/r:' + $assembly)
    ('/out:' + $runner)
    (Join-Path $root 'tests/RepositoryBuildProbe.cs')
)
$compile = Invoke-KjLogged $work $compiler $compilerArguments 'compiler.txt'
if ($compile.ExitCode -ne 0) { throw 'Compiled app probe compilation failed.' }
$run = Invoke-KjLogged $work $runner @($app) 'results.txt'
Write-Output $run.Output
if ($run.ExitCode -ne 0) { throw 'Compiled app probe failed.' }
$success = $true
Write-Output ('Evidence: ' + $work.Evidence)
} finally { Complete-KjWork $work $success }
