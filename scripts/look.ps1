param([int]$TimeoutSec=120,[string]$Folder='Builds/logs/look/generated')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$log=Join-Path $repo 'Builds/logs/look-player.log'
$shots=Join-Path $repo $Folder
New-Item -ItemType Directory -Path $shots -Force | Out-Null
$started=[DateTime]::UtcNow
$arguments=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-scene','Hub_CinderHalo','-loadout','starter','-look',$shots,'-savepath',(Join-Path $repo 'Builds/look-saves'),'-logFile',$log)
$process=Start-Process -FilePath (Join-Path $repo 'Builds/WindowsDev/Lattice.exe') -ArgumentList $arguments -PassThru
try{
 if(-not $process.WaitForExit($TimeoutSec*1000)){throw 'FAILED: look timeout'}
 $text=[IO.File]::ReadAllText($log)
 if($text -match 'FAILED:|Exception:' -or $text -notmatch 'ZONE_LOOK_OK count=18'){throw "FAILED: look; $log"}
 foreach($zone in @('Hub_CinderHalo','Hub_Decks','Sorrel_Ridges','Gullet_Tunnel','TallowApproach','TallowDrift')){foreach($i in 0..2){$file=Get-Item -LiteralPath (Join-Path $shots "$zone-$i.png");if($file.Length -lt 30000 -or $file.LastWriteTimeUtc -lt $started){throw "FAILED: missing/blank/stale look evidence $zone-$i"}}}
 Write-Host "ZONE_LOOK_OK count=18 folder=$shots"
}finally{if(-not $process.HasExited){Stop-LatticeProcessTree $process.Id}}
