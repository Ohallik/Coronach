# Chapter 8 script manifest - Harrowgate (the Vaskar Reach)

For the engineers wiring chapter 8. Script: [Harrowgate.yarn](Harrowgate.yarn). Barks: [barks.json](../barks.json). Brief: [ch08-harrowgate.md](../../chapters/ch08-harrowgate.md). Earlier manifests, in the same shape: [chapter 3](../ch03-manifest.md), [chapter 4](ch04-manifest.md), [chapter 5](ch05-manifest.md), [chapter 6](ch06-manifest.md), [chapter 7](ch07-manifest.md).

**What is here:** 62 nodes, 254 dialogue lines, no option labels (254 strings). Compiled clean with the project's own compiler (Yarn Spinner 3.2.4, the nine live files and the six pending ones together: 0 errors, 457 nodes, no duplicate titles). No new Yarn commands: `setFlag`, `startQuest`, `shop`.

**This file waits in `pending/`** until the chapter's people and quests are defined: five new portrait sets (Drask, Tovra, Odrun, Pip, Bryn), five `QuestDef`s, and the expectations in section 8.

**Where the lines are.** The Picket, the Flows and the Foundry have 19 nodes between them, most of them five lines or fewer, because those are the fights. Harrowgate has 33. The fights themselves are carried by barks (section 7).

## 1. Conventions this file follows

- Every title starts with its owning speaker id: `Brekk…`, `Tovra…`, `Vessa…`, `Hessa…`, `Pip…`, `Odrun…`, `Drask…`, `Bryn…`, `Meret…`, `Idra…`, the returning `Abri…` (speaker `Keeper`) and `Pell…`, and `Sela…` for the party with nobody else to walk up to.
- Each quest is started by exactly one node. A talk that completes a step sets a flag for a `Flag` step. No node in this chapter is offered at a quest-gated point, so nothing here meets the discovery-report trap.
- No node has choices.
- **What people call each other.** Idra: "Vosk", "Irun", "smith" (Brekk), "Archivist" as before. Tovra: "the fitter" (Taren), "Rauk" (Idra), "Tamar" (Brekk), "Mother" (Odrun). Odrun: "Kesh" (Tovra), "Drask", "Tamar". Drask: "Officer Sorn", "Shapers", his soldiers by first name. Vessa: "rescue frame", "Shapers", "Gate" (her sister). Hessa: "Shapers". Bryn: "traffic", for everyone, and hulls by their line number.
- **Tovra swears by one tool.** "Tongs." It is her whole vocabulary of surprise, and she says it twice: at the three lines from Oriel, and when Odrun makes her sit. Never in anger.
- Checked against the word tests: no retired phrase, no id-shaped word.

### The lock: what is said, and what isn't

| Moment | Node | What is said |
|---|---|---|
| Taren says it | `IdraPicketPlan` | "It's mine." Then, asked to say it again: "I installed that lock. It was a job. The order said 'unregistered traffic'. I knew who that meant. I took the job." Nothing else. No reason is offered and none is asked for. |
| Idra's answer | `IdraPicketPlan` | "How many bolts?" She does not say she forgives him or that she doesn't. Her next line is point four of the plan. |
| Sela | `IdraPicketPlan` | "I'll fly point." Three words, and not about it. |
| The orders | `IdraLockReached`, barks `hail.lock` and `lock` | Taren starts to explain the frame; Idra: "I hadn't asked yet." "No." After that she calls each piece and he answers with the piece's name and "Off." |
| It is noticed | `IdraLockOff` | Idra: "Irun flew that whole lane and said nothing to you. She talks through everything. If I were you, that's the one I'd mind." Taren: "I do." |
| It is noticed again | `AbriThreeWords` | Optional. "And Sela said three words. I've heard her say more than that to a kettle." |
| Sela's one remark | `SelaTrophyGallery` | In a room of things taken from conquered worlds: "Everything in this room was a job, to somebody." Taren: "Yes." It is the only thing she says about it in the chapter. |
| The tally | `IdraLockCounted` | Late and small. "That's a new mark." "The lock." "Which side?" "It's counted." |

Idra's two columns are chapter 6's: the left side of the brace is who she has pulled out, the right side is what she owes. Which one the lock went in is not said here and should not be said later.

### The release on a living road

The words are chapter 7's, unchanged, and this time a forewoman shouts them as work calls.

| Step | Who says it | What happens |
|---|---|---|
| | Tovra: "Hands! Four clamps, fifteen to a clamp. Knock the skin off the seats. Tamar, the fourth is yours." | Sixty smiths. The hand seats were poured a finger deep and called solid (`TovraFirst`). |
| Take its weight | Tovra: "Take its weight! Lean till the lock's carrying nothing. If your clamp's still singing, you're not leaning." | |
| Wait for the slack | Tovra: "Wait for the slack. Nobody opens on the pull. Nobody. Rauk, call it." Idra: "Pulling. Pulling. Ease." | **The cadence is nine and four**: it pulls for nine and eases for four. Idra timed it in `SelaCagedRoad`, before the fight. |
| Let go together | Tovra: "Let go together!" | |
| Then give it room | Sela: "Back. Everyone, to the walls. Give it room." | Nobody explains the phrase. It does not move at first. Sela: "It doesn't know it can." |

**Who saw it** (Idra, in `TovraRelease`): sixty smiths; eleven of the garrison, with their hands empty; one dig-master, on the gantry. "I want every name. Somebody is going to ask who saw it." Chapter 10 can ask.

The player does not hold a clamp this time. That is on purpose: at Oriel the party did it, here other people's hands do.

### "You didn't see me"

Brekk said it in chapter 2. In this chapter it turns out to be how the whole town talks: people who live inside an institution of war look after each other by not having seen things. It is never remarked on.

