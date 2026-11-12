# Chapter 5 script manifest - Many Hands

For the engineers wiring chapter 5. Script: [ManyHands.yarn](ManyHands.yarn). Barks: [barks.json](../barks.json). Brief: [ch05-many-hands.md](../../chapters/ch05-many-hands.md). Earlier manifests, in the same shape: [chapter 3](../ch03-manifest.md), [chapter 4](ch04-manifest.md).

**What is here:** 91 nodes, 305 dialogue lines, 18 option labels (323 strings). Compiled clean with the project's own compiler (Yarn Spinner 3.2.4, the nine live files and the three pending ones together: 0 errors, 310 nodes, no duplicate titles). No new Yarn commands: only `setFlag`, `startQuest`, `shop`, `bench`, `repair`.

**This file waits in `pending/`.** It moves up one folder with its script when the chapter's people and quests are defined: two new portrait sets (Meret, Dessik), four `QuestDef`s and the test lists in section 8.

## 1. Conventions this file follows

- Every title starts with its owning speaker id, which here means **the person the player walks up to**: `Abri…` (speaker `Keeper`), `Pell…`, `Ruva…`, `Maru…`, `Binna…`, `Teo…`, `Jask…`, `Hadda…`, `Corra…`, `Dov…`, `Ona…`, `Pim…`, `Garo…`, `Dabb…`, `Ilo…`, `Dessik…`, `Patrol…`, `Meret…`, and `Sela…` for the party alone. `RuvaDinnerNumber` and `IloFootnotes` are mostly Meret talking; they are named for who starts them.
- Each quest is started by exactly one node. A node sets a flag when the conversation is the event; every step in this chapter's quests that a talk completes is a `Flag` step, so nothing here is caught by the discovery-report trap (chapter 3 manifest, section 1).
- A choice changes a flag inside its own option body (`SelaMooringRail`, `TeoStowawayDecide`) and the node goes on to the same end either way. That is ordinary Yarn and needs nothing new.
- Checked against the word tests: no retired phrase, no id-shaped word, at most three options on any node.

### The dinner is seven talks

Each is short, each sets its own flag, and the next one is with somebody else at the table, so the player gets up and walks between them. None runs past 12 lines; the longest, the meal, comes to 47 seconds by the ninety-second test's arithmetic.

| # | Node | Walk up to | Sets |
|---|---|---|---|
| 1 | `MeretArrives` | Meret, behind the captain at the sealed cabins | `hands.meretMet` |
| 2 | `BinnaDinner` | Binna at the galley hatch | `hands.dinnerSat` |
| 3 | `MeretStillwater` | Meret, seated | `hands.stillwaterHeard` |
| 4 | `RuvaDinnerNumber` | Ruva, across the table | `hands.numberHeard` |
| 5 | `IloFootnotes` | Ilo, with the case | `hands.footnotesTabled` |
| 6 | `MeretSorrel` | Meret, seated | `hands.sorrelNamed` |
| 7 | `MeretDrawings` | Meret, standing at the galley door | `hands.dinnerDone` |

Between 2 and 7 five optional talks are open (`AbriDinnerTalk`, `BinnaDinnerTalk`, `MaruDinnerTalk`, `TeoDinnerTalk`, `JaskDinnerTalk`). `MeretSorrel` ends on her "Yes." and nothing follows it in the node: the walk to the door is the silence the brief's music note asks for.

**She says "Yes." twice, on purpose.** Once to Sela in `RuvaDinnerNumber` (a road that stays open for the rest of her life), once to Taren in `MeretSorrel` (the anchor kills the nursery). Same word, same face (`Neutral`). Please don't vary the portrait between them.

### Numbers the script fixes

They are said aloud by people who would know them, and later chapters will have to agree with them.

| Number | What | Said by |
|---|---|---|
| 312,000 | Stillwater's capacity; "three people in five" | Meret |
| 204,619 | The people in the footnotes | Ilo |
| Forty days | What Meret can wait | Meret |
| Sixty days | What it takes her to strike clause nine | Meret |
| Thirty days, and thirty-one | The salvage clause, and how long it has been | Dessik |
| Six cabins, thirty-one residents | The sealed section; all alive | Patrol |
| Nine years, six years | How long she has worked on the plan; how long section eleven has been posted | Meret |
| Four times | The pull the anchor is built to take ("I would not build it at three") | Meret |
| 412 | Jask, on Abri's count | Taren, Teo |
| 206 | The hulls in Dessik's ledger | Ilo |

