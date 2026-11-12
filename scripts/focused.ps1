<#
.SYNOPSIS
  One call for the lead's usual check after a batch of edits: compile in Unity, optionally the EditMode suite, then
  the named PlayMode classes, with a short summary of failures and a before/after hash of Nathan's autosave.

.EXAMPLE
  .\scripts\focused.ps1 -Classes 'QaFightTests|EnemyHitTests' -EditMode -Label impact1
  .\scripts\focused.ps1 -Classes 'Lattice.Tests.PlayMode.QaFightTests.SomeOneTest'      # a full name passes through

  Results are copied to Builds/quality/focused/<label>-{editmode,playmode}.xml. Exit code is 0 only when everything
  that ran passed. Run it alone: only one Unity may own Lattice/.
#>
param(
    [string]$Classes = '',
    [switch]$EditMode,
    [switch]$SkipVerify,
    [string]$Label = (Get-Date -Format 'MMdd-HHmm'),
    [int]$TimeoutSec = 5400,
    [int]$WaitMinutes = 90,
    # Tests run in a test mirror when one exists (..\CoronachTest\A and so on, made by mirror-sync.ps1): a second copy
    # of the project with its own Unity, so tests don't wait for builders and builders don't wait for tests. Each run
    # copies the working tree across first. -Mirror names one mirror; -NoSync tests the mirror as it stands (the suite
    # copies once and runs every part on that frozen copy); -Main runs in the main project the old way.
    [string]$Mirror = '',
    [switch]$NoSync,
    [switch]$Main,
    # Every shared test case that names one of these zones, and nothing else: -Zones 'Driftreed,SporeVeil'. The shared
    # zone classes walk every zone in the game and take over an hour whole; one zone's cases take a few minutes.
    [string]$Zones = ''
)
if ($Zones) {
    $byZone = $Zones.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ } | ForEach-Object { "Lattice.Tests.PlayMode.*$_.*" }
    $Classes = (@($Classes) + @($byZone) | Where-Object { $_ }) -join '|'
}
$ErrorActionPreference = 'Continue'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$save = Join-Path $env:USERPROFILE 'AppData\LocalLow\Nathan\Lattice\Saves\autosave.json'
# The "before" hash is taken when this run gets its own turn at Unity, not at script start: while another test run
# holds Unity its save shield has test saves in the live folder, and a hash taken then reads as a change (November 11).
$script:before = $null
function Save-Hash { try { if (Test-Path $save) { return (Get-FileHash $save -ErrorAction Stop).Hash.Substring(0, 8) } } catch { }; return 'none' }
function Note-Before { if (-not $script:before) { $script:before = Save-Hash } }
$out = Join-Path $root 'Builds\quality\focused'
New-Item -ItemType Directory -Force $out | Out-Null
$failed = $false

function Show-Results([string]$xmlPath, [string]$name) {
    # The verdict goes in a variable, not the return value: a function's printed lines and its return value share one
    # output stream, so "if (-not (Show-Results ...))" swallowed the lines and always read as passed (November 11).
    $script:lastOk = $false
    if (-not (Test-Path $xmlPath)) { "FOCUSED $name result XML is missing (a compile error in a test? read Builds/logs/headless-playtests.log)"; return }
    [xml]$x = Get-Content $xmlPath
    $r = $x.'test-run'
    "FOCUSED $name total=$($r.total) passed=$($r.passed) failed=$($r.failed) skipped=$($r.skipped)"
    foreach ($case in $x.SelectNodes("//test-case[@result='Failed']")) {
        $m = "$($case.failure.message.'#cdata-section')"
        "FAIL $($case.fullname)"
        "     " + $m.Substring(0, [Math]::Min(600, $m.Length)).Replace("`n", ' ')
    }
    $script:lastOk = ($r.failed -eq '0' -and [int]$r.total -gt 0)
}

