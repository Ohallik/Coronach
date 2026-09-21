# Lattice reproducible Blender runner. Blender MCP is optional; checked-in
# scripts invoked here are the shipping source of truth.
param(
    [Parameter(Mandatory = $true)][string]$Script,
    [int]$TimeoutSec = 600,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$ScriptArgs
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$ToolsRoot = Join-Path $RepoRoot "tools\blender"
$LogDir = Join-Path $RepoRoot "Builds\logs"
New-Item -ItemType Directory -Force $LogDir | Out-Null

$candidates = @()
if ($env:BLENDER_EXE) { $candidates += $env:BLENDER_EXE }
$command = Get-Command blender.exe -ErrorAction SilentlyContinue
if ($command) { $candidates += $command.Source }
$candidates += @(
    "C:\Program Files\Blender Foundation\Blender 5.1\blender.exe",
    "$env:LOCALAPPDATA\Programs\Blender Foundation\Blender 4.5 LTS\blender.exe",
    "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe",
    "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe"
)
$appx = Get-AppxPackage BlenderFoundation.Blender4.5LTS -ErrorAction SilentlyContinue
if ($appx) { $candidates += (Join-Path $appx.InstallLocation "Blender\blender.exe") }
$Blender = $candidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
if (-not $Blender) {
    throw "Blender LTS not found. Set BLENDER_EXE or install BlenderFoundation.Blender.LTS.4.5."
}

$requested = if ([System.IO.Path]::IsPathRooted($Script)) { $Script } else { Join-Path $RepoRoot $Script }
if (-not (Test-Path -LiteralPath $requested)) {
    $requested = Join-Path $ToolsRoot $Script
}
$scriptPath = (Resolve-Path -LiteralPath $requested).Path
$toolsPath = (Resolve-Path -LiteralPath $ToolsRoot).Path
if (-not $scriptPath.StartsWith($toolsPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Blender scripts must live under $toolsPath"
}

$stamp = (Get-Date -Format "yyyyMMdd-HHmmss") + '-' + [guid]::NewGuid().ToString('N').Substring(0,6)
$stem = [System.IO.Path]::GetFileNameWithoutExtension($scriptPath)
$log = Join-Path $LogDir "blender-$stem-$stamp.log"
$cleanArgs = @($ScriptArgs | Where-Object { $_ -ne "--" })

Write-Host "Blender: $Blender"
Write-Host "Script:  $scriptPath"
Write-Host "Log:     $log"
$argsList = @('--background','--factory-startup','--python-exit-code','1','--python',('"'+$scriptPath+'"'),'--') + @($cleanArgs | ForEach-Object {'"'+$_+'"'})
$info = New-Object System.Diagnostics.ProcessStartInfo
$info.FileName = $Blender
$info.Arguments = $argsList -join ' '
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.RedirectStandardOutput = $true
$info.RedirectStandardError = $true
$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $info
if (-not $proc.Start()) { throw 'FAILED: Blender failed to start' }
$stdout = $proc.StandardOutput.ReadToEndAsync()
$stderr = $proc.StandardError.ReadToEndAsync()
try {
    if (-not $proc.WaitForExit($TimeoutSec * 1000)) { throw "FAILED: Blender timeout; $log" }
    [IO.File]::WriteAllText($log, $stdout.Result + $stderr.Result)
    if ($proc.ExitCode -ne 0) { throw "FAILED: Blender exited $($proc.ExitCode); $log" }
} finally { if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force } }
Write-Host "BLENDER_SCRIPT_OK script=$stem log=$log"
