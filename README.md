# LATTICE

A gamepad-first action RPG: fly Cinder Halo, shape for combat on Sorrel, and cross the Gullet with Taren and Sela.

Current status: **playable blockout; P1 species approval pending**. The final generated models and portraits are not present yet. See [progress](docs/PROGRESS.md) and the [species review packet](docs/art/SPECIES.md). No final release tag has been made.

## Run

Playable checkpoint: `Builds/Windows/Lattice.exe`. Select **New Game**, dock at Orrin's office, and follow the objective guide. Talk to Hal for supplies/fabrication and hail Neve before entering the Gullet.

The development package is `Builds/WindowsDev/Lattice.exe`; it also supports the arena and verification arguments below. Keep each EXE beside its `Lattice_Data` folder and Unity runtime files. Consult `docs/PROGRESS.md` for open gates.

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

Saves: `%USERPROFILE%/AppData/LocalLow/Nathan/Lattice/Saves`. Three manual slots plus autosave. Docking, warp and repair autosave; Party menu saves a manual slot. Continue lists all four. Automated tests use isolated save directories or the runner's save shield.

Report the scene, active character/form, action, expected result, actual result and reproduction steps. Include a screenshot, the affected save, and `%USERPROFILE%/AppData/LocalLow/Nathan/Lattice/Player.log`. Logs are replaced on the next player run.

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
```

Development player arguments: `-scene`, `-spawn`, `-loadout starter|moon|gullet`, `-route slice`, `-perf <report>`, `-screenshot <png>`. Runtime shortcuts are disabled in release builds. Gates require fresh tests, state assertions and opened screenshots; logs alone are insufficient.

All visible final art must be generated. [Credits](docs/CREDITS.md) record the permitted fonts, sounds and particle textures. Raw art, private credentials, build products and Unity caches are excluded from git.
