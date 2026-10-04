param([switch]$ShowCommand)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $repo 'Builds/Workshop/Player/Coronach.exe'
$profile=Join-Path $repo 'Builds/Workshop/Profile'
$logs=Join-Path $repo 'Builds/Workshop/Logs'
if(-not (Test-Path -LiteralPath $exe -PathType Leaf)){throw 'The workshop package is missing. See docs/WORKSHOP.md for the local package and build instructions.'}
$log=Join-Path $logs ('play-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss')+'.log')
$launch=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-savepath',$profile,'-audio-settings-path',$profile,'-logFile',$log)
$quoted=($launch | ForEach-Object {'"'+$_+'"'}) -join ' '
if($ShowCommand){Write-Output ('"'+$exe+'" '+$quoted);return}
$running=@(Get-CimInstance Win32_Process | Where-Object {$_.ExecutablePath -eq $exe})
if($running.Count){throw 'This Coronach player is already running. Close it before opening another workshop session.'}
New-Item -ItemType Directory -Path $profile,$logs -Force | Out-Null
Write-Output "Workshop saves and sound preferences: $profile"
Start-Process -FilePath $exe -ArgumentList $quoted -WorkingDirectory (Split-Path $exe -Parent)
