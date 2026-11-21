# Runs replay routes in order, each resuming from the last validated segment's saves.
# Run the whole chain as ONE call and issue nothing else meanwhile: every segment is a
# focus-checked player window. A rejected segment is kept and retried once under a new
# name, because some fights are random; a segment that keeps failing stops the chain.
# A boss lost at its door is not a rejected segment any more: the driver takes the defeat
# screen's Retry itself, up to twice (a route step's "bossDoor"), and each try is noted here
# from the segment's run.json. A segment with three tries at a boss runs long, hence the timeout.
#   .\scripts\quality-chain.ps1 -Output Builds/quality/QA/base [-Capture] [-Build Release]
param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string[]]$Routes=@('chapter-01-new-game-decks','chapter-02-continue-sorrel','chapter-03-defeat-retry','chapter-04-haul-preparation','chapter-05-drill-equipment','chapter-06-burrower','chapter-07-enter-hushwell','chapter-08-hushwell-nursery','chapter-09-calibrated-gullet','chapter-10-gullet-evasive','chapter-11-tallow-reading-return'),
    [string]$ResumeFrom,
    [ValidateSet('Development','Release')][string]$Build='Release',
    [switch]$Capture,
    [int]$Retries=1,
    [int]$FirstNumber=1,
    [string[]]$PlayerArgs=@(),
    [int]$TimeoutSec=1900,
    # Reconnaissance (D290): finds where the routes stop while engineers work at the same machine. Its lines say SCOUTED, never
    # PASS; nothing it writes is evidence, and a chain without this switch cannot resume from any of its segments.
    [switch]$Reconnaissance,
    # With -Reconnaissance: a route that found faults but ended in the zone its last step expects does not stop the chain; the next
    # route goes on from its saves, and the chain says so. One run then finds every stop in a chapter. A route that ends somewhere
    # else still stops it.
    [switch]$PastFaults,
    # With -Reconnaissance: a window after all, which a run that captures needs (quality-replay.ps1 says why).
    [switch]$Windowed
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$summary=Join-Path $repo ($Output+'/chain.txt')
New-Item -ItemType Directory -Force -Path (Split-Path $summary) | Out-Null
function Note([string]$text){Add-Content -LiteralPath $summary -Value $text;Write-Output $text}
if($PastFaults -and -not $Reconnaissance){throw 'Only reconnaissance goes on past faults'}
$went=if($Reconnaissance){'SCOUTED'}else{'PASS'}
# Whether a faulted reconnaissance segment ended where its route's last step expects: its saves are then worth going on from.
function EndedInPlace([string]$name){
    try{
        $folder=Join-Path $repo $name
        $route=Get-Content -LiteralPath (Join-Path $folder 'route.json') -Raw | ConvertFrom-Json
        $state=Get-Content -LiteralPath (Join-Path $folder 'final-state.json') -Raw | ConvertFrom-Json
        $expected=@($route.steps | Where-Object { $_.expectedScene } | Select-Object -Last 1)[0].expectedScene
        if(-not $expected -or $state.zone -ne $expected){return $false}
        # And with everything the route's steps expected of the story set: a route that walked on from a boss it never beat ends in the
        # right zone too (the Last Warden, November 14), and whatever is played from that save is nonsense.
        foreach($flag in @($route.steps | Where-Object { $_.expectedFlag } | ForEach-Object { $_.expectedFlag } | Select-Object -Unique)){
            if(-not ($state.flags.PSObject.Properties[$flag] -and $state.flags.$flag)){return $false}
        }
        return $true
    }catch{return $false}
}
if($Reconnaissance){Note 'RECONNAISSANCE: nothing below is evidence of a pass (D290)'}
# What the driver did of its own accord in a segment, passed or not: every try at a boss from its door, and what a stock step put in the pouch.
# A note that can't be read is left out: it must never turn a passed segment into a failed one.
function NoteDriver([string]$name){
    try{
        $run=Join-Path $repo ($name+'/run.json')
        if(-not (Test-Path -LiteralPath $run)){return}
        $report=Get-Content -LiteralPath $run -Raw | ConvertFrom-Json
        foreach($try in $report.bossAttempts){if($try){Note "  boss $($try.boss) attempt $($try.attempt): $($try.outcome) in step $($try.lastStep) '$($try.lastStepName)' after $([int]($try.to-$try.from)) s (boss Integrity left: $($try.bossIntegrity))"}}
        foreach($stock in $report.stocks){if($stock){Note "  stock $($stock.item): $($stock.before) -> $($stock.after), $($stock.presses) presses$(if($stock.refused){', the last one refused'})"}}
    }catch{}
}
$previous=$ResumeFrom;$number=$FirstNumber;$passed=@();$faulted=@()
foreach($route in $Routes){
    $done=$false
    foreach($attempt in 0..$Retries){
        $suffix=if($attempt -eq 0){''}else{[string][char](97+$attempt)}
        $name=$Output+('/s{0:D2}{1}' -f $number,$suffix)
        $arguments=@{Route="docs/quality/routes/$route.json";Output=$name;Build=$Build;TimeoutSec=$TimeoutSec}
        if($Capture){$arguments.Capture=$true}
        if($previous){$arguments.ResumeFrom=$previous}
        if($PlayerArgs.Count){$arguments.PlayerArgs=$PlayerArgs}
        if($Reconnaissance){$arguments.Reconnaissance=$true}
        if($PastFaults){$arguments.PastFaults=$true}
        if($Windowed){$arguments.Windowed=$true}
        try{
            & (Join-Path $PSScriptRoot 'quality-replay.ps1') @arguments | Out-Null
            & python (Join-Path $repo 'tools/quality_analyze.py') (Join-Path $repo $name) | Out-Null
            if($LASTEXITCODE -ne 0){throw 'analysis rejected'}
            Note "$went $route $name"
            NoteDriver $name
            $previous=$name;$passed+=$name;$done=$true;break
        }catch{
            Note "FAIL $route $name : $($_.Exception.Message)"
            $run=Join-Path $repo ($name+'/run.json')
            if(Test-Path -LiteralPath $run){(Get-Content -LiteralPath $run -Raw | ConvertFrom-Json).failures | ForEach-Object {Note "  failure: $_"}}
            NoteDriver $name
        }
    }
    if(-not $done -and $PastFaults -and (EndedInPlace $name)){
        # The analysis a rejected segment never got, so the next route's resume can read it (its verdict is already "faults").
        & python (Join-Path $repo 'tools/quality_analyze.py') (Join-Path $repo $name) | Out-Null
        Note "FAULTS $route $name : it ended where its route ends, so the chain goes on from its saves (reconnaissance)"
        $previous=$name;$faulted+=$name;$done=$true
    }
    if(-not $done){Note "CHAIN_STOPPED at $route";exit 1}
    $number++
}
Note "$(if($Reconnaissance){'CHAIN_SCOUTED'}else{'CHAIN_OK'}) $($passed.Count) segments$(if($faulted.Count){", and $($faulted.Count) with faults stepped past: $($faulted -join ', ')"})"
& python (Join-Path $repo 'tools/quality_chain_summary.py') @($passed | ForEach-Object {Join-Path $repo $_}) | Tee-Object -FilePath $summary -Append
