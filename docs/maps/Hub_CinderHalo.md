# Hub_CinderHalo — exterior of the Decks

2026-09-28. **Design implemented; blockout PASSED; final MAP_EYE_TEST PASSED again after the ring correction (reviewer: Claude Code agent review). C1 pending.**

The longer C4 perimeter view supersedes the earlier final acceptance below. Codex opened `Builds/quality/C4/civil-hulls-sela-03/003.png` and `004.png`: the back ring has ragged projections and over-stretched panel detail, reading as damaged sheetwork rather than a continuous structural utility section. The public/interior routes still work, but this exterior construction needs correction and fresh paired inspection before the first station can pass again. A controlled comparison will retain its 4 × 5 m section while replacing each over-stretched solid wall with normally proportioned generated top/bottom skins and inner/outer webs. No revised release geometry has been accepted yet.

2026-09-28 ring correction (Claude Code). `StationRedesign.Ring` keeps the same 62 m radius and 4 × 5 m section, with its top still at y=-3.5 against the hull casings. It now builds 96 spans, each a generated deck plate over a 3.5 m inner and outer wall fascia. These are the hull roof and wall modules at near-native proportion. Plates meet at the outer polygon vertex, and alternate plates sit 2 cm proud so their inner overlap never shares a plane. The fascias' exposed faces meet at their own vertices. The underside stays open because no flight or review camera sees below the flight plane. `StationRedesign.Exterior` rebuilds only this scene, and the object inventory differs from the committed scene only in the ring pieces. Controlled views opened by Claude (`Builds/quality/station/ring-before-01` and `ring-after-01`: free span, far arc, hull contact, quadrant and side elevation): the before set shows the wall's pilasters lying flat as brown bars and spiked stretched edges. The after set reads as a continuous deck band matching the hull roofs, with panelled fascias matching the hull walls and a clean keel-level contact at the west hull. **Controlled construction views PASS.** In-player recapture on the rebuilt Release player (`54d57fc`) used the unchanged ordinary virtual-gamepad circuit routes. `C4/civil-hulls-sela-04` passes (310.9 s, 18,648/18,648 focused samples) and `civil-hulls-taren-05` passes (310.3 s, 18,617/18,617), and both dock into the Decks at full integrity. Claude opened Sela frames 003/004 and Taren frame 002, the same perimeter viewpoint that reopened this review. The ring reads as a continuous panelled deck band with fascia sides in the hulls' own construction; no stretched texture, spikes or damaged-sheetwork edge remain. The standard overhead/structure pair (`station/maps-after-13`) shows the ring and roofs as one station. `civil-hulls-taren-04` is retained as rejected: focus was lost at 133 s while this session was issuing shell commands, which is the likely cause. **Final MAP_EYE_TEST PASS.** Motion was reviewed from stills, not continuous video, which remains UNVERIFIED.

Purpose: residential/service vessel neighbourhood and local freight interchange. Visitors fly the inner approach; haulers follow a distinct outer traffic lane. Three dock identities match the Decks. Sorrel's exit lies east; the Gullet beacon lies beyond the north ring. These landmarks must remain visible without labels.

Use [the paired interior functional plan](Hub_Decks.md). The three occupied hulls sit on a continuous structural arc, with enclosed links between adjoining bulkheads. Extend the remaining ring with consistent joints and readable structural continuity. Generated curved pieces must have matching tangents and endpoints; alternating unrelated curved modules at equal angular intervals failed that test.

```text
SECTION (not to scale)
       roof envelope / outer maintenance equipment
       ┌────────────────────────────────────┐
       │  occupied rooms / public gallery   │
       ├──────────────── walking deck ──────┤── attached launch vestibule
       │ keel, utility trunks, life support │      and approach pad
       └─────────────┬───────────┬──────────┘
                  structural ring / clamps
```