| Who | Line |
|---|---|
| Tovra | "Then by now they've been read, burnt and denied. That's a family for you." |
| Hessa | "If you embarrass me in there, I never saw you." / "Was it. I was on the harbour gate. Ask anyone." |
| Pip | "I'm not supposed to repeat that." / "The case key's on the hook behind the harbour bell. I didn't tell you." |
| Odrun | "Nobody eats here. Those forty bowls are for nobody." / "I don't want to know. I never do." / "If anyone asks, you came for soup, and there wasn't any." |

### The duel: fewer each time

Nobody announces that his guards are leaving him. Three things carry it.

| When | Drask (bark `shout`) | Who comes | The hero (bark `shout`) |
|---|---|---|---|
| Before the fight (`DraskOnTheField`) | "Arrek, your girl walked this week, I hear. Good. Sool, mind that knee." | He is naming the two men he will call. | |
| First shout | "Guard wing! To me!" | Arrek and Sool | "Two pikes in. Split them." / "Two came. Mind the flanks." / "Two." |
| Second shout | "Arrek! Sool! To me!" | Sool. Arrek's wife is the one who "can count" (`PipWashing`). | "One pike." / "One." / "One." |
| Third shout | "Sool!" | Nobody | "Just him." / "He's alone out there." / "Just him." |
| Between the parts (`DraskLeavesField`) | "Sool. You held the Karst gate beside me for two days." Then, not angry: "No. Stay where you are. You've the knee. I should have asked a younger man." | He excuses the man who has just left him. | |
| In flight (bark `hail.harbour`) | "Harbour guns, mark them." / "Battery two. Battery four. Mark them." / "Battery four." | The same, in guns | |

From the second shout on, smiths with pikes from the armoury are filing in behind the stands. Nobody says so until `HessaArmouryGate`, and she denies it.

### Numbers and names the script fixes

