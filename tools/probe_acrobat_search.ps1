<#
.SYNOPSIS
Compiles a nonresident library; optionally checks one user-prepared search field.
.DESCRIPTION
Text inspection requires explicit permission and the exact synthetic query krsna
in a focused Acrobat Find/Advanced Search field. No input, clipboard, settings,
window activation, search execution, resident startup or installer is involved.
Reports status and lengths only, never PDF text or the field's contents.
AllowConversion additionally requires permission to select and replace that
prepared query using the production transaction. It does not execute a search.
#>
param([switch]$AllowInspection, [switch]$AllowConversion,
    [ValidateRange(0, 60)][int]$WaitSeconds = 0)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $projectRoot 'build-common.ps1')
$output = Join-Path $projectRoot 'out/acrobat-search'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$library = Join-Path $output 'KeyJaya.dll'
$arguments = @('/nologo', '/optimize+', '/platform:x64', '/codepage:65001',
    '/langversion:5', '/target:library', '/r:System.Web.Extensions.dll', ('/out:' + $library)) + $references + $runtime
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Probe library compilation failed.' }
if (!$AllowInspection -and !$AllowConversion) {
    Write-Output 'Nonresident library compiled; no windows inspected.'
    exit 0
}
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
[void][Reflection.Assembly]::LoadFrom($library)
$clock = [Diagnostics.Stopwatch]::StartNew()
do {
    $owner = [KeyJaya.Native]::GetForegroundWindow()
    [uint32]$ownerProcess = 0
    [void][KeyJaya.Native]::GetWindowThreadProcessId($owner, [ref]$ownerProcess)
    if ([KeyJaya.Native]::ProcessImageIs($ownerProcess, 'Acrobat.exe') -or
        [KeyJaya.Native]::ProcessImageIs($ownerProcess, 'AcroRd32.exe')) { break }
    if ($clock.Elapsed.TotalSeconds -ge $WaitSeconds) {
        Write-Output 'Acrobat is not foreground; no text inspected.'
        exit 0
    }
    Start-Sleep -Milliseconds 250
} while ($true)
$identity = [KeyJaya.Native]::FocusIdentity()
if (!$identity) { throw 'No native focus identity.' }
$focus = [Windows.Automation.AutomationElement]::FocusedElement
if (!$focus -or $focus.Current.ProcessId -ne $ownerProcess -or !$focus.Current.HasKeyboardFocus) {
    throw 'No verified Acrobat focus.'
}
$handle = [IntPtr]$focus.Current.NativeWindowHandle
$kind = New-Object Text.StringBuilder 128
[void][KeyJaya.Native]::GetClassName($handle, $kind, $kind.Capacity)
if ($handle -eq [IntPtr]::Zero -or $kind.ToString() -ne 'Edit') { throw 'Expected the prepared native Edit field.' }
$value = $null
$read = [KeyJaya.Native]::TryReadEditText($handle, [ref]$value)
$readError = $(if ($read) { 0 } else { [Runtime.InteropServices.Marshal]::GetLastWin32Error() })
[int]$start = -1; [int]$end = -1
$selection = $true
try { [KeyJaya.Native]::GetEditSelection($handle, [ref]$start, [ref]$end) } catch { $selection = $false }
$selectionError = $(if ($selection) { 0 } else { [Runtime.InteropServices.Marshal]::GetLastWin32Error() })
$details = New-Object 'Collections.Generic.List[string]'
[KeyJaya.SafeEdit]::Trace = { param($message) $details.Add($message) }
# Production discovery runs only after the exact synthetic buffer is proved.
$caret = $null
if ($read -and $value -ceq 'krsna' -and
    [KeyJaya.EditGuard]::SameFocus($identity, [KeyJaya.Native]::FocusIdentity())) {
    $caret = [KeyJaya.SafeEdit]::ReadCaret($owner, $false)
}
$selected = $false
$sent = $false
$converted = $false
$unchanged = $false
if ($AllowConversion) {
    if (!$caret -or !$caret.Edit.AtOwnedCaret() -or $start -ne 5 -or $end -ne 5) {
        throw 'Conversion refused: expected exactly krsna with its caret at the end.'
    }
    $context = {
        $owner -eq [KeyJaya.Native]::GetForegroundWindow() -and
        [KeyJaya.EditGuard]::SameFocus($identity, [KeyJaya.Native]::FocusIdentity())
    }.GetNewClosure()
    try {
        $selected = $caret.Edit.Select($context)
        if ($selected -and (& $context) -and $caret.Edit.ReadyForPaste()) {
            $replacement = 'k' + [char]0x1e5b + [char]0x1e63 + [char]0x1e47 + 'a'
            $sent = [KeyJaya.Native]::TypeText($replacement)
            Start-Sleep -Milliseconds 150
            $after = $null
            if ((& $context) -and [KeyJaya.Native]::TryReadEditText($handle, [ref]$after)) {
                $converted = $after -ceq $replacement
                $unchanged = $after -ceq 'krsna'
            }
        }
    } finally {
        if (!$sent -and (& $context)) { $null = $caret.Edit.RestoreCaret() }
    }
}
if ($owner -ne [KeyJaya.Native]::GetForegroundWindow() -or
    ![KeyJaya.EditGuard]::SameFocus($identity, [KeyJaya.Native]::FocusIdentity())) { throw 'Focus changed; result discarded.' }
[pscustomobject]@{
    read_succeeded = $read; read_error = $readError
    characters = $(if ($read) { $value.Length } else { -1 })
    synthetic_query_matches = ($read -and $value -ceq 'krsna')
    selection_read = $selection; selection_error = $selectionError; start = $start; end = $end
    caret_discovered = ($null -ne $caret)
    caret_characters = $(if ($caret) { $caret.Text.Length } else { -1 })
    caret_owned = $(if ($caret) { $caret.Edit.AtOwnedCaret() } else { $false })
    font_available = $(if ($caret) { $null -ne $caret.Font } else { $false })
    conversion_requested = [bool]$AllowConversion; selection_verified = $selected
    input_dispatched = $sent; conversion_verified = $converted; query_unchanged = $unchanged
    diagnostics = @($details)
} | ConvertTo-Json -Depth 4
