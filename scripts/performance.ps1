param([int]$TimeoutSec=90)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $repo 'Builds/WindowsDev/Lattice.exe'
$report=Join-Path $repo 'Builds/logs/perf-gullet.json'
$shot=Join-Path $repo 'Builds/logs/perf-gullet.png'
$log=Join-Path $repo 'Builds/logs/perf-gullet.log'
foreach($path in @($report,$shot,$log)){if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}}
$playerArgs=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-scene','Gullet_Tunnel','-spawn','Performance','-perf',$report,'-screenshot',$shot,'-savepath',(Join-Path $repo 'Builds/perf-saves'),'-logFile',$log)
$process=Start-Process -FilePath $exe -ArgumentList $playerArgs -PassThru
try{
    if(-not $process.WaitForExit($TimeoutSec*1000)){throw 'FAILED: performance timeout'}
    $text=[IO.File]::ReadAllText($log)
    if($text -match 'FAILED:|Exception:' -or $text -notmatch 'PERF_OK'){throw "FAILED: performance run; $log"}
    if(-not (Test-Path $report) -or -not (Test-Path $shot) -or (Get-Item $shot).Length -lt 30000){throw 'FAILED: performance artifacts missing'}
    Get-Content -LiteralPath $report
}finally{if(-not $process.HasExited){Stop-LatticeProcessTree $process.Id}}
