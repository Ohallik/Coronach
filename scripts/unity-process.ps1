function Assert-LatticeUnlocked {
    param([string]$ProjectPath)
    $conflicts=@(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($ProjectPath) })
    if($conflicts.Count -gt 0){throw "FAILED: Unity already holds Lattice project: $($conflicts.ProcessId -join ',')"}
}
function Stop-LatticeProcessTree {
    param([int]$ProcessId)
    $children=@(Get-CimInstance Win32_Process -Filter "ParentProcessId = $ProcessId" -ErrorAction SilentlyContinue)
    foreach($child in $children){Stop-LatticeProcessTree -ProcessId $child.ProcessId}
    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}
