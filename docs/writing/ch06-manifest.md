# Chapter 6 script manifest - The Long Sleep (Hoarfell)

For the engineers wiring chapter 6. Script: [Hoarfell.yarn](Hoarfell.yarn). Barks: [barks.json](../barks.json). Brief: [ch06-the-long-sleep.md](../../chapters/ch06-the-long-sleep.md). Earlier manifests, in the same shape: [chapter 3](../ch03-manifest.md), [chapter 4](ch04-manifest.md), [chapter 5](ch05-manifest.md).

**What is here:** 52 nodes, 213 dialogue lines, 2 option labels (215 strings). Compiled clean with the project's own compiler (Yarn Spinner 3.2.4, the nine live files and the four pending ones together: 0 errors, 362 nodes, no duplicate titles). No new Yarn commands: `setFlag`, `startQuest`, `joinParty`, `shop`.

**This file waits in `pending/`** until the chapter's people and quests are defined: six new portrait sets (Idra's two, Tern, Hask, Iru, Vessa), four `QuestDef`s, and the test lists in section 8.

## 1. Conventions this file follows

- Every title starts with its owning speaker id: `Idra…`, `Tern…`, `Hask…`, `Iru…`, `Vessa…`, the returning `Abri…` (speaker `Keeper`), `Ilo…`, `Binna…`, `Maru…`, `Ruva…`, `Teo…`, and `Sela…` for the party with nobody else to walk up to.
- Each quest is started by exactly one node. Talks that complete a step set a flag for a `Flag` step; talks at a point offered only at a quest's step set nothing, so the point's `report` reaches its `Interact` step.
- **Three hands from `IdraJoins` on.** Taren, Sela and Idra are all in every story scene after it, whoever is deployed. One of them carries each scene: the table in section 2 says who. Nobody is given a line just to be present.
- **Idra says "Irun" and "Vosk"**, never first names, all chapter. Sela calls Taren "Vosk" as always and has no name for Idra yet.
- Checked against the word tests: no retired phrase, no id-shaped word.

### Idra's tally

She keeps two counts on the brace: the left side is people she has pulled out, the right is what she owes. Between her and the party it runs as below, and every number is said aloud by her.

| When | Count | Where |
|---|---|---|
| The tow in the Shear | The party owes her one | `IdraShearHail` |
| The crew pod, if recovered | She owes one ("the right side of the brace") | `IdraCutterFound` |
| Three couplings held for her | She is two down "before I count anything else" | `IdraTally` |
| Binna feeds nineteen of her crew | "I can't even find the column for that" | `IdraLiftFigures` |
| She fixes the lift figures | "Don't thank me. It restarts the count." | `IdraLiftFigures` |

The wording holds whether or not the pod was recovered.

## 2. Nodes

Lines are dialogue lines (options not counted). "Compulsory" marks the main path; the longest, `TernLongSleep`, is 16 lines and 71 seconds by the ninety-second test's arithmetic.

### Many Hands, before leaving

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `SelaHoarfellChart` | 8 | Sela, Taren, Ilo | Sela | **Compulsory.** At the chart table, `chapter5.complete` and `LongSleep` not started. Sela names the herders; Ilo writes his first entry; Sela still half holds with Meret's arithmetic. | `hoar.charted` | `startQuest LongSleep` |
| `AbriThirtySix` | 1 | Keeper | | Abri, `hoar.charted` and not `hoar.idraHailed` | | |
| `IloLastlightEntry` | 1 | Ilo | | Ilo, `hoar.charted` and not `chapter6.complete` | | |