| What | Value | Said by |
|---|---|---|
| Days left, at the start | Twenty-seven | Abri (`AbriTwentySeven`) |
| Days left, at the end | "Twenty-three days were left of the forty. I said I would not plan on all of them." | Meret (`MeretAllStations`) |
| The lock | Installed eight years ago; four collars and a keeper bar; forty-four bolts; a Veyr convoy turned round at it with Idra flying escort | Taren, Idra |
| The smiths | Sixty; fifteen to a clamp | Tovra |
| The cage | Nineteen ribs, "not a cold shut in one of them"; nine hundred tonnes; five more on order (six in all, as on Vessa's slate in chapter 6) | Tovra, Vessa |
| The cadence | Pull nine, ease four | Idra |
| Drask's record | Thirty years, eleven campaigns, every company home | Drask, Pip |
| The Karst gate | Held for two days by Drask and Sool, among others; "every man home" | Drask, Odrun |
| The field | Four thousand can stand on it; Pip is in the third file, by the drain | Pip |
| Odrun's kitchen | Forty bowls | Odrun |
| The harbour | Nine cranes, the big one called Auntie; the carrier is the Long Patience; a skiff runs "under Auntie, round the flak mole, up the slip" | Pip |
| Nacre | Nine hundred households at the door; Oda's hauler on line nine hundred and four; a ferry on line eight sixty; one tug | Bryn, Oda, Meret |
| When | "At first watch a cage was closed on the Nacre road" | Meret |

## 2. Nodes

Lines are dialogue lines. "Compulsory" marks the main path; the longest, `DraskReceives`, is 15 lines and 71 seconds by the ninety-second test's arithmetic. Nothing else passes 55.

### Many Hands, before leaving

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `BrekkScoriaWord` | 6 | Brekk, Taren, Idra | Brekk | **Compulsory.** Crossing point on the dock, `chapter7.complete` and `Harrowgate` not started. He has come from Lanternwick himself. | | `startQuest Harrowgate` |
| `IdraPicketPlan` | 12 | Idra, Taren, Sela | Idra, then Taren | **Compulsory.** Idra at the chart table. The Picket in four numbered points; Taren interrupts the third. | `vask.lockAdmitted` | |
| `AbriTwentySeven` | 1 | Keeper | | Abri, `chapter7.complete` and not `vask.lockAdmitted` | | |
| `AbriThreeWords` | 1 | Keeper | | Abri, `vask.lockAdmitted` and not `vask.picketCrossed` | | |

### The Picket of Teeth (flight; radio)

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IdraPicketEdge` | 5 | Idra, Sela | Idra | **Compulsory.** Crossing point short of the first buoy. Teaches the cone, the shadows and the three stages. Sela hates their charts. | *vask.picketRead* | |
| `IdraLockReached` | 4 | Taren, Idra | Idra | **Compulsory.** Crossing point in sight of the lock. | *vask.lockSeen* | |
| `IdraLockOff` | 5 | Idra, Taren | Idra | **Compulsory.** As the keeper bar comes out. | `vask.lockRemoved` | |

### The Scoria Flows

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `BrekkCrust` | 4 | Brekk, Idra | Brekk | **Compulsory.** Crossing point at the landing. Teaches the crust. He is good at this ground. | *vask.crustRead* | |
| `BrekkCrossing` | 4 | Brekk, Idra | Brekk | **Compulsory.** Crossing point at the first garrison crossing. Teaches the paired pikeguards, and who they are. | *vask.garrisonSeen* | |
| `TovraFirst` | 12 | Tovra, Brekk, Taren, Idra | Tovra | **Compulsory.** Tovra at the Unsworn camp, first node. The brief's line; the weakness she put in; the three lines, which were in her drawings' margin all along. | `vask.unswornMet` | |
| `TovraRepeat` | 1 | Tovra | | Tovra's repeat until the Colossus is down | | |
| `TovraCages` | 4 | Tovra, Taren | Tovra | Tovra, once: `vask.unswornMet`, not `tovra.cagesHeard`. How well they work. Taren: "I know that one." | `tovra.cagesHeard` | |
| `TovraQuietOffer` | 4 | Tovra, Taren | | Tovra, `vask.unswornMet`, `QuietHands` not started | | `startQuest QuietHands` |
| `TovraQuietReturn` | 2 | Tovra | | Tovra, `QuietHands` at step 1 | `tovra.messagesLeft` | |
| `BrekkCampStall` | 1 | Brekk | | Brekk at the camp's stall, first node | | `shop` |
| `BrekkCampRepeat` | 1 | Brekk | | Brekk's repeat at the stall | | `shop` |
| `BrekkPikeHead` | 5 | Tovra, Brekk | Brekk | Interaction at the camp forge, `vask.unswornMet`, with the smiths standing round. Optional. | *brekk.pikeDressed* | |

### The Bridle Foundry

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IdraFirstSluice` | 3 | Taren, Idra | Taren | **Compulsory.** Crossing point at the first sluice wheel. Teaches flood and drain. | *vask.sluiceRead* | |
| `VessaGallery` | 4 | Vessa, Sela | Vessa | **Compulsory.** As the party enters the casting gallery. She is on the floor this time, not a loudhailer. | *vask.vessaMet* | |
| `VessaFallsBack` | 3 | Vessa, Idra | Vessa | **Compulsory.** At half Integrity (`Npc.Say`). She writes off her squad aloud and wakes the Colossus. | `vask.vessaBeaten` | |
| `SelaCagedRoad` | 4 | Sela, Taren, Idra | Sela | **Compulsory.** Crossing point at the heart's door, the cage in view, before the Colossus stands. Idra times the pull. | *vask.cageSeen* | |
| `TovraRelease` | 13 | Tovra, Brekk, Idra, Sela, Taren | Tovra | **Compulsory.** After `bossdown.SlagColossus`: the smiths file in. The release, the room it is given, and who saw it. | `vask.juvenileFreed` | |
| `BrekkAnvil` | 1 | Brekk | | **Compulsory.** Interaction beside Tovra, who has sat down on an anvil. She has no node of her own here. | *vask.anvilSeen* | |

### To Harrowgate

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `TovraHarrowgateRoad` | 4 | Tovra, Brekk, Idra | Tovra | **Compulsory.** Tovra at the camp, `vask.juvenileFreed`. Why the town; why the front gate. | `vask.roadSaid` | |
| `HessaSisters` | 5 | Vessa, Hessa | Hessa | **Compulsory.** Crossing point as the party comes up to the harbour gate. Vessa is on her way in to report. Overheard. | *vask.sistersHeard* | |
| `HessaParley` | 7 | Hessa, Idra, Taren | Hessa | **Compulsory.** Hessa at the gate. She vouches for the flag. | `vask.parley` | |
| `HessaGateRepeat` | 1 | Hessa | | Hessa's repeat | | |
| `HessaWarmaster` | 2 | Hessa | | Hessa, once: `vask.draskMet`, not `vask.duelWon`, not `hessa.warmasterHeard` | `hessa.warmasterHeard` | |

### Harrowgate, under the flag

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `PipFirst` | 4 | Pip, Idra | Pip | **Compulsory.** Crossing point just inside the gate, `vask.parley`. He attaches himself. | | `startQuest PipsOath` |
| `PipField` | 5 | Pip, Taren, Sela | Pip | Interaction at the muster field's wall. `PipsOath` step 0. | `pip.fieldTalked` | |
| `PipOathHall` | 4 | Pip, Taren, Idra | Pip | Interaction at the oath hall's wall of names. Optional. | *pip.hallSeen* | |
| `PipWashing` | 4 | Pip, Idra | Pip | Interaction under the washing lines in the family quarter. Optional. | *pip.washingSeen* | |
| `PipStairGame` | 3 | Pip, Sela | Pip | Interaction on the family stair, small children playing. Optional. | *pip.gameSeen* | |
| `PipArmourers` | 3 | Pip, Taren | Pip | Interaction on the armourers' street, half the forges cold. Optional. | *pip.forgesSeen* | |
| `PipHarbourWall` | 4 | Pip, Sela | Pip | Interaction on the wall above the war harbour. Optional. It is the pursuit's route, asked for as if idly. | *pip.harbourSeen* | |
| `SelaTrophyGallery` | 7 | Pip, Sela, Taren | Sela | **Compulsory.** Crossing point in the trophy gallery. Lanternwick's glass. | *vask.gallerySeen* | `startQuest RelayGlass` |
| `PipGallery` | 5 | Pip, Sela | Pip | Interaction at the case with the door in it, after `SelaTrophyGallery`. `PipsOath` step 1. | `pip.galleryTalked` | |
| `OdrunFirst` | 10 | Odrun, Taren, Sela, Idra | Odrun | **Compulsory.** Odrun in her kitchen, first node. | `vask.odrunMet` | |
| `OdrunRepeat` | 1 | Odrun | | Odrun's repeat | | |
| `OdrunSleepless` | 3 | Odrun | | Odrun, once: met, not `odrun.sleeplessHeard` | `odrun.sleeplessHeard` | |
| `OdrunKitchenOffer` | 4 | Odrun, Taren | | Odrun, met, `OdrunsKitchen` not started | | `startQuest OdrunsKitchen` |
| `OdrunKitchenReturn` | 2 | Odrun | | Odrun, `OdrunsKitchen` at step 1 | `odrun.kitchenStocked` | `shop` |
| `OdrunShop` | 1 | Odrun | | Odrun's repeat once `odrun.kitchenStocked` | | `shop` |
| `OdrunTovra` | 4 | Odrun, Tovra | Odrun | Interaction at the kitchen table, `vask.draskMet`: Tovra has come in the back way. Optional. | *vask.kitchenTable* | |
| `BrekkKitchen` | 2 | Brekk | | Brekk in the hidden quarter, behind the kitchen | | |

### The Warmaster

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `DraskReceives` | 15 | Drask, Idra, Sela, Taren | Drask | **Compulsory.** Drask in the oath hall, eating what his soldiers eat; `vask.odrunMet` and the gallery seen. | `vask.draskMet` | |
| `DraskOnTheField` | 3 | Drask, Idra | Drask | **Compulsory.** On the muster field at the oath bell, as the party steps out. The fight follows. | *vask.fieldTaken* | |
| `DraskLeavesField` | 4 | Drask, Idra | Drask | **Compulsory.** Part one beaten (`Npc.Say`). The checkpoint. The pursuit follows. | `vask.duelWon` | |
| `OdrunVerdict` | 7 | Drask, Odrun, Idra | Odrun | **Compulsory.** His skiff is down on his own field. The Unsworn and half the garrison are standing on it. | `vask.deposed` | |
| `HessaArmouryGate` | 4 | Idra, Hessa, Taren | Hessa | **Compulsory.** Crossing point at the armoury gate, on the way down to the harbour. | *vask.gateDenied* | |
| `PipAfterDuel` | 6 | Pip, Taren, Idra | Pip | Pip, `vask.deposed`, `PipsOath` at step 2 | `pip.oathLeft` | |
| `PipOathDay` | 1 | Pip | | Pip's post (`vask.deposed`) when the two earlier talks never happened | | |
| `PipPost` | 1 | Pip | | Pip's post once `pip.oathLeft` | | |
| `IdraLockCounted` | 4 | Taren, Idra | Idra | **Compulsory.** Crossing point on the stair down to the harbour cells. She is scratching the brace. | *idra.lockCounted* | |
| `DraskCustody` | 5 | Drask, Taren, Sela | Drask | **Compulsory.** Drask in the harbour cell. Ends on a question nobody answers. | *vask.orderSaid* | |
| `BrynNacreTraffic` | 7 | Idra, Bryn, Oda | Bryn | **Compulsory.** At once: every radio in the harbour. | `vask.nacreHeard` | |
| `MeretAllStations` | 5 | Meret, Taren | Meret | **Compulsory, chapter end.** At once. The last line is Taren's. | `chapter8.complete` | |

### Many Hands, afterwards

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `TovraAboard` | 2 | Tovra | | Tovra in the freight hall, `chapter8.complete` | | |
| `BrekkAboard` | 1 | Brekk | | Brekk at Tovra's forge, `chapter8.complete` | | |
| `AbriSmiths` | 1 | Keeper | | Abri, `chapter8.complete` | | |
| `DraskAboard` | 2 | Drask | | Drask, in custody aboard, `chapter8.complete` | | |
| `PellAumiAnswers` | 1 | Pell | | Pell, `chapter8.complete` and `quest.RelayGlass.complete` | | |

### Suggested routing (first match wins)

| Speaker | Beats, in order | Npc tiers |
|---|---|---|
| Tovra (camp) | `TovraHarrowgateRoad` (`vask.juvenileFreed`), `TovraQuietReturn`, `TovraCages`, `TovraQuietOffer` | first `TovraFirst`; repeat `TovraRepeat`. Aboard: `TovraAboard`. |
| Brekk | none | at the stall: first `BrekkCampStall`, repeat `BrekkCampRepeat`; in the quarter: `BrekkKitchen`; aboard: `BrekkAboard` |
| Hessa | `HessaWarmaster` | first `HessaParley`; repeat `HessaGateRepeat` |
| Pip | `PipAfterDuel` | he follows and has no first node of his own: his talks are the points he stops at. Post `PipOathDay`, or `PipPost` once `pip.oathLeft`. |
| Odrun | `OdrunKitchenReturn`, `OdrunSleepless`, `OdrunKitchenOffer` | first `OdrunFirst`; repeat `OdrunRepeat`, or `OdrunShop` once `odrun.kitchenStocked` |
| Drask | none | first `DraskReceives`; in the cell `DraskCustody`; aboard `DraskAboard` |
| Keeper | `AbriSmiths`, `AbriThreeWords`, `AbriTwentySeven`, then chapter 7's | |
| Pell | `PellAumiAnswers`, then her earlier lines | |

## 3. Flags

**From the brief, and who sets them:** `vask.lockAdmitted` (`IdraPicketPlan`, aboard, before the Picket), `vask.picketCrossed` (world), `vask.lockRemoved` (`IdraLockOff`), `vask.unswornMet` (`TovraFirst`), `vask.sluice1..3` (world), `vask.vessaBeaten` (`VessaFallsBack`), `vask.colossusDestroyed` (world), `vask.juvenileFreed` (`TovraRelease`), `vask.parley` (`HessaParley`), `vask.draskMet` (`DraskReceives`), `vask.duelWon` (`DraskLeavesField`), `vask.pursuitWon` (world), `vask.deposed` (`OdrunVerdict`), `vask.nacreHeard` (`BrynNacreTraffic`), `chapter8.complete` (`MeretAllStations`).

**Added by the script:** `vask.roadSaid`, `vask.odrunMet`, `tovra.cagesHeard`, `tovra.messagesLeft`, `hessa.warmasterHeard`, `odrun.sleeplessHeard`, `odrun.kitchenStocked`, `pip.fieldTalked`, `pip.galleryTalked`, `pip.oathLeft`.

**Suggested for the world to own:** the italic flags in section 2.

## 4. Quests

Kinds are `ObjectiveKind` (TalkTo 0, Reach 1, Kill 2, Collect 3, Interact 4, Flag 5). Rewards are the brief's. `Harrowgate` is the main quest (`main: true`, `chapter: 8`).

### Harrowgate - Harrowgate (main)

Description: `The Muster's foundry on Scoria has a young road in a cage, and its Warmaster has orders in for five more.`

| # | Kind | Target | Step text | Points at |
|---|---|---|---|---|
| 0 | Flag | `vask.lockAdmitted` | Hear Idra's plan for the Picket at the chart table | ManyHands, speaker `Idra` |
| 1 | Reach | `PicketOfTeeth` | Take the lane to the Muster's border | PicketOfTeeth, from ManyHandsHull or the chart |
| 2 | Flag | `vask.picketCrossed` | Cross the Picket, from shadow to shadow or straight through | PicketOfTeeth, the clean lane |
| 3 | Flag | `vask.lockRemoved` | Take the route lock apart to Idra's count | PicketOfTeeth, the lock |
| 4 | Flag | `vask.unswornMet` | Follow Brekk across the Flows to the Unsworn camp | ScoriaFlows, speaker `Tovra` |
| 5 | Reach | `BridleFoundry` | Go into the Bridle Foundry | BridleFoundry, from ScoriaFlows |
| 6 | Flag | `vask.vessaBeaten` | Take the casting gallery from Vessa Korl | BridleFoundry, encounter `VessaKorl` |
| 7 | Kill | `SlagColossus` | Cool the Slag Colossus at the sluices, and break it | BridleFoundry, encounter `SlagColossus` |
| 8 | Flag | `vask.juvenileFreed` | Stand back while the smiths open the cage | BridleFoundry, the heart |
| 9 | Flag | `vask.roadSaid` | Go back to Tovra at the camp | ScoriaFlows, speaker `Tovra` |
| 10 | Flag | `vask.parley` | Walk in at Harrowgate's harbour gate under a parley flag | Harrowgate, speaker `Hessa` |
| 11 | Flag | `vask.draskMet` | See the town, and then the Warmaster | Harrowgate, speaker `Drask` |
| 12 | Flag | `vask.duelWon` | Answer the Warmaster on the muster field | Harrowgate, encounter `DraskVaal` |
| 13 | Flag | `vask.pursuitWon` | Stop his skiff before it reaches the carrier | Harrowgate harbour, encounter `DraskSkiff` |
| 14 | Flag | `vask.deposed` | Stand on the field while Harrowgate answers | Harrowgate, the muster field |
| 15 | Flag | `vask.nacreHeard` | See the Warmaster in the harbour cells | Harrowgate, speaker `Drask` |

The brief has the Picket crossed before the lock is reached, and steps 2 and 3 follow it; if the lock is met first, swap them. Reward: XP, scrip, a T4 part choice.

### QuietHands - Quiet Hands (Tovra)

Description: `Half of Tovra's smiths have family holding pikes at the crossings. Three of them have written, and a pikeguard seen taking paper is flogged.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `KettleStone` | Leave a message under the kettle stone at each of three garrison cookfires | Leave the three messages |
| 1 | Flag | `tovra.messagesLeft` | Tell Tovra they're left | Tell Tovra (waits on `Tovra`) |

Reward: a frame part ("off the cage's own run"), and more smiths aboard. The message is "We're going. Come if you can. Don't hold the gate too hard." It is why fewer answer the shouts, for anyone who did the quest; the duel does not depend on it.

### RelayGlass - What Was Taken (Sela)

Description: `Lanternwick's relay glass is in Harrowgate's trophy galleries, under a card that says the canyon yielded it.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `GlassCase` | Take the relay glass out of three cases: the gallery, the east gallery, the oath hall | Take back the relay glass |

One step, and nobody to report to: Sela gave it to herself. Reward: a module, and Aumi's answer by lantern (`PellAumiAnswers`).

### OdrunsKitchen - Odrun's Kitchen (Odrun)

Description: `A kitchen that doesn't exist can't draw rations. Mother Odrun wants three things she can't buy with her own face.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `KitchenWant` | Find garrison salt, ash-root flour and a regulation mess kettle | Find Odrun's three things |
| 1 | Flag | `odrun.kitchenStocked` | Take them to Mother Odrun | Take them to Odrun (waits on `Odrun`) |

Reward: provisions, and the Unsworn quarter's back table as a shop. The three points are the quartermaster's stair, the harbour scales and the armourers' street; each has its own bark (section 7), so they can be found in any order.

### PipsOath - The Oath (Pip)

Description: `Cadet Pip takes the Muster's oath tomorrow, in the third file, by the drain. He is showing you the town because nobody told him not to.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Flag | `pip.fieldTalked` | Let Pip show you the muster field | See the field with Pip |
| 1 | Flag | `pip.galleryTalked` | Find Pip in the trophy gallery, by the door | Find Pip in the gallery |
| 2 | Flag | `pip.oathLeft` | Find Pip after the duel | Find Pip after the duel (waits on `Pip`) |

Reward: a Response token. Pip does not take the oath. If the first two talks never happened he still means to (`PipOathDay`), and the quest stays open.

## 5. Display names

### New speakers (`CharacterDef.displayName`)

| Speaker id | Display name |
|---|---|
| `Drask` | Drask Vaal |
| `Tovra` | Tovra Kesh |
| `Odrun` | Mother Odrun |
| `Pip` | Pip |
| `Bryn` | Bryn Asta |

`Brekk` is now "Brekk Tamar" in his own first line; his display name can stay "Brekk". Hessa, Vessa, Brekk, Oda, Meret, Keeper and Pell speak on sheets they have or are owed. Nobody uses an expression new to them: Idra uses five of her six (no Flustered), Vessa two of her three (no Smug), Meret two of her five.

### Sheets, for the artist

All four new Vaskar are as chapter 2 set the people: tall, heavy-boned, skin in large overlapping plates, two membranous crest-sails.

**Drask Vaal.** Vaskar man in his fifties, the largest person in any room and the stillest. Plates the brown of old bronze, worn smooth at the brow by thirty years of helmets. Crest-sails full, nicked along the trailing edge, carried half open and at ease. A plain soldier's sash with a Warmaster's tabs on it, and nothing else. Deep-set eyes that rest on whoever is speaking as if nobody else were on the field. His resting face is a host's. 23 lines: Neutral 7, Determined 5, Happy 3, Tender 3, Amused 3, Sad 1, Curious 1. **He has no angry cell and should not look as if he could.** His Determined is certainty, not heat. His Sad is one line, to a soldier who has stepped back from him. (The boss body in a war-frame is separate.)

**Tovra Kesh.** Vaskar woman in her forties, a forewoman's build: heavy shoulders, forearms pitted with old spatter burns, plates dulled by soot that no longer washes out. Crest-sails folded flat and tied back with a leather thong, so they don't catch sparks. A leather apron, tongs through the belt, slag in the seams of everything. Eyes ringed dark. She has not slept properly in a long time and it shows before anything else does. 30 lines: Determined 11, Neutral 9, Sad 3, Amused 2, Smug 1, Shocked 1, Curious 1, Intense 1, Flustered 1. Her Smug is brief and bitter ("I'm too good."). Her Flustered is the only time anyone tells her what to do.

**Mother Odrun.** Very old Vaskar woman, short for her people and broad. Plates gone pale and chalky with age; one crest-sail torn long ago and never mended, the other folded round her shoulders like a shawl. Sleeves rolled, flour to the elbow, a ladle she points with. She looks at everyone as if they were late. 23 lines: Neutral 18, Amused 4, Determined 1. Her Amused is a flat mouth and one raised brow plate. Her Determined is used once, for the verdict, and should be the same face as her Neutral with the ladle put down.

**Pip.** Vaskar boy of twelve or thirteen, all elbows. Plates still pale and soft-edged; crest-sails too big for his head and never quite folded; a cadet's grey sash pressed very flat, one pip on the rank tab, a scab on one knee. He stands to attention when he remembers. 30 lines: Happy 11, Worried 5, Curious 4, Smug 3, Flustered 3, Neutral 2, Playful 1, Sad 1. His Smug is a child reciting something he was told. His Sad is one syllable.

**Bryn Asta.** Vael woman in her forties, of the Halo's own people: no new species. Crest fronds cut square and short under a tug crew's headset; a controller's level gaze; laugh lines she isn't using. The collar of a tug captain's work jacket, with the Welcome Back's hull stripe on it. She is heard by radio only in this chapter. 4 lines: Neutral 2, Determined 1, Tender 1. Chapter 9 will want her whole sheet.

### People named but never speaking

| Name | Who |
|---|---|
| Sergeant Arrek | Guard wing. His girl walked this week; his wife counts who got meat. Answers the first shout only. |
| Sool | Guard wing. A bad knee. Held the Karst gate with Drask for two days. Answers twice. |
| Pip's mother | Does the whole stair's sashes, on a rota. The source of the gossip. |
| Pip's sergeant | "The Muster is bigger than one Warmaster." Didn't go when the guard was called. |
| Brekk's aunt | Hid his pike in the flour the night he walked. Tells forty people about a gate hinge. |
| The one by the stove | A Karst veteran with a medal, who eats in Odrun's kitchen because it has no window on the street. |
| Aumi | Answers by lantern: three long, and one short she hopes nobody can read. |

None needs a sheet. Arrek and Sool should be recognisable pikeguards on the field: Sool favours a knee.

### Other words the script introduces

| Word | What |
|---|---|
| Tongs | Tovra's oath |
| the fitter | Tovra's name for Taren |
| a cold shut | A casting flaw. Tovra's cage hasn't one. |
| the hand seats; the skin | The two hand-holds on every clamp; the finger of metal she poured over them |
| a kettle stone | Where a garrison family leaves and finds messages |
| tabs; a pip | Rank marks. A pip is the smallest. "Take his tabs" is how Harrowgate deposes someone. |
| oath day; the oath bell; the third file | When a cadet swears; what rings for it; where Pip stands |
| the great work | What the speeches call the Bridle Foundry |
| Lantern Canyon | The Muster's name for Lanternwick's gorge, on the trophy card: "Lantern Canyon, yielded" |
| the Karst gate | One of Drask's eleven campaigns. Nothing else is said of it. |
| Auntie; the Long Patience; the flak mole; the slip | The big harbour crane; the carrier; the harbour's gun pier; the carrier's ramp |
| traffic | What Bryn Asta calls every hull and everybody |
| the Welcome Back | Her tug |
| nine-oh-four; line eight sixty | Hulls called by their place in the transfer queue |
| first watch | When the cage closed on the Nacre road |

### Enemies and bosses (`EnemyDef.displayName`)

| Id | Display | Note |
|---|---|---|
| `SensorBuoy` | Sensor Buoy | Spoken of as "buoys" |
| `MusterLancer` | Muster Lancer | "hunters", "a hunter wing" |
| `FlakGate` | Flak Gate | "the curtain" |
| `MusterPikeguard` | Muster Pikeguard | "pikes", "a pair" |
| `MusterMortar` | Muster Mortar | |
| `AshCrawler` | Ash Crawler | |
| `PourDrone` | Pour Drone | |
| `ForgeHound` | Forge Hound | |
| `VessaKorl` | VESSA KORL | Epithet: `Dig-master of the Muster, counting the cost`. An elite: if she has no boss kit, her two barks are simply unused. |
| `SlagColossus` | THE SLAG COLOSSUS | Epithet: `A crucible on legs, too hot to hurt`. Status: `TOO HOT TO HURT  ·  OPEN A SLUICE`. Break targets: `Pour-arm`. |
| `DraskVaal` | WARMASTER DRASK VAAL | Epithet: `Thirty years, eleven campaigns, every company home`. Status in part one: `THE MUSTER IS WATCHING`. |
| `DraskSkiff` | THE WARMASTER'S SKIFF | Epithet: `Running for the Long Patience`. Part two, in flight. |

The boss ids must be exactly these for their barks to be found.

### Zones (`ZoneDef.displayName`)

| Id | Display |
|---|---|
| `PicketOfTeeth` | The Picket of Teeth |
| `ScoriaFlows` | The Scoria Flows |
| `BridleFoundry` | The Bridle Foundry |
| `Harrowgate` | Harrowgate |

### Floor signs and landmark labels

- The Unsworn camp: `UNSWORN CAMP`, `CAMP FORGE`, `STALL`
- The Foundry: `CHARGING FLOOR`, `CASTING GALLERY`, `THE HEART`
- Harrowgate: `HARBOUR GATE`, `MUSTER FIELD`, `OATH HALL`, `ARMOURERS' STREET`, `FAMILY QUARTER`, `TROPHY GALLERY`, `EAST GALLERY`, `HARBOUR WALL`, `ARMOURY GATE`, `HARBOUR CELLS`

Mother Odrun's kitchen has no sign. It doesn't exist.

### Prompts

`Take the collar off`, `Leave it under the kettle stone`, `Turn the sluice wheel`, `Look at the pike-head`, `Look at the wall of names`, `Look down at the harbour`, `Open the case`, `Take the salt`, `Take the flour`, `Take the kettle`, `Stand by the anvil`.

### Items

The script names two, both existing: Plating Wax and Repair Gel, on Brekk's stall. The messages, the glass and Odrun's three things are carried in the story and are not inventory items.

## 6. Objective strings

Where the panel takes its line from the main quest's current step, section 4's step text is what shows. These are the rows the steps can't say.

| Zone | When | Label |
|---|---|---|
| PicketOfTeeth | buoys amber | Get into a rock's shadow: the buoys have half heard you |
| PicketOfTeeth | a hunter wing out | Break the hunter wing, or lose it in shadow |
| PicketOfTeeth | the carrier turning | Run the lane: the carrier is awake |
| PicketOfTeeth | at the lock | Take the lock apart in Idra's order (1/4) |
| ScoriaFlows | on a glowing seam | Get onto dark crust: the seam is about to go |
| BridleFoundry | the Colossus glowing | Open a sluice: it is too hot to hurt |
| BridleFoundry | a plate dark | Hit the dark plate while it lasts |
| BridleFoundry | plates cooled, arms left | Break the pour-arms (1/2) |
| Harrowgate | `vask.parley`, Drask not met | See the trophy gallery and Mother Odrun's kitchen |
| Harrowgate | `vask.draskMet`, before the duel | Go to the muster field when the oath bell rings |
| Harrowgate | the pursuit | Stop the skiff before it reaches the Long Patience |
| any | `chapter8.complete` | Chapter 8 complete. The Warmaster's last order outlived his command. Nacre is calling. |

Chapter-end card (`QuestToast`): heading `CHAPTER 8 COMPLETE`, title `HARROWGATE`, body `The route lock is off the Picket, and the Muster's cage is open: sixty smiths, eleven of the garrison and one dig-master saw the young road go. Harrowgate took its Warmaster's tabs itself. His last order closed a cage on the Nacre road, the transfer berth is gone, and Stillwater proceeds now. Until then nothing closes behind you, so keep exploring.`

## 7. Barks added

Existing entries are untouched. Every new hero key is there for Taren, Sela and Idra alike.

| Speaker | Keys | Used by | Wiring |
|---|---|---|---|
| Taren, Sela, Idra | `bossphase.VessaKorl`, `bossopening.VessaKorl`, `bossphase.SlagColossus`, `bossopening.SlagColossus`, `bossphase.DraskVaal`, `bossopening.DraskVaal`, `bossphase.DraskSkiff`, `bossopening.DraskSkiff` | `BossController.Situation` | Automatic for those ids |
| Taren, Sela, Idra | `picket.amber`, `picket.hunters`, `picket.carrier` | The three alarm stages | One line each, as the stage is reached |
| Taren, Sela, Idra | `shadow` | The active ship enters a sensor shadow | Two in turn |
| Taren, Sela, Idra | `flak`, `lancer` | A Flak Gate's curtain; a Lancer lining up | |
| Taren, Sela, Idra | `lock` | A piece of the lock comes off | Four in turn, in a fixed order: port collar, starboard, keel, keeper bar. Taren names each and says "Off."; Idra's are the count, "One." to "Four." |
| Idra | `hail.lock` | Before each piece | `BarkService.Play("Idra", "hail.lock", true)`; four in turn. Her order, then the `lock` line as its answer. |
| Taren, Sela, Idra | `crust`, `flow`, `squall` | A seam starts to glow or the party reaches dark crust; bright flow throws a hero back; an ash squall closes | `crust` is two in turn |
| Taren, Sela, Idra | `pikeguard`, `mortar`, `pour` | First sight of each | |
| Brekk | `hail.crust` | Leading across the Flows, before the camp | `RadioHail` or a guide's bark; three in turn |
| Taren, Sela, Idra | `message` | A message left under a kettle stone (`QuietHands`) | `DiscoveryPoint.situation`; three in turn |
| Taren, Sela, Idra | `sluice` | A sluice wheel turned | Three in turn |
| Tovra | `hail.sluice` | The works' speaking tube, at each sluice round the heart | `RadioHail`, speaker `Tovra`; three in turn |
| Vessa | `hail.gallery` | The casting gallery fight | Two in turn |
| Taren, Sela, Idra | `arm` | A pour-arm breaks | Two in turn |
| Taren, Sela, Idra | `glass` | A trophy case opened (`RelayGlass`) | `DiscoveryPoint.situation`; three in turn |
| Taren, Sela, Idra | `kitchen.salt`, `kitchen.flour`, `kitchen.kettle` | Each of Odrun's three things (`OdrunsKitchen`) | `DiscoveryPoint.situation`, one key to a point |
| Drask | `shout` | Each command shout in part one | Three in turn (section 1) |
| Taren, Sela, Idra | `shout` | Straight after each of his | Three in turn. A count, nothing more. |
| Drask | `duel` | When a hero lands a clean hit in part one | Three in turn. He is courteous while he fights. |
| Drask | `hail.harbour` | Part two: his calls to the harbour's guns | Three in turn, as fewer batteries fire |
| Taren, Sela, Idra | `harbour` | Part two begins | |

## 8. What the script assumes

**No new commands or dialogue features.**

1. **Five portrait sets.** Drask, Tovra, Odrun, Pip, Bryn. Section 5 describes each and counts the expressions used. Idra's, Meret's and Vessa's sets are owed from earlier chapters and need nothing added.
2. **Expectations** (`[ContentRegistry.Expectations]`, as the chapter guide's section 11.2 has it). Every node begins with a `CharacterDef` id once the five new defs exist; `Abri…` and `Sela…` are as before. No node has choices. Four nodes end in the shop and want `book.Branch(node,"shop")`: `BrekkCampStall`, `BrekkCampRepeat`, `OdrunKitchenReturn`, `OdrunShop`. No node needs `book.Optional`: the longest is 71 seconds. `PeopleMarks.Repeats` gets `AbriTwentySeven`, `AbriThreeWords`, `TovraRepeat`, `BrekkCampRepeat`, `HessaGateRepeat`, `OdrunRepeat`, `OdrunShop`, `BrekkKitchen`, `PipOathDay`, `PipPost`, `TovraAboard`, `BrekkAboard`, `AbriSmiths`, `DraskAboard`, `PellAumiAnswers`. The `Resources` copy of `barks.json` must be brought level with this one, or the test that compares them fails.
3. **The lock comes off in a fixed order**, four pieces, under fire. Before each, Idra's `hail.lock`; after each, the active hero's `lock`. Taren's ship is the one on the lock whoever the player is flying.
4. **Brekk leads across the Flows** from the landing to the camp, a few paces ahead, and stops at the first crossing. After that he is on the stall.
5. **The release is a scene.** Sixty smiths come in after the Colossus falls, fifteen to a clamp, Brekk on the fourth. The player holds nothing. The cage should be seen to pull for nine and ease for four from `SelaCagedRoad` on, so that Idra's call lands on a real ease.
6. **Vessa is on the gantry during the release** and gone when it ends. She reaches Harrowgate's gate ahead of the party (`HessaSisters`) and is not seen after that.
7. **Tovra on the anvil.** Between `TovraRelease` and the camp she sits and has no node. `BrekkAnvil` is the only thing that can be said near her, and nobody should bark there.
8. **Under the flag.** Nobody in Harrowgate fights the party until the field. The trophy cases open once Pip has said where the key is (`pip.galleryTalked`) or after `vask.deposed`.
9. **Pip follows** from the gate, and stops at each of his points. During the duel he is in the third file, by the drain.
10. **The duel** as section 1's table. Arrek and Sool are the two pikeguards of the guard wing. Smiths with pikes come in behind the stands from the second shout on.
11. **The verdict is the town's.** In `OdrunVerdict` the party has one line, Idra's, and it only offers a place to put him.
12. **The end runs without a break**: `HessaArmouryGate`, `IdraLockCounted`, `DraskCustody`, then `BrynNacreTraffic` and `MeretAllStations` at once, as one stretch of radio with three portraits (Bryn, Oda, Meret). Nothing barks after Taren's last line. Then the card.
13. **Two shops.** Brekk's stall under `ScoriaFlows`; Odrun's back table under `Harrowgate`, shut until `odrun.kitchenStocked`.
14. **Drask comes aboard Many Hands in custody** (`DraskAboard`), as the story bible has it.
15. **If the side quests are left undone**: Pip still means to swear; the kitchen sells nothing; the glass stays under its card, and Sela's line in the gallery stands as it is.

## 9. Where the script departs from the brief

- **The lock is admitted aboard, before the Picket**, as the beats have it, so `vask.lockAdmitted` is set ahead of `vask.picketCrossed`.
- **Idra's whole answer is "How many bolts?"** She neither forgives him nor refuses to. The orders start in the same breath.
- **Sela says three words at the table and one sentence in the trophy gallery.** Idra notices on the main path; Abri notices for anyone who talks to her.
- **The tally is four lines** on a stair, just before the end.
- **Tovra's weakness is the hand seats.** The Warmaster ordered them poured solid, because a cage with a handle can be opened by somebody else. She poured them a finger deep. The three lines from Oriel were in the margin of her drawings the whole time, in a hand none of the smiths could read.
- **The player does not perform the release.** The smiths do. Taren has one line in it, Sela gives it room, and it takes a moment to go.
- **The witnesses include Vessa.** She watches from the gantry, leaves, and enters it at the harbour gate as nine hundred tonnes lost.
- **The line at the anvil is Brekk's**: "She poured every rib of it. Let her sit."
- **The sisters' exchange is overheard at the gate**, before Hessa turns to the party. It ends "Sorry has no weight." Vessa is not redeemed and is not seen again; where she is after the verdict is left open.
- **Hessa vouches out of spite**, and says so. She denies the armoury gate with her sister's word for her: "Then I was careless. It's on my record."
- **Pip's name is a rank mark**, the smallest one. He is still proud of the drill at the end, and Idra tells him to keep it.
- **Odrun's kitchen officially feeds nobody.** The town's habit of not having seen things (section 1) is the script's, not the brief's.
- **Drask's stakes are the party, not the convoy**: "If you fall, you fly my colours." He offers all three of them at once against a war-frame and calls it even.
- **He runs on the letter of his own promise.** "I said that if I fell, the Muster would hear your question. It has heard it. I never said I'd stop being its Warmaster."
- **He excuses the man who leaves him.** Sool has a bad knee, and Drask says so for him.
- **Idra offers Many Hands as the place to hold him**, and Odrun accepts. It is the only thing the party contributes to the verdict.
- **Bryn Asta is Vael**, and this is the first time she is heard. Chapter 9's brief has her on the radio since chapter 3; no earlier script has her.
- **Oda names Taren on the open band.** "Tell him the door jammed. He'll know."
- **Meret speaks to Many Hands inside an all-stations notice**, to give the count: twenty-three days were left.
- **The last line is Taren's: "Nine hundred and four."**
- **New names**: Brekk's aunt, Arrek, Sool, the Karst gate, Auntie, the Long Patience. Where Harrowgate is, on Scoria or off it, is not said.
- **`RelayGlass` is one step** with nobody to report to, and its reward arrives aboard.
- **Ten talks the brief does not list**: `BrekkCrossing`, `BrekkPikeHead`, `TovraCages`, `HessaWarmaster`, `PipOathHall`, `PipWashing`, `PipStairGame`, `PipArmourers`, `PipHarbourWall`, `OdrunSleepless`, plus `OdrunTovra` and `BrekkKitchen` in the hidden quarter. They are Harrowgate's texture and Brekk's standing with his own people.
