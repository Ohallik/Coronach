<#
.SYNOPSIS
  Runs one editor method through exec.ps1, waiting for the tree to compile and for Unity to be free.

  With several engineers editing one working tree, a run can find the tree red for a minute (somebody is mid-edit) or
  Unity held by another run. This waits and retries instead of failing: it type-checks outside Unity first, starts
  Unity only on a green tree, and tries again if Unity is busy or the compile fails after all.

.EXAMPLE
  & .\scripts\exec-when-green.ps1 -Method Lattice.EditorTools.ProgressionBuilder.Build -Marker PROGRESSION_OK
  & .\scripts\exec-when-green.ps1 -Method Lattice.EditorTools.LookDev.Render -Marker LOOKDEV_OK -Graphics -ExtraArgs @('-look-run','x','-look-zones','Rootmarket')
#>
param(
    [Parameter(Mandatory)][string]$Method,
    [Parameter(Mandatory)][string]$Marker,
    [switch]$Graphics,
    [int]$TimeoutSec = 1500,
    [string[]]$ExtraArgs = @(),
    [int]$Attempts = 30,
    [int]$WaitSec = 50
)
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
for ($i = 1; $i -le $Attempts; $i++) {
    $check = python tools/typecheck.py | Select-Object -Last 1
    if ($check -ne 'TYPECHECK_OK') { "EXEC_WAIT $i tree is red"; Start-Sleep -Seconds $WaitSec; continue }
    $params = @{ Method = $Method; Marker = $Marker; TimeoutSec = $TimeoutSec }
    if ($Graphics) { $params.Graphics = $true }
    if ($ExtraArgs.Count) { $params.ExtraArgs = $ExtraArgs }
    $text = ''
    try { $text = & (Join-Path $PSScriptRoot 'exec.ps1') @params 2>&1 | Out-String } catch { $text = "$text`n$($_.Exception.Message)" }
    $code = $LASTEXITCODE
    if ($text -match 'already holds') { "EXEC_WAIT $i Unity is busy"; Start-Sleep -Seconds $WaitSec; continue }
    # exec.ps1 prints its marker with Write-Host, which a capture doesn't see: go by the exit code and the run's own log.
    $log = [regex]::Match($text, '[A-Za-z]:\\[^\r\n;]*headless-exec-[^\r\n;]*\.log').Value
    if (-not $log) { $log = (Get-ChildItem (Join-Path $root 'Builds\logs') -Filter 'headless-exec-*.log' | Sort-Object LastWriteTime | Select-Object -Last 1).FullName }
    if ($code -ne 0 -and $log -and (Test-Path $log) -and (Select-String -Path $log -Pattern 'error CS\d+' -Quiet)) {
        "EXEC_WAIT $i Unity found a compile error (someone is mid-edit):"
        Select-String -Path $log -Pattern 'error CS\d+' | Select-Object -First 3 | ForEach-Object { "  " + $_.Line.Substring(0, [Math]::Min(220, $_.Line.Length)) }
        Start-Sleep -Seconds $WaitSec; continue
    }
    ($text -split "`r?`n" | Where-Object { $_ } | Select-Object -Last 4) -join "`n"
    if ($code -eq 0 -and $text -notmatch 'FAILED') { "EXEC_OK $Method"; exit 0 }
    "EXEC_FAILED $Method exit=$code log=$log"; exit 1
}
"EXEC_GAVE_UP $Method after $Attempts attempts"; exit 2
