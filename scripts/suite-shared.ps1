<#
.SYNOPSIS
  The whole suite, run beside everybody else's work.

  It runs in test mirror S (..\CoronachTest\S, see mirror-sync.ps1): the working tree is copied there once, when it
  compiles, and EditMode and then every PlayMode part run on that frozen copy, so the verdict is for one state of the
  tree and nobody's later edit can change it half-way. Builders, renders and focused test runs carry on meanwhile.

  -Main runs it in the main project the old way (parts that wait for a free Unity between other people's runs).

.EXAMPLE
  .\scripts\suite-shared.ps1 -Label nov12a            # then: python tools/suite_results.py nov12a
  .\scripts\suite-shared.ps1 -Label nov12a -From 5    # resume at part 5 on the same copy (same -Parts as before)
#>
param([Parameter(Mandatory)][string]$Label, [int]$Parts = 8, [int]$From = 0, [string]$Mirror = 'S', [switch]$Main)
$ErrorActionPreference = 'Continue'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$out = Join-Path $root 'Builds\quality\suite'
New-Item -ItemType Directory -Force $out | Out-Null
python tools/split_suite.py $Parts | Select-Object -Last 1
$groups = Get-Content Builds/logs/suite_groups.json -Raw | ConvertFrom-Json
$where = @{ Main = $true }
# Mirror S is the suite's alone while this runs; focused runs use it otherwise (focused.ps1 reads this marker).
$suiteMark = Join-Path (Join-Path (Split-Path $root -Parent) 'CoronachTest') '.suite-running'
if (-not $Main) { Set-Content -LiteralPath $suiteMark -Value "$Label $(Get-Date -Format s)" -Encoding ascii -ErrorAction SilentlyContinue }
if (-not $Main) {
    if ($From -le 0) {
        $synced = $false
        for ($i = 0; $i -lt 120 -and -not $synced; $i++) {
            if ((python tools/typecheck.py | Select-Object -Last 1) -ne 'TYPECHECK_OK') { if ($i -eq 0) { "SUITE waiting for a tree that compiles" }; Start-Sleep -Seconds 30; continue }
            $line = & (Join-Path $PSScriptRoot 'mirror-sync.ps1') -Name $Mirror | Select-Object -Last 1
            "SUITE $line"
            $synced = "$line" -like 'MIRROR_SYNC_OK*'
            if (-not $synced) { Start-Sleep -Seconds 30 }
        }
        if (-not $synced) { "SUITE_SHARED_FAILED $Label could not copy the tree to mirror $Mirror"; exit 1 }
    }
    $where = @{ Mirror = $Mirror; NoSync = $true }
}
if ($From -le 0) {
    & (Join-Path $PSScriptRoot 'focused.ps1') -Label "$Label-edit" -EditMode -SkipVerify @where | Where-Object { $_ -match '^FOCUSED|^FAIL|^     ' }
    Copy-Item (Join-Path $root "Builds\quality\focused\$Label-edit-editmode.xml") (Join-Path $out "$Label-editmode-results.xml") -Force -ErrorAction SilentlyContinue
}
$n = 0
foreach ($group in $groups) {
    $n++
    if ($n -lt $From) { continue }
    "== part $n of $($groups.Count): $($group.classes.Count) classes, about $([int]$group.seconds) s, $(Get-Date -Format HH:mm:ss)"
    & (Join-Path $PSScriptRoot 'focused.ps1') -Label "$Label-part$n" -SkipVerify -Classes ($group.classes -join '|') -TimeoutSec 7000 @where | Where-Object { $_ -match '^FOCUSED|^FAIL|^     ' }
    Copy-Item (Join-Path $root "Builds\quality\focused\$Label-part$n-playmode.xml") (Join-Path $out "$Label-part$n-playmode-results.xml") -Force -ErrorAction SilentlyContinue
    if ($Main) { git checkout -- Lattice/Packages/packages-lock.json Lattice/Assets/_Project/Resources/WorldMaterials/staged-light-line.mat 2>$null }
}
if (-not $Main) { Remove-Item -LiteralPath $suiteMark -Force -ErrorAction SilentlyContinue }
"SUITE_SHARED_DONE $Label $(Get-Date -Format HH:mm:ss)"
