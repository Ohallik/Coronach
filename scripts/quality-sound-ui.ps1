param([Parameter(Mandatory=$true)][string]$Run,[ValidateSet(720,1080)][int]$Height=1080)
$ErrorActionPreference='Stop'
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
. ./scripts/unity-process.ps1
Assert-LatticeUnlocked (Join-Path $PWD 'Lattice')
if(Get-Process Coronach -ErrorAction SilentlyContinue){throw 'Wait for the existing player'}
if($Run -notmatch '^[a-z0-9-]+$'){throw 'Use one new run folder name'}
$folder=Join-Path $PWD ('Builds/quality/C5/'+$Run)
New-Item -ItemType Directory -Path $folder -ErrorAction Stop | Out-Null
$width=if($Height -eq 720){1280}else{1920}
$exe=Join-Path $PWD 'Builds/WindowsDev/Coronach.exe'
$launch=@('-screen-fullscreen','0','-screen-width',$width,'-screen-height',$Height,'-review',$folder,'-savepath',(Join-Path $folder 'saves'),'-logFile',(Join-Path $folder 'player.log'))
@{input='ordinary virtual gamepad through ReviewInput, decision-paused; not continuous motion or physical feel';width=$width;height=$Height;parent=(& git rev-parse HEAD);assemblyHash=(Get-FileHash Builds/WindowsDev/Coronach_Data/Managed/Lattice.UI.dll).Hash} | ConvertTo-Json | Set-Content ($folder+'/build.json')
# Visible game: these screenshots inspect the actual UI at the selected resolution.
$player=Start-Process -FilePath $exe -ArgumentList ($launch | ForEach-Object {'"'+$_+'"'}) -PassThru
$script:stepId=0
function Step([string]$Button,[float]$Seconds=.12) {
    $script:stepId++
    if($Button){& ./scripts/review-step.ps1 -Folder $folder -Id $script:stepId -Buttons $Button -Seconds $Seconds | Out-File ($folder+'/steps.log') -Append}
    else{& ./scripts/review-step.ps1 -Folder $folder -Id $script:stepId -Seconds $Seconds | Out-File ($folder+'/steps.log') -Append}
}
function CheckSettings([float]$Expected) {
    $saved=Get-Content -LiteralPath ($folder+'/saves/audio-settings.json') -Raw | ConvertFrom-Json
    foreach($key in @('master','music','sfx','ambience','ui')){if([Math]::Abs($saved.$key-$Expected) -gt .001){throw ('Ordinary UI gain rejected: '+$key+'='+$saved.$key+' expected '+$Expected)}}
}
try {
    $deadline=[DateTime]::UtcNow.AddSeconds(30)
    while(-not(Test-Path -LiteralPath ($folder+'/000.json'))){if($player.HasExited -or [DateTime]::UtcNow -gt $deadline){throw 'Review player did not become ready'};Start-Sleep -Milliseconds 100}
    Step DpadDown
    Step South
    Step DpadLeft
    for($i=0;$i -lt 4;$i++){Step DpadDown;Step DpadLeft}
    Step East
    CheckSettings .9
    Step DpadUp
    Step South 2
    Step Start
    for($i=0;$i -lt 5;$i++){Step RightShoulder}
    Step DpadLeft
    for($i=0;$i -lt 4;$i++){Step DpadDown;Step DpadLeft}
    Step DpadDown
    Step South
    Step East
    CheckSettings .8
    @{status='ordinary gamepad title/pause gains persisted at 90 then 80 percent; screenshots require separate opening';steps=$script:stepId;player=$player.Id;settingsHash=(Get-FileHash ($folder+'/saves/audio-settings.json')).Hash} | ConvertTo-Json | Set-Content ($folder+'/result.json')
    'SOUND_UI_INPUT_CHECK_OK '+$folder
} finally {if(-not $player.HasExited){Stop-LatticeProcessTree $player.Id}}
