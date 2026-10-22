param([string]$Python='python')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $root 'build-common.ps1')
$app=Join-Path $root 'out/app'
$assembly=Join-Path $app 'engine/keyjaya-app.exe'
if(!(Test-Path -LiteralPath $assembly)){throw 'Build the app before rendering the demo.'}
$output=Join-Path $root 'out/readme-demo'
New-Item -ItemType Directory -Path $output -Force | Out-Null
Copy-Item -LiteralPath $assembly -Destination (Join-Path $output 'keyjaya-app.exe') -Force
$runner=Join-Path $output 'ReadmeDemoWords.exe'
& $compiler /nologo /optimize+ /platform:x64 /codepage:65001 /r:System.Web.Extensions.dll ('/r:'+$assembly) ('/out:'+$runner) (Join-Path $root 'tools/ReadmeDemoWords.cs')
if($LASTEXITCODE -ne 0){throw 'Demo engine probe compilation failed.'}
$words=Join-Path $output 'words.json'
& $runner $app $words
if($LASTEXITCODE -ne 0){throw 'Demo engine output verification failed.'}
& $Python -X utf8 (Join-Path $root 'tools/render_readme_demo.py') --words $words
if($LASTEXITCODE -ne 0){throw 'README asset rendering failed; see the Python error above.'}
