# Shared compiler inputs for runtime, regression, and setup-source builds.
# Dot-sourcing declares paths only; it must not compile, install, or start anything.
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$framework = Split-Path $compiler

# Conversion/configuration code has no dependency on the resident entry point.
$coreNames = @(
    'Core'
    'IndexedStore'
    'Vocabulary'
    'Hindi'
    'CopiedConversion'
    'IndicScripts'
    'Itrans'
    'Configuration'
    'ShortcutSettings'
    'Number'
    'SingleLetters'
    'ResidentReplacement'
    'SelectionFormatting'
    'ControlItalics'
)
$core = $coreNames | ForEach-Object { Join-Path $PSScriptRoot ('src/' + $_ + '.cs') }

# Preserve the relative ordering of partial Core declarations. Editor adapters and
# orchestration are separate source files, but still compile into one resident.
$runtimeNames = @(
    'LaunchRequests'
    'TriggerEditorStore'
    'TriggerEditorWindow'
    'WordShortcutInspector'
    'WordShortcutWindow'
    'TrayMenu'
    'TrayAppearance'
    'WindowAppearance'
    'CheatSheet'
    'ShortcutWindow'
    'AssemblyInfo'
    'SelectionCommit'
    'Native'
    'InDesignSelection'
    'InDesignFindSyntax'
    'InDesignFindField'
    'ExcelCellField'
    'WordApplication'
    'WordCommentFocus'
    'WordSelection'
    'SafeEdit'
    'Runtime'
    'ResidentInput'
    'Program'
)
$runtime = $core + ($runtimeNames | ForEach-Object { Join-Path $PSScriptRoot ('src/' + $_ + '.cs') })
$references = @(
    '/r:System.Windows.Forms.dll'
    '/r:System.Drawing.dll'
    ('/r:' + (Join-Path $framework 'WPF/UIAutomationClient.dll'))
    ('/r:' + (Join-Path $framework 'WPF/UIAutomationTypes.dll'))
    ('/r:' + (Join-Path $framework 'WPF/WindowsBase.dll'))
)

# Setup and its rollback integration test compile the same implementation.
# The test uses a library target and never calls the installer entry point.
$setupSourceNames = @(
    'AssemblyInfo', 'Core', 'IndexedStore', 'Vocabulary', 'Hindi', 'Itrans',
    'CopiedConversion', 'IndicScripts', 'Configuration', 'SingleLetters',
    'Number', 'ResidentReplacement', 'SelectionFormatting', 'Setup',
    'SetupWindow', 'EspansoMigration', 'InstallTransaction', 'DistributionFiles',
    'ApplicationShortcuts'
)
$setupSources = $setupSourceNames | ForEach-Object { Join-Path $PSScriptRoot ('src/' + $_ + '.cs') }
