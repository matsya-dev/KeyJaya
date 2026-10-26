<#
.SYNOPSIS
Compiles setup with an embedded, already prepared payload.
.DESCRIPTION
Build-time only. Does not execute setup, install files, register startup, or start the app.
Application and payload VERSION values must agree before compilation.
#>
param(
    [string]$ReleaseRoot = '',
    [string]$PayloadPath = '',
    [string]$OutputPath = ''
)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$kjRelease = if ($ReleaseRoot) { [IO.Path]::GetFullPath($ReleaseRoot) } else { Join-Path $taskRoot 'release' }
$kjPayload = if ($PayloadPath) { [IO.Path]::GetFullPath($PayloadPath) } else { Join-Path $taskRoot 'payload.zip' }
$kjSource = [IO.File]::ReadAllText((Join-Path $taskRoot 'src/Core.cs'))
$kjVersion = [regex]::Match($kjSource, 'Current\s*=\s*"([^"]+)"').Groups[1].Value
if (!$kjVersion -or ([IO.File]::ReadAllText((Join-Path $kjRelease 'VERSION')).Trim() -ne $kjVersion)) {
    throw 'Setup payload version must match current source'
}
$kjOutput = if ($OutputPath) { [IO.Path]::GetFullPath($OutputPath) } else { Join-Path $taskRoot ('packages/KeyJaya-Setup-v' + $kjVersion + '.exe') }
New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($kjOutput)) | Out-Null

$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$framework = Split-Path $compiler
$setupReferences = @(
    '/r:System.Web.Extensions.dll'
    '/r:System.Windows.Forms.dll'
    '/r:System.Drawing.dll'
    ('/r:' + (Join-Path $framework 'System.IO.Compression.dll'))
)
. (Join-Path $taskRoot 'build-common.ps1')
$setupArguments = @(
    '/nologo'
    '/optimize+'
    '/platform:x64'
    '/target:winexe'
    '/main:KeyJaya.Setup'
    ('/win32manifest:' + (Join-Path $taskRoot 'src/app.manifest'))
    ('/win32icon:' + (Join-Path $kjRelease 'assets/keyjaya.ico'))
    ('/resource:' + $kjPayload + ',payload.zip')
    ('/out:' + $kjOutput)
) + $setupReferences + $setupSources
& $compiler @setupArguments
if ($LASTEXITCODE -ne 0) { throw 'Setup compilation failed' }
Write-Output ('Built KeyJaya ' + $kjVersion + ' Setup at ' + $kjOutput)

# Regression executables are produced only by test.ps1, not this build.
