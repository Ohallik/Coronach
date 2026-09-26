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
