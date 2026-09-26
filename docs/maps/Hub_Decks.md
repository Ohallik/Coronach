# Hub_Decks — first-station reconstruction brief

2026-09-26. **Design OPEN; blockout OPEN; integrated MAP_EYE_TEST OPEN.**
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

Target workshop envelope: roughly 88 × 48 m, three unequal hulls around x=-28, 0, +28. This is the corrected workshop neighbourhood, not the atlas's eventual 320 × 240 m campaign town. A wider map must earn its travel time during C8.

Visitor route: office dock → pressure vestibule → arrivals desk → promenade → market or repair reception. Resident route: quiet northern cabin corridor → galley → public promenade. Delivery route: repair dock → receiving → stores → rear service gallery → market stock; no freight through the office or a private cabin. Worker route loops through the service gallery and two end cross-passages back to the public frontage.

The underlying keel carries utilities, with visible hull supports and connection trunks. The accessible service gallery stays on the walking plane so upper floors cannot hide the player from the fixed camera. Non-playable cabins and plant spaces are implied behind bounded, labelled doors with enough hull volume to contain them. There are no doors into empty space.

The exterior and interior share hull centers, extents, dock identities and bridge locations through a single layout definition. Exterior roofs/keels enclose the interior deck; small-person launch pads attach to the same hull's vestibule. Reused hulls and their separation collars establish the later town launch without explaining away unsupported geometry.

## Preservation and verification

Keep zone IDs and spawn IDs `Arrival`, `Office`, `Shop`, `Repair` stable, moving their safe positions with their corresponding vestibules. Current saves store zone/spawn, not arbitrary player coordinates. Preserve speakers, quest flags, shop and bench behavior. Update navigation fixtures to the new clear paths after baseline capture; old hardcoded routes are not a design constraint.

Reject blockout for disconnected hull pieces, blind routes, obstructed doors, a delivery path crossing private housing, missing edge boundaries or an exterior that cannot enclose the interior. Open overhead, section and ordinary arrival views without labels; walk both public and service loops. Repeat with HUD and interactions. Record concrete observations and recapture defects before either pass can close. Rerun C1 after geometry changes.

## Before observations

Opened `Builds/quality/C0/maps-before/Hub_Decks-overhead.png` and `Hub_Decks-arrival.png`: three almost identical enormous tiled bays; props do not establish distinct uses. The first continuous route hit benches across the apparent cross-station walkway. There is no quieter residential branch, usable rear service route, or clear pressure boundary at the front edge. **FAILED.**

After evidence and acceptance: **pending implementation**.
