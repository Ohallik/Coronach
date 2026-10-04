# Port of Frostbound p64-exec: marker ownership, bounded wait, exact process tree.
param(
    [Parameter(Mandatory=$true)][string]$Method,
    [Parameter(Mandatory=$true)][string]$Marker,
    [string[]]$ExtraArgs=@(), [switch]$Graphics, [switch]$Async, [int]$TimeoutSec=600,
    [switch]$PackageRetry
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
$unityArgs=@('-batchmode','-projectPath',$project,'-executeMethod',$Method,'-logFile',$log)
if(-not $Async){$unityArgs+='-quit'}
if(-not $Graphics){$unityArgs+='-nographics'}
$unityArgs+=$ExtraArgs
$startInfo=New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName=$unity
$startInfo.Arguments=($unityArgs | ForEach-Object { '"'+([string]$_).Replace('"','\"')+'"' }) -join ' '
$startInfo.UseShellExecute=$false
$startInfo.CreateNoWindow=$true
$proc=New-Object System.Diagnostics.Process
$proc.StartInfo=$startInfo
if(-not $proc.Start()){throw 'FAILED: Unity process did not start'}
Write-Host "UNITY_EXEC pid=$($proc.Id) method=$Method"
$deadline=[datetime]::UtcNow.AddSeconds($TimeoutSec)
$green=$false
$retryKnownPackage=$false
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
    if(-not $green){
        if($proc.HasExited){$proc.WaitForExit();throw "FAILED: Unity pid=$($proc.Id) exited with code $($proc.ExitCode) before $Marker; $log"}
        throw "FAILED: missing $Marker within ${TimeoutSec}s; $log"
    }
    if(-not $proc.WaitForExit(5000)){Stop-LatticeProcessTree $proc.Id}
    Write-Host "$Marker log=$log"
} catch {
    # A cold clone resolves PackageCache after the pre-launch compatibility
    # pass. Retry once only for the known Unity 6000.4 Shader Graph GUID error.
    $compileErrors=@([regex]::Matches([string]$content,'(?m)^[^\r\n]*error CS\d+[^\r\n]*'))
    $unexpected=@($compileErrors | Where-Object {$_.Value -notmatch 'PackageCache[\\/]com\.unity\.shadergraph@.+error CS0246:.+[''"]GUID[''"]'})
    if(-not $PackageRetry -and $compileErrors.Count -gt 0 -and $unexpected.Count -eq 0 -and $content -notmatch 'FAILED:|No valid Unity Editor license'){
        $retryKnownPackage=$true
    } else {throw}
} finally {if(-not $proc.HasExited){Stop-LatticeProcessTree $proc.Id}}
if($retryKnownPackage){
    $coldLog=$log+'.cold-import-'+[datetime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'.log'
    Copy-Item -LiteralPath $log -Destination $coldLog
    $patchResult=& python (Join-Path $repo 'tools/patch_unity_packages.py')
    $patchResult | Write-Output
    if($LASTEXITCODE -ne 0 -or $patchResult -notmatch 'modified=[1-9]\d*'){throw "FAILED: known cold-import error was not corrected; $coldLog"}
    Write-Host "UNITY_COLD_IMPORT_RETRY evidence=$coldLog"
    & $PSCommandPath -Method $Method -Marker $Marker -ExtraArgs $ExtraArgs -Graphics:$Graphics -Async:$Async -TimeoutSec $TimeoutSec -PackageRetry
}
