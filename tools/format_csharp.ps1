<#
.SYNOPSIS
Formats repository C# with Roslyn while proving that executable tokens are unchanged.
.DESCRIPTION
Optional developer tool: requires PowerShell 7 and its bundled Roslyn assemblies.
It does not install packages, compile the app, access the clipboard, or start hooks.
The application still builds with the .NET Framework compiler and PowerShell 5.1.
.PARAMETER Check
Validate formatting without modifying source files; useful before submitting a change.
.PARAMETER Path
Limit the operation to explicit C# paths beneath this repository.
#>
param(
    [switch]$Check,
    [string[]]$Path = @()
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$roslyn = Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll'
$csharp = Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll'
if (!(Test-Path -LiteralPath $roslyn) -or !(Test-Path -LiteralPath $csharp)) {
    throw 'Run this optional formatter in PowerShell 7 with its bundled Roslyn libraries.'
}

Add-Type -Path $roslyn
Add-Type -Path $csharp
$options = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithLanguageVersion(
    [Microsoft.CodeAnalysis.CSharp.LanguageVersion]::CSharp5
)
$normalize = [Microsoft.CodeAnalysis.SyntaxNodeExtensions].GetMethods() |
    Where-Object { $_.Name -eq 'NormalizeWhitespace' -and $_.GetParameters().Count -eq 4 } |
    Select-Object -First 1
$normalize = $normalize.MakeGenericMethod([Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax])

if ($Path.Count) {
    $files = $Path | ForEach-Object { Get-Item -LiteralPath $_ }
} else {
    $files = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src'),
        (Join-Path $projectRoot 'tests'), (Join-Path $projectRoot 'tools') -Filter '*.cs' -Recurse -File
}

$report = @()
$needsFormatting = @()
$rootPrefix = [IO.Path]::GetFullPath($projectRoot) + [IO.Path]::DirectorySeparatorChar
foreach ($file in $files) {
    if (!$file.FullName.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase) -or $file.Extension -ne '.cs') {
        throw 'Formatter targets must be C# files beneath the repository.'
    }

    $text = [IO.File]::ReadAllText($file.FullName)
    $original = [Microsoft.CodeAnalysis.CSharp.SyntaxFactory]::ParseCompilationUnit($text, 0, $options)
    $errors = @($original.GetDiagnostics() | Where-Object Severity -eq Error)
    if ($errors.Count) {
        throw ('Invalid C# 5 source: ' + $file.Name + ': ' + ($errors -join ', '))
    }

    $formatted = $normalize.Invoke($null, @($original, '    ', "`r`n", $false)).ToFullString().TrimEnd() + "`r`n"
    $parsed = [Microsoft.CodeAnalysis.CSharp.SyntaxFactory]::ParseCompilationUnit($formatted, 0, $options)
    $beforeTokens = @($original.DescendantTokens($null, $false))
    $afterTokens = @($parsed.DescendantTokens($null, $false))
    if ($beforeTokens.Count -ne $afterTokens.Count) {
        throw ('Formatting changed the token count: ' + $file.Name)
    }
    for ($index = 0; $index -lt $beforeTokens.Count; $index++) {
        if ($beforeTokens[$index].RawKind -ne $afterTokens[$index].RawKind -or
            $beforeTokens[$index].Text -cne $afterTokens[$index].Text) {
            throw ('Formatting changed executable text: ' + $file.Name)
        }
    }

    $changed = $formatted -cne $text
    if ($changed) {
        $needsFormatting += $file.FullName.Substring($rootPrefix.Length)
        if (!$Check) {
            [IO.File]::WriteAllText($file.FullName, $formatted, (New-Object Text.UTF8Encoding($false)))
        }
    }
    $report += @{ file = $file.FullName.Substring($rootPrefix.Length); tokens = $beforeTokens.Count; executable_tokens_unchanged = $true }
}

$reportFolder = Join-Path $projectRoot 'out/formatting'
New-Item -ItemType Directory -Path $reportFolder -Force | Out-Null
$report | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $reportFolder 'csharp.json') -Encoding UTF8
if ($Check -and $needsFormatting.Count) {
    throw ('C# formatting differs: ' + ($needsFormatting -join ', '))
}
Write-Output ('PASS ' + $report.Count + ' C# files; executable token sequences preserved.')
