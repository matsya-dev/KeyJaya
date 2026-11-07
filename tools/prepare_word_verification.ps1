<#
.SYNOPSIS
Compiles and self-tests the complete existing Word body/comment/recovery matrix.
.DESCRIPTION
Preparation only. There is deliberately no desktop switch or resident-launch
path. No Word, UIA providers, hooks, clipboard, installed profile or trial state
is accessed. Outputs are binaries, source hashes and a run plan beneath out/.
Live execution requires separate user approval. Resident conversion/recovery
also requires a fresh diagnostic session after the uninterrupted trial.
#>
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
. (Join-Path $root 'build-common.ps1')
$output = Join-Path $root 'out/word-full-verification'
$build = Join-Path $output 'build'
foreach ($directory in @((Join-Path $root 'out'), $output, $build)) {
    if (!(Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
    if ((Get-Item -LiteralPath $directory).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Preparation output must not be linked.' }
}
$lock = [IO.File]::Open((Join-Path $output 'prepare.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    # A previous report must not survive a failed re-preparation as a ready plan.
    $report = Join-Path $output 'preparation.json'
    [ordered]@{ schema=1; status='preparing'; live_tests_run=$false } | ConvertTo-Json | Set-Content -LiteralPath $report -Encoding UTF8
    $inputs = @($runtime) + @('VERSION','build-common.ps1','src/runtime.manifest','src/keyjaya-app.exe.config','tests/fixtures/word-performance.json','tests/integration/WordPerformanceTests.cs','tests/integration/WordPerformanceSupport.cs','tests/integration/NativeWordBindingTests.cs','tests/contracts/WordPerformanceContracts.cs','tests/integration/ModernWordCommentTests.cs','tests/integration/ModernWordSetup.cs','tests/integration/ModernWordPanel.cs','tests/contracts/ModernWordSetupTests.cs','tests/integration/WordRecoveryFixture.cs','tools/test_word_performance.ps1','tools/test_word_modern.ps1','tools/prepare_word_verification.ps1' | ForEach-Object { Join-Path $root $_ })
    $sources = @($inputs | Select-Object -Unique | ForEach-Object { [ordered]@{ relative=$_.Substring($root.Length + 1); sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash } })
    $bodyLines = @(& (Join-Path $root 'tools/test_word_performance.ps1') -Iterations 3 -Warmups 1)
    $bodyLines | Set-Content -LiteralPath (Join-Path $output 'body-preparation.txt') -Encoding UTF8
    $bodyEvidence = @($bodyLines | Where-Object { $_ -like 'Prepared and self-tested. Word was not started. Evidence: *' })
    if ($bodyEvidence.Count -ne 1) { throw 'Body preparation did not return one verified report.' }
    $bodyRun = $bodyEvidence[0].Substring('Prepared and self-tested. Word was not started. Evidence: '.Length)
    $body = Get-Content -LiteralPath (Join-Path $bodyRun 'preparation.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $bodyChecks = Get-Content -LiteralPath (Join-Path $bodyRun 'self-tests.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($bodyChecks.status -ne 'passed' -or !$body.same_adapter_source) { throw 'Expected a passing same-source full body preparation.' }

    $commentLines = @(& (Join-Path $root 'tools/test_word_modern.ps1') -Scope All)
    $commentLines | Set-Content -LiteralPath (Join-Path $output 'comment-preparation.txt') -Encoding UTF8
    $commentEvidence = @($commentLines | Where-Object { $_ -like 'Compiled only: *' })
    if ($commentEvidence.Count -ne 1) { throw 'Comment preparation did not return one compiled runner.' }
    $commentRunner = $commentEvidence[0].Substring('Compiled only: '.Length)
    $commentScope = Get-Content -LiteralPath (Join-Path (Split-Path $commentRunner) 'scope.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($commentScope.scope -ne 'All' -or $commentScope.needs_settlement -ne $true) { throw 'Full comment scope must retain settlement verification.' }

    $normalFolder = Join-Path $build 'normal'
    if (!(Test-Path -LiteralPath $normalFolder)) { New-Item -ItemType Directory -Path $normalFolder | Out-Null }
    if ((Get-Item -LiteralPath $normalFolder).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Normal candidate output must not be linked.' }
    $normal = Join-Path $normalFolder 'keyjaya-app.exe'
    $arguments = @('/nologo','/optimize+','/platform:x64','/codepage:65001','/langversion:5','/target:winexe','/r:System.Web.Extensions.dll',('/win32manifest:' + (Join-Path $root 'src/runtime.manifest')),'/main:KeyJaya.Program',('/out:' + $normal)) + $references + $runtime
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Normal diagnostic candidate compilation failed.' }
    Copy-Item -LiteralPath (Join-Path $root 'src/keyjaya-app.exe.config') -Destination ($normal + '.config') -Force
    # --version returns before all resident/UI/provider/profile initialization.
    $version = Start-Process -FilePath $normal -ArgumentList '--version' -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output 'version.txt') -RedirectStandardError (Join-Path $output 'version-stderr.txt')
    $null = $version.Handle
    if (!$version.WaitForExit(5000)) { $version.Kill(); throw 'Normal candidate version probe timed out.' }
    if ($version.ExitCode -ne 0 -or (Get-Content -LiteralPath (Join-Path $output 'version.txt') -Raw).Trim() -ne (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()) { throw 'Normal candidate version probe failed.' }

    # The native runner identifies the resident/helper by its real executable
    # name. This is a test-only binary in out/, never the trial or installed path.
    $recovery = Join-Path $build 'keyjaya-app.exe'
    if (Test-Path -LiteralPath (Join-Path $build 'silent-focus-once.flag')) { throw 'An armed recovery fixture must be retired explicitly before rebuilding.' }
    $fixture = Join-Path $root 'tests/integration/WordRecoveryFixture.cs'
    $arguments = @('/nologo','/optimize+','/platform:x64','/codepage:65001','/langversion:5','/r:System.Web.Extensions.dll',('/win32manifest:' + (Join-Path $root 'src/runtime.manifest')),'/main:KeyJaya.WordRecoveryFixture',('/out:' + $recovery)) + $references + $runtime + @($fixture)
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Recovery fixture compilation failed.' }
    Copy-Item -LiteralPath (Join-Path $root 'src/keyjaya-app.exe.config') -Destination ($recovery + '.config') -Force
    foreach ($probe in @('--self-test','--not-authorized')) {
        $name = $probe.TrimStart('-')
        $child = Start-Process -FilePath $recovery -ArgumentList $probe -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output ($name + '.txt')) -RedirectStandardError (Join-Path $output ($name + '-stderr.txt'))
        $null = $child.Handle
        if (!$child.WaitForExit(10000)) { $child.Kill(); throw 'Recovery fixture preparation probe timed out.' }
        $expected = if ($probe -eq '--self-test') { 0 } else { 2 }
        if ($child.ExitCode -ne $expected) { throw ('Recovery fixture probe failed: ' + $probe) }
    }
    $scope = Start-Process -FilePath $commentRunner -ArgumentList @('--scope-info','ContextualRecovery') -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output 'recovery-scope.json') -RedirectStandardError (Join-Path $output 'recovery-scope-stderr.txt')
    $null = $scope.Handle
    if (!$scope.WaitForExit(5000)) { $scope.Kill(); throw 'Recovery scope probe timed out.' }
    if ($scope.ExitCode -ne 0) { throw 'Recovery scope probe failed.' }
    $recoveryScope = Get-Content -LiteralPath (Join-Path $output 'recovery-scope.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($recoveryScope.scope -ne 'ContextualRecovery' -or $recoveryScope.needs_settlement -ne $true) { throw 'Recovery scope must retain settlement verification.' }

    foreach ($source in $sources) {
        if ((Get-FileHash -LiteralPath (Join-Path $root $source.relative) -Algorithm SHA256).Hash -ne $source.sha256) { throw ('Source changed during preparation: ' + $source.relative) }
    }
    if ((Get-FileHash -LiteralPath (Join-Path $root 'out/word-performance-tests/build/WordPerformanceTests.exe') -Algorithm SHA256).Hash -ne $body.candidate_binary_sha256 -or
        (Get-FileHash -LiteralPath (Join-Path $root 'out/word-performance-tests/build/WordPerformanceBaseline.dll') -Algorithm SHA256).Hash -ne $body.baseline_binary_sha256) { throw 'Body binaries changed after their self-tests; prepare again.' }
    $binaries = @((Join-Path $root 'out/word-performance-tests/build/WordPerformanceTests.exe'),(Join-Path $root 'out/word-performance-tests/build/WordPerformanceBaseline.dll'),$commentRunner,$normal,($normal + '.config'),$recovery,($recovery + '.config')) | ForEach-Object { [ordered]@{ path=$_; sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash } }
    $cases = (Get-Content -LiteralPath (Join-Path $root 'tests/fixtures/word-performance.json') -Raw -Encoding UTF8 | ConvertFrom-Json).cases
    [ordered]@{
        schema=1; status='prepared_not_live_verified'; prepared_utc=[datetime]::UtcNow.ToString('o'); language='C# 5'; framework='.NET Framework 4.8'
        live_tests_run=$false; word_started=$false; resident_started=$false; trial_accessed=$false; installed_profile_accessed=$false; recovery_armed=$false
        body=@{ preparation=(Join-Path $bodyRun 'preparation.json'); cases=@($cases.id); tracking=@($false,$true); variants=@('baseline','candidate'); iterations=3; warmups=1; samples=($cases.Count*2*2*4); isolated_assertions=$bodyChecks.assertions; same_source_calibration=$true; requires_fresh_live_approval=$true }
        comments=@{ resident_candidate=$normal; runner=$commentRunner; scope='All'; views=@('contextual new','contextual posted edit','contextual reply','panel new','panel posted edit','panel reply'); action_cases=@($commentScope.case_keys).Count; case_keys=@($commentScope.case_keys); requires_fresh_diagnostic_session=$true; requires_fresh_live_approval=$true }
        recovery=@{ resident_fixture=$recovery; runner=$commentRunner; scope='ContextualRecovery'; fault='one first helper ready but silent; subsequent helpers use unchanged production sources'; asserts=@('same resident PID','same comment/caret without refocusing','replacement helper resolves editor','conversion and plain formatting','native undo','body unchanged'); requires_private_profile_after_trial=$true; requires_fresh_live_approval=$true }
        limitations=@('No live result is claimed by preparation.','Body timings are same-source calibration, not a speedup comparison.','Comment dispatch does not prove physical keyboard-hook delivery.','Recovery covers the prepared contextual silent-helper case, not every failure in every view.','Current trial diagnostic window cannot be extended by preparation; do not treat an expired zero probe as idle.','No profile, installer or installation is created; no current app is stopped, restarted or overwritten.')
        sources=$sources; binaries=@($binaries)
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $report -Encoding UTF8
    Write-Output ('Prepared all three Word phases without desktop access: ' + $report)
} finally { $lock.Dispose() }
