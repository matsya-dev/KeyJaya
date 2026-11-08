param([Parameter(Mandatory=$true)][string]$FixtureRoot, [switch]$RequireJunction)
$ErrorActionPreference='Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
. (Join-Path $repo 'tools/workspace.ps1')
. (Join-Path $repo 'tools/build-output.ps1')
$checks=0
function Check($Value, $Label) { if (-not $Value) { throw $Label }; $script:checks++ }
function Fails($Action, $Label) { $failed=$false; try { & $Action } catch { $failed=$true }; Check $failed $Label }
function Seed($Path, $Text) { New-Item -ItemType Directory -Path (Split-Path $Path -Parent) -Force | Out-Null; [IO.File]::WriteAllText($Path,$Text) }

$sentinel=Join-Path $FixtureRoot 'source/keep.txt'
Seed $sentinel 'source remains'
Fails { Assert-KjOutputPath $FixtureRoot (Join-Path $FixtureRoot 'out/../source') } 'Escaping output path accepted'
Fails { Assert-KjOutputPath $FixtureRoot (Join-Path $FixtureRoot 'out') } 'Output root itself accepted'
$unmarked=Join-Path $FixtureRoot 'out/w/manual'
Seed (Join-Path $unmarked 'keep.txt') 'manual record'

$work=New-KjWork $FixtureRoot 'tests'
Seed (Join-Path $work.Path 'extra-artifacts/clone/data/file.dat') 'copied data'
$second=New-KjWork $FixtureRoot 'tests'
Check (Test-Path -LiteralPath $work.Path) 'Another run deleted active workspace'
Complete-KjWork $second $true
Complete-KjWork $work $true
Check (-not (Test-Path -LiteralPath $work.Path)) 'Successful work left scratch behind'
Check (Test-Path -LiteralPath (Join-Path $work.Evidence 'report.json')) 'Successful evidence missing'
Check (Test-Path -LiteralPath (Join-Path $unmarked 'keep.txt')) 'Unmarked output removed'

# A restricted process view must not turn a held lease into stale-work recovery.
$busy=New-KjWork $FixtureRoot 'tests'
$ownerPath=Join-Path $busy.Path 'owner.json'
$owner=Get-Content -LiteralPath $ownerPath -Raw | ConvertFrom-Json
$originalPid=$owner.pid; $owner.pid=2147483646
Write-KjJson $ownerPath $owner
$notices=@(& { New-KjWork $FixtureRoot 'tests' } 3>&1)
$warnings=@($notices | Where-Object { $_ -is [Management.Automation.WarningRecord] })
$other=$notices | Where-Object { $_ -isnot [Management.Automation.WarningRecord] }
Check ($warnings.Count -eq 0) 'Busy lease was reported as abandoned'
Check (Test-Path -LiteralPath $busy.Path) 'Held lease did not protect active workspace'
$owner.pid=$originalPid; Write-KjJson $ownerPath $owner
Complete-KjWork $other $true
Complete-KjWork $busy $true

$failed=New-KjWork $FixtureRoot 'tests'
Seed (Join-Path $failed.Path 'config/settings.ini') 'unique failure settings'
Seed (Join-Path $failed.Path 'data/dictionary.dat') 'regenerable data'
Complete-KjWork $failed $false
Check (-not (Test-Path -LiteralPath $failed.Path)) 'Failed work left copied data behind'
Add-Type -AssemblyName System.IO.Compression
$stream=[IO.File]::OpenRead((Join-Path $failed.Evidence 'failure.zip'))
$zip=New-Object IO.Compression.ZipArchive($stream,[IO.Compression.ZipArchiveMode]::Read)
try {
    $reader=New-Object IO.StreamReader($zip.GetEntry('work/config/settings.ini').Open())
    try { Check ($reader.ReadToEnd() -eq 'unique failure settings') 'Failure settings not preserved' } finally { $reader.Dispose() }
    Check ($null -eq $zip.GetEntry('work/data/dictionary.dat')) 'Copied dictionary retained in failure snapshot'
} finally { $zip.Dispose(); $stream.Dispose() }

$kept=New-KjWork $FixtureRoot 'tests' -KeepArtifacts
Seed (Join-Path $kept.Path 'config/keep.ini') 'inspect this'
Complete-KjWork $kept $true
Check (Test-Path -LiteralPath $kept.Path) 'KeepArtifacts was ignored'
$kept.Keep=$false
$owner=Get-Content -LiteralPath (Join-Path $kept.Path 'owner.json') -Raw | ConvertFrom-Json
$owner.keep=$false
Write-KjJson (Join-Path $kept.Path 'owner.json') $owner
Complete-KjWork $kept $true

$bad=New-KjWork $FixtureRoot 'tests'
$ownerPath=Join-Path $bad.Path 'owner.json'
$owner=Get-Content -LiteralPath $ownerPath -Raw | ConvertFrom-Json
$owner.token='not-our-token'; Write-KjJson $ownerPath $owner
Fails { Complete-KjWork $bad $true } 'Changed owner token was accepted'
Check (Test-Path -LiteralPath $bad.Path) 'Changed-owner work was deleted'
$owner.token=$bad.Token; Write-KjJson $ownerPath $owner
Complete-KjWork $bad $true

