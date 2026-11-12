# Chapter 10 script manifest - A Route With Room To Turn Back

For the engineers wiring chapter 10. Script: [LastMooring.yarn](LastMooring.yarn). Barks: [barks.json](../barks.json). Brief: [ch10-a-route-with-room-to-turn-back.md](../../chapters/ch10-a-route-with-room-to-turn-back.md). Earlier manifests, in the same shape: [chapter 3](../ch03-manifest.md), [chapter 4](ch04-manifest.md), [chapter 5](ch05-manifest.md), [chapter 6](ch06-manifest.md), [chapter 7](ch07-manifest.md), [chapter 8](ch08-manifest.md), [chapter 9](ch09-manifest.md).

**What is here:** 78 nodes, 180 dialogue lines, no option labels (180 strings). Compiled clean with the project's own compiler (Yarn Spinner 3.2.4, the nine live files and the eight pending ones together: 0 errors, 639 nodes, no duplicate titles). No new Yarn commands: `setFlag`, `startQuest`.

**This file waits in `pending/`** until the chapter's quests are defined (three `QuestDef`s) and the expectations in section 8 are written. **It needs no new speaker and no new sheet.** Meret's sheet needs two expressions she has not used before (section 5).

**Where the lines are.** The operation is 22 nodes and 99 lines, and the fights between them are barks: every ally is heard doing their job by name (section 7). The epilogue is 56 nodes and 81 lines, 44 of them one line long.

## 1. Conventions this file follows

- Every title starts with its owning speaker id. Nobody is new. Dace's are `Survivor…`; `Abri…` is speaker `Keeper`; `Sela…` as before.
- Each quest is started by exactly one node. No node is offered at a point whose own flag it sets.
- **Where the brief and chapters 7 to 9 disagree, chapters 7 to 9 win.** Sorrel has nobody on it: Dace and his ten are aboard, and he talks the party over his own ridge by radio. The anchor has a release Meret drew herself. Drask is aboard Many Hands. Bryn's navigator has Sela's pencil. The beacon chain counts two short and one long.
- **Nobody speaks of killing Mother of Doors**, in a node or a bark. Idra's plan says it before anyone can ask: "Nobody shoots at her."
- **Nobody says the title.**
- Checked against the word tests: no retired phrase, no id-shaped word.

### One operation: who does what, and where it is heard

Each crew says its job once, in `OrrinRoll`, one line each. Then each is heard doing it.

| Crew | Says, in the roll | Heard doing it |
|---|---|---|
| Lastlight's herders | Hask: "We keep the young ones cold and asleep, and we hum. All the way through." | `hail.hum` (Hask, Iru) at Sorrel: "Cold slowly." "Never alone." "Sung to." Her sweeps narrow as they do. |
| The Wick-folk | Pell: "Two short, one long, at every turn. If you lose us, look for the count." | `hail.count` in the passage; the convoy closes up on the lanterns in `BrynFallsBehind` |
| Nacre's navigators | Bryn: "If the surveyor's busy, traffic follows us." | `hail.lead`: "Follow the pencil. She's drawn it true." |
| The Unsworn | Tovra: "It fits the anchor's own seats. There'll be hands on every clamp." | `hail.gear`, then `IshenAnchorSequence` |
| Driftreed | Maru: "If a hull opens, we get it breathing." | `MaruSectionHeld`: "Rough side to the breach." `hail.mats` |
| Xylos | Ruva: "Anybody put out of a cabin sleeps among the beds." | In view at Last Mooring; `TeoHomecoming` |
| The Crawl | Dorrow: "I keep your districts walking in step. They'll complain. Lovely." | `hail.step` |
| Hal and his nine | "If it breaks, bring it, or bring us." | `hail.cradle`: "Ket's on it. I'm only holding the lamp." |
| Mira | "Nobody goes through that gate uncounted. I'll want signatures." | `OrrinGateOpen`, `hail.manifest`, `OrrinRendezvous` |
| Neve | "I'm last through." | `hail.rear` |
| Ilo | "I read each name as its hull comes through. Aloud." | `IloFirstName`, `hail.names`, `OrrinRendezvous` |
| Orrin | "And I've the board." | `OrrinGateOpen`, `hail.board`, "Give her room." |
| Idra's crews | In her own plan: "My crews brace the boarding spine." | `IdraSpineBraced`, `hail.spine`: Kott forward, Sibb aft |
| Ishen | (not in the roll) | Reads the sequence at the anchor |
| Dace | (not in the roll) | Talks the party over his own ridge; has the one line when she goes |

### Meret

Twenty-five lines. She is courteous, exact and tired, as before, and she is not angry at any point.

