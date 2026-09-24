param([int]$TimeoutSec=100)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $repo 'Builds/WindowsDev/Coronach.exe'
$folder=Join-Path $repo 'Builds/logs/ui'
$log=Join-Path $repo 'Builds/logs/ui-smoke.log'
New-Item -ItemType Directory -Force $folder | Out-Null
$started=[DateTime]::UtcNow
$playerArgs=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-scene','Arena_Ground','-loadout','moon','-uismoke',$folder,'-savepath',(Join-Path $repo 'Builds/ui-smoke-saves'),'-logFile',$log)
$process=Start-Process -FilePath $exe -ArgumentList $playerArgs -PassThru
try{
    if(-not $process.WaitForExit($TimeoutSec*1000)){throw 'FAILED: UI smoke timeout'}
    $text=[IO.File]::ReadAllText($log)
    if($text -match 'FAILED:|Exception:' -or $text -notmatch 'UI_SMOKE_OK'){throw "FAILED: UI smoke; $log"}
    foreach($name in @('dialogue','menu-0','menu-1','menu-2','menu-3','menu-4','menu-5','shop')){
        $path=Join-Path $folder "$name.png"
        if(-not (Test-Path $path)){throw "FAILED: missing $name screenshot"}
        $item=Get-Item $path
        if($item.Length -lt 30000 -or $item.LastWriteTimeUtc -lt $started){throw "FAILED: blank/stale $name screenshot"}
    }
    Write-Host "UI_SMOKE_OK screenshots=$folder"
}finally{if(-not $process.HasExited){Stop-LatticeProcessTree $process.Id}}
