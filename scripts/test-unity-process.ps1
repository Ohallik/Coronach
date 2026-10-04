param([Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'unity-process.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$directory=[IO.Path]::GetFullPath((Join-Path $repo $Output))
$quality=[IO.Path]::GetFullPath((Join-Path $repo 'Builds/quality'))+[IO.Path]::DirectorySeparatorChar
if(-not $directory.StartsWith($quality,[StringComparison]::OrdinalIgnoreCase)){throw 'Contract evidence must be inside Builds/quality'}
if(Test-Path -LiteralPath $directory){throw 'Choose a fresh contract directory'}
New-Item -ItemType Directory -Path $directory | Out-Null
$exe=Join-Path $directory 'Fixture.exe'
if(Test-UnityPlayerPath $exe){throw 'An ordinary executable without UnityPlayer.dll was classified as a Unity player'}
[IO.File]::WriteAllBytes((Join-Path $directory 'UnityPlayer.dll'),[byte[]]@())
if(-not (Test-UnityPlayerPath $exe)){throw 'Ordinary Unity player path was missed'}
if(-not (Test-UnityPlayerPath ('\\?\'+$exe))){throw 'Extended Windows Unity player path was missed'}
if(Test-UnityPlayerPath $null){throw 'Missing executable path was classified as a player'}
if(Test-UnityPlayerPath 'Fixture.exe'){throw 'Bare executable name searched an unrelated working directory'}
Write-Output 'UNITY_PROCESS_PATH_CONTRACT_OK ordinary=1 extended=1 absent=0 null=0 bare=0'
