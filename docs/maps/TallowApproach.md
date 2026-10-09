# Tallow Approach - exterior pressure envelope

2026-09-26. Reviewer: Codex. Paired with [Tallow Drift](TallowDrift.md). **MAP_EYE_TEST PASSED for corrected layout and final spatial review.**

The beacon's circular pressure body houses the refuge, its Keeper, repair service, stores and private rest cabins. The long external arm serves beacon utilities; it is not a corridor the visitor must cross. Arrivals use a short dock apron and enclosed pressure-transfer tower above the occupied deck. The tower and support beams meet the generated outer shell. A scene transfer represents the airlock/lift descent; the matching interior launch vestibule remains at the south end of the refuge.

```text
OVERHEAD (north up, local flight coordinates)
                 beacon/service arm
                      /
           circular pressure body
          /----------------------\
          |  refuge deck 25 x 24  |  deck origin (0, 15)
          |  rear cabins/stores   |
          |  waiting / repair     |
          |    pressure tower     |
          \--------[PAD]---------/  pad (0, 0.5)
                    |
               clear approach
              Arrival (0, -25)

SECTION (indicative, shared gravity)
              tower roof ~+3.8 m
              | pressure hatch |
flight y=1 ---|--- dock apron --|  deck of apron y=0
              | lift / utility |-- supports enter the outer hull
          ____|________________|____
         /   refuge floor about -9.4 m \
        /     3.5 m occupied clearance \
        \_______ lower pressure shell _/
```

The first envelope judgment used the overall 42 x 42 m mesh bounds. A closer overhead and raw-vertex audit under `Builds/quality/station/tallow-envelope-before.csv` rejected that shortcut: the circular body is centered near (-3.8, 10.8), while antenna/arm geometry shifts its bounding center. The deck's corners crowded the rim. This reopens the exterior/interior part of the earlier blockout pass.

The revised exterior scales the same generated hull by 1.25 and offsets its wrapper to (4.75, -7, 20.25), centering the main body near (0, 15). Its roughly 21 m body radius leaves room around the 17.33 m diagonal of the deck's half-extents. The docking pad is now mounted to a pressure-transfer tower; it no longer floats over the original shell. Opened `station/maps-after-04/TallowApproach-{overhead,arrival}.png` and the paired interior views. Ordinary launch/redock and final clearance review remain required.

Preserve Arrival/Dock IDs and completion/save behavior. Do not present overall mesh bounds as proof of interior fit. Final review must examine the visible body, its attached dock and the ordinary flight camera together.


The final-art section was checked against actual world-space triangles, exported read-only as `station/tallow-envelope.obj`. A 1 m section grid across the 25 by 24 m deck finds the shallowest bottom near -9.78 m and the occupied roof above -5.3 m, apart from the malformed local roof opening beneath the fitted inspection cover. A shared floor near -9.4 m leaves 3.5 m clear occupancy below the roof; -10 m was too low at the northeast corner and has been corrected in the section. This is a broad spatial fit check, not an engineering simulation. The detached-looking black roof triangles were visible in `maps-after-05`; a larger generated inspection cover hides that patch in the opened `maps-after-06` arrival. Final ordinary-player evidence remains required.

## Pressure-transfer alignment correction

The final-02 ordinary-player review exposed a mismatch hidden by the broad shell-fit check: the existing tower occupied exterior z=7.5..12.5 while the interior arrival bay maps to exterior z=3..8 (interior z=-12..-7 plus the 15 m origin). The rebuilt tower and apron move 4.5 m forward, retain Arrival/Dock IDs, and place the dock at z=0.5 with return at -8.5. The shaft enclosure reaches the occupied deck near y=-9.4; its roof stays at +3.65. The lower cabin exits north through the existing central arrival opening; the upper dock enters from the south. Fit the interior cabin to x=+/-2.5, add matching closed front pressure hatch, and put life-support cells with service hatches on either side. The adjoining public routes remain outside the cabin. Final eye and docking checks remain OPEN.

Opened `station/maps-after-11/TallowApproach-{arrival,structure}.png` with the paired `TallowDrift-{arrival,overhead}.png`. The apron beams visibly meet the outer shell; the tower descends into it. The cabin footprint and the exterior shaft now agree, and the closed front panel reads as a pressure boundary. This editor inspection clears the identified alignment contradiction; rebuilt-player traversal is still required.

## Final spatial review

Codex accepts the corrected pressure-lift/refuge arrangement after opening the unlabelled maps-after-11 comparisons and final-tallow-03 release arrival, repair/service, launch and redock views. The complete 47-checkpoint ordinary-input route passes in 177.594 s, including both heroes, repair/save and return. [Final review](../validation/station-final.md) records concrete observations and prior rejections. The historical OPEN notes above document the iterations before this review. C1 reruns, watched continuous motion, audio audition and physical controller feel remain separate pending checks.

## Moving-refuge story blockout — October 9

The historical accepted refuge/dock layout remains. A new port reel/platform and paired cantilevers attach to the unchanged circular pressure hull; the line has a genuinely free fitting and an independently drifting marker. Only the new exterior root and four interior Keeper fields change. The garden, dock, pressure-transfer geometry and old object IDs remain. Three eight-image controlled sets were individually opened: corrected marker cropping, then fitting occlusion, then a rejected crossed loop. The retained smooth open loop has a readable end but keeps partial reel overlap and generated-kit limitations. **Changed-scope final MAP_EYE_TEST is OPEN**; no reeling animation, ordinary HUD/launch/inspection/return, continuous motion or clean timing is accepted. The 125-step ordinary extension preserves all 18 earlier Tallow checkpoint fields but is REJECTED for a return waypoint miss and later focus loss. Eight ordinary images were opened; 117 remain unopened. Dialogue imagery rejects the marker moving behind the panel and out of view. Completion of the actual Keeper return remains UNVERIFIED. [Exact evidence](../validation/C8-tallow-mooring.md).
