# LATTICE — technical architecture

Status 2026-09-20. Read with `GAME_DESIGN.md`. This is the shape the slice is built in and the
shape the full game grows from.

## 1. Repository and toolchain

```
C:\Users\natem\Projects\SpaceRPG\          repo root (git init in P0)
  Lattice\                                 Unity project — 6000.4.7f1, URP 17.4, 3D forward
    Assets\_Project\{Art,Audio,Data,Prefabs,Resources,Scenes,Scripts,Settings,Yarn}
    Assets\Tests\{EditMode,PlayMode}
    Assets\TextMesh Pro\                   copied from Frostbound (headless flows cannot run the TMP importer prompt)
  art-src\                                 git-ignored archive: packs + Generated\<batch>\
  docs\                                    design, architecture, prompts, credits, progress, handoffs
  scripts\                                 headless.ps1, smoketest.ps1, exec.ps1, blender.ps1
  tools\                                   meshy\ (bridge + .env), gen_batch.py, portraits_*.py, blender\
  Builds\                                  git-ignored: Windows\Lattice.exe, logs\
```

Fixed facts of this machine: Unity at `C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe`;
Blender 5.x on PATH (`scripts\blender.ps1` finds it); Python 3.14 with Pillow, numpy, requests;
Real-ESRGAN at `C:\Users\natem\Projects\FrostboundUnity\art-src\realesrgan\realesrgan-ncnn-vulkan.exe`
(model `realesrgan-x4plus-anime`); Meshy bridge and key at
`C:\Users\natem\Projects\FrostboundUnity\tools\meshy\` (`meshy.py`, `.env`). Unity's entitlement
licence expires about monthly; exit code 198 with "No valid Unity Editor license" means Nathan
must sign in to Unity Hub (launch it with `ELECTRON_RUN_AS_NODE` removed from the environment).

Packages (manifest): `com.unity.render-pipelines.universal` 17.4.0, `com.unity.inputsystem`
1.19.0, `com.unity.cinemachine` 3.1.7, `com.unity.ai.navigation` 2.0.13, `com.unity.splines`
(latest for 6000.4), `com.unity.probuilder` 6.1.1, `com.unity.nuget.newtonsoft-json` 3.2.2,
`com.unity.2d.sprite` 1.0.0, `com.unity.ugui` 2.0.0, `com.unity.timeline`, `com.unity.test-framework`
1.4.6, `com.unity.modules.screencapture`, `dev.yarnspinner.unity`
(`https://github.com/YarnSpinnerTool/YarnSpinner-Unity.git#v3.2.4`). Pin versions to what
6000.4 accepts (check `https://packages.unity.com/<pkg>`).

## 2. Assemblies and namespaces

One asmdef per folder, namespace = assembly name. Dependencies flow downward only.

| Assembly | Holds | Depends on |
|---|---|---|
| `Lattice.Data` | ScriptableObject definitions, enums, pure data structs | — |
| `Lattice.Core` | services (GameState, Flags, Save, SceneFlow, Audio), input (GameInput, PadBridge, PadSupport, UiActions, PromptService), ZoneController, CameraRig | Data |
| `Lattice.Rpg` | Stats, Levels, Inventory, Gear, Fabrication, Loot, Quests | Data, Core |
| `Lattice.Combat` | Health, DamagePacket, Hitbox/Hurtbox, Projectile pool, Break, Skills, PlayerBrain, PartnerBrain, EnemyBrain, Boss, FlightMotor, GroundMotor, FormController | Data, Core, Rpg |
| `Lattice.World` | Interactable, Npc, Shop, RepairBay, Bench, WarpBeacon, DockingPad, TrafficLane, EncounterVolume, Membrane, MineNode, Salvage | Data, Core, Rpg, Combat |
| `Lattice.Dialogue` | Yarn integration, DialogueSystem, presenters, EmotionTags, PortraitSet lookup by form | Data, Core, Rpg |
| `Lattice.UI` | HUD, menus, shop UI, dialogue box, title, pause, prompts, PadFocus, UiKit | Data, Core, Rpg, Combat, World, Dialogue |
| `Lattice.Editor` | BatchTools, PackStaging, GenIntake, ScenesBuilder, PortraitTools, render/contact-sheet tools, gates | everything (Editor only) |
| `Lattice.Tests.EditMode` / `.PlayMode` | tests | everything |

