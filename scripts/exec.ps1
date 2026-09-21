# Port of Frostbound p64-exec: marker ownership, bounded wait, exact process tree.
param(
    [Parameter(Mandatory=$true)][string]$Method,
    [Parameter(Mandatory=$true)][string]$Marker,
    [string[]]$ExtraArgs=@(), [switch]$Graphics, [int]$TimeoutSec=600
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
. (Join-Path $PSScriptRoot 'exec-log-name.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$project=Join-Path $repo 'Lattice'
Assert-LatticeUnlocked $project
& python (Join-Path $repo 'tools/patch_unity_packages.py')
if($LASTEXITCODE -ne 0){throw 'FAILED: package compatibility patch'}
$log=Join-Path $repo ('Builds/logs/'+(Get-HeadlessExecLogName $Method))
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
if(Test-Path -LiteralPath $log){Remove-Item -LiteralPath $log}
$unity='C:/Program Files/Unity/Hub/Editor/6000.4.7f1/Editor/Unity.exe'
$unityArgs=@('-batchmode','-projectPath',$project,'-executeMethod',$Method,'-logFile',$log,'-quit')
if(-not $Graphics){$unityArgs+='-nographics'}
$unityArgs+=$ExtraArgs
$proc=Start-Process -FilePath $unity -ArgumentList $unityArgs -WindowStyle Hidden -PassThru
$deadline=[datetime]::UtcNow.AddSeconds($TimeoutSec)
$green=$false
try {
    while([datetime]::UtcNow -lt $deadline){
        if(Test-Path $log){
            $content=Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue
            if($content -match 'error CS\d|FAILED:|No valid Unity Editor license'){throw "FAILED: Unity error; $log"}
            if($content -match [regex]::Escape($Marker)){$green=$true;break}
        }
        if($proc.HasExited){break}
        Start-Sleep -Milliseconds 750
    }
    if(-not $green){throw "FAILED: missing $Marker within ${TimeoutSec}s; $log"}
    if(-not $proc.WaitForExit(5000)){Stop-LatticeProcessTree $proc.Id}
    Write-Host "$Marker log=$log"
} finally {if(-not $proc.HasExited){Stop-LatticeProcessTree $proc.Id}}