### The Aurora Shear (flight; radio)

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IdraShearHail` | 9 | Idra, Sela, Taren | Idra | **Compulsory.** Crossing point where the first current turns against the party: her striped cutter puts a tow line on them. Opens on the brief's line. Teaches the currents. | `hoar.idraHailed` | |
| `IdraShearLanes` | 5 | Idra, Sela, Taren | Idra | **Compulsory.** Crossing point at the first Muster tender, on a short lane Sela would take. Teaches the tender's burning wake. | *hoar.tenderSeen* | |
| `IdraCutterFound` | 5 | Sela, Idra, Taren | Idra | Interaction at the downed cutter under the ice shelf, `CutterDown` at step 0 (`report CrewPod`). Idra speaks by radio, or in person if she has joined. Names Kott and Sibb. | *hoar.podFound* | |

### Lastlight

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `SelaLastlightArrival` | 4 | Sela, Taren, Idra | Sela | **Compulsory.** Crossing point at the landing, the ribs overhead, herders carrying ladders. Idra has led them in. | *hoar.ribsSeen* | |
| `TernFirst` | 12 | Tern, Taren, Sela | Tern | **Compulsory.** Tern's first node, in her hall under the fourth rib. She is seated and stays seated. What the host is; why she won't answer yet. | `hoar.ternHeard` | |
| `TernRepeat` | 1 | Tern | | Tern's repeat until the Marshal sleeps | | |
| `IdraPens` | 9 | Idra, Hask, Taren | Idra | **Compulsory.** Crossing point at the beast pens, `hoar.ternHeard`: Idra and Hask mid-argument. Taren offers to fetch her crew. | *hoar.pensSeen* | `startQuest CutterDown` |
| `IdraPlan` | 11 | Idra, Taren, Sela | Idra | **Compulsory.** Idra at the carving yard, over the herders' bone map. The plan in five numbered lines; Taren's one question; "Good. You'll do." | `hoar.planTaken` | |
| `IdraRepeat` | 1 | Idra | | Idra at the carving yard after the plan, until the party leaves for the Reach | | |
| `HaskFirst` | 3 | Hask | | Hask at the pens, first node. Options: trade, or leave. | | `shop` (option 1) |
| `HaskRepeat` | 1 | Hask | | Hask's repeat | | `shop` |
| `HaskStraysOffer` | 4 | Hask, Taren | | Hask, met, `hoar.planTaken`, `StrayBeasts` not started | | `startQuest StrayBeasts` |
| `HaskStraysWaiting` | 1 | Hask | | Hask, `StrayBeasts` at step 0 (his count is three short, so `HaskRepeat` would be wrong) | | |
| `HaskStraysReturn` | 2 | Hask | | Hask, `StrayBeasts` at step 1 | `hask.straysCounted` | `shop` |
| `IruFirst` | 4 | Iru, Sela | | Iru at the lamp shrine, first node | | |
| `IruRepeat` | 1 | Iru | | Iru's repeat | | |
| `IruLampsOffer` | 4 | Iru, Taren | | Iru, met, `hoar.ternHeard`, `ShrineLamps` not started | | `startQuest ShrineLamps` |
| `IruLampsReturn` | 2 | Iru | | Iru, `ShrineLamps` at step 1 | `iru.lampsLit` | |
| `SelaRibTally` | 2 | Sela, Taren | Sela | Interaction at the fourth rib, by Tern's hall: eleven rows of cuts. Optional; open from arrival. Tern explains it later. | *hoar.tallySeen* | |

### The White Reach and the dig

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `SelaReachGate` | 4 | Sela, Taren | Sela | **Compulsory.** Crossing point just outside the Reach gate. Taren's relief, shown. | *hoar.reachEntered* | |
| `IdraHerd` | 5 | Idra, Sela, Taren | Idra | **Compulsory.** Crossing point as the herd comes through the gap. Taren hums; Idra doesn't. | *hoar.herdMet* | |
| `IdraWhiteout` | 4 | Idra, Sela | Idra | **Compulsory.** Crossing point as the first whiteout closes. Idra briefs; Sela would have improvised. | *hoar.whiteoutMet* | |
| `VessaLoudhailer` | 6 | Vessa, Sela, Idra | Vessa | **Compulsory.** Crossing point on the dig's upper gallery. Vessa is a voice on the rig's loudhailer. | *hoar.vessaHeard* | |
| `IdraDigGalleries` | 3 | Taren, Sela, Idra | Idra | **Compulsory.** Crossing point at the first rank in the ice. Teaches amber ice. | *hoar.ranksSeen* | |
| `VessaSecondLevel` | 7 | Vessa, Idra, Sela | Idra | **Compulsory.** Crossing point on the second level. Vessa's offer; Idra's answer. | *hoar.offerHeard* | |
| `SelaDigShortcut` | 4 | Sela, Idra | Sela | Interaction at a gap a thawed rank has left, second level. Optional: a short way down. | *hoar.gapTaken* | |
| `SelaMusterSlate` | 6 | Sela, Taren, Idra | Sela | **Compulsory.** Crossing point at the loading winch: Vessa's lading slate. What the Muster wants, read off a list. | *hoar.slateRead* | |
| `IdraJoins` | 6 | Idra, Sela | Idra | **Compulsory.** Crossing point at the bottom gallery, by the first coupling. She becomes playable when it closes. | `hoar.idraJoined` | `joinParty Idra` |
| `IdraMarshalWakes` | 4 | Taren, Idra, Sela | Idra | **Compulsory.** As the Host Marshal steps out of the wall. Teaches the couplings. | *hoar.marshalSeen* | |
| `IdraMarshalSleeps` | 6 | Taren, Idra, Vessa, Sela | Idra | **Compulsory.** After `bossdown.HostMarshal` (`Npc.Say`). Vessa lifts with a dozen guards and the plans. | `hoar.marshalAsleep` | |

### Lastlight, afterwards

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `TernLongSleep` | 16 | Tern, Taren, Sela, Idra | Tern, then Sela | **Compulsory.** Tern, `hoar.marshalAsleep` and not `hoar.sleepLearned`. The chapter's centre. Sela has four lines: two questions, nothing after the second answer, then the three rules back. | `hoar.sleepLearned` | |
| `TernAfter` | 1 | Tern | | Tern's last tier (`hoar.sleepLearned`) | | |
| `HaskOwes` | 3 | Hask, Idra | Hask | Hask, once: `hoar.marshalAsleep`, not `hask.owesHeard` | `hask.owesHeard` | |
| `HaskGoes` | 2 | Hask | | Hask's post (`hoar.sleepLearned`) | | |
| `IruSpeaksUp` | 6 | Iru, Tern | Iru | Iru standing in Tern's hall, once: `quest.ShrineLamps.complete` and `hoar.sleepLearned`, not `iru.spokeUp`. The side quest's reward. | `iru.spokeUp` | |
| `IruGoes` | 2 | Iru | | Iru's post (`hoar.sleepLearned`) when the lamps were never lit; she goes all the same, unsaid | | |
| `SelaVentBath` | 6 | Sela, Idra | Sela | Interaction at the vent bath-house, `hoar.marshalAsleep`. Taren is asleep in the steam and has no line. Optional. | *hoar.bathSeen* | |
| `SelaClampScars` | 3 | Taren, Idra | Taren | Interaction at the lamp shrine's four cut seats, `hoar.sleepLearned`. Sela says nothing here on purpose. Optional. | *hoar.scarsSeen* | |
| `IdraTally` | 6 | Idra, Sela, Taren | Idra | Interaction at the carving yard, where she is scratching the brace, `hoar.marshalAsleep`. Optional. | `idra.tallyShown` | |

### Many Hands, coming home

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IdraLiftFigures` | 12 | Idra, Keeper, Sela, Taren | Idra, then Sela | **Compulsory, chapter end.** Crossing point on the dock as her cutters lash on, `hoar.sleepLearned`. The lift figures; the tally; the crate's marks; Ishen sent for. | `hoar.liftFixed`, `chapter6.complete` | |
| `BinnaCutterCrews` | 1 | Binna | | Binna, `chapter6.complete`, once | | |
| `BinnaKottSibb` | 2 | Binna | | Binna, `chapter6.complete` and `quest.CutterDown.complete`, once. This is where Kott and Sibb become "named faces aboard". | | |
| `AbriCutters` | 1 | Keeper | | Abri, `chapter6.complete` | | |
| `HaskAboard` | 1 | Hask | | Hask in the freight hall with the beasts, `chapter6.complete` | | |
| `IruAboard` | 1 | Iru | | Iru on the lamp gallery with Pell, `chapter6.complete` | | |
| `MaruCutters`, `RuvaBeasts`, `TeoLiftFigures`, `IloHoarfellPost` | 1 each | Maru, Ruva, Teo, Ilo | | Each at their post, `chapter6.complete`, once: they take the place of that person's chapter 5 post line for one talk | | |

