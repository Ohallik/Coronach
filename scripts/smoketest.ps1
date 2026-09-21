param([string]$Scene='Title',[string]$Route,[switch]$Dev,[int]$TimeoutSec=80,[switch]$RequirePad)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$folder=if($Dev){'WindowsDev'}else{'Windows'}
$exe=Join-Path $repo "Builds/$folder/Lattice.exe"
$log=Join-Path $repo "Builds/logs/$Scene-player.log"
$shot=Join-Path $repo "Builds/logs/$Scene-screenshot.png"
$playerLog=Join-Path $env:USERPROFILE 'AppData/LocalLow/Nathan/Lattice/Player.log'
foreach($path in @($log,$shot)){if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}}
$playerArgs=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-smoketest','-scene',$Scene,'-screenshot',$shot,'-savepath',(Join-Path $repo 'Builds/smoke-saves'))
if($Route){$playerArgs+=@('-route',$Route)}
$proc=Start-Process -FilePath $exe -ArgumentList $playerArgs -PassThru
try {
    if(-not $proc.WaitForExit($TimeoutSec*1000)){throw 'FAILED: player timeout'}
    Copy-Item -LiteralPath $playerLog -Destination $log
    $text=[IO.File]::ReadAllText($log)
    if($text -match 'Exception:|FAILED:'){throw "FAILED: player error; $log"}
    $marker=if($Scene -eq 'Title'){'TITLE_SMOKE_OK'}else{'ARENA_SMOKE_OK '+$Scene}
    if(-not $text.Contains($marker)){throw "FAILED: missing $marker"}
    if(-not (Test-Path $shot) -or (Get-Item $shot).Length -lt 30000){throw 'FAILED: absent/blank screenshot'}
    if($RequirePad -and $text -notmatch 'PAD_BRIDGE_ATTACH[^\r\n]*vid=(0x)?046D pid=(0x)?C21A profile=Logitech'){throw 'FAILED: physical Logitech not attached'}
    Write-Host "SMOKE_OK scene=$Scene screenshot=$shot"
    Select-String -LiteralPath $log -Pattern 'PAD_BRIDGE_ATTACH|TITLE_BOOT_OK|SCREENSHOT_OK|SMOKE_OK' | ForEach-Object {$_.Line}
} finally {if(-not $proc.HasExited){Stop-LatticeProcessTree $proc.Id}}
