<#
.SYNOPSIS
Compiles a read-only Excel accessibility-capability probe; use ReadOnly to run it.
.DESCRIPTION
Does not start Excel, open/read workbooks, read cell text, move focus, send input
or change settings. It inspects only bounded native/editor accessibility metadata
in existing Excel windows, with a three-second provider deadline.
#>
param([switch]$ReadOnly)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $projectRoot 'build-common.ps1')
$run = Join-Path $projectRoot ('out/excel-capabilities/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$runner = Join-Path $run 'ExcelCapabilities.exe'
$arguments = @('/nologo','/platform:x64','/r:System.Web.Extensions.dll',('/out:' + $runner)) + $references + (Join-Path $PSScriptRoot 'ExcelCapabilities.cs')
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Read-only Excel probe compilation failed.' }
if (!$ReadOnly) { Write-Output ('Compiled only: ' + $runner); return }
& $runner | Tee-Object -FilePath (Join-Path $run 'capabilities.json')
if ($LASTEXITCODE -ne 0) { throw 'Read-only Excel probe failed.' }
Write-Output ('Evidence: ' + $run)
