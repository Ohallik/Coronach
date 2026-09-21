# LATTICE slice progress

## Current work

2026-09-20: P0_TOOLCHAIN_OK is committed. P1 species and hero packet is awaiting Nathan. The development and release packages now contain the complete blockout route from New Game through Tallow Drift. The first complete route smoke passed every ordered milestone in 395.2 seconds, ending at level 7; the XP curve and bosses are being tuned. Latest completed checks: 35 EditMode and 13 PlayMode tests, including actual Continue from every zone and short pad taps through Yarn dialogue. All eight menu/dialogue/shop captures were opened and corrected. Generated terrain, skies, planet maps and UI are in the scenes; character, enemy, ship and environment models remain blockouts. Meshy spending is zero. A fresh full route and final packaging check are in progress.

## Gates

| Gate | Status | Evidence / defect that must fail |
|---|---|---|
| P0_TOOLCHAIN_OK | CLOSED | `VERIFY_OK`, `BOOT_SCENES_OK`, `BUILD_OK`; 19/19 EditMode + 3/3 PlayMode; title smoke and inspected 1920x1080 screenshot; physical Logitech attachment in Player.log. |
| NATHAN_SPECIES_APPROVED | AWAITING NATHAN | Review packet `docs/art/SPECIES.md`; four final generated images below. P2 continues. |
| P1_DESIGN_OK | PENDING | All design references inspected; no fused limbs, gloss, detached components. |
| P2_CORE_OK | OPEN | Tests and arena smokes passed; current regression pass and physical 60-second feel session remain. |
| P3_ART_OK | PENDING | Generated asset provenance, humanoid motion, poses and actual renders required. |
| P4_CONTENT_OK | OPEN | First full ordered route passed. Final generated art, look review and balance remain. |
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

P1 dependent work: Nathan's species/hero approval is pending. No paid Meshy requests have been made. Independent core systems, UI art, and zone blockout/content work can continue.

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

Double-click `Builds/Windows/Lattice.exe` for the release blockout or `Builds/WindowsDev/Lattice.exe` for development probes. Arena boots in the development player use `-scene Arena_Ground` or `-scene Arena_Flight`. Title uses d-pad + bottom face button (A), B returns from settings. Both packages contain all five zones; these are playable checkpoints, not the final generated-art release.

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/headless.ps1 verify|tests|playtests|build`

`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/smoketest.ps1 -RequirePad`

### P2 evidence in progress (gate still open)

- EditMode: 35/35 passed, no skips, fresh fail-closed XML. Includes damage, break, levels, equipment/affixes, atomic crafting/inventory, quests, save slots and corruption, pause/Flash arbitration, late quest acceptance and pad translation.
- PlayMode: 14/14 passed. Ground attack kill, flight projectile kill, measured 8 m dash, form/motor transitions, swap and timed revive, additive dock and flag-gated warp, Yarn line/options/bench command, short A taps during typewriter delays, all six menu tabs, analog lock-on/d-pad separation, actual Continue from all five zones, defeat/retry restoring living actors, Cantor segment motion/phases and existing pad/title tests.
- Test-discovered fixes: Json.NET now replaces initialized collections on load (avoids duplicate Taren); flight dash has a separate duration from cruise clamp; equipped gear/affixes feed runtime stats; one lunge shares its victim set along the path.
- Fresh arena smoke captures: `Builds/logs/Arena_Ground-screenshot.png` and `Arena_Flight-screenshot.png`, both opened after the clock and UI fixes; three kills, swap, and Flash Move assertions passed, Logitech C21A attached.
- Render review found bloom missing: main camera post-processing was disabled, renderer postProcessData was null, and volume components needed persistent subassets. All fixed in reproducible builders. The emissive cube visibly blooms in the inspected ground arena; ordinary geometry does not.
- Physical 60-second hands-on controller feel evidence is not available yet. Automated input and attached hardware do not close that requirement.
- Generated UI art (panel, button, slider/reticle, sixteen items, eight skills) is archived with prompts under `art-src/Generated/P2/refs/`, provenance in `docs/art/image-manifest.json`.
- Implementation checkpoint committed as `70acfa9` (`P2_CORE_CHECKPOINT`). This deliberately does not claim the physical-feel gate is closed.

### Route, look and performance evidence

- Historical first route: the wrapper emitted `ROUTE_MARKER_CONTRACT_OK` after every required marker in order; runtime ended `SLICE_SMOKE_OK elapsed=395.2 level=7`, `SLICE_COMPLETE`, and a saved completion flag. Later runs supersede the reusable log path. The newest failed run is archived as `Builds/logs/route-combat-timeout-rejected.log`; its completed 97.8-second Cantor fight exposed a travel timer counting nested combat. Fresh route verification follows the fix. Route uses real movement, damage, NPC interactions, key pickup, gates and saves, with no flag/kill injection.
- Five reproducible builders create Halo traffic/docks, the Decks, twelve Sorrel encounters and six mines, a 900 m Gullet with four chambers, and Tallow Drift. Yarn first/repeat/post lines and twelve ambient barks are present.
- All fifteen initial zone screenshots in `Builds/logs/zones/` were opened. Halo/Decks/Sorrel/Tallow show generated texture/UI work on obvious blockouts. Gullet screenshots exposed outward-facing shell triangles; fixed winding, and the updated interior is visible in `Builds/logs/perf-gullet.png` (opened). None of these close a final-art LOOK gate.
- `Builds/logs/perf-gullet.json`: 30.018 s, 30,370 samples, at least twelve enemies, 1920x1080, RTX 5070; 1,011.7 fps mean, 1.244 ms p95. This is a blockout performance baseline, not a final-model performance claim. Fresh report/screenshot and thresholds are required by `scripts/performance.ps1`.
- Registered interaction prompts replace per-frame scene scans. Menus paginate inventory/shop stock; late-accepted crystal quests use collection history; pause has priority over Flash slow motion.
- `Builds/logs/ui/`: eight second-capture 1080p renders opened (dialogue, six menu tabs, shop). Review fixed dialogue hint placement, row margins, atlas crop leakage and gameplay HUD showing through menus. Portrait space remains empty pending generated portrait intake.
- New Meshy intake code fails empty batches, missing albedo, non-human avatars, and stationary walk bones. It is compiled but has not processed a generated model yet. Portrait pipeline now preserves all sixteen emotions, requires actual 4x Real-ESRGAN output, and imports 2304x2304 sheets with sixteen full 576x576 rects.

### ART_PENDING / unfinished gates

- ART_PENDING: Taren and Sela Natural/Shaped/Flight models; all civilian bodies/retextures; eight enemies including Cantor head/body/tail; three traffic ship classes; the full static environment set. Waiting on P1 before downstream designs and paid generation.
- ART_PENDING: eight generated portrait grids and their dialogue-box render review. No temporary downloaded faces are used.
- Texture/skies are generated at the image tool's native returned sizes (tiles 1254 square; panoramas/maps 1774x887), not the 4096x2048 requested in prompts. Provenance records the actual dimensions. Final look review remains open.
- Boss durations, T2 comparison, final level-6 curve, release-route physical pad feel and final art performance are not closed.
- No `slice-v0.1` tag has been created. P6 requires the finished slice, not this checkpoint.
