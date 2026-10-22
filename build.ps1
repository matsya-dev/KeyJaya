param([switch]$AppOnly,[string]$Python='python')
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
. (Join-Path $root 'build-common.ps1')
if(!(Test-Path -LiteralPath $compiler)){throw '.NET Framework 4.8 and its x64 C# compiler are required.'}
$app=Join-Path $root 'out/app'
New-Item -ItemType Directory -Path $app,(Join-Path $app 'engine') -Force | Out-Null
foreach($name in @('assets','data')){
 New-Item -ItemType Directory -Path (Join-Path $app $name) -Force | Out-Null
 Get-ChildItem -LiteralPath (Join-Path $root $name) -File | Where-Object {$_.Name -notin @('README.md','manifest.json')} | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $app $name) -Force}
}
New-Item -ItemType Directory -Path (Join-Path $app 'config') -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $root 'defaults') -File | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $app 'config') -Force}
Copy-Item -LiteralPath (Join-Path $root 'docs/USER_GUIDE.md') -Destination (Join-Path $app 'README.md') -Force
foreach($notice in @('LICENSE','THIRD_PARTY_NOTICES.md')){Copy-Item -LiteralPath (Join-Path $root $notice) -Destination (Join-Path $app $notice) -Force}
New-Item -ItemType Directory -Path (Join-Path $app 'docs') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'docs/LICENSING.md') -Destination (Join-Path $app 'docs/LICENSING.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'VERSION') -Destination (Join-Path $app 'VERSION') -Force
$common=@('/nologo','/optimize+','/platform:x64','/codepage:65001','/r:System.Web.Extensions.dll')
& $compiler @common @references /target:winexe /main:KeyJaya.Program ('/win32manifest:'+(Join-Path $root 'src/runtime.manifest')) ('/win32icon:'+(Join-Path $root 'assets/keyjaya.ico')) ('/out:'+(Join-Path $app 'engine/keyjaya-app.exe')) @runtime
if($LASTEXITCODE -ne 0){throw 'Application compilation failed'}
Copy-Item -LiteralPath (Join-Path $root 'src/keyjaya-app.exe.config') -Destination (Join-Path $app 'engine/keyjaya-app.exe.config') -Force
& $compiler @common /r:System.Windows.Forms.dll /target:winexe ('/win32manifest:'+(Join-Path $root 'src/app.manifest')) ('/win32icon:'+(Join-Path $root 'assets/keyjaya.ico')) ('/out:'+(Join-Path $app 'KeyJaya-Uninstall.exe')) (Join-Path $root 'src/AssemblyInfo.cs') (Join-Path $root 'src/Uninstall.cs') (Join-Path $root 'src/ApplicationShortcuts.cs')
if($LASTEXITCODE -ne 0){throw 'Uninstaller compilation failed'}
if($AppOnly){Write-Output ('Built application: '+(Join-Path $app 'engine/keyjaya-app.exe'));return}
& $Python (Join-Path $root 'package.py') --release $app --output (Join-Path $root 'out/payload.zip')
if($LASTEXITCODE -ne 0){throw 'Payload packaging failed; Python 3.11 or newer is required.'}
$version=([IO.File]::ReadAllText((Join-Path $root 'VERSION'))).Trim()
$installer=Join-Path $root ('out/KeyJaya-Setup-v'+$version+'.exe')
& (Join-Path $root 'setup-build.ps1') -ReleaseRoot $app -PayloadPath (Join-Path $root 'out/payload.zip') -OutputPath $installer
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
Write-Output ('Built installer: '+$installer)
