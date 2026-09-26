param(
    [Parameter(Mandatory=$true)][string]$Route,
    [Parameter(Mandatory=$true)][string]$Output,
    [ValidateSet('Development','Release')][string]$Build='Development',
    [switch]$Capture,
    [switch]$Arrows,
    [int]$TimeoutSec=780
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$folder=[IO.Path]::GetFullPath((Join-Path $repo $Output))
$qualityRoot=[IO.Path]::GetFullPath((Join-Path $repo 'Builds/quality'))+[IO.Path]::DirectorySeparatorChar
if (-not $folder.StartsWith($qualityRoot,[StringComparison]::OrdinalIgnoreCase)) { throw 'Evidence and isolated saves must be inside Builds/quality/' }
if (Test-Path -LiteralPath $folder) { throw 'Choose a new output directory; preserve earlier evidence' }
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$routePath=[IO.Path]::GetFullPath((Join-Path $repo $Route))
$exe=Join-Path $repo $(if($Build -eq 'Release'){'Builds/Windows/Coronach.exe'}else{'Builds/WindowsDev/Coronach.exe'})
$log=Join-Path $folder 'player.log'
$launch=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-quality-route',$routePath,'-quality-output',$folder,'-savepath',(Join-Path $folder 'saves'),'-logFile',$log)
if ($Capture) { $ffmpeg=(& python -c 'import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())').Trim(); $launch+=@('-quality-ffmpeg',$ffmpeg) }
if ($Arrows) { $launch+='-quality-arrows' }
$manifest=@{source=(& git -C $repo rev-parse HEAD);routeHash=(Get-FileHash -LiteralPath $routePath).Hash;exeHash=(Get-FileHash -LiteralPath $exe).Hash;build=$Build;capture=[bool]$Capture;started=[DateTime]::UtcNow.ToString('o')}
$assemblyDir=Join-Path (Split-Path $exe -Parent) 'Coronach_Data/Managed'
$manifest.assemblies=@(Get-ChildItem -LiteralPath $assemblyDir -Filter 'Lattice.*.dll' | ForEach-Object { @{name=$_.Name;hash=(Get-FileHash -LiteralPath $_.FullName).Hash} })
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'build.json')
# This is the visible game being reviewed. Focus is verified per frame; no focus override.
$process=Start-Process -FilePath $exe -ArgumentList ($launch | ForEach-Object { '"'+$_+'"' }) -PassThru
Set-Content -LiteralPath (Join-Path $folder 'process.txt') -Value $process.Id
Write-Output "QUALITY_PLAYER pid=$($process.Id) output=$folder"
try {
    $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSec)
    while (-not $process.HasExited) {
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Quality replay timeout' }
        Start-Sleep -Milliseconds 500
    }
    $result=Join-Path $folder 'run.json'
    if (-not (Test-Path -LiteralPath $result)) { throw "Quality report missing; inspect $log" }
    Get-Content -LiteralPath $result
    if ($Capture -and (Test-Path -LiteralPath (Join-Path $folder 'mix.wav'))) {
        & $ffmpeg -hide_banner -loglevel error -i (Join-Path $folder 'video.mp4') -i (Join-Path $folder 'mix.wav') -c:v copy -c:a aac -b:a 192k -shortest (Join-Path $folder 'replay.mp4')
        if ($LASTEXITCODE -ne 0) { throw 'Video/audio mux failed; raw artifacts preserved' }
    }
    if ($process.ExitCode -ne 0 -or (Get-Content -LiteralPath $log -Raw) -match 'Exception:|QUALITY_REPLAY_REJECTED') { throw "Quality replay rejected; inspect $folder" }
} finally { if (-not $process.HasExited) { Stop-LatticeProcessTree $process.Id } }
