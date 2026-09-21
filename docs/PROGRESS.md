# LATTICE slice progress

## Current work

2026-09-20: P0_TOOLCHAIN_OK is committed and NATHAN_SPECIES_APPROVED is received. Downstream design and generated art production are underway, with more distinctive hero faces required by Nathan's feedback. Both Windows packages contain the complete blockout route from New Game through Tallow Drift. The latest route passed every ordered milestone in 555.5 seconds, ending at level 6 with completion saved. Latest suites: 35 EditMode and 15 PlayMode tests. Both arena smokes passed with the final enemy behaviors; menu/dialogue/shop captures were opened and corrected. Generated terrain, skies, planet maps and UI are in the scenes; models remain blockouts until intake. Meshy spending is zero at approval.

## Gates

| Gate | Status | Evidence / defect that must fail |
|---|---|---|
| P0_TOOLCHAIN_OK | CLOSED | `VERIFY_OK`, `BOOT_SCENES_OK`, `BUILD_OK`; 19/19 EditMode + 3/3 PlayMode; title smoke and inspected 1920x1080 screenshot; physical Logitech attachment in Player.log. |
| NATHAN_SPECIES_APPROVED | CLOSED | Nathan approved continuing, with more distinctive hero faces required. Exact response in `docs/art/species-approval.json` and D027. |
| P1_DESIGN_OK | IN PROGRESS | Hero/species packet inspected. NPC, enemy, ship and environment boards underway. |
| P2_CORE_OK | OPEN | 35 EditMode and 15 PlayMode tests, both arena smokes and the full blockout route passed; physical 60-second feel session remains. |
| P3_ART_OK | PENDING | Generated asset provenance, humanoid motion, poses and actual renders required. |
| P4_CONTENT_OK | OPEN | First full ordered route passed. Final generated art, look review and balance remain. |
| P5_PLAYABLE_OK | OPEN | Blockout performance and all-zone Continue pass. Final-art performance and physical controller feel remain. |
| P6_HANDOFF_OK | PENDING | Documentation, final build, credits and tag required. |

## NATHAN GATE

**NATHAN_SPECIES_APPROVED, 2026-09-20.** Nathan: "in general the designs of the race are fine. when we actually design the main characters they'll need to be more unique looking faces but that is okay for now. please continue". Approved review images:

- `C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/species-sheet-v2.png`
- `C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/species-faces-v2.png`
- `C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/taren-board.png`
- `C:/Users/natem/Projects/SpaceRPG/art-src/Generated/P1/refs/sela-board-v2.png`

The user-requested P1 gate is closed by the actual response above. Main-character facial distinctiveness is a production requirement. P2 code, generated UI, all five zone blockouts and the automated route were built and verified while waiting. Paid generation may now proceed within the 1,200-credit cap.

## BLOCKED

No current user-input blocker. Physical controller feel, final artwork, final balance and the remaining gates are not claimed complete. Continue autonomously; no second design approval is required.

## Art budget

Meshy limit: 1,200 credits. Spent by this project: 0. Ledger: `docs/art/gen-manifest.json`.

## Verification integrity

Synthetic controller tests demonstrate translation and navigation, not a human pad feel session. Physical attachment is checked in a graphics-enabled built player. Never describe unattended tests as hands-on play.

### P0 evidence

- Build: `Builds/Windows/Lattice.exe` (Unity 6000.4.7f1, URP 17.4).
- Compile/build logs: `Builds/logs/headless-exec-BatchTools-*.log`.
- At P0, fresh suites passed 19 EditMode and 3 PlayMode tests. The reusable `Builds/logs/editmode-results.xml` and `playmode-results.xml` paths now contain the expanded 35/15 suites below.
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
- PlayMode: 15/15 passed. Ground attack kill, flight projectile kill, measured 8 m dash, form/motor transitions, swap and timed revive, additive dock and flag-gated warp, Yarn line/options/bench command, short A taps during typewriter delays, all six menu tabs, analog lock-on/d-pad separation, actual Continue from all five zones, defeat/retry restoring living actors, Cantor segment motion/phases, pack lunge/Drifter volley behavior and existing pad/title tests.
- Test-discovered fixes: Json.NET now replaces initialized collections on load (avoids duplicate Taren); flight dash has a separate duration from cruise clamp; equipped gear/affixes feed runtime stats; one lunge shares its victim set along the path.
- Fresh arena smoke captures: `Builds/logs/Arena_Ground-screenshot.png` and `Arena_Flight-screenshot.png`, both opened after the final enemy behavior changes; three kills, swap, and Flash Move assertions passed, Logitech C21A attached. The flight log also confirms `LUNGE_KILL` and a Sela skill.
- Render review found bloom missing: main camera post-processing was disabled, renderer postProcessData was null, and volume components needed persistent subassets. All fixed in reproducible builders. The emissive cube visibly blooms in the inspected ground arena; ordinary geometry does not.
- Physical 60-second hands-on controller feel evidence is not available yet. Automated input and attached hardware do not close that requirement.
- Generated UI art (panel, button, slider/reticle, sixteen items, eight skills) is archived with prompts under `art-src/Generated/P2/refs/`, provenance in `docs/art/image-manifest.json`.
- Implementation checkpoint committed as `70acfa9` (`P2_CORE_CHECKPOINT`). This deliberately does not claim the physical-feel gate is closed.
- Retry regression fix committed as `29eb6d1`. The test failed before the fix (`Builds/logs/retry-regression-red.xml`) and the complete 14-test PlayMode suite passed afterward.
- Final checkpoint packages include the new lunge/dive/volley behaviors. Release title smoke passed with `TITLE_BOOT_OK`, `TITLE_SMOKE_OK` and physical Logitech attachment; its second 1080p screenshot was opened. Development arena and full-route verification also passed. The current code/docs are committed as a further `P2_CORE_CHECKPOINT`; this does not close the physical-feel or final-art gates.

