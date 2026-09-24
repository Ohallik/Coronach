# CORONACH

A gamepad-first action RPG: fly Cinder Halo, shape for combat on Sorrel, and cross the Gullet with Taren and Sela.

The playable slice includes five zones, Taren and Sela, both bosses, fabrication, dialogue, saves and generated art throughout. The Coronach presentation pass adds native-resolution rendering without distance blur, separate walk/run and combat motions, two generated spacecraft forms and Nathan's music. There are now 50 production models and eight expression sheets. See [progress](docs/PROGRESS.md) for current verification and the remaining controller-feel checks. The original `slice-v0.1` tag is historical.

**Current quality review: not yet at the professional target.** Playing the opening and ground/flight encounters exposed input and partner-following defects, now corrected, plus unfinished environment composition, combat readability and menus. [Play review](docs/PLAY_REVIEW.md) records what was actually played, the fixes, remaining issues and limits of virtual-gamepad testing. The current build includes those corrections; the original tag's results above are historical.

The [Coronach presentation review](docs/PRESENTATION_REVIEW.md) records the latest town, combat and spacecraft checks, including the boss-gravity and companion-defense defects found during regression. [Animation direction](docs/ANIMATION_DIRECTION.md) distinguishes the implemented moveset from the remaining choreography work.

The game is named **Coronach** (formerly LATTICE). The existing Unity project is `Lattice/`; add that folder in Unity Hub and open `Assets/_Project/Scenes/_Boot.unity` to play in the Editor.

Full-game proposals: [story](docs/STORY_CAMPAIGN.md), [world and boss atlas](docs/WORLD_ATLAS.md), and [combat animation direction](docs/ANIMATION_DIRECTION.md). The story follows towns learning to travel as the living routes between worlds migrate. These documents plan the campaign; the playable slice remains the workshop for combat and presentation.

Current music: **Title Theme** on the title, **Hub Town Groove** in town/civil areas, **Moonbase Market** in shops, and **Adventure Awaits** in moon combat. Tracks crossfade and town music resumes when leaving a shop. All seven supplied originals remain in `Music/`; the other three have future uses in the atlas.

## Run

Run `Builds/Windows/Coronach.exe`. Select **New Game**, dock at Orrin's office, and follow the objective guide. Talk to Hal for supplies/fabrication and hail Neve before entering the Gullet. At Tallow Drift, dock, talk to the keeper, then use the repair console to save and finish.

The development package is `Builds/WindowsDev/Coronach.exe`; it also supports the arena and verification arguments below. Keep each EXE beside its `Coronach_Data` folder and Unity runtime files. Consult `docs/PROGRESS.md` for open gates.

## Controls

The connected Logitech Precision is supported through a DirectInput bridge. Its d-pad supplies movement. Button names below describe their gamepad positions; A is the bottom face button.

| Control | Ground | Flight | Safe zones |
|---|---|---|---|
| Stick / d-pad | Move | Thrust direction | Move / fly |
| A | Attack / nearby interaction | Fire / nearby interaction | Interact / advance |
| B | Dodge | Roll | Cancel menus |
| X | Guard | Blade lunge | — |
| Y | Swap | Swap | Swap |
| Hold RB + A/B/X/Y | Skills 1–4 | Skills 1–4 | — |
| LB | Lock-on | Lock-on | — |
| RT | Sprint | Boost | Sprint / boost |
| LT | Quick item | Brake | Brake in flight |
| D-pad up/down | Cycle item | Cycle item | — |
| D-pad left/right | Cycle target | Cycle target | — |
| Start | Pause menu | Pause menu | Pause menu |
| Select | Quest log | Quest log | Quest log |

**Precision:** hold **LB + d-pad** to cycle items or targets, so ordinary movement never changes the selection. Analog pads use the independent d-pad. In menus, LB/RB change tabs and B goes back. Mouse navigation also works.

Keyboard: WASD move, J attack/fire, Space dodge/roll, K guard/lunge, Tab swap, 1–4 skills, Q lock-on, Shift sprint/boost, F item/brake, E interact, Esc menu, M quest log, arrow keys cycle items/targets. Keyboard actions can be rebound in Settings.

## Saves and bug reports

Saves: `%USERPROFILE%/AppData/LocalLow/Nathan/Lattice/Saves`. This original save location is retained across the rename. Three manual slots plus autosave. Docking, warp and repair autosave; Party menu saves a manual slot. Continue lists all four. Automated tests use isolated save directories or the runner's save shield.

Report the scene, active character/form, action, expected result, actual result and reproduction steps. Include a screenshot, the affected save, and `%USERPROFILE%/AppData/LocalLow/Nathan/Coronach/Player.log`. Logs are replaced on the next player run.

## Build and verify

Unity 6000.4.7f1. Run only one Unity process on this project.

```powershell
powershell -ExecutionPolicy Bypass -File scripts/headless.ps1 verify
powershell -ExecutionPolicy Bypass -File scripts/headless.ps1 tests
powershell -ExecutionPolicy Bypass -File scripts/headless.ps1 playtests
powershell -ExecutionPolicy Bypass -File scripts/headless.ps1 builddev
powershell -ExecutionPolicy Bypass -File scripts/headless.ps1 build
powershell -ExecutionPolicy Bypass -File scripts/smoketest.ps1 -Dev -Scene Arena_Ground -RequirePad
powershell -ExecutionPolicy Bypass -File scripts/smoketest.ps1 -Dev -Scene Arena_Flight -RequirePad
powershell -ExecutionPolicy Bypass -File scripts/smoketest.ps1 -Dev -Route slice -RequirePad
powershell -ExecutionPolicy Bypass -File scripts/performance.ps1
powershell -ExecutionPolicy Bypass -File scripts/ui-smoke.ps1
powershell -ExecutionPolicy Bypass -File scripts/look.ps1
powershell -ExecutionPolicy Bypass -File scripts/balance.ps1
```

Development player arguments: `-scene`, `-spawn`, `-loadout starter|moon|gullet`, `-route slice`, `-perf <report>`, `-look <folder>`, `-balance <report>`, `-screenshot <png>`. Runtime shortcuts are disabled in release builds. Gates require fresh tests, state assertions and opened screenshots; logs alone are insufficient. Rebuild the generated world with `scripts/exec.ps1 -Method Lattice.EditorTools.WorldBuilder.BuildFinal -Marker WORLD_GENERATED_OK` before packaging scene changes.

All visible model, portrait, environment, texture and UI art is generated. [Credits](docs/CREDITS.md) record the permitted animation donors, fonts, sounds and particle textures. Meshy production used 795 of the 1,200-credit budget; 405 remain. Raw art, private credentials, build products and Unity caches are excluded from git.
