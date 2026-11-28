# Starts the shipping player at one of the story's checkpoints, with saves of its own.
#
#   .\scripts\playtest.ps1 -List         # the checkpoints: a number, the stretch of story that starts there, the party's Sync
#   .\scripts\playtest.ps1 -At 65        # play from where stretch 65 begins (then: Continue, Autosave)
#   .\scripts\playtest.ps1 -At 65 -Resume   # carry on from where the last run at 65 left off
#   .\scripts\playtest.ps1 -At 1         # a new game, from the title
#
# A checkpoint is the autosave the replay driver left at the end of the stretch before, from the last whole capture
# (Builds\quality\nov25-whole\sNN\saves). The player is pointed at a copy of it under Builds\playtest with -savepath, so the
# real saves in AppData\LocalLow are never read or written, and neither is the capture. Sound and key settings are kept in
# that folder too, so a checkpoint starts with every slider at its default.
param(
    [int]$At = 0,
    [switch]$List,
    [switch]$Resume,
    [switch]$DryRun,
    [string]$From = 'Builds\quality\nov25-whole',
    [string]$Player = 'Builds\Windows\Coronach.exe'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$capture = Join-Path $root $From
$exe = Join-Path $root $Player
if (-not (Test-Path $capture)) { throw "No whole capture at $capture" }
$segments = @(Get-ChildItem $capture -Directory | Where-Object Name -match '^s\d+$' | Sort-Object { [int]$_.Name.Substring(1) })
$last = $segments.Count
function Read-Stretch([int]$n) {
    $run = Get-Content (Join-Path $capture ('s{0:d2}\run.json' -f $n)) -Raw | ConvertFrom-Json
    ($run.route -replace '^chapter-\d+-', '') -replace '-', ' '
}
function Read-Sync([int]$n) {
    if ($n -le 1) { return 1 }
    $save = Get-Content (Join-Path $capture ('s{0:d2}\saves\autosave.json' -f ($n - 1))) -Raw | ConvertFrom-Json
    $save.party[0].level
}
if ($List -or $At -eq 0) {
    for ($n = 1; $n -le $last; $n++) { '{0,3}  {1,-34} Sync {2}' -f $n, (Read-Stretch $n), (Read-Sync $n) }
    ''
    'Start at one with:  .\scripts\playtest.ps1 -At <number>'
    return
}
if ($At -lt 1 -or $At -gt $last) { throw "-At takes 1 to $last (see -List)" }
if (-not (Test-Path $exe)) { throw "No player at $exe" }
$folder = Join-Path $root ('Builds\playtest\at-{0:d2}' -f $At)
$have = Test-Path (Join-Path $folder 'autosave.json')
if ($Resume -and -not $have -and $At -gt 1) { throw "Nothing to resume at $At yet: run it once without -Resume" }
if (-not $Resume) {
    # A fresh start never deletes the last run at this checkpoint: it is set aside under its own time.
    if (Test-Path $folder) {
        $aside = '{0}-{1}' -f $folder, (Get-Date -Format 'MMdd-HHmmss')
        if ($DryRun) { "WOULD set aside $folder as $aside" } else { Move-Item $folder $aside }
    }
    if (-not $DryRun) { New-Item -ItemType Directory -Force $folder | Out-Null }
    if ($At -gt 1) {
        $saves = Join-Path $capture ('s{0:d2}\saves' -f ($At - 1))
        foreach ($name in 'autosave.json', 'autosave.json.bak') {
            $file = Join-Path $saves $name
            if (-not (Test-Path $file)) { if ($name -eq 'autosave.json') { throw "No autosave at $saves" } else { continue } }
            if ($DryRun) { "WOULD copy $file" } else { Copy-Item $file $folder }
        }
    }
}
'PLAYTEST {0}: {1}, Sync {2}' -f $At, (Read-Stretch $At), (Read-Sync $At)
"  saves: $folder"
if ($At -eq 1 -and -not $Resume) { '  at the title: New Game' } else { '  at the title: Continue, then Autosave' }
if ($DryRun) { "WOULD start $exe -savepath `"$folder`""; return }
Start-Process -FilePath $exe -ArgumentList @('-savepath', ('"{0}"' -f $folder)) -WorkingDirectory (Split-Path -Parent $exe)