### Suggested routing (first match wins)

| Speaker | Beats, in order | Npc tiers |
|---|---|---|
| Tern | `TernLongSleep` | first `TernFirst`; repeat `TernRepeat`; final `TernAfter` (`finalFlag hoar.sleepLearned`) |
| Idra | none. She is an Npc only at the carving yard, from `hoar.ternHeard` until the party enters the Reach. | first `IdraPlan`; repeat `IdraRepeat` |
| Hask | `HaskStraysReturn`, `HaskStraysWaiting`, `HaskOwes`, `HaskStraysOffer` | first `HaskFirst`; repeat `HaskRepeat`; post `HaskGoes` (`hoar.sleepLearned`). Aboard: `HaskAboard`. |
| Iru | `IruSpeaksUp`, `IruLampsReturn`, `IruLampsOffer` | first `IruFirst`; repeat `IruRepeat`; post `IruGoes`. Aboard: `IruAboard`. |
| Keeper | `AbriCutters`, `AbriThirtySix`, then chapter 5's | |
| Ilo | `IloHoarfellPost`, `IloLastlightEntry`, then chapter 5's | |
| Binna | `BinnaKottSibb`, `BinnaCutterCrews`, then chapter 5's | |

## 3. Flags

**From the brief, and who sets them:** `hoar.charted` (`SelaHoarfellChart`), `hoar.idraHailed` (`IdraShearHail`), `hoar.arrived` (world), `hoar.ternHeard` (`TernFirst`), `hoar.planTaken` (`IdraPlan`), `hoar.herdPassed` (world), `hoar.idraJoined` (`IdraJoins`), `hoar.coupling1..3` (world), `hoar.marshalAsleep` (`IdraMarshalSleeps`), `hoar.sleepLearned` (`TernLongSleep`), `chapter6.complete` (`IdraLiftFigures`).

