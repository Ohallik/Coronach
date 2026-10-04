function Assert-LatticeUnlocked {
    param([string]$ProjectPath)
    $conflicts=@(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($ProjectPath) })
    if($conflicts.Count -gt 0){throw "FAILED: Unity already holds Lattice project: $($conflicts.ProcessId -join ',')"}
}
function Test-UnityPlayerPath {
    param([AllowNull()][string]$ExecutablePath)
    if(-not $ExecutablePath){return $false}
    # CIM can return extended Windows paths (\\?\C:\...). PowerShell's
    # filesystem provider cannot Join-Path those; native IO preserves them.
    $directory=[IO.Path]::GetDirectoryName($ExecutablePath)
    if($directory){return [IO.File]::Exists([IO.Path]::Combine($directory,'UnityPlayer.dll'))}
    return $false
}
function Stop-LatticeProcessTree {
    param([int]$ProcessId)
    $children=@(Get-CimInstance Win32_Process -Filter "ParentProcessId = $ProcessId" -ErrorAction SilentlyContinue)
    foreach($child in $children){Stop-LatticeProcessTree -ProcessId $child.ProcessId}
    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}
function Get-LatticeSaveWriters {
    param([string]$ProjectPath)
    Get-CimInstance Win32_Process -Filter "Name = 'Coronach.exe' OR Name = 'Lattice.exe' OR Name = 'Unity.exe'" | Where-Object {
        $_.Name -in @('Coronach.exe','Lattice.exe') -or -not $_.CommandLine -or $_.CommandLine.IndexOf($ProjectPath,[StringComparison]::OrdinalIgnoreCase) -ge 0
    }
}
