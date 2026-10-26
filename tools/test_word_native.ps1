<#
.SYNOPSIS
Opt-in native Word fault tests in a new, exclusively owned Word process.
.DESCRIPTION
Requires desktop Word and a quiet keyboard. Opens only its own scratch document,
briefly delays that new Word server and tests formatting, undo, focus changes,
disconnection and input-queue cleanup. Existing documents and the installed app
are untouched. No hooks, keyboard injection, clipboard or installer are used.
Also checks classic comments and threaded replies in Draft and Print Layout,
including Unicode/Balarama, cycling, formatting, native undo and continued typing.
.PARAMETER AllowDesktop
Required to run the native test; otherwise only compile the test executable.
.PARAMETER DelayMilliseconds
Bounded Word-server delay; an independent thread resumes only the owned server.
.PARAMETER PhysicalTyping
Wait for a real Ctrl tap and immediate typing in the new scratch document.
Uses the running resident without editing its settings or injecting keystrokes.
#>
param(
    [switch]$AllowDesktop,
    [ValidateRange(350, 10000)][int]$DelayMilliseconds = 350,
    [switch]$PhysicalTyping
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $projectRoot 'build-common.ps1')
$run = Join-Path $projectRoot ('out/native-word/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
$runner = Join-Path $run 'NativeWordBindingTests.exe'
$arguments = @('/nologo', '/platform:x64', '/codepage:65001', '/r:System.Web.Extensions.dll', '/main:KeyJaya.NativeWordBindingTests', ('/out:' + $runner)) + $references + $runtime + (Join-Path $projectRoot 'tests/integration/NativeWordBindingTests.cs')
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Native Word test compilation failed.' }
if (!$AllowDesktop) {
    Write-Output ('Compiled only. Native Word not started: ' + $runner)
    return
}
$testArguments = @($projectRoot, (Join-Path $run 'scratch'), [string]$DelayMilliseconds)
if ($PhysicalTyping) { $testArguments += 'physical' }
& $runner @testArguments 2>&1 | Tee-Object -FilePath (Join-Path $run 'results.txt')
if ($LASTEXITCODE -ne 0) { throw 'Native Word interoperability test failed; retain evidence under out/native-word.' }
Write-Output ('Evidence: ' + $run)
