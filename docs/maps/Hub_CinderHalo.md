# Hub_CinderHalo — exterior of the Decks

2026-09-27. **Design implemented; blockout PASSED; final MAP_EYE_TEST REOPENED. C1 pending.**

The longer C4 perimeter view supersedes the earlier final acceptance below. Codex opened `Builds/quality/C4/civil-hulls-sela-03/003.png` and `004.png`: the back ring has ragged projections and over-stretched panel detail, reading as damaged sheetwork rather than a continuous structural utility section. The public/interior routes still work, but this exterior construction needs correction and fresh paired inspection before the first station can pass again. A controlled comparison will retain its 4 × 5 m section while replacing each over-stretched solid wall with normally proportioned generated top/bottom skins and inner/outer webs. No revised release geometry has been accepted yet.

2026-09-28 ring correction (Claude Code). `StationRedesign.Ring` keeps the same 62 m radius and 4 × 5 m section, with its top still at y=-3.5 against the hull casings. It now builds 96 spans, each a generated deck plate over a 3.5 m inner and outer wall fascia. These are the hull roof and wall modules at near-native proportion. Plates meet at the outer polygon vertex, and alternate plates sit 2 cm proud so their inner overlap never shares a plane. The fascias' exposed faces meet at their own vertices. The underside stays open because no flight or review camera sees below the flight plane. `StationRedesign.Exterior` rebuilds only this scene, and the object inventory differs from the committed scene only in the ring pieces. Controlled views opened by Claude (`Builds/quality/station/ring-before-01` and `ring-after-01`: free span, far arc, hull contact, quadrant and side elevation): the before set shows the wall's pilasters lying flat as brown bars and spiked stretched edges. The after set reads as a continuous deck band matching the hull roofs, with panelled fascias matching the hull walls and a clean keel-level contact at the west hull. **Controlled construction views PASS; the in-player perimeter recapture is still required before final MAP_EYE_TEST can pass again.**

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