## 2. Nodes

Lines are dialogue lines (options not counted). "Compulsory" marks the main path.

### Coming aboard (the dock)

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `AbriHandsArrival` | 10 | Keeper, Taren, Sela | **Compulsory.** Crossing point on the dock, first landing. A queue of delegates holding Compact berth forms with "Nacre" struck out. Names the three jobs. | `hands.aboard` | `startQuest ManyHands` |
| `SelaDockDoor` | 3 | Sela, Taren | Interaction at the dock door: "MANY HANDS" in chalk, the rest under the slid cargo door. Optional; it sets up Pell. | *hands.doorRead* | |
| `AbriCourier` | 1 | Keeper | Abri at her desk, the three jobs done and not `hands.iloAboard`. The pointer to Ilo. | | |

### Making it a town (three jobs, any order)

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `RuvaMooring` | 6 | Ruva, Taren, Sela | **Compulsory.** Ruva in the garden, her first node aboard. Her case: roots want still water. | `hands.ruvaHeard` | |
| `MaruMooring` | 6 | Maru, Sela, Taren | **Compulsory.** Maru in the raft bay, her first node aboard. Her case: a raft can't be pinned. | `hands.maruHeard` | |
| `SelaMooringRail` | 7 | Taren, Sela | **Compulsory.** Interaction at the hold wall between the two, once both are heard. Taren fits running rings on a rail either way. Two options: whose word it gets. | `hands.mooringSettled`, and `hands.mooringForRoots` or `hands.mooringForTides` | |
| `RuvaMooringLost` | 1 | Ruva | Ruva, once: `hands.mooringForTides`, not `ruva.mooringGrumbled` | `ruva.mooringGrumbled` | |
| `MaruMooringLost` | 1 | Maru | Maru, once: `hands.mooringForRoots`, not `maru.mooringGrumbled` | `maru.mooringGrumbled` | |
| `BinnaVent` | 9 | Binna, Taren | **Compulsory.** Binna in the galley, her first node aboard. She talks; Taren fixes the vent in three short lines (the mat was in backwards). Carries the first clue to Jask: his bowl. | `hands.ventFixed` | |
| `PellChartLamps` | 7 | Pell, Sela, Taren | **Compulsory.** Pell on the lamp gallery, her first node aboard. Who chalked the name. Then the three gallery lamps are to be sighted from the chart table. | | |
| `SelaChartOpen` | 4 | Sela, Taren | **Compulsory.** At the chart table once the three lamps are sighted. The route chart opens here: fast travel. | `hands.chartOpen` | |

### The stowaway

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `TeoHands` | 4 | Teo, Sela | Teo on the garden deck, his first node aboard, while Jask is not found. The second clue: his lists are one short of Abri's count. | | |
| `JaskFound` | 8 | Sela, Jask, Taren | Interaction behind the seed racks off Marrow-3, in the garden. Optional, signposted twice. | `hands.jaskFound` | |
| `JaskFoundRepeat` | 1 | Jask | Jask in the racks, found and not yet placed | | |
| `TeoStowawayOffer` | 4 | Teo, Taren | Teo, `hands.jaskFound`, `StowawayFare` not started. Also his first meeting aboard if Jask was found first (`LateMeetings`). | | `startQuest StowawayFare` |
| `RuvaStowaway` | 4 | Ruva, Taren | Ruva, `StowawayFare` at step 0 | `hands.ruvaOnJask` | |
| `TeoStowawayDecide` | 5 | Teo | Teo, `StowawayFare` at step 1. Three options: garden, raft bay ferry, lamp gallery. | `hands.jaskPlaced`, and one of `jask.worksGarden`, `jask.worksFerry`, `jask.worksLamps` | |
| `JaskThanksGarden`, `JaskThanksFerry`, `JaskThanksLamps` | 1 each | Jask | Jask at the place chosen, once. His thanks, loudly. | `jask.thanked` | |
| `JaskGardenHand`, `JaskFerryHand`, `JaskLampHand` | 1 each | Jask | Jask at that place afterwards: his line for the rest of the game | | |

