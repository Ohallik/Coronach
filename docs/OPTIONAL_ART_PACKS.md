# Optional purchased landscape art

The public Unity project opens and builds without either Staggart pack. Committed scenes contain complete generated grass and water assets using `Lattice/Toon`. No gameplay, collider, navigation, save or quest depends on a paid package. D127 permits the licensed packs in Nathan's local production build; D131 defines the boundary.

## Public clone

Use Unity **6000.4.7f1** and the committed package manifest. From the repository root, with its editor closed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/headless.ps1 -Mode build -TimeoutSec 1800
```

The executable is `Builds/Windows/Coronach.exe`. First import resolves ordinary Unity/Yarn dependencies. Unity 6000.4's known Shader Graph `GUID` namespace defect is patched only in `Library/PackageCache` by `tools/patch_unity_packages.py`. The execute wrapper preserves the first error log and retries once for that exact compiler error after a cold import; other compiler and package resolution failures remain failures. It also patches already imported projects before launch. The paid packs are never manifest dependencies.

## Licensed local build

Close the editor owning this project. Download the purchased Unity 6 packages through Nathan's Asset Store account. The installer expects:

`%APPDATA%/Unity/Asset Store-5.x/Staggart Creations/Shaders/`

```powershell
python tools/install_staggart.py
python tools/install_staggart.py --install
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/exec.ps1 -Method Lattice.EditorTools.LocalBiomeBake.Build -Marker LOCAL_BIOMES_OK
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/headless.ps1 -Mode build -TimeoutSec 1800
```

The first command is a dry run. Installation checks archive destinations, git exclusion and Unity project ownership before writing. Validated versions: Stylized Water 3 **3.2.7**, Stylized Grass Shader **2.1.0** for Unity 6. Package hashes are in the landscape validation report.

Everything licensed or derived from it stays in these ignored roots, including their root metadata:

- `Lattice/Assets/Stylized Water 3/`
- `Lattice/Packages/xyz.staggart-creations.stylized-grass/`
- `Lattice/Assets/_Project/LocalStaggart/`

`LocalBiomeBake` is our integration recipe, not a copy of vendor implementation. It clones materials, combines the supplied smaller grass LOD, and creates a local URP renderer/pipeline with the vendor features. Every resulting material, grass mesh, renderer and resource reference is private. Public scenes retain only our generated meshes/materials and stable resource names. At startup `BiomePatch` installs the optional local pipeline and swaps a patch only when its local mesh, material and supported shader are all present. Otherwise the public fallback remains visible. A runtime swap does not save the scene.

The private renderer preserves the project's HD-2D and SSAO features, uses opaque depth before water, and enables restrained refraction/reflection. Grass bending, water height prepass, directional caustics and transparent-object refraction are disabled. Save format is identical in both builds.

## Editing and committing

`BiomeLandscapeUpgrade.Apply` is the isolated public surface builder for Sorrel, the ground proving yard, Hushwell and Tallow. It also updates affected navigation. Run it after a base map rebuild, then rerun `LocalBiomeBake.Build` if using the packs. Do not use `WorldBuilder.Prepare` or `BuildAll`.

Unity automatically adds embedded grass to the local `packages-lock.json`. Remove that entry before committing; keep the official built-in `com.unity.modules.wind` dependency. Never force-add ignored files, vendor examples, pack archives or private overrides. Review staged names and scene dependencies. `BiomeSurfaceTests` rejects any serialized scene dependency into a vendor/private root.

To test the public boundary, `python tools/public_clone.py --output Builds/quality/<new-short-name>` exports tracked and nonignored Unity source, scripts and the package compatibility patch, with a SHA-256 manifest. It excludes Library, local overrides, paid packs and saves, and strips the embedded grass lock entry. Build with the copied headless script. Keep export paths short on Windows; package extraction can encounter OS rename/path failures. Preserve failures as evidence rather than calling an incomplete import a pass.

## Palette and placement direction

Sorrel uses dry olive grass in low clumps along sheltered ridges and outpost edges. The proving yard uses perimeter fringes; its fighting floor stays open. Hushwell uses blue-green banks around three real shallow basins, with dry access to the nursery discovery and lift. Tallow's garden is a small contained recycling bed outside the public aisle. Maximum blade height is 0.52 m, below the 0.6 m sightline limit; dressing adds no colliders.

For the later atlas maps, carry this boundary forward: broken reeds and shallow channels in the swamp, sparse frost grass beside meltwater on the ice world, and dry ash grasses with mineral pools on the volcanic world. Lava needs a separate emissive material and readable hazard boundary. These are art directions, not claims that those maps or performance passes exist. Each new map still needs its brief, blockout, fixed-camera review, ordinary traversal and C1 measurements.
