param([string]$ReleaseRoot='',[string]$PayloadPath='',[string]$OutputPath='')
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$kjRelease=if($ReleaseRoot){[IO.Path]::GetFullPath($ReleaseRoot)}else{Join-Path $taskRoot 'release'}
$kjPayload=if($PayloadPath){[IO.Path]::GetFullPath($PayloadPath)}else{Join-Path $taskRoot 'payload.zip'}
$kjSource=[IO.File]::ReadAllText((Join-Path $taskRoot 'src/Core.cs'))
$kjVersion=[regex]::Match($kjSource,'Current\s*=\s*"([^"]+)"').Groups[1].Value
if(!$kjVersion -or ([IO.File]::ReadAllText((Join-Path $kjRelease 'VERSION')).Trim() -ne $kjVersion)){throw 'Setup payload version must match current source'}
$kjOutput=if($OutputPath){[IO.Path]::GetFullPath($OutputPath)}else{Join-Path $taskRoot ('packages/KeyJaya-Setup-v'+$kjVersion+'.exe')}
New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($kjOutput)) | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$framework = Split-Path $compiler
$refs = @('/r:System.Web.Extensions.dll','/r:System.Windows.Forms.dll','/r:System.Drawing.dll',('/r:' + (Join-Path $framework 'System.IO.Compression.dll')))
$kjSetupArgs = @('/nologo','/optimize+','/platform:x64','/target:winexe','/main:KeyJaya.Setup',('/win32manifest:' + (Join-Path $taskRoot 'src/app.manifest')),('/win32icon:' + (Join-Path $kjRelease 'assets/keyjaya.ico')),('/resource:' + $kjPayload + ',payload.zip'),('/out:' + $kjOutput))
& $compiler @kjSetupArgs @refs (Join-Path $taskRoot 'src/AssemblyInfo.cs') (Join-Path $taskRoot 'src/Core.cs') (Join-Path $taskRoot 'src/Vocabulary.cs') (Join-Path $taskRoot 'src/Hindi.cs') (Join-Path $taskRoot 'src/Itrans.cs') (Join-Path $taskRoot 'src/CopiedConversion.cs') (Join-Path $taskRoot 'src/IndicScripts.cs') (Join-Path $taskRoot 'src/Configuration.cs') (Join-Path $taskRoot 'src/SingleLetters.cs') (Join-Path $taskRoot 'src/Number.cs') (Join-Path $taskRoot 'src/ResidentReplacement.cs') (Join-Path $taskRoot 'src/SelectionFormatting.cs') (Join-Path $taskRoot 'src/Setup.cs') (Join-Path $taskRoot 'src/SetupWindow.cs') (Join-Path $taskRoot 'src/EspansoMigration.cs') (Join-Path $taskRoot 'src/InstallTransaction.cs') (Join-Path $taskRoot 'src/ApplicationShortcuts.cs')
if ($LASTEXITCODE -ne 0) { throw 'Setup compilation failed' }
Write-Output ('Built KeyJaya '+$kjVersion+' Setup at '+$kjOutput)

# Test runners are built separately by test.ps1; application builds do not create
# development executables in the repository root or start the application.
