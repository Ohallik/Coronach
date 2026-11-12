# Chapter 9 script manifest - The House That Can Leave

For the engineers wiring chapter 9. Script: [Home.yarn](Home.yarn). Barks: [barks.json](../barks.json). Brief: [ch09-the-house-that-can-leave.md](../../chapters/ch09-the-house-that-can-leave.md). Earlier manifests, in the same shape: [chapter 3](../ch03-manifest.md), [chapter 4](ch04-manifest.md), [chapter 5](ch05-manifest.md), [chapter 6](ch06-manifest.md), [chapter 7](ch07-manifest.md), [chapter 8](ch08-manifest.md).

**What is here:** 104 nodes, 271 dialogue lines, 3 option labels (274 strings). Compiled clean with the project's own compiler (Yarn Spinner 3.2.4, the nine live files and the seven pending ones together: 0 errors, 561 nodes, no duplicate titles). No new Yarn commands: `setFlag`, `startQuest`, `shop`, `bench`, `repair`.

**This file waits in `pending/`** until the chapter's people and quests are defined: three new portrait sets (Dorrow, Tam, Jun) and the rest of Bryn Asta's, five `QuestDef`s, and the expectations in section 8.

**Nearly half the nodes are one line long** (47 of 104). The Coronach is 52 nodes and 68 lines: 8 doors, 7 guests, 3 last words, a call, a close and 32 extras. Nothing in it is longer than three lines. That is the pacing the music asks for.

## 1. Conventions this file follows

- Every title starts with its owning speaker id. New: `Dorrow…`, `Tam…`, `Jun…`. Dace's are `Survivor…`, as in chapter 1. `Taren…` is used once, for his own door; `Sela…` as before; `Abri…` is speaker `Keeper`.
- Each quest is started by exactly one node. Talks that complete a step set a flag for a `Flag` step. Talks at a point offered only at a quest's step (`SelaWeddingTable`, `TamDoorMarks`, `TamLeaningTree`, `TamWornStep`) set nothing, so the point's `report` reaches its `Interact` step.
- **No Coronach door sets a flag.** Each is a discovery point, and the point's own flag is the world's (section 3). That keeps all of them clear of the discovery-report trap.
- **Nobody counts days.** Meret stopped at the end of chapter 8. The chapter has no number in it.
- Checked against the word tests: no retired phrase, no id-shaped word, at most three options on any node.

### Who has been moving the stall

In chapter 1's first scene the stall over Taren's yellow line is Mira's, and she says so: "It did stop. Then the pears needed room." She sells things that spark. She has never said the pears were hers.

They are **Jun's**. Jun has sold pears off the walkway end of Mira's pitch for thirty-one years, and every morning before the shift bell she sets them one crate past the line. Mira has answered for it since the first scene because Mira is the one who talks. Chapter 1 stands exactly as written; Jun was behind the pears.

| Where it comes out | Line |
|---|---|
| `JunFirst` | "The pears are on your line. I'm behind the pears." |
| `MiraManifest` | "Where's Jun? She has sold pears off the end of my stall for thirty-one years." |
| `MiraStallDoor` | "And every morning I told that man the pears needed room. They did. They weren't even my pears." |
| `JunLineDoor` | "Every spring somebody painted you fresh. Nobody ever moved a crate back." |

Chapter 10's epilogue has the fruit seller across the line again. That is Jun, and a crate of pears.

### First lines and last lines

Each of home's people ends on something they said in chapter 1.

| Who | Chapter 1 | Chapter 9 |
|---|---|---|
| Mira | "Did the walkway just move?" | The same line, as the market lifts (`NeveFirstOut`). Taren: "All of it. On purpose." |
| Mira | "If it sparks, I sell it." "The pears keep a schedule." | "If it sparked, I sold it off you." Forty-one lots of other people's belongings and nine of her own. |
| Orrin | "Everything's under the evacuation notices." "I'll put that on the board, and nobody will believe it." | The separation survey was under them (`OrrinPostponed`). "Cinder Halo: departed, all sections. Nobody will believe it." Then: "I brought the chalk." |
| Hal | "Hold still a moment." "A hurry's harder to fix." "Easy on the landing." | "Hold still a moment." to Taren, who does (`HalLastMachine`). "You were the one thing in this town that made them hold still." "Easy on the landing." to Meret. |
| Hal | "Just visiting." "Good. Do that more often." | "Good. You did say you would, more often." |
| Neve | "Brake before you turn." "Did it arrive warm?" "That's the nicest thing anyone's said to me all cycle." | All three, the last two about her own letter. |
| Oda | "Everything we own is in this hauler, and most of it's strapped to the outside." "My mother says I'm morbid." "Line nine hundred and four. Mother's had it framed." | "Hold on to something that's strapped to the inside." "Morbid. I was right about the door, and I'd give the hauler to have been wrong." The frame comes; the line doesn't. |
| Ilo | "They're in the footnotes. I didn't write the footnotes." "The capacity line is the short one." | "I didn't write them. I'm tired of saying that." He reads the room's names, and keeps the pins. |
| Dace | "Tell her to stop sending socks." | "I complained every time. I'm wearing two pair." |
| Sela | "I've got a receipt for one that left an hour ago." | "It's the best thing a door has ever done for me. I still have the receipt." |
| Taren | "A little. I hadn't tightened that joint yet." | "You weren't failing. You were telling me which way you wanted to go." |

### The meeting is eight talks

Each is short, each sets its own flag, and the next is with somebody else in the room, so the player crosses the floor between them. None runs past six lines; the longest, `OrrinPostponed`, is 29 seconds.

| # | Node | Walk up to | Sets |
|---|---|---|---|
| 1 | `OrrinMeetingOpens` | Orrin, at the board | `home.meetingOpened` |
| 2 | `MeretBerthOffer` | Meret, standing, at the front | `home.offerMade` |
| 3 | `HalApprentices` | Hal, with nine apprentices behind him | `home.halAsked` |
| 4 | `MiraManifest` | Mira, who has the bound copy by the end of it | `home.manifestShown` |
| 5 | `IloReadsNames` | Ilo, with the footnotes | `home.namesRead` |
| 6 | `OdaStands` | Oda, in the third row. She stands on her first word. Dace's ten stand at the door; Jun stands and says nothing. | `home.odaStood` |
| 7 | `OrrinPostponed` | Orrin | `home.orrinSaid` |
| 8 | `MeretWithdraws` | Orrin again: the hands, then Meret | `home.berthRefused`, `home.meetingHeld` |