### Ilo, Hulljack and the breach

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `IloArrives` | 8 | Ilo, Sela, Taren | **Compulsory.** Ilo at the dock with a case, once the three jobs are done | `hands.iloAboard` | |
| `DessikClaim` | 8 | Dessik, Keeper, Taren, Sela | **Compulsory.** Radio, straight after `IloArrives` (`Npc.Say`). The claim read aloud; then the launch to the hull. | *hands.claimRead* is set by the node | |
| `DessikHall` | 5 | Dessik, Taren, Sela | **Compulsory.** Crossing point inside the freight hall, before part two | *hands.hallEntered* | |
| `DessikYields` | 7 | Dessik, Taren, Sela | **Compulsory.** Dessik, once `bossdown.DessikVarn`. Ends on Taren noticing the cabin alarm has stopped. | `hands.dessikYielded` | |
| `DessikHeld` | 1 | Dessik | Dessik in custody on the freight spine, after that | | |
| `PatrolBreachSealed` | 8 | Patrol, Taren, Sela | **Compulsory.** The captain at the sealed cabins, `hands.dessikYielded`. His last line is spoken over his shoulder, to Meret. | `hands.breachSealed` | |
| `PatrolRepeat` | 1 | Patrol | The captain at the seal until Meret has left | | |

### The dinner, and after

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `MeretArrives` | 9 | Meret, Taren, Sela | **Compulsory.** Meret behind the captain, `hands.breachSealed` | `hands.meretMet` | |
| `BinnaDinner` | 12 | Binna, Meret, Taren, Keeper | **Compulsory.** Binna at the galley hatch, `hands.meretMet`. The meal; the air. | `hands.dinnerSat` | |
| `MeretStillwater` | 8 | Binna, Meret, Taren | **Compulsory.** Meret at the table, `hands.dinnerSat`. Stillwater, explained to a cook. | `hands.stillwaterHeard` | |
| `RuvaDinnerNumber` | 11 | Ruva, Meret, Sela, Taren | **Compulsory.** Ruva at the table, `hands.stillwaterHeard`. The number; Sela half agrees aloud; the single navigator; Ruva's question. | `hands.numberHeard` | |
| `IloFootnotes` | 10 | Ilo, Meret, Ruva, Maru | **Compulsory.** Ilo at the table, `hands.numberHeard`. Maru speaks from where she stands. | `hands.footnotesTabled` | |
| `MeretSorrel` | 10 | Taren, Meret, Sela | **Compulsory.** Meret at the table, `hands.footnotesTabled`. The act turn. | `hands.sorrelNamed` | |
| `MeretDrawings` | 7 | Meret, Sela, Taren | **Compulsory.** Meret standing at the galley door, `hands.sorrelNamed`. The drawings; the thrashing road; forty days. | `hands.dinnerDone` | |
| `AbriDinnerTalk` | 1 | Keeper | Abri at the table, between `hands.dinnerSat` and `hands.dinnerDone` | | |
| `BinnaDinnerTalk` | 3 | Binna | Binna, the same window, after `BinnaDinner` | | |
| `MaruDinnerTalk` | 4 | Maru, Sela | Maru standing by the galley wall, the same window | | |
| `TeoDinnerTalk` | 1 | Teo | Teo at the table, the same window | | |
| `JaskDinnerTalk` | 7 | Jask, Meret | Jask at the table, the same window, only if `hands.jaskFound`. Meret must be near enough to answer. | | |
| `MeretParting` | 6 | Meret, Taren | Meret on the freight spine by her cutter, `hands.dinnerDone`, once. Optional: the Muster warning, and the rings. | `hands.partingHeard` | |
| `SelaLampGallery` | 9 | Taren, Sela | **Compulsory, chapter end.** At the chart table, `hands.dinnerDone` | `hands.chartRead`, `chapter5.complete` | |

### The hub: everyone's lines aboard

Each person has a first talk aboard, a line that holds for repeat visits, and a line for after the dinner (`postFlag hands.dinnerDone`). For six of them the first talk is one of the jobs above.