Rules carried from Frostbound: **one MonoBehaviour per file, file name = class name** (scene
deserialisation breaks otherwise); every `CharacterController` sets `minMoveDistance = 0`; runtime
UI is built from `UiKit` in code (no hand-authored prefab UI); one material authority
(`LatticeToon` shader) applied on import by `ToonAssetPostprocessor`.

## 3. Boot and scene model

- `_Boot.unity` is the persistent scene: `[Services]` (GameState, SaveSystem, SceneFlow,
  AudioManager, PadSupport), `[UI]` root canvas (HUD, dialogue, menus, fade), `[Camera]`
  (main camera + Cinemachine brain). Never unloaded.
- Every playable area is a **zone scene** loaded additively by `SceneFlow.LoadZone(ZoneDef,
  spawnId)` with a fade. A zone scene contains a `ZoneRoot` with its `ZoneDef` reference,
  spawn points, encounter volumes, interactables and its own environment.
- `ZoneController` reads `ZoneDef.Kind` on load and applies: player form (`FormController.Set`),
  movement motor (Ground vs Flight), combat enable, camera profile (`CameraRig.Apply`), post
  volume, ambient audio, safe-zone rules (attack disabled, A = interact).
- Slice scenes: `Title`, `_Boot`, `Hub_CinderHalo` (SpaceSafe), `Hub_Decks` (GroundSafe),
  `Sorrel_Ridges` (GroundCombat, with an outpost safe pocket as a trigger volume),
  `Gullet_Tunnel` (SpaceCombat), `TallowDrift` (SpaceSafe), `Arena_Ground` and `Arena_Flight`
  (dev sandboxes for combat tuning and smoke tests).
- Dev boot (`LATTICE_DEV` define, dev player only): `-scene <name> -spawn <id> -loadout <id>`
  command-line args drop straight into any zone with a canned party; `-smoketest` drives the
  scripted route and prints markers; `-screenshot <path>` captures.

## 4. Input

Copy from Frostbound, rename namespaces: `Core/PadBridge.cs`, `Core/PadSupport.cs`,
`Core/UiActions.cs`, `Core/PromptService.cs`, `UI/PadFocus.cs`, and the relevant parts of
`Core/GameInput.cs` and `UI/UiKit.cs` (LinkGrid, HoverSelect). Spec: FrostboundUnity
`docs/CONTROLLER_SUPPORT.md`. Tests to port: `PadSupportTests` (EditMode), `PadSupportPlayTests`
(PlayMode, synthetic `extend: Joystick` layout, `backgroundBehavior = IgnoreFocus`, no
InputTestFixture).

Facts that matter: the desk pad is **Logitech Precision, VID 046D PID C21A** — a DirectInput HID
`Joystick`, not a `Gamepad`; d-pad on the X/Y axes, no hat, ten buttons (1 2 3 4 =
left/bottom/right/top, 5–8 shoulders/triggers, 9/10 select/start), no analog sticks, no stick
clicks. `PadBridge` re-emits it as a virtual `Gamepad` with `StickIsDpad`. **Batchmode enumerates
zero devices**: the only proof of pad support is the built player's log line
`PAD_BRIDGE_ATTACH vid=046D pid=C21A` plus an input-probe overlay in the dev build.

Action maps (one asset, built in code by `GameInput` so no scene serialises the package defaults):
`Ground` (Move, Attack, Dodge, Guard, Swap, SkillMod, Skill1–4, LockOn, Sprint, QuickItem,
CycleItem, CycleTarget, Interact, Pause, Log), `Flight` (Move, Fire, Roll, Lunge, Swap, SkillMod,
Skill1–4, LockOn, Boost, Brake, CycleItem, CycleTarget, Interact, Pause, Log), `UI` (from
`UiActions`). `ZoneController` enables exactly one gameplay map. Interact and Attack share A:
`PromptService` owns the arbitration (a visible prompt wins; safe zones disable Attack/Fire).

