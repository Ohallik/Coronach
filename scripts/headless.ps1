# Lattice headless verification helpers.
# Usage:
#   .\scripts\headless.ps1 verify             # compile check (exit 0 = green)
#   .\scripts\headless.ps1 scene              # (re)create the Title scene + build settings
#   .\scripts\headless.ps1 tests              # run EditMode tests
#   .\scripts\headless.ps1 playtests          # run PlayMode tests
#   .\scripts\headless.ps1 testcontract       # prove test-result adjudication without Unity
#   .\scripts\headless.ps1 build              # build Windows player to Builds\Windows
#   .\scripts\headless.ps1 builddev           # * POLISH-50B: DEV player to Builds\WindowsDev (LATTICE_DEV set)
#   .\scripts\headless.ps1 lint               # ★ POLISH-51: the standing script lint (writing gate)
#   .\scripts\headless.ps1 exec -Method Lattice.EditorTools.ArtTools.BuildDiorama   # any static method
param(
    [Parameter(Mandatory = $true)][ValidateSet("verify", "scene", "tests", "playtests", "testcontract", "build", "builddev", "lint", "exec")][string]$Mode,
    [string]$Method,
    # POLISH-51: report mode measures and always exits 0; -Enforce fails the build on any
    # budget breach. ValidateShip reads the report either way and re-checks its freshness.
    [switch]$Report,
    # #395: optional NUnit full-name filter for tests/playtests (e.g.
    # "Lattice.Tests.PlayMode.Phase14Act6Tests"). A filtered run rides the same
    # save shield as the full suite; the FULL suite remains the shipping gate.
    [string]$TestFilter,
    # 2026-08-21 (the 27-hour bugfix pass): the unity-batchmode-ilpp-hang law,
    # automated. If the -logFile goes silent for this many minutes while Unity is
    # still alive, the run is declared stalled: the Unity tree we launched is
    # killed and the launch retried ($StallRetries times). 0 disables the
    # watchdog. Markers: HEADLESS_STALL_DETECTED, HEADLESS_STALL_RETRY,
    # HEADLESS_STALL_EXIT_HANG (test run whose results XML was already complete,
    # fresh and green - judged by evidence, not re-run), HEADLESS_STALL_TIMEOUT
    # (exit code 5 - a real red after the retry, never a flake to wait out).
    [int]$StallTimeoutMinutes = 3,
    [int]$StallRetries = 1,
    [int]$TimeoutSec = 900
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "exec-log-name.ps1")

# P63: Unity's process exit is not test evidence. Unity can exit zero without
# writing results, can leave yesterday's XML in place, and can serialize a red
# child below a misleading root. Keep adjudication here, beside the launcher, so
# every EditMode/PlayMode invocation gets the same fail-closed contract.
function Get-RequiredTestCount {
    param(
        [Parameter(Mandatory = $true)][System.Xml.XmlElement]$Node,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not $Node.HasAttribute($Name)) {
        throw "test-run is missing required '$Name' count"
    }
    $raw = $Node.GetAttribute($Name)
    if ($raw -notmatch '^\d+$') {
        throw "test-run '$Name' count is not a nonnegative integer: '$raw'"
    }
    try { return [long]$raw }
    catch { throw "test-run '$Name' count is outside the supported integer range: '$raw'" }
}

function Read-UnityTestResult {
    param([Parameter(Mandatory = $true)][string]$Path)

    $settings = New-Object System.Xml.XmlReaderSettings
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $reader = [System.Xml.XmlReader]::Create($Path, $settings)
    try {
        $document = New-Object System.Xml.XmlDocument
        $document.XmlResolver = $null
        $document.Load($reader)
        return $document
    }
    finally {
        $reader.Dispose()
    }
}

