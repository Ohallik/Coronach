<#
.SYNOPSIS
  In-player stills of zones from the dev player (Builds/WindowsDev): three viewpoints a zone, <Zone>-0.png to <Zone>-2.png in -Folder.

  With no -Zones it takes every built zone: chapter 1's six first, as always, then each other ZoneDef whose scene exists, by its chapter and then
  its id (the build list's order). The six keep the viewpoints set by hand in ZoneLookProbe; any other zone is taken at its arrival and at the two
  places furthest from it. The dev player must hold the zone: a zone built since the player was fails the run by name, before the first still.
  The time allowed is 20 s a zone (120 s for the six) unless -TimeoutSec says otherwise.

.EXAMPLE
  .\scripts\look.ps1
  .\scripts\look.ps1 -Zones Rootmarket,BraidedRiver -Folder Builds/logs/look/xylos        # only these, in this order ('Rootmarket,BraidedRiver' as one string too)
  .\scripts\look.ps1 -Zones Rootmarket -Loadout chapter3                                  # with the party and the story as they arrive at that chapter
#>
param([int]$TimeoutSec=0,[string]$Folder='Builds/logs/look/generated',[string[]]$Zones=@(),[string]$Loadout='starter')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$repo=Split-Path $PSScriptRoot -Parent
# The six ZoneLookProbe has hand-set viewpoints for, in the order their stills have always been taken. Nothing is added here: a new zone joins by having a ZoneDef and a scene.
$first=@('Hub_CinderHalo','Hub_Decks','Sorrel_Ridges','Gullet_Tunnel','TallowApproach','TallowDrift')
$Zones=@($Zones | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ } | Select-Object -Unique)
if($Zones.Count -eq 0){
 $defs=Join-Path $repo 'Lattice/Assets/_Project/Resources/Definitions/ZoneDef';$scenes=Join-Path $repo 'Lattice/Assets/_Project/Scenes'
 # Chapter then id, ordinal, a zone with no chapter last: BatchTools.SceneNames' order. The key pads the chapter so one ordinal sort does both.
 [string[]]$later=@(Get-ChildItem -LiteralPath $defs -Filter '*.asset' | Where-Object { $first -cnotcontains $_.BaseName -and (Test-Path -LiteralPath (Join-Path $scenes ($_.BaseName+'.unity'))) } | ForEach-Object {
  $chapter=0;$said=[regex]::Match([IO.File]::ReadAllText($_.FullName),'(?m)^\s*chapter:\s*(\d+)');if($said.Success){$chapter=[int]$said.Groups[1].Value}
  if($chapter -le 0){$chapter=[int]::MaxValue}
  '{0:D10}|{1}' -f $chapter,$_.BaseName })
 [Array]::Sort($later,[StringComparer]::Ordinal)
 $Zones=@($first)+@($later | ForEach-Object { ($_ -split '\|',2)[1] })
}
foreach($zone in $Zones){if($zone -notmatch '^[A-Za-z0-9_]+$'){throw "FAILED: look; '$zone' is not a zone id"}}
$count=3*$Zones.Count
if($TimeoutSec -le 0){$TimeoutSec=[Math]::Max(120,20*$Zones.Count)}
$log=Join-Path $repo 'Builds/logs/look-player.log'
$shots=Join-Path $repo $Folder
New-Item -ItemType Directory -Path $shots -Force | Out-Null
$started=[DateTime]::UtcNow
$arguments=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-scene',$Zones[0],'-loadout',$Loadout,'-look',$shots,'-look-zones',($Zones -join ','),'-savepath',(Join-Path $repo 'Builds/look-saves'),'-logFile',$log)
$process=Start-Process -FilePath (Join-Path $repo 'Builds/WindowsDev/Coronach.exe') -ArgumentList $arguments -PassThru
try{
 if(-not $process.WaitForExit($TimeoutSec*1000)){throw 'FAILED: look timeout'}
 $text=[IO.File]::ReadAllText($log)
 # The probe names a zone this player doesn't hold before it takes anything: say which, so nobody has to open the log to learn the dev player is older than the zone.
 $why=[regex]::Match($text,'FAILED: look: [^\r\n]*')
 if($text -match 'FAILED:|Exception:' -or $text -notmatch "ZONE_LOOK_OK count=$count\b"){throw ("FAILED: look; $log"+$(if($why.Success){'; '+$why.Value}))}
 foreach($zone in $Zones){foreach($i in 0..2){$file=Get-Item -LiteralPath (Join-Path $shots "$zone-$i.png");if($file.Length -lt 30000 -or $file.LastWriteTimeUtc -lt $started){throw "FAILED: missing/blank/stale look evidence $zone-$i"}}}
 Write-Host "ZONE_LOOK_OK count=$count folder=$shots"
}finally{if(-not $process.HasExited){Stop-LatticeProcessTree $process.Id}}
