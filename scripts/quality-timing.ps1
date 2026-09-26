param([Parameter(Mandatory=$true)][string]$Prefix,[int]$Repeats=1,
    [ValidateSet('Baseline','FirstWarm','Stability')][string]$RouteSet='FirstWarm',
    [ValidateSet('Development','Release')][string[]]$BuildsToRun=@('Development','Release'))
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$failures=@()
foreach($build in $BuildsToRun) {
    foreach($zone in @('decks','tallow')) {
        for($i=1;$i -le $Repeats;$i++) {
            $output="Builds/quality/C1/$Prefix-$($build.ToLower())-$zone-$i"
            $route=if($RouteSet -eq 'Baseline'){"c0-$zone"}elseif($RouteSet -eq 'Stability'){"station-$zone-ten-minute"}else{"station-$zone-first-warm"}
            $diagnostics=if($RouteSet -eq 'Stability'){@('-Census')}else{@()}
            & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'quality-replay.ps1') -Route "docs/quality/routes/$route.json" -Output $output -Build $build @diagnostics
            if($LASTEXITCODE -ne 0){$failures+="$output runtime";continue}
            & python (Join-Path $repo 'tools/quality_analyze.py') (Join-Path $repo $output) --performance | Set-Content (Join-Path $repo "$output/analysis-console.txt")
            if($LASTEXITCODE -ne 0){$failures+="$output timing"}
            Get-Content (Join-Path $repo "$output/analysis.json") -Raw | ConvertFrom-Json | Select-Object valid,p95Ms,p99Ms,worstMs,failures | ConvertTo-Json -Compress
        }
    }
}
if($failures.Count){throw ($failures -join ', ')}