function Assert-UnityTestResult {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][datetime]$NotBeforeUtc,
        [Parameter(Mandatory = $true)][string]$Platform
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Platform result XML is missing: $Path"
    }
    $item = Get-Item -LiteralPath $Path
    if ($item.Length -le 0) {
        throw "$Platform result XML is empty: $Path"
    }
    if ($item.LastWriteTimeUtc -lt $NotBeforeUtc) {
        throw "$Platform result XML is stale (written $($item.LastWriteTimeUtc.ToString('o')), launch $($NotBeforeUtc.ToString('o'))): $Path"
    }

    try { $document = Read-UnityTestResult -Path $Path }
    catch { throw "$Platform result XML is malformed or unsafe: $($_.Exception.Message)" }
    $run = $document.DocumentElement
    if ($null -eq $run -or $run.LocalName -ne "test-run") {
        throw "$Platform result XML has no test-run root"
    }
    if ($run.GetAttribute("result") -ne "Passed") {
        throw "$Platform test run did not complete green (result='$($run.GetAttribute("result"))')"
    }

    $startRaw = $run.GetAttribute("start-time")
    $endRaw = $run.GetAttribute("end-time")
    if ([string]::IsNullOrWhiteSpace($startRaw) -or [string]::IsNullOrWhiteSpace($endRaw)) {
        throw "$Platform test run is incomplete: start-time/end-time is missing"
    }
    try {
        $startTime = [DateTimeOffset]::Parse($startRaw, [Globalization.CultureInfo]::InvariantCulture)
        $endTime = [DateTimeOffset]::Parse($endRaw, [Globalization.CultureInfo]::InvariantCulture)
    }
    catch { throw "$Platform test run has malformed start-time/end-time" }
    if ($endTime -lt $startTime) {
        throw "$Platform test run ends before it starts"
    }
    # NUnit serializes these fields to whole seconds, while the launcher records
    # sub-second UTC. A two-second allowance avoids a same-second false red while
    # still rejecting an old payload copied into a newly touched file.
    if ($startTime.UtcDateTime -lt $NotBeforeUtc.ToUniversalTime().AddSeconds(-2) -or
        $endTime.UtcDateTime -lt $NotBeforeUtc.ToUniversalTime().AddSeconds(-2)) {
        throw "$Platform result XML contains stale run timestamps"
    }

    $total = Get-RequiredTestCount -Node $run -Name "total"
    $passed = Get-RequiredTestCount -Node $run -Name "passed"
    $failed = Get-RequiredTestCount -Node $run -Name "failed"
    $inconclusive = Get-RequiredTestCount -Node $run -Name "inconclusive"
    $skipped = Get-RequiredTestCount -Node $run -Name "skipped"
    if ($total -le 0 -or ($passed + $failed + $inconclusive) -le 0) {
        throw "$Platform test run executed zero tests"
    }
    if (($passed + $failed + $inconclusive + $skipped) -ne $total) {
        throw "$Platform test totals are incoherent (total=$total passed=$passed failed=$failed inconclusive=$inconclusive skipped=$skipped)"
    }
    if ($failed -ne 0) {
        throw "$Platform test run reports $failed failed test(s)"
    }

    foreach ($countNode in @($document.SelectNodes("//*[@failed or @errors]"))) {
        foreach ($attributeName in @("failed", "errors")) {
            if (-not $countNode.HasAttribute($attributeName)) { continue }
            $rawCount = $countNode.GetAttribute($attributeName)
            if ($rawCount -notmatch '^\d+$' -or [long]$rawCount -ne 0) {
                throw "$Platform result contains a nonzero/malformed $attributeName count: '$rawCount'"
            }
        }
    }
    foreach ($resultNode in @($document.SelectNodes("//*[@result or @label]"))) {
        if ($resultNode.GetAttribute("result") -in @("Failed", "Error") -or
            $resultNode.GetAttribute("label") -eq "Error") {
            throw "$Platform result contains a failed/error node '$($resultNode.LocalName)'"
        }
    }
    if ($null -ne $document.SelectSingleNode("//*[local-name()='failure' or local-name()='error']")) {
        throw "$Platform result contains a failure/error payload"
    }

    return [pscustomobject]@{ Total = $total; Passed = $passed; Skipped = $skipped }
}

function Clear-UnityTestEvidence {
    param([Parameter(Mandatory = $true)][string[]]$Paths)

    foreach ($evidencePath in $Paths) {
        if (Test-Path -LiteralPath $evidencePath) {
            if (-not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
                throw "Refusing to clear non-file test evidence path: $evidencePath"
            }
            Remove-Item -LiteralPath $evidencePath -Force
        }
        if (Test-Path -LiteralPath $evidencePath) {
            throw "Could not clear prior test evidence: $evidencePath"
        }
    }
}