| Phase | Before it | In it (her barks) |
|---|---|---|
| Ship to ship | `MeretShipToShip`: "I published that number. I never hid it. It is the worst thing I know, and I know it exactly." Then: "Come and check this." | `ship`: she tells them when not to shoot her shield, and marks a clamp "for you" |
| The gantry | `MeretGantry`: "You're very good at this. You were always going to be. I wish it were the same as being right." And: "Mind the third rung. It's the one thing aboard I never got right." | `gantry`: she names each machine as she uses it. They are the Saint's press, the Works' welding line and the Foundry's sluices, built properly. |
| The well | `MeretWell`: "If I stop because I'm tired, I've decided nothing. Make me wrong. I should like to see it done." | `duel`: she coaches, and once she asks: "Is the nursery clear? Tell me truthfully." |
| Yielding | `MeretYields`: Taren: "Tell your crews." She does, at once, in one order. "They were always better than their orders." | |
| The anchor opens | `MeretSeesItOpen`: "It opens." Then Taren: "It was built to open." Meret: "Yes." | |

**Her third "Yes."** The first two are chapter 5's, to Sela and to Taren. This one is to Taren again, and it is the same word on the same face (`Neutral`). His line is his own from the first clamp on Oriel. Nothing follows it in the node.

**Her sequence is the same three steps in a duller hand.** Ishen reads it from section eleven: "Take up the load. Hold for the slack. Release all clamps as one." Tovra: "It's the same drill. She's only spelt it worse." The cadence at the anchor is pull twelve, ease three, and Idra calls it.

**Custody** is `IloCustody`: her own former surveyor comes for her, with the list. "Is there a chair?" "There's a chair." "I'll stand, I think. They did."

**The account** is `MeretAccount`, in the epilogue: one person a day, by her own request. "Two hundred and four thousand, six hundred and eighteen to go."

### Room

The phrase is said twice in the chapter and never explained.

| Where | Who | Line |
|---|---|---|
| `BrynFallsBehind` | Orrin, on the board, to the whole convoy, as Sela turns round in the lane | "Close up on the lanterns, and leave the middle of the lane empty. She's turning round. Give her room." |
| `JunLastLine` | Taren, to nobody in particular | "Then give them room." |

The first is the title earned: a town that knows how to stand back from a joint stands back from one of its own. In chapter 1 Taren's answer to the pears was that he needed room. The last line of the game is him giving it.

### Lines that come back

| First said | Now |
|---|---|
| Chapter 1, Taren to Sela: "I'd already counted you in." | Sela: "Vosk. I'm going back for her." Taren: "I'd already counted myself in." |
| Chapter 8 and 9, Bryn to a burning ferry and to a disabled cutter: "You're traffic. I've got you." | Sela, to Bryn, with the line on: "You're traffic. I've got you." Bryn: "That's my line." |
| Chapter 6, Idra: "I don't hum." | In the passage, the young road alongside: "I'm humming. Nobody is to remark on it." |
| Chapter 7, Taren at the first clamp: "Somebody was proud of this." "It was built to open." | Both, aboard the Continuance and at the anchor |
| Chapter 5, the captain of the Steady Hand: "No declaration is needed." | He stands aside: "You may pass. No declaration is needed." |
| Chapter 5, Meret: "Rough side to the smoke. That's right, and it is never fitted right." | Maru: "Rough side to the breach." |
| Chapter 5, Meret: "That is in writing as well." | Taren: "Is it enough?" Meret: "No. That is in writing as well." |
| Chapter 9, Hal to Taren: "Hold the lamp." | Hal, by radio: "Ket's on it. I'm only holding the lamp." |
| Chapter 6, Idra's brace, two columns | "I've sanded the brace. Both sides. I'm told that's allowed." |
| Chapter 1, Taren and Mira at the yellow line | `JunLastLine` |

### The last scene

Jun has put the pears one crate past the new line. Four lines:

> Taren: Jun. The yellow line is where the stall stops.
> Mira: It did stop.
> Jun: Then the pears needed room.
> Taren: Then give them room.

Taren's line is chapter 1's, with Jun's name where Mira's was. Mira starts the answer she has given for Jun for thirty-one years, and Jun finishes it herself. Then the credits. Nothing else should be heard after Taren's line: no bark, no toast.

### Numbers the script keeps

312,000 and 204,619 (Sela and Meret say both); the fortieth day (Abri); nineteen years (Bryn); thirty-one of thirty-one (the captain); Meret is sixty-one; the anchor pulls for twelve and eases for three; Lastlight's count, asked for at last, is sixty-one (`hail.names`); the epilogue is seven weeks on (Drask, Dorrow); Mira's pitch is eleven now.

## 2. Nodes

Lines are dialogue lines. "Compulsory" marks the main path; the longest, `OrrinRoll`, is 13 lines and 58 seconds by the ninety-second test's arithmetic. Nothing else passes 35.

