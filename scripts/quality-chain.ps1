# Runs replay routes in order, each resuming from the last validated segment's saves.
# Run the whole chain as ONE call and issue nothing else meanwhile: every segment is a
# focus-checked player window. A rejected segment is kept and retried once under a new
# name, because some fights are random; a segment that keeps failing stops the chain.
#   .\scripts\quality-chain.ps1 -Output Builds/quality/QA/base [-Capture] [-Build Release]
param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string[]]$Routes=@('chapter-01-new-game-decks','chapter-02-continue-sorrel','chapter-03-defeat-retry','chapter-04-haul-preparation','chapter-05-drill-equipment','chapter-06-burrower','chapter-07-enter-hushwell','chapter-08-hushwell-nursery','chapter-09-calibrated-gullet','chapter-10-gullet-evasive','chapter-11-tallow-reading-return'),
    [string]$ResumeFrom,
    [ValidateSet('Development','Release')][string]$Build='Release',
    [switch]$Capture,
    [int]$Retries=1,
    [int]$FirstNumber=1,
    [int]$TimeoutSec=1300
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$summary=Join-Path $repo ($Output+'/chain.txt')
New-Item -ItemType Directory -Force -Path (Split-Path $summary) | Out-Null
function Note([string]$text){Add-Content -LiteralPath $summary -Value $text;Write-Output $text}
$previous=$ResumeFrom;$number=$FirstNumber;$passed=@()
foreach($route in $Routes){
    $done=$false
    foreach($attempt in 0..$Retries){
        $suffix=if($attempt -eq 0){''}else{[string][char](97+$attempt)}
        $name=$Output+('/s{0:D2}{1}' -f $number,$suffix)
        $arguments=@{Route="docs/quality/routes/$route.json";Output=$name;Build=$Build;TimeoutSec=$TimeoutSec}
        if($Capture){$arguments.Capture=$true}
        if($previous){$arguments.ResumeFrom=$previous}
        try{
            & (Join-Path $PSScriptRoot 'quality-replay.ps1') @arguments | Out-Null
            & python (Join-Path $repo 'tools/quality_analyze.py') (Join-Path $repo $name) | Out-Null
            if($LASTEXITCODE -ne 0){throw 'analysis rejected'}
            Note "PASS $route $name"
            $previous=$name;$passed+=$name;$done=$true;break
        }catch{
            Note "FAIL $route $name : $($_.Exception.Message)"
            $run=Join-Path $repo ($name+'/run.json')
            if(Test-Path -LiteralPath $run){(Get-Content -LiteralPath $run -Raw | ConvertFrom-Json).failures | ForEach-Object {Note "  failure: $_"}}
        }
    }
    if(-not $done){Note "CHAIN_STOPPED at $route";exit 1}
    $number++
}
Note "CHAIN_OK $($passed.Count) segments"
& python (Join-Path $repo 'tools/quality_chain_summary.py') @($passed | ForEach-Object {Join-Path $repo $_}) | Tee-Object -FilePath $summary -Append
