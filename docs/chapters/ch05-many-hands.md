# Chapter 5 - Many Hands

Brief for every stream working on chapter 5. Plan: [SHIPPING_PLAN.md](../SHIPPING_PLAN.md). Story bible: [STORY_CAMPAIGN.md](../STORY_CAMPAIGN.md). How-to: [CHAPTER_GUIDE.md](../CHAPTER_GUIDE.md). Chapters 3 and 4 (briefs and scripts) come first in the story.

## What the chapter is for

The convoy becomes a town. Tallow's cabins, Xylos's seed carriers and Driftreed's rafts are lashed and docked into one ship that somebody has named **Many Hands**, and the party comes home to a place that didn't exist when they left. This is the hub for the rest of the game and the end of the second movement. It is short on fighting and long on people: one two-part boss and the scene the story has been walking toward since the Halo's evacuation notices. **Meret Venn** arrives in person, saves people the party could not reach, sits down to dinner, and is right about nearly everything.

- **What the party changes:** a mooring dispute settled, a vent fixed, the chart table calibrated (the route chart opens for fast travel), Hulljack's seizure claim broken and its operator taken alive.
- **What changes the party:** Meret's patrol seals a breached residential section before they can. At dinner she explains the Stillwater Plan in practical terms, publishes its capacity as she always has, and Sela half agrees. **Act turn:** the moon Stillwater will spend is Sorrel, and the nursery with it. Meret does not deny it. She hands over the engineering drawings and asks for a better plan *that has carried people* before her deadline.
- **What it sets up:** everything. The convoy is now a public alternative to the Compact. Ilo defects with the footnotes (the settlements the capacity figure leaves out). The nursery will have to cross a cold passage nobody knows how to cross (chapter 6, Hoarfell). Meret warns that Muster raiders are working the frontier (chapters 6 and 8).

Sync in: about 15. Sync out: about 16. Play time: 40-50 minutes.

## Meret Venn

A Vael, not a Shaper. Public route engineer and director of the Anchor Compact; in her sixties; silver plumes cropped close, pale steady eyes with dim teal sync-lines, a white-and-teal Compact field coat worn soft at the cuffs, an engineer's stylus she turns in her fingers when she is working something out. She has saved thousands of people by being competent early. She believes a smaller guaranteed future is more honest than a larger uncertain one. She is courteous, exact, tired, and funny in a dry way that sounds like Taren; the two of them like each other, which is the problem. She never lies, never gloats, never raises her voice, and never says anything she hasn't costed. She pilots machines rather than shaping Lattice. Portrait: sixteen expressions, Natural. Her theme at dinner is **Regal Alien Town**.

## Other new faces

| Name | Who | Look | Voice |
|---|---|---|---|
| **Dessik Varn**, "Hulljack" | Salvage boss (M, 50s). Holds a real, registered seizure claim on vessels that have left their moorings. Fights from a salvage exosuit. | Heavy-set Vael, plumes shaved to stubble, old burn across the jaw, a claims ledger chained to his belt | Reasonable in the way of a man reading from a contract. Not a pirate in his own eyes: "I don't steal. I *file*." |
| **Ilo** | The Compact surveyor from the Halo (chapter 1). Arrives by courier with the footnotes. | Existing portrait (`Ilo_Natural`) | Nervous, precise, done with being told the figure is the figure |
| **Captain of the *Steady Hand*** (speaker id `Patrol`) | Meret's rescue patrol. Heard in chapter 3, seen here. | Compact rescue crew; townsfolk-grade body, no sheet | Calm, correct, kind in a procedural way |

Returning and aboard: Abri (keeper of the ship now, appalled by the paperwork), Pell (lamps), Ruva and the growers (the garden), Teo (his carriers are the garden deck), Binna (the galley), Jask (found out as a stowaway in the garden), Maru and the weavers (the raft bay), Ona if she came, Corra Vey from Lanternwick (a stall in the arcade: "somebody has to sell these people a kettle").

## Places

### ManyHands (town and hub, safe; the freight hall becomes a combat pocket for the boss's second part)
A ship assembled from mismatched vessels, and it should look it: hull colours and deck plates change at every join. Six districts on one walkable deck plan with a short upper gallery:
1. **The freight spine** (south): the dock where the party lands, containers, the freight hall (the boss's second arena, off the spine behind a cargo door).
2. **The arcade** (centre): an old liner's passenger arcade. Corra's stall (parts), a provisioner's counter, the bench (a pump-and-press built from Quill's drawings), the repair cradle.
3. **The galley** (east): Binna's kitchen, long tables. The dinner scene.
4. **The garden** (north-east): the Xylos seed garden under Teo's carrier ribs: root beds, water, growers. Light and green: the prettiest room in the game so far.
5. **The raft bay** (west): Driftreed's rafts afloat in a flooded hold, rope bridges, mats drying. Maru's tide-line drawings on the bulkhead.
6. **The lamp gallery** (north, the upper level at the back): Pell's lamps, Abri's desk, **Sela's chart table** (the route chart).
The **cabins** run along the north-west; a section of them is the breach.

