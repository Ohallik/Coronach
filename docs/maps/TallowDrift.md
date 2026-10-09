# Tallow Drift — repair refuge inside the beacon hull

2026-09-26. **Baseline FAILED; revised blockout and corrected layout fit PASSED; final spatial MAP_EYE_TEST PASSED.** Reviewer: Codex, actual scene stills. Continuous traversal is recorded separately under C0.

The generated approach hull is an enclosed beacon/refuge with a forward docking stem and a broad central pressure body. The rejected baseline interior was a 25 × 24 m exposed rectangle with one rear wall and three invisible collision fences. It offered a Keeper, repair console and departure pad, but no visible protected waiting/rest space, stores or believable enclosure. Opened `C0/maps-before/TallowDrift-overhead.png` and `TallowApproach-structure.png`; the baseline interior could not be read as that exterior's occupied deck.

Preserve Arrival/Dock IDs, completion/reward behavior and the free repair/save service. Use the existing exterior hull; the interior's local origin corresponds to the hull's central deck, about 15 m north of the exterior origin. Its 25 × 24 m room fits inside the 46 × 42 m outer body. The exterior docking stem at z=5 aligns with the interior's south vestibule at z=-10 after that offset.

That initial fit judgment was too coarse: the main circular body was offset within its overall bounds. The final-art review reopened it and enlarged/recentered the exterior, with an attached airlock/lift tower. See the corrected [Tallow Approach envelope and section](TallowApproach.md). Retain the paragraph above as the rejected design assumption, not proof of fit.

Required circulation: south pressure hatch → clear central arrival aisle → Keeper/help desk → starboard repair alcove. A separate north service strip connects repair stores, life-support controls and private rest cabins. Port-side seating is a refuge waiting area, outside the main cross aisle. The repair operator must reach stores without crossing the launch hatch or a sleeping room. Keep outer boundaries visible and collidable; use the same deliberate low-wall/roof cutaway convention as the Decks while retaining an enclosed exterior.

Before acceptance, inspect an overhead, exterior/section comparison and actual arrival, repair, waiting/rest and service-route views. Walk the public and staff loops, press against every visible boundary, launch and redock. Rerun the 120-second Tallow timing route after changes. The blockout pass below covers circulation; final acceptance remains open.

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
+-- service hatch --+-- public opening --+-- service hatch --+
| life support      | pressure-lift cabin | life support      |
|                   |       launch       |                   |
+-------------------+-- SOUTH HATCH ------+-------------------+
```

Codex opened the unlabelled overhead and arrival views under `station/blockout-03`, the retained generated exterior/structure comparison, and ordinary service/waiting/dialogue frames. The port waiting alcove and starboard repair/stores leave a clear central aisle. The rear cabins have real room volumes, closed fronts and a separate service strip; the visible cutaway perimeter now agrees with an enclosed refuge. The final generated-art check must confirm prop scale and the exterior deck envelope.

`tallow-blockout-walk-01` rejected the fixture's diagonal across the repair console. A corner waypoint preserves the same destination and assertions. `tallow-blockout-walk-02` passes all 47 checkpoints, 177.72 seconds/210.77 m, both heroes, Keeper dialogue, actual free repair/save (sliceComplete), visible boundaries, launch and redock. Opened the repair-store corner frame 033. This establishes ordinary-input circulation, not continuous watched motion or clean performance. Final generated-art review and timing remain required.

The final-02 review requires the five-metre central arrival compartment to read as the lower pressure-lift cabin. Its z=-12..-7 footprint maps to the revised exterior tower z=3..8. Cutaway side bulkheads at x=+/-2.5 enclose the cabin; closed service hatches lead into two flanking life-support cells with operator access. The front hatch has a real opening in the outer wall, with no wall drawn across the panel. The central north opening retains access to the public waiting/repair floor. Arrival stays (0,0,-8); launch and save IDs stay unchanged. This replaces an ambiguous launch prompt beside a plain bulkhead and requires fresh ordinary-player capture.

Opened the rebuilt unlabelled arrival and overhead views in `station/maps-after-11`. The front hatch occupies a real gap in the hull wall; its panel is visible from the lower lift compartment. Flanking machinery has closed service access from the public floor, and the cabin opens directly onto the waiting/repair aisle. Public traversal and launch checks remain required in the rebuilt player.

## Final spatial review

Codex accepts the corrected pressure-lift/refuge arrangement after opening the unlabelled maps-after-11 comparisons and final-tallow-03 release arrival, repair/service, launch and redock views. The complete 47-checkpoint ordinary-input route passes in 177.594 s, including both heroes, repair/save and return. [Final review](../validation/station-final.md) records concrete observations and prior rejections. The historical OPEN notes above document the iterations before this review. C1 reruns, watched continuous motion, audio audition and physical controller feel remain separate pending checks.

## Recycling garden (October 3)

A low contained water/plant bed at x=11, z=-2 makes the refuge's maintained life-support work visible. Its 2 by 5 m rim belongs to the starboard hull edge, outside the x<=9 public aisle and south of the boundary waypoint at (11,1). Service and repair access stay open. [Changed-map review and recapture](../validation/C8-landscape-surfaces.md).

## Moving-refuge story blockout — October 9

The historical accepted refuge/dock layout remains. A new port reel/platform and paired cantilevers attach to the unchanged circular pressure hull; the line has a genuinely free fitting and an independently drifting marker. Only the new exterior root and four interior Keeper fields change. The garden, dock, pressure-transfer geometry and old object IDs remain. Three eight-image controlled sets were individually opened: corrected marker cropping, then fitting occlusion, then a rejected crossed loop. The retained smooth open loop has a readable end but keeps partial reel overlap and generated-kit limitations. **Changed-scope final MAP_EYE_TEST is OPEN**; no reeling animation, ordinary HUD/launch/inspection/return, continuous motion or clean timing is accepted. The 125-step ordinary extension preserves all 18 earlier Tallow checkpoint fields but is REJECTED for a return waypoint miss and later focus loss. Eight ordinary images were opened; 117 remain unopened. Dialogue imagery rejects the marker moving behind the panel and out of view. Completion of the actual Keeper return remains UNVERIFIED. [Exact evidence](../validation/C8-tallow-mooring.md).


## Dialogue-reading follow-up - October 9

The exterior story camera now keeps the actual moving assembly and both craft above dialogue during the controlled reading interval. No existing geometry or interior is changed. Twelve before/after images were opened; partial reel overlap, blockout fitting and missing reeling animation remain. Current ordinary confirmation is **UNVERIFIED**: the read-only preflight at 10:00:40 UTC found VS Code (PID 20408) in the foreground, so no competing visible replay was started. No foreground was changed. The earlier rejected mooring/player02 remains the latest ordinary evidence. Final MAP_EYE_TEST and continuous audiovisual/physical review remain OPEN/UNVERIFIED. [Evidence](../validation/C8-tallow-reading.md).
