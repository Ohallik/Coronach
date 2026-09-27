# Hub_Decks — first-station reconstruction brief

2026-09-26. **Design implemented; blockout PASSED; integrated spatial MAP_EYE_TEST PASSED. C1 pending.**
Reviewed by Codex, not Nathan or an independent viewer.

The Decks are a small working neighbourhood made from three retired vessels joined along the inner edge of Cinder Halo. Orrin receives visitors and freight manifests, Mira supplies residents and crews, and Hal repairs their equipment. Housing and shared meals explain why people stay here. The ring carries the connected hulls, power and traffic; it is not a necklace of disconnected floating ornaments.

## Functional plan before construction

All dimensions below are metres. One continuous gravity plane; ordinary adults are about 1.9 m tall. Public paths have at least 3 m clear width, cargo/service paths 4 m and major hatches 3–4 m. Camera-facing hull walls use a consistent low cutaway, with full-height end frames making the missing roof understandable. No invisible edge may be the only apparent enclosure.

```text
                                      NORTH / OUTER RING
                     visible structural ring and utility connections
        ┌──────────────── SERVICE GALLERY (4 m clear) ────────────────┐
        │ access to plant     provisions/stock       parts & freight │
 ┌──────┴────────────┐   ┌────────────┴───────────┐   ┌──────┴─────────────┐
 │ residential      │   │ shared galley         │   │ stores / machinery │
 │ doors + corridor │   │ tables / meal counter  │   │ operator clearance │
 ├──────────────────┤   ├────────────────────────┤   ├────────────────────┤
 │ Orrin's office   │   │ Mira's market          │   │ Hal's repair bay   │
 │ arrivals desk    │   │ front sales / backstock│   │ bench, work space  │
 │ visitor seats    │   │ delivery via gallery  │   │ receiving adjacent │
 ├──────┬───────────┴═══┴────────────────────────┴═══┴──────────┬─────────┤
 │      │            PUBLIC PROMENADE (4–6 m clear)          │         │
 │arrival vestibule │      public market vestibule    │ cargo vestibule│
 └──────┬───────────┘   └────────────┬───────────┘     └──────┬─────────┘
     OFFICE DOCK                 MARKET DOCK               REPAIR DOCK
                            SOUTH / INNER FLIGHT LANE

     ═══ = short enclosed bridge with visible separation collar,
           joined to hull bulkheads, never a gap in the floor.
```

Target workshop envelope: roughly 80 × 44 m, three occupied hulls around x=-28, 0, +28. This is the corrected workshop neighbourhood, not the atlas's eventual 320 × 240 m campaign town. A wider map must earn its travel time during C8.

Visitor route: office dock → pressure vestibule → arrivals desk → promenade → market or repair reception. Resident route: quiet northern cabin corridor → galley → public promenade. Delivery route: repair dock → receiving → stores → rear service gallery → market stock; no freight through the office or a private cabin. Worker route loops through the service gallery and two end cross-passages back to the public frontage.

The underlying keel carries utilities, with visible hull supports and connection trunks. The accessible service gallery stays on the walking plane so upper floors cannot hide the player from the fixed camera. Non-playable cabins and plant spaces are implied behind bounded, labelled doors with enough hull volume to contain them. There are no doors into empty space.

The exterior and interior share hull centers, extents, dock identities and bridge locations through a single layout definition. Exterior roofs/keels enclose the interior deck; small-person launch pads attach to the same hull's vestibule. Reused hulls and their separation collars establish the later town launch without explaining away unsupported geometry.

## Preservation and verification

Keep zone IDs and spawn IDs `Arrival`, `Office`, `Shop`, `Repair` stable, moving their safe positions with their corresponding vestibules. Current saves store zone/spawn, not arbitrary player coordinates. Preserve speakers, quest flags, shop and bench behavior. Update navigation fixtures to the new clear paths after baseline capture; old hardcoded routes are not a design constraint.

Reject blockout for disconnected hull pieces, blind routes, obstructed doors, a delivery path crossing private housing, missing edge boundaries or an exterior that cannot enclose the interior. Open overhead, section and ordinary arrival views without labels; walk both public and service loops. Repeat with HUD and interactions. Record concrete observations and recapture defects before either pass can close. Rerun C1 after geometry changes.

## Before observations

Opened `Builds/quality/C0/maps-before/Hub_Decks-overhead.png` and `Hub_Decks-arrival.png`: three almost identical enormous tiled bays; props do not establish distinct uses. The first continuous route hit benches across the apparent cross-station walkway. There is no quieter residential branch, usable rear service route, or clear pressure boundary at the front edge. **FAILED.**

Blockout iteration: three 24 × 40 m occupied hulls now share 6 m public bridges and 5 m service bridges, with four bounded cabins, a galley, market stock, receiving and repair spaces. Office and repair furniture/people initially obstructed the center aisle; `station/blockout-walk-01` rejected those checkpoints. Moving them into work alcoves cleared the entire public/service/housing route in `blockout-walk-02`.

That second run exposed a separate functional failure: the direct-steering companion remained at the service wall, 30 m behind, so swapping failed the final dock checkpoints. The focused companion regression reproduces 30.730 m separation (`station/companion-red.xml`). Baked collision-derived navigation now passes the unchanged companion regression, and the complete party walk (`blockout-walk-03`) and three-dock exterior loop (`blockout-dock-02`) pass. No teleport or relaxed checkpoint is used. Codex accepts the blockout spatial plan after opening `blockout-03` overhead/arrival/structure views and actual route stills (office, galley, service route and swapped-hero return). Exterior review also rejected hanging exposed beams as leg-like; the lower utility keel is now enclosed and joined to the ring.

Final integrated spatial acceptance: **PASSED**, after final-walk-05 and final-docks-07 on the current release package. See the final review below.

Blockout review and exact evidence: [station-blockout.md](../validation/station-blockout.md). Generated furniture must retain the proven clearances. The final eye test and performance rerun remain required.

Final review, September 26: Codex accepts the integrated spatial construction/circulation after complete focused ordinary-input routes `final-docks-07` and `final-walk-05`, opened hatch/room/approach stills and the unlabelled paired overhead/structure comparisons. The lower-casing/bridge seam is closed. Exact evidence and remaining presentation limits: [station-final.md](../validation/station-final.md). Fresh C1 performance, continuous motion/audio observation and the all-map workshop gate remain open.