# ---- In a test mirror ------------------------------------------------------------------------------------------
$mirrorBase = Join-Path (Split-Path $root -Parent) 'CoronachTest'
$pool = @()
if (-not $Main -and (Test-Path $mirrorBase)) {
    # S belongs to the whole suite; a focused run takes whichever of the others is free.
    $pool = @(Get-ChildItem $mirrorBase -Directory | Where-Object { (Test-Path (Join-Path $_.FullName '.test-mirror')) -and (Test-Path (Join-Path $_.FullName 'Lattice\Library')) } | ForEach-Object { $_.Name })
    # While a whole suite runs (suite-shared.ps1 leaves a marker; one older than five hours is a suite that died), S is
    # the suite's alone. Otherwise it is a fourth place for focused runs: fourteen engineers and three mirrors got
    # five test runs through in half an hour (November 12).
    $suiteMark = Join-Path $mirrorBase '.suite-running'
    $suiteOn = (Test-Path $suiteMark) -and ((Get-Item $suiteMark).LastWriteTime -gt (Get-Date).AddHours(-5))
    $pool = if ($Mirror) { @($pool | Where-Object { $_ -eq $Mirror }) } else { @($pool | Where-Object { $_ -ne 'S' -or -not $suiteOn }) }
}
if ($pool.Count -gt 0) {
    $deadline = (Get-Date).AddMinutes($WaitMinutes)
    $turn = $null
    $name = $null
    while (-not $turn -and (Get-Date) -lt $deadline) {
        foreach ($n in $pool) {
            $m = New-Object System.Threading.Mutex($false, "Global\CoronachMirror$n")
            $got = $false
            try { $got = $m.WaitOne(0) } catch [System.Threading.AbandonedMutexException] { $got = $true }
            if ($got) { $turn = $m; $name = $n; break }
            $m.Dispose()
        }
        if (-not $turn) { Start-Sleep -Seconds 3 }
    }
    if (-not $turn) { "FOCUSED gave up waiting for a free test mirror"; "FOCUSED_FAILED $Label"; exit 1 }
    try {
        $mroot = Join-Path $mirrorBase $name
        $mlogs = Join-Path $mroot 'Builds\logs'
        $script:hs = Join-Path $mroot 'scripts\headless.ps1'
        function Sync-Mirror {
            if ($NoSync) { return $true }
            while ((Get-Date) -lt $deadline) {
                if ((python tools/typecheck.py | Select-Object -Last 1) -ne 'TYPECHECK_OK') { Start-Sleep -Seconds 30; continue }
                $line = & (Join-Path $PSScriptRoot 'mirror-sync.ps1') -Name $name | Select-Object -Last 1
                if ("$line" -like 'MIRROR_SYNC_OK*') { return $true }
                "FOCUSED $line"; return $false
            }
            return $false
        }
        # Copy, run, and if somebody's half-finished edit was copied (a compile error in the mirror), copy and run again.
        function Mirror-Phase([string]$phase, [scriptblock]$run, [string]$log) {
            for ($try = 1; $try -le 6; $try++) {
                if (-not (Sync-Mirror)) { "FOCUSED $phase could not copy the tree to mirror $name (the tree stayed red, or the copy failed)"; return }
                try { & $run | Select-Object -Last 3 } catch { "FOCUSED $phase launch: $($_.Exception.Message)" }
                $compile = $log -and (Test-Path $log) -and (Select-String -Path $log -Pattern 'error CS\d+' -Quiet)
                if (-not $compile) { return }
                "FOCUSED $phase hit a compile error in the mirror, try $try"
                Select-String -Path $log -Pattern 'error CS\d+' | Select-Object -First 2 | ForEach-Object { '  ' + $_.Line.Substring(0, [Math]::Min(200, $_.Line.Length)) }
                if ($NoSync) { return }
                Start-Sleep -Seconds 60
            }
        }
        if ($EditMode) {
            $xml = Join-Path $mlogs 'editmode-results.xml'
            Mirror-Phase 'EditMode' { & $script:hs -Mode tests -TimeoutSec 1500 } (Join-Path $mlogs 'headless-tests.log')
            $copy = Join-Path $out "$Label-editmode.xml"
            if (Test-Path $copy) { Remove-Item $copy -Force }
            if (Test-Path $xml) { Copy-Item $xml $copy -Force }
            Show-Results $copy 'EditMode'
            if (-not $script:lastOk) { $failed = $true }
        }
        if ($Classes) {
            $script:filter = ($Classes.Split('|') | Where-Object { $_ } | ForEach-Object { if ($_ -like 'Lattice.*') { $_ } else { "Lattice.Tests.PlayMode.$_" } }) -join '|'
            $xml = Join-Path $mlogs 'playmode-results.xml'
            Mirror-Phase 'PlayMode' { & $script:hs -Mode playtests -Graphics -TestFilter $script:filter -TimeoutSec $TimeoutSec } (Join-Path $mlogs 'headless-playtests.log')
            $copy = Join-Path $out "$Label-playmode.xml"
            if (Test-Path $copy) { Remove-Item $copy -Force }
            if (Test-Path $xml) { Copy-Item $xml $copy -Force }
            Show-Results $copy 'PlayMode'
            if (-not $script:lastOk) { $failed = $true }
        }
        "FOCUSED ran in test mirror $name ($mroot): its logs and anything a test writes are under that folder's Builds; Nathan's saves are not involved"
    } finally { $turn.ReleaseMutex() }
    if ($failed) { "FOCUSED_FAILED $Label"; exit 1 }
    "FOCUSED_OK $Label"
    exit 0
}

