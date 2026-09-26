param([Parameter(Mandatory=$true)][string]$Prefix,[int]$Repeats=1,
    [ValidateSet('Baseline','FirstWarm','Stability')][string]$RouteSet='FirstWarm')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$failures=@()
foreach($build in @('Development','Release')) {
    foreach($zone in @('decks','tallow')) {
        for($i=1;$i -le $Repeats;$i++) {
            $output="Builds/quality/C1/$Prefix-$($build.ToLower())-$zone-$i"
            $route=if($RouteSet -eq 'Baseline'){"c0-$zone"}elseif($RouteSet -eq 'Stability'){"station-$zone-ten-minute"}else{"station-$zone-first-warm"}
            & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'quality-replay.ps1') -Route "docs/quality/routes/$route.json" -Output $output -Build $build
            if($LASTEXITCODE -ne 0){$failures+="$output runtime";continue}
            & python (Join-Path $repo 'tools/quality_analyze.py') (Join-Path $repo $output) --performance
            if($LASTEXITCODE -ne 0){$failures+="$output timing"}
        }
    }
}
if($failures.Count){throw ($failures -join ', ')}
