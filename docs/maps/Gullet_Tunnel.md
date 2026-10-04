# Gullet - living passage brief

2026-09-28 redesign (Claude Code). **Blockout MAP_EYE_TEST PASSED (agent review). Final in-player pass: PASS from the mouth through the salvage eddy** (both heroes' ordinary circuits, full focus, 2026-09-30, [evidence](../validation/C4-gullet-circuits05.md)). The nursery gate, coil and exit were traversed by the scripted smoke only; their ordinary pass remains OPEN.

The Gullet is the interior passage of an adult Choir that travellers use as a route. It should read as one organism's anatomy, in the order a traveller meets it, rather than a repeated tube. `Lattice.World.GulletProfile` holds the plan; the scene builder, smoke route, circuit generator (`tools/gullet_routes.py`) and layout tests all read it.

| z (m) | Section | Function and construction |
|---|---|---|
| -14 to 30 | **Mouth** | Opening to space, 34–42 m across, framed by heavy lip folds that flare outward. Arrival spawns inside the lip. |
| 30 to 110 | **Entry canal** | A 24 m muscular canal that curves slightly east. |
| 110 to 255 | **Feeding chamber** | Asymmetric belly up to 60 m, fuller on the left. Pod clusters grow from both walls where Chorister darts feed. Encounter `Gullet_Chamber_0`; drifters patrol, and mines drift deeper in the chamber. |
| 262 | **Valve 0** | An 18 m sphincter with angled lip folds on both sides. `Choir membrane 0` spans exactly the pinch and opens when the feeding chamber is cleared. |
| 270 to 485 | **Slalom throat** | A 26 m muscular throat. Tissue folds reach 7–8 m from alternating walls at 298, 330, 362 and 452 m, and the travel line weaves between them (at least 6.7 m clearance). Encounter `Gullet_Chamber_1`. |
| 390 to 432 | **Salvage eddy** | A right-hand pocket up to 44 m wide where the current slows. A lost skiff, cargo crates, Neve's cache and the chamber's mines have collected there. It is the throat's one roomy space. |
| 490 | **Valve 1** | Sphincter and `Choir membrane 1`. |
| 495 to 695 | **Nursery gate chamber** | A 52 m oval chamber lined with clustered, luminous egg pods, densest near the far gate; large gate pods flank the next valve. Encounter `Gullet_Chamber_2`, with drifters guarding. |
| 700 | **Valve 2** | Sphincter and `Choir membrane 2`. |
| 705 to 868 | **The Cantor's coil** | A 76 m chamber where the route serpent coils, wide enough for both ships and the boss to turn (encounter 66 × 58 m). Four Compact clamp plates bolted into the walls show where its navigation collar is anchored. |
| 868 to 905 | **Exit valve** | A 20 m aperture with lip folds and the warp to Tallow (`bossdown.Cantor`). |

**Rules visible in the scene:** the upper shell is cut away above y=4 for the camera, with the same rule throughout. Membranes close only at valves. Each organ keeps one generated membrane texture, tinted per section: warm feeding tissue, a dark muscular throat, a luminous nursery, a deep coil and a bright exit. The tint changes only at valves, where the sphincter hides the seam. Rib walls follow the curving wall at varied spacing and height.

**Identities preserved:** `Arrival` and `Performance` spawns, encounters `Gullet_Chamber_0..2` and `Gullet_Cantor`, `Choir membrane 0..2`, Neve's `SalvageField`, the `bossdown.Cantor` exit flag and the scene name. Saves record a zone and spawn ID, not a position, so any save in this zone restores at the unchanged `Arrival` or `Performance` spawn.

### Blockout review — September 28, Claude Code

Before (`Builds/quality/workshop/gullet-before-02`): one uniform 27 m sine-wave strip whose floor ends abruptly against space, with evenly alternating rib modules and three identical chambers. Nothing indicated what any part was for. **Rejected**, confirming Codex's September 26 finding.

After (`gullet-blockout-01..03`, the last being current): opened the full-length overhead and all ten section views.
- The overhead reads as anatomy: distinct bellies, pinched valves and the eddy pocket, from the mouth to the largest belly (the coil).
- The mouth flares with lip folds. The slalom folds visibly narrow the throat from alternating sides.
- The eddy's collected debris gives Neve's cache a believable place.
- Valve membranes now tuck behind the sphincter lips. Blockout 01 showed them poking out of the tube into empty space; that was corrected and is now tested.
- The nursery's egg clusters read as clusters rather than scattered buds. Blockout 01's small isolated pods were enlarged and grouped.

Still open or weak:
- The mouth's floor still ends in a clean arc against space.
- The coil's clamp plates are small against the chamber.
- The atlas's side pocket that previews the Cantor before combat is not built.
- The collar-release story change is not implemented.

**Blockout PASS; final pass pending** the ordinary flight review.

The layout checks (`GulletLayoutTests`) were **rejected on the old scene**: the travel line struck the old shell and ribs, and the membranes sat 47 m from any valve. They pass on the new scene.

### Gate, coil and exit traversal (October 3)

Codex: the whole mouth-to-TallowApproach route passes ordinary input, all three
chambers and Cantor cleared, 784.98 s, 47,093/47,093 frames focused. [Evidence](../validation/MAP-remaining-routes.md).
The replay's first attempt exposed and then regression-tested two recorder
defects. Opened gameplay views still reject the coil's framing: Cantor is often
above the screen at ordinary firing range and the chamber reads as floor.
Traversal is now proven; the final visual MAP_EYE_TEST remains OPEN.

### History

Codex's September 26 source inspection and rejection of the original repeated sinusoid (900 m, periodic 24–30 m width, 180 identical side modules and a 44 m Cantor trigger in a narrow passage) is superseded by this plan; the before images are retained under `Builds/quality/workshop/`.

### Ordinary circuits and the rib fix (September 30)

Claude Code. The first ordinary circuits on this anatomy exposed two construction defects the blockout review had missed:

- **Wall ribs in the passage.** Height-fitted rib slabs centred on the edge stood about 2 m inside the passage everywhere, and more at the eddy's flare.
- **Debris among the mines.** The eddy's debris lay among its mines.

Ribs are now pushed out until no point of their collision footprint stands more than 0.4 m inside the edge; the membrane shell is the wall and the ribs lap it from outside. The wreck and crates now settle at the back of the pocket. New layout tests sweep the whole passage and the eddy's fighting space. Both heroes' ordinary circuits then passed with full focus, and gameplay-scale stills from the passing run read as intended.