### ManyHandsHull (space, combat arena)
The outside of the ship: the mismatched hulls from above, lit windows, the garden's glass, cargo booms as cover. The Hulljack exosuit clamps on with four magnetic feet.

## Map brief

**ManyHands** (about 170 x 120 m, a first circuit of 5-6 minutes; it will be revisited for the rest of the game). The spine runs south to north as the main street; the arcade crosses it at the centre; the galley and garden open east, the raft bay west; the lamp gallery is up a stair at the north end, at the back of the frame, looking down the spine. Every district is visible from the crossing. Twenty or more residents, including recognisable townsfolk from Rootmarket and Driftreed, with walking loops between districts (growers carrying trays to the galley, weavers carrying mats to the cabins, children from both peoples running the spine). The look target: dense, warm, lived-in, every still at or under 0.15 on `tools/look_density.py`.

**ManyHandsHull** (flight arena, about 500 m across). The ship fills the centre; the party flies around and over it. Four feet at four hull joins; cargo booms and antenna masts break lines of fire; claim drones launch from Hulljack's tug standing off to the south.

## The fight: Hulljack

**Part one, in flight.** The exosuit walks the hull on four magnetic feet, cutting at a join. Each foot is a break target, exposed when it plants. Its attacks: a cutting-torch sweep across a marked arc of hull, grapple shots (a marked line; caught ships are reeled and must roll free), magnetised debris flung in a fan, and claim drones that tether and slow. Each foot severed makes it lurch to another join and changes which booms are cover. With four gone it tears through a cargo door into the freight hall. **Checkpoint and docking transition.**

**Part two, on foot, in the freight hall.** Dessik in the damaged suit, operator exposed at the chest. Grapple claw (a pull, guard or dodge), torch sweeps that leave burning lines for a few seconds, thrown containers (marked landing circles; a landed container is cover), a magnet pulse that drags loose crates across the floor. Break him and the suit kneels: strike the cockpit. At zero he yields (a person: `yieldsOnDefeat`), and is taken into custody by Abri's people, complaining about procedure.

**During part two** the alarm for the cabin breach sounds. The party cannot leave the hall. When the fight ends, the *Steady Hand*'s rescue cutters are already latched to the hull and the section is sealed.

## Story beats (in order)

1. **Coming home to somewhere new.** The dock. The name. Abri and the queue of settlements asking to follow.
2. **Making it a town** (three short jobs, any order): the mooring dispute between Ruva (roots) and Maru (tides); the galley vent, fixed with Binna talking throughout; the chart table calibrated against Pell's lamps (the route chart opens). Jask is found in the garden.
3. **Ilo.** Off a courier with a case of paper: the footnotes.
4. **Hulljack.** The claim read aloud over the radio. Part one. Part two. The breach alarm.
5. **The *Steady Hand*.** The section is sealed; everyone in it is alive. The patrol captain reports to someone behind him.
6. **Meret.** She asks if there is anything to eat.
7. **Dinner.** Binna cooks. Meret explains Stillwater: one adult passage pinned, a protected corridor, a published capacity. Sela: a route that stays open is a route she could finally leave by. Ruva asks who isn't in the number. Ilo puts the footnotes on the table. Taren asks what the anchor costs. **Sorrel.** The nursery. Meret: "Yes." She gives them the drawings. "Bring me a plan that has carried people. I can wait forty days. The routes can't."
8. **After.** Sela and Taren on the lamp gallery. The chart: to move the nursery, it has to cross the cold passage, and nobody alive knows how young Choir survive cold. Old charts name herders who do. (Chapter 6: Hoarfell.)

## Quests

| Id | Title | Giver | Steps | Reward |
|---|---|---|---|---|
| `ManyHands` (main) | Many Hands | Abri | Come aboard; settle the mooring; fix the vent; calibrate the chart; meet Ilo; sever Hulljack's feet; take the freight hall; see the breach sealed; dinner; read the chart | XP, scrip, a T3 part choice |
| `KettleTrade` | Somebody Has to Sell a Kettle | Corra | Carry three households' orders between districts (a tour of the ship) | Stall discount; a module |
| `StowawayFare` | The Stowaway's Fare | Teo | Decide with Ruva and Teo how Jask works off his passage (garden, ferry or the lamp gallery); he is seen there afterwards | A Fortune token; Jask's thanks, loudly |
| `ClaimsLedger` | The Claims Ledger | Ilo (after the fight) | Read Dessik's ledger with Ilo: three "seized" hulls whose owners are aboard; tell each | Scrip; three households keep their ships in the epilogue |

## Flags (prefix `hands.`)

`hands.aboard`, `hands.mooringSettled`, `hands.ventFixed`, `hands.chartOpen`, `hands.jaskFound`, `hands.iloAboard`, `hands.feet1..4`, `hands.hullCleared`, `hands.dessikYielded`, `hands.breachSealed`, `hands.meretMet`, `hands.dinnerDone`, `hands.sorrelNamed`, `chapter5.complete`.

## Music

The ship: Starward Journey (shops: Moonbase Market). Hulljack, both parts: Glitch Boss. Meret's dinner: Regal Alien Town, then silence under the last lines, then Starward Journey.
