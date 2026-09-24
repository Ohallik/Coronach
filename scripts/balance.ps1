param([int]$TimeoutSec=340)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$log=Join-Path $repo 'Builds/logs/balance.log'
$report=Join-Path $repo 'Builds/logs/balance.json'
$shot=Join-Path $repo 'Builds/logs/balance.png'
foreach($path in @($log,$report,$shot)){if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}}
$arguments=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-scene','Arena_Ground','-balance',$report,'-screenshot',$shot,'-savepath',(Join-Path $repo 'Builds/balance-saves'),'-logFile',$log)
$process=Start-Process -FilePath (Join-Path $repo 'Builds/WindowsDev/Coronach.exe') -ArgumentList $arguments -PassThru
try{
 if(-not $process.WaitForExit($TimeoutSec*1000)){throw 'FAILED: balance timeout'}
 $text=[IO.File]::ReadAllText($log)
 if($text -match 'FAILED:|Exception:' -or $text -notmatch 'BALANCE_OK'){throw "FAILED: balance; $log"}
 if(-not (Test-Path -LiteralPath $report) -or -not (Test-Path -LiteralPath $shot) -or (Get-Item -LiteralPath $shot).Length -lt 30000){throw 'FAILED: balance evidence absent'}
 Get-Content -LiteralPath $report
}finally{if(-not $process.HasExited){Stop-LatticeProcessTree $process.Id}}