## 5. Camera and rendering

- `CameraRig`: one Cinemachine camera per zone kind (`GroundFollow`, `FlightFollow`), profiles in
  `CameraProfile` SOs (pitch, yaw, FOV, distance, dead-zone, look-ahead, speed-zoom). Fixed yaw.
- HD-2D: a URP Renderer Feature `Hd2dFeature` that (1) renders the scene colour into a 960×540
  target and blits with point filtering, (2) applies a tilt-shift blur (two-band DoF driven by
  screen Y) after the upscale, plus URP Volume: Bloom (threshold tuned so only emissives bloom),
  Color Lookup per zone, Vignette. Toon shading via `LatticeToon.shader` (ramp + rim + emission
  mask + outline pass). All parameters live in `Hd2dProfile` SOs referenced by `ZoneDef`.
- Shader stripping law: any shader used at runtime must be referenced by a material asset in a
  built scene (`Shader.Find` returns null in players).

## 6. Data model (ScriptableObjects in `Assets/_Project/Data`)

| SO | Fields |
|---|---|
| `CharacterDef` | id, name, sex, baseStats, growthPerLevel, forms {Natural, Shaped, Flight prefabs}, portraitNatural, portraitShaped, skills[4], startingGear, speakerId, syncHue |
| `ZoneDef` | id, scene, kind (GroundSafe/GroundCombat/SpaceSafe/SpaceCombat), cameraProfile, hd2dProfile, ambientAudio, musicId, mapIcon, safePockets |
| `EnemyDef` | id, prefab, kind (Biped/Quadruped/Static/Procedural), statsAtLevel(curve), attacks[] (`AttackDef`: telegraph, hitShape, damage, type, breakPower), weakness, resistance, breakThreshold, aiArchetype, lootTable, xp |
| `TechPartDef` | id, slot, damageType, tier, baseStats, affixPool, model, icon, salvage[] |
| `MaterialDef`, `ConsumableDef` | id, icon, stack, effect |
| `RecipeDef` | discipline, tier, inputs[], output, craftXp, unlockLevel |
| `SkillDef` | id, chargeCost, cooldown, damageType, modes (Ground/Flight/Both), groundVariant, flightVariant (anim, hit shape, VFX, projectile) |
| `QuestDef` | id, steps[] (`Objective`: TalkTo/Reach/Kill/Collect/Interact/Flag), rewards, flagsOnComplete |
| `LootTable`, `ShopInventory`, `AffixDef`, `PortraitSet` (16-slot, `PortraitEmotion` enum from Frostbound with `Tech` renamed `Synced`) | |

Runtime state (`GameState`, serialised by `SaveSystem` to JSON): party (members, active index,
levels, stats, gear, skill cooldowns reset), inventory (parts with instance ids, materials,
consumables, scrip), fabrication levels and XP, flags (`FlagService`), quest states, current zone
and spawn, playtime, three slots plus autosave. Save on repair-bay use, docking, warp, and zone
entry (autosave).

## 7. Combat runtime

- `Health` (Integrity, Break meter, resistances) receives `DamagePacket {amount, type, source,
  knockback, breakPower, isCrit}`; applies weakness/resist, break, i-frames, emits events for
  HUD numbers and VFX.
- `Hurtbox` (collider + owner + multiplier — boss weak points use ×2), `HitVolume` (spawned by
  attacks for N frames, overlap query on a layer mask, dedupes per victim), `Projectile` (pooled,
  moves in the plane, collides with Hurtbox/walls, carries a packet).
- `CombatActor`: state machine Idle/Move/Attack(n)/Dodge/Guard/Skill/Stagger/Down/Dead, animation
  events for hit frames, cancel windows, the Flash Move/Flash Guard timers.
- `PlayerBrain` maps input → actor by mode; `PartnerBrain` follows at 3 m, picks the player's
  target, attacks at 60% damage, retreats under 30% Integrity; `PartyController` holds both rigs
  and swaps brains (swap strike on entry).
