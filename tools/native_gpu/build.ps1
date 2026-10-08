param(
    [Parameter(Mandatory=$true)][string]$Zig,
    [string]$UnityEditor='C:/Program Files/Unity/Hub/Editor/6000.4.7f1/Editor/Unity.exe',
    [string]$Output='Builds/quality/NativeGpuCompiler/CoronachGpuClock.dll'
)
$ErrorActionPreference='Stop'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$compiler=(Resolve-Path -LiteralPath $Zig).Path
$version=(& $compiler version).Trim()
if($LASTEXITCODE -ne 0 -or $version -ne '0.17.0'){throw 'Use the pinned Zig 0.17.0 Windows x86_64 toolchain'}
$headers=Join-Path (Split-Path (Resolve-Path -LiteralPath $UnityEditor).Path -Parent) 'Data/PluginAPI'
$source=Join-Path $PSScriptRoot 'CoronachGpuClock.cpp'
$destination=[IO.Path]::GetFullPath((Join-Path $repo $Output))
$quality=[IO.Path]::GetFullPath((Join-Path $repo 'Builds/quality'))+[IO.Path]::DirectorySeparatorChar
if(-not $destination.StartsWith($quality,[StringComparison]::OrdinalIgnoreCase)){throw 'Build native output under Builds/quality before installing it'}
if(Test-Path -LiteralPath $destination){throw 'Preserve prior binary; choose a new output folder'}
New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
$arguments=@('c++','-target','x86_64-windows-gnu','-std=c++17','-O2','-shared','-fno-exceptions','-fno-rtti','-I'+$headers,$source,'-ldxguid','-o',$destination)
& $compiler @arguments *> ($destination+'.build.log')
if($LASTEXITCODE -ne 0){throw 'Native compilation rejected; preserve the build log'}
$headerInventory=@('IUnityInterface.h','IUnityGraphics.h','IUnityGraphicsD3D12.h' | ForEach-Object { $p=Join-Path $headers $_;@{name=$_;sha256=(Get-FileHash -LiteralPath $p).Hash} })
@{zigVersion=$version;compilerSha256=(Get-FileHash -LiteralPath $compiler).Hash;sourceSha256=(Get-FileHash -LiteralPath $source).Hash;binarySha256=(Get-FileHash -LiteralPath $destination).Hash;arguments=$arguments;unityHeaders=$headerInventory} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath ($destination+'.build.json') -Encoding UTF8
Write-Output ('NATIVE_GPU_BUILD_OK '+$destination)
