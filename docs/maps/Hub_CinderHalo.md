# Hub_CinderHalo — exterior of the Decks

2026-09-26. **Design implemented; blockout PASSED; integrated MAP_EYE_TEST OPEN.**

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

After: the blockout now has a continuous 64-joint ring, three 24 ? 40 m hull envelopes, closed lower utility casings and enclosed public/service bridges matching the interior. Bare hanging beams were rejected and enclosed. The traffic arcs were moved outside the almost-99 m hull-corner radius (112-142 m lanes). Opened `station/blockout-03/Hub_CinderHalo-{overhead,structure,arrival}.png`: the former disjoint arcs and floating pods are gone; attached docking stems and a continuous ring-to-keel connection read as construction. `blockout-dock-02` completes all three ordinary-input dock/interior-launch/return pairs. Its traffic radius predates the last clearance change; final flight recording must use the latest lanes.

Codex accepts the spatial blockout, with [review evidence](../validation/station-blockout.md). Final generated surfaces, roof machinery and vessel identity remain to inspect; blockout boxes are not final art. No paid generation is budgeted for this redesign.
