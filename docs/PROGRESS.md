# LATTICE slice progress

## Current work

2026-09-20: P0_TOOLCHAIN_OK. Unity Windows title build exists and its rendered screenshot was opened and inspected. P1 design images underway; P2 is next. New Game currently enters an empty destination blockout; this is a toolchain build, not yet the playable slice.

## Gates

| Gate | Status | Evidence / defect that must fail |
|---|---|---|
| P0_TOOLCHAIN_OK | CLOSED | `VERIFY_OK`, `BOOT_SCENES_OK`, `BUILD_OK`; 19/19 EditMode + 3/3 PlayMode; title smoke and inspected 1920x1080 screenshot; physical Logitech attachment in Player.log. |
| NATHAN_SPECIES_APPROVED | AWAITING NATHAN | Review packet `docs/art/SPECIES.md`; four final generated images below. P2 continues. |
| P1_DESIGN_OK | PENDING | All design references inspected; no fused limbs, gloss, detached components. |
| P2_CORE_OK | PENDING | Fresh nonempty passing test XML, arena kill smokes and inspected renders required. |
| P3_ART_OK | PENDING | Generated asset provenance, humanoid motion, poses and actual renders required. |
| P4_CONTENT_OK | PENDING | Route milestones must reflect gameplay state, in order. |
| P5_PLAYABLE_OK | PENDING | Performance, controller feel and save/continue evidence required. |
| P6_HANDOFF_OK | PENDING | Documentation, final build, credits and tag required. |

## NATHAN GATE

**P1 approval requested 2026-09-20:** approve or redirect the Vael species, Taren and Sela. Review `docs/art/SPECIES.md` with embedded images:

- `C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/species-sheet-v2.png`
- `C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/species-faces-v2.png`
- `C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/taren-board.png`
- `C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/sela-board-v2.png`

This is the user-requested gate in `docs/SLICE_PROMPT.md` P1. No approval is inferred from silence. No Vael Meshy credits may be spent until Nathan responds; P2 code and blockout work continues now.

## BLOCKED

None confirmed.

## Art budget

Meshy limit: 1,200 credits. Spent by this project: 0. Ledger: `docs/art/gen-manifest.json`.

## Verification integrity

Synthetic controller tests demonstrate translation and navigation, not a human pad feel session. Physical attachment is checked in a graphics-enabled built player. Never describe unattended tests as hands-on play.

### P0 evidence

- Build: `Builds/Windows/Lattice.exe` (Unity 6000.4.7f1, URP 17.4).
- Compile/build logs: `Builds/logs/headless-exec-BatchTools-*.log`.
- Fresh tests: `Builds/logs/editmode-results.xml` (19 passed), `Builds/logs/playmode-results.xml` (3 passed).
- Physical hardware: `%USERPROFILE%/AppData/LocalLow/Nathan/Lattice/Player.log` and copied `Builds/logs/Title-player.log`: `PAD_BRIDGE_ATTACH vid=046D pid=C21A profile=LogitechPrecision`. Graphics player ran for more than ten seconds.
- Render: `Builds/logs/Title-screenshot.png`, second capture, visually inspected: title, four menu rows and generated Cinder Halo key art visible. First hidden-window attempt was black and rejected; interactive players now launch visibly.
- Test instrument: `headless.ps1 testcontract` accepted one valid fixture and rejected eight defects (missing, stale, stale payload, malformed, failure, hidden child error, incomplete, zero tests).
- Input instrument: synthetic Joystick states prove A submission, X separation, d-pad translation, Start, disconnect, title navigation, disabled Continue skip, settings confirm/cancel and focus restoration.
- Blender 5.1: `BLENDER_TOOLCHAIN_OK`; Meshy bridge payload contract test passes. Meshy credentials copied privately; zero paid requests.
- `.gitignore` verified for art-src, Builds, Library and tools/meshy/.env.

## How to run and verify (current)

Double-click `Builds/Windows/Lattice.exe`. Title menu uses d-pad + bottom face button (A), B returns from settings. P0 gameplay destinations are empty until P2 builders land.

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/headless.ps1 verify|tests|playtests|build`

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/smoketest.ps1 -RequirePad`