| Who | Where | First aboard | Repeat | After the dinner |
|---|---|---|---|---|
| Keeper | Dock, then her desk on the lamp gallery | `AbriHandsArrival` | `AbriHandsRepeat` (1) | `AbriHandsPost` (3) |
| Pell | Lamp gallery | `PellChartLamps` | `PellHandsRepeat` (1) | `PellHandsPost` (2) |
| Ruva | Garden | `RuvaMooring` | `RuvaHandsRepeat` (1) | `RuvaHandsPost` (2) |
| Teo | Garden deck | `TeoHands` (4) | `TeoHandsRepeat` (1) | `TeoHandsPost` (2) |
| Garo | Garden deck | `GaroHands` (1) | `GaroHandsRepeat` (1) | `GaroHandsPost` (1) |
| Dabb | Garden, at the pumps | `DabbHands` (1) | `DabbHandsRepeat` (1) | `DabbHandsPost` (1) |
| Jask | Garden racks, then where he is placed | `JaskFound` | the three `…Hand` lines | `JaskHandsPost` (1) |
| Binna | Galley | `BinnaVent` | `BinnaHandsRepeat` (1) | `BinnaHandsPost` (2) |
| Maru | Raft bay | `MaruMooring` | `MaruHandsRepeat` (1) | `MaruHandsPost` (2) |
| Ona | Raft bay, the weaving bench | `OnaHands` (2) | `OnaHandsRepeat` (1) | `OnaHandsPost` (1) |
| Pim | Raft bay | `PimHands` (2) | `PimHandsRepeat` (1) | `PimHandsPost` (1) |
| Hadda | Arcade, the pump-and-press | `HaddaHands` (3) | `HaddaHandsRepeat` (2) | `HaddaHandsPost` (2) |
| Corra | Arcade, her stall | `CorraHands` (5) | `CorraHandsRepeat` (1) | `CorraHandsPost` (1) |
| Dov | Arcade, the provisioner's counter | `DovHands` (3) | `DovHandsRepeat` (1) | `DovHandsPost` (2) |
| Ilo | A desk beside Abri's | `IloArrives` | `IloHandsRepeat` (1) | `IloHandsPost` (2) |
| Dessik | In custody on the freight spine | `DessikYields` | `DessikHeld` (1) | `DessikHeldPost` (2) |

Hadda's three nodes each end in the same options (press, repair, a leave line). Corra's and Dov's first talks offer shop or leave; their other nodes open the shop.

### Side quests

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `CorraKettleOffer` | 4 | Corra, Taren | Corra, met aboard, the three jobs done, `KettleTrade` not started | | `startQuest KettleTrade`, `shop` |
| `GaroKettle` | 1 | Garo | Garo, `KettleTrade` started and not `kettle.garden` | `kettle.garden` | |
| `OnaKettle` | 2 | Ona | Ona, `KettleTrade` started and not `kettle.bay` | `kettle.bay` | |
| `AbriKettle` | 2 | Keeper | Abri at her desk, `KettleTrade` started and not `kettle.cabins`. She takes cabin nine's. | `kettle.cabins` | |
| `CorraKettleReturn` | 2 | Corra | Corra, all three delivered | `kettle.reported` | `shop` |
| `IloLedgerOffer` | 5 | Ilo, Taren | Ilo, `hands.dessikYielded`, `ClaimsLedger` not started. Best held until `hands.dinnerDone` so it doesn't cut across the dinner. | | `startQuest ClaimsLedger` |
| `TeoLedger` | 1 | Teo | Teo, `ClaimsLedger` started and not `ledger.teo` | `ledger.teo` | |
| `MaruLedger` | 2 | Maru | Maru, `ClaimsLedger` started and not `ledger.maru` | `ledger.maru` | |
| `AbriLedger` | 2 | Keeper | Abri, `ClaimsLedger` started and not `ledger.abri` | `ledger.abri` | |
| `IloLedgerReturn` | 2 | Ilo | Ilo, all three told | `ledger.returned` | |

### Suggested routing (first match wins)

| Speaker | Beats, in order |
|---|---|
| Keeper | `AbriLedger`, `AbriKettle`, `AbriDinnerTalk`, `AbriCourier`; tiers: repeat `AbriHandsRepeat`, post `AbriHandsPost` |
| Ruva | `RuvaDinnerNumber` (`hands.stillwaterHeard`, not `hands.numberHeard`), `RuvaStowaway`, `RuvaMooringLost`; tiers: first `RuvaMooring`, repeat, post |
| Maru | `MaruDinnerTalk` (the dinner window, once), `MaruLedger`, `MaruMooringLost`; tiers: first `MaruMooring`, repeat, post |
| Binna | `BinnaDinner` (`hands.meretMet`, not `hands.dinnerSat`), `BinnaDinnerTalk` (the dinner window); tiers: first `BinnaVent`, repeat, post |
| Teo | `TeoDinnerTalk`, `TeoLedger`, `TeoStowawayDecide`, `TeoStowawayOffer`; tiers: first `TeoHands`, repeat, post. `LateMeetings`: `TeoStowawayOffer` once `hands.jaskFound`. |
| Jask | `JaskDinnerTalk` (the dinner window, once), the `JaskThanks…` node for his place (not `jask.thanked`); tiers by object: racks `JaskFound` / `JaskFoundRepeat`; placed `…Hand`; post `JaskHandsPost` |
| Ilo | `IloFootnotes` (`hands.numberHeard`, not `hands.footnotesTabled`), `IloLedgerReturn`, `IloLedgerOffer`; tiers: first `IloArrives`, repeat, post |
| Meret | `MeretStillwater`, `MeretSorrel`, `MeretDrawings`, `MeretParting`, each on the flag before it; first `MeretArrives` |
| Corra | `CorraKettleReturn`, `CorraKettleOffer`; tiers: first, repeat, post |
| Ona, Garo | `OnaKettle`, `GaroKettle`; tiers: first, repeat, post |
| Pell, Hadda, Dov, Pim, Dabb, Dessik, Patrol | tiers only |

