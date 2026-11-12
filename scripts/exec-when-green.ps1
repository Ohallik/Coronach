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
    [int]$Attempts = 110,
    [int]$WaitSec = 50
)
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
. (Join-Path $PSScriptRoot 'exec-log-name.ps1')
$said = $false
# A green check is good for ten minutes: after waiting for Unity the run starts at once instead of checking again,
# because a second nine-second check lost every gap between two suite parts to the suite (eighty minutes, November 11).
# If the tree went red meanwhile, Unity says so and the compile-error branch below waits and checks again.
$greenAt = [datetime]::MinValue
$unityTurn = New-Object System.Threading.Mutex($false, 'Global\CoronachUnityTurn')
for ($i = 1; $i -le $Attempts; $i++) {
    if (((Get-Date) - $greenAt).TotalMinutes -gt 10) {
        $check = python tools/typecheck.py | Select-Object -Last 1
        if ($check -ne 'TYPECHECK_OK') { "EXEC_WAIT $i tree is red"; Start-Sleep -Seconds $WaitSec; continue }
        $greenAt = Get-Date
        # Unity may be busy: wait here, then launch the moment it is free.
        $free = $false
        for ($poll = 0; $poll -lt 290 -and -not $free; $poll++) {
            $free = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains((Join-Path $root 'Lattice')) }).Count -eq 0
            if (-not $free) { if ($poll -eq 0) { "EXEC_WAIT Unity is busy" }; Start-Sleep -Seconds 2 }
        }
        if (-not $free) { $i--; continue }
    }
    $params = @{ Method = $Method; Marker = $Marker; TimeoutSec = $TimeoutSec }
    if ($Graphics) { $params.Graphics = $true }
    if ($ExtraArgs.Count) { $params.ExtraArgs = $ExtraArgs }
    # One launch at a time, machine-wide: two waiters that both saw Unity free started it in the same second, and the
    # loser died resolving packages (November 11). The mutex is held for the whole run, so it is also the queue.
    $text = ''
    $held = $false
    try { $held = $unityTurn.WaitOne([TimeSpan]::FromMinutes(45)) } catch [System.Threading.AbandonedMutexException] { $held = $true }
    if (-not $held) { "EXEC_WAIT still queued for Unity"; $i--; continue }
    try {
        try { $text = & (Join-Path $PSScriptRoot 'exec.ps1') @params 2>&1 | Out-String } catch { $text = "$text`n$($_.Exception.Message)" }
        $code = $LASTEXITCODE
        # Keep this run's own copy of the log before giving up the turn: exec.ps1 names its log by the method alone, so
        # the next engineer's run of the same method deletes it (a failed build's log was lost this way, November 12).
        $kept = ''
        $shared = Join-Path $root ('Builds\logs\' + (Get-HeadlessExecLogName $Method))
        if (Test-Path -LiteralPath $shared) {
            $runs = Join-Path $root 'Builds\logs\runs'
            New-Item -ItemType Directory -Force $runs | Out-Null
            $kept = Join-Path $runs (([IO.Path]::GetFileNameWithoutExtension($shared)) + '-' + (Get-Date -Format 'MMdd-HHmmss') + "-$PID.log")
            Copy-Item -LiteralPath $shared -Destination $kept -Force -ErrorAction SilentlyContinue
            Get-ChildItem $runs -File | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-2) } | Remove-Item -Force -ErrorAction SilentlyContinue
        }
    } finally { $unityTurn.ReleaseMutex() }
    # Busy is a short wait and doesn't use up an attempt: a long test part can hold Unity for twenty minutes, and the
    # gap between two parts is a few seconds, so look often (a 50 s poll starved for ninety minutes, November 11).
    if ($text -match 'already holds') {
        if (-not $said) { "EXEC_WAIT Unity is busy"; $said = $true }
        $free = $false
        for ($poll = 0; $poll -lt 290 -and -not $free; $poll++) {
            Start-Sleep -Seconds 1
            $free = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains((Join-Path $root 'Lattice')) }).Count -eq 0
        }
        # Free (or ten minutes gone): the green check above decides whether to check again before launching.
        $i--; continue
    }
    $said = $false
    # exec.ps1 prints its marker with Write-Host, which a capture doesn't see: go by the exit code and the run's own log.
    $log = if ($kept -and (Test-Path -LiteralPath $kept)) { $kept } else { [regex]::Match($text, '[A-Za-z]:\\[^\r\n;]*headless-exec-[^\r\n;]*\.log').Value }
    if (-not $log) { $log = (Get-ChildItem (Join-Path $root 'Builds\logs') -Filter 'headless-exec-*.log' | Sort-Object LastWriteTime | Select-Object -Last 1).FullName }
    # When exec.ps1 throws, $LASTEXITCODE stays 0: a failed run is a non-zero code OR "FAILED" in what it printed.
    $bad = ($code -ne 0) -or ($text -match 'FAILED')
    if ($bad -and $log -and (Test-Path $log) -and (Select-String -Path $log -Pattern 'error CS\d+' -Quiet)) {
        "EXEC_WAIT $i Unity found a compile error (someone is mid-edit):"
        Select-String -Path $log -Pattern 'error CS\d+' | Select-Object -First 3 | ForEach-Object { "  " + $_.Line.Substring(0, [Math]::Min(220, $_.Line.Length)) }
        $greenAt = [datetime]::MinValue
        Start-Sleep -Seconds $WaitSec; continue
    }
    if ($bad -and $log -and (Test-Path $log) -and (Select-String -Path $log -Pattern 'Failed to resolve packages: operation cancelled|another Unity instance is running|It looks like another Unity instance|Project folder or disk is read only' -Quiet)) {
        "EXEC_WAIT $i Unity start-up collided with another launch"; Start-Sleep -Seconds 8; continue
    }
    ($text -split "`r?`n" | Where-Object { $_ } | Select-Object -Last 4) -join "`n"
    if (-not $bad) { "EXEC_OK $Method log=$log"; exit 0 }
    "EXEC_FAILED $Method exit=$code log=$log"; exit 1
}
"EXEC_GAVE_UP $Method after $Attempts attempts"; exit 2