### Many Hands, among the Halo's districts

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IdraOnePlan` | 6 | Idra | Idra | **Compulsory.** Idra at the chart table, `chapter9.complete`. Five places, numbered. | *end.planHeard* | |
| `OrrinRoll` | 13 | Orrin, Hask, Pell, Bryn, Tovra, Maru, Ruva, Dorrow, Hal, Mira, Neve, Ilo | Orrin | **Compulsory.** Orrin at his board, straight after. Each crew stands as it is pointed at. | `end.planSaid` | `startQuest TurnBack` |
| `AbriFortiethDay` | 1 | Keeper | | Abri, `end.planSaid`, until Sorrel | | |
| `DraskBeforeTheField` | 2 | Drask | | Drask, in custody, `end.planSaid`, until the epilogue | | |
| `SelaLastSurveyOffer` | 4 | Sela, Taren | | Interaction at the chart table, `end.planSaid`, `LastSurvey` not started. Also open in the epilogue. | | `startQuest LastSurvey` |

### Sorrel's second sky

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `SurvivorSecondSky` | 4 | Survivor, Sela, Hask, Idra | Dace | **Compulsory.** Crossing point as the ridge and the clamp come into view. Dace and Hask by radio. | *end.sorrelSeen* | |
| `SurvivorMotherGoes` | 1 | Survivor | | **Compulsory.** As she goes and the young follow. One line, by radio, from the man who slept over them. | `end.motherFreed` | |

### Last Mooring

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `MaruSectionHeld` | 3 | Keeper, Maru | Maru | **Compulsory.** When Many Hands' galley quarter is hit, in view. By radio. | `end.sectionHeld` | |
| `OrrinGateOpen` | 2 | Orrin, Mira | Orrin | **Compulsory.** The Tollkeeper's gate motor dies; the first hulls come through behind the party. | `end.gateOpen` | |

### The Continuance

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IdraSpineBraced` | 3 | Idra, Sela, Taren | Idra | **Compulsory.** At the top of the boarding spine. | `end.boarded` | |
| `PatrolLetsPass` | 3 | Patrol | | **Compulsory.** The captain of the Steady Hand, across a maintenance deck with his crew. He steps aside on his third line. | *end.captainPassed* | |
| `MeretShipToShip` | 6 | Meret, Sela, Taren | Meret | **Compulsory.** Before phase one. Her voice from her craft. | *end.meretHailed* | |
| `MeretGantry` | 4 | Meret, Taren | Meret | **Compulsory.** Phase one beaten (`Npc.Say`); checkpoint. She is at the gantry's rail. | `end.meretShip` | |
| `MeretWell` | 6 | Meret, Sela, Idra | Meret | **Compulsory.** Phase two beaten; checkpoint. In the control well. | `end.meretGantry` | |
| `MeretYields` | 5 | Meret, Taren | Meret | **Compulsory.** At zero. She is standing. | `end.meretYielded`, `end.crewsOrdered` | |
| `IshenAnchorSequence` | 6 | Ishen, Tovra, Idra, Taren | Tovra | **Compulsory.** The crews come into the well. It ends on Idra's "Ease.", and the player's one hold follows. | *end.sequenceRead* | |
| `MeretSeesItOpen` | 5 | Meret, Taren | Meret | **Compulsory.** As the four clamps let go and the road moves. | `end.anchorReleased` | |
| `IloCustody` | 4 | Ilo, Meret | Ilo | **Compulsory.** Straight after. Civilian crews are at the well's door. | *end.custody* | |