# ---- In the main project (no mirror, or -Main) -----------------------------------------------------------------
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$project = Join-Path $root 'Lattice'
# Several engineers share this tree and this Unity: wait for a tree that compiles and a Unity that is free, and try a
# phase again when somebody's half-finished edit breaks the compile under it.
function Wait-Ready {
    # Type-check first (nine seconds), then watch for Unity every two seconds: checking Unity first left a nine-second
    # gap in which another engineer's run took it, every time, for ten minutes (November 11).
    $deadline = (Get-Date).AddMinutes($WaitMinutes)
    while ((Get-Date) -lt $deadline) {
        if ((python tools/typecheck.py | Select-Object -Last 1) -ne 'TYPECHECK_OK') { Start-Sleep -Seconds 30; continue }
        for ($poll = 0; $poll -lt 40; $poll++) {
            $busy = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($project) }).Count -gt 0
            if (-not $busy) {
                # Yield to short jobs: a builder or a render launches the instant Unity is free, a test run only once
                # Unity has stayed free for six seconds, so twenty-minute test parts can't starve one-minute builds.
                Start-Sleep -Seconds 6
                $busy = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($project) }).Count -gt 0
                if (-not $busy) { Note-Before; return $true }
            }
            Start-Sleep -Seconds 2
        }
    }
    return $false
}
# One launch at a time, machine-wide (see exec-when-green.ps1): a test run takes the turn only if nobody holds it.
$unityTurn = New-Object System.Threading.Mutex($false, 'Global\CoronachUnityTurn')
function Take-Turn { try { return $unityTurn.WaitOne(0) } catch [System.Threading.AbandonedMutexException] { return $true } }
function Invoke-Phase([string]$name, [scriptblock]$run, [string]$log) {
    for ($try = 1; $try -le 6; $try++) {
        if (-not (Wait-Ready)) { "FOCUSED $name gave up waiting for a green tree and a free Unity"; return $false }
        if (-not (Take-Turn)) { Start-Sleep -Seconds 5; $try--; continue }
        # Another stream can start Unity in the seconds between the check and the launch: that is a wait, not a failure.
        $taken = $false
        try { try { & $run | Select-Object -Last 3 } catch { if ("$_" -match 'already holds') { $taken = $true } else { throw } } } finally { $unityTurn.ReleaseMutex() }
        if ($taken) { Start-Sleep -Seconds 3; $try--; continue }
        $code = $LASTEXITCODE
        $compile = $log -and (Test-Path $log) -and ((Get-Item $log).LastWriteTime -gt (Get-Date).AddMinutes(-30)) -and (Select-String -Path $log -Pattern 'error CS\d+' -Quiet)
        if ($compile) { "FOCUSED $name hit a compile error (someone mid-edit), try $try"; Select-String -Path $log -Pattern 'error CS\d+' | Select-Object -First 2 | ForEach-Object { '  ' + $_.Line.Substring(0, [Math]::Min(200, $_.Line.Length)) }; Start-Sleep -Seconds 60; continue }
        return $true
    }
    return $false
}

