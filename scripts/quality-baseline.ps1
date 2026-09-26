param([string]$Run='capture-01')
$failed=@()
foreach ($fixture in @('c0-tallow','c0-cinder-dock','c0-motion-hub_decks','c0-motion-arena_ground','c0-motion-sorrel_ridges')) {
    try {
        & (Join-Path $PSScriptRoot 'quality-replay.ps1') -Route ('docs/quality/routes/'+$fixture+'.json') -Output ('Builds/quality/C0/'+$fixture+'-'+$Run) -Capture -TimeoutSec 240
    } catch { Write-Output $_.Exception.Message; $failed+=$fixture }
}
if ($failed.Count) { throw ('Incomplete baseline fixtures: '+($failed -join ', ')) }
