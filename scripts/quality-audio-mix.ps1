param([Parameter(Mandatory=$true)][string]$Run,[ValidateSet('','bypass-sfx')][string]$Fault='')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
. ./scripts/unity-process.ps1
Assert-LatticeUnlocked (Join-Path $PWD 'Lattice')
if(Get-Process Coronach -ErrorAction SilentlyContinue){throw 'Another review player is running'}
if($Run -notmatch '^[a-z0-9-]+$'){throw 'Use one new evidence folder name'}
$folder=Join-Path $PWD ('Builds/quality/C5/'+$Run)
New-Item -ItemType Directory -Path $folder -ErrorAction Stop | Out-Null
$exe=Join-Path $PWD 'Builds/WindowsDev/Coronach.exe'
$launch=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-audio-mix-probe',$folder,'-savepath',(Join-Path $folder 'saves'),'-logFile',(Join-Path $folder 'player.log'))
if($Fault){$launch+=@('-audio-mix-fault',$Fault)}
$manifest=@{sourceParent=(& git rev-parse HEAD);input='diagnostic generated tone, AudioListener PCM measurement; no audition';fault=$Fault;started=[DateTime]::UtcNow.ToString('o');exeHash=(Get-FileHash -LiteralPath $exe).Hash}
$manifest.assemblies=@(Get-ChildItem Builds/WindowsDev/Coronach_Data/Managed -Filter 'Lattice.*.dll' | ForEach-Object {@{name=$_.Name;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'build.json')
$process=Start-Process -FilePath $exe -ArgumentList ($launch | ForEach-Object {'"'+$_+'"'}) -WindowStyle Hidden -PassThru
try {
    if(-not $process.WaitForExit(60000)){throw 'PCM routing probe timed out'}
    $reportPath=Join-Path $folder 'audio-mix.json'
    if(-not(Test-Path -LiteralPath $reportPath)){throw 'PCM routing report missing'}
    $report=Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if(@($report.samples).Count -ne 24){throw 'PCM routing matrix incomplete'}
    if($Fault) {
        $bad=@($report.samples | Where-Object {-not $_.accepted})
        if($report.valid -or $bad.Count -ne 4 -or @($bad | Where-Object {$_.channel -ne 'SFX'}).Count){throw 'Fault control did not isolate the expected four SFX gain failures'}
        'PCM_GAIN_FAULT_REJECTED'
    } else {
        if(-not $report.valid){throw ('PCM routing rejected: '+($report.failures -join '; '))}
        'PCM_GAIN_MATRIX_PASSED'
    }
    $report | ConvertTo-Json -Depth 5
} finally {if(-not $process.HasExited){Stop-LatticeProcessTree $process.Id}}