if (-not $SkipVerify) {
    $verifyLog = { (Get-ChildItem (Join-Path $root 'Builds\logs') -Filter 'headless-exec-BatchTools-Verify-*.log' | Sort-Object LastWriteTime | Select-Object -Last 1).FullName }
    $ok = $false
    for ($try = 1; $try -le 6 -and -not $ok; $try++) {
        if (-not (Wait-Ready)) { break }
        if (-not (Take-Turn)) { Start-Sleep -Seconds 5; $try--; continue }
        $taken = $false
        try { try { & (Join-Path $PSScriptRoot 'headless.ps1') -Mode verify -TimeoutSec 1500 | Select-Object -Last 2 } catch { if ("$_" -match 'already holds') { $taken = $true } else { throw } } } finally { $unityTurn.ReleaseMutex() }
        if ($taken) { Start-Sleep -Seconds 3; $try--; continue }
        if ($LASTEXITCODE -eq 0) { $ok = $true } else { "FOCUSED verify failed, try $try"; Start-Sleep -Seconds 60 }
    }
    if (-not $ok) { "FOCUSED verify failed: read $(& $verifyLog)"; exit 1 }
}
if ($EditMode) {
    $xml = Join-Path $root 'Builds\logs\editmode-results.xml'
    if (Test-Path $xml) { Remove-Item $xml -Force }
    [void](Invoke-Phase 'EditMode' { & (Join-Path $PSScriptRoot 'headless.ps1') -Mode tests -TimeoutSec 1500 } (Join-Path $root 'Builds\logs\headless-tests.log'))
    if (Test-Path $xml) { Copy-Item $xml (Join-Path $out "$Label-editmode.xml") -Force }
    Show-Results $xml 'EditMode'
    if (-not $script:lastOk) { $failed = $true }
}
if ($Classes) {
    $filter = ($Classes.Split('|') | Where-Object { $_ } | ForEach-Object { if ($_ -like 'Lattice.*') { $_ } else { "Lattice.Tests.PlayMode.$_" } }) -join '|'
    $xml = Join-Path $root 'Builds\logs\playmode-results.xml'
    if (Test-Path $xml) { Remove-Item $xml -Force }
    $script:filter = $filter
    [void](Invoke-Phase 'PlayMode' { & (Join-Path $PSScriptRoot 'headless.ps1') -Mode playtests -Graphics -TestFilter $script:filter -TimeoutSec $TimeoutSec } (Join-Path $root 'Builds\logs\headless-playtests.log'))
    if (Test-Path $xml) { Copy-Item $xml (Join-Path $out "$Label-playmode.xml") -Force }
    Show-Results $xml 'PlayMode'
    if (-not $script:lastOk) { $failed = $true }
}
Note-Before
$before = $script:before
$after = Save-Hash
"FOCUSED saves $before -> $after" + $(if ($before -ne $after) { '  CHANGED: check Builds/logs for a stranded backup' } else { '' })
Get-ChildItem (Join-Path $root 'Builds\logs') -Directory -Filter 'playmode-human-saves-backup-*' -ErrorAction SilentlyContinue | ForEach-Object { "FOCUSED STRANDED BACKUP $($_.Name)"; $failed = $true }
Get-ChildItem (Join-Path $root 'Lattice\Assets') -Filter 'InitTestScene*' -ErrorAction SilentlyContinue | ForEach-Object { "FOCUSED leftover $($_.Name)" }
if ($failed) { "FOCUSED_FAILED $Label"; exit 1 }
"FOCUSED_OK $Label"
exit 0
