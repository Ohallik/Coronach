# Tallow Approach - exterior pressure envelope

2026-09-26. Reviewer: Codex. Paired with [Tallow Drift](TallowDrift.md). **Final MAP_EYE_TEST OPEN.**

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
          \--------[PAD]---------/  pad (0, 5)
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
