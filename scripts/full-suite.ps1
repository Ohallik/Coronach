# The whole test suite: EditMode, then PlayMode in parts by class (tools/split_suite.py), so no single time limit can
# lose every result. Unfiltered, PlayMode takes about 2.5 hours, and a timeout loses all of it and strands Nathan's saves.
# Run it in the background and start no other Unity work while it runs:
#   .\scripts\full-suite.ps1 -Label <name> [-Parts 3]
# Results go to Builds/quality/suite/<Label>-editmode-results.xml and <Label>-partN-playmode-results.xml:
#   python tools/suite_results.py <Label>
param([Parameter(Mandatory=$true)][string]$Label,[int]$Parts=3)
$ErrorActionPreference='Continue'
Set-Location (Split-Path -Parent $PSScriptRoot)
$out='Builds/quality/suite';New-Item -ItemType Directory -Force $out | Out-Null
python tools/split_suite.py $Parts
$groups=Get-Content Builds/logs/suite_groups.json -Raw | ConvertFrom-Json
Remove-Item Builds/logs/editmode-results.xml -ErrorAction SilentlyContinue
.\scripts\headless.ps1 -Mode tests -TimeoutSec 900 | Select-Object -Last 2
"editmode exit=$LASTEXITCODE"
git checkout -- Lattice/Packages/packages-lock.json
Copy-Item Builds/logs/editmode-results.xml "$out/$Label-editmode-results.xml" -ErrorAction SilentlyContinue
$n=0
foreach($group in $groups){
    $n++
    $filter=($group.classes | ForEach-Object { "Lattice.Tests.PlayMode.$_" }) -join '|'
    "== part $n of $($groups.Count): $($group.classes.Count) classes, about $([int]$group.seconds) s, $(Get-Date -Format HH:mm:ss)"
    # A part that writes nothing mustn't be counted with the last part's results.
    Remove-Item Builds/logs/playmode-results.xml -ErrorAction SilentlyContinue
    .\scripts\headless.ps1 -Mode playtests -Graphics -TestFilter $filter -TimeoutSec 7000 | Select-Object -Last 2
    "part $n exit=$LASTEXITCODE $(Get-Date -Format HH:mm:ss)"
    git checkout -- Lattice/Packages/packages-lock.json
    Copy-Item Builds/logs/playmode-results.xml "$out/$Label-part$n-playmode-results.xml" -ErrorAction SilentlyContinue
}
$saves=Join-Path $env:USERPROFILE 'AppData\LocalLow\Nathan\Lattice\Saves'
Get-ChildItem $saves -ErrorAction SilentlyContinue | ForEach-Object { "save $($_.Name) $((Get-FileHash $_.FullName -Algorithm SHA256).Hash.Substring(0,12))" }
"FULL_SUITE_DONE $(Get-Date -Format HH:mm:ss)"
