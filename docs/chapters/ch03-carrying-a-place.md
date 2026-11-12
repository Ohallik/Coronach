# Chapter 3 - Carrying a Place (Xylos)

Brief for every stream working on chapter 3. Plan: [SHIPPING_PLAN.md](../SHIPPING_PLAN.md). Story bible: [STORY_CAMPAIGN.md](../STORY_CAMPAIGN.md). How-to: [CHAPTER_GUIDE.md](../CHAPTER_GUIDE.md).

## What the chapter is for

Tallow Drift can move, but it can't feed the cabins that now ride it. The river growers of Xylos won't sell a bulk harvest, because a field you sell once is gone. What they need is their own river back: a storm has stranded their seed carriers on the wrong bank and driven a root island, the Walking Canopy, across the crossing. Taren and Sela earn a partnership with labour, and the growers come aboard on their own terms, with a vote. It is the first time a community *joins*, and the convoy idea becomes real.

- **What the party changes:** the river routes reopen; the Floodcrown leaves the crossing; the growers agree to travel, keeping ownership of their crop systems.
- **What it sets up:** the seed garden aboard Many Hands (chapter 5), Jask as a passenger, Hulljack's claim drones (chapter 5's boss), the Compact's patrols and Meret Venn's name (met in chapter 5).
- **Taren and Sela:** Taren keeps reaching for the repair that puts things back as they were; Ruva keeps telling him the bank has moved. Sela treats Xylos as a stop on a chart until Jask asks her where she is going *to*, and she has no answer she likes.

Sync in: about 9-10. Sync out: about 13. Play time: 60-80 minutes.

## The people of Xylos: the Okhari

A new people, so Xylos doesn't look like another Vael town. **Okhari** are short (1.3-1.5 m), broad river people: smooth amphibian skin in moss green through river-clay brown with a paler throat, wide-set large dark eyes with a gold rim, a broad mouth, no hair, and a pair of **gill-frills** (fan-like crests) at the sides of the head that flare, droop and flutter with mood - the Okhari expression instrument, as plumes are for the Vael. Four fingers with webbing to the first knuckle. They dress in woven leaf-fibre wraps and seed-bead cords, and carry tools everywhere. They are practical, funny, argumentative and not mystical: growers, ferry hands, cooks, seed-carrier crews. The town also has a few Vael and other settlers; Okhari and Vael have traded for generations.

| Name | Who | Look | Voice |
|---|---|---|---|
| **Ruva Okh** | Seed-house speaker (F, old). Chairs the growers' votes and hates chairing. | Clay-brown, pale scar across one frill, a cord of seed beads for every harvest she has called | Blunt, warm, bargains in labour not scrip. Short sentences. Never flatters. "You can't eat a field once and still have it." |
| **Teo Marrow** | Seed-carrier captain (M, middle-aged). His three carriers are on the wrong bank. | Moss green, weathered, one frill notched, a pilot's whistle on a cord | Fatalistic and dry; counts things; talks about his boats like difficult relatives |
| **Binna** | Cook and provisioner (F, young). Feeds everyone, sells to visitors. | Bright green, apron of leaf-fibre, always a ladle | Fast, generous, nosy. Asks what you ate today before anything else. |
| **Jask** | Ferry pole-hand (M, adolescent). Wants to see a place with no ground. | Small, pale green, frills too big for his head | A question every line, mostly for Sela. Earnest, never cute. |
| **Old Senn** | Grower who isn't going (M, very old). | Dark, deeply creased, frills gone thin | Dry and unhurried. "A root island's already a boat. I don't need a bigger one." |
| **Wen** | The mender (F, middle-aged; a Vael who married into Rootmarket). Runs the press (fabrication bench), repairs and sells parts. | Vael, moss-dyed plumes cropped short, grower's wraps over collar node | Practical, amused by Shapers; speaks of the Okhari as "we" |

Portraits: sixteen expressions each, as for every named face. Okhari emotion reads mostly in the frills and eyes.

Off screen, heard: **Compact patrol *Steady Hand*** (a calm, correct radio voice; speaker id `Patrol`), which notes the party's position "outside the shelter corridor" and wishes them a safe transit on behalf of Director Venn's office.

## Places