### Route, look and performance evidence

- Latest complete route: `Builds/logs/route-enemy-patterns-passed.log`, every ordered milestone accepted by `ROUTE_MARKER_CONTRACT_OK`, `SLICE_SMOKE_OK elapsed=555.5 level=6`. Burrower took 75.9 s and Cantor 100.4 s. `Builds/logs/completed-enemy-patterns-save.json` contains both heroes at level 6, no crafted equipment, and `sliceComplete=true` in Tallow Drift. The final completion screenshot was opened. Route uses real movement, damage, NPC interactions, key pickup, gates and saves, with no flag/kill injection; this is an automated regression duration, not a human playtime claim.
- Historical routes: the first 395.2 s run reached level 7 before XP tuning; its reusable log was superseded. `route-pre-aim-fix-passed.log` completed in 604.3 s at level 6. `route-aim-corrected-passed.log` completed in 549.9 s at level 6, with 64.7/99.5 s boss fights. Their completion saves are archived alongside the logs. The final run above includes the new pack lunge, Dart dive and Drifter volley.
- Preserved rejected runs: `Builds/logs/route-combat-timeout-rejected.log` exposed a travel timer that counted nested combat; `burrower-overlong-rejected.log` rejected the 30,000-Integrity Burrower experiment. Both informed fixes before the completed runs above. Balance and physical feel remain open.
- Five reproducible builders create Halo traffic/docks, the Decks, twelve Sorrel encounters and six mines, a 900 m Gullet with four chambers, and Tallow Drift. Yarn first/repeat/post lines and twelve ambient barks are present.
- All fifteen initial zone screenshots in `Builds/logs/zones/` were opened. Halo/Decks/Sorrel/Tallow show generated texture/UI work on obvious blockouts. Gullet screenshots exposed outward-facing shell triangles; fixed winding, and the updated interior is visible in `Builds/logs/perf-gullet.png` (opened). None of these close a final-art LOOK gate.
- `Builds/logs/perf-gullet.json`: final checkpoint run 30.019 s, 28,459 samples, at least twelve enemies, 1920x1080, RTX 5070; 948.0 fps mean, 1.384 ms p95, 1.595 ms p99. The second screenshot was opened and shows live attacks. This is a blockout performance baseline, not a final-model performance claim. Fresh report/screenshot and thresholds are required by `scripts/performance.ps1`.
- Registered interaction prompts replace per-frame scene scans. Menus paginate inventory/shop stock; late-accepted crystal quests use collection history; pause has priority over Flash slow motion.
- `Builds/logs/ui/`: eight second-capture 1080p renders opened (dialogue, six menu tabs, shop). Review fixed dialogue hint placement, row margins, atlas crop leakage and gameplay HUD showing through menus. Portrait space remains empty pending generated portrait intake.
- New Meshy intake code fails empty batches, missing albedo, non-human avatars, and stationary walk bones. It is compiled but has not processed a generated model yet. Portrait pipeline now preserves all sixteen emotions, requires actual 4x Real-ESRGAN output, and imports 2304x2304 sheets with sixteen full 576x576 rects.

### ART_PENDING / unfinished gates

- ART_PENDING: Taren and Sela Natural/Shaped/Flight models; all civilian bodies/retextures; eight enemies including Cantor head/body/tail; three traffic ship classes; the full static environment set. P1 approval is received; downstream designs and production are underway.
- ART_PENDING: eight generated portrait grids and their dialogue-box render review. No temporary downloaded faces are used.
- Texture/skies are generated at the image tool's native returned sizes (tiles 1254 square; panoramas/maps 1774x887), not the 4096x2048 requested in prompts. Provenance records the actual dimensions. Final look review remains open.
- Main-route progression to level 6 is verified. The T2 comparison, broader balance, release-route physical pad feel and final art performance remain open.
- Tallow Drift currently loads its ground deck directly; the separate SpaceSafe docking approach remains unfinished. The Gullet membrane texture is visually busy and needs the final environment look pass. All model and portrait renders remain required.
- No `slice-v0.1` tag has been created. P6 requires the finished slice, not this checkpoint.