$linked=New-KjWork $FixtureRoot 'tests'
$link=Join-Path $linked.Path 'external'
$junction=$false
try {
    New-Item -ItemType Junction -Path $link -Target (Split-Path $sentinel -Parent) | Out-Null
    $junction=$true
} catch [UnauthorizedAccessException] {
    if ($RequireJunction) { throw }
    Write-Output 'SKIP junction creation: denied by the execution environment.'
} catch [ComponentModel.Win32Exception] {
    if ($RequireJunction -or $_.Exception.NativeErrorCode -notin @(5,1314)) { throw }
    Write-Output 'SKIP junction creation: denied by the execution environment.'
}
if ($junction) {
    Fails { Complete-KjWork $linked $true } 'Linked directory was traversed for deletion'
    Check ([IO.File]::ReadAllText($sentinel) -eq 'source remains') 'Junction target was modified'
    [IO.Directory]::Delete($link,$false)
}
Complete-KjWork $linked $true

$orphan=New-KjWork $FixtureRoot 'tests'
Seed (Join-Path $orphan.Path 'config/lost.ini') 'interrupted evidence'
$ownerPath=Join-Path $orphan.Path 'owner.json'
$owner=Get-Content -LiteralPath $ownerPath -Raw | ConvertFrom-Json
$owner.pid=2147483646; $owner.started_ticks=0; Write-KjJson $ownerPath $owner
$orphan.Lease.Dispose()
$next=New-KjWork $FixtureRoot 'tests'
Check (-not (Test-Path -LiteralPath $orphan.Path)) 'Abandoned owned work was not recovered'
Check (Test-Path -LiteralPath (Join-Path $orphan.Evidence 'failure.zip')) 'Interrupted evidence missing'
Complete-KjWork $next $true

# Retention is bounded, but the latest failure and manually edited evidence survive.
$edited=New-KjWork $FixtureRoot 'tests'
Complete-KjWork $edited $true
Seed (Join-Path $edited.Evidence 'user-added.txt') 'keep my note'
$annotated=New-KjWork $FixtureRoot 'tests'
Complete-KjWork $annotated $true
$annotationPath=Join-Path $annotated.Evidence 'report.json'
$annotation=Get-Content -LiteralPath $annotationPath -Raw | ConvertFrom-Json
$annotation | Add-Member NoteProperty user_note 'preserve investigation'
Write-KjJson $annotationPath $annotation
for ($i=0;$i -lt 9;$i++) { $w=New-KjWork $FixtureRoot 'tests'; Complete-KjWork $w $true }
Check (Test-Path -LiteralPath (Join-Path $edited.Evidence 'user-added.txt')) 'Edited report was pruned'
$reports=@(Get-ChildItem -LiteralPath (Join-Path $FixtureRoot 'out/reports/tests') -Directory)
Check ($reports.Count -le 8) 'Unbounded report retention'
Check (Test-Path -LiteralPath $annotationPath) 'Annotated report marker was pruned'
Check (Test-Path -LiteralPath (Join-Path $orphan.Evidence 'failure.zip')) 'Latest failure was pruned by successes'

function Stage($Work,$Text) {
    $app=Join-Path $Work.Path 'app'
    Seed (Join-Path $app 'engine/keyjaya-app.exe') $Text
    Seed (Join-Path $app 'config/settings.ini') 'factory settings'
    Seed (Join-Path $Work.Path 'payload.zip') ($Text+' payload')
    Seed (Join-Path $Work.Path 'setup.exe') ($Text+' installer')
    return $app
}
$initial=New-KjWork $FixtureRoot 'build'
$app=Stage $initial 'old'
Publish-KjBuild $initial $app (Join-Path $initial.Path 'payload.zip') (Join-Path $initial.Path 'setup.exe') '1.2.3'
Complete-KjWork $initial $true
$receipt=[IO.File]::ReadAllText((Join-Path $FixtureRoot 'out/build-output.json'))
$replacement=New-KjWork $FixtureRoot 'build'
$app=Stage $replacement 'new'
$script:failingSource=Join-Path $replacement.Path 'payload.zip'
function Move-Item {
    [CmdletBinding()]param([string]$LiteralPath,[string]$Destination)
    if ($LiteralPath -eq $script:failingSource) { throw 'Injected promotion failure' }
    Microsoft.PowerShell.Management\Move-Item -LiteralPath $LiteralPath -Destination $Destination -ErrorAction Stop
}
Fails { Publish-KjBuild $replacement $app (Join-Path $replacement.Path 'payload.zip') (Join-Path $replacement.Path 'setup.exe') '1.2.3' } 'Promotion fault was ignored'
Remove-Item Function:\Move-Item
Check ([IO.File]::ReadAllText((Join-Path $FixtureRoot 'out/app/engine/keyjaya-app.exe')) -eq 'old') 'Last good app not restored'
Check ([IO.File]::ReadAllText((Join-Path $FixtureRoot 'out/payload.zip')) -eq 'old payload') 'Last good payload not restored'
Check ([IO.File]::ReadAllText((Join-Path $FixtureRoot 'out/build-output.json')) -eq $receipt) 'Receipt changed after failed promotion'
Complete-KjWork $replacement $false

$edit=New-KjWork $FixtureRoot 'build'
$app=Stage $edit 'third'
Seed (Join-Path $FixtureRoot 'out/app/config/settings.ini') 'user customization'
Fails { Publish-KjBuild $edit $app } 'Edited app output was overwritten'
Check ([IO.File]::ReadAllText((Join-Path $FixtureRoot 'out/app/config/settings.ini')) -eq 'user customization') 'App customization lost'
Complete-KjWork $edit $false
Check ([IO.File]::ReadAllText($sentinel) -eq 'source remains') 'Source sentinel changed'
Write-Output ('PASS ' + $checks + ' workspace ownership, cleanup, evidence, retention and build rollback checks; no installed app or desktop operations.')