Keep the old `Office`, `Shop`, `Repair`, `Arrival`, `Outer` and `Moon` spawn IDs. Arrival must be outside dock interaction range, with unobstructed braking space and a view of the destination. Returning to flight must not place the ship inside hull collision. Ordinary flight and docking, not a teleporting look probe, verify the approach.

Before: opened `Builds/quality/C0/maps-before/Hub_CinderHalo-overhead.png` and `Hub_CinderHalo-arrival.png`. Ring segments visibly miss their neighbours; several curve in inconsistent directions. Three dock pods float in the center without an inhabited hull or bridge connecting them. The 64 × 28 m interior cannot be inferred from that exterior. **FAILED**, reviewer Codex. The renders are actual scene geometry with labels hidden, not route evidence.

After: the blockout now has a continuous 64-joint ring, three 24 by 40 m hull envelopes, closed lower utility casings and enclosed public/service bridges matching the interior. Bare hanging beams were rejected and enclosed. The traffic arcs were moved outside the almost-99 m hull-corner radius (112-142 m lanes). Opened `station/blockout-03/Hub_CinderHalo-{overhead,structure,arrival}.png`: the former disjoint arcs and floating pods are gone; attached docking stems and a continuous ring-to-keel connection read as construction. `blockout-dock-02` completes all three ordinary-input dock/interior-launch/return pairs. Its traffic radius predates the last clearance change; final flight recording must use the latest lanes.

Codex accepts the spatial blockout, with [review evidence](../validation/station-blockout.md). Final generated surfaces, roof machinery and vessel identity remain to inspect; blockout boxes are not final art. No paid generation is budgeted for this redesign.

The closer final-docks-03 hatch/edge inspection rejects a gap beneath the walking deck: lower casing topped at y=-0.5 while the thin walking floor begins at -0.16; bridge support topped at -1. Before acceptance, extend the casing from -3.5 to 0 and each bridge support from -3 to 0 so both overlap the underside of the sealed floor. Keep the occupied deck, ring, doors and dock coordinates unchanged. Record the visible before/after join and rerun station navigation/docking.

Final review, September 26: Codex accepts the integrated spatial construction/circulation after complete focused ordinary-input routes `final-docks-07` and `final-walk-05`, opened hatch/room/approach stills and the unlabelled paired overhead/structure comparisons. The lower-casing/bridge seam is closed. Exact evidence and remaining presentation limits: [station-final.md](../validation/station-final.md). Fresh C1 performance, continuous motion/audio observation and the all-map workshop gate remain open.

### Ship-borne residents (September 29, Claude Code)

The town music is for flying around the station and talking to people in other ships (D118). Two more crews now hail from their own parked vessels, beside Neve's skiff, and bring the story's first-town seeds (STORY_CAMPAIGN Act I) before Meret appears:

- **Oda's hauler**, on the market approach at (17, −45), is a household packed for the Nacre transfer. Oda's worry about one berth and one departure foreshadows Nacre's disaster.
- **Ilo's Compact cutter**, off the repair hull's east side at (50, −9) on the way to the moon approach, is an Anchor Compact surveyor. Ilo introduces Director Venn's published shelter capacity and its footnotes.

Both speak through `HailPoint` from 7 m with their bodies removed, and neither ship collides, so no recorded flight circuit or dock approach changes. `CinderResidentTests` was rejected before they existed. It then caught a real defect: the speakers' placeholder capsule carried a live collider, an invisible obstacle in the lane, which is now removed. `StationRedesign.Exterior` rebuilt only the flight exterior. Opened views (`workshop/cinder-residents-01`) show both ships clear of the docks, hulls and lanes. Both have their own 16-expression portrait sets. These were generated in the house template (Meshy image-to-image, 12 credits each, 3,812 credits left), upscaled and keyed by the existing pipeline, and imported through the isolated `PortraitIntake.ImportNamed`. The resident test was rejected ("Ilo has no portrait") before the import and passes after it.