**Added by the script:** `hoar.liftFixed`, `hask.straysCounted`, `hask.owesHeard`, `iru.lampsLit`, `iru.spokeUp`, `idra.tallyShown`.

**Suggested for the world to own:** the italic flags in section 2.

## 4. Quests

Kinds are `ObjectiveKind` (TalkTo 0, Reach 1, Kill 2, Collect 3, Interact 4, Flag 5). Rewards are the brief's. `LongSleep` is the main quest (`main: true`, `chapter: 6`).

### LongSleep - The Long Sleep (main)

Description: `Sorrel's nursery can only leave by a road that runs cold, and the herders of Lastlight are the only people said to know how a young road sleeps.`

| # | Kind | Target | Step text | Points at |
|---|---|---|---|---|
| 0 | Reach | `AuroraShear` | Take the lane to Hoarfell through the Aurora Shear | AuroraShear (via ManyHandsHull or the chart) |
| 1 | Flag | `hoar.idraHailed` | Ride the currents down the ice ring | AuroraShear |
| 2 | Reach | `Lastlight` | Follow the striped cutter down to Lastlight | Lastlight |
| 3 | Flag | `hoar.ternHeard` | Find Old Tern under the fourth rib | Lastlight, speaker `Tern` |
| 4 | Flag | `hoar.planTaken` | Hear Idra Sorn's plan at the carving yard | Lastlight, speaker `Idra` (the pens crossing is on the way) |
| 5 | Flag | `hoar.idraJoined` | Cross the White Reach and go down to the bottom of the dig | FrozenHost, the bottom gallery |
| 6 | Interact x3 | `Coupling` | Break the three couplings, from the bottom up | FrozenHost, report `Coupling` |
| 7 | Kill | `HostMarshal` | Put the Host Marshal back to sleep | FrozenHost, encounter `HostMarshal` |
| 8 | Flag | `hoar.sleepLearned` | Go back to Old Tern, and sit down | Lastlight (via FrozenHost), speaker `Tern` |
| 9 | Flag | `hoar.liftFixed` | Bring Idra's cutters home to Many Hands | ManyHands, the dock |

