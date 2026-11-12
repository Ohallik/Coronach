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
    [int]$WaitMinutes = 90
)
$ErrorActionPreference = 'Continue'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$save = Join-Path $env:USERPROFILE 'AppData\LocalLow\Nathan\Lattice\Saves\autosave.json'
$before = if (Test-Path $save) { (Get-FileHash $save).Hash.Substring(0, 8) } else { 'none' }
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
            if (-not $busy) { return $true }
            Start-Sleep -Seconds 2
        }
    }
    return $false
}
function Invoke-Phase([string]$name, [scriptblock]$run, [string]$log) {
    for ($try = 1; $try -le 6; $try++) {
        if (-not (Wait-Ready)) { "FOCUSED $name gave up waiting for a green tree and a free Unity"; return $false }
        # Another stream can start Unity in the seconds between the check and the launch: that is a wait, not a failure.
        try { & $run | Select-Object -Last 3 } catch { if ("$_" -match 'already holds') { Start-Sleep -Seconds 3; $try--; continue } else { throw } }
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
        $taken = $false
        try { & (Join-Path $PSScriptRoot 'headless.ps1') -Mode verify -TimeoutSec 1500 | Select-Object -Last 2 } catch { if ("$_" -match 'already holds') { $taken = $true } else { throw } }
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
$after = if (Test-Path $save) { (Get-FileHash $save).Hash.Substring(0, 8) } else { 'none' }
"FOCUSED saves $before -> $after" + $(if ($before -ne $after) { '  CHANGED: check Builds/logs for a stranded backup' } else { '' })
Get-ChildItem (Join-Path $root 'Builds\logs') -Directory -Filter 'playmode-human-saves-backup-*' -ErrorAction SilentlyContinue | ForEach-Object { "FOCUSED STRANDED BACKUP $($_.Name)"; $failed = $true }
Get-ChildItem (Join-Path $root 'Lattice\Assets') -Filter 'InitTestScene*' -ErrorAction SilentlyContinue | ForEach-Object { "FOCUSED leftover $($_.Name)" }
if ($failed) { "FOCUSED_FAILED $Label"; exit 1 }
"FOCUSED_OK $Label"
exit 0
