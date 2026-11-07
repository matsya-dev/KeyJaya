<#
.SYNOPSIS
Compiles an opt-in passive monitor of Ctrl/focus metadata in a prepared Acrobat field.
.DESCRIPTION
Requires explicit user permission for -AllowDesktop. No text is read, keys sent
or suppressed, windows activated, clipboard accessed, or resident started.
#>
param([switch]$AllowDesktop)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $projectRoot 'build-common.ps1')
$output = Join-Path $projectRoot 'out/acrobat-search'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$runner = Join-Path $output 'AcrobatControlTapProbe.exe'
$arguments = @('/nologo', '/optimize+', '/platform:x64', '/codepage:65001',
    '/langversion:5', '/r:System.Web.Extensions.dll', '/main:KeyJaya.AcrobatControlTapProbe', ('/out:' + $runner)) +
    $references + $runtime + (Join-Path $projectRoot 'tests/integration/AcrobatControlTapProbe.cs')
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Passive monitor compilation failed.' }
if ($AllowDesktop) { & $runner --allow-desktop } else { Write-Output 'Passive monitor compiled; no windows or input inspected.' }
