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
    [int]$TimeoutSec = 5400
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
    if (-not (Test-Path $xmlPath)) { "FOCUSED $name result XML is missing (a compile error in a test? read Builds/logs/headless-playtests.log)"; return $false }
    [xml]$x = Get-Content $xmlPath
    $r = $x.'test-run'
    "FOCUSED $name total=$($r.total) passed=$($r.passed) failed=$($r.failed) skipped=$($r.skipped)"
    foreach ($case in $x.SelectNodes("//test-case[@result='Failed']")) {
        $m = "$($case.failure.message.'#cdata-section')"
        "FAIL $($case.fullname)"
        "     " + $m.Substring(0, [Math]::Min(600, $m.Length)).Replace("`n", ' ')
    }
    return ($r.failed -eq '0' -and [int]$r.total -gt 0)
}

if (-not $SkipVerify) {
    & (Join-Path $PSScriptRoot 'headless.ps1') -Mode verify -TimeoutSec 1500 | Select-Object -Last 2
    if ($LASTEXITCODE -ne 0) { "FOCUSED verify failed (exit $LASTEXITCODE): read the newest Builds/logs/headless-exec-BatchTools-Verify-*.log"; exit 1 }
}
if ($EditMode) {
    & (Join-Path $PSScriptRoot 'headless.ps1') -Mode tests -TimeoutSec 1500 | Select-Object -Last 2
    $xml = Join-Path $root 'Builds\logs\editmode-results.xml'
    if (Test-Path $xml) { Copy-Item $xml (Join-Path $out "$Label-editmode.xml") -Force }
    if (-not (Show-Results $xml 'EditMode')) { $failed = $true }
}
if ($Classes) {
    $filter = ($Classes.Split('|') | Where-Object { $_ } | ForEach-Object { if ($_ -like 'Lattice.*') { $_ } else { "Lattice.Tests.PlayMode.$_" } }) -join '|'
    $xml = Join-Path $root 'Builds\logs\playmode-results.xml'
    if (Test-Path $xml) { Remove-Item $xml -Force }
    & (Join-Path $PSScriptRoot 'headless.ps1') -Mode playtests -Graphics -TestFilter $filter -TimeoutSec $TimeoutSec | Select-Object -Last 3
    if (Test-Path $xml) { Copy-Item $xml (Join-Path $out "$Label-playmode.xml") -Force }
    if (-not (Show-Results $xml 'PlayMode')) { $failed = $true }
}
$after = if (Test-Path $save) { (Get-FileHash $save).Hash.Substring(0, 8) } else { 'none' }
"FOCUSED saves $before -> $after" + $(if ($before -ne $after) { '  CHANGED: check Builds/logs for a stranded backup' } else { '' })
Get-ChildItem (Join-Path $root 'Builds\logs') -Directory -Filter 'playmode-human-saves-backup-*' -ErrorAction SilentlyContinue | ForEach-Object { "FOCUSED STRANDED BACKUP $($_.Name)"; $failed = $true }
Get-ChildItem (Join-Path $root 'Lattice\Assets') -Filter 'InitTestScene*' -ErrorAction SilentlyContinue | ForEach-Object { "FOCUSED leftover $($_.Name)" }
if ($failed) { "FOCUSED_FAILED $Label"; exit 1 }
"FOCUSED_OK $Label"
exit 0
