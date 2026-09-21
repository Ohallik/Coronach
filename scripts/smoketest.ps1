param([string]$Scene='Title',[string]$Route,[switch]$Dev,[int]$TimeoutSec=80,[switch]$RequirePad)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$folder=if($Dev){'WindowsDev'}else{'Windows'}
$exe=Join-Path $repo "Builds/$folder/Lattice.exe"
$runName=if($Route){"route-$Route"}else{$Scene}
if($Route -and -not $PSBoundParameters.ContainsKey('TimeoutSec')){$TimeoutSec=900}
$log=Join-Path $repo "Builds/logs/$runName-player.log"
$shot=Join-Path $repo "Builds/logs/$runName-screenshot.png"
foreach($path in @($log,$shot)){if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}}
$playerArgs=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-smoketest','-scene',$Scene,'-screenshot',$shot,'-savepath',(Join-Path $repo "Builds/smoke-saves/$runName"),'-logFile',$log)
if($Route){$playerArgs+=@('-route',$Route)}
$startedUtc=[DateTime]::UtcNow
$proc=Start-Process -FilePath $exe -ArgumentList $playerArgs -PassThru
try {
    if(-not $proc.WaitForExit($TimeoutSec*1000)){throw 'FAILED: player timeout'}
    if(-not (Test-Path -LiteralPath $log) -or (Get-Item -LiteralPath $log).LastWriteTimeUtc -lt $startedUtc){throw 'FAILED: missing/stale smoke log'}
    $text=[IO.File]::ReadAllText($log)
    if($text -match 'Exception:|FAILED:'){throw "FAILED: player error; $log"}
    $marker=if($Route){'SLICE_SMOKE_OK'}elseif($Scene -eq 'Title'){'TITLE_SMOKE_OK'}else{'ARENA_SMOKE_OK '+$Scene}
    if(-not $text.Contains($marker)){throw "FAILED: missing $marker"}
    if(-not (Test-Path $shot) -or (Get-Item $shot).Length -lt 30000){throw 'FAILED: absent/blank screenshot'}
    if($RequirePad -and $text -notmatch 'PAD_BRIDGE_ATTACH[^\r\n]*vid=(0x)?046D pid=(0x)?C21A profile=Logitech'){throw 'FAILED: physical Logitech not attached'}
    if($Route){
        $position=0
        $markers=@('TITLE_BOOT_OK','NEW_GAME_OK','ZONE_ENTER Hub_CinderHalo','DOCK_OK','DIALOGUE_OK Orrin','PARTY_JOIN Sela','ZONE_ENTER Sorrel_Ridges','FORM Shaped','COMBAT_KILL Ridgehound','SWAP_OK','FLASH_MOVE_OK','BOSS_DOWN Burrower','QUEST_STEP warpkey','ZONE_ENTER Gullet_Tunnel','FORM Flight','LUNGE_KILL','BOSS_DOWN Cantor','ZONE_ENTER TallowApproach','FORM CivilFlight','DOCK_OK','ZONE_ENTER TallowDrift','FORM Natural','SAVE_OK','SLICE_COMPLETE')
        foreach($required in $markers){$found=$text.IndexOf($required,$position,[StringComparison]::Ordinal);if($found -lt 0){throw "FAILED: absent/out-of-order route marker $required"};$position=$found+$required.Length}
        Write-Host 'ROUTE_MARKER_CONTRACT_OK'
    }
    Write-Host "SMOKE_OK scene=$Scene screenshot=$shot"
    Select-String -LiteralPath $log -Pattern 'PAD_BRIDGE_ATTACH|TITLE_BOOT_OK|SCREENSHOT_OK|SMOKE_OK' | ForEach-Object {$_.Line}
} finally {if(-not $proc.HasExited){Stop-LatticeProcessTree $proc.Id}}