## 3. Flags

**From the brief, and who sets them:** `hands.aboard` (`AbriHandsArrival`), `hands.mooringSettled` (`SelaMooringRail`), `hands.ventFixed` (`BinnaVent`), `hands.chartOpen` (`SelaChartOpen`), `hands.jaskFound` (`JaskFound`), `hands.iloAboard` (`IloArrives`), `hands.feet1..4` (world), `hands.hullCleared` (world), `hands.dessikYielded` (`DessikYields`), `hands.breachSealed` (`PatrolBreachSealed`), `hands.meretMet` (`MeretArrives`), `hands.dinnerDone` (`MeretDrawings`), `hands.sorrelNamed` (`MeretSorrel`), `chapter5.complete` (`SelaLampGallery`).

**Added by the script:** `hands.ruvaHeard`, `hands.maruHeard`, `hands.mooringForRoots`, `hands.mooringForTides`, `ruva.mooringGrumbled`, `maru.mooringGrumbled`, `hands.claimRead`, `hands.dinnerSat`, `hands.stillwaterHeard`, `hands.numberHeard`, `hands.footnotesTabled`, `hands.partingHeard`, `hands.chartRead`, `hands.ruvaOnJask`, `hands.jaskPlaced`, `jask.worksGarden`, `jask.worksFerry`, `jask.worksLamps`, `jask.thanked`, `kettle.garden`, `kettle.bay`, `kettle.cabins`, `kettle.reported`, `ledger.teo`, `ledger.maru`, `ledger.abri`, `ledger.returned`.

**Suggested for the world to own:** the italic flags in section 2, and the three sighted gallery lamps.

## 4. Quests

Kinds are `ObjectiveKind` (TalkTo 0, Reach 1, Kill 2, Collect 3, Interact 4, Flag 5). Rewards are the brief's. `ManyHands` is the main quest (`main: true`, `chapter: 5`).

### ManyHands - Many Hands (main)

Description: `The convoy has become one ship with a name on its dock door, and nobody has yet made it a town.`

| # | Kind | Target | Step text | Points at |
|---|---|---|---|---|
| 0 | Reach | `ManyHands` | Dock at Many Hands | ManyHands |
| 1 | Flag | `hands.mooringSettled` | Settle where the rafts hold on: hear Ruva and Maru out | speakers `Ruva`, `Maru`, then the hold wall |
| 2 | Flag | `hands.ventFixed` | Fix the galley vent for Binna | speaker `Binna` |
| 3 | Flag | `hands.chartOpen` | Set the chart table by Pell's lamps on the lamp gallery | speaker `Pell`, then the chart table |
| 4 | Flag | `hands.iloAboard` | Meet the courier asking for you at the dock | speaker `Ilo` |
| 5 | Kill | `Hulljack` | Cut the salvage frame's four feet off the hull | ManyHandsHull, encounter `Hulljack` |
| 6 | Kill | `DessikVarn` | Stop Dessik Varn in the freight hall | ManyHands, encounter `DessikVarn` |
| 7 | Flag | `hands.breachSealed` | Get to the breached cabins | speaker `Patrol` |
| 8 | Flag | `hands.meretMet` | Hear what Director Venn has come for | speaker `Meret` |
| 9 | Flag | `hands.dinnerSat` | Find Director Venn something to eat | speaker `Binna` |
| 10 | Flag | `hands.stillwaterHeard` | Let her explain Stillwater | speaker `Meret` |
| 11 | Flag | `hands.numberHeard` | Hear the number | speaker `Ruva` |
| 12 | Flag | `hands.footnotesTabled` | Let Ilo answer Ruva | speaker `Ilo` |
| 13 | Flag | `hands.sorrelNamed` | Ask what the anchor costs | speaker `Meret` |
| 14 | Flag | `hands.dinnerDone` | See Director Venn to the galley door | speaker `Meret` |
| 15 | Flag | `hands.chartRead` | Read the chart on the lamp gallery | the chart table |