If the couplings are the Marshal's break targets and not interactions on the way down, drop step 6 and let step 7 carry them. Reward: XP, scrip, a T4 part choice; Idra joins at step 5.

### StrayBeasts - Count Them Twice (Hask)

Description: `Three of Hask's rime-beasts went through the fence when the vents blew, and a beast alone doesn't know to come home.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `StrayBeast` | Walk the three strays in from the steam on the White Reach, without running | Bring in the strays |
| 1 | Flag | `hask.straysCounted` | Let Hask count them | Let Hask count them (waits on `Hask`) |

Reward: a frame part ("rib-bone, cut when my father was lead"); the rime-beasts come aboard either way (`HaskAboard`).

### ShrineLamps - The Skull Lamps (Iru)

Description: `The lamp shrine's three high lamps went out in the thaw, and Iru can't climb the rib-halls alone.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `SkullLamp` | Climb the rib-halls and light the three high lamps from the low flame | Light the high lamps |
| 1 | Flag | `iru.lampsLit` | Tell Iru the shrine is lit | Tell Iru it's lit (waits on `Iru`) |

Reward: a module, and `IruSpeaksUp` after the main story's last talk with Tern.

### CutterDown - Cutter Down (Idra)

Description: `One of Idra Sorn's three cutters is down in the Aurora Shear with two of her crew in its pod.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact | `CrewPod` | Find the striped cutter under the ice shelf in the Aurora Shear and tow its pod in | Find Idra's cutter |

One step: her thanks is in the talk at the wreck, so nothing depends on finding her afterwards (she may be in the party by then). Reward: a drive part; Kott and Sibb named aboard.

## 5. Display names

### New speakers (`CharacterDef.displayName`)

| Speaker id | Display name |
|---|---|
| `Idra` | Idra Sorn |
| `Tern` | Old Tern |
| `Hask` | Hask |
| `Iru` | Iru |
| `Vessa` | Vessa Korl |

No speaker beyond the brief's. Ilo, Keeper, Binna, Maru, Ruva and Teo speak on sheets they have.

### Sheets, for the artist

**Idra Sorn, two sheets.** As the brief: Rauk, slate-grey fine-grained skin, ember heat-seams, a low crest of horn-plates, a burn scar across the left brow plate, rescue-orange collar, a coil of line at her shoulder. On the Braced sheet the visor is up and the left arm's brace shows two fields of scratches: many fine old marks, and a few fresh ones on the other side. She has 65 lines and uses six expressions. The portrait test wants every one of the six on both sheets; this is where each is actually seen.

| Expression | Braced (Shear, Reach, dig) | Natural (Lastlight, Many Hands) | What it is on her |
|---|---|---|---|
| Neutral | 16 | 15 | Level, counting exits. Most of what she says, including "Tall." and "I did." |
| Determined | 9 | 6 | Giving an order: brow plates flat, seams brighter. The five lines of the plan. |
| Amused | 6 | 4 | Deadpan. Nothing moves but one brow plate. "You're flat. They don't mind." |
| Tender | 2 | 1 | Tired and unguarded for a moment. "That's a soldier coming off watch." |
| Worried | 1 | 2 | Not fear: arithmetic she dislikes. "It's that kind of week." |
| Flustered | 0 | 3 | Only about the tally. Seams flare, eyes go to the brace. "I have never been down." |

**Old Tern.** Very old Kept woman, seated in three hides and never drawn standing. Fur gone silver at the muzzle, a brow ruff so long it droops over one eye, cheek-whiskers braided with bone beads, a carved counting-stick across her knees; bright, amused eyes. 25 lines: Amused 9, Neutral 8, Tender 3, Worried 2, Determined 2, Sad 1.

**Hask.** Kept man in his forties, broad even for his people. Grey-brown fur weathered pale at the brow ruff, whiskers cropped short for work, a hide coat with bone toggles, a rib-bone herding whistle. His lips are always slightly parted, counting. 17 lines: Amused 5, Neutral 3, Determined 3, Worried 3, Tender 2, Happy 1.

**Iru.** Young Kept woman. Dark sleek fur with a pale blaze in the brow ruff, long whiskers that tremble, lamp soot on her fingers and one ear, a wick-knife and a string of bone lamp-hooks at her belt. Her eyes tend to the middle distance. 15 lines: Determined 4, Flustered 3, Curious 2, Worried 2, Happy 2, Tender 1, Shocked 1.

**Vessa Korl.** Vaskar, Hessa's elder sister: leaner, plates the grey of slag, crest-sails kept furled tight where Hessa's flare, frost on the plate edges, a dig-master's slate hung at her chest, no ornament. Eyes that weigh things. 9 lines: Neutral 7, Determined 1, Smug 1. She should look the same in all three.

### People named but never speaking

| Name | Who |
|---|---|
| Kott | Idra's winch hand. Big, saves the lamp cell, complains about the eel and eats four. Spoken for by Idra and Binna. |
| Sibb | Idra's pilot. Put the cutter in the ice; knocks three and two for "sorry"; apologises for her docking. Spoken for by Idra and Binna. |

Neither needs a sheet for this chapter. If they are to speak in the finale, they will.

### Other words the script introduces

| Word | What |
|---|---|
| the brace's two sides | Idra's tally (section 1) |
| "Tall." | Everything Idra says about the Host Marshal on first sight |
| cold slowly, never alone, sung to | The long sleep, in Tern's order. Sela says it back in the same words; chapter 10 should too. |
| eleven winters | How long the held road pulled; the tally on the fourth rib |
| the roof | What Tern calls the dead road her town is inside |
| a winter | The Kept's year ("sixty winters") |
| Vessa's slate | "Guards, a dozen to a cage. Cages, six, on order at the foundry. Roads, private, two. Stock for the first: juvenile, Sorrel, the whole clutch." |

### Enemies and boss (`EnemyDef.displayName`)

| Id | Display | Note |
|---|---|---|
| `MusterTender` | Muster Tender | Idra: "a tender" |
| `RimeStalker` | Rime-stalker | |
| `FrostMotes` | Frost Motes | |
| `MusterExcavator` | Muster Excavator | Vessa: "excavators" |
| `ThawedGuard` | Thawed Guard | Spoken of as "guards" and "ranks" |
| `BridleHound` | Bridle-hound | |
| `HostMarshal` | THE HOST MARSHAL | Epithet: `An Ancelane commander, still giving its last order`. Status in phase one: `THE RANKS ARE WAKING  ·  BREAK A COUPLING`. Break targets: `Thaw coupling`. |

The boss id must be exactly `HostMarshal` for its barks to be found.

### Zones (`ZoneDef.displayName`)

| Id | Display |
|---|---|
| `AuroraShear` | The Aurora Shear |
| `Lastlight` | Lastlight |
| `FrozenHost` | The Frozen Host |

### Floor signs and landmark labels

- Lastlight: `FOURTH RIB`, `TERN'S HALL`, `BEAST PENS`, `CARVING YARD`, `VENT BATH-HOUSE`, `LAMP SHRINE`, `REACH GATE`
- The White Reach: `THE WHITE REACH`, `VENT POCKET`
- The dig: `UPPER GALLERY`, `LOADING WINCH`, `BOTTOM GALLERY`

