param([int]$TimeoutSec = 360)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repo 'Lattice'
$log = Join-Path $repo 'Builds/logs/create.log'
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
if (Test-Path (Join-Path $project 'ProjectSettings/ProjectVersion.txt')) { throw 'Project already exists; refusing recreation.' }
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.4.7f1/Editor/Unity.exe'
$proc = Start-Process -FilePath $unity -ArgumentList @('-batchmode','-nographics','-quit','-createProject', $project, '-logFile', $log) -WindowStyle Hidden -PassThru
if (-not $proc.WaitForExit($TimeoutSec * 1000)) {
    Stop-Process -Id $proc.Id -Force
    throw "FAILED: create project timeout; see $log"
}
if (-not (Test-Path (Join-Path $project 'ProjectSettings/ProjectVersion.txt'))) { throw "FAILED: project creation; see $log" }
if (Select-String -LiteralPath $log -Pattern 'error CS\d|FAILED:|No valid Unity Editor license') { throw "FAILED: project creation; see $log" }
Write-Host 'PROJECT_CREATE_OK'
