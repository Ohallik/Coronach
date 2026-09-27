# Asset sources and licences

**Policy (decided 2026-09-20): the game's visible art is generated, not sourced.** Characters,
portraits, enemies, ships, environment pieces, textures, skyboxes, UI panels and icons come from
Nathan's image + Meshy pipeline (see `ARCHITECTURE.md` §10 and `SLICE_PROMPT.md`). The packs below
stay on disk as blockout stand-ins, rigging donors, particle prefabs/textures, input glyphs, fonts
and sound only.

Every external pack in `art-src/` (git-ignored archive) with its origin and licence, recorded
at acquisition time. Rule carried over from Frostbound: CC0 preferred; CC-BY needs a credit
line here the day it is downloaded; NC / ND / SA licences are rejected. Curated subsets get
staged into the Unity project through a `PackStaging` table, never hand-copied.

## Downloaded 2026-09-20 into `SpaceRPG/art-src/`

| Folder | Source | Licence | Contents / intended use |
|---|---|---|---|
| `Kenney_SpaceKit` | https://kenney.nl/assets/space-kit | CC0 | 150 models: craft (speeder A–D, cargo, miner, racer), hangars, corridors, platforms, terrain + road tiles, craters, rocks, crystals, rover, turrets, monorail, rocket parts, 2 astronauts, 1 alien, 2 weapons. Moon surface + space-city exterior blockout. |
| `Kenney_SpaceStationKit` | https://kenney.nl/assets/space-station-kit | CC0 | ~100 interior pieces: walls/doors/windows, floors, stairs, beds, chairs, tables, computers, displays, containers. Station / shop interiors. |
| `Kenney_BlasterKit` | https://kenney.nl/assets/blaster-kit | CC0 | 18 blasters + scopes/silencers/clips, grenades, targets. Player + NPC energy weapons. |
| `Kenney_SciFiSounds` | https://kenney.nl/assets/sci-fi-sounds | CC0 | 78 OGG: lasers (small/large/retro), engines, thrusters, force fields, explosions, doors, computer noise. |
| `Kenney_AlienUfoPack` | https://kenney.nl/assets/alien-ufo-pack | CC0 | 2D sprites: UFOs, laser bolts/bursts in 5 colours. Projectile textures for particles/trails. |
| `Kenney_Planets` | https://kenney.nl/assets/planets | CC0 | 2D planet sprites. Map / warp-select UI icons. |
| `Kenney_UiPackSciFi` | https://kenney.nl/assets/ui-pack-sci-fi | CC0 | Sci-fi UI panels, buttons, sliders (the "space expansion" of the UI pack). HUD + menus. |
| `Kenney_InputPrompts` | https://kenney.nl/assets/input-prompts | CC0 | Keyboard/mouse/gamepad glyphs for on-screen button prompts. |
| `KayKit_SpaceBaseBits` | https://kaylousberg.itch.io/space-base-bits | CC0 | 114 models: base modules, landers, landing pads, cargo, space trucks, drill, terrain slopes, tunnels, solar panels, wind turbines. Moon outpost + quest site. |
| `Quaternius_UltimateSpaceKit` | https://quaternius.com/packs/ultimatespacekit.html (Drive blocked; pulled via https://poly.pizza/bundle/Space-Kit-YWh743lqGX; `Textures/Atlas.png` extracted from the poly.pizza GLB) | CC0 | 87 models: 4 spaceships, 4 mechs, 3 astronauts, 3 enemies (flying/large/extra-small), 11 planets, alien trees/plants/rocks, house pods, dome, rovers, pickups. Player flight/ground-form kitbash source, NPC ships, moon flora. |
| `Quaternius_LowPolySpaceships` | https://quaternius.itch.io/lowpoly-spaceships | CC0 | 5 spaceships. NPC traffic variety. |
| `Quaternius_50LowPolyGuns` | https://quaternius.itch.io/50-lowpoly-guns | CC0 | 55 gun + attachment meshes. Kitbash parts for tech weapons. |
| `Quaternius_AnimatedAlien` | https://quaternius.com/packs/animatedalien.html (Drive blocked; 3 models pulled singly from poly.pizza, ids HYUUkdugoP / RRliSQBP7r / sUTLXji0aL; flat materials, no textures) | CC0 | Small rigged aliens with 9–14 clips each. Enemy / critter candidates, not the player race. |

## Already owned in `C:\Users\natem\Projects\FrostboundUnity\art-src\` (reuse, do not re-download)

All CC0 unless noted; licences already recorded in FrostboundUnity `docs/CREDITS.md`.