### The Returning Passage

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IloFirstName` | 4 | Ilo, Keeper, Idra | Ilo | **Compulsory.** As the run begins, the young road alongside. By radio. | *end.namesBegun* | |
| `BrynFallsBehind` | 9 | Bryn, Sela, Taren, Idra, Orrin | Sela | **Compulsory.** Near the far end of the run. The convoy opens the middle of the lane on Orrin's line. | `end.passageRun` | |
| `BrynOnTheLine` | 4 | Bryn, Sela | Sela | **Compulsory.** The tow-drones are off and the line is on. | `end.tugFetched` | |
| `OrrinRendezvous` | 4 | Ilo, Orrin, Mira, Idra | Orrin | **Compulsory.** The tug comes through, last. | `end.rendezvous` | |

### Neighbourhood Afloat, seven weeks on

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `RuvaPassageRules` | 3 | Ruva | | Ruva, chairing in the square. It gives out `Homecomings`. | | `startQuest Homecomings` |
| `IruTernsMessage` | 2 | Iru | | Interaction at the message board: a knotted cord | *end.msgTern* | |
| `PellAumisMessage` | 2 | Pell | | Interaction at the gallery lantern | *end.msgAumi* | |
| `RuvaSennsMessage` | 2 | Ruva | | Interaction at the message board: a seed tag | *end.msgSenn* | |
| `PimQuillsMessage` | 2 | Pim | | Interaction at the message board: a page of valve drawings with a note in the corner | *end.msgQuill* | |
| `MeretAccount` | 5 | Meret, Taren | Meret | Meret, in a plain room off the square, the door open. She is standing. | *end.accountHeard* | |
| `DraskWatches` | 2 | Drask | | Drask, at a window over the square | | |
| `OrrinDirectorship` | 5 | Orrin, Taren | Orrin | **Compulsory.** Orrin at the new board. | `end.handrail` | |
| `SelaReturnLeg` | 4 | Bryn, Sela | Sela | **Compulsory.** Bryn at the travel terminal. | *end.routeFiled* | |
| `SelaSurveyHome` | 2 | Sela | | Interaction at the route chart once the course has been flown (*end.surveyFlown*), `LastSurvey` at step 1 | `sela.surveyFiled` | |
| `IdraTiedUp` | 4 | Idra, Taren | Idra | Interaction at the moorings: her cutter and Sela's ship on one cleat | *end.cleatSeen* | |
| `JaskNewSky` | 1 | Jask | | Jask, on the gallery, looking up | | |
| `PipRota` | 1 | Pip | | Pip, on the gallery, mornings | | |
| `TamSkyGarden` | 1 | Tam | | Tam, on the sky-garden deck, if Tam did not go back (`quest.StillGround.complete` not set) | | |
| `JunLastLine` | 4 | Taren, Mira, Jun | | **Compulsory, the last scene.** At the fruit stall, `end.handrail`: the handrail is mended, the line is new, and the pears are one crate past it. | `game.complete` | |

### Homecomings: nine visits

Each is an interaction offered at `Homecomings` step 0 (`report Homecoming`), in any order. None sets a flag.

| Node | Lines | Speaker | Community | Where |
|---|---|---|---|---|
| `AbriHomecoming` | 1 | Keeper | Tallow Drift | The east terrace |
| `TeoHomecoming` | 1 | Teo | Xylos | The garden arcade |
| `MaruHomecoming` | 1 | Maru | Driftreed | The low street, the raft bay |
| `HaskHomecoming` | 1 | Hask | Lastlight | Under the rib-arch over the square |
| `PellHomecoming` | 1 | Pell | Lanternwick | The lantern gallery |
| `TovraHomecoming` | 1 | Tovra | The Unsworn | The forge, by the garden stair |
| `OdaHomecoming` | 1 | Oda | Nacre | A door, with an empty frame beside it |
| `DorrowHomecoming` | 1 | Dorrow | The Crawl | The sky-garden deck |
| `MiraHomecoming` | 1 | Mira | Cinder Halo | Pitch eleven, in the arcade |

### Homecomings: thirty-two extras

One for each side quest from chapter 1 to chapter 9, as at the Coronach, each standing in its community's street and each there only if its quest was finished. They are not counted by the quest. `Hushwell` again has none.

| Node | Lines | Speaker | Needs |
|---|---|---|---|
| `MiraHomeCrystals` | 1 | Mira | `quest.MiraCrystals.complete` |
| `NeveHomeParcels` | 1 | Neve | `quest.NeveParcel.complete` |
| `OdaHomeStamp` | 1 | Oda | `quest.OdaBerth.complete` |
| `SurvivorHomeLamps` | 1 | Dace | `quest.SorrelBeacons.complete` |
| `PellHomeMothersLamp` | 1 | Pell | `quest.PellsLamp.complete` |
| `IshenHomeShelves` | 1 | Ishen | `quest.IshensRecord.complete` |
| `BrekkHomeAwning` | 1 | Brekk | `quest.UnswornRations.complete` |
| `BinnaHomeBaskets` | 1 | Binna | `quest.ThreeBaskets.complete` |
| `RuvaHomePost` | 1 | Ruva | `quest.MooringPost.complete` |
| `JaskHomeThree` | 1 | Jask | `quest.Questions.complete` |
| `TeoHomeTags` | 1 | Teo | `quest.ClaimTags.complete` |
| `MaruHomeHarbourLamp` | 1 | Maru | `quest.BeaconKeeper.complete` |
| `DovHomeSmokehouse` | 1 | Dov | `quest.SmokehouseOrder.complete` |
| `DovHomeBell` | 1 | Dov | `quest.SchoolBell.complete` |
| `PimHomeKey` | 1 | Pim | `quest.FourthPump.complete` |
| `CorraHomeKettles` | 1 | Corra | `quest.KettleTrade.complete` |
| `JaskHomeFare` | 1 | Jask | `quest.StowawayFare.complete` |
| `IloHomeBook` | 1 | Ilo | `quest.ClaimsLedger.complete` |
| `HaskHomeStrays` | 1 | Hask | `quest.StrayBeasts.complete` |
| `IruHomeLamps` | 1 | Iru | `quest.ShrineLamps.complete` |
| `IdraHomeCrew` | 1 | Idra | `quest.CutterDown.complete` |
| `RuvaHomeShoot` | 1 | Ruva | `quest.EmptyGardens.complete` |
| `IshenHomeMoons` | 1 | Ishen | `quest.MoonNames.complete` |
| `PellHomeOriel` | 1 | Pell | `quest.LumesLantern.complete` |
| `TovraHomePikes` | 1 | Tovra | `quest.QuietHands.complete` |
| `AbriHomeGlass` | 1 | Keeper | `quest.RelayGlass.complete` |
| `BrekkHomeKettle` | 1 | Brekk | `quest.OdrunsKitchen.complete` |
| `PipHomeFile` | 1 | Pip | `quest.PipsOath.complete` |
| `MiraHomeTable` | 1 | Mira | `quest.SecondInventory.complete` |
| `HalHomeBenches` | 1 | Hal | `quest.PortableBenches.complete` |
| `NeveHomeSocks` | 1 | Neve | `quest.ReturnAddress.complete` |
| `TamHomeFrame` | 1 | Tam | `quest.StillGround.complete` |

`RuvaHomeShoot` is where chapter 7's jar of thousand-year-old seed comes up: one shoot, and nobody knows what it is.

### Suggested routing (first match wins)

| Speaker | Beats, in order | Npc tiers |
|---|---|---|
| Orrin | `OrrinDirectorship` in the epilogue; `OrrinRoll` aboard | After `end.handrail` he has no further node: he is at the board, chalking |
| Ruva | none | epilogue: `RuvaPassageRules` |
| Meret | none | epilogue: `MeretAccount`, every time |
| Drask | none | aboard `DraskBeforeTheField`; epilogue `DraskWatches` |
| Keeper | `AbriFortiethDay`, then chapter 9's | |
| Everyone with a visit or an extra | none | Their visit, then their extra or extras, in the table's order |

## 3. Flags

**From the brief, and who sets them:** `end.planSaid` (`OrrinRoll`), `end.clamp1..4` (world), `end.motherFreed` (`SurvivorMotherGoes`), `end.sectionHeld` (`MaruSectionHeld`), `end.brace1..3` (world), `end.gateOpen` (`OrrinGateOpen`), `end.boarded` (`IdraSpineBraced`), `end.meretShip` (`MeretGantry`), `end.meretGantry` (`MeretWell`), `end.meretYielded` and `end.crewsOrdered` (`MeretYields`), `end.anchorReleased` (`MeretSeesItOpen`), `end.passageRun` (`BrynFallsBehind`), `end.tugFetched` (`BrynOnTheLine`), `end.rendezvous` (`OrrinRendezvous`), `end.epilogue` (world, on arriving in the neighbourhood), `end.handrail` (`OrrinDirectorship`), `game.complete` (`JunLastLine`).

**Added by the script:** `sela.surveyFiled`.

**Suggested for the world to own:** the italic flags in section 2.

## 4. Quests

Kinds are `ObjectiveKind` (TalkTo 0, Reach 1, Kill 2, Collect 3, Interact 4, Flag 5). `TurnBack` is the main quest (`main: true`, `chapter: 10`).

### TurnBack - A Route With Room To Turn Back (main)

Description: `Every road out runs through one passage. Stillwater's anchor is closing on it at Sorrel, over the nursery, and the whole convoy is going through together or not at all.`

