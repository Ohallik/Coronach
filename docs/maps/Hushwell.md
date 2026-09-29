# Hushwell — the moon cave above the nursery

2026-09-28 functional brief (Claude Code), written before construction as MAP_EYE_TEST requires. **Blockout PASS (agent review); final built with the generated kit, in-player pass OPEN.** Evidence: [validation/MAP-Hushwell.md](../validation/MAP-Hushwell.md). Atlas map 04; music **Moon Caverns**; boss **B02 Bellows Below**.

## Purpose and formation

Hushwell is a natural cave system under Sorrel's drill site: old lava tubes widened by the young Choir growing below. Sorrel's outpost drill bored straight down into it. That is why the drilling machine began following signals, and why the Burrower guarded the site.

The cave has one visible rule. **The nursery breathes.** Every six seconds a pressure pulse travels up through the rock. Every vent shares the cycle: three seconds of exhale, three of inhale. On the exhale, floor vents in the pressure gallery throw a scalding dust plume (3.2 m) that hurts whoever stands in it, hounds included, while the ledges between vents stay dry. The vent-membranes over the two side passages open on the same exhale and seal on the inhale. A vent's throat glows brighter through the inhale, so the next exhale is always readable. Nothing opens or burns without a visible cause.

The walls carry evenly spaced curved grooves. On the upper level they look like a surveyor's grid. Lower down they curve inward, and at the bottom they wrap around eggs. This is the Choir's growth geometry, the same curve Sela later finds on the Gullet's walls. The discovery is made through space, not a codex.

## Users and access

Outpost miners used the upper level until the transmitter shutdown; their scaffolds, lights and a stalled ore cart remain. Nobody has been below. The party enters by the drill's bore from Sorrel's excavation, after the Burrower is defeated. The drill shaft's service lift, repaired at the bottom, is the return shortcut to the surface.

## Layout (three levels, about 550 m of route)

```text
LEVEL 1  y=0   DRILL BREACH ──► MINERAL GALLERIES ──► survey-grid wall ──► ramp down
               (scaffolds, cart,      (crystal clusters,                    │
                bore opening)          first grooves)                       ▼
LEVEL 2  y=-8  PRESSURE GALLERY: three chambers joined by vent-membranes ──► BELLOWS
               (the pulse opens the side vents; safe ledges stay dry)        CHAMBER
                                                                              │ (B02)
LEVEL 3  y=-16                                     NURSERY ◄── curling grooves ◄┘
               (eggs cradled in the grooves; quiet; drill-shaft lift back up to Sorrel)
```

| Area | Function | Size | Combat |
|---|---|---|---|
| Drill breach | Arrival where the bore broke through; miners' scaffold, ore cart and work lights | 30 × 24 m | none (safe pocket) |
| Mineral galleries | Two linked galleries with crystal clusters and the first grooves | 90 × 22 m | 2 packs (Scrapmites) |
| Survey-grid wall | The grooves' most regular stretch; a slope descends | 40 m | none |
| Pressure gallery | Three chambers with six floor vents that scald on the exhale; the ledges between them stay dry; two side passages behind breathing vent-membranes | 3 × 26 m | 2 packs (Ridgehounds) |
| Bellows chamber | Boss arena: an inflating predator in a domed chamber, with two pressure organs to break. While either organ pumps, the Bellows is braced (half damage, armoured impacts). Its breath is an inhale inside a marked ring, then an exhale that strikes and staggers everything inside the ring; the ring widens below half health. Each exhale also blasts out through one pumping organ, in turn, across that organ's half of the chamber (marked during the inhale). Breaking an organ makes its half permanently safe, so the player chooses which half to keep, as the atlas's B02 design asks | 42 m round | B02 |
| Nursery | Eggs cradled around a spiral groove; looking at them is the discovery (`hushwell.nursery`), which wakes the drill-shaft lift back to Sorrel | 36 × 28 m | none |

Circulation: one main descending route with two optional side pockets behind vents (salvage and a crystal), and the shortcut lift. Every drop is a readable slope or lift; no unexplained ledges.

**Camera and scale.** The fixed ground camera (pitch 40°, yaw 20°, 19.5 m, 30° field of view) sees about 18 m across, so the cave uses a cutaway rule measured against that camera. Rock between the camera and any floor stays below the sightline to a hero standing there, measured from the lower of the two levels so a ramp shoulder never hides the next chamber. That leaves at least a steep 1.5 m lip, which keeps heroes off the rock. Far walls stand 6–7.4 m, and the generated grooved faces stand only where their whole 5 m slab is in rock and below every sightline beyond it. Corridors are 10 m wide (at least 8.9 m after the wall wander); side passages are 6.8 m.

## Identities

Scene `Hushwell`; spawn `Arrival` (breach). Sorrel gains spawn `Hushwell` at the bore. Encounters are `Hushwell_0..3` and `Hushwell_Bellows`. Flags are `hushwell.nursery` (discovery), `bossdown.BellowsBelow`, `clear.Hushwell_*` and `cache.HushwellMiners`; ore ids are `hushwell_0..2`. The Sorrel bore (the miners' scaffold beside the drill) requires `bossdown.Burrower`. The breach scaffold climbs back to Sorrel at any time, and the nursery lift does too once the nursery is seen. Hushwell is an optional branch until the first-chapter quest is rewritten, so the existing warp-key route and saves are unaffected. It carries its own side quest, **What the drill found** (`QuestDef Hushwell`). The quest starts on first entry, needs the Bellows killed and the nursery discovered, and rewards 220 XP and 150 scrip. The objective HUD leads to the Bellows, then the nursery, then the lift. Back in Sorrel, the Survivor mentions the breathing cave after the warp key; after the discovery they answer with a new nursery conversation, keeping the nursery thread alive from Sorrel onward. The plan is code: `HushwellLayout` holds the route, rooms, levels and clearance function shared by the builder and the tests.

## Generated kit (Meshy)

Board first, then isolated references QA'd before any 3D spend. Palette follows Sorrel: dusty warm rock #A88461, darker cave rock #6E5540, slate plating #485568, cream accents, cyan #22F5FF mineral light. Pieces: grooved cave wall, cave column, slope shelf, grooved marker slab, pressure vent, nursery egg cradle, mining scaffold, ore cart and drill-shaft lift. Bellows Below gets a pattern prototype on placeholder geometry before its final model. The prototype is built: `EnemyDef BellowsBelow` (12,000 integrity; weak to Pulse, resists Kinetic), the breath and half-chamber blast pattern in `BossController`, and two `PressureOrgan`s using the Gullet's Choir pod. The organs are weak to Kinetic, so Taren is the organ breaker, and the companion AI targets them like any hostile. All nine kit models cost 147 credits (a 12-credit board plus 9 × 15). A second batch (P41) adds the Bellows Below's final model: a limbless, domed bellows-sac on a skirt-foot, shaped nothing like the Burrower's worm. It needs no rig, because its breathing is its animation. The batch also adds nine floor props: stalagmites, rubble, the crystal beds that are Hushwell's ore, low cover ridges, a fallen work lamp, a broken drill bit, Choir growth, hatched shells and supply crates. The last open item from the atlas design is its final phase moving across exposed ribs.