### XylosNearspace (space, combat) - rule: moving cargo
Green-gold planet limb below with the river delta visible; a slow debris stream; **orbital seed barges** (big pods lashed with vine cable, Teo's fleet) drifting through it; the cracked **weather mirror** as the landmark. Three barges are snagged by **snagvine** (orbital weed) and being stripped by podskimmers. Cut each barge free (three vine anchors each: blade lunge or sustained fire), and it drifts back into the lane and becomes cover. With three free, the delta beacon grants landing. Hulljack's **claim drones** are tagging "unmoored vessels" at the far barge: an optional fight and the first sight of the seizure claim. The Compact patrol hails once, mid-zone.

### Rootmarket (town, safe)
Inhabited root platforms around a communal landing pool. Dry upper walkways and a lower ferry reconnect; the seed house is the landmark; the mender's press, Binna's kitchen, the ferry stage and Senn's island hang off the ring. Arrival scene: growers are hauling a root island to its mooring on ropes, mid-argument, and Ruva puts Taren on a rope before she learns his name. A Compact capacity notice is nailed to the seed-house door.

### BraidedRiver (wild, combat)
Three channels split and rejoin under root arches. **Two sluices** change which channels can be waded: the first drains the middle channel (reach the mid-river camp: Jask's ferry stage, an anvil, a save), the second drains the south (reach Seedfall bank). Carriers *Marrow-1* and *Marrow-2* are aground mid-river with their crews holed up; *Marrow-3* is beached at Seedfall under the **Seedthane**. The Walking Canopy's mooring is at the far east, visible from the first arch. Resources: Amber Sap and Rootfibre nodes; spice pods for Binna.

### WalkingCanopy (dungeon)
A storm-driven root island. Three **anchor roots** can be cut or set; each time, the island lurches and a different set of root-doors opens, so the same halls loop three ways. A flooding hall (pressure), the seed vault (relief, a chest and a view of the boss basin), a root-ladder shortcut back to the mooring, and a door checkpoint at the basin. **Floodcrown** in the basin.

## Map briefs

The ground camera is fixed (pitch 40, yaw 20, 19.5 m, FOV 30): "back" is the far side from the camera, and tall things belong there or at the map's edges (D119, D191). Each map must pass MAP_EYE_TEST: a stranger can say what the place is for, how people use it and how its parts connect. Every play-zoom still must measure 0.22 or less on `tools/look_density.py`. Jungle canopy cannot hang over walkable floor (it would hide the party): the jungle reads through trunks and buttress roots at the back and edges, leaf shadow moving on the ground, ferns and litter underfoot, water, drifting pollen and insects.

**XylosNearspace** (flight, about 1.6 km of lane, 8-10 minutes). West to east: arrival beacon; the debris stream curving north-east with the first snagged barge 250 m in (teaches cutting anchors); the weather mirror as a huge cracked disc north of the lane, with barge two in its shadow among mirror shards (cover, tighter flying); barge three at the stream's far bend under the claim drones; the delta beacon at the east end over the planet's limb. Free barges drift back into the lane and stay as cover. Podskimmer nests sit off the lane as optional pockets with a cache each. Empty travel never exceeds 20 seconds.

**Rootmarket** (town, about 150 x 120 m, a purposeful first circuit of 4-5 minutes). A ring of root platforms around the landing pool (the party lands on a pad at the pool's south edge). Clockwise from the pad: the ferry stage (west, Jask), the seed house at the north (the tallest thing, at the back; Ruva; the Compact notice on its door), Binna's kitchen with tables under a leaf awning (north-east), the mender's press (east, Wen: bench, repair, parts), the mooring where the root island is being hauled in (south-east, the arrival scene), and a rope bridge out to Senn's island (south-west). An upper dry walkway crosses the ring behind the seed house; the lower ferry route crosses the pool. The river gate (east, beyond the press) leads to the Braided River. Twelve or more residents: growers hauling, a child chasing a pod, two arguing over a rope, cooks, a ferry queue.

**BraidedRiver** (wild, about 420 x 260 m, 20-25 minutes). Enter at the west bank from Rootmarket. Three channels run west to east with two long islands between them. The north channel is fast and deep (never wadeable; reedstalker water along its edge). **Sluice A** on the north bank, a short spur past the first Thornback pocket, drains the middle channel: the party wades to the **mid-river camp** on the first island (Jask's ferry stage, anvil, save cradle, a cache; *Marrow-1* aground on its west tip with its crew) and to the second island (*Marrow-2*, Sporecasters in the wreck's lee; Senn's wife's island is a small spur off it with the mooring ring). **Sluice B** at the east end of the second island drains the south channel and opens **Seedfall bank**: a wide gravel fan under seed-pod trees at the back, *Marrow-3* beached across it, the Seedthane's arena. Beyond Seedfall, root arches frame the **Canopy mooring**: the Walking Canopy's flank fills the east edge. Three spice-pod plants (Binna) sit one on each island and one at Seedfall. A return shortcut: after Sluice B, a root bridge drops from Seedfall back to the west bank.

**WalkingCanopy** (dungeon, about 300 m of route on two levels, 20-25 minutes). Enter by the mooring roots into the **root hall** (hub). Three anchor roots: **A** in the hall itself, **B** down the flooding gallery, **C** beyond the seed vault. Cutting A opens the gallery and shuts the vault stair; cutting B (at the gallery's end, with the water rising behind the party: a marked safe ledge every few metres) opens the vault stair from below and drops a root ladder back to the hall (shortcut); cutting C in the vault's far side swings the island onto the crossing and opens the basin door. The vault is the breather: a chest, seed racks, a window slot down into the basin where the Floodcrown can be seen wading. Door checkpoint, then the **basin**: a round arena about 34 m across with three flood gates on the rim and three rooted platforms that stay dry in phase one.

## Enemies

| Enemy | Where | Shape and behaviour | Weak / resists |
|---|---|---|---|
| Podskimmer | Nearspace | Small fast seed-thief, swarms of five, strafing runs then scatter | Beam / Pulse |
| Snagvine Node | Nearspace | Anchored weed knot on a barge; lashes at close range; lunge-safe | Kinetic / Plasma |
| Claim Drone | Nearspace | Hulljack's salvage drone: tags, then fires a tether that slows | Pulse / Kinetic |
| Thornback | River | Armoured grazer, charges in a line, sticks in root walls (an opening) | Plasma / Kinetic |
| Reedstalker | River | Ambusher that rises from marked water; two quick bites and withdraws | Kinetic / Beam |
| Sporecaster | River, Canopy | Lobs spore pods that leave a slowing cloud; keeps its distance | Beam / Pulse |
| Mirefly Swarm | River, Canopy | A cloud that harries; weak; bursts when broken | Pulse / Beam |
| Rootwarden | Canopy | Slow root-armoured guardian; slam and root-line eruption; guards doors | Plasma / Pulse |

**Seedthane (mini-boss, Seedfall bank).** A tall many-legged seed-bearer defending the beached carrier as a nest. Thorn-fan sweeps; knock its hanging pods loose for cover; a planted phase (legs are break targets) and a mobile phase (it circles and fans). It is driven off the crossing, not killed.

**Floodcrown (boss, the basin).** An amphibious giant with an antler crown and control of the basin's root-gates. Phase one in the flooded basin: antler sweep, gate-slam wave down a marked lane, a wading charge; three **flood gates** on the rim are break targets during its openings, and each one severed drains the water and uncovers more floor. Phase two in the drained basin: a faster duel, crown charges that leave puddles which slow. At zero it goes down the drain channel to the river and is gone: the storm's creature leaving with the storm's water.

## Story beats (in order)

1. **Tallow, before leaving.** Abri: the cabins are asking about dinner. Sela has matched the lantern pattern to a lane. Pell is aboard, relighting lamps that aren't out.
2. **Nearspace.** Teo Marrow on the radio from a snagged barge, counting his problems. Three barges freed. The patrol's hail. Claim drones.
3. **Rootmarket arrival.** The rope. Ruva refuses the bulk sale: "You want to carry a place? Start by carrying this." Terms: get the carriers back across and she'll put it to a vote.
4. **Around town.** Binna feeds them and sells; Wen's press; Jask's questions; Senn isn't going; the Compact notice (Sela reads the capacity line twice).
5. **The river.** Marrow-1 and Marrow-2 relieved; sluices; Jask's ferry at mid-river.
6. **Seedfall.** Seedthane driven off Marrow-3. Teo: the crossing is still blocked - the Canopy has walked onto it and something has made it home.
7. **The Canopy.** Anchors, the vault, the basin. Floodcrown.
8. **The vote.** The carriers cross. Ruva's terms, said plainly: the growers travel with their own crop systems, they keep them, and they vote on where the convoy goes. Senn stays. Jask is suddenly nowhere to be found (he has stowed away; chapter 5). Sela, asked by Ruva where the convoy is going: "Somewhere with room to turn back. I'm still charting it."
9. **Chapter end.** Abri on the radio: Tallow has dinner, and three more settlements have asked to follow. And the air aboard is going stale. (Chapter 4: Wendmire.)

## Quests

| Id | Title | Giver | Steps | Reward |
|---|---|---|---|---|
| `SeedRoad` (main) | Carrying a Place | Abri, then Ruva | Chart the lane; free three barges; land at Rootmarket; relieve Marrow-1 and Marrow-2; open both sluices; drive the Seedthane off Marrow-3; cross the Canopy; stop the Floodcrown; hear the vote | XP, scrip, a T3 part choice |
| `ThreeBaskets` | Three Baskets | Binna | Gather river spice pods of three kinds along the river | Cooked food (buff consumables) in her shop |
| `MooringPost` | The Mooring Post | Old Senn | Carry his carved mooring post to the island his wife kept (mid-river); come back | A module; Senn's last word on leaving |
| `Questions` | Questions for a Surveyor | Jask | Three short talks with Jask (town, mid-river ferry, after the vote) | Sela's Fortune +1 token; Jask aboard in chapter 5 |
| `ClaimTags` | Unmoored | Teo | Strip Hulljack's claim tags from three barges in nearspace | Scrip, a drive part; Teo's warning about the salvage crew |

## Flags (prefix `xylos.`)

`xylos.charted`, `xylos.barge1..3`, `xylos.landed`, `xylos.ropeHauled`, `xylos.termsHeard`, `xylos.marrow1`, `xylos.marrow2`, `xylos.sluiceA`, `xylos.sluiceB`, `xylos.seedthaneGone`, `xylos.anchor1..3`, `xylos.floodcrownGone`, `xylos.voteHeard`, `chapter3.complete`.

## Music

Nearspace: Starfield Frontier (fights: Starfight). Rootmarket: Seed Shakers of Xylos (shops: Shop Theme). Braided River: Jungle Planet Groove; the sluice-and-carrier stretch can step up to Riverbed Stomp. Walking Canopy: Riverbed Stomp. Seedthane and Floodcrown: Alien Boss Battle.
