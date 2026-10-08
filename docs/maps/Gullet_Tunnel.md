# Gullet - living passage brief

2026-09-28 redesign (Claude Code). **Blockout MAP_EYE_TEST PASSED (agent review). Final in-player pass: PASS from the mouth through the salvage eddy** (both heroes' ordinary circuits, full focus, 2026-09-30, [evidence](../validation/C4-gullet-circuits05.md)). The nursery gate, coil and exit were traversed by the scripted smoke only; their ordinary pass remains OPEN.

The Gullet is the interior passage of an adult Choir that travellers use as a route. It should read as one organism's anatomy, in the order a traveller meets it, rather than a repeated tube. `Lattice.Data.GulletProfile` holds the plan; the scene builder, smoke route, circuit generator (`tools/gullet_routes.py`) and layout tests all read it.

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
- The atlas's side pocket now has a playable dormant preview; its quiet composition remains rejected below.
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

### Encounter framing recapture (October 3, later)

D134 fits the active ship and nearby locked target while retaining the flight
camera's angle. The complete ordinary route passes again in 783.852 s with
47,025/47,025 focused frames. Opened views 099/107/117 now show Cantor's full
segmented body and both ships; 155/157 show travel framing and the exit restored.
The off-screen combat defect is resolved. The broad uniform coil floor and small
clamp plates remain weak; this is **not** full final MAP_EYE_TEST acceptance.
[Tests, capture and limitations](../validation/C4-flight-framing.md).

### Collar gameplay blockout — October 8

The route profile, walls, moorings, encounter volume and exit identity remain fixed during this actor/mechanics pass. The original shared animal-health fight does not implement the atlas's independent collar links. Three targetable restraints now occupy separate moving body joints; the head lock opens only after all three are severed. The four damage pools share the original definition's total integrity budget. This preserves the budget, not an assumed fight duration: ordinary earned equipment and input still have to prove balance.

| Restraint | Moving attachment | Consequence of cutting it |
|---|---|---|
| Sweep link | First body segment | The marked sweep's damaging radius contracts from 8 m to 5 m. |
| Chorus link | Third body segment | Song radius contracts from 14 m to 10 m; its hit loses the stagger and is weaker. |
| Volley link | Fifth body segment | The emitted three-shot fan becomes one shot. |
| Final lock | Head collar | The existing bounded animal release, once-only rewards and saved exit flag resolve. |

After a cut, later phases alternate the marked sweep and song. Target cycling names each link, and the objective reports link progress before directing the final lock release. Shared break reactions must still interrupt the carrier's actual tell. The camera must fit the whole animal while a link is selected. Severed equipment separates into its own visual lifetime; it must not be mistaken for anatomy that must travel through the exit. Recovery and scene removal must reset or dispose of every owned part.

The completed recovery handoff restores fresh link collision after the presentation's death-time snapshot. A stationary carrier retains its restored chain pose until it moves or turns; then the body and fresh attack timers resume. Inactive owners hide all equipment roots without resetting cut state or debris age. Delayed collision shutdown, frozen AI, the first repair's changed pose, a deliberately frozen chain and inactive-owner visibility all have retained rejecting checks; the old release geometry/pose assertions stay intact.

The visible segmented bands are **blockout geometry, not accepted final art**. Final intake should match the inspected Compact mooring's pale alloy, ochre edging and restrained status light, with an unmistakable separation between restraint and living resonator. Prove the blockout and ordinary combat first, then generate the reusable split collar module. No new paid art has been submitted in this pass. Live Meshy balance observed October 8: **2,413 credits**; the difference from the earlier 2,778 is unattributed, not an invented project expense.

Partial encounter damage is not a new persistent save state; an unfinished fight resets under the existing encounter-checkpoint rules. Completed `bossdown.Cantor` / `clear.Gullet_Cantor` saves retain access without a second fight or fictional new link history. The pre-combat side pocket, collar art, approach/release dialogue and final ordinary MAP_EYE_TEST remain **OPEN**. The earlier map blockout pass does not accept this new combat presentation.

The new earned route passes with all three links cut and the lock released: 801.840 seconds, full recorded focus, awake display endpoints and VSync 1. Both living heroes reach TallowApproach. Thirteen opened frames make the blockout bands/objectives legible, but **Sela is partly clipped during the release at +1.50/+1.80**. Keep that framing rejection open and fix it before accepting presentation. Selected companion/body overlap and repeated miss text also need diagnosis. Final map quality, continuous motion/audio and physical feel remain unaccepted. [Exact evidence](../validation/C3-cantor-collar.md).

### Companion framing follow-up — October 8

The changed earned route passes in 797.269 seconds with all 47,815 samples focused. Ten opened frames show both ships inside the sampled release/return frame, including +1.50/+1.80; the nearby companion now contributes to camera bounds. Cantor is whole through selected +1.50 and starts cropping by +1.80 after the bounded hold. **Sela still overlaps the animal in earlier combat stills**, so final presentation remains unaccepted. The repeated line observed previously is a successful Flash-dodge bark, not proof of missed shots. Displays off/VSync 0 and the retained 71.915 ms step-44 hitch exclude timing acceptance. All source/package bindings, failures, preserved saves and observation limits are in [the companion report](../validation/C4-companion-framing.md). Final map/art/preview/story and continuous quality remain OPEN.

### Companion spacing and explicit reactive input — October 8

The companion now takes a lateral firing position beside the selected segment and turns its actual nose toward it. Six controlled cases verify clearance, settled hits and flight altitude. The unchanged earned input subsequently **fails** after two links and both heroes down; preserve that 905.666-second capture. A separate explicitly reactive-input route retains every original step/assertion/duration/button and passes in 855.939 seconds, cutting all links and saving both living heroes at TallowApproach. Its 150.6-second fight does not accept balance. Eleven opened frames show improved companion separation and retained ship framing, with the whole animal through +1.50 and top cropping by +1.80. Real-time evasion and Flash-text arbitration repairs, all failures and source/package bindings are in [the spacing report](../validation/C4-companion-spacing.md). Display-off VSync-0 capture accepts no C1 presentation; continuous audiovisual/physical quality, final collar art, preview/story and final map review remain OPEN.

### Generated collar candidate — October 8

The blockout plates are replaced by one 950-triangle Compact module with outward-facing pale alloy, ochre edge and restrained cyan pigment. Existing link positions, collision, integrity, exit identities and 1.2-second debris lifetime remain. The earned route passes in 870.855 seconds with all three cuts and both living heroes at TallowApproach. Twelve opened frames show equipment separated from anatomy and detached release debris, but detail is small/dark and **Sela overlaps the animal in some combat samples**. The 167-second fight, 82.560/49.736 ms engagement hitches, final art, preview/story and continuous quality remain unaccepted. Exact source/build/art/failure records are in [the collar-art report](../validation/C8-collar-art.md). One 15-credit task leaves 2,398 live Meshy credits. No final MAP_EYE_TEST acceptance is claimed.

### Articulated travel follow-up - October 8

World-space chain following and bounded physical steering replace the earlier reversed neck; companion paths and rolls clear the measured animal/party hulls and choose a firing line to the selected restraint. The final 66 affected checks and nine deliberate faults are in [the travel report](../validation/C4-companion-travel.md). The 784.573-second earned capture reaches TallowApproach; eleven opened stills show broader curves and clear, framed ships. This does not accept continuous quality, the 73.4-second fight or final MAP_EYE_TEST. A complete damage trace confirms Sela's two earlier wall contacts near z363 and z391; diagnose that actual traversal next. Dormant preview, side pocket, contextual release/Tallow story, continuous audiovisual/physical observation and final map acceptance remain OPEN/UNVERIFIED. No profile, geometry or player-input change is part of this checkpoint.

### Companion wall guidance follow-up - October 8

Actual shell and far-side fold fixtures reproduce 12-integrity companion impacts. Bounded scene-collision guidance repairs those cases without changing this map or player controls: 91 affected passes, eight rejecting faults, and an earned 782.625-second route with no wall-contact logs or Sela damage. Fifteen opened Gullet images show selected clear traversal and broad animal curves; full motion and final MAP_EYE_TEST remain unaccepted. Early Taren damage increases on a changed path and stays an open balance/provenance concern. Release framing still ends after the bounded hold, and the generic bark crosses the body. Dormant preview, side pocket and contextual release/Tallow story remain unfinished. [Evidence](../validation/C4-companion-walls.md).

### Dormant preview shoulder - October 8, functional checkpoint; composition open

The actual Cantor and its equipment now wait inside the existing coil. A viewing point at (23,1,768) sits before the original encounter boundary, with a 3.5 m interaction/camera-interest radius. Two short generated tissue lips at z=756 and z=777 run from x=29 into the existing right shell. Their roots meet the bed and their tops retain the existing four-metre cutaway convention. The central flight route, original 66 by 58 m encounter, spawn identities, shell profile and exit remain unchanged.

```text
NORTH / existing coil: actual animal, links and four existing moorings
       original activation boundary; neither hero enters it from the pocket
central lane         open shoulder             existing right shell
     |          point (23,768), radius 3.5      lip x=29..shell at z=777
     |              ships face the coil        open shell-side recess
     |                                         lip x=29..shell at z=756
SOUTH / existing approach at (0,744) and old pre-combat stop near (0,755)
```

The optional six-line exchange identifies the rings, head lock and wall grooves, without granting rewards or inventing a nursery visit for legacy saves. Combat remains possible without talking. A completed observation directs the player back toward the collar; activation consumes the same animal and links, restores each original collision/damage state and starts AI/music. Completed encounter saves do not prepare an absent animal. Preview camera interest stays in this pocket and yields to actual targets or the bounded release introduction.

Codex opened all four unlabelled `cantor-preview/map01` construction images and all four corrected `map02` images. The first fixed shoulder fixture cropped a ship, and the entry fixture left both ships behind; those rejected images and source remain. Geometry-fitted review framing corrects the former, and moving the entry fixtures corrects the latter. The revised overhead/section show a clear central lane, open approach, separated ships and lips rooted into the shell slope. The pocket remains visually subtle and the pre-Start animal is straight. The first player failed after two links and remains preserved. The repaired held-input player passes the identical route in 842.516 seconds, three cuts and both living heroes at TallowApproach. Seventeen new stills were opened: the quiet preview still fails final composition because the animal/links are small beside four dominant moorings; selected +1.50 release imagery retains a possible Sela/body silhouette overlap. The optional conversation is readable in sampled lines, but continuous quality remains UNVERIFIED. These editor views are not a runtime camera or final MAP_EYE_TEST pass. [Current implementation evidence](../validation/C8-cantor-preview.md).

### Earlier map history

Codex's September 26 source inspection and rejection of the original repeated sinusoid (900 m, periodic 24–30 m width, 180 identical side modules and a 44 m Cantor trigger in a narrow passage) is superseded by this plan; the before images are retained under `Builds/quality/workshop/`.

### Ordinary circuits and the rib fix (September 30)

Claude Code. The first ordinary circuits on this anatomy exposed two construction defects the blockout review had missed:

- **Wall ribs in the passage.** Height-fitted rib slabs centred on the edge stood about 2 m inside the passage everywhere, and more at the eddy's flare.
- **Debris among the mines.** The eddy's debris lay among its mines.

Ribs are now pushed out until no point of their collision footprint stands more than 0.4 m inside the edge; the membrane shell is the wall and the ribs lap it from outside. The wreck and crates now settle at the back of the pocket. New layout tests sweep the whole passage and the eddy's fighting space. Both heroes' ordinary circuits then passed with full focus, and gameplay-scale stills from the passing run read as intended.
