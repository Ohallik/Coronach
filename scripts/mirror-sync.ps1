<#
.SYNOPSIS
  Creates or refreshes a test mirror: a second copy of the project, beside this one, with its own Unity Library.

  Tests run in a mirror (focused.ps1 and suite-shared.ps1 do this by themselves) so that they don't wait for the main
  project's Unity and builders don't wait for them. Mirrors live in ..\CoronachTest\<Name>: A, C, ... serve focused
  runs, S serves the whole suite.

  A mirror never touches Nathan's saves: headless.ps1 sees the .test-mirror marker, skips the save shield and passes
  -savepath, so the game keeps its saves under the mirror's own Builds\logs.

  The copy is one-way (main to mirror) and mirrors exactly: anything a test wrote into the mirror's Assets is undone.
  It waits for the mirror's own Unity to be gone, and for a builder in the main project to finish (ten minutes at
  most), so a half-written scene isn't copied.

.EXAMPLE
  .\scripts\mirror-sync.ps1 -Name A        # first call creates the mirror and copies Library (about 6 GB)
#>
param(
    [Parameter(Mandatory)][ValidatePattern('^[A-Z]$')][string]$Name,
    [int]$TurnWaitSec = 600,
    [int]$MirrorWaitMinutes = 60
)
$ErrorActionPreference = 'Continue'
$root = Split-Path $PSScriptRoot -Parent
if (Test-Path -LiteralPath (Join-Path $root '.test-mirror')) { "MIRROR_SYNC_FAILED this tree is itself a mirror"; exit 1 }
$base = Join-Path (Split-Path $root -Parent) 'CoronachTest'
$mirror = Join-Path $base $Name
# /MIR deletes whatever the source doesn't have: only ever aim it inside the mirror folder.
if (-not ([IO.Path]::GetFullPath($mirror)).StartsWith([IO.Path]::GetFullPath($base) + '\', [StringComparison]::OrdinalIgnoreCase)) { "MIRROR_SYNC_FAILED unsafe target $mirror"; exit 1 }
New-Item -ItemType Directory -Force (Join-Path $mirror 'Lattice') | Out-Null
New-Item -ItemType Directory -Force (Join-Path $mirror 'Builds\logs') | Out-Null

function Mirror-Unity { @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf((Join-Path $mirror 'Lattice'), [StringComparison]::OrdinalIgnoreCase) -ge 0 }).Count -gt 0 }
function Main-Builder { @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf((Join-Path $root 'Lattice'), [StringComparison]::OrdinalIgnoreCase) -ge 0 -and $_.CommandLine -notmatch '-runTests' }).Count -gt 0 }
function Take([System.Threading.Mutex]$m, [TimeSpan]$wait) { try { return $m.WaitOne($wait) } catch [System.Threading.AbandonedMutexException] { return $true } }
function Copy-Tree([string]$rel, [string[]]$more = @()) {
    $from = Join-Path $root $rel
    if (-not (Test-Path $from)) { return 0 }
    $to = Join-Path $mirror $rel
    $null = robocopy $from $to /MIR /R:1 /W:1 /MT:16 /XJ /NFL /NDL /NJH /NJS /NP @more
    # Robocopy: below 8 is success (bits 1, 2, 4 mean copied, extra, mismatched).
    if ($LASTEXITCODE -ge 8) { "MIRROR_SYNC $rel robocopy code $LASTEXITCODE"; return 1 }
    return 0
}

$mirrorTurn = New-Object System.Threading.Mutex($false, "Global\CoronachMirror$Name")
if (-not (Take $mirrorTurn ([TimeSpan]::FromMinutes($MirrorWaitMinutes)))) { "MIRROR_SYNC_FAILED mirror $Name stayed busy"; exit 1 }
$mainTurn = New-Object System.Threading.Mutex($false, 'Global\CoronachUnityTurn')
$heldMain = $false
$bad = 0
try {
    $deadline = (Get-Date).AddMinutes($MirrorWaitMinutes)
    while ((Mirror-Unity) -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 3 }
    if (Mirror-Unity) { "MIRROR_SYNC_FAILED a Unity is still running in mirror $Name"; exit 1 }
    # Wait out a builder (it is writing scenes and definitions), not a test run (it writes no assets). Holding the
    # main turn while copying keeps a new builder from starting half-way through.
    # Queue for the main turn like a builder does. Trying it only when it happened to be free lost every time to the
    # builders waiting on it properly: with twenty engineers each copy waited its full ten minutes and then copied
    # anyway, and the mirrors stood idle meanwhile (four test runs in half an hour, November 12).
    $heldMain = Take $mainTurn ([TimeSpan]::FromSeconds($TurnWaitSec))
    # Holding the turn, no builder started through exec-when-green is running; one started some other way gets a minute.
    $until = (Get-Date).AddSeconds(60)
    while ((Main-Builder) -and (Get-Date) -lt $until) { Start-Sleep -Seconds 2 }
    $quiet = -not (Main-Builder)
    $started = Get-Date
    $first = -not (Test-Path (Join-Path $mirror 'Lattice\Library'))
    if ($first) {
        $null = robocopy (Join-Path $root 'Lattice\Library') (Join-Path $mirror 'Lattice\Library') /E /R:0 /W:0 /MT:16 /XJ /NFL /NDL /NJH /NJS /NP /XF EditorInstance.json ilpp.pid ArtifactDB-lock SourceAssetDB-lock
        # A file Unity holds open may not copy: the mirror's Unity imports again whatever is missing.
        "MIRROR_SYNC first copy of Library, robocopy code $LASTEXITCODE"
    }
    $bad += Copy-Tree 'Lattice\Assets' @('/XF', 'InitTestScene*')
    $bad += Copy-Tree 'Lattice\Packages'
    $bad += Copy-Tree 'Lattice\ProjectSettings'
    $bad += Copy-Tree 'docs'
    $bad += Copy-Tree 'tools' @('/XF', '.env', '/XD', '__pycache__')
    $bad += Copy-Tree 'scripts'
    Set-Content -LiteralPath (Join-Path $mirror '.test-mirror') -Value $Name -Encoding ascii
    $note = if ($quiet) { '' } else { ' (a builder was still running in the main project: copied anyway, its output may be half there)' }
    Set-Content -LiteralPath (Join-Path $mirror 'Builds\logs\mirror-sync.txt') -Value ("synced {0:yyyy-MM-dd HH:mm:ss} from {1}" -f (Get-Date), $root) -Encoding ascii
} finally {
    if ($heldMain) { $mainTurn.ReleaseMutex() }
    $mirrorTurn.ReleaseMutex()
}
if ($bad) { "MIRROR_SYNC_FAILED $Name"; exit 1 }
"MIRROR_SYNC_OK $Name {0:N0}s$note" -f ((Get-Date) - $started).TotalSeconds
exit 0
