# Sorrel Ridges - working landscape brief

2026-09-26. Source inspection by Codex. Blockout and final MAP_EYE_TEST **OPEN**. This records the built workshop's spatial problems before its next geometry pass; the larger atlas chapter is not implemented here.

Sorrel is a small inhabited extraction outpost beside exposed mineral ridges. Workers need a landing/receiving area, sheltered habitation, repair and stores, then access to the drill site. Ridgehounds occupy the rock cover, scrapmites follow discarded equipment and crystals occur in exposed seams. The drill disturbance is the cause of the journey toward the nursery, not an unrelated arena at the end of a corridor.

```text
Current workshop, north up (metres)
   z 201  outer launch / recovered key
     181  drill equipment
 153-191  sealed Burrower clearing
     140  fabrication anvil
          west route | centre | east route
  45-129  four repeated encounter rows per route
          two roughly parallel rock spines
      18  hab / Survivor / hab
      16  repair and safe pocket
     -18  skiff, supplies and landing
          x -70 -------------------- +70
```

The current continuous floor is 140 by 230 m. Three lanes at x=-32/0/32 run through four repeated encounter rows. The broad, straight boss seals span 125 m. Those dimensions come from `SorrelBuilder`; they do not establish a natural formation or a convincing outpost. The safe pocket has no presented receiving/habitation/service plan. The anvil sits on the approach to the drill but its support and operator access need an actual view. Invisible boundary boxes need a visible terrain reason from ordinary cameras.

The next blockout should trace a worker/haul route from landing to stores and drill, and one exploratory loop through mineral seams that rejoins it. Let ridge strata and excavated spoil determine the route bends and the boss clearing. Leave a clear return path to repair. Preserve Arrival/Outpost, mine-node IDs, encounter IDs and saved completion flags, adapting route coordinates only after the space works. Do not expand to the atlas's final scale merely to add empty walking.

Required evidence: unlabelled arrival, hab service side, each route junction, seam/creature relationship, drill clearing and both launch points; overhead terrain continuity; continuous ordinary traversal including the return. `MapEvidence.WorkshopOthers` provides the pending baseline views. No new spatial pass is claimed from this brief.


### Actual baseline image review

Codex, September 26: Opened the full overhead: an exposed rectangular ground sheet, repeated rocks along one edge, sparse regularly spaced obstacles and no convincing terrain enclosure or connected settlement. This fails natural formation and inhabited route logic. Evidence: `Builds/quality/workshop/maps-before-01/`. No blockout/final acceptance is claimed.


### Isolated redesign blockout in progress

September 26, Codex. Raw iterations are preserved under `Builds/quality/workshop/sorrel-blockout-01` through `06`. No blockout or final pass is claimed yet. The rebuild preserves the scene GUID, Arrival/Outpost names, all twelve exploration encounter IDs, six mine IDs, boss ID and warp-key state. It does not regenerate other zones, definitions or dialogue.

```text
Proposed functional plan, north up; coordinates in metres
  201            key / outer launch, beyond the drill cradle seal
  191      cliff |============== 26 m seal ==============| cliff
  180            drill on footing, clear operator/haul side
  164            excavated Burrower space
  153      cliff |============== 26 m entry =============| cliff
  141            maintenance anvil beside the haul approach
                 /                |                 \
  35-129  west seam loop    bending haul wash    equipment service branch
          ore / rock cover        |              discarded equipment
                 \                |                 /
   18      hab -- entrance -- common court -- entrance -- hab
    7      repair work pad         |                    parts/stores
  -11                             |                    receiving apron
  -18      parked skiff       arrival / launch
```

The terrain is a continuous basin with rounded hollows, low foreground rim and higher rear strata. The main graded route is approximately 12 m wide, side trails 10 m; rock cover and clearings vary along the bends. The outpost's 8 by 8 m hab envelopes have 2.3 m entrance markers and 3 m approaches. Receiving, stores and repair pads leave separate standing/handling space. The quiet common court faces both hab entrances and the Survivor. Hab interiors are implied, not playable rooms. The excavation is roughly 26-34 m wide, with the drill on a footing along its west side and an unobstructed central work area. Its energy seals terminate in visible cliffs.

Opened images rejected the first blockout's high foreground occlusion and rectangular tray. The second exposed the common flat material obscuring boxes and the remaining rectangular outpost cut. Iteration 03 gives the habs distinct envelopes and entrances, separates the crates from their pad material, rounds the outpost hollow and varies the surrounding strata. Opened unlabelled views show receiving/storage beside the landing, operator space beside repair, inward-facing hab access, branching ridges and the drill recess. Iteration 04 retains those adjacencies while restoring an unbroken rim: the earlier rim merged into a low hill, allowing a 400-plus-metre path around the closed drill seal. The unchanged closure assertion rejected it. The repaired rim passes closed/open/bidirectional-return/reclosed navigation checks. Its arrival, overhead and excavation images were opened again; the landing remains visible.

A separate red regression proved that opening a membrane removed physical collision but left a carved navigation obstacle. `Membrane.SetOpen` now changes both. The red and green test results and exact candidate sources are retained. The older failed destination-at-key-centre fixture is explicitly separate from this reproduced defect.

