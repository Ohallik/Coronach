# Chapter 4 script manifest - Breathing Room (Wendmire)

For the engineers wiring chapter 4. Script: [Wendmire.yarn](Wendmire.yarn). Barks: [barks.json](barks.json). Brief: [ch04-breathing-room.md](../chapters/ch04-breathing-room.md). Chapter 3's manifest, in the same shape: [ch03-manifest.md](ch03-manifest.md).

**What is here:** 58 nodes, 249 dialogue lines, 23 option labels (272 strings). Compiled clean with the project's own compiler (Yarn Spinner 3.2.4, all eleven files together: 0 errors, 219 nodes, no duplicate titles). No new Yarn commands: only `setFlag`, `startQuest`, `jump`, `shop`, `bench`, `repair`.

**Read section 8 before the next world build.** The builders copy every `docs/writing/*.yarn` into `Resources/Dialogue`, so `Wendmire.yarn` joins the Yarn project the next time one runs, wired or not. From then `DialogueLayoutTests` and `DialogueInputTests` fail until the five portrait sets, the five `QuestDef`s and the test lists in section 8 are in.

## 1. Conventions this file follows

- Every title starts with its owning speaker id: `Abri…` (speaker `Keeper`), `Pell…`, `Maru…`, `Ona…`, `Dov…`, `Quill…`, `Pim…`, and `Sela…` for the party reading something with nobody else there.
- A node sets a flag itself when the conversation is the event. A node sets nothing when the world owns the event, or when it plays at a point offered only at a quest's step: that point must answer an `Interact` step with its own `report`, and a talk that set the point's flag would stop the report (`DiscoveryPoint.CompleteDialogue`). Suggested flags for the world to own are in italics.
- Each quest is started by exactly one node. `BeaconKeeper` can be reached two ways, so both run on (`<<jump>>`) into one shared node that holds the start.
- `bossdown.Rotbloom` is the engine's. The brief's `wend.rotbloomDead` is the story flag, set by the talk in the hall afterwards.
- Checked against the word tests: no retired phrase (`TheScriptStopsPromisingWhatIsntThere`; Driftreed's crossings are "rope walks"), no id-shaped word (`NoRawIdOrWrongWordInTheScriptOrTheData`).

### The two light signals

The script uses two, and each is always said aloud by someone who sees it, so nothing depends on seeing a pulse or hearing a bell. Wherever a row below says a light is "in view", the world should draw it too, and a mark on screen (dots and bars) would not go amiss.

| Signal | Means | Where it is said |
|---|---|---|
| Two short, one long | Driftreed's own count: somebody keeps this lamp | `MaruVeilHail`, `MaruDeadBeacon`, `SelaSchoolRaft`, `MaruLureLesson`, `SelaBellBuoy`, `MaruPortico`, `PimFound`, `MaruAfterBloom` |
| Long, long, short | The pilots' call: still afloat | Maru's lamp to Ona, unexplained (`OnaTerms`, `MaruTide`); Pim's glow-spots, where it is explained (`PimFound`); Ona's own, answered at last (`OnaDecides`, `MaruComes`) |

A lure flickers shorts and never holds a long ("nothing hungry waits"), and lately gets the two shorts right, which is what took Pim.

## 2. Nodes

Lines are dialogue lines (options not counted). "Compulsory" marks the main path; the longest of those, `OnaTerms`, comes to 45 seconds by the ninety-second test's arithmetic.

### Tallow Drift

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `AbriReedRoad` | 9 | Keeper, Teo, Taren, Sela | **Compulsory.** Abri, once `chapter3.complete` and `ReedRoad` not started. Teo is a voice on the radio from his carriers. It takes the place of `AbriDinnerServed`. | | `startQuest ReedRoad` |
| `AbriReedRoadRepeat` | 1 | Keeper | Abri, `ReedRoad` started and not `wend.charted` | | |
| `AbriReedRoadCharted` | 1 | Keeper | Abri, `wend.charted` and not `wend.onaHeard` | | |
| `AbriReedRoadWaiting` | 1 | Keeper | Abri, `wend.onaHeard` and not `chapter4.complete` (a visit home mid-chapter) | | |
| `PellVeilLantern` | 6 | Pell, Sela, Taren | **Compulsory.** Pell at the lantern, `ReedRoad` started and not `wend.charted`. Opens the Wendmire warp. It takes the place of `PellAfterVote`. | `wend.charted` | |
| `PellVeilRepeat` | 1 | Pell | Pell, `wend.charted` and not `chapter4.complete` | | |
| `AbriManyHandsCall` | 10 | Keeper, Taren, Sela | **Compulsory, chapter end.** Radio, once `wend.decided`: on the walk to the pilots' stage, or on launch (`Npc.Say`). | `chapter4.complete` | |
| `AbriManyHands` | 1 | Keeper | Abri, `chapter4.complete` | | |
| `PellManyHands` | 1 | Pell | Pell, `chapter4.complete` | | |

### The Spore Veil (flight; all radio)

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `MaruVeilHail` | 9 | Maru, Sela, Taren | **Compulsory.** Crossing point just inside the first beacon. Teaches the glow (fire, boost and skills draw swarms; roll, or go dark and count two) and the beacons' count. | *wend.maruHailed* | |
| `MaruVeilStray` | 3 | Maru, Sela | Crossing point at the mouth of the first off-lane pocket, `BeaconKeeper` not started. The slipped beacon in view, pulsing short, long, short. Runs on into `MaruBeaconsAsk`. | *wend.strayed* | `jump MaruBeaconsAsk` |
| `MaruBeaconsAsk` | 2 | Maru, Sela | Never asked for by the world: the shared tail of `MaruVeilStray` and `MaruBeaconsOffer`, and the one node that starts the quest. | | `startQuest BeaconKeeper` |
| `MaruDeadBeacon` | 3 | Sela, Maru | **Compulsory.** Interaction at the dark beacon on leg three. The talk comes before the lamp comes up; Maru's verdict afterwards is the bark `hail.relit`. | `wend.beaconRelit` | |

Her remarks on each leg are barks (section 7), so nothing stops the flying.

### Driftreed

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `MaruArrival` | 9 | Maru, Sela, Taren | **Compulsory.** Crossing point on the pilots' stage, first landing. Ends with the stern line in Sela's hands. | *wend.arrivalSeen* | |
| `MaruLineHold` | 1 | Maru | Maru's `holdNode` until `wend.lineHeld` | | |
| `OnaLineHold` | 1 | Ona | Ona's `holdNode` until `wend.lineHeld` | | |
| `MaruFirst` | 7 | Maru, Sela, Taren | Maru's first node, once `wend.lineHeld`. Optional: it points at Ona and can be missed. | | |
| `MaruRepeat` | 1 | Maru | Maru's repeat, before the tide | | |
| `OnaTerms` | 11 | Ona, Taren, Sela | **Compulsory.** Ona's first node, once `wend.lineHeld`. Maru's lamp in view from the hall door, across the water at the pilots' stage: long, long, short. | `wend.onaHeard` | |
| `OnaRepeat` | 4 | Ona | Ona's repeat until Pim is found. Three options, all talk. Her first line is to a weaver about a comb on the bench: Pim's. | | |
| `OnaPimBack` | 2 | Ona | Ona, `wend.pimFound` and not `wend.rotbloomDead` | | |
| `OnaDecides` | 11 | Ona, Taren, Sela | **Compulsory.** Ona, `wend.rotbloomDead` and not `wend.decided`. Pim at the bench; the reed store open, poles by the door. Ona's own glow-spots: long, long, short. | `wend.decided` | |
| `OnaAfter` | 2 | Ona | Ona's last tier (`wend.decided`). Whether she travels is left open for chapter 5. | | |
| `MaruTide` | 10 | Maru, Sela, Taren | **Compulsory.** Maru, `wend.onaHeard` and not `wend.tideTurned`. Also her first meeting if nobody spoke to her after the line (`LateMeetings`); it reads right either way. The rope walks swing when it closes. | `wend.tideTurned` | |
| `MaruOnaWaits` | 1 | Maru | Maru back at the stage, `wend.rotbloomDead` and not `wend.decided` | | |
| `MaruComes` | 6 | Maru, Taren, Sela | Maru, once: `wend.decided`, not `maru.comesHeard`. Her condition in her own words; names Ruva for chapter 5's mooring dispute. | `maru.comesHeard` | |
| `MaruBeaconsOffer` | 3 | Maru, Sela | Maru, `maru.comesHeard` and `BeaconKeeper` not started (the party never left the lane). Runs on into `MaruBeaconsAsk`. | | `jump MaruBeaconsAsk` |
| `MaruBeaconsReturn` | 4 | Maru, Sela | Maru at the stage (not while she is in the mire), `BeaconKeeper` at step 1 | `maru.beaconsReported` | |
| `MaruPost` | 1 | Maru | Maru's post (`wend.decided`), once the above are heard | | |
| `DovFirst` | 9 | Dov, Taren, Sela | Dov's first node. Holds whether or not Ona has been heard. Options: shop, or leave. | | `shop` (option 1) |
| `DovRepeat` | 3 | Dov | Dov's repeat. Options: gossip, shop, leave. | | `shop` (option 2) |
| `DovOrderOffer` | 4 | Dov, Taren | Dov, met, `wend.tideTurned`, `SmokehouseOrder` not started | | `startQuest SmokehouseOrder`, `shop` |
| `DovOrderReturn` | 4 | Dov, Sela | Dov, `SmokehouseOrder` at step 1 | `dov.orderFilled` | `shop` |
| `DovBellOffer` | 5 | Dov, Sela | Dov, `SmokehouseOrder` started, `SchoolBell` not started. He asks for Iri, who has no lines. | | `startQuest SchoolBell`, `shop` |
| `DovBellReturn` | 3 | Dov | Dov, `SchoolBell` at step 1. The school raft will travel. | `dov.bellReported` | `shop` |
| `DovPost` | 2 | Dov | Dov's post (`wend.decided`) | | `shop` |
| `QuillFirst` | 7 | Quill, Taren, Sela | Quill at the pump raft, first node. Options: press, repair, parts. | | `bench`, `repair`, `shop` |
| `QuillRepeat` | 1 | Quill | Quill's repeat. Same options. | | `bench`, `repair`, `shop` |
| `QuillPumpOffer` | 5 | Quill, Taren | Quill, met, `wend.onaHeard`, `FourthPump` not started. Same options after it, so the press is never a second talk away. | | `startQuest FourthPump`, `bench`, `repair`, `shop` |
| `QuillPumpReturn` | 7 | Quill, Taren, Sela | Quill, `FourthPump` at step 2. His reading of the mark. Same options. | `quill.markRead` | `bench`, `repair`, `shop` |
| `QuillPost` | 2 | Quill | Quill's post (`wend.decided`): the drawings go, he stays. Same options. | | `bench`, `repair`, `shop` |
| `PimHome` | 2 | Pim | Pim at the bench in the weaving hall, from `wend.rotbloomDead` | | |
| `SelaWendNotice` | 2 | Sela, Taren | Interaction at the market post. Optional. The Compact thread, in two lines (Dov has a third as gossip). | *wend.noticeRead* | |
| `SelaSchoolRaft` | 3 | Sela, Taren | Interaction at the school raft: the children clapping the count, Iri among them. Optional. | *wend.schoolSeen* | |

### The mire and the Drowned Waystation

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `MaruMireGate` | 4 | Sela, Maru, Taren | **Compulsory.** Crossing point on the first island, the portico in view to the north. Maru is there with a lamp. | *wend.mireEntered* | |
| `MaruTideCord` | 8 | Sela, Maru | Maru on the first island, talked to, once. Optional, and the chapter's best look at Sela learning from her. | `maru.cordShown` | |
| `MaruMireRepeat` | 1 | Maru | Maru in the mire and at the portico after that | | |
| `MaruLureLesson` | 9 | Maru, Taren, Sela | **Compulsory.** Crossing point at the lure ambush, before the Lurelight wakes (`previewEncounter`). Two lamps in view: the far one keeping two short, one long; the near, brighter one flickering shorts. | `wend.lureLearned` | |
| `SelaBellBuoy` | 4 | Taren, Sela | Interaction at the bell-buoy, `SchoolBell` at step 0 (`report BellBuoy`). The bell rung two short, one long; pot lids answer from the town. | *wend.bellRung* | |
| `MaruPortico` | 5 | Maru, Sela, Taren | **Compulsory.** Crossing point at the station's portico. Maru goes no further; her lamp stays at the door. | *wend.porticoReached* | |
| `SelaWaystationHall` | 4 | Sela, Taren | **Compulsory.** Crossing point inside the entry hall | *wend.hallSeen* | |
| `SelaPumpOne` | 4 | Taren, Sela | **Compulsory.** Interaction at pump one, before it turns. The sentries still under water in their alcoves. | *wend.pump1* | |
| `PimFound` | 10 | Sela, Taren, Pim | **Compulsory.** Pim behind a grille in the dry pocket off the lower gallery, once pump two has run. Pim's glow-spots in view: long, long, short. Afterwards Pim leaves for the portico. | `wend.pimFound` | |
| `PimPortico` | 1 | Pim | Pim beside Maru at the portico until the Rotbloom is dead | | |
| `SelaVaultMark` | 4 | Sela, Taren | Interaction at the vault's marked wall, `FourthPump` at step 1 (`report VaultMark`) | *wend.vaultRead* | |
| `SelaBloomDoor` | 5 | Taren, Sela | **Compulsory.** Crossing point at the great hall's door checkpoint, the Rotbloom and its four stalks in view. Teaches burning a stalk. | *wend.bloomSeen* | |
| `SelaBloomDead` | 4 | Sela, Taren | **Compulsory.** In the hall after `bossdown.Rotbloom` (`Npc.Say`). Taren's one line about it. | `wend.rotbloomDead` | |
| `MaruAfterBloom` | 5 | Maru, Taren | **Compulsory.** Crossing point at the portico on the way out, `wend.rotbloomDead`. Sela has no line here on purpose. | *wend.lampCarried* | |

### Suggested routing (first match wins, as in `SideStories.Beats`)

| Speaker | Beats, in order | Npc tiers |
|---|---|---|
| Keeper | `AbriManyHands`, `AbriReedRoadWaiting`, `AbriReedRoadCharted`, `AbriReedRoadRepeat`, `AbriReedRoad`, then chapter 3's | unchanged |
| Pell | `PellManyHands`, `PellVeilRepeat`, `PellVeilLantern`, then chapter 3's | unchanged |
| Maru | `MaruBeaconsReturn` (at the stage only), `MaruTide`, `MaruTideCord` (in the mire: `wend.tideTurned`, not `maru.cordShown`, not `wend.rotbloomDead`), `MaruOnaWaits`, `MaruComes`, `MaruBeaconsOffer`. `LateMeetings`: `MaruTide` once `wend.onaHeard`. | hold `MaruLineHold`; first `MaruFirst`; repeat `MaruRepeat` in town, `MaruMireRepeat` on the mire objects; post `MaruPost` (`wend.decided`) |
| Ona | `OnaDecides`, `OnaPimBack` | hold `OnaLineHold`; first `OnaTerms`; repeat `OnaRepeat`; final `OnaAfter` (`finalFlag wend.decided`) |
| Dov | `DovOrderReturn`, `DovBellReturn`, `DovOrderOffer`, `DovBellOffer` | first `DovFirst`; repeat `DovRepeat`; post `DovPost` |
| Quill | `QuillPumpReturn`, `QuillPumpOffer` | first `QuillFirst`; repeat `QuillRepeat`; post `QuillPost` |
| Pim | none | station object: first `PimFound`. Portico object: repeat `PimPortico`. Hall object: repeat `PimHome`. |

## 3. Flags

**From the brief, and who sets them:** `wend.charted` (`PellVeilLantern`), `wend.beacon1..3` (world), `wend.beaconRelit` (`MaruDeadBeacon`), `wend.arrived` (world), `wend.lineHeld` (world), `wend.onaHeard` (`OnaTerms`), `wend.tideTurned` (`MaruTide`), `wend.lureLearned` (`MaruLureLesson`), `wend.pump1..4` (world), `wend.pimFound` (`PimFound`), `wend.rotbloomDead` (`SelaBloomDead`), `wend.decided` (`OnaDecides`), `chapter4.complete` (`AbriManyHandsCall`).

**Added by the script:** `maru.comesHeard`, `maru.cordShown`, `maru.beaconsReported`, `dov.orderFilled`, `dov.bellReported`, `quill.markRead`.

**Suggested for the world to own:** the italic flags in section 2.

## 4. Quests

Kinds are `ObjectiveKind` (TalkTo 0, Reach 1, Kill 2, Collect 3, Interact 4, Flag 5). Rewards are the brief's; the numbers are the systems stream's. `ReedRoad` is the main quest (`main: true`, `chapter: 4`). "Points at" is what each step's `QuestTarget` would name; a side quest's "Panel brief" is its `QuestTarget.brief`.

### ReedRoad - Breathing Room (main)

Description: `The convoy is running out of air, and Driftreed's weavers won't send a mat anywhere without a weaver to mend it.`

| # | Kind | Target | Step text | Points at |
|---|---|---|---|---|
| 0 | Flag | `wend.charted` | Teach Pell's lantern the lane to Wendmire | TallowDrift, speaker `Pell` |
| 1 | Reach | `SporeVeil` | Take the Wendmire warp off Tallow's approach | SporeVeil (via TallowApproach) |
| 2 | Flag | `wend.beaconRelit` | Follow Maru's beacons and relight the dark one | SporeVeil, the dark beacon's point |
| 3 | Reach | `Driftreed` | Come down dark through the tide-gate to Driftreed | Driftreed |
| 4 | Flag | `wend.onaHeard` | Hold the pilots' line, then ask Ona Sedge about mats | Driftreed, speaker `Ona` |
| 5 | Flag | `wend.tideTurned` | Ask Maru Wend to take you into the mire | Driftreed, speaker `Maru` |
| 6 | Flag | `wend.lureLearned` | Follow Maru's lamp across the mire: two short, one long | DrownedWaystation (via Driftreed), the lure lesson's point |
| 7 | Interact x2 | `Pump` | Run the pumps that drain the hall and the lower gallery | DrownedWaystation, report `Pump` |
| 8 | Flag | `wend.pimFound` | Find Ona's apprentice in the lower gallery | DrownedWaystation, speaker `Pim` |
| 9 | Interact | `Pump` | Run the last pump, at the great hall's door | DrownedWaystation, report `Pump` |
| 10 | Kill | `Rotbloom` | Kill the Rotbloom | DrownedWaystation, encounter `Rotbloom` |
| 11 | Flag | `wend.decided` | Go back to Ona Sedge and hear what Driftreed decides | Driftreed (via DrownedWaystation), speaker `Ona` |

The pumps are counted, and a counted step can't catch up on a report it missed, so pump three must stay shut until `wend.pimFound` (step 8 sits between the second pump and the third). Steps 1, 5 and 6 are not in the brief's list. Reward: XP, scrip, a T3 part choice.

### BeaconKeeper - Two Short, One Long (Maru)

Description: `Two of Driftreed's beacons in the Spore Veil have slipped their count, and a lamp with the wrong rhythm is worse than a dark one.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x2 | `OffLaneBeacon` | Set the two beacons off the Spore Veil's lane back to two short, one long | Retune the off-lane beacons |
| 1 | Flag | `maru.beaconsReported` | Tell Maru Wend both beacons are keeping time | Tell Maru they're in time (waits on `Maru`) |

Reward: a drive part ("it came off a courier that stayed tied up too long"), and Maru's respect, briefly: `MaruBeaconsReturn` ends on it.

### SmokehouseOrder - The Smokehouse Order (Dov)

Description: `Dov Hale smokes his eel over Glowspore, and every cap of it grows in the mire.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Collect x6 | `Glowspore` | Gather six Glowspore from the wet side of the mire's peat islands | Gather Glowspore |
| 1 | Flag | `dov.orderFilled` | Bring the Glowspore to Dov's smokehouse | Bring Dov the Glowspore (waits on `Dov`) |

Reward: smoked provisions on Dov's counter (`ShopOffer.requires: dov.orderFilled`).

### SchoolBell - The School Bell (Dov, asking for Iri)

Description: `The school raft's bell-buoy slipped its line and is ringing out in the mire with no count to it.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact | `BellBuoy` | Find the school raft's bell-buoy in the mire and ring it true | Ring the bell-buoy |
| 1 | Flag | `dov.bellReported` | Tell Dov the bell is ringing true | Tell Dov it's ringing (waits on `Dov`) |

Reward: a module; the school raft travels with the convoy (Dov says so for Iri in `DovBellReturn`).

### FourthPump - The Fourth Pump (Quill)

Description: `Quill has counted the pipes for a pump that drains one small room, and can't say what the old engineers kept dry in it.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact | `FourthPump` | Find the fourth pump off the lower gallery's east side and run it | Run the fourth pump |
| 1 | Interact | `VaultMark` | Take a rubbing of the marks in the drained vault | Take a rubbing in the vault |
| 2 | Flag | `quill.markRead` | Show Quill the old engineers' mark | Show Quill the rubbing (waits on `Quill`) |

Reward: a T3 module ("it came out of the fourth pump's housing"), and Quill's reading in `QuillPumpReturn`: the ring-and-bar works mark is on the pumps and on the sentries, so one workshop made both, and the strokes under it are a tally of guards kept here and guards sent "up the cold road". That is the pointer at chapter 6's Frozen Host and chapter 7's Oriel; neither is named. Sela wants the rubbing sent to Ishen.

## 5. Display names

### Speakers (`CharacterDef.displayName`)

| Speaker id | Display name | Emotions the script uses |
|---|---|---|
| `Maru` | Maru Wend | Amused, Angry, Curious, Determined, Happy, Neutral, Smug, Tender, Worried |
| `Ona` | Ona Sedge | Amused, Determined, Neutral, Tender |
| `Dov` | Dov Hale | Amused, Determined, Happy, Smug, Tender, Worried |
| `Quill` | Quill | Amused, Curious, Determined, Flustered, Happy, Sad, Tender, Worried |
| `Pim` | Pim | Curious, Determined, Flustered, Happy, Worried |

**No speaker beyond the brief's five**, so no extra sheet. `Teo` has two radio lines in `AbriReedRoad` (Neutral) on his chapter 3 sheet; `Keeper` and `Pell` are as before.

One note for Ona's sheet: every line she has before Pim is home is Neutral, Determined or Amused, and every Tender line comes after. If her glow-spots are dim in the first three and lit in Tender, the portrait carries what she never says.

### People named but never speaking

| Name | Who |
|---|---|
| Iri | The teacher on the school raft. A townsfolk body, no sheet, no lines: Dov asks and thanks for her. |
| Maru's apprentice | Twelve, sets beacons less cleanly than Sela. Never seen. |

### Other words the script introduces

| Word | What |
|---|---|
| Lantern | What Maru calls Sela, from her first line on the radio to her last |
| still afloat | What long, long, short means |
| the thrifty one | The old engineer Quill would have liked: left-handed, never built a pump for nothing |
| blue eel | Dov's smoked eel, coloured by Glowspore |
| rope walks | Driftreed's swinging crossings |

### Enemies and boss (`EnemyDef.displayName`)

| Id | Display | Note |
|---|---|---|
| `SporeSwarm` | Spore Swarm | |
| `SporePod` | Spore Pod | |
| `VeilRay` | Veil Ray | Maru calls it "the ray" |
| `Lurelight` | Lurelight | Spoken of as "a lure" |
| `Bogstrider` | Bogstrider | |
| `MireLeeches` | Mire Leeches | |
| `RotPod` | Rot-pod | |
| `DrownedSentry` | Drowned Sentry | Spoken of as "the sentries", and by Pim as "the bronze ones" |
| `Rotbloom` | THE ROTBLOOM | Epithet: `It learned Driftreed's lamps, and it is fishing`. Status in phase one: `ITS MOUTHS ARE SHUT  ·  BURN A LURE-STALK`. Break targets: `Lure-stalk`. |

The boss id must be exactly `Rotbloom` for its barks to be found.

### Zones (`ZoneDef.displayName`)

| Id | Display |
|---|---|
| `SporeVeil` | The Spore Veil |
| `Driftreed` | Driftreed |
| `DrownedWaystation` | The Drowned Waystation |

### Floor signs and landmark labels

- Spore Veil: `TIDE-GATE`
- Driftreed: `PILOTS' STAGE`, `STILT MARKET`, `SMOKEHOUSE`, `WEAVING HALL`, `PUMP RAFT`, `SCHOOL RAFT`, `MIRE GATE`
- The mire: `FIRST ISLAND`, `SECOND ISLAND`, `LEECH CHANNEL`, `PORTICO`
- The waystation: `ENTRY HALL`, `LOWER GALLERY`, `THE VAULT`, `GREAT HALL`

### Prompts

`Hold the stern line`, `Read the Compact notice`, `Watch the school raft`, `Relight the dark beacon`, `Retune the beacon`, `Ring the bell-buoy`, `Run the pump`, `Take a rubbing of the marks`.

### Items

| Id | Display | Note |
|---|---|---|
| `Glowspore` | Glowspore | From the brief. Always capitalised in the script. |

The script names no other item. Dov's reward is "smoked provisions … the kind that keep" (a `Blue Eel` would suit his lines), Maru's is a drive off a wrecked courier, Quill's is a module out of the fourth pump's housing.

## 6. Objective strings

In `ObjectiveHud`'s style. Where a zone takes its line from the main quest's current step, section 4's step text is what shows, and only the rows the steps can't say are needed from here.

| Zone | When | Label |
|---|---|---|
| TallowDrift | `ReedRoad` not started | See Keeper Abri: the air is going thick |
| TallowDrift | started, not `wend.charted` | Teach Pell's lantern the lane to Wendmire |
| TallowDrift | `wend.charted` | Launch for the Wendmire warp |
| TallowApproach | `wend.charted` | Take the warp to Wendmire |
| SporeVeil | on the lane | Follow the beacons: two short, one long |
| SporeVeil | leg three, not `wend.beaconRelit` | Relight the dark beacon while the ray is on its far pass |
| SporeVeil | `wend.beaconRelit`, not landed | Come down dark through the tide-gate |
| SporeVeil | `wend.decided` | Warp home to Tallow Approach |
| Driftreed | arrival seen, not `wend.lineHeld` | Hold the stern line: don't tie it |
| Driftreed | `wend.lineHeld`, not `wend.onaHeard` | Ask Ona Sedge about mats at the weaving hall |
| Driftreed | `wend.onaHeard`, not `wend.tideTurned` | Talk to Maru Wend at the pilots' stage |
| Driftreed | `wend.tideTurned` | Take the mire gate: Maru is waiting on the first island |
| Driftreed | `wend.rotbloomDead`, not `wend.decided` | Go to Ona Sedge at the weaving hall |
| Driftreed | `wend.decided` | Launch from the pilots' stage: the weavers are coming |
| DrownedWaystation | not `wend.lureLearned` | Follow Maru's lamp: two short, one long |
| DrownedWaystation | `wend.lureLearned`, outside | Cross the mire to the old station's portico |
| DrownedWaystation | inside, fewer than two pumps run | Run the pumps to drain the station (1/2) |
| DrownedWaystation | two pumps run, not `wend.pimFound` | Find Ona's apprentice in the lower gallery |
| DrownedWaystation | `wend.pimFound` | Run the last pump, at the great hall's door |
| DrownedWaystation | fight, phase one | Burn a lure-stalk to open one of its mouths |
| DrownedWaystation | fight, phase two | Kill the Rotbloom |
| DrownedWaystation | `bossdown.Rotbloom` | Walk out to Maru's lamp at the portico |
| DrownedWaystation | `wend.rotbloomDead`, at the portico and beyond | Carry the lamp back to Driftreed |
| any | `chapter4.complete` | Chapter 4 complete. Many Hands is waiting, and so is a tug with a ledger. |

Chapter-end card (`QuestToast`): heading `CHAPTER 4 COMPLETE`, title `BREATHING ROOM`, body `Driftreed's weavers are coming up on their own rafts, tied to nobody, and the mire has only its own lamps again. The convoy has a name now, Many Hands, and a salvage tug off its stern asking where it is moored. Until then nothing closes behind you, so keep exploring.`

## 7. Barks added

Existing entries are untouched. New keys were added to `Taren` and `Sela`, and `Maru` is new.

| Speaker | Key | Used by | Wiring |
|---|---|---|---|
| Taren, Sela | `bossphase.Rotbloom`, `bossopening.Rotbloom` | `BossController.Situation` | Automatic once the boss's `health.id` is `Rotbloom`. The opening line is for a burned stalk and a gaping mouth. |
| Taren, Sela | `glow` | The glow gauge fills | Two lines in turn |
| Taren, Sela | `swarm` | A swarm clings | |
| Taren, Sela | `tune` | An off-lane beacon retuned | `DiscoveryPoint.situation = "tune"`; two lines in turn, the second fits the second beacon |
| Taren, Sela | `lure` | A Lurelight shows itself | Two lines in turn |
| Taren, Sela | `leech` | The leech channel | `DiscoveryPoint.situation`, or the encounter's start |
| Taren, Sela | `pump` | Pumps two and three run | Two lines in turn. Pump one has the node `SelaPumpOne`. |
| Maru | `hail.pods`, `hail.swarm`, `hail.ray` | Entering legs one, two and three | One `RadioHail` each, placed at the leg's start |
| Maru | `hail.bright` | The first time the glow gauge fills | `BarkService.Play("Maru","hail.bright",true)`, once |
| Maru | `hail.relit`, `hail.gate` | The dark beacon is lit | One `RadioHail`, both lines in order, `needs wend.beaconRelit` |
| Maru | `line` | The stern line | Three lines in turn, one per surge, the last when the raft lies easy |
| Maru | `hail.leech` | Nearing the leech channel | `RadioHail`: she calls it from the island behind |

`barks.json` still has no per-zone arrival lines, so none were added.

## 8. What the script assumes

**No new commands or dialogue features.**

1. **Five portrait sets, no more.** Maru, Ona, Dov, Quill, Pim. Iri has no lines, so needs none.
2. **Tests to extend.** `DialogueInputTests`: add `Maru`, `Ona`, `Dov`, `Quill`, `Pim` to the speaker-prefix list; to `branches`: `OnaRepeat` {world, world, world}; `DovFirst` {shop, world}; `DovRepeat` {world, shop, world}; `QuillFirst`, `QuillRepeat`, `QuillPumpOffer`, `QuillPumpReturn`, `QuillPost` {bench, repair, shop}. `DovOrderOffer`, `DovOrderReturn`, `DovBellOffer`, `DovBellReturn` and `DovPost` end in the shop. `QaPlanWorldTests`: the quest-starter table gets (`ReedRoad`, `AbriReedRoad`), (`BeaconKeeper`, `MaruBeaconsAsk`), (`SmokehouseOrder`, `DovOrderOffer`), (`SchoolBell`, `DovBellOffer`), (`FourthPump`, `QuillPumpOffer`); the ninety-second list gets the nodes marked compulsory in section 2. `PeopleMarks.Repeats` gets `AbriReedRoadRepeat`, `AbriReedRoadCharted`, `AbriReedRoadWaiting`, `AbriManyHands`, `PellVeilRepeat`, `PellManyHands`, `OnaPimBack`, `MaruOnaWaits`. All five `QuestDef`s must exist before this file is copied in.
3. **Two counters in Driftreed.** Dov sells provisions and sends parts buyers west; Quill's third option is his parts. That wants `Driftreed_Dov` and `Driftreed_Quill` under `ShopUi.For`. As with Hadda, Quill's three options are all services and there is no "leave" row, on all five of his nodes.
4. **The line.** One interaction between `MaruArrival` and the first talks, setting `wend.lineHeld`; Maru's `line` barks run under it. It should be a hold, not a tie: that is the scene.
5. **Lights the world must draw.** Maru's lamp at the pilots' stage, seen from the weaving hall (long, long, short, at every tide until `wend.decided`); the two lamps of the lure lesson; Pim's glow-spots; Ona's, in `OnaDecides`; the slipped beacons (short, long, short); Maru's lamp at the portico. Every one is also spoken.
6. **People in more than one place.** Maru: radio in the Veil; the pilots' stage; the first island from `wend.tideTurned`; the portico from `MaruPortico` until she walks back with the party; the stage again from `wend.rotbloomDead`. She never enters the station. Pim: the dry pocket; the portico beside Maru; the bench in the hall from `wend.rotbloomDead`.
7. **Gates.** Pump three waits on `wend.pimFound`. The off-lane beacons, the bell-buoy, pump four and the vault wall are offered only at their quest's step. `DovOrderOffer` and `DovBellOffer` wait for the tide.
8. **Order.** `MaruDeadBeacon` and `SelaPumpOne` are said as the hand goes to the thing, before it answers. `SelaBloomDead` follows the kill. The shared `boss` bark ("We can stop shouting now.") would talk over the quiet that scene is for: leave it out for this boss, or let `SelaBloomDead` come first.
9. **Chapter 3's last lines give way.** `AbriReedRoad` and `PellVeilLantern` take the places of `AbriDinnerServed` and `PellAfterVote` once chapter 4 is in the build, as chapter 3's opening took `AbriLanternAfter`'s. Those two stay in the script and will only be heard in a build that stops at chapter 3.

## 9. Where the script departs from the brief

- **A second light signal.** The brief gives the lamps' count. The script adds the pilots' "still afloat" (long, long, short) as the thing Maru says to Ona that the party can't read until Pim explains it.
- **The lure is learning.** The brief's lures flicker without a rhythm. Here they flicker shorts, can't hold a long, and have lately got the two shorts right: that is how Pim, who knows the count, was taken.
- **Iri's quest goes through Dov.** Offer and thanks are both his, in her name. It reads better than a silent teacher, and Dov relaying what she "said quietly" is the joke.
- **Maru doesn't go inside.** She leads the mire, stops at the portico ("I don't walk under a roof that doesn't float") and keeps a lamp at the door. Pim is sent out to her before the fight, so nobody the party cares about is in the hall for it.
- **Ona's poles.** "No weaver of hers is going anywhere" is made a fact: twelve poles, eleven locked in the reed store and counted at each tide. At the end the store is open. Whether Ona herself travels is not said.
- **Abri's count is one over Teo's.** Four hundred and twelve heads, four hundred and eleven on the lists. Nobody says why; chapter 5 finds him in the garden.
- **Hadda's tip comes by way of Teo**, so the chapter opens the same whether or not the player spoke to her in Rootmarket.
- **The name is chalked on the dock door** by somebody, in the night. Who is left for chapter 5.
- **`BeaconKeeper` can start in the Veil** (stray off the lane) or from Maru in town at the end; both reach the one starting node.
- **The vault's reading names neither Hoarfell nor Oriel.** It gives the works mark shared by pumps and sentries, and a tally "up the cold road".
- **Pim before the last pump.** The brief's order of steps has the three pumps, then Pim; the map puts Pim between the second and third, so the quest does too.
