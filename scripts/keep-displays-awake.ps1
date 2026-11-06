# Holds the displays awake for timing runs (D154): a display-required execution state,
# plus a one-pixel mouse move because the state cannot wake displays already dark.
# Start it hidden before a chain and stop it afterwards:
#   $awake = Start-Process powershell -WindowStyle Hidden -PassThru -ArgumentList '-NoProfile','-File','scripts/keep-displays-awake.ps1','-Minutes','90'
#   Stop-Process -Id $awake.Id
param([int]$Minutes=90)
Add-Type -Name Native -Namespace KeepDisplays -MemberDefinition @'
[DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint esFlags);
[DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, System.UIntPtr dwExtraInfo);
'@
[KeepDisplays.Native]::mouse_event(1,1,0,0,[UIntPtr]::Zero)
[KeepDisplays.Native]::mouse_event(1,-1,0,0,[UIntPtr]::Zero)
$deadline=(Get-Date).AddMinutes($Minutes)
try { while((Get-Date) -lt $deadline){ [void][KeepDisplays.Native]::SetThreadExecutionState(0x80000003); Start-Sleep -Seconds 30 } }
finally { [void][KeepDisplays.Native]::SetThreadExecutionState(0x80000000) }