The actual generated-prop audit now establishes the hab door on +Z and a 5.673 m footprint; final inward yaw is prepared. Iterations 05/06 raise buried launch pads and grade the northern apron. Opened 06 arrival, north-return and excavation-section views show those corrections. The closed barrier also resets correctly if its opening animation is interrupted. Ordinary outpost 01 passes; western 02 clears all four packs without a downed hero but remains rejected for focus loss during return.

Still required: finish ordinary blockout traversal, resolve final equipment supports/operator access, convert and inspect final art, traverse the final routes and return in the player, then rerun integrated regressions. The blockout route uses ordinary virtual gamepad input with the companion active and isolated saves; navmesh reachability alone is not a full ordinary traversal.

### Hushwell bore (September 28)

Claude Code. The drill site now leads down into [Hushwell](Hushwell.md), the moon cave the drill broke into. The miners' scaffold east of the drill, at (8, 177) inside the Burrower's excavation, is gated on `bossdown.Burrower`, and a `Hushwell` spawn at (8, 172) receives parties returning by the breach climb or the nursery lift. Sorrel was rebuilt with `SorrelRedesign.Blockout`, matching its committed blockout state; nothing else in its layout changed, and its terrain asset is byte-identical. `HushwellTests` covers the bore's gate and target.

### Final art conversion (September 29)

Claude Code. `SorrelRedesign.Final` now builds the connected basin with the generated kit in place of the blockout stand-ins. Nothing moves; only the pieces' own measured bounds replace the grey envelopes. The route bake still passes (`SORREL_ROUTE_BAKE_OK points=25`: every haul, seam and service point, plus reachable stances at all six ore nodes).

Controlled views in `Builds/quality/workshop/sorrel-final-01` were opened:

- **Outpost.** It reads as an inhabited outpost: domed habs face inward on their footings, with a common court and the Survivor between them. The receiving apron and stores sit beside the landing pad and parked skiff, and the repair pad has its console.
- **Wilds.** Ridge strata, cover rocks and cyan ore line the western seam and the service branch.
- **Drill site.** The excavation reads as a cut with cliffs on both sides. Both seals end in those cliffs, the drill stands on its footing, and the Hushwell scaffold is readable beside it.

Weaknesses:

- Wide stretches of sand between features are empty.
- The generated ridge rocks read as stacked blocks.
- The editor renders show the civilian unanimated; that is expected in edit mode.

The blockout's ordinary-traversal evidence (outpost 01 passed; western 02 rejected for focus loss) was recorded on the same routes. The final routes still need their ordinary in-player traversal and return, which waits for a quiet machine.

### Route-edge dressing (September 29)

Claude Code. Fourteen generated pieces now sit along the route verges:

- rubble and low strata on the western seam;
- discarded lamps, a broken drill bit and supply crates on the equipment service branch;
- crates and rubble along the haul road.

Each piece has a little scattered spoil beside it. Placement is computed from the routes and rejects any spot within 8 m of an encounter, within 6 m of ore, within 7 m of another route, north of the drill approach, or on rising ground. The route bake still passes (25 points and six ore stances), and the four Sorrel-dependent test classes pass 35/35. Opened views (`sorrel-final-03`) show the service branch reading as a worked equipment trail. The wide sand between features remains a terrain-surface weakness that props do not solve.

### Ordinary traversal on final art (September 30)

Claude Code, release player rebuilt from the integrated source, each capture run alone.

- `workshop/sorrel-final-outpost-01` (`sorrel-outpost-map`): **PASS**, 153.4 s, 9,205/9,205 frames focused.
- `workshop/sorrel-final-west-01` (`sorrel-west-loop-map`): **PASS**, 280.6 s, 16,837/16,837 focused. All four western packs cleared; the party dipped to 48 health and finished at 138. This is the loop previously rejected only for focus loss.
- **Stills.** Opened gameplay stills show the outpost's habs, pads and crates at play scale, combat in the seam with crystal ore, and the new verge rubble.
- **Coverage.** The haul road to the drill, the Burrower and the key are traversed by the passing slice smoke; the service branch has no ordinary route yet.

### Remaining ordinary routes (October 3)

Codex: the east service branch and return pass ordinary input (260.1 s, full
focus); the haul road through all four encounters into Hushwell also passes
(208.7 s, full focus). [Evidence](../validation/MAP-remaining-routes.md).
Opened gameplay views retain two presentation issues for the land pass: uniform
bare sand, and the bore scaffold standing over intact-looking ground. Traversal
coverage is complete; these are not erased by the route result.

## Landscape dressing (October 3)

Low dry olive grass occupies sheltered ridge/outpost margins, with the haul, seam, service and encounter lanes kept clear. Macro ground color varies under the fixed camera. The bore scaffold now stands over a real narrow terrain cut; its approach stays level and the compact cut retains support under the frame. Navigation was rebaked. Public generated fallbacks and optional licensed local art share the same placement/collision. [Validation](../validation/C8-landscape-surfaces.md); the final service and bore walks pass with full focus, including the transition into Hushwell.
