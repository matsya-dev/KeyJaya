<#
.SYNOPSIS
Reads focused Acrobat control metadata only, after explicit user preparation.
.DESCRIPTION
Run while the user has focused an EMPTY Find or Advanced Search field in a
disposable PDF. Does not activate windows, send input, inspect field values,
read PDF text, use the clipboard, or change Acrobat preferences.
#>
param([switch]$AllowInspection, [switch]$CompileOnly,
    [ValidateRange(0, 60)][int]$WaitSeconds = 0)
$ErrorActionPreference = 'Stop'
if (!$AllowInspection -and !$CompileOnly) {
    Write-Output 'Inspection requires -AllowInspection and an empty, user-focused Acrobat search field.'
    exit 0
}
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class AcrobatFocusMetadata {
    [StructLayout(LayoutKind.Sequential)] public struct Info {
        public int Size, Flags;
        public IntPtr Active, Focus, Capture, Menu, Move, Caret;
        public int Left, Top, Right, Bottom;
    }
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
    [DllImport("user32.dll")] public static extern bool GetGUIThreadInfo(uint t, ref Info i);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder b, int n);
    [DllImport("user32.dll")] public static extern bool IsWindowUnicode(IntPtr h);
    [DllImport("user32.dll", EntryPoint="GetWindowLongW")] public static extern int Style(IntPtr h, int index);
    [DllImport("user32.dll", EntryPoint="GetClassLongPtrW")] public static extern IntPtr ClassModule(IntPtr h, int index);
    public static string ClassName(IntPtr h) { var b = new StringBuilder(128); GetClassName(h,b,b.Capacity); return b.ToString(); }
}
'@
if ($CompileOnly) {
    Write-Output 'Metadata helper compiled; no windows inspected.'
    exit 0
}
$clock = [Diagnostics.Stopwatch]::StartNew()
do {
    $owner = [AcrobatFocusMetadata]::GetForegroundWindow()
    [uint32]$ownerProcess = 0
    $thread = [AcrobatFocusMetadata]::GetWindowThreadProcessId($owner, [ref]$ownerProcess)
    $processName = ''
    try { $processName = (Get-Process -Id $ownerProcess -ErrorAction Stop).ProcessName } catch { }
    if ($processName -in @('Acrobat', 'AcroRd32')) { break }
    if ($clock.Elapsed.TotalSeconds -ge $WaitSeconds) {
        Write-Output 'Acrobat is not foreground; no field inspected.'
        exit 0
    }
    Start-Sleep -Milliseconds 250
} while ($true)
$info = New-Object AcrobatFocusMetadata+Info
$info.Size = [Runtime.InteropServices.Marshal]::SizeOf($info)
if (![AcrobatFocusMetadata]::GetGUIThreadInfo($thread, [ref]$info)) { throw 'Native focus unavailable.' }
$rows = @()
$node = [Windows.Automation.AutomationElement]::FocusedElement
for ($level = 0; $level -lt 8 -and $node; $level++) {
    $current = $node.Current
    if ($current.ProcessId -ne $ownerProcess) { break }
    $rows += [pscustomobject]@{
        level = $level; class = $current.ClassName
        automation_id = $current.AutomationId; control = $current.ControlType.ProgrammaticName
        native_window = $current.NativeWindowHandle; has_focus = $current.HasKeyboardFocus
        native_class = [AcrobatFocusMetadata]::ClassName([IntPtr]$current.NativeWindowHandle)
        enabled = $current.IsEnabled; password = $current.IsPassword
        patterns = @($node.GetSupportedPatterns() | ForEach-Object ProgrammaticName)
    }
    $node = [Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($node)
}
$after = New-Object AcrobatFocusMetadata+Info
$after.Size = [Runtime.InteropServices.Marshal]::SizeOf($after)
if ($owner -ne [AcrobatFocusMetadata]::GetForegroundWindow() -or
    ![AcrobatFocusMetadata]::GetGUIThreadInfo($thread, [ref]$after) -or
    $after.Focus -ne $info.Focus) { throw 'Focus changed; metadata discarded.' }
[pscustomobject]@{
    process = $processName; owner_class = [AcrobatFocusMetadata]::ClassName($owner)
    native_focus = $info.Focus.ToInt64(); native_focus_class = [AcrobatFocusMetadata]::ClassName($info.Focus)
    native_unicode = [AcrobatFocusMetadata]::IsWindowUnicode($info.Focus)
    native_style = ('{0:X8}' -f [AcrobatFocusMetadata]::Style($info.Focus, -16))
    native_class_module = $([AcrobatFocusMetadata]::ClassModule($info.Focus, -16)).ToInt64()
    controls = $rows
} | ConvertTo-Json -Depth 5
