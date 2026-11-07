<#
.SYNOPSIS
Opt-in Word editor control metadata; no document/comment text or input.
.DESCRIPTION
Compile-only by default. With separately authorized AllowInspection, the user
focuses a synthetic comment editor in a disposable Word document. Inspect focus,
native caret and pointer hit ancestors only inside that foreground Word process.
Never reads Name/Value/Text, activates windows, sends keys, or uses the clipboard.
Run in Windows PowerShell 5.1. Callers should bound the helper process lifetime.
#>
param([switch]$AllowInspection, [switch]$CompileOnly,
    [ValidateRange(0,60)][int]$WaitSeconds = 0)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,WindowsBase
Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class WordEditorMetadata {
 [StructLayout(LayoutKind.Sequential)] public struct Info {
  public int Size,Flags;
  public IntPtr Active,Focus,Capture,Menu,Move,Caret;
  public int Left,Top,Right,Bottom;
 }
 [StructLayout(LayoutKind.Sequential)] public struct Point { public int X,Y; }
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll")] public static extern bool GetGUIThreadInfo(uint t,ref Info i);
 [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr w,ref Point p);
 [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point p);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr w,StringBuilder s,int n);
 public static string Class(IntPtr w) {var s=new StringBuilder(128);GetClassName(w,s,s.Capacity);return s.ToString();}
}
'@
if ($CompileOnly -or !$AllowInspection) {
    Write-Output 'Word metadata helper compiled; no windows inspected.'
    exit 0
}
$clock = [Diagnostics.Stopwatch]::StartNew()
do {
    $owner = [WordEditorMetadata]::GetForegroundWindow()
    [uint32]$wordProcess = 0
    $thread = [WordEditorMetadata]::GetWindowThreadProcessId($owner,[ref]$wordProcess)
    $name = ''
    try { $name = (Get-Process -Id $wordProcess -ErrorAction Stop).ProcessName } catch { }
    if ($name -eq 'WINWORD') { break }
    if ($clock.Elapsed.TotalSeconds -ge $WaitSeconds) {
        Write-Output 'Word is not foreground; no editor inspected.'
        exit 0
    }
    Start-Sleep -Milliseconds 200
} while ($true)
$before = New-Object WordEditorMetadata+Info
$before.Size = [Runtime.InteropServices.Marshal]::SizeOf($before)
if (![WordEditorMetadata]::GetGUIThreadInfo($thread,[ref]$before)) { throw 'Native Word focus unavailable.' }
$properties = @('ProcessId','AutomationId','ClassName','ControlType','NativeWindowHandle','HasKeyboardFocus','IsEnabled','IsPassword')
$request = New-Object Windows.Automation.CacheRequest
$request.TreeScope = [Windows.Automation.TreeScope]::Element
$request.TreeFilter = [Windows.Automation.Automation]::RawViewCondition
foreach ($property in $properties) { $request.Add([Windows.Automation.AutomationElement]::($property+'Property')) }
function ReadAncestors($node) {
    $rows = @()
    for ($depth=0; $depth -lt 18 -and $node; $depth++) {
        $cached = $node.GetUpdatedCache($request)
        $info = $cached.Cached
        if ($info.ProcessId -ne $wordProcess) { break }
        $rows += [pscustomobject]@{
            depth=$depth; control=$info.ControlType.ProgrammaticName
            automation_id=$info.AutomationId; class=$info.ClassName
            native_window=$info.NativeWindowHandle; has_focus=$info.HasKeyboardFocus
            enabled=$info.IsEnabled; password=$info.IsPassword
            patterns=@($node.GetSupportedPatterns() | ForEach-Object ProgrammaticName)
        }
        if ($info.IsPassword) { break }
        $node = [Windows.Automation.TreeWalker]::RawViewWalker.GetParent($cached,$request)
    }
    return ,$rows
}
$paths = @()
$paths += [pscustomobject]@{ source='focused'; controls=(ReadAncestors ([Windows.Automation.AutomationElement]::FocusedElement)) }
if ($before.Caret -ne [IntPtr]::Zero) {
    $point = New-Object WordEditorMetadata+Point
    $point.X = $before.Left
    $point.Y = $before.Top + [Math]::Max(0,[int](($before.Bottom-$before.Top)/2))
    if ([WordEditorMetadata]::ClientToScreen($before.Caret,[ref]$point)) {
        try { $paths += [pscustomobject]@{ source='native_caret'; controls=(ReadAncestors ([Windows.Automation.AutomationElement]::FromPoint((New-Object Windows.Point($point.X,$point.Y))))) } } catch { }
    }
}
$point = New-Object WordEditorMetadata+Point
if ([WordEditorMetadata]::GetCursorPos([ref]$point)) {
    try { $paths += [pscustomobject]@{ source='pointer'; controls=(ReadAncestors ([Windows.Automation.AutomationElement]::FromPoint((New-Object Windows.Point($point.X,$point.Y))))) } } catch { }
}
$after = New-Object WordEditorMetadata+Info
$after.Size = [Runtime.InteropServices.Marshal]::SizeOf($after)
if ($owner -ne [WordEditorMetadata]::GetForegroundWindow() -or
    ![WordEditorMetadata]::GetGUIThreadInfo($thread,[ref]$after) -or
    $before.Focus -ne $after.Focus) { throw 'Focus changed; metadata discarded.' }
[pscustomobject]@{
    owner_class=[WordEditorMetadata]::Class($owner)
    native_focus_class=[WordEditorMetadata]::Class($before.Focus)
    paths=$paths
    scope='Control metadata only; no Name, Value, Text, input or clipboard'
} | ConvertTo-Json -Depth 8