| # | Kind | Target | Step text | Points at |
|---|---|---|---|---|
| 0 | Flag | `end.planSaid` | Hear the plan, and who is doing which part of it | ManyHands, speaker `Orrin` |
| 1 | Reach | `SorrelSecondSky` | Go back to the first moon | SorrelSecondSky, from the chart |
| 2 | Flag | `end.motherFreed` | Open the four clamps on Mother of Doors: take the weight, wait for the slack | SorrelSecondSky, report `Clamp` |
| 3 | Reach | `LastMooring` | Bring the convoy up to Last Mooring | LastMooring, from SorrelSecondSky |
| 4 | Flag | `end.gateOpen` | Break the Tollkeeper's braces, and stop its gate motor on foot | LastMooring, encounter `Tollkeeper` |
| 5 | Flag | `end.boarded` | Go up the Continuance's boarding spine | Continuance, the spine |
| 6 | Flag | `end.meretShip` | Put out the three shield projectors on the director's craft | Continuance, encounter `MeretShip` |
| 7 | Flag | `end.meretGantry` | Open the two arm couplings on her frame | StillwaterChamber, encounter `MeretGantry` |
| 8 | Flag | `end.crewsOrdered` | Answer her in the control well | StillwaterChamber, encounter `MeretWell` |
| 9 | Flag | `end.anchorReleased` | Take the last collar: one hold, on Idra's call | StillwaterChamber, the anchor |
| 10 | Flag | `end.passageRun` | Run the passage with the convoy | ReturningPassage |
| 11 | Flag | `end.tugFetched` | Go back up the lane for the tug Welcome Back | ReturningPassage, the tug |
| 12 | Flag | `end.rendezvous` | Bring her through to the rendezvous | ReturningPassage, the far mouth |
| 13 | Flag | `end.handrail` | See the new neighbourhood, and then Orrin | NeighbourhoodAfloat, speaker `Orrin` |
| 14 | Flag | `game.complete` | Mend the handrail on the garden stair, and repaint the line | NeighbourhoodAfloat, the fruit stall |

