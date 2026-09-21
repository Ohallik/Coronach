param([int]$TimeoutSec=90)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$folder=Join-Path $repo 'Builds/logs/portraits'
$log=Join-Path $repo 'Builds/logs/portrait-smoke.log'
New-Item -ItemType Directory -Force $folder | Out-Null
$started=[DateTime]::UtcNow
$playerArgs=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-scene','Arena_Ground','-loadout','moon','-portraitsmoke',$folder,'-savepath',(Join-Path $repo 'Builds/portrait-smoke-saves'),'-logFile',$log)
# Visible graphics player: hidden-window captures on this machine are black.
$process=Start-Process -FilePath (Join-Path $repo 'Builds/WindowsDev/Lattice.exe') -ArgumentList $playerArgs -PassThru
try{
    if(-not $process.WaitForExit($TimeoutSec*1000)){throw 'FAILED: portrait smoke timeout'}
    $content=[IO.File]::ReadAllText($log)
    if($content -match 'FAILED:|Exception:' -or $content -notmatch 'PORTRAIT_SMOKE_OK count=16'){throw "FAILED: portrait smoke; $log"}
    foreach($character in @('Taren','Sela','Orrin','Mira','Hal','Neve')){
        $forms=if($character -in @('Taren','Sela')){@('Natural','Shaped')}else{@('Natural')}
        foreach($form in $forms){foreach($emotion in @('Neutral','Shocked')){
            $path=Join-Path $folder ($character+'_'+$form+'_'+$emotion+'.png')
            if(-not (Test-Path -LiteralPath $path)){throw "FAILED: missing $path"}
            $item=Get-Item -LiteralPath $path
            if($item.Length -lt 30000 -or $item.LastWriteTimeUtc -lt $started){throw "FAILED: blank/stale $path"}
        }}
    }
    Write-Host "PORTRAIT_SMOKE_OK screenshots=$folder"
}finally{if(-not $process.HasExited){Stop-LatticeProcessTree $process.Id}}
