# Tallow Drift — repair refuge inside the beacon hull

2026-09-26. **Baseline FAILED; revised blockout PASSED; final MAP_EYE_TEST OPEN.** Reviewer: Codex, actual scene stills. Continuous traversal is recorded separately under C0.

The generated approach hull is an enclosed beacon/refuge with a forward docking stem and a broad central pressure body. The current interior is a 25 × 24 m exposed rectangle with one rear wall and three invisible collision fences. It offers a Keeper, repair console and departure pad, but no visible protected waiting/rest space, stores or believable enclosure. Opened `C0/maps-before/TallowDrift-overhead.png` and `TallowApproach-structure.png`; the interior cannot currently be read as that exterior's occupied deck.

Preserve Arrival/Dock IDs, completion/reward behavior and the free repair/save service. Use the existing exterior hull; the interior's local origin corresponds to the hull's central deck, about 15 m north of the exterior origin. Its 25 × 24 m room fits inside the 46 × 42 m outer body. The exterior docking stem at z=5 aligns with the interior's south vestibule at z=-10 after that offset.

That initial fit judgment was too coarse: the main circular body was offset within its overall bounds. The final-art review reopened it and enlarged/recentered the exterior, with an attached airlock/lift tower. See the corrected [Tallow Approach envelope and section](TallowApproach.md). Retain the paragraph above as the rejected design assumption, not proof of fit.

Required circulation: south pressure hatch → clear central arrival aisle → Keeper/help desk → starboard repair alcove. A separate north service strip connects repair stores, life-support controls and private rest cabins. Port-side seating is a refuge waiting area, outside the main cross aisle. The repair operator must reach stores without crossing the launch hatch or a sleeping room. Keep outer boundaries visible and collidable; use the same deliberate low-wall/roof cutaway convention as the Decks while retaining an enclosed exterior.

Before acceptance, inspect an overhead, exterior/section comparison and actual arrival, repair, waiting/rest and service-route views. Walk the public and staff loops, press against every visible boundary, launch and redock. Rerun the 120-second Tallow timing route after changes. No blockout or final pass is claimed yet.

## Revised blockout review

```text
NORTH: closed rear pressure wall
+---------- rest cabin --------+-------- rest cabin --------+
| bench / personal storage     | bench / personal storage    |
+--- closed door --------------+-------- closed door --------+
|          rear service aisle / access to stores            |
| help console       Keeper         repair console   crates |
| waiting seats        public aisle                         |
|                     cross circulation                     |
+---------------- pressure vestibule -----------------------+
|                         launch                            |
+------------------- SOUTH HATCH ---------------------------+
```

Codex opened the unlabelled overhead and arrival views under `station/blockout-03`, the retained generated exterior/structure comparison, and ordinary service/waiting/dialogue frames. The port waiting alcove and starboard repair/stores leave a clear central aisle. The rear cabins have real room volumes, closed fronts and a separate service strip; the visible cutaway perimeter now agrees with an enclosed refuge. The final generated-art check must confirm prop scale and the exterior deck envelope.

`tallow-blockout-walk-01` rejected the fixture's diagonal across the repair console. A corner waypoint preserves the same destination and assertions. `tallow-blockout-walk-02` passes all 47 checkpoints, 177.72 seconds/210.77 m, both heroes, Keeper dialogue, actual free repair/save (sliceComplete), visible boundaries, launch and redock. Opened the repair-store corner frame 033. This establishes ordinary-input circulation, not continuous watched motion or clean performance. Final generated-art review and timing remain required.
