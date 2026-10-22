param([string[]]$Suite=@(),[switch]$Full)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
. (Join-Path $root 'build-common.ps1')
$registry=Get-Content -LiteralPath (Join-Path $root 'tests/suites.json') -Raw | ConvertFrom-Json
$available=[string[]]$registry
if($Suite.Count -gt 0){$names=$Suite}
elseif($Full){$names=$available}
else{$names=@('WordSnapshotTests','InDesignFindSyntaxTests','SingleLetterTests','CompoundCaretTests','SlashWordTests','NumberCtrlTests','TriggerEditorTests','BookStyleTests','ScriptLanguageDetectionTests','ItransTests','HindiTests','CorrectionRaceTests','ShortcutSettingsTests','NativeKeyTests','ToggleTests')}
foreach($name in $names){if($name -notin $available){throw ('Unknown or interactive test suite: '+$name)}}
$output=Join-Path $root 'out/tests'
$fixture=Join-Path $output 'fixture'
New-Item -ItemType Directory -Path $output,$fixture -Force | Out-Null
foreach($folder in @('assets','data')){Copy-Item -LiteralPath (Join-Path $root $folder) -Destination $fixture -Recurse -Force}
$inputs=Join-Path $output 'inputs'
New-Item -ItemType Directory -Path $inputs -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $root 'tests/fixtures') -File | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $inputs -Force}
Copy-Item -LiteralPath (Join-Path $inputs 'english-guard.txt') -Destination (Join-Path $fixture 'data/english-guard.txt') -Force
$results=@()
foreach($name in $names){
 New-Item -ItemType Directory -Path (Join-Path $fixture 'config') -Force | Out-Null
 Get-ChildItem -LiteralPath (Join-Path $root 'defaults') -File | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $fixture 'config') -Force}
 $runner=Join-Path $output ($name+'.exe')
 & $compiler /nologo /optimize+ /platform:x64 /codepage:65001 /r:System.Web.Extensions.dll @references ('/main:KeyJaya.'+$name) ('/out:'+$runner) @runtime (Join-Path $root ('tests/csharp/'+$name+'.cs'))
 if($LASTEXITCODE -ne 0){throw ($name+' compilation failed')}
 $extra=@()
 if($name -in @('StringentTests','CopiedConversionTests','FontCyclingTests')){$extra=@((Join-Path $inputs 'expanded-en_US-words.txt'))}
 if($name -eq 'SanskritTests'){$extra=@((Join-Path $inputs 'english-prose.txt'))}
 if($name -eq 'ScriptFeaturesTests'){$extra=@((Join-Path $inputs 'script-reference-passages.tsv'))}
 if($name -eq 'ToggleTests'){$extra=@((Join-Path $inputs 'contraction-keys.txt'))}
 $result=& $runner $fixture @extra 2>&1
 $exitCode=$LASTEXITCODE
 $result | Set-Content -LiteralPath (Join-Path $output ($name+'.txt')) -Encoding UTF8
 $results+=@{suite=$name;exit_code=$exitCode;output=($result -join "`n")}
 $results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'results.json') -Encoding UTF8
 Write-Output ($name+': '+($result -join "`n"))
 if($exitCode -ne 0){throw ($name+' failed')}
}
Write-Output ('Passed '+$names.Count+' isolated automated suites. Results: '+(Join-Path $output 'results.json'))
