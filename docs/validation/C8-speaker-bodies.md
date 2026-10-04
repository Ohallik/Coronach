# Survivor and Keeper bodies - October 3, 2026

**Body replacement accepted; C8 remains OPEN.** Codex generated isolated full-body
references with built-in imagegen from the existing portrait identities, inspected
them, then used Meshy T2 at 10,000 target polygons and its humanoid rigging endpoint.
Each body cost 15 + 5 credits, **40 total**. The live API balance was 2,863 before
this work and 2,823 afterward. Its pre-batch balance was 925 below D125's historical
estimate; that difference is not attributed to this batch. Actual task IDs and
charges remain in [the ledger](../art/gen-manifest.json), with complete prompts,
local reference paths and review notes in [P44-npc-bodies.json](../art/P44-npc-bodies.json).

The visible bodies, meshes and albedos are their own generated assets. Existing
owned animation clips supply idle/walk motion. Intake stripped embedded duplicate
textures, resized unique albedos to 1024 and applied the project toon shader.
Humanoid avatars pass; actual posed skin movement is measured, not inferred from
the presence of an Animator. Heights are 1.850 m / 1.950 m. Walk skin travel relative
to height is 0.081 / 0.082, with 260 / 260 degrees of summed local joint rotation.
The natural forms have no extracted cyan emission coverage.

## Rejections and corrections

- The first Blender contact sheet clipped the Keeper's crown. A new posed-vertex
  projection check rejected the old framing. The preview now freezes imported
  animation before scale normalization and measures evaluated vertices; all four
  views of both bodies fit. Raw red/green logs and images are retained under
  `Builds/quality/session-2026-10-03/` and `Builds/logs/blender-p64_preview-20261003-1940*`.
- `SpeakerBodyTests` first failed 0/2 because the definitions borrowed Orrin and
  Hal. Independent intake fixed those references, but the first Sorrel gameplay
  capture still showed Orrin: the map had baked the old prefab into its NPC.
  Two scene-level checks then failed on both maps. `SpeakerBodyUpgrade.Apply`
  replaces only those two visual children, preserving NPC roots, interactions,
  flags and layouts. The retired alias assignment is removed from definition
  setup. Full EditMode now passes **60/60**.
- The first Keeper launch failed during process preflight before a player began:
  `Join-Path` received an empty executable parent. The guard now checks that a
  parent exists. A synthetic process record reproduces the old exception; the
  corrected guard handles it and still detects both Unity editors and arbitrary
  Unity player executables. The control is retained in `npc-bodies/preflight-parent-control.txt`.
- Keeper capture `-02` stopped outside interaction range and was rejected.
  Moving the route's approach from z=1.3 to z=2.2 fixes the navigation. Dialogue,
  flag and focus assertions are unchanged.

## Final ordinary-input captures

Release, Ultra, 1920 x 1080, 60 fps, VSync off. Each capture ran alone as a blocking
call through the ordinary Input System, with AutoAdvance off.

| Under `Builds/quality/workshop/` | Result |
|---|---|
| `npc-survivor-body-01` | Traversal passed but **visual rejection**: baked donor body remained. |
| `npc-survivor-body-02` | **PASS**, 57.23 s, 3,435/3,435 focused, analyzer valid. Correct generated body beside its portrait; approach, conversation and return path work. |
| `npc-keeper-body-03` | **PASS**, 32.02 s, 1,923/1,923 focused, analyzer valid. Correct shawl/coat body and portrait; conversation, side path and return work. |

Opened final gameplay dialogue views Survivor `007` and Keeper `003`, plus
nearby approach/side views. Opened Unity front/back/side and walk poses; the
prefab render set is archived in `npc-bodies/prefab-renders/`. Complete video/audio
files are retained, but no continuous viewing, audio audition or human physical
feel is claimed. These captures are not clean performance measurements.

Nathan's original save SHA-256 values remain unchanged. Raw test XML, intake logs,
rendered poses and preflight control are under `Builds/quality/workshop/npc-bodies/`.
[Evidence hashes](C8-speaker-bodies-evidence.json) identify the exact artifacts.
The full PlayMode integrated rerun remains due before the workshop gate, as
recorded in the preceding route checkpoint.
