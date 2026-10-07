param(
    [Parameter(Mandatory=$true)][string]$Route,
    [Parameter(Mandatory=$true)][string]$Output,
    [ValidateSet('Development','Release')][string]$Build='Development',
    [switch]$Capture,
    [switch]$Arrows,
    [switch]$Profile,
    [switch]$Headroom,
    [switch]$Census,
    [switch]$Motion,
    [ValidateSet('Auto','D3D11','D3D12')][string]$GraphicsApi='Auto',
    [ValidateSet(-1,0,1)][int]$DiagnosticVSync=-1,
    [string]$ResumeFrom,
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
# Clean timing must not silently overlap another project's build/render work.
# Inspect and record only; never stop an unrelated process to obtain a pass.
$cleanTiming=-not $Capture -and -not $Profile
$interference=@()
function Get-QualityCompetitors([int]$IgnoreId=0) {
    @(Get-CimInstance Win32_Process | Where-Object {
        if($_.ProcessId -eq $IgnoreId){return $false}
        if($_.Name -match '^UnityCrashHandler(32|64)?\.exe$'){return $false}
        if($_.Name -match '^(Unity|UnityShaderCompiler|bee_backend|ffmpeg|ffmpeg-win-x86_64-v7.1)\.exe$'){return $true}
        # A second Unity player can consume the GPU and steal focus just as an editor can.
        if(Test-UnityPlayerPath $_.ExecutablePath){return $true}
        return $false
    } | Select-Object ProcessId,Name,CommandLine)
}
$initialCompetitors=@(Get-QualityCompetitors)
@{cleanTiming=$cleanTiming;started=[DateTime]::UtcNow.ToString('o');initial=$initialCompetitors} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $folder 'environment.json')
if($cleanTiming -and $initialCompetitors.Count){throw "Clean timing unavailable while editor/build/encoder processes are running; see $folder/environment.json"}
$routePath=[IO.Path]::GetFullPath((Join-Path $repo $Route))
& python (Join-Path $repo 'tools/quality_route.py') $routePath
if($LASTEXITCODE -ne 0){throw 'Route checkpoints were rejected; player was not started'}
if($ResumeFrom){
    $routeData=Get-Content -LiteralPath $routePath -Raw | ConvertFrom-Json
    if($routeData.scene -ne 'Title' -or $routeData.starterParty -ne $false -or $routeData.loadout){throw 'Resume must enter the actual Title Continue menu without a development loadout'}
    & python (Join-Path $repo 'tools/quality_resume.py') (Join-Path $repo $ResumeFrom) $folder
    if($LASTEXITCODE -ne 0){throw 'Prior replay saves were rejected; player was not started'}
}
$exe=Join-Path $repo $(if($Build -eq 'Release'){'Builds/Windows/Coronach.exe'}else{'Builds/WindowsDev/Coronach.exe'})
$log=Join-Path $folder 'player.log'
$launch=@('-screen-fullscreen','0','-screen-width','1920','-screen-height','1080','-quality-route',$routePath,'-quality-output',$folder,'-savepath',(Join-Path $folder 'saves'),'-logFile',$log)
if($GraphicsApi -ne 'Auto'){$launch+=('-force-'+$GraphicsApi.ToLowerInvariant())}
if($DiagnosticVSync -ge 0){$launch+=@('-quality-vsync',[string]$DiagnosticVSync)}
if ($Capture) { $ffmpeg=(& python -c 'import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())').Trim(); $launch+=@('-quality-ffmpeg',$ffmpeg) }
if ($Arrows) { $launch+='-quality-arrows' }
if ($Profile) { $launch+='-quality-profile' }
if ($Headroom) { $launch+='-quality-headroom' }
if ($Census) { $launch+='-quality-census' }
if ($Motion) { $launch+='-quality-motion' }
$manifest=@{source=(& git -C $repo rev-parse HEAD);routeHash=(Get-FileHash -LiteralPath $routePath).Hash;exeHash=(Get-FileHash -LiteralPath $exe).Hash;build=$Build;capture=[bool]$Capture;graphicsApi=$GraphicsApi;diagnosticVSync=$DiagnosticVSync;started=[DateTime]::UtcNow.ToString('o')}
$assemblyDir=Join-Path (Split-Path $exe -Parent) 'Coronach_Data/Managed'
$manifest.assemblies=@(Get-ChildItem -LiteralPath $assemblyDir -Filter 'Lattice.*.dll' | ForEach-Object { @{name=$_.Name;hash=(Get-FileHash -LiteralPath $_.FullName).Hash} })
# Include packaged scene/asset content, not just the Unity launcher EXE and code.
$dataDir=Join-Path (Split-Path $exe -Parent) 'Coronach_Data'
$manifest.content=@(Get-ChildItem -LiteralPath $dataDir -File | ForEach-Object { @{name=$_.Name;bytes=$_.Length;hash=(Get-FileHash -LiteralPath $_.FullName).Hash} })
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'build.json')
# This is the visible game being reviewed. Focus is verified per frame; no focus override.
$process=Start-Process -FilePath $exe -ArgumentList ($launch | ForEach-Object { '"'+$_+'"' }) -PassThru
Set-Content -LiteralPath (Join-Path $folder 'process.txt') -Value $process.Id
Write-Output "QUALITY_PLAYER pid=$($process.Id) output=$folder"
try {
    $deadline=[DateTime]::UtcNow.AddSeconds($TimeoutSec)
    $nextEnvironmentCheck=[DateTime]::UtcNow.AddSeconds(5)
    while (-not $process.HasExited) {
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Quality replay timeout' }
        if($cleanTiming -and [DateTime]::UtcNow -ge $nextEnvironmentCheck){
            foreach($competitor in (Get-QualityCompetitors -IgnoreId $process.Id)){$interference+=@{at=[DateTime]::UtcNow.ToString('o');process=$competitor}}
            $nextEnvironmentCheck=[DateTime]::UtcNow.AddSeconds(5)
        }
        Start-Sleep -Milliseconds 500
    }
    $result=Join-Path $folder 'run.json'
    if (-not (Test-Path -LiteralPath $result)) { throw "Quality report missing; inspect $log" }
    Get-Content -LiteralPath $result
    if($interference.Count){
        $interference | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $folder 'interference.json')
        $report=Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
        $report.valid=$false;$report.failures+=@('concurrent editor/build/encoder invalidates clean timing')
        [IO.File]::WriteAllText($result,($report | ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
        throw "Clean timing contaminated; see $folder/interference.json"
    }
    if ($Capture -and (Test-Path -LiteralPath (Join-Path $folder 'mix.wav'))) {
        & $ffmpeg -hide_banner -loglevel error -i (Join-Path $folder 'video.mp4') -i (Join-Path $folder 'mix.wav') -c:v copy -c:a aac -b:a 192k -shortest (Join-Path $folder 'replay.mp4')
        if ($LASTEXITCODE -ne 0) { throw 'Video/audio mux failed; raw artifacts preserved' }
    }
    if ($process.ExitCode -ne 0 -or (Get-Content -LiteralPath $log -Raw) -match 'Exception:|QUALITY_REPLAY_REJECTED') { throw "Quality replay rejected; inspect $folder" }
} finally { if (-not $process.HasExited) { Stop-LatticeProcessTree $process.Id } }