Steps 1 to 3 are flags, so the three jobs can be done in any order (`QuestService.CatchUp`). The brief's "dinner" is steps 8 to 14, one per talk, so the objective line always names who to walk to. Reward: XP, scrip, a T3 part choice.

### KettleTrade - Somebody Has to Sell a Kettle (Corra)

Description: `Corra Vey has three kettles paid for and nobody to carry them the length of the ship.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Flag | `kettle.garden` | Take a kettle to Marrow-1's crew on the garden deck | Kettle for the garden deck (waits on `Garo`) |
| 1 | Flag | `kettle.bay` | Take a kettle to the weaving bench in the raft bay | Kettle for the weaving bench (waits on `Ona`) |
| 2 | Flag | `kettle.cabins` | Take cabin nine's kettle to Keeper Abri's desk | Kettle for cabin nine (waits on `Keeper`) |
| 3 | Flag | `kettle.reported` | Tell Corra all three are delivered | Tell Corra they're delivered (waits on `Corra`) |

Any order. Reward: the stall discount ("stall price is yours from now on") and a module ("it came in a crate of kettles").

### StowawayFare - The Stowaway's Fare (Teo)

Description: `Jask rode up from Rootmarket in Marrow-3's seed racks, and a passage has a price.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Flag | `hands.ruvaOnJask` | Hear what Ruva has to say about Jask | Hear Ruva on Jask (waits on `Ruva`) |
| 1 | Flag | `hands.jaskPlaced` | Settle with Teo where Jask works off his passage | Settle it with Teo (waits on `Teo`) |
| 2 | Flag | `jask.thanked` | See how Jask is getting on | See how Jask is doing (waits on `Jask`) |

Reward: a Fortune token, and Jask's thanks, loudly, at step 2.

### ClaimsLedger - The Claims Ledger (Ilo)

