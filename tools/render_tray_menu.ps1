param()
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $root 'build-common.ps1')
$assembly=Join-Path $root 'out/app/engine/keyjaya-app.exe'
if(!(Test-Path -LiteralPath $assembly)){throw 'Build the app before rendering its menu.'}
$output=Join-Path $root 'out/menu-preview'
New-Item -ItemType Directory -Path $output -Force | Out-Null
Copy-Item -LiteralPath $assembly -Destination (Join-Path $output 'keyjaya-app.exe') -Force
$runner=Join-Path $output 'RenderTrayMenu.exe'
& $compiler /nologo /optimize+ /platform:x64 /codepage:65001 /r:System.Drawing.dll /r:System.Windows.Forms.dll ('/r:'+$assembly) ('/win32manifest:'+(Join-Path $root 'src/runtime.manifest')) ('/out:'+$runner) (Join-Path $root 'tools/RenderTrayMenu.cs')
if($LASTEXITCODE -ne 0){throw 'Menu preview compilation failed.'}
& $runner (Join-Path $root 'docs/images/tray-menu.png')
if($LASTEXITCODE -ne 0){throw 'Menu preview rendering failed.'}
