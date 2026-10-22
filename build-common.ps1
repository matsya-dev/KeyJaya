# Shared compiler inputs for app, test and setup builds. No build runs on import.
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$framework = Split-Path $compiler
$core = @('Core','Vocabulary','Hindi','CopiedConversion','IndicScripts','Itrans','Configuration','ShortcutSettings','Number','SingleLetters','ResidentReplacement','SelectionFormatting') | ForEach-Object { Join-Path $PSScriptRoot ('src/' + $_ + '.cs') }
$runtime = $core + (@('LaunchRequests','TriggerEditorStore','TriggerEditorWindow','TrayMenu','TrayAppearance','WindowAppearance','CheatSheet','ShortcutWindow','AssemblyInfo','SelectionCommit','Native','InDesignSelection','InDesignFindSyntax','InDesignFindField','ExcelCellField','WordSelection','Runtime') | ForEach-Object { Join-Path $PSScriptRoot ('src/' + $_ + '.cs') })
$references = @('/r:System.Windows.Forms.dll','/r:System.Drawing.dll',('/r:' + (Join-Path $framework 'WPF/UIAutomationClient.dll')),('/r:' + (Join-Path $framework 'WPF/UIAutomationTypes.dll')),('/r:' + (Join-Path $framework 'WPF/WindowsBase.dll')))
