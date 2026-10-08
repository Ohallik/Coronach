# Coronach native GPU diagnostic

Owned C++ source for an **opt-in Direct3D 12 main-camera envelope**. The DLL is dormant during ordinary play. The windowed player, raw Unity counters, saves and frozen chapter retain their existing policies.

The producer brackets main-camera callbacks with D3D12 bottom-of-pipe timestamp queries. A GPU-written frame tag and a private queue fence protect each of 64 reusable slots. The ending event explicitly flushes timestamp commands before signaling that fence. This introduces submission/synchronization overhead, can include CPU submission gaps, and excludes later overlays/presentation. It is **not clean timing, GPU busy time, headroom acceptance or physical scan-out evidence**.

Never align Unity's asynchronous counters by CSV row number. `native-gpu-frames.csv` binds sample/frame/step/time explicitly; `native-gpu-results.csv` retains raw ticks, frequency, tag, fences and callback QPC. `tools/native_gpu_analyze.py` requires exact coverage and identity. Preserve rejected captures and never convert an impossible Unity value to a corrected duration.

Build from a local [Zig 0.17.0 distribution](https://ziglang.org/download/0.17.0/) and the installed Unity editor's `Data/PluginAPI` headers:

```powershell
powershell -ExecutionPolicy Bypass -File tools/native_gpu/build.ps1 -Zig <path-to-zig.exe> -Output Builds/quality/<new-folder>/CoronachGpuClock.dll
```

The verified Windows x86_64 archive is `zig-x86_64-windows-0.17.0.zip`, SHA-256 `b5663f69581dcf391293fbf16c06cb80d81d806545ce618b4d0bab7f0eb8c428`, from Zig's [official download index](https://ziglang.org/download/index.json). No machine-wide compiler/SDK installation is needed. The build records compiler, source, installed headers, exact arguments and binary hashes. Only install the resulting DLL at `Lattice/Assets/Plugins/x86_64/CoronachGpuClock.dll` while no Unity owner/player is running, preserving the previous binary and package first. Keep the existing Windows-only importer metadata. Do not update the frozen workshop package.

Unity's installed headers are consumed in place, not copied into this repository. They carry the Unity Companion License. This code uses the documented [Unity rendering plugin interface](https://github.com/Unity-Technologies/NativeRenderingPlugin) and [Direct3D 12 timestamp API](https://learn.microsoft.com/en-us/windows/win32/direct3d12/timing); it does not change driver settings or request stable power state.

The initial frame-fence prototype is rejected: uncapped capture read the slot from 64 frames earlier even though Unity's frame fence had reached the sampled value. Preserve the source, binary, valid short pilot, wrong-tag control and rejected long capture under `Builds/quality/C1/native-gpu/`. A framework frame fence is not a completion guarantee for this independently submitted query sequence.

The build copies [compiler runtime notices](THIRD-PARTY-NOTICES.txt) beside every player. The native DLL and notices are owned/permitted tooling; no paid art or private package source is included.