| Pack | Use here |
|---|---|
| `Quaternius_ModularSciFiMegaKit` (759 files) | Station / city interiors and exteriors: corridors, columns, platforms, doors, decals; 3 alien creatures (Cyclop, Oculichrysalis, Scolitex). |
| `Quaternius_SciFiEssentialsKit` | Guns, 3 robot enemies (EyeDrone, QuadShell, Trilobite), sci-fi props (lockers, desks, crates, health packs, key cards). |
| `Kenney_ModularSpaceKit` | Corridor / room templates. |
| `Quaternius_UniversalBaseCharacters_Source` (paid Source tier, CC0) | 6 rigged humanoid bodies + 19 hairstyles: the cheapest base for the player race (retint + head kitbash). |
| `Quaternius_UniversalAnimationLibrary` 1 + 2 | Humanoid clips for that rig. |
| `Quaternius_UltimateMonsters` (38 rigged) | Space-faring life in warp tunnels. |
| `Quaternius_AnimatedFish`, `Quaternius_AnimatedCuteFish` | Space fish / whales. |
| `Quaternius_AnimatedRobot`, `Quaternius_AnimatedDinosaurs`, `Quaternius_AnimatedMonsters`, `Quaternius_AnimatedEasyEnemies` | Enemy rosters. |
| `Quaternius_CyberpunkGameKit`, `Quaternius_UltimateModularSciFi` | Only `.blend` sources present (Drive download never finished); convert with Blender headless if needed. |
| `Kenney_ParticlePack`, `Kenney_ImpactSounds`, `Kenney_InterfaceSounds`, `Kenney_RpgAudio`, `Kenney_MusicJingles` | Particles + UI/impact audio. |
| `Sonniss_GDC2020/2023/2024/2026` (~125 GB) | Deep SFX library (royalty-free GDC bundles). |
| `UI_Fonts`, `UI_GameIcons` | Fonts (OFL) and game-icons.net icons (CC-BY 3.0, credit required). |

## Unity Asset Store cache (`%APPDATA%\Unity\Asset Store-5.x`, Asset Store EULA)

| Package | Use here |
|---|---|
| Jean Moreno, Cartoon FX Remaster Free | Explosions, electric arcs, hits, sword slashes. |
| MaykerStudio, Free Slash VFX | Laser-sword slash arcs and projectiles. |
| Unity Technologies, Particle Pack Starter Assets | Energy explosions, plasma, sparks, flames. |
| RetroStyle Games, Stylized Sci Fi Mech Robot | Rigged mech with idle/walk/landing/death. Ground-combat-form candidate. |
| KMProd, Sci-Fi Old Rusty Props PBR | Industrial props (containers, bulkheads, ladders). |
| Staggart Creations, Stylized Water 3 + Stylized Grass Shader (paid, owned) | Moon-surface liquids / vegetation if wanted. |
| Archie Andrews, Prefab Brush | Editor dressing tool. |

## Not available free — will be authored

- The player race (species design, rigged model, 16-cell expression sheets per named NPC).
- The hero's flight form and ground-combat form (kitbash from Space Kit mechs/ships, or Meshy).
- Space skybox / nebulae (procedural, generated in Python or shader).
- Warp-tunnel visuals (shader on a tube mesh).
- Laser swords (mesh + emissive + trail).
- HD-2D post stack (custom URP render feature: low-res target, tilt-shift DoF, bloom).
- Music (composer: Nathan).


September 26 lifecycle intake: `Art/Animation/HeroDeath.fbx` contains only the owned CC0 UAL1 skeleton/`Death01` take; `HeroRevive.fbx` contains only the owned CC0 UAL2 skeleton/`LayToIdle` take. Existing `UAL-License.txt`/`UAL2-License.txt` cover these exports. `tools/blender/prepare_animation_donor.py --set hero-death|hero-revive` reproduces them; source paths and zero-mesh reports are retained in `art-src/Donors/HeroDeath.json` and `HeroRevive.json`. No donor visible geometry is staged, and no Meshy/image credits or music changes are involved.

September 26 Sentinel recoil derivative: `Art/Animation/SentinelRecoil.anim` copies the owned UAL1 `Hit_Chest` take already staged as HeroLocomotion/Stagger, then authors its spine front/back curve for a visible masked torso response. `EnemyRecoilLayerSetup.Configure` reproduces the derivative and its upper-body mask. `UAL-License.txt` covers the source. The persistent armor cue reuses the staged Kenney `circle_02` particle material; it adds no new texture or geometry asset.