- `GroundMotor` (CharacterController, 8-way, sprint, dash) and `FlightMotor` (Rigidbody-free
  kinematic integrator: velocity += thrust·dt, drag, boost multiplier, brake, plane lock at Y =
  zone plane, wall contact damage). Both expose the same `IMotor` (Move(dir), Dash(dir), Facing).
- `FormController`: activates the child visual for the current form, plays the shaping VFX,
  reparents weapon attach points, toggles vanes.
- `EnemyBrain`: Idle/Patrol/Alert/Chase/Attack/Recover/Stagger/Dead, archetypes as strategies
  (PackHunter, Swarm, Sentinel, Spitter, Mine, Serpent), telegraphs (light + audio 0.4–0.8 s before
  the hit), leash to spawn, `LootDrop` on death, `Spawner` and `EncounterVolume` (membranes drop
  when the count reaches zero). `BossController` layers phases over an EnemyBrain and drives
  weak-point Hurtboxes and the boss HUD.
- Procedural motion for static bodies: `Hover`, `Wriggle` (serpent segments follow a chain),
  `Skitter`.

## 8. World and interaction

`Interactable` (prompt text, glyph action, range, OnInteract), `Npc` (speaker id, Yarn node,
form-aware portrait lookup), `Shop` (ShopInventory → ShopUi), `RepairBay` (restore + save +
fee), `Bench` (Fabrication UI), `WarpBeacon` (locked flag → zone load), `DockingPad` (zone load
to the Decks room), `HailPoint` (a ship you fly alongside; dialogue in flight), `TrafficLane`
(spline + `TrafficShip` movers, simple separation), `MineNode` (materials on interact, respawn
flag), `SalvageField` (float-through pickups), `Membrane` (encounter gate visual). Quests advance
through `QuestService.Report(objectiveKind, id)`.

## 9. Dialogue and portraits

Yarn Spinner 3.2.4, one `.yarn` per zone plus `Ambient.yarn`. Lines carry `#emotion:<name>`;
`EmotionTags` (copied) resolves to `PortraitEmotion`; `PortraitLookup(speakerId, currentForm)`
picks Natural or Shaped. `DialogueSystem`, `LinePresenter`, `OptionsPresenter`, `TypewriterPacing`
ported from Frostbound with the Frostbound-specific bits (CAPS speech, cutscene director) removed.
Yarn commands available to writers: `<<setFlag>>`, `<<giveItem>>`, `<<startQuest>>`, `<<shop>>`,
`<<repair>>`, `<<bench>>`, `<<joinParty Sela>>`, `<<warpUnlock>>`.

## 10. Art pipeline (generated art only)

Policy: every visible asset is generated for this game — characters, portraits, enemies, ships,
environment pieces, textures, skyboxes, UI panels, icons. Owned packs in `art-src` are allowed only
for blockout geometry that is replaced before a LOOK gate, for rigging donors (skeleton only),
for particle textures and prefabs (Cartoon FX Remaster Free, Free Slash VFX, Unity Particle Pack,
Kenney Particle Pack), for input glyphs (Kenney Input Prompts), for fonts, and for sound.

Lanes:
1. **2D references and sheets** — the session's image-generation tool, following
   `generated-3d-characters/references/prompt-craft.md` (bounded ratios, anti-fusion gaps, no
   gloss, one view, no scenery). Sync-lines are drawn in a single saturated key colour so they
   survive reconstruction and can be extracted as an emission mask.
2. **Models** — `tools/meshy/meshy.py img3d --riggable --band humanoid` (heroes, NPCs, Sentinel
   Husk) then `rig --input-task-id <id> --height <m>`; `--band hero` static for ships, bosses,
   Drifter/Dart/Shellmine, environment; quadruped Ridgehound through the Blender donor graft
   (`tools/blender/p64_donor_rig.py`, Fox donor); `retexture` for NPC body variants. Every task's
   credits go in `docs/art/gen-manifest.json`. Strip embedded textures
   (`p64_strip_embedded.py`), downscale albedo to 1024, extract the sync-line emission mask
   (`tools/emission_mask.py`, colour-key on the albedo).