Reward: the ending.

### LastSurvey - The Last Nursery Survey (Sela's chart)

Description: `The young cut a road of their own on the way out. It is narrow, it turns twice on itself, and it isn't on the way to anything.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact | `NurseryRoad` | Fly the nursery's new road to its far end, and back | Fly the nursery's new road |
| 1 | Flag | `sela.surveyFiled` | Put it on the route chart | Put it on the chart |

Reward: a new prospect on the route chart ("Green, I think. I didn't land."), and `SelaSurveyHome`.

### Homecomings - Homecomings (the square)

Description: `Nine communities came through the passage. Ruva is chairing motion nine and would like somebody else to go and see where they have all settled.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x9 | `Homecoming` | Find where each of the nine has settled in the neighbourhood | Visit all nine communities |

Reward: the game's last cache. The extras stand beside the visits and are not counted.

## 5. Display names

### Speakers

**Nobody new.** Thirty-three people speak, and every one has a sheet or is owed one from an earlier chapter.

### Meret's sheet

She has used five expressions in chapters 5 to 9: Neutral, Tender, Amused, Sad, Curious. Chapter 10 uses those and **two she has not used before, once each**:

| Expression | Line | What it is on her |
|---|---|---|
| Determined | "Not yet. If I stop because I'm tired, I've decided nothing. Make me wrong. I should like to see it done." | The only time she sets her jaw. Still not anger. |
| Shocked | "It opens." | The only time in the game her face is not composed. It should be small: the eyes, and nothing else. |

25 lines: Neutral 9, Tender 6, Sad 4, Curious 2, Amused 2, Determined 1, Shocked 1. "Yes." is Neutral, as it was twice in chapter 5.

Nobody else uses an expression new to them. Idra uses five of her six, and Flustered three times: for the humming, for having nothing left to count, and for the cleat.

### People named but never speaking

| Name | Who |
|---|---|
| The navigator with the pencil | Leads the convoy through the passage. Sela wants the borrowed pencil back, not that one. |
| Kott and Sibb | Forward ring and aft ring on the spine; afterwards the harbour winch and the docking school |
| Old Tern, Aumi, Old Senn, Quill | Each sends a message; each is read aloud by somebody who knows them |
| Ona, Iri, Lume, Mother Odrun, Oda's mother, the Arrens, Ket | As before |
| A grower from Rootmarket | Page four. Fifty hands and a seed house. The first to ask Meret her question. |

### Enemies and bosses (`EnemyDef.displayName`)

| Id | Display | Note |
|---|---|---|
| `MotherOfDoors` | MOTHER OF DOORS | Epithet: `A grown road, come back for her young`. Status: `SHE IS FRIGHTENED  ·  OPEN A CLAMP`. Not "defeated" in any string: she goes. |
| `Tollkeeper` | THE TOLLKEEPER | Epithet: `An anchor-fleet carrier, with a cage for a gate` |
| `MeretShip` | DIRECTOR MERET VENN | Epithet: `Aboard the Continuance, with the last anchor`. Phase one. |
| `MeretGantry` | DIRECTOR MERET VENN | Epithet: `Every machine you have beaten, built properly`. Phase two. |
| `MeretWell` | MERET VENN | Epithet: `Sixty-one, in an emergency frame, with a cough`. Phase three. |

The boss ids must be exactly these for their barks to be found. Ordinary enemies are the brief's and need no words from the script.

### Zones (`ZoneDef.displayName`)

| Id | Display |
|---|---|
| `SorrelSecondSky` | Sorrel's Second Sky |
| `LastMooring` | Last Mooring |
| `Continuance` | The Continuance |
| `StillwaterChamber` | The Anchor Chamber |
| `ReturningPassage` | The Returning Passage |
| `NeighbourhoodAfloat` | Neighbourhood Afloat |

### Floor signs and landmark labels

- The Continuance: `BOARDING SPINE`, `MAINTENANCE DECK FOUR`, `ANCHOR WORKS`, `FRAME GANTRY`, `CONTROL WELL`
- The neighbourhood: `THE SQUARE`, `MESSAGE BOARD`, `EAST TERRACE`, `GARDEN ARCADE`, `LOW STREET`, `LANTERN GALLERY`, `SKY-GARDEN`, `PITCH ELEVEN`, `MOORINGS`, `TRAVEL TERMINAL`, `GARDEN STAIR`