### Prompts

`Look at the cuts in the rib`, `Walk the stray in`, `Light the high lamp`, `Tow the crew pod`, `Take the gap down`, `Sit by the vent`, `Look at the cut seats`, `Break the coupling`.

### Items

The script names none. Hask trades "hide, bone fittings, rime-wool"; his reward is rib-bone plating.

## 6. Objective strings

Where the panel takes its line from the main quest's current step, section 4's step text is what shows. These are the rows the steps can't say.

| Zone | When | Label |
|---|---|---|
| AuroraShear | a current running against the party | Cross to a current running your way |
| AuroraShear | `chapter6.complete` not yet, returning | Ride the fast lane home |
| Lastlight | `hoar.ternHeard`, pens not yet seen | Find the Rauk officer at the beast pens |
| FrozenHost | the herd in the gap | Walk through the herd: don't run |
| FrozenHost | a whiteout closing | Get to the vent pocket |
| FrozenHost | the fight, couplings left | Break a coupling to put the wall back to sleep (1/3) |
| FrozenHost | the fight, phase two | Outlast the Host Marshal as its heat fails |
| FrozenHost | `hoar.marshalAsleep` | Climb out ahead of the cold |
| any | `chapter6.complete` | Chapter 6 complete. The Muster has the plans for a cage, and Ishen knows who drew the first one. |