Taren has one line in all of it and Sela and Idra have none. It is the Halo's room.

### The Coronach

No combat, no timer, no bark, no prompt to hurry. `OrrinCallsCoronach` opens it. Then:

1. **Eight doors, in any order.** Seven are needed (Dace's is not). Each is a neighbour standing at a place, speaking to the place, not to the party.
2. **Seven guests at the edge**, one line each, optional, in any order.
3. **Thirty-two extras**, one line each, each beside a door or a guest, each there only if its side story was finished (table below).
4. When the seven are heard, **Taren's, Sela's and Idra's** open, in that order or any.
5. When those three are heard, `OrrinGiveItRoom` closes it, from wherever Orrin is standing, and the last joint opens.

| Door | Who | Where | Node | Lines |
|---|---|---|---|---|
| The stall | Mira | Pitch nine, crated | `MiraStallDoor` | 3 |
| The board | Orrin | The dock office board | `OrrinBoardDoor` | 2 |
| The bench | Hal | Four bolt holes in his workshop floor | `HalBenchDoor` | 2 |
| Lock nine | Neve | The airlock she flies from | `NeveLockDoor` | 2 |
| The dock office | Oda | The stamping window | `OdaOfficeDoor` | 3 |
| The yellow line | Jun | The walkway end of the stall | `JunLineDoor` | 3 |
| The notice board | Ilo | Where the capacity line was pinned | `IloNoticeDoor` | 2 |
| The dock (not needed) | Dace | The dock rail | `SurvivorDockDoor` | 2 |

| Guest | Node | Stands |
|---|---|---|
| Pell | `PellEdge` | Under a lamp she has lit over the dock office |
| Ruva | `RuvaEdge` | By the fruit stall |
| Maru | `MaruEdge` | At the walkway's end |
| Hask | `HaskEdge` | Across from the board |
| Tovra | `TovraEdge` | By Hal's workshop door |
| Dorrow | `DorrowEdge` | Anywhere, with his goggles pushed up |
| Abri | `AbriEdge` | By the notice board, writing |

| Last | Node | Where |
|---|---|---|
| Taren | `TarenWalkway` | The walkway joint from the first scene |
| Sela | `SelaGateSix` | The departure gate she missed |
| Idra | `IdraNoDoor` | Wherever she is standing. She has no door. |
| Orrin closes | `OrrinGiveItRoom` | |

**"Give it room."** Orrin says it, as a dock order, to get people back from the joint. He does not know where it comes from. Nobody looks up. Please don't stage a reaction.

#### The extras

Each plays at its speaker's place in the walk, after that speaker's own line if they have one. Each needs `home.coronachCalled` and its quest's flag. Every side quest written from chapter 1 to chapter 9 has one, except `Hushwell`, which I could not place.

| Node | Lines | Speaker | Needs | Beside |
|---|---|---|---|---|
| `MiraDoorCrystals` | 1 | Mira | `quest.MiraCrystals.complete` | Her stall |
| `SurvivorDoorSocks` | 1 | Dace | `quest.NeveParcel.complete` | The dock |
| `SurvivorDoorBeacons` | 1 | Dace | `quest.SorrelBeacons.complete` | The dock |
| `OdaDoorStamp` | 1 | Oda | `quest.OdaBerth.complete` | The dock office |
| `PellDoorMothersLamp` | 1 | Pell | `quest.PellsLamp.complete` | Her lamp |
| `IshenDoorRecord` | 1 | Ishen | `quest.IshensRecord.complete` | A crate, writing |
| `BrekkDoorWalked` | 1 | Brekk | `quest.UnswornRations.complete` | With Tovra |
| `BinnaDoorBaskets` | 1 | Binna | `quest.ThreeBaskets.complete` | A pot on a crate, by the stall |
| `RuvaDoorSenn` | 1 | Ruva | `quest.MooringPost.complete` | Where she stands |
| `JaskDoorQuestion` | 1 | Jask | `quest.Questions.complete` | The promenade stair |
| `TeoDoorTags` | 1 | Teo | `quest.ClaimTags.complete` | The dock rail, counting |
| `MaruDoorLamps` | 1 | Maru | `quest.BeaconKeeper.complete` | A lamp at each end of the walkway |
| `DovDoorEel` | 1 | Dov | `quest.SmokehouseOrder.complete` | A tray nobody has touched |
| `DovDoorBell` | 1 | Dov | `quest.SchoolBell.complete` | The same place; a bell is heard once |
| `IshenDoorQuill` | 1 | Ishen | `quest.FourthPump.complete` | His crate |
| `CorraDoorKettles` | 1 | Corra | `quest.KettleTrade.complete` | A handcart of kettles |
| `JaskDoorFare` | 1 | Jask | `quest.StowawayFare.complete` | The stair |
| `IloDoorLedger` | 1 | Ilo | `quest.ClaimsLedger.complete` | The notice board |
| `HaskDoorStrays` | 1 | Hask | `quest.StrayBeasts.complete` | Where he stands |
| `IruDoorLamp` | 1 | Iru | `quest.ShrineLamps.complete` | Beside Pell's lamp, with one of her own |
| `IdraDoorCrew` | 1 | Idra | `quest.CutterDown.complete` | With her own line; Kott and Sibb are in view |
| `RuvaDoorGardener` | 1 | Ruva | `quest.EmptyGardens.complete` | The gardener stands by the fruit stall, its light low |
| `IshenDoorMoons` | 1 | Ishen | `quest.MoonNames.complete` | His crate |
| `PellDoorOriel` | 1 | Pell | `quest.LumesLantern.complete` | A second lantern, carried down from the gallery |
| `TovraDoorPikes` | 1 | Tovra | `quest.QuietHands.complete` | Three smiths, each with a young pikeguard |
| `AbriDoorLanterns` | 1 | Abri | `quest.RelayGlass.complete` | The notice board |
| `BrekkDoorKettle` | 1 | Brekk | `quest.OdrunsKitchen.complete` | Holding a kettle |
| `PipDoorFile` | 1 | Pip | `quest.PipsOath.complete` | At attention, alone, by the drain |
| `MiraDoorTable` | 1 | Mira | `quest.SecondInventory.complete` | A table top is carried past in two halves |
| `HalDoorBenches` | 1 | Hal | `quest.PortableBenches.complete` | The bolt holes |
| `NeveDoorLetter` | 1 | Neve | `quest.ReturnAddress.complete` | Lock nine |
| `TamDoorEdge` | 1 | Tam | `quest.StillGround.complete` | With Dorrow |

Jask's two are all questions, as he always is. The gardener and the bell have no line: Ruva and Dov read them.

### The pencil

`BrynChartLesson`. Sela starts to explain how she reads a moving road, finds she can't, and hands one of Bryn's navigators her pencil. Bryn: "That's your pencil." "It's a pencil." The navigator draws the bend a finger early and Sela lets it stand. She does not take the pencil back, in this scene or after: in `OrrinChalk` it is the navigator who draws the ring. Nothing is said about minding or not minding.

### Numbers and names the script fixes

| What | Value | Said by |
|---|---|---|
| Nacre | Four hundred hulls on Bryn's tethers; nineteen years on the approach; six navigators in a pod built for two | Bryn |
| Aboard Many Hands after Nacre | Four hundred and eleven more | Abri |
| The Meridian Crawl | Two hundred years without stopping; tread-hall number four; nine kitchens and a schoolroom, everyone walked out; twenty engineers and Tam come | Maru, Dorrow |
| Mira's stock | Forty-one lots of other people's, nine of her own; three disputed | Mira |
| Hal's apprentices | Nine: three born on the ring, six off the convoy. The berth includes three. Ket, Lo and Marn get the folding benches. | Hal, Meret |
| Jun | Thirty-one years at the end of Mira's pitch | Mira, Jun |
| Taren | Eleven years of tightening the joints | Taren, Dorrow |
| Orrin's survey | Fourteen months under the evacuation notices | Orrin |
| The vote | Four hands for | Orrin |
| The footnotes read | One unregistered trader; six apprentices; ten of the Sorrel outpost; line nine hundred and four | Ilo |
| The bench | Nine pieces | Hal |
| Places | Pitch nine; lock nine; gate six | Mira, Neve, Sela |
| The joint | "Has waited a hundred years" | Orrin |

## 2. Nodes

Lines are dialogue lines (options not counted). "Compulsory" marks the main path; the longest, `DorrowFirst`, is 8 lines and 41 seconds by the ninety-second test's arithmetic.

### Harrowgate, by radio

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `AbriTurnsForNacre` | 4 | Keeper, Idra, Sela | Abri | **Compulsory.** At once after `chapter8.complete`, on the harbour. Abri by radio. | | `startQuest HouseLeaves` |

### Nacre Grave (flight; radio)

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `BrynNacreApproach` | 7 | Bryn, Taren, Idra | Bryn | **Compulsory.** Crossing point at the outer marker. Teaches the spiral and names the three lamps. | `home.nacreSpoken` | |
| `OdaFound` | 8 | Oda, Taren | Taren | **Compulsory.** At Oda's hauler. | `home.odaFound` | |
| `BrynWelcomeBack` | 5 | Bryn, Idra | Bryn | **Compulsory.** At the tug. | `home.tugFound` | |
| `BrynNavigators` | 4 | Bryn, Sela | Sela | **Compulsory.** At the navigators' pod. The chart on the hatch. | `home.navigatorsFound` | |
| `NeveReturnOffer` | 4 | Neve, Taren | | Neve's ship in the outer spiral, `home.nacreSpoken`, `ReturnAddress` not started | | `startQuest ReturnAddress` |

### Many Hands

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `MeretBerthNotice` | 5 | Meret, Keeper | Meret | **Compulsory.** Crossing point on the dock, all three lamps found. Meret by radio; Abri is standing there. | `home.noticeHeard` | |
| `BrynChartLesson` | 7 | Bryn, Sela | Sela | **Compulsory.** At the chart table. The pencil. | `home.chartLent` | |
| `IdraTownMovers` | 5 | Idra, Maru | Idra | **Compulsory.** Idra in the raft bay, `home.chartLent`. Maru names the Crawl. | `home.crawlNamed` | |
| `OdaAboard` | 1 | Oda | | Oda, in her cabin aboard, until the Halo | | |
| `DraskCount` | 3 | Drask, Idra | | Drask, in custody, once: `home.odaFound`, not `drask.asked` | `drask.asked` | |
| `AbriNamesOnTheWall` | 2 | Keeper | | Abri, `home.noticeHeard` | | |

### Duskline

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `DorrowFirst` | 8 | Dorrow, Idra, Taren | Dorrow | **Compulsory.** Dorrow at the Crawl's helm, first node. The brief's line. The run opens on it. | `home.crawlAsked` | |
| `DorrowRepeat` | 1 | Dorrow | | Dorrow's repeat until the section is in | | |
| `TamFirst` | 4 | Tam, Sela, Taren | | Tam on the market street. Optional. | | |
| `DorrowTreadHome` | 4 | Dorrow, Taren | Dorrow | **Compulsory.** As the section rejoins. | `home.treadRecovered` | |

The run against the dawn has no node. It is barks (section 7).

### The Decks, on separation day

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `OrrinHomecoming` | 5 | Orrin, Taren | Orrin | **Compulsory.** Orrin at the board, `home.treadRecovered`. He is tempted and says so. | `home.returned` | |
| `JunFirst` | 3 | Jun, Taren | | Jun, at the walkway end of the stall, first node | | |
| `JunRepeat` | 1 | Jun | | Jun's repeat | | |
| `MiraInventoryOffer` | 5 | Mira, Taren | | Mira, `home.returned`, `SecondInventory` not started. Her first node on the day. | | `startQuest SecondInventory`, `shop` |
| `MiraCratedRepeat` | 1 | Mira | | Mira's repeat on the day | | `shop` |
| `MiraInventoryReturn` | 3 | Mira | | Mira, `SecondInventory` at step 1 | `mira.inventorySettled` | `shop` |
| `HalCrated` | 3 | Hal | | Hal, `home.returned`, until his bench comes down. Options: bench, repair, or just visiting. | | `bench`, `repair` |
| `HalBenchesOffer` | 4 | Hal, Taren | | Hal, `home.returned`, `PortableBenches` not started | | `startQuest PortableBenches` |
| `HalBenchesReturn` | 2 | Hal | | Hal, `PortableBenches` at step 1 | `hal.benchesLit` | |
| `NeveFlyingAStreet` | 2 | Neve | | Neve at lock nine, `home.returned`, until the launch | | |
| `NeveReturnAnswer` | 3 | Neve, Taren | | Neve, `ReturnAddress` at step 3 | `neve.letterAnswered` | |
| `SurvivorDecks` | 2 | Survivor | | Dace on the dock with his crew, first node on the day | | |
| `SurvivorNevesLetter` | 4 | Survivor, Taren | | Dace, `ReturnAddress` at step 2 | `dace.letterRead` | |
| `OdaDecks` | 1 | Oda | | Oda at the dock office, until the meeting | | |
| `IloDecks` | 1 | Ilo | | Ilo at the notice board, until the meeting | | |
| `DorrowListens` | 1 | Dorrow | | Dorrow in a service hatch, with his ear to the deck | | |
| `TamStillOffer` | 3 | Tam, Sela | | Tam on the promenade, `home.returned`, `StillGround` not started | | `startQuest StillGround` |
| `TamDoorMarks` | 3 | Tam, Taren | | Interaction at a marked doorframe, `StillGround` at step 0 (`report StillThing`) | | |
| `TamLeaningTree` | 3 | Tam, Sela | | Interaction at Jun's pear tree, the same | | |
| `TamWornStep` | 3 | Tam, Taren | | Interaction at a worn stair, the same | | |
| `TamGoesBack` | 4 | Tam, Taren | | Tam, `StillGround` at step 1 | `tam.goingBack` | |

### The town meeting

All compulsory; section 1 has the order and where each speaker stands.

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `OrrinMeetingOpens` | 3 | Orrin | Orrin | **Compulsory.** | `home.meetingOpened` | |
| `MeretBerthOffer` | 5 | Meret, Taren | Meret | **Compulsory.** | `home.offerMade` | |
| `HalApprentices` | 5 | Hal, Meret | Hal | **Compulsory.** | `home.halAsked` | |
| `MiraManifest` | 5 | Mira, Meret | Mira | **Compulsory.** | `home.manifestShown` | |
| `IloReadsNames` | 4 | Ilo | Ilo | **Compulsory.** | `home.namesRead` | |
| `OdaStands` | 4 | Oda, Survivor, Mira | Oda | **Compulsory.** | `home.odaStood` | |
| `OrrinPostponed` | 5 | Orrin | Orrin | **Compulsory.** | `home.orrinSaid` | |
| `MeretWithdraws` | 6 | Orrin, Meret, Hal | Orrin, then Meret | **Compulsory.** | `home.berthRefused`, `home.meetingHeld` | |

### The Separation Works

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `DorrowWorks` | 6 | Dorrow, Taren, Sela | Taren | **Compulsory.** Crossing point at the first joint under the promenade. What the Halo is made of. | `home.worksRead` | |
| `SelaWeddingTable` | 4 | Sela, Taren | | Interaction at a table in a store bay, `SecondInventory` at step 0 (`report Consignment`) | | |
| `OrrinSaintSpeaks` | 3 | Orrin | Orrin | **Compulsory.** After the override rig is destroyed (`Npc.Say`): Orrin's voice from the press's speaker. | `home.saintReleased` | |

### Hal

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `HalLastMachine` | 7 | Hal, Taren | Hal | **Compulsory.** Hal at his bench, `home.saintReleased`. Taren has one line, which is refused. | `home.halChose` | |

### The Coronach

Section 1 has the three tables. The extras are counted there.

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `OrrinCallsCoronach` | 3 | Orrin | Orrin | **Compulsory.** Orrin on the promenade, `home.halChose`. | `home.coronachCalled` | |
| `MiraStallDoor` | 3 | Mira | | **Compulsory.** Interaction; any order | *coronach.mira* | |
| `OrrinBoardDoor` | 2 | Orrin | | **Compulsory.** | *coronach.orrin* | |
| `HalBenchDoor` | 2 | Hal | | **Compulsory.** | *coronach.hal* | |
| `NeveLockDoor` | 2 | Neve | | **Compulsory.** | *coronach.neve* | |
| `OdaOfficeDoor` | 3 | Oda | | **Compulsory.** | *coronach.oda* | |
| `JunLineDoor` | 3 | Jun | | **Compulsory.** | *coronach.jun* | |
| `IloNoticeDoor` | 2 | Ilo | | **Compulsory.** | *coronach.ilo* | |
| `SurvivorDockDoor` | 2 | Survivor | | Optional | *coronach.dace* | |
| `PellEdge` | 1 | Pell | | Optional | *coronach.pell* | |
| `RuvaEdge` | 1 | Ruva | | Optional | *coronach.ruva* | |
| `MaruEdge` | 1 | Maru | | Optional | *coronach.maru* | |
| `HaskEdge` | 1 | Hask | | Optional | *coronach.hask* | |
| `TovraEdge` | 1 | Tovra | | Optional | *coronach.tovra* | |
| `DorrowEdge` | 1 | Dorrow | | Optional | *coronach.dorrow* | |
| `AbriEdge` | 1 | Keeper | | Optional | *coronach.abri* | |
| `TarenWalkway` | 2 | Taren | | **Compulsory.** Once the seven are heard (*coronach.doorsHeard*) | *coronach.taren* | |
| `SelaGateSix` | 2 | Sela | | **Compulsory.** The same | *coronach.sela* | |
| `IdraNoDoor` | 1 | Idra | | **Compulsory.** The same | *coronach.idra* | |
| `OrrinGiveItRoom` | 2 | Orrin | Orrin | **Compulsory.** Once those three are heard | `home.coronachWalked` | |

### The launch, the Ring, and after

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `NeveFirstOut` | 6 | Orrin, Neve, Mira, Taren, Meret | Neve | **Compulsory.** As the market district lifts. Meret's one radio line of the launch is in it. | `home.launched` | |
| `BrynTowsCutter` | 3 | Bryn, Idra | Bryn | **Compulsory.** After `bossdown.DebrisCutter`. Its crew live. | `home.cutterDisabled` | |
| `OrrinChalk` | 8 | Sela, Bryn, Taren, Orrin | Sela, then Orrin | **Compulsory, chapter end.** At the chart table, aboard. The navigator has the pencil. | `home.chartRead`, `chapter9.complete` | |

### Suggested routing (first match wins)

| Speaker | Beats, in order | Npc tiers |
|---|---|---|
| Orrin | `OrrinGiveItRoom`, `OrrinCallsCoronach`, the two meeting nodes, `OrrinHomecoming` | On the day his chapter 1 tiers are held. After `home.launched` he is aboard and has no node until chapter 10. |
| Mira | `MiraInventoryReturn`, `MiraInventoryOffer` | repeat `MiraCratedRepeat` |
| Hal | `HalLastMachine`, `HalBenchesReturn`, `HalBenchesOffer` | first and repeat `HalCrated`, until `home.halChose` |
| Neve | `NeveReturnAnswer`; at Nacre `NeveReturnOffer` | at lock nine `NeveFlyingAStreet` |
| Dace | `SurvivorNevesLetter` | `SurvivorDecks` |
| Jun | none | first `JunFirst`; repeat `JunRepeat` |
| Dorrow | none | Crawl: first `DorrowFirst`, repeat `DorrowRepeat`; Halo: `DorrowListens` |
| Tam | `TamGoesBack`, `TamStillOffer` | Crawl: `TamFirst` |
| Oda, Ilo | none | aboard `OdaAboard`; on the Decks `OdaDecks`, `IloDecks` |
| Keeper | `AbriNamesOnTheWall`, then chapter 8's | |
| Drask | `DraskCount`, then `DraskAboard` | |

## 3. Flags

**From the brief, and who sets them:** `home.nacreReached` (world), `home.odaFound` (`OdaFound`), `home.tugFound` (`BrynWelcomeBack`), `home.navigatorsFound` (`BrynNavigators`), `home.crawlReached` (world), `home.coupling1..3` (world), `home.treadRecovered` (`DorrowTreadHome`), `home.returned` (`OrrinHomecoming`), `home.meetingHeld` and `home.berthRefused` (`MeretWithdraws`), `home.joint1..6` (world), `home.saintReleased` (`OrrinSaintSpeaks`), `home.halChose` (`HalLastMachine`), `home.coronachWalked` (`OrrinGiveItRoom`), `home.launched` (`NeveFirstOut`), `home.cutterDisabled` (`BrynTowsCutter`), `chapter9.complete` (`OrrinChalk`).

**Added by the script:** `home.nacreSpoken`, `home.noticeHeard`, `home.chartLent`, `home.crawlNamed`, `home.crawlAsked`, `home.meetingOpened`, `home.offerMade`, `home.halAsked`, `home.manifestShown`, `home.namesRead`, `home.odaStood`, `home.orrinSaid`, `home.worksRead`, `home.coronachCalled`, `home.chartRead`, `drask.asked`, `mira.inventorySettled`, `hal.benchesLit`, `dace.letterRead`, `neve.letterAnswered`, `tam.goingBack`.

**Suggested for the world to own:** the italic `coronach.` flags in section 2, one to a door, and *coronach.doorsHeard* (a `FlagAll` over the seven needed doors). `OrrinGiveItRoom` waits on a second `FlagAll` over *coronach.taren*, *coronach.sela* and *coronach.idra*.

## 4. Quests

Kinds are `ObjectiveKind` (TalkTo 0, Reach 1, Kill 2, Collect 3, Interact 4, Flag 5). Rewards are the brief's. `HouseLeaves` is the main quest (`main: true`, `chapter: 9`).

### HouseLeaves - The House That Can Leave (main)

Description: `The Nacre berth is gone and Stillwater is closing early. Cinder Halo has been offered a place in the corridor, if it leaves its neighbours outside.`

| # | Kind | Target | Step text | Points at |
|---|---|---|---|---|
| 0 | Reach | `NacreGrave` | Take the lane to Nacre | NacreGrave, from Harrowgate or the chart |
| 1 | Flag | `home.odaFound` | Find the hauler on line nine hundred and four | NacreGrave, Oda's lamp |
| 2 | Flag | `home.tugFound` | Find the tug Welcome Back | NacreGrave, the tug's lamp |
| 3 | Flag | `home.navigatorsFound` | Bring in the navigators' pod | NacreGrave, the pod's lamp |
| 4 | Flag | `home.crawlNamed` | Go back to Many Hands, and find somebody who has moved a town | ManyHands, speaker `Idra` |
| 5 | Reach | `MeridianCrawl` | Catch up with the Meridian Crawl on Duskline | MeridianCrawl, from the chart |
| 6 | Flag | `home.treadRecovered` | Re-mate three couplings and bring the tread-hall in ahead of the dawn | Terminator, by way of `Dorrow` |
| 7 | Flag | `home.returned` | Go home | The Decks, speaker `Orrin` |
| 8 | Flag | `home.meetingHeld` | Attend the town meeting | The Decks, the dock office |
| 9 | Interact x3 | `Joint` | Reopen the separation joints under the promenade | SeparationWorks, report `Joint` |
| 10 | Flag | `home.saintReleased` | Release the Foundry Saint's arms, and take the override off its back | SeparationWorks, encounter `FoundrySaint` |
| 11 | Flag | `home.halChose` | Hold the lamp for Hal | The Decks, speaker `Hal` |
| 12 | Flag | `home.coronachWalked` | Walk the town, and hear each neighbour at their door | The Decks, every door |
| 13 | Flag | `home.cutterDisabled` | Keep the Debris Cutter off the districts as they lift | BrokenRing, encounter `DebrisCutter` |
| 14 | Flag | `home.chartRead` | See where every road out now runs | ManyHands, the chart table |

Steps 1 to 3 can be met in any order. Reward: XP, scrip, a T5 part choice.

### SecondInventory - Mira's Second Inventory (Mira)

Description: `Mira's evacuation stock is forty-one lots of other people's belongings and nine of her own. Three of the lots have two owners each.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `Consignment` | Find the three disputed lots: at Nacre, on the Crawl, and under the promenade | Find the three disputed lots |
| 1 | Flag | `mira.inventorySettled` | Tell Mira what each owner decided | Tell Mira (waits on `Mira`) |

Reward: a module ("It's from my nine"). The table top, with the names, is in the epilogue. The Nacre and Crawl points bark (section 7); the one in the Works plays `SelaWeddingTable`.

### PortableBenches - Hal's Apprentices (Hal)

Description: `Hal has nine apprentices and a workshop that won't go through a hatch. He has made three benches that fold.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `PortableBench` | Carry a bench to Ket on Many Hands, Lo on the tread-hall and Marn at the tug dock | Carry the three benches |
| 1 | Flag | `hal.benchesLit` | Tell Hal they're lit | Tell Hal (waits on `Hal`) |

Reward: a bench that works in each of the three places, and something Ket made. Each point has its own bark, so they can be done in any order.

### ReturnAddress - Neve's Return Address (Neve)

Description: `Neve is still a courier, with three parcels and no lanes left to run them down. The third is her own.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact | `TugParcel` | Take the first parcel to the tug Welcome Back | Parcel for the tug |
| 1 | Interact | `CrawlParcel` | Take the second to the town that walks | Parcel for the Crawl |
| 2 | Flag | `dace.letterRead` | Find where the third one's address has gone | Find who the third is for (waits on `Survivor`) |
| 3 | Flag | `neve.letterAnswered` | Tell Neve it arrived | Tell Neve (waits on `Neve`) |

Reward: a drive part, and her line at lock nine in the Coronach.

### StillGround - Still Ground (Tam)

Description: `Dorrow's apprentice has never stood on a floor that stays where it is, and has been sick twice.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `StillThing` | Show Tam a doorframe, a pear tree and a stair | Show Tam three still things |
| 1 | Flag | `tam.goingBack` | Hear what Tam has decided | Find Tam (waits on `Tam`) |

Reward: a token. Tam goes back to the Crawl, and says why.

## 5. Display names

### New speakers (`CharacterDef.displayName`)

| Speaker id | Display name |
|---|---|
| `Dorrow` | Chief Engineer Dorrow |
| `Tam` | Tam |
| `Jun` | Jun |

`Bryn` was defined for chapter 8. Everyone else speaks on a sheet they have or are owed, and nobody uses an expression new to them: Meret uses four of her five, Idra three of her six.

### Sheets, for the artist

**Bryn Asta, the whole set now.** As chapter 8 described her: Vael, forties, crest fronds cut square under a tug crew's headset, a controller's level gaze, the Welcome Back's hull stripe on her collar. She is seen in person from Nacre on, and she has not slept. 17 lines: Neutral 5, Determined 3, Amused 3, Tender 2, Worried 2, Sad 1, Curious 1. Her Amused is gallows: the mouth moves and the eyes don't. Her Sad is one line, about why they were all in one place, and should look like someone doing arithmetic.

**The Crawlers, and their one instrument.** Small, soot-dark, wrapped to the nose against grit, and **goggled: their eyes are not seen.** The goggles are round brass with a leaf shutter over each lens, like a camera's iris, and the shutters are the whole of a Crawler's expression. Nothing else on the face should move.

| Expression | The shutters |
|---|---|
| Neutral | Both half open |
| Happy | Both wide; the leather creases at the cheek |
| Curious | One wide, one half |
| Amused | One almost shut: a wink that stays |
| Shocked | Both fully open, rims catching the light |
| Worried | Both narrowed, unevenly |
| Determined | Both narrowed to slits, level |
| Flustered | Lenses fogged |
| Sad | Both shut |
| Tender | **The goggles pushed up on the forehead.** The only cell in which eyes are seen: pale rings in the soot. |

**Chief Engineer Dorrow.** Crawler man in his sixties, round, grease to the elbow, a chief's whistle on a cord. His left lens is cracked and he has not replaced it. 15 lines: Happy 10, Amused 2, Curious 1, Neutral 1, Tender 1. He is cheerful about everything, and the Happy cell will carry most of his part. His Tender is one line, at the Coronach.

**Tam.** Adolescent Crawler. The same goggles, a size too big, the strap wound twice; a notebook on a string. 14 lines: Curious 4, Shocked 2, Flustered 2, Determined 2, Tender 2, Worried 1, Amused 1. The fogged lenses are seasickness.

**Jun.** Old Vael woman, seventy or more: small, upright, crest fronds gone pale and thin under a faded headcloth; a canvas apron with thirty-one years of pear on it; a pear, or the edge of a crate, always in one hand. Her face hardly moves. 6 lines: Neutral 5, Tender 1. Her Tender is the same face with the eyes lowered, to a painted line on the floor.

### People named but never speaking

| Name | Who |
|---|---|
| Oda's mother | Checked the paper before the frame. Made friends with the whole queue; still writes to line eight hundred and ninety. Short. |
| Bryn's six navigators | Charted the wreck field on a pod hatch in grease, with a thumb. One of them has Sela's pencil. |
| Ket, Lo, Marn | Three of Hal's nine apprentices, who get the folding benches |
| The Arrens | The wedding table's two owners, who agreed for the first time since the wedding |
| Hesk | Left a pot on in tread-hall four. It's always Hesk. |
| Kott and Sibb, Old Senn, Iri, Quill, Aumi, Mother Odrun | Spoken for in the extras, as in their own chapters |
| The gardener | Stands by the fruit stall with its light low |

### Other words the script introduces

| Word | What |
|---|---|
| the board | Orrin's dock office board, where the town's news is chalked. He takes the chalk with him. |
| pitch nine; lock nine; gate six | Mira's pitch; the airlock Neve flies from; the departure gate Sela missed |
| the separation survey | Fourteen months on Orrin's desk, under the evacuation notices |
| local control | What the press says when the override is off, in Orrin's voice |
| a tread-hall | A section of the Crawl. Number four is the one that fell behind. |
| up-spin, down-spin | Dorrow's words for how the Halo creaks |
| a still one | What the Crawl calls a town that doesn't move |
| the roll; the bound copy | The Halo's households as the corridor counts them; Meret's manifest |

### Enemies and bosses (`EnemyDef.displayName`)

| Id | Display | Note |
|---|---|---|
| `TowDrone` | Tow-drone | |
| `BridleFragment` | Bridle Fragment | "a piece of the cage" |
| `HeatScavenger` | Heat-scavenger | "a burrower" |
| `SlagGull` | Slag Gull | |
| `OverrideRig` | Override Rig | "a Compact rig" |
| `WeldArm` | Weld Arm | Taren: "my welding arm, under somebody else's hand" |
| `CompactWarden` | Compact Warden | "crew". They give ground and are let go. |
| `Interceptor` | Interceptor | |
| `FoundrySaint` | THE FOUNDRY SAINT | Epithet: `The Halo's own separation press, under somebody else's hand`. Status: `RELEASE AN ARM AT THE SHOULDER`. Targets: `Shoulder collar`. |
| `DebrisCutter` | THE DEBRIS CUTTER | Epithet: `A Compact interceptor among the departing streets` |

The boss ids must be exactly these for their barks to be found.

### Zones (`ZoneDef.displayName`)

| Id | Display |
|---|---|
| `NacreGrave` | Nacre Grave |
| `MeridianCrawl` | The Meridian Crawl |
| `Terminator` | The Terminator |
| `SeparationWorks` | The Separation Works |
| `BrokenRing` | The Broken Ring |

### Floor signs and landmark labels

- The Decks, added for the day: `DOCK OFFICE`, `NOTICE BOARD`, `PITCH NINE`, `LOCK NINE`, `GATE SIX`, and a chalked number at each joint
- The Crawl: `HELM`, `MARKET STREET`, `SKY-GARDEN`, `TREAD-HALL FOUR`
- The Works: `PRESS HALL`, `CONTROL ROOM`

### Prompts

`Stop and listen` (every Coronach door and guest), `Say yours` (Taren's and Sela's), `Hold the lamp`, `Take the joint's weight`, `Re-mate the coupling`, `Hand over the bench`, `Show Tam`, `Look at the table`.

### Items

The script names none. Parcels, benches and the three disputed lots are carried in the story.

## 6. Objective strings

Where the panel takes its line from the main quest's current step, section 4's step text is what shows. These are the rows the steps can't say.

| Zone | When | Label |
|---|---|---|
| NacreGrave | in the inner spiral | Watch the marked tracks: inward is quick and full of wrecks |
| NacreGrave | all three lamps found | Go back to Many Hands |
| Terminator | the dawn line in view | Keep ahead of the dawn |
| Terminator | at a coupling | Re-mate the coupling (1/3) |
| Terminator | the third coupling mated | Ride the tread-hall out |
| The Decks | `home.returned`, before the meeting | See your neighbours, then go to the dock office |
| SeparationWorks | the Saint, arms left | Release an arm at its shoulder collar (1/3) |
| SeparationWorks | three arms down | Climb the fallen arms to the rig on its back |
| The Decks | `home.coronachCalled` | Walk the town. There is no hurry. |
| BrokenRing | the Cutter, pods left | Disable a weapon pod: your choice which first (1/3) |
| any | `chapter9.complete` | Chapter 9 complete. The Halo is a fleet, and every road out runs past Sorrel. |

Chapter-end card (`QuestToast`): heading `CHAPTER 9 COMPLETE`, title `THE HOUSE THAT CAN LEAVE`, body `Cinder Halo turned down a berth that left its neighbours in the footnotes. It walked its own streets, said what each door had done, and came apart on purpose. It is a fleet now, beside the convoy. Every road out runs through one passage, and Stillwater's anchor is closing on it at Sorrel, over the nursery. Until then nothing closes behind you, so keep exploring.`

## 7. Barks added

Existing entries are untouched. Every new hero key is there for Taren, Sela and Idra alike. **Nothing barks during the meeting, Hal's scene or the Coronach.**

| Speaker | Keys | Used by | Wiring |
|---|---|---|---|
| Taren, Sela, Idra | `bossphase.FoundrySaint`, `bossopening.FoundrySaint`, `bossphase.DebrisCutter`, `bossopening.DebrisCutter` | `BossController.Situation` | Automatic for those ids |
| Taren, Sela, Idra | `spiral` | Crossing into the inner spiral, and back out | Two in turn |
| Taren, Sela, Idra | `towdrone`, `fragment` | First sight of each | |
| Taren, Sela, Idra | `wreck` | Passing a wreck with its hull colours showing | Three in turn. Quiet ones. |
| Bryn | `hail.spiral` | Nacre Grave, as the party flies it | `RadioHail`; two in turn |
| Taren, Sela, Idra | `dawn` | The dawn line closes: far, near, on them | Three in turn. **This is the run's script.** |
| Dorrow | `hail.dawn` | The same three moments, from the Crawl | `RadioHail`; three in turn. He stays cheerful. |
| Taren, Sela, Idra | `remate` | A coupling re-mated | Three in turn |
| Dorrow | `hail.remate` | After each | Three in turn |
| Taren, Sela, Idra | `scavenger`, `gull` | First sight of each | |
| Taren, Sela, Idra | `joint` | A separation joint reopened | Three in turn, round again for the other three |
| Taren, Sela, Idra | `override`, `weldarm`, `warden` | First sight of each. `warden` is for when they give ground. | |
| Taren, Sela, Idra | `saint.arm` | An arm released at its collar | Three in turn |
| Orrin | `hail.section` | The four districts lifting, in the order market, repair quarter, residential crescent, tug dock | `RadioHail`; four in turn. The first follows `NeveFirstOut`. |
| Taren, Sela, Idra | `section` | A district passes the party during the Cutter fight | Three in turn: repair quarter, crescent, tug dock |
| Neve | `hail.lane` | During the Cutter fight | Two in turn |
| Mira | `hail.market` | When the Cutter's beam crosses the market district | |
| Hal | `hail.repair` | When the repair quarter clears the ring | |
| Bryn | `hail.ring` | The launch | Two in turn |
| Idra | `hail.ring` | When she is not the active ship: she holds the Ring | Three in turn |
| Taren, Sela, Idra | `interceptor` | First pair | |
| Taren, Sela, Idra | `pod` | A weapon pod disabled | Three in turn, whichever the player chose |
| Taren, Sela, Idra | `consign.nacre`, `consign.crawl` | The two disputed lots that are not the table | `DiscoveryPoint.situation` |
| Taren, Sela, Idra | `bench.ket`, `bench.lo`, `bench.marn` | Each folding bench handed over | `DiscoveryPoint.situation`, one key to a point |
| Taren, Sela, Idra | `parcel.tug`, `parcel.crawl` | Neve's first two parcels | `DiscoveryPoint.situation` |

## 8. What the script assumes

**No new commands or dialogue features.**

1. **Portraits.** Dorrow, Tam, Jun, and the rest of Bryn's set. Section 5. Jun has no set in `Resources/Portraits` today.
2. **Expectations** (`[ContentRegistry.Expectations]`). Three new `CharacterDef`s: `Dorrow`, `Tam`, `Jun`. One node has choices: `book.Branch("HalCrated","bench","repair","world")`. Three end in the shop and want `book.Branch(node,"shop")`: `MiraInventoryOffer`, `MiraCratedRepeat`, `MiraInventoryReturn`. No node needs `book.Optional`: the longest is 41 seconds. `PeopleMarks.Repeats` gets `OdaAboard`, `AbriNamesOnTheWall`, `DorrowRepeat`, `TamFirst`, `JunRepeat`, `MiraCratedRepeat`, `HalCrated`, `NeveFlyingAStreet`, `SurvivorDecks`, `OdaDecks`, `IloDecks`, `DorrowListens`. The `Resources` copy of `barks.json` needs bringing level again.
3. **Chapter 1's people on the day.** From `home.returned` Orrin's, Mira's, Hal's, Neve's, Oda's and Ilo's chapter 1 tiers are held, and the nodes in section 2 take their place. After `home.launched` the Decks as a place is gone.
4. **The meeting** is one room and eight walks (section 1). Meret is there in person and standing. Nobody sits on the front bench: it has a crate on it.
5. **Hal's scene.** Taren holds a lamp from his fourth line to the end, and does nothing else. Hal does all the work. No bark, no prompt for Taren to help, and no line from Sela or Idra. Please don't add a reaction to "Hold still a moment."
6. **The Coronach** as section 1 has it: discovery points, any order, no timer, no enemy, no bark, its own music. A door's speaker faces the door, not the party. An extra plays straight after its speaker's own line, or on its own where the speaker has none. If a guest's side story was not done, the guest is still there.
7. **The gardener, the bell, the table top.** Three extras are things seen or heard that somebody else reads: the gardener by the fruit stall with its light low (`RuvaDoorGardener`), one stroke of the school raft's bell (`DovDoorBell`), a table top carried past in two halves (`MiraDoorTable`).
8. **The pencil stays with the navigator** from `BrynChartLesson` on.
9. **Neve is at Nacre** in the first act, flying survivors, and at lock nine from `home.returned`. Dace and nine of his crew are on the Decks' dock from `home.returned`.
10. **Meret speaks once in the launch**, inside `NeveFirstOut`, and not again in the chapter. Her crews in the Works and the Ring are hers; none of them speaks.
11. **The Cutter's crew live**, and Bryn tows it (`BrynTowsCutter`).
12. **If a side quest is left undone**, nothing on the main path mentions it. Tam stays seasick; Dace has not had his letter, and Neve's door is two lines and not three.

## 9. Where the script departs from the brief

- **Jun is new, and the stall was always half hers.** Section 1. She needs a sheet. Chapter 1 is not contradicted: Mira never said the pears were hers.
- **The reunion at Nacre** is not the brief's two lines. Taren: "It's the repairman." Oda: "You're late. The door jammed days ago. I sent word." It runs on her own first lines (the paint, the frame, morbid, strapped to the outside).
- **Nobody was left in the tread-hall.** Everyone walked out; nine kitchens and a schoolroom couldn't. The run is for a place, not for lives.
- **What Orrin postponed** is the separation survey, fourteen months under the evacuation notices, "and that's why they went on top". What cannot travel: the promenade end to end, and Hal's shop as a shop. He says he wants the berth, says he is angry, and votes against it.
- **The names Ilo reads** are Jun, Hal's six apprentices from the convoy, Dace's ten, and line nine hundred and four. Oda says "Here." Dace says it too. Jun stands and says nothing, and Mira says it for her.
- **Four hands go up for the berth.** Orrin: "I see you, and I don't blame you."
- **Meret has two lines after the refusal**: the brief's, and one telling them there will be crews in their way. Hal answers her: "Director. Easy on the landing."
- **Dace is on the Decks.** Compact engineers gave the Sorrel outpost an hour and a receipt; what is under his bunk is "covered in section eleven". That is the pointer at the chapter's last line.
- **Hal's machine is the bench from chapter 1**, in nine pieces. Taren offers to take the far side and is told to hold the lamp.
- **"Give it room" is Orrin's**, as a dock order.
- **Seven doors are needed, not eight.** Dace's is optional because he never lived here, and he says so.
- **Idra counts who spoke.** "Nobody was left out of it. I checked twice."
- **Thirty-two extras**, one for each side quest from chapter 1 on. `Hushwell` has none: I could not tell from the scripts whether it is a side story or a place.
- **Neve's own parcel is a letter to Dace**, which makes her chapter 1 joke true.
- **Tam is never "he" or "she"** in the script.
- **The chapter ends on Orrin**: "I brought the chalk."
- **No day count anywhere.**
- **New names**: Ket, Lo, Marn, the Arrens, Hesk; pitch nine, lock nine, gate six.
