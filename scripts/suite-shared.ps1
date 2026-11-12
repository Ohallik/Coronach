<#
.SYNOPSIS
  The whole suite, run in short parts that share Unity with other engineers.

  full-suite.ps1 holds Unity for about 2.5 hours and fails if anyone else starts it. This runs EditMode and then the
  PlayMode classes in N parts (default 8, about 20 minutes each) through focused.ps1, which waits for a tree that
  compiles and a Unity that is free before each part, so builders and renders can run between parts.

.EXAMPLE
  .\scripts\suite-shared.ps1 -Label nov11a            # then: python tools/suite_results.py nov11a
  .\scripts\suite-shared.ps1 -Label nov11a -From 5    # resume at part 5 (same -Parts as before)
#>
param([Parameter(Mandatory)][string]$Label, [int]$Parts = 8, [int]$From = 0)
$ErrorActionPreference = 'Continue'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$out = Join-Path $root 'Builds\quality\suite'
New-Item -ItemType Directory -Force $out | Out-Null
python tools/split_suite.py $Parts | Select-Object -Last 1
$groups = Get-Content Builds/logs/suite_groups.json -Raw | ConvertFrom-Json
if ($From -le 0) {
    & (Join-Path $PSScriptRoot 'focused.ps1') -Label "$Label-edit" -EditMode | Where-Object { $_ -match '^FOCUSED|^FAIL|^     ' }
    Copy-Item (Join-Path $root "Builds\quality\focused\$Label-edit-editmode.xml") (Join-Path $out "$Label-editmode-results.xml") -Force -ErrorAction SilentlyContinue
}
$n = 0
foreach ($group in $groups) {
    $n++
    if ($n -lt $From) { continue }
    "== part $n of $($groups.Count): $($group.classes.Count) classes, about $([int]$group.seconds) s, $(Get-Date -Format HH:mm:ss)"
    & (Join-Path $PSScriptRoot 'focused.ps1') -Label "$Label-part$n" -SkipVerify -Classes ($group.classes -join '|') -TimeoutSec 7000 | Where-Object { $_ -match '^FOCUSED|^FAIL|^     ' }
    Copy-Item (Join-Path $root "Builds\quality\focused\$Label-part$n-playmode.xml") (Join-Path $out "$Label-part$n-playmode-results.xml") -Force -ErrorAction SilentlyContinue
    git checkout -- Lattice/Packages/packages-lock.json Lattice/Assets/_Project/Resources/WorldMaterials/staged-light-line.mat 2>$null
}
"SUITE_SHARED_DONE $Label $(Get-Date -Format HH:mm:ss)"
