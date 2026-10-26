<#
.SYNOPSIS
Builds a clean application tree and, unless AppOnly is set, its installer.
.DESCRIPTION
Compilation and packaging only: never launches a resident or changes the installed profile.
A prior development output is archived so obsolete files cannot leak into the payload.
.PARAMETER AppOnly
Compile the resident and uninstaller without creating a payload ZIP or setup executable.
.PARAMETER Python
Python 3.11+ executable used only for payload packaging.
#>
param(
    [switch]$AppOnly,
    [string]$Python = 'python'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
. (Join-Path $root 'build-common.ps1')
if (!(Test-Path -LiteralPath $compiler)) {
    throw '.NET Framework 4.8 and its x64 C# compiler are required.'
}

$app = Join-Path $root 'out/app'
$exclusions = @(Get-Content -LiteralPath (Join-Path $root 'assets/distribution-exclusions.txt') -Encoding UTF8 |
    ForEach-Object { $_.Trim() } | Where-Object { $_ -and !$_.StartsWith('#') })
foreach ($relative in $exclusions) {
    $leaf = ($relative -split '/')[-1]
    if ($relative -notmatch '^(?:THIRD_PARTY_NOTICES\.md|(?:data|docs)/[^/\\]+)$' -or
        $leaf -in @('.', '..') -or $leaf.EndsWith('.') -or $relative -match '[:*?~\x00-\x1f]' -or
        [IO.Path]::GetExtension($leaf).ToLowerInvariant() -in @('.dat', '.idx', '.hash', '.tsv', '.ini', '.exe', '.dll', '.ico')) {
        throw ('Unsafe distribution exclusion: ' + $relative)
    }
}
if (Test-Path -LiteralPath $app) {
    $out = [IO.Path]::GetFullPath((Join-Path $root 'out')) + [IO.Path]::DirectorySeparatorChar
    $previous = Join-Path $root ('out/previous-app-' + [Guid]::NewGuid().ToString('N'))
    foreach ($target in @($app, $previous)) {
        if (![IO.Path]::GetFullPath($target).StartsWith($out, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Build output outside out/.'
        }
    }
    if ((Get-Item -LiteralPath $app).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'Build output must not be a linked directory.'
    }
    Move-Item -LiteralPath $app -Destination $previous
}

New-Item -ItemType Directory -Path $app, (Join-Path $app 'engine') -Force | Out-Null
foreach ($name in @('assets', 'data')) {
    $destination = Join-Path $app $name
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $root $name) -File |
        Where-Object { $_.Name -notin @('README.md', 'manifest.json') -and ($name + '/' + $_.Name) -notin $exclusions } |
        ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $destination -Force }
}
New-Item -ItemType Directory -Path (Join-Path $app 'config') -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $root 'defaults') -File |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $app 'config') -Force }
Copy-Item -LiteralPath (Join-Path $root 'assets/short-trigger-defaults.yml') -Destination (Join-Path $app 'config/short-triggers.yml') -Force
# Keep the installed help usable offline. Repository-relative links need a small
# relocation when the user guide becomes the application's top-level README.
$helpFiles = @{
    'docs/USER_GUIDE.md' = 'README.md'
    'docs/FIRST_RUN_CHECK.md' = 'FIRST_RUN_CHECK.md'
    'docs/PRIVACY.md' = 'PRIVACY.md'
    'SECURITY.md' = 'SECURITY.md'
}
foreach ($source in $helpFiles.Keys) {
    $text = [IO.File]::ReadAllText((Join-Path $root $source))
    $text = $text.Replace('](../SECURITY.md)', '](SECURITY.md)').Replace('](docs/PRIVACY.md)', '](PRIVACY.md)')
    $text = $text.Replace('](TESTING.md)', '](https://github.com/matsya-dev/KeyJaya/blob/main/docs/TESTING.md)')
    [IO.File]::WriteAllText((Join-Path $app $helpFiles[$source]), $text, (New-Object Text.UTF8Encoding($false)))
}
New-Item -ItemType Directory -Path (Join-Path $app 'images') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'docs/images/tray-menu.png') -Destination (Join-Path $app 'images/tray-menu.png') -Force
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $app 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $root 'VERSION') -Destination (Join-Path $app 'VERSION') -Force

$common = @('/nologo', '/optimize+', '/platform:x64', '/codepage:65001', '/r:System.Web.Extensions.dll')
$appArguments = $common + $references + @(
    '/target:winexe'
    '/main:KeyJaya.Program'
    ('/win32manifest:' + (Join-Path $root 'src/runtime.manifest'))
    ('/win32icon:' + (Join-Path $root 'assets/keyjaya.ico'))
    ('/out:' + (Join-Path $app 'engine/keyjaya-app.exe'))
) + $runtime
& $compiler @appArguments
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed' }
Copy-Item -LiteralPath (Join-Path $root 'src/keyjaya-app.exe.config') -Destination (Join-Path $app 'engine/keyjaya-app.exe.config') -Force

$uninstallArguments = $common + @(
    '/r:System.Windows.Forms.dll'
    '/target:winexe'
    ('/win32manifest:' + (Join-Path $root 'src/app.manifest'))
    ('/win32icon:' + (Join-Path $root 'assets/keyjaya.ico'))
    ('/out:' + (Join-Path $app 'KeyJaya-Uninstall.exe'))
    (Join-Path $root 'src/AssemblyInfo.cs')
    (Join-Path $root 'src/Uninstall.cs')
    (Join-Path $root 'src/ApplicationShortcuts.cs')
)
& $compiler @uninstallArguments
if ($LASTEXITCODE -ne 0) { throw 'Uninstaller compilation failed' }
if ($AppOnly) {
    Write-Output ('Built application: ' + (Join-Path $app 'engine/keyjaya-app.exe'))
    return
}

& $Python (Join-Path $root 'package.py') --release $app --output (Join-Path $root 'out/payload.zip')
if ($LASTEXITCODE -ne 0) { throw 'Payload packaging failed; Python 3.11 or newer is required.' }
$version = ([IO.File]::ReadAllText((Join-Path $root 'VERSION'))).Trim()
$installer = Join-Path $root ('out/KeyJaya-Setup-v' + $version + '.exe')
& (Join-Path $root 'setup-build.ps1') -ReleaseRoot $app -PayloadPath (Join-Path $root 'out/payload.zip') -OutputPath $installer
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
Write-Output ('Built installer: ' + $installer)