### Prompts

`Take the clamp's weight` (as at Oriel), `Take the last collar`, `Read the message`, `Stop and listen`, `Look at the moorings`, `Mend the handrail`, `Repaint the line`.

### Items

The script names none.

## 6. Objective strings

Where the panel takes its line from the main quest's current step, section 4's step text is what shows. These are the rows the steps can't say.

| Zone | When | Label |
|---|---|---|
| SorrelSecondSky | a clamp's guns up | Fly the clamp's guns down, then dock |
| SorrelSecondSky | on a clamp's works | Take its weight and wait for the slack (1/4) |
| SorrelSecondSky | the fourth clamp | Open the last clamp alongside her as she moves |
| LastMooring | the cage closing | Weave out before the cage closes |
| LastMooring | on the arm | Stop the gate motor before the arm draws in |
| StillwaterChamber | her shield up | Hold your fire: her shield gives it back |
| StillwaterChamber | the anchor | Wait for Idra's call |
| ReturningPassage | the run | Keep to the lanterns: two short, one long |
| ReturningPassage | turned back | Fly against the wake to the tug |
| ReturningPassage | at the tug | Clear the tow-drones off her (1/3) |
| NeighbourhoodAfloat | `end.epilogue` | There is no hurry |

The end card is the credits (section 10). There is no "chapter complete" toast.

## 7. Barks added

Existing entries are untouched. Every new hero key is there for Taren, Sela and Idra alike. **The release at each of Mother of Doors' clamps reuses chapter 7's `clamp` key** ("Taking its weight. Cover me."): nothing new is introduced.

| Speaker | Keys | Used by | Wiring |
|---|---|---|---|
| Taren, Sela, Idra | `bossphase.MotherOfDoors`, `bossopening.MotherOfDoors`, `bossphase.Tollkeeper`, `bossopening.Tollkeeper`, `bossphase.MeretShip`, `bossopening.MeretShip`, `bossphase.MeretGantry`, `bossopening.MeretGantry`, `bossphase.MeretWell`, `bossopening.MeretWell` | `BossController.Situation` | Automatic for those ids |
| Taren, Sela, Idra | `mother`, `young` | Her sweep; the young moving in the bore | `mother` is two in turn: wide, then narrower |
| Hask | `hail.hum` | Over the whole fight: the three rules of the long sleep, as a rhythm | `RadioHail`; three in turn, round again |
| Iru | `hail.hum` | As clamps come off | Two in turn: after the first, after the third |
| Taren, Sela, Idra | `sectionhit` | A convoy section is struck (before `MaruSectionHeld`) | |
| Maru | `hail.mats` | After it, if the player looks back | |
| Taren, Sela, Idra | `tollbrace` | A brace destroyed | Three in turn |
| Taren, Sela, Idra | `standaside`, `helpers` | Compact crews who step back, and one who helps | Chapter 9's `warden` still serves for those who give ground |
| Patrol | `hail.aside` | After `PatrolLetsPass`, to his own crews | |
| Idra | `hail.spine` | When she is not the active hero | Two in turn |
| Meret | `ship`, `gantry`, `duel` | One set to a phase | `BarkService.Play("Meret", …, true)`: two, three and four in turn. The third `duel` line is a question and should be given a moment of quiet after it. |
| Taren, Sela, Idra | `returnfire` | Her shield is up | |
| Taren, Sela, Idra | `oldmachines` | The press, the welding line, the coolant, as each is first used | Three in turn, in that order |
| Tovra | `hail.gear` | As the smiths fit the gear, before `IshenAnchorSequence` | Three in turn |
| Taren, Sela, Idra | `passage`, `youngchoir` | The run begins; the young come alongside | |
| Pell | `hail.count` | At each turn of the passage | Three in turn |
| Ilo | `hail.names` | As hulls pass the mouth | Six in turn. Each ends "Through." |
| Orrin | `hail.board` | Half, three in four, one to come | Three in turn |
| Mira | `hail.manifest` | During the run | Two in turn |
| Dorrow | `hail.step` | During the run | Two in turn |
| Hal | `hail.cradle` | During the run | |
| Neve | `hail.rear` | During the run | Two in turn |
| Bryn | `hail.lead` | Before her tug fails | |
| Taren, Sela, Idra | `wake` | Flying back up the lane | |
| Taren, Sela, Idra | `tugdrones` | A tow-drone cleared off the tug | Three in turn |

## 8. What the script assumes

**No new commands or dialogue features, no new speakers, no new sheets.**