Description: `Three hulls in Dessik Varn's ledger belong to people standing aboard Many Hands.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Flag | `ledger.teo` | Tell Teo his seed barges are in the ledger | Tell Teo (waits on `Teo`) |
| 1 | Flag | `ledger.maru` | Tell Maru her raft is in the ledger | Tell Maru (waits on `Maru`) |
| 2 | Flag | `ledger.abri` | Tell Keeper Abri that Tallow is in the ledger | Tell Keeper Abri (waits on `Keeper`) |
| 3 | Flag | `ledger.returned` | Bring the three statements back to Ilo | Bring the statements to Ilo (waits on `Ilo`) |

Any order. Reward: scrip; three households keep their ships in the epilogue.

## 5. Display names

### New speakers (`CharacterDef.displayName`)

| Speaker id | Display name | Sheet |
|---|---|---|
| `Meret` | Meret Venn | New. See below. |
| `Dessik` | Dessik Varn | New. Uses Neutral, Smug, Determined, Angry, Sad, Flustered, Amused. |
| `Patrol` | Steady Hand | As chapter 3: `Neutral` only, all six lines. A one-cell set still passes. |

Everyone else speaks on a sheet they already have: Keeper, Pell, Corra, Ilo (chapters 1 and 2); Ruva, Teo, Binna, Jask, Hadda, Garo, Dabb (chapter 3); Maru, Ona, Pim, Dov (chapter 4). No speaker is introduced beyond the brief's.

### Meret's sheet

She has 45 lines and uses **five** expressions. Nothing else is asked of the sheet in this chapter.

| Expression | Lines | What it is on her |
|---|---|---|
| Neutral | 23 | Level and attentive; the face she listens with. Both "Yes." lines, the whole of the Sorrel talk bar one line, and "The routes can't." |
| Tender | 8 | Regard for good work, and for people she is about to disappoint. "You came home with the weavers." "I would trade." "It was never hidden, Ilo." "You can't mend what nobody has told you is broken." |
| Amused | 7 | One corner of the mouth; the eyes stay tired. "He is usually nearest. I've stopped asking how." "It would fit on a ladle" is set up by it. |
| Sad | 5 | Not grief: somebody reading out a figure she dislikes and won't round. "I've spent nine years trying to make it four." "You are the first two people to ask me about it." "Being right costs a nursery." |
| Curious | 2 | Inspecting a piece of work: head tilted, stylus stopped. The vent mat; Taren's rings. |

She is never Angry, Smug, Shocked, Flustered, Intense, Playful, Worried, Determined or plainly Happy. If one cell gets the most care, make it Neutral: it carries the act turn.

### Other names and words

| Word | What |
|---|---|
| the name on the dock door | Pell's notice: "Many hands make light work. Lamp trimmers wanted, ask Pell." The cargo door slid across all but the first two words. |
| clause nine | The registry clause Dessik files under; Meret's own registry |
| section eleven | Third volume of Stillwater's engineering: the loss of Sorrel's nursery, published six years ago |
| the footnotes | Ilo's case: the settlements outside the capacity figure. He adds nothing to them in this chapter; chapter 6 has his first entry. |
| running rings | Taren's mooring: rings on a rail, a hand's play at each stop |
| the route chart | Sela's chart table once it has taken Pell's three lamps |
| cabin nine | Still never seen. In the breached section, alive, asking about supper; orders a kettle in writing. |

### Bosses (`EnemyDef.displayName`)

| Id | Display | Note |
|---|---|---|
| `Hulljack` | HULLJACK | The salvage frame on the hull, part one. Epithet: `A salvage frame with a filed claim on your hull`. Break targets: `Magnetic foot`. |
| `DessikVarn` | DESSIK VARN | Part two, in the freight hall. Epithet: `Licensed claimant, in a hired suit`. Yields. |

The barks expect exactly these two ids.

### Zones (`ZoneDef.displayName`)

| Id | Display |
|---|---|
| `ManyHands` | Many Hands |
| `ManyHandsHull` | The Hull of Many Hands |

### Floor signs and landmark labels

`DOCK`, `FREIGHT SPINE`, `FREIGHT HALL`, `ARCADE`, `GALLEY`, `GARDEN`, `RAFT BAY`, `LAMP GALLERY`, `CABINS`, `CHART TABLE`.

### Prompts

`Look at the dock door`, `Fit the running rings`, `Sight the gallery lamp`, `Open the route chart`, `Look behind the seed racks`.

### Items

The script names none. Corra's module "came in a crate of kettles"; the kettles themselves are carried in words only.

## 6. Objective strings

Where the panel takes its line from the main quest's current step, section 4's step text is what shows. These are the rows the steps can't say.

| When | Label |
|---|---|
| On the hull, part one | Cut the feet off the salvage frame (1/4) |
| In the freight hall, part two | Break the suit, then strike the cockpit |
| Part two, once the alarm sounds | Stop Dessik Varn: the cabins are open and you can't reach them |
| `hands.dessikYielded`, not `hands.breachSealed` | Get to the cabins |
| Between `hands.sorrelNamed` and `hands.dinnerDone` | She is at the galley door |
| `chapter5.complete` | Chapter 5 complete. The nursery's only way out runs cold, and nobody alive knows how to cross it. |

Chapter-end card (`QuestToast`): heading `CHAPTER 5 COMPLETE`, title `MANY HANDS`, body `Many Hands is a town, with a chart that can take it anywhere it has been. Director Venn's anchor will be braced on Sorrel, and she has given you forty days and her own drawings to find something better. Until then nothing closes behind you, so keep exploring.`

## 7. Barks added

Existing entries are untouched. New keys on `Taren` and `Sela`; `Dessik`, `Keeper` and `Patrol` are new speakers in the file.

| Speaker | Key | Used by | Wiring |
|---|---|---|---|
| Taren, Sela | `bossphase.Hulljack`, `bossopening.Hulljack` | `BossController.Situation` | Automatic for id `Hulljack`: it lurches to another join; a foot plants |
| Taren, Sela | `bossphase.DessikVarn`, `bossopening.DessikVarn` | `BossController.Situation` | Automatic for id `DessikVarn`: he starts throwing containers; the suit kneels |
| Taren, Sela | `foot` | A foot is cut | Three lines in turn, none tied to a count |
| Taren, Sela | `breach` | The cabin alarm, during part two | Once |
| Dessik | `foot` | A foot is cut | Four lines in turn, one for each |
| Keeper | `hail.breach` | The cabin alarm | Once, with the alarm |
| Patrol | `hail.latched` | Some seconds after the alarm | Once: "No declaration is needed." It answers his own line from chapter 3. |

## 8. What the script assumes

**No new commands or dialogue features.**