Chapter-end card (`QuestToast`): heading `CHAPTER 6 COMPLETE`, title `THE LONG SLEEP`, body `Cold slowly, never alone, sung to: the herders of Lastlight are aboard with what they know, and Old Tern has stayed with her roof. Idra Sorn's cutters give Many Hands the engines it lacked. The Muster left Hoarfell with a crate of plans. Until then nothing closes behind you, so keep exploring.`

## 7. Barks added

Existing entries are untouched.

| Speaker | Keys | Used by | Wiring |
|---|---|---|---|
| Idra | Every key under `Taren`, 55 in all, in her voice | Whatever plays them for the other two | Automatic once she is the active or partner hero. `teach.guard` is written for her Brace, `extra` for Last One Out. |
| Idra | `hail.tender`, `hail.fastlane`, `hail.reach` | Before she joins: radio in the Shear, and leading on the Reach | `RadioHail`, speaker `Idra` |
| Taren, Sela, Idra | `bossphase.HostMarshal`, `bossopening.HostMarshal` | `BossController.Situation` | Automatic for id `HostMarshal`: the last coupling goes; it rallies the wall |
| Taren, Sela, Idra | `current` | A current turns against the active ship | Two lines in turn |
| Taren, Sela, Idra | `whiteout` | A whiteout begins | |
| Taren, Sela, Idra | `thaw` | A rank steps out of the ice | |
| Taren, Sela, Idra | `coupling` | A coupling breaks | Three lines in turn, in order. Idra's are "One.", "Two.", "Three. Up." |
| Taren, Sela, Idra | `stray` | A stray beast walked in | `DiscoveryPoint.situation`; three in turn |
| Taren, Sela, Idra | `lamp` | A high lamp lit | `DiscoveryPoint.situation`; three in turn |
| Vessa | `hail.rank1`, `hail.rank2`, `hail.coupling1`, `hail.coupling2`, `hail.excavator` | The loudhailer, down the dig | `RadioHail`, or `BarkService.Play("Vessa", …)` as each thing happens |

## 8. What the script assumes

**No new commands or dialogue features.**