1. **Meret's sheet** gains Determined and Shocked (section 5).
2. **Expectations** (`[ContentRegistry.Expectations]`). No node has choices or ends in a service. No node needs `book.Optional`: the longest is 58 seconds. `PeopleMarks.Repeats` gets `AbriFortiethDay`, `DraskBeforeTheField`, `MeretAccount`, `DraskWatches`, `JaskNewSky`, `PipRota`, `TamSkyGarden`, the four message nodes, and every visit and extra in the epilogue. The `Resources` copy of `barks.json` needs bringing level.
3. **The roll.** In `OrrinRoll` each speaker stands, or steps forward, on their own line. Nobody cheers.
4. **Mother of Doors.** Four releases, each the hold from Oriel with the partner covering; the fourth alongside her as she moves. Hask's hum runs under all of it and her sweeps narrow with each clamp. When she goes, `SurvivorMotherGoes`, and nothing else for a while.
5. **Meret between phases.** She is seen and standing in `MeretGantry`, `MeretWell` and `MeretYields`. She does not fall, kneel or shout. Each of the three is a checkpoint.
6. **The anchor.** Tovra's smiths at four clamps, Idra's people on the braces, Ishen with the page, Taren at the last collar. `IshenAnchorSequence` ends on "Ease." and the player's one hold begins on that word. `MeretSeesItOpen` plays as the clamps let go. Nothing barks between "Yes." and `IloCustody`.
7. **The turn back.** On Orrin's line in `BrynFallsBehind` the convoy closes to the lanterns at the lane's edges and the middle is left empty. Sela's ship is the one that turns, whoever the player is flying; Taren's and Idra's turn with it.
8. **The epilogue is a town to walk round.** No combat, no timer. Its order is the player's, except that `JunLastLine` waits on `end.handrail`, and Orrin's offer waits on nothing.
9. **The moorings.** Idra's cutter and Sela's ship are on one cleat, and there are forty.
10. **The last scene.** A new yellow line, wet paint; a mended handrail in view; a crate of pears one crate past the line. After Taren's last word, the credits. Nothing else is said.
11. **If a side quest was left undone**, its extra is simply not there, and the visit beside it stands alone.

## 9. Where the script departs from the brief

- **Sorrel is empty.** Dace's crew left in chapter 9. He is aboard and talks the party in by radio, and the single line when Mother of Doors goes is his: "Go on, then. You've had the quiet. Have the rest."
- **Meret's three lines are rewritten**, keeping the brief's first two as their openings (section 1). She fights by naming what she is using and telling them how to beat it.
- **Her release is the same three steps, worded as an engineer would.** Tovra recognises the drill. Meret's line when it opens is two words, and then why she kept the page clean.
- **A third "Yes."**
- **Ilo takes her into custody**, with the list, and she declines the chair.
- **The room is given by Orrin**, to Sela, on the board, in the passage. Bryn objects that Sela is the chart; Sela says the navigator has her pencil.
- **Idra's line as they turn** is "It's a rescue, Irun. You're in my trade now. Try to keep up." Her tally ends in the epilogue: she has sanded the brace.
- **Idra hums**, once, and forbids comment.
- **The last scene has three speakers.** Mira begins the old answer and Jun finishes it. The last words of the game are Taren's: "Then give them room."
- **The fortieth day** is noted once, by Abri, as the day it would have been.
- **The thousand-year-old seed comes up**, one shoot, if `EmptyGardens` was done.
- **Tam is in the epilogue either way**: on the sky-garden deck, or back for a week.
- **`Homecomings` counts nine visits.** The thirty-two extras are beside them and uncounted.
- **`LastSurvey` is given at the chart** by Sela, to herself, with a line about her pencil.
- **Lastlight's count** is finally asked for and read: sixty-one.
- **No one says "defeated" or "won"** of Mother of Doors or of Meret, in any line or label I have written. "Killed" is said once, by Idra, as a rule: the director is not to be.

## 10. The credits

Names are in `docs/CREDITS.md`. This is only the frame.

**Before the roll**, over the convoy under its new sky, two lines, plain type, one after the other:

> Nobody came through this alone.
> These are the hands.

**Section headings**, in the game's voice. Use the ones that have people under them.

| Heading | For |
|---|---|
| WHO DREW THE CHART | Direction and design |
| WHO SAID IT AT THE DOORS | Writing |
| WHO BUILT THE STREETS | World, level and environment |
| WHO PAINTED THE LINE | Art, portraits, animation and interface |
| WHO HUMMED | Music and sound |
| WHO KEPT THE LAMPS | Engineering and tools |
| WHO COUNTED TWICE | Testing |
| WHAT WE BORROWED, WITH THANKS | Engines, libraries, licensed and third-party work |
| WHO HELD THE LADDER | Special thanks |
| AND WHOEVER WALKED IT THIS FAR | The player. One line under it: `Thank you. Easy on the landing.` |

**After the roll**, alone on the screen, before the title returns:

> The pears keep a schedule.
