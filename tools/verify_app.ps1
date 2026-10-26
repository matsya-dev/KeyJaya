<#
.SYNOPSIS
Checks the compiled application through a noninteractive conversion probe.
.DESCRIPTION
Loads a private copy of the assembly beneath out/verification. Never calls the
resident entry point, installs the app, opens documents, or registers hooks.
#>
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $root 'build-common.ps1')

$app = Join-Path $root 'out/app'
$assembly = Join-Path $app 'engine/keyjaya-app.exe'
if (!(Test-Path -LiteralPath $assembly)) {
    throw 'Build the app before running this probe.'
}
$output = Join-Path $root 'out/verification/app-probe'
New-Item -ItemType Directory -Path $output -Force | Out-Null
Copy-Item -LiteralPath $assembly -Destination (Join-Path $output 'keyjaya-app.exe') -Force

$runner = Join-Path $output 'RepositoryBuildProbe.exe'
$compilerArguments = @(
    '/nologo', '/optimize+', '/platform:x64', '/codepage:65001'
    ('/r:' + $assembly)
    ('/out:' + $runner)
    (Join-Path $root 'tests/RepositoryBuildProbe.cs')
)
& $compiler @compilerArguments
if ($LASTEXITCODE -ne 0) { throw 'Compiled app probe compilation failed.' }
& $runner $app
if ($LASTEXITCODE -ne 0) { throw 'Compiled app probe failed.' }