1. **Six portrait sets.** Idra's two, Tern, Hask, Iru, Vessa. Section 5 describes each and counts the expressions used. Both of Idra's sheets must hold all six of hers: the portrait test checks each of a hero's sets against every expression the hero uses.
2. **Tests to extend.** `DialogueInputTests`: add `Idra`, `Tern`, `Hask`, `Iru`, `Vessa` to the speaker-prefix list; to `branches`: `HaskFirst` {shop, world}. `HaskRepeat` and `HaskStraysReturn` end in the shop. `IdraJoins` adds a party member: that test compares the party before and after a save, which still holds, but any test that assumes two members will not. `QaPlanWorldTests`: the quest-starter table gets (`LongSleep`, `SelaHoarfellChart`), (`CutterDown`, `IdraPens`), (`StrayBeasts`, `HaskStraysOffer`), (`ShrineLamps`, `IruLampsOffer`); the ninety-second list gets the nodes marked compulsory. `PeopleMarks.Repeats` gets `AbriThirtySix`, `IloLastlightEntry`, `HaskStraysWaiting`, `AbriCutters`.
3. **Idra before she joins.** A striped cutter in the Shear that tows the party out of the first bad current; an Npc at the pens and the carving yard; a guide on the Reach and down the dig, ahead of the party, until the bottom gallery. `joinParty Idra` is in `IdraJoins`.
4. **Idra's portrait by place.** `PortraitLookup` picks the Natural set in safe zones and the other set everywhere else, so she is Braced in the Shear, on the Reach and in the dig, and Natural in Lastlight and aboard. That is already how it works for the other two.
5. **One shop in Lastlight**, Hask's, under the zone's own inventory. The bench is the carving yard and the repair cradle is the vent bath-house: world objects, no speaker.
6. **Vessa is never on screen.** Three loudhailer talks and five barks. She has a sheet because she has lines.
7. **Tern is seated in every scene** and `IruSpeaksUp` is played in her hall with Iru standing in it.
8. **Gates.** `IdraPens` needs `hoar.ternHeard`. The Reach gate opens on `hoar.planTaken`. The strays, the high lamps and the crew pod are offered only at their quest's step. `SelaClampScars` needs `hoar.sleepLearned` (it answers Tern's "clamps at the head").
9. **Order.** `IdraMarshalSleeps` follows the Marshal kneeling. The shared `boss` bark should be left out for `HostMarshal`: Idra's own would be "Done. Count heads." over a thing being put to rest.
10. **If `CutterDown` is left undone**, Idra's cutters still dock three at the end: she has fetched the pod herself, unsaid. Only `BinnaKottSibb` is lost.

## 9. Where the script departs from the brief

- **The couplings.** The brief has them one to a level, broken on the way up, and also has three around the Marshal's gallery as his break targets. The plan as Idra says it ("from the bottom up; the cold comes in behind us") holds for either, and the lines in the fight say only that each one broken puts a third of the wall back to sleep.
- **Sela's half-agreement ends in what she asks.** In `TernLongSleep` she asks why this road didn't sleep, then whether it stayed. Tern: "Oh, it stayed. It's the best roof on Hoarfell. Nothing has gone anywhere along it since." Sela says nothing to that, and her next line is the three rules, said back. Meret is not named in the scene. Chapter 7's line at the Glass Choir is still hers to say.
- **Who says Meret is right.** Idra, at Vessa's slate: "Your director is right about that much. Leave a road open, and this is who comes down it." Taren asks what keeps them off; she has "a cutter and a tally". Nobody has more.
- **The tally is on her brace**, two sides, and the party is ahead by the end (section 1).
- **`CutterDown` starts in a compulsory scene** (Taren offers; "I don't ask." "You didn't.") and is a single step, so it can be finished before or after she joins.
- **Kott and Sibb have no lines.** Idra reads the pod's lamp and the knocking; Binna reports them aboard.
- **Tern's refusal.** She won't tell the long sleep until the dig is shut, which is what sends the party to Idra. The brief's line ("You came to ask how the young ones sleep. Sit down.") opens the second scene.
- **"Thirty-six days"** is said once, at the start. The chapter ends without a count.
- **Iru goes either way.** With the lamps lit she tells Tern herself (`IruSpeaksUp`); without, Tern has watched her pack and told her which wicks to take (`IruGoes`).
- **Hask has the shop.** The brief names no trader in Lastlight.
- **The crate is recognised by its carving**, the same hand as Lanternwick's archive wall, so Sela sends for Ishen herself. Oriel is not named.