1. **Two portrait sets.** Meret and Dessik. Section 5 says which expressions each uses.
2. **Tests to extend.** `DialogueInputTests`: add `Meret` and `Dessik` to the speaker-prefix list (the rest are there once chapters 3 and 4 are); to `branches`: `SelaMooringRail` {world, world}; `TeoStowawayDecide` {world, world, world}; `HaddaHands`, `HaddaHandsRepeat`, `HaddaHandsPost` {bench, repair, world}; `CorraHands` and `DovHands` {shop, world}. `CorraHandsRepeat`, `CorraKettleOffer`, `CorraKettleReturn`, `CorraHandsPost`, `DovHandsRepeat` and `DovHandsPost` end in the shop. `QaPlanWorldTests`: the quest-starter table gets (`ManyHands`, `AbriHandsArrival`), (`KettleTrade`, `CorraKettleOffer`), (`StowawayFare`, `TeoStowawayOffer`), (`ClaimsLedger`, `IloLedgerOffer`); the ninety-second list gets the nodes marked compulsory. `PeopleMarks.Repeats` gets `AbriCourier` and the five `…DinnerTalk` nodes if they are left repeatable.
3. **Two counters and a bench.** Corra sells parts (`ManyHands_Corra`), Dov provisions (`ManyHands_Dov`), under `ShopUi.For`. Hadda keeps the pump-and-press and the repair cradle and sells nothing, so her third option is a leave line.
4. **Who is at the table.** Meret, Ruva, Ilo, Abri, Teo and (if found) Jask seated; Binna at the hatch; Maru standing; the party. Fourteen places laid, by Teo's count. Everyone else keeps their post and their ordinary line.
5. **The route chart.** `SelaChartOpen` sets `hands.chartOpen`; the chart table then opens the route chart screen. The script has no command for it and needs none.
6. **Three gallery lamps to sight** between `PellChartLamps` and `SelaChartOpen`, and **the running rings to fit** in `SelaMooringRail` (the talk is the fitting).
7. **Jask in four places.** The seed racks until placed; then the garden, the raft bay ferry or the lamp gallery by flag; at the table during the dinner if found.
8. **Meret off screen until `PatrolBreachSealed` ends.** The captain's last line is said to her. She leaves the ship after `MeretParting`, or when the chapter closes if the player never walks down.
9. **The alarm can't be answered.** The party is held in the freight hall; `hail.breach`, then `hail.latched`, are the only news until Dessik yields. Taren's last line in `DessikYields` is that the alarm has stopped.
10. **The shared `boss` bark** after part two would land on top of Dessik's yield. Leave it out for `DessikVarn`.
11. **Order-proofing.** `BinnaVent`'s line about the bowl and `TeoHands` both hold if Jask is already found only because `TeoStowawayOffer` takes Teo's first meeting in that case; Binna's line was written to hold either way.

## 9. Where the script departs from the brief

- **Pell chalked the name.** It was a notice asking for lamp trimmers, and nobody came about the lamps. It is played for the joke.
- **Who is aboard.** Ona came ("the racks came up last; so did I"). Hadda keeps the bench, built from Quill's drawings and her own press. Dov has the provisioner's counter. Garo and Dabb are on the garden deck.
- **Meret says "Yes." twice** (section 1).
- **The nursery's loss was published.** Section eleven, six years ago, in plain words; the party are the first two people to ask her about it. She has never hidden it, which is the brief's point about her and chapter 10's line ("I never hid the number").
- **Half the power.** Her chapter 1 conditions were food, power and a route that waits. She counts the food, the route and half the power: a fourth thing she is right about, and chapter 6's engines answer it.
- **Hulljack's clause is the Compact's own registry.** Meret says so first and will have it struck, in sixty days.
- **The Muster warning is in an optional parting talk**, and Abri repeats it afterwards (the captain left a list), so it is heard either way.
- **The numbers in section 1** are the script's.
- **Ilo adds nothing to the footnotes yet.** Driftreed has no page and he has "left room", so chapter 6's first entry is still his first.
- **`StowawayFare` gains a third step** (see Jask afterwards) so the thanks is the end of the quest.
- **The three hulls in the ledger** are Teo's seed barges, Maru's raft and Tallow Drift. **The three kettles** go to Marrow-1's crew, the weaving bench and cabin nine by way of Abri.
- **The chapter ends on the herders unnamed.** Sela reads a margin note: herders on an ice world who "keep the long sleep". Chapter 6 opens with her naming them.