function Invoke-UnityTestResultContractSelfTest {
    $tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("lattice-test-result-contract-" + [guid]::NewGuid().ToString("N"))
    $fixture = Join-Path $tempRoot "results.xml"
    New-Item -ItemType Directory -Path $tempRoot | Out-Null
    $fixtureStart = [DateTimeOffset]::UtcNow.AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
    $fixtureEnd = [DateTimeOffset]::UtcNow.ToString("yyyy-MM-dd HH:mm:ss'Z'", [Globalization.CultureInfo]::InvariantCulture)
    $valid = @"
<?xml version="1.0" encoding="utf-8"?>
<test-run result="Passed" total="2" passed="2" failed="0" inconclusive="0" skipped="0" start-time="$fixtureStart" end-time="$fixtureEnd">
  <test-suite result="Passed" failed="0"><test-case result="Passed" /><test-case result="Passed" /></test-suite>
</test-run>
"@
    $redCases = 0
    try {
        Set-Content -LiteralPath $fixture -Value $valid -Encoding UTF8
        $green = Assert-UnityTestResult -Path $fixture -NotBeforeUtc ([datetime]::UtcNow.AddMinutes(-1)) -Platform "SelfTest"
        if ($green.Total -ne 2 -or $green.Passed -ne 2) { throw "valid fixture returned wrong totals" }
        Write-Host "TEST_RESULT_CONTRACT_CASE valid => GREEN"

        $cases = @(
            @{ Name = "missing"; Prepare = { Remove-Item -LiteralPath $fixture -Force -ErrorAction SilentlyContinue }; NotBefore = { [datetime]::UtcNow.AddMinutes(-1) } },
            @{ Name = "stale"; Prepare = { Set-Content -LiteralPath $fixture -Value $valid -Encoding UTF8; (Get-Item -LiteralPath $fixture).LastWriteTimeUtc = [datetime]::UtcNow.AddMinutes(-5) }; NotBefore = { [datetime]::UtcNow } },
            @{ Name = "stale-payload"; Prepare = { Set-Content -LiteralPath $fixture -Value ($valid -replace '(start-time|end-time)="[^"]+"', '$1="2001-01-01 00:00:00Z"') -Encoding UTF8 }; NotBefore = { [datetime]::UtcNow.AddMinutes(-1) } },
            @{ Name = "malformed"; Prepare = { Set-Content -LiteralPath $fixture -Value '<test-run' -Encoding UTF8 }; NotBefore = { [datetime]::UtcNow.AddMinutes(-1) } },
            @{ Name = "failed"; Prepare = { Set-Content -LiteralPath $fixture -Value ($valid -replace 'result="Passed" total="2" passed="2" failed="0"', 'result="Failed" total="2" passed="1" failed="1"') -Encoding UTF8 }; NotBefore = { [datetime]::UtcNow.AddMinutes(-1) } },
            @{ Name = "hidden-error"; Prepare = { Set-Content -LiteralPath $fixture -Value ($valid -replace '<test-case result="Passed" />', '<test-case result="Failed" label="Error"><failure /></test-case>') -Encoding UTF8 }; NotBefore = { [datetime]::UtcNow.AddMinutes(-1) } },
            @{ Name = "incomplete"; Prepare = { Set-Content -LiteralPath $fixture -Value ($valid -replace ' end-time="[^"]+"', '') -Encoding UTF8 }; NotBefore = { [datetime]::UtcNow.AddMinutes(-1) } },
            @{ Name = "zero-executed"; Prepare = { Set-Content -LiteralPath $fixture -Value ($valid -replace 'total="2" passed="2"', 'total="0" passed="0"') -Encoding UTF8 }; NotBefore = { [datetime]::UtcNow.AddMinutes(-1) } }
        )
        foreach ($case in $cases) {
            & $case.Prepare
            $rejected = $false
            try { $null = Assert-UnityTestResult -Path $fixture -NotBeforeUtc (& $case.NotBefore) -Platform "SelfTest" }
            catch { $rejected = $true }
            if (-not $rejected) { throw "case '$($case.Name)' unexpectedly passed" }
            $redCases++
            Write-Host "TEST_RESULT_CONTRACT_CASE $($case.Name) => RED"
        }

        # Cleanup semantics are part of freshness: both result and log evidence
        # must be absent before a new Unity process is allowed to start.
        $oldLog = Join-Path $tempRoot "headless-tests.log"
        Set-Content -LiteralPath $fixture -Value $valid -Encoding UTF8
        Set-Content -LiteralPath $oldLog -Value "old log" -Encoding UTF8
        Clear-UnityTestEvidence -Paths @($fixture, $oldLog)
        if ((Test-Path -LiteralPath $fixture) -or (Test-Path -LiteralPath $oldLog)) {
            throw "evidence cleanup fixture remained on disk"
        }
        Write-Host "TEST_RESULT_CONTRACT_SELFTEST_OK cases=9 green=1 red=$redCases cleanup=2/2"
    }
    finally {
        if (Test-Path -LiteralPath $tempRoot) {
            if (-not ([IO.Path]::GetFullPath($tempRoot).StartsWith([IO.Path]::GetTempPath()))) { throw "Unsafe contract fixture path" }
            Remove-Item -LiteralPath $tempRoot -Recurse -Force
        }
    }
}

