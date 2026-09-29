param(
    [Parameter(Mandatory=$true)][string]$Folder,
    [Parameter(Mandatory=$true)][int]$Id,
    [float]$X=0, [float]$Y=0, [float]$Seconds=.15, [float]$LeftTrigger=0, [float]$RightTrigger=0,
    [string[]]$Buttons=@()
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($Folder)
if (-not (Test-Path -LiteralPath $root -PathType Container)) { throw 'Start the opt-in review player first' }
$result=Join-Path $root ('{0:d3}.json' -f $Id)
if (Test-Path -LiteralPath $result) { throw 'Review step ID already exists' }
$Buttons=@($Buttons | ForEach-Object { $_ -split ',' })
$command=@{id=$Id;x=$X;y=$Y;leftTrigger=$LeftTrigger;rightTrigger=$RightTrigger;seconds=$Seconds;buttons=$Buttons} | ConvertTo-Json -Compress
$temp=Join-Path $root 'command.tmp'
[IO.File]::WriteAllText($temp,$command)
$destination=Join-Path $root 'command.json'
# Publish whole commands atomically. The player briefly opens the previous
# command for reading on Windows; do not expose a partially rewritten file.
$publishDeadline=[DateTime]::UtcNow.AddSeconds(2)
while ($true) {
    try {
        if ([IO.File]::Exists($destination)) {
            $previous=Get-Content -LiteralPath $destination -Raw | ConvertFrom-Json
            if ($previous.id -ge $Id -or -not (Test-Path -LiteralPath (Join-Path $root ('{0:d3}.json' -f $previous.id)))) {
                throw 'Previous review command is not complete; preserve it'
            }
            [IO.File]::Replace($temp,$destination,(Join-Path $root 'command.previous'))
        } else { [IO.File]::Move($temp,$destination) }
        break
    } catch [IO.IOException] {
        if ([DateTime]::UtcNow -gt $publishDeadline) { throw }
        Start-Sleep -Milliseconds 25
    }
}
$deadline=[DateTime]::UtcNow.AddSeconds(15)
while (-not (Test-Path -LiteralPath $result)) {
    if ([DateTime]::UtcNow -gt $deadline) { throw "Review step $Id timed out" }
    Start-Sleep -Milliseconds 100
}
Get-Content -LiteralPath $result
Write-Output (Join-Path $root ('{0:d3}.png' -f $Id))