3. **Unity intake** — `PackStaging` copies `art-src/Generated/<batch>` subsets into
   `Assets/_Project/Art/Generated/<batch>`; `ToonAssetPostprocessor` assigns `LatticeToon`,
   binds albedo by subject token, sets Humanoid import for bipeds with the explicit Meshy bone
   map (Hips→Hips, Spine02→Spine, Spine01→Chest, Spine→UpperChest …) and gates on
   `avatar.isHuman`; `GenIntake` builds `Prefabs/Characters/<Name>_<Form>.prefab` and
   `Prefabs/Enemies/<Name>.prefab` with animator, hurtboxes, attach points, and measures height
   through bone matrices.
4. **Animation** — Humanoid clips from the owned Universal Animation Library (`UAL1_Humanoid.fbx`,
   imported Humanoid) and KayKit Character Animations retargeted onto Meshy avatars; a
   `LatticeHumanoid.controller` with parameters `Speed`, `Attack`, `Dodge`, `Guard`, `Skill`,
   `Hit`, `Dead`, `Flight`. Flight uses a horizontal pose blend, vanes driven by a script.
5. **Portraits** — one 4×4 grid per (character, form) at 2304×2304 after 4× Real-ESRGAN
   (`portraits_redo_slice.py` → upscale → `portraits_redo_assemble.py`), slot order = enum order,
   sliced in one import (`PortraitTools.Import`), wired into a `PortraitSet`.
6. **Environment** — a generated modular set (list in `SLICE_PROMPT.md` P3) plus generated
   tileable textures on ProBuilder/spline geometry; skybox equirect 4096×2048 per space zone;
   planet/gas-giant textures; tunnel membrane texture.
7. **Verification** — every model rendered posed in `Arena_Ground`/`Arena_Flight` with graphics
   on, twice (keep the second), into `Builds/logs/renders/<name>.png`, plus a labelled contact
   sheet (`tools/contact_sheet.py`). Inspected with the Read tool before the asset is declared
   done.

## 11. Verification and build

- `scripts/headless.ps1 verify|tests|playtests|build|builddev|exec -Method <T.M>`: Frostbound's
  script with `$ProjectPath = Lattice`, save path `LocalLow\Nathan\Lattice`, method prefix
  `Lattice.EditorTools`. Keep the silence watchdog (ILPP hang) and the fail-closed test XML
  adjudication.
- `scripts/exec.ps1 -Method … -Marker …`: Frostbound's `p64-exec.ps1` pattern (wait for the log
  marker, then kill Unity) for asset-heavy editor jobs, with `-Graphics` for renders.
- `scripts/smoketest.ps1 -Scene <zone> [-Route slice]`: boots `Builds\Windows\Lattice.exe` with
  rendering, asserts markers, writes `Builds\logs\<scene>-screenshot.png`. The screenshot must be
  opened and judged; compare its byte size against neighbours (a 12 KB PNG is a black frame, not
  evidence).
- Markers the slice route must emit in order: `TITLE_BOOT_OK`, `NEW_GAME_OK`, `ZONE_ENTER
  Hub_CinderHalo`, `DOCK_OK`, `DIALOGUE_OK Orrin`, `PARTY_JOIN Sela`, `ZONE_ENTER Sorrel_Ridges`,
  `FORM Shaped`, `COMBAT_KILL Ridgehound`, `SWAP_OK`, `FLASH_MOVE_OK`, `BOSS_DOWN Burrower`,
  `QUEST_STEP warpkey`, `ZONE_ENTER Gullet_Tunnel`, `FORM Flight`, `LUNGE_KILL`, `BOSS_DOWN Cantor`,
  `ZONE_ENTER TallowDrift`, `SAVE_OK`, plus `PAD_BRIDGE_ATTACH` whenever the Logitech pad is
  present.
- Tests: EditMode for every pure system (damage math, break, stats, levels, fabrication XP,
  inventory, quests, save round-trip, emotion tags, pad translation); PlayMode for zone load →
  form → motor → one kill in each arena, swap, dock, warp.
- Performance gate: 60 fps at 1080p in the Gullet with 12 enemies, on this machine (RTX 5070).