if ($Mode -eq "testcontract") {
    try {
        Invoke-UnityTestResultContractSelfTest
        exit 0
    }
    catch {
        Write-Host "TEST_RESULT_CONTRACT_SELFTEST_RED $($_.Exception.Message)"
        exit 1
    }
}

. (Join-Path $PSScriptRoot "unity-process.ps1")
function Stop-HeadlessUnityTree {
    param([System.Diagnostics.Process]$UnityProcess)
    Stop-LatticeProcessTree $UnityProcess.Id
}

$Unity = "C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $RepoRoot "Lattice"
Assert-LatticeUnlocked $ProjectPath
& python (Join-Path $RepoRoot 'tools/patch_unity_packages.py')
if ($LASTEXITCODE -ne 0) { throw 'FAILED: package compatibility patch' }
$LogDir = Join-Path $RepoRoot "Builds\logs"
New-Item -ItemType Directory -Force $LogDir | Out-Null
$LogName = if ($Mode -eq "exec") { Get-HeadlessExecLogName $Method } else { "headless-$Mode.log" }
$LogFile = Join-Path $LogDir $LogName

# Unity PlayMode tests use Application.persistentDataPath, so even a green test
# can exercise the real autosave slot. Keep the user's bytes entirely outside the
# test process and restore them on every exit path. Other headless modes do not
# enter player runtime and retain their historical behavior.
$ShieldPlaytestSaves = $Mode -eq "playtests"
$SaveDir = [IO.Path]::GetFullPath((Join-Path $env:USERPROFILE "AppData\LocalLow\Nathan\Lattice\Saves"))
$SaveSuffix = [IO.Path]::Combine("AppData", "LocalLow", "Nathan", "Lattice", "Saves")
if ($ShieldPlaytestSaves -and -not $SaveDir.EndsWith(
        $SaveSuffix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing unsafe PlayMode save path: $SaveDir"
}
$SaveBackup = Join-Path $LogDir ("playmode-human-saves-backup-" + [guid]::NewGuid().ToString("N"))
$HadSaves = $ShieldPlaytestSaves -and (Test-Path -LiteralPath $SaveDir -PathType Container)
$SaveStashed = $false
$SafeToClearPlaytestSaves = $false

$commonArgs = @("-batchmode", "-nographics", "-projectPath", $ProjectPath, "-logFile", $LogFile)
$TestResultsFile = $null

switch ($Mode) {
    "verify" { $args2 = $commonArgs + @("-executeMethod", "Lattice.EditorTools.BatchTools.Verify") }
    "scene"  { $args2 = $commonArgs + @("-executeMethod", "Lattice.EditorTools.BatchTools.CreateBootScenes") }
    "build"  { $args2 = $commonArgs + @("-executeMethod", "Lattice.EditorTools.BatchTools.BuildWindowsPlayer") }
    # POLISH-50B: the DEV player. LATTICE_DEV is injected through
    # BuildPlayerOptions.extraScriptingDefines, never written into ProjectSettings - so the
    # release path above cannot pick it up, and an EditMode test asserts that.
    "builddev" { $args2 = $commonArgs + @("-executeMethod", "Lattice.EditorTools.BatchTools.BuildWindowsDevPlayer") }
    "exec" {
        if (-not $Method) { Write-Error "exec mode requires -Method Full.Type.StaticMethod"; exit 2 }
        $args2 = $commonArgs + @("-executeMethod", $Method)
    }
    "tests" {
        $TestResultsFile = Join-Path $LogDir "editmode-results.xml"
        $args2 = $commonArgs + @("-runTests", "-testPlatform", "EditMode", "-testResults", $TestResultsFile)
        if ($TestFilter) { $args2 += @("-testFilter", $TestFilter) }
    }
    "playtests" {
        $TestResultsFile = Join-Path $LogDir "playmode-results.xml"
        $args2 = $commonArgs + @("-runTests", "-testPlatform", "PlayMode", "-testResults", $TestResultsFile)
        if ($TestFilter) { $args2 += @("-testFilter", $TestFilter) }
    }
}

if ($Mode -in @("verify", "scene", "build", "builddev")) {
    $target = switch ($Mode) {
        'verify' { 'Lattice.EditorTools.BatchTools.Verify' }
        'scene' { 'Lattice.EditorTools.BatchTools.CreateBootScenes' }
        'build' { 'Lattice.EditorTools.BatchTools.BuildWindowsPlayer' }
        'builddev' { 'Lattice.EditorTools.BatchTools.BuildWindowsDevPlayer' }
    }
    $marker = switch ($Mode) { 'verify' {'VERIFY_OK'} 'scene' {'BOOT_SCENES_OK'} default {'BUILD_OK'} }
    & (Join-Path $PSScriptRoot 'exec.ps1') -Method $target -Marker $marker -TimeoutSec $TimeoutSec
    exit $LASTEXITCODE
}
Write-Host "Running Unity headless: $Mode $Method (log: $LogFile)"
$processExitCode = 1
$IsTestRun = $Mode -in @("tests", "playtests")
$testLaunchNotBeforeUtc = [datetime]::MinValue
try {
    if ($ShieldPlaytestSaves) {
        $conflicts = @(Get-LatticeSaveWriters $ProjectPath)
        if ($conflicts.Count -gt 0) {
            throw "PlayMode save shield requires no running Lattice player/editor: $($conflicts.ProcessId -join ',')"
        }
    }
    if ($HadSaves) {
        Write-Host "PLAYMODE SAVE_SHIELD stash=$SaveBackup"
        Move-Item -LiteralPath $SaveDir -Destination $SaveBackup
        $SaveStashed = $true
    }
    if ($ShieldPlaytestSaves) {
        $SafeToClearPlaytestSaves = $SaveStashed -or -not $HadSaves
    }
    # P63 i12 instrument repair: Start-Process -Wait can wait for inherited child
    # processes after the Unity parent has already terminated. That left a compile-red
    # test launch hanging until its caller timeout even though Editor.log recorded
    # "Application will terminate with return code 1" and no Unity process remained.
    # Wait on the exact process handle we launched; its exit code is the evidence owner.
    # Use ProcessStartInfo directly because Windows PowerShell's Start-Process wrapper
    # exposes a blank ExitCode after manual WaitForExit (which PowerShell coerces to 0).
    #
    # Silence watchdog (unity-batchmode-ilpp-hang): ILPP/Bee can deadlock with a
    # silent log after a clean compile, and the exit-hang variant finishes the work,
    # prints its markers, then never exits. Poll the -logFile; a log silent past
    # $StallTimeoutMinutes is a stall - kill the tree and retry once. A stalled TEST
    # run whose results XML is already complete, fresh and green is the exit-hang:
    # judge it green by its evidence instead of re-running the suite.
    $maxAttempts = 1 + [Math]::Max(0, $StallRetries)
    $watchdogOn = $StallTimeoutMinutes -gt 0
    $finalStalled = $false
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        if ($IsTestRun) {
            # Erase both channels before every launch. A zero-exit Unity process must
            # never inherit yesterday's green XML or a prior attempt's partial one.
            Clear-UnityTestEvidence -Paths @($TestResultsFile, $LogFile)
            $testLaunchNotBeforeUtc = [datetime]::UtcNow
        }
        $attemptStartUtc = [datetime]::UtcNow
        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = $Unity
        $startInfo.Arguments = (($args2 | ForEach-Object {
            '"' + ([string]$_).Replace('"', '\"') + '"'
        }) -join ' ')
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $proc = New-Object System.Diagnostics.Process
        $proc.StartInfo = $startInfo
        if (-not $proc.Start()) { throw "Unity process failed to start" }
        $stalled = $false
        if ($watchdogOn) {
            while (-not $proc.WaitForExit(20000)) {
                $logItem = Get-Item -LiteralPath $LogFile -ErrorAction SilentlyContinue
                # Before Unity's first write the old log's mtime predates the attempt;
                # clamp to attempt start so a stale file can never trip an instant kill.
                $lastActivityUtc = if ($logItem -and $logItem.LastWriteTimeUtc -gt $attemptStartUtc) {
                    $logItem.LastWriteTimeUtc
                } else { $attemptStartUtc }
                $silentMinutes = ([datetime]::UtcNow - $lastActivityUtc).TotalMinutes
                if ($silentMinutes -ge $StallTimeoutMinutes -or ([datetime]::UtcNow - $attemptStartUtc).TotalSeconds -ge $TimeoutSec) {
                    Write-Host ("HEADLESS_STALL_DETECTED mode=$Mode attempt=$attempt/$maxAttempts silentMinutes=" +
                        [Math]::Round($silentMinutes, 1) + " log=$LogFile")
                    Stop-HeadlessUnityTree -UnityProcess $proc
                    $stalled = $true
                    break
                }
            }
        }
        if (-not $stalled) {
            $proc.WaitForExit()
            $processExitCode = $proc.ExitCode
            Write-Host "Unity exited with code $processExitCode"
            $finalStalled = $false
            break
        }
        $finalStalled = $true
        if ($IsTestRun) {
            $exitHangGreen = $false
            try {
                $stallPlatform = if ($Mode -eq "tests") { "EditMode" } else { "PlayMode" }
                Assert-UnityTestResult -Path $TestResultsFile -NotBeforeUtc $testLaunchNotBeforeUtc -Platform $stallPlatform | Out-Null
                $exitHangGreen = $true
            }
            catch {}
            if ($exitHangGreen) {
                Write-Host "HEADLESS_STALL_EXIT_HANG mode=$Mode judged=green-by-fresh-results-xml"
                $processExitCode = 0
                $finalStalled = $false
                break
            }
        }
        if ($attempt -lt $maxAttempts) {
            Write-Host "HEADLESS_STALL_RETRY mode=$Mode nextAttempt=$($attempt + 1)/$maxAttempts"
        }
    }
    if ($finalStalled) {
        Write-Host "HEADLESS_STALL_TIMEOUT mode=$Mode attempts=$maxAttempts silentLimitMinutes=$StallTimeoutMinutes"
        $processExitCode = 5
    }
}
finally {
    if ($ShieldPlaytestSaves) {
        $liveWriters = @(Get-LatticeSaveWriters $ProjectPath)
        if ($SafeToClearPlaytestSaves -and $liveWriters.Count -gt 0) {
            throw "PlayMode cleanup refused while a save writer is alive; human stash remains at $SaveBackup"
        }
        if ($SafeToClearPlaytestSaves -and (Test-Path -LiteralPath $SaveDir)) {
            Remove-Item -LiteralPath $SaveDir -Recurse -Force
        }
        if ($SaveStashed -and -not (Test-Path -LiteralPath $SaveBackup -PathType Container)) {
            throw "PlayMode save restore failed: missing stash $SaveBackup"
        }
        if ($SaveStashed) {
            Move-Item -LiteralPath $SaveBackup -Destination $SaveDir
        }
    }
}
if ($IsTestRun) {
    $resultContractCode = 0
    try {
        $platform = if ($Mode -eq "tests") { "EditMode" } else { "PlayMode" }
        $summary = Assert-UnityTestResult -Path $TestResultsFile -NotBeforeUtc $testLaunchNotBeforeUtc -Platform $platform
        Write-Host "TEST_RESULT_CONTRACT_OK platform=$platform total=$($summary.Total) passed=$($summary.Passed) skipped=$($summary.Skipped) fresh=1"
    }
    catch {
        Write-Host "TEST_RESULT_CONTRACT_RED $($_.Exception.Message)"
        $resultContractCode = 3
    }
    if ($processExitCode -ne 0) { exit $processExitCode }
    if ($resultContractCode -ne 0) { exit $resultContractCode }
}
exit $processExitCode
