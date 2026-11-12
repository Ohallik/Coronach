# Chapter 3 script manifest - Carrying a Place (Xylos)

For the engineers wiring chapter 3. Script: [Xylos.yarn](Xylos.yarn). Barks: [barks.json](barks.json). Brief: [ch03-carrying-a-place.md](../chapters/ch03-carrying-a-place.md).

**What is here:** 68 nodes, 290 dialogue lines, 20 option labels (310 strings). Compiled clean with the project's own compiler (Yarn Spinner 3.2.4, all ten files together: 0 errors, 161 nodes, no duplicate titles). No new Yarn commands are used: only `setFlag`, `startQuest`, `shop`, `bench`, `repair`.

**Read section 8 before the next world build.** `WorldBuilder` and `LanternRoadBuilder` copy every `docs/writing/*.yarn` into `Resources/Dialogue`, so `Xylos.yarn` joins the Yarn project the next time either runs, wired or not. From that moment `DialogueLayoutTests` (a portrait set for every speaker) and `DialogueInputTests` (a known speaker prefix for every node, and `startQuest` throws on a quest that doesn't exist) fail until the portraits, the five `QuestDef`s and the test lists in section 8 are in. Hold the file out of the copy, or land those first.

## 1. Conventions this file follows

- Every title starts with its owning speaker id, as `DialogueInputTests` expects: `Abri…` (speaker `Keeper`), `Pell…`, `Ruva…`, `Teo…`, `Binna…`, `Jask…`, `Senn…`, `Hadda…`, `Patrol…`, `Garo…`, `Dabb…`, and `Sela…` for the party reading something with nobody else there.
- A node sets a flag itself when **the conversation is the event** (terms heard, a crew relieved, the vote). A node sets nothing when the world owns the event (a barge cut free, the rope, an anchor root). Suggested flags for the second kind are in the table in italics; name them as you like.
- `bossdown.Seedthane` and `bossdown.Floodcrown` are the engine's. The brief's `xylos.seedthaneGone` and `xylos.floodcrownGone` are used as the story flags: the party has heard what the fight changed (as `relay.hessaYielded` sits beside `bossdown.HessaKorl`).
- Trap, from `DiscoveryPoint.CompleteDialogue`: a point whose dialogue node sets the point's own flag saves and returns **without** `Discover()`, so its `report` never reaches the quest. Every node below that sets a flag is meant for a `Flag` step, never a counted `Interact` step.

## 2. Nodes

Lines are dialogue lines (options not counted). "Compulsory" marks the main path; none of those runs past eleven lines (the longest, the vote, is about half a minute of text at the typewriter's pace).

### Tallow Drift

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `AbriSeedRoad` | 9 | Keeper, Taren, Sela | **Compulsory.** Abri, once `chapter2.homecoming` and `SeedRoad` not started. Replaces `AbriLanternAfter`. | | `startQuest SeedRoad` |
| `AbriSeedRoadRepeat` | 1 | Keeper | Abri, `SeedRoad` started and not `xylos.charted` | | |
| `AbriSeedRoadCharted` | 1 | Keeper | Abri, `xylos.charted` and not `xylos.termsHeard` | | |
| `AbriSeedRoadWaiting` | 1 | Keeper | Abri, `xylos.termsHeard` and not `chapter3.complete` (a visit home mid-chapter) | | |
| `PellLaneLantern` | 7 | Pell, Sela, Taren | **Compulsory.** Pell at Tallow's lantern, `SeedRoad` started and not `xylos.charted`. Opens the Xylos warp. | `xylos.charted` | |
| `PellLaneRepeat` | 1 | Pell | Pell, `xylos.charted` and not `chapter3.complete` | | |
| `AbriDinnerCall` | 11 | Keeper, Taren, Sela | **Compulsory, chapter end.** Radio, once `xylos.voteHeard`: on the walk back to the landing pool, or on launch. Nobody is standing there (`Npc.Say("AbriDinnerCall","Keeper")` or a crossing point). | `chapter3.complete` | |
| `AbriDinnerServed` | 1 | Keeper | Abri, `chapter3.complete` | | |
| `PellAfterVote` | 1 | Pell | Pell, `chapter3.complete` | | |

### Xylos nearspace (flight; all radio)

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `TeoBargeHail` | 8 | Teo, Taren, Sela | **Compulsory.** Crossing point just inside the arrival beacon. Teaches the knots (blade or sustained fire) and the delta beacon. | *xylos.teoHailed* | |
| `PatrolHail` | 9 | Patrol, Sela, Taren | **Compulsory.** Crossing point mid-lane, between barge one and the weather mirror. Once. Written to hold whether or not barges are already free. | *xylos.patrolHeard* | |
| `TeoClaimDrones` | 6 | Teo, Sela, Taren | Crossing point on the approach to barge three, before the claim drones wake. Optional. Written to hold whether barge three is still snagged or already free. | *xylos.claimSeen* | `startQuest ClaimTags` |
| `SelaWeatherMirror` | 4 | Sela, Taren | Interaction at the weather mirror. Optional. | *xylos.mirrorSeen* | |

Teo's line as each barge comes free, and the landing call, are barks (section 7), so nothing stops the flying.

### Rootmarket

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `RuvaArrival` | 8 | Ruva, Binna, Sela, Taren | **Compulsory.** Crossing point between the pad and the south-east mooring, first landing. Ruva is at the mooring; Binna shouts from her kitchen and need not be moved. Ends with the rope in Taren's hands. | *xylos.arrivalSeen* | |
| `RuvaRopeHold` | 1 | Ruva | Ruva's `holdNode` until `xylos.ropeHauled` | | |
| `BinnaRopeHold` | 1 | Binna | Binna's `holdNode` until `xylos.ropeHauled` | | |
| `RuvaTerms` | 10 | Ruva, Taren, Sela | **Compulsory.** Ruva's first node, once `xylos.ropeHauled`. Names the river gate "east, past Hadda's press". | `xylos.termsHeard` | |
| `RuvaRepeat` | 4 | Ruva | Ruva's repeat. Three options, all talk. | | |
| `RuvaCrewsHome` | 2 | Ruva | Ruva, once: `xylos.marrow1` and `xylos.marrow2`, not `xylos.crewsThanked`, not `xylos.floodcrownGone` | `xylos.crewsThanked` | |
| `RuvaVote` | 10 | Ruva, Senn, Taren, Sela | **Compulsory.** Ruva at the seed house, `xylos.floodcrownGone` and not `xylos.voteHeard`. Senn has one line, called across from his island; he need not be present. | `xylos.voteHeard` | |
| `RuvaAfterVote` | 2 | Ruva | Ruva, `xylos.voteHeard`, once | `ruva.lineHeld` | |
| `RuvaAfterVoteRepeat` | 1 | Ruva | Ruva, `ruva.lineHeld` | | |
| `SelaSeedHouse` | 3 | Sela, Taren | Interaction at the seed-house doorway. Optional. Needs `xylos.termsHeard` ("what she won't sell"). | *xylos.seedHouseSeen* | |
| `SelaCompactNotice` | 6 | Sela, Taren | Interaction on the seed-house door. Optional. | *xylos.noticeRead* | |
| `TeoFirst` | 7 | Teo, Taren, Sela | Teo by the landing pool. He should arrive once `xylos.termsHeard` (the first line after his thanks cites Ruva). | | |
| `TeoRepeat` | 4 | Teo | Teo's repeat until the vote. Three options, one per carrier. | | |
| `TeoTagsOffer` | 5 | Teo, Sela, Taren | Teo, met, `ClaimTags` not started (the party never went near barge three) | | `startQuest ClaimTags` |
| `TeoTagsReturn` | 5 | Teo, Sela | Teo, `ClaimTags` at step 1 | `xylos.tagsReported` | |
| `TeoPost` | 2 | Teo | Teo back in town, `xylos.voteHeard` | | |
| `BinnaFirst` | 8 | Binna, Taren, Sela | Binna's first node. Options: shop, or leave. She sends parts buyers to Hadda. | | `shop` (option 1) |
| `BinnaRepeat` | 1 | Binna | Binna's repeat | | `shop` |
| `BinnaBasketsOffer` | 6 | Binna, Taren, Sela | Binna, met, `xylos.termsHeard`, `ThreeBaskets` not started | | `startQuest ThreeBaskets`, `shop` |
| `BinnaBasketsReturn` | 4 | Binna, Sela | Binna, `ThreeBaskets` at step 1 | `binna.basketsReturned` | `shop` |
| `BinnaAfterBaskets` | 1 | Binna | Binna, once: `quest.ThreeBaskets.complete`, not `binna.afterBaskets`, not `xylos.voteHeard` | `binna.afterBaskets` | `shop` |
| `BinnaPost` | 2 | Binna | Binna's post (`xylos.voteHeard`) | | `shop` |
| `JaskFirst` | 9 | Jask, Sela, Taren | Jask at the town ferry stage (west), first node | | `startQuest Questions` |
| `JaskRepeat` | 2 | Jask, Sela | Jask in town, repeat | | |
| `SelaJaskPole` | 5 | Taren, Sela | Interaction at the town ferry stage: his pole against the post. `Questions` at step 1 and `xylos.voteHeard`. Jask himself is gone from every scene after the vote. | `jask.poleFound` | |
| `SennFirst` | 6 | Senn, Taren, Sela | Senn on his island, first node | | |
| `SennRepeat` | 1 | Senn | Senn's repeat | | |
| `SennPostOffer` | 5 | Senn, Taren | Senn, met, `xylos.termsHeard`, `MooringPost` not started | | `startQuest MooringPost` |
| `SennPostReturn` | 6 | Senn, Taren | Senn, `MooringPost` at step 1 | `senn.postReported` | |
| `SennAfterVote` | 2 | Senn | Senn's post (`xylos.voteHeard`) | | |
| `SennLastWord` | 4 | Senn, Taren | Senn, once: `quest.MooringPost.complete` and `xylos.voteHeard`, not `senn.lastWord`. This is the quest's "last word on leaving"; it outranks `SennAfterVote`. | `senn.lastWord` | |
| `HaddaFirst` | 8 | Hadda, Taren, Sela | Hadda at the press, first node. Carries the brief's Driftreed mat mention. Options: press, repair, parts. | | `bench`, `repair`, `shop` |
| `HaddaRepeat` | 1 | Hadda | Hadda's repeat. Same options. | | `bench`, `repair`, `shop` |
| `HaddaPost` | 2 | Hadda | Hadda's post (`xylos.voteHeard`). Points at Wendmire in one line; holds before or after Abri's call. Same options. | | `bench`, `repair`, `shop` |
| `HaddaAir` | 3 | Taren, Hadda | Hadda, once: `chapter3.complete`, not `hadda.airHeard`. The pointer said outright: Driftreed, on Wendmire. Then back to `HaddaPost`. Same options. | `hadda.airHeard` | `bench`, `repair`, `shop` |

### The Braided River

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `SelaRiverArch` | 3 | Sela, Taren | **Compulsory.** Crossing point under the first arch, with the Canopy in view | *xylos.archSeen* | |
| `SelaSluiceA` | 4 | Taren, Sela | **Compulsory.** Interaction at Sluice A. Written as they reach for the wheel, so it holds whether the water drops before or after the talk. | `xylos.sluiceA` | |
| `GaroRelief` | 5 | Garo, Taren | **Compulsory.** Garo at Marrow-1's hatch, once the encounter round the carrier is cleared | `xylos.marrow1` | |
| `GaroRepeat` | 1 | Garo | Garo's repeat | | |
| `DabbRelief` | 5 | Dabb, Taren, Sela | **Compulsory.** Dabb at Marrow-2's hatch, once its encounter is cleared | `xylos.marrow2` | |
| `DabbRepeat` | 1 | Dabb | Dabb's repeat | | |
| `JaskFerryFirst` | 6 | Jask, Sela | Jask at the mid-river ferry stage, as that Npc's `firstNode` (plays only if he was never met in town) | | `startQuest Questions` |
| `JaskFerry` | 11 | Jask, Sela, Taren | Jask at the mid-river ferry stage: met, `Questions` at step 0, and `xylos.sluiceA` (so the beat can't fire on the town Jask). The "where is it going to" talk. | `jask.ferryTalk` | |
| `JaskFerryRepeat` | 2 | Jask, Sela | Jask at the ferry stage, repeat | | |
| `SelaMooringPost` | 5 | Taren, Sela | Interaction at the ring on Ulma's island (the spur off the second island). `MooringPost` at step 0. | `senn.postSet` | |
| `SelaSluiceB` | 3 | Sela, Taren | **Compulsory.** Interaction at Sluice B, Seedfall and Marrow-3 in view across the south channel | `xylos.sluiceB` | |
| `SelaSeedfall` | 4 | Taren, Sela | **Compulsory.** Crossing point at the edge of Seedfall, before the Seedthane wakes (`previewEncounter`). Teaches the pods as cover. | *xylos.seedfallSeen* | |
| `SelaSeedthaneGone` | 3 | Sela, Taren | As the Seedthane leaves (`bossdown.Seedthane`). Optional colour; drop it if the exit plays better without. | *xylos.seedthaneSeenOff* | |
| `TeoSeedfall` | 10 | Teo, Taren, Sela | **Compulsory.** Teo beside Marrow-3 once `bossdown.Seedthane`. Sends the party east. | `xylos.seedthaneGone` | |
| `TeoSeedfallRepeat` | 1 | Teo | Teo at Marrow-3 until the vote | | |

### The Walking Canopy

| Node | Lines | Speakers | Where and when | Sets | Runs |
|---|---|---|---|---|---|
| `SelaCanopyMooring` | 3 | Sela, Taren | **Compulsory.** Crossing point in the root hall, anchor root A in view | *xylos.canopyEntered* | |
| `SelaCanopyAnchor` | 3 | Taren, Sela | **Compulsory.** After anchor root A's lurch has settled (`xylos.anchor1`): `Npc.Say` once the gallery is open and the vault stair shut, which is what the lines say. B and C use the `anchor` bark. | | |
| `SelaSeedVault` | 4 | Sela, Taren | Interaction at the vault's window slot, the Floodcrown wading below | *xylos.vaultSeen* | |
| `SelaFloodcrownBasin` | 4 | Taren, Sela | **Compulsory.** Crossing point at the basin door checkpoint. Teaches the flood gates. | *xylos.basinSeen* | |
| `SelaFloodcrownGone` | 5 | Sela, Taren | **Compulsory.** After `bossdown.Floodcrown`, once it has gone down the drain channel | `xylos.floodcrownGone` | |

### Suggested routing (first match wins, as in `SideStories.Beats`)

| Speaker | Beats, in order | Npc tiers |
|---|---|---|
| Keeper | `AbriDinnerServed`, `AbriSeedRoadWaiting`, `AbriSeedRoadCharted`, `AbriSeedRoadRepeat`, `AbriSeedRoad`, then the existing chapter 2 beats | unchanged |
| Pell | `PellAfterVote`, `PellLaneRepeat`, `PellLaneLantern` | `PellTallow` stays her line before chapter 3 starts |
| Ruva | `RuvaVote`, `RuvaCrewsHome` | hold `RuvaRopeHold`; first `RuvaTerms`; repeat `RuvaRepeat`; final `RuvaAfterVote` (`finalFlag xylos.voteHeard`, `finalCompletionFlag ruva.lineHeld`, `finalRepeatNode RuvaAfterVoteRepeat`) |
| Teo | `TeoSeedfall` (while `bossdown.Seedthane` and not `xylos.seedthaneGone`), `TeoTagsReturn`, `TeoTagsOffer` | town: first `TeoFirst`, repeat `TeoRepeat`, post `TeoPost` (`xylos.voteHeard`). Seedfall object: repeat `TeoSeedfallRepeat`. Only one Teo in the world at a time. |
| Binna | `BinnaBasketsReturn`, `BinnaBasketsOffer`, `BinnaAfterBaskets` | hold `BinnaRopeHold`; first `BinnaFirst`; repeat `BinnaRepeat`; post `BinnaPost` |
| Jask | `JaskFerry` | town object: first `JaskFirst`, repeat `JaskRepeat`. Ferry object: first `JaskFerryFirst`, repeat `JaskFerryRepeat`. |
| Senn | `SennLastWord`, `SennPostReturn`, `SennPostOffer` | first `SennFirst`; repeat `SennRepeat`; post `SennAfterVote` |
| Hadda | none | first `HaddaFirst`; repeat `HaddaRepeat`; post `HaddaPost`; final `HaddaAir` (`finalFlag chapter3.complete`, `finalCompletionFlag hadda.airHeard`, `finalRepeatNode HaddaPost`) |
| Garo, Dabb | none | first `GaroRelief` / `DabbRelief`; repeat `GaroRepeat` / `DabbRepeat` |

## 3. Flags

**From the brief, and who sets them:** `xylos.charted` (`PellLaneLantern`), `xylos.barge1..3` (world; 1 is the first barge, 2 the one in the mirror's shadow, 3 the far one under the claim drones: Teo's barks are written to that order), `xylos.landed` (world), `xylos.ropeHauled` (world), `xylos.termsHeard` (`RuvaTerms`), `xylos.marrow1` (`GaroRelief`), `xylos.marrow2` (`DabbRelief`), `xylos.sluiceA` (`SelaSluiceA`), `xylos.sluiceB` (`SelaSluiceB`), `xylos.seedthaneGone` (`TeoSeedfall`), `xylos.anchor1..3` (world), `xylos.floodcrownGone` (`SelaFloodcrownGone`), `xylos.voteHeard` (`RuvaVote`), `chapter3.complete` (`AbriDinnerCall`).

**Added by the script:** `xylos.tagsReported`, `xylos.crewsThanked`, `binna.basketsReturned`, `binna.afterBaskets`, `jask.ferryTalk`, `jask.poleFound`, `senn.postSet`, `senn.postReported`, `senn.lastWord`, `ruva.lineHeld`, `hadda.airHeard`.

**Suggested for the world to own:** the italic flags in section 2.

## 4. Quests

Kinds are `ObjectiveKind` (TalkTo 0, Reach 1, Kill 2, Collect 3, Interact 4, Flag 5). Rewards are the brief's; the numbers are the systems stream's. `SeedRoad` is the main quest: `main: true`, `chapter: 3` on the new `QuestDef` fields (or `PauseMenu.MainQuests` if those haven't landed). The "Points at" column is what each step's `QuestTarget` would name (`scene`, then `speaker`, `report` or `encounter`); a side quest's "Panel brief" is its `QuestTarget.brief`.

### SeedRoad - Carrying a Place (main)

Description: `Tallow's cabins need feeding, and the river growers of Xylos won't sell a field they would have to pull up by the root.`

| # | Kind | Target | Step text | Points at |
|---|---|---|---|---|
| 0 | Flag | `xylos.charted` | Teach Pell's lantern the lane to Xylos | TallowDrift, speaker `Pell` |
| 1 | Reach | `XylosNearspace` | Take the Xylos warp off Tallow's approach | XylosNearspace (via TallowApproach) |
| 2 | Interact x3 | `SeedBarge` | Cut Teo Marrow's seed barges free of the snagvine | XylosNearspace, report `SeedBarge` |
| 3 | Reach | `Rootmarket` | Follow the delta beacon down to Rootmarket | Rootmarket |
| 4 | Flag | `xylos.termsHeard` | Take the mooring line, then hear Ruva Okh out | Rootmarket, speaker `Ruva` |
| 5 | Flag | `xylos.sluiceA` | Open the middle sluice on the Braided River's north bank | BraidedRiver, the Sluice A point |
| 6 | Flag | `xylos.marrow1` | Relieve the crew shut inside Marrow-1, on the first island | BraidedRiver, speaker `Garo` |
| 7 | Flag | `xylos.marrow2` | Relieve the crew shut inside Marrow-2, on the second island | BraidedRiver, speaker `Dabb` |
| 8 | Flag | `xylos.sluiceB` | Open the south sluice to reach Seedfall bank | BraidedRiver, the Sluice B point |
| 9 | Kill | `Seedthane` | Drive the Seedthane off Marrow-3 | BraidedRiver, encounter `Seedthane` |
| 10 | Flag | `xylos.seedthaneGone` | Ask Teo Marrow what still blocks the crossing | BraidedRiver, speaker `Teo` |
| 11 | Interact x3 | `AnchorRoot` | Work the Walking Canopy's anchor roots to reach its basin | WalkingCanopy (via BraidedRiver), report `AnchorRoot` |
| 12 | Kill | `Floodcrown` | Stop the Floodcrown holding the basin's flood gates | WalkingCanopy, encounter `Floodcrown` |
| 13 | Flag | `xylos.voteHeard` | Tell Ruva Okh the crossing is open, and hear the vote | Rootmarket (via BraidedRiver), speaker `Ruva` |

Steps 5 to 8 are flags so the river can be done in any order: `QuestService.CatchUp` skips a flag step already satisfied, and it can't do that for a counted interaction. Step 11 is counted, so the Canopy mooring must stay shut until `xylos.seedthaneGone` (step 10), or anchor reports are lost while the quest waits on Teo. Steps 1, 4 and 10 are not in the brief's list. Each step's text is short enough to be the objective panel's line where a zone has no hand-written one (the longest is 59 characters). Reward: XP, scrip, a T3 part choice.

### ThreeBaskets - Three Baskets (Binna)

Description: `The storm stripped Binna's spice garden, and she is cooking for a vote.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `SpicePods` | Pick red, blue and yellow spice pods: one plant on each river island, one at Seedfall | Pick spice pods |
| 1 | Flag | `binna.basketsReturned` | Bring the three baskets back to Binna's kitchen | Bring the baskets to Binna (waits on `Binna`) |

Reward: cooked food on Binna's counter. `ShopOffer.requires: binna.basketsReturned` puts it there when she says it will be ("There'll be lane food on the shelf").

### MooringPost - The Mooring Post (Old Senn)

Description: `Old Senn carved a mooring post for his wife's island and never carried it out there.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Flag | `senn.postSet` | Set Senn's mooring post on Ulma's island, off the Braided River's second island | Set Senn's mooring post |
| 1 | Flag | `senn.postReported` | Tell Old Senn where the post went | Tell Senn where it went (waits on `Senn`) |

Reward: a module (Senn hands over the thing that "read the river for Ulma"). `SennLastWord` follows after the vote.

### Questions - Questions for a Surveyor (Jask)

Description: `Jask poles the Rootmarket ferry and has never seen a place with no ground under it. He has questions, mostly for Sela.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Flag | `jask.ferryTalk` | Find Jask at his ferry stage, mid-river | Find Jask at his ferry (waits on `Jask`) |
| 1 | Flag | `jask.poleFound` | Find Jask at Rootmarket's ferry stage once the growers have voted | Find Jask after the vote |

The first of the brief's three talks is the one that starts the quest (`JaskFirst`, or `JaskFerryFirst`). Reward: Sela's Fortune +1 token; Jask aboard in chapter 5.

### ClaimTags - Unmoored (Teo)

Description: `A salvage crew has tagged Teo's barges as vessels off their registered mooring, the moment they were worth taking.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `ClaimTag` | Burn Hulljack's claim tags off Teo's three seed barges | Burn off the claim tags |
| 1 | Flag | `xylos.tagsReported` | Tell Teo Marrow his barges are unclaimed | Tell Teo they're gone (waits on `Teo`) |

Reward: scrip, a drive part (Teo: "a drive from my spare rack"). The warning about the salvage crew is in `TeoTagsReturn`.

## 5. Display names

### Speakers (`CharacterDef.displayName`)

| Speaker id | Display name | Note |
|---|---|---|
| `Ruva` | Ruva Okh | |
| `Teo` | Teo Marrow | |
| `Binna` | Binna | |
| `Jask` | Jask | |
| `Senn` | Old Senn | |
| `Hadda` | Hadda | |
| `Patrol` | Steady Hand | Radio only; uses `Neutral` and nothing else |
| `Garo` | Garo | **New name.** Mate of Marrow-1. Written without a pronoun. |
| `Dabb` | Dabb | **New name.** Pump hand of Marrow-2 (M). |

Emotions each new speaker uses, for the portrait check: Ruva (Amused, Curious, Determined, Neutral, Tender, Worried), Teo (Amused, Angry, Determined, Happy, Neutral, Tender, Worried), Binna (Amused, Curious, Determined, Happy, Worried), Jask (Curious, Happy, Shocked, Worried), Senn (Amused, Neutral, Smug, Tender), Hadda (Amused, Determined, Flustered, Happy, Playful, Tender), Garo (Amused, Determined, Neutral, Worried), Dabb (Amused, Determined, Worried), Patrol (Neutral).

### Other names the script introduces

| Name | What |
|---|---|
| Ulma | Senn's late wife; her island is the spur off the second island |
| Hulljack Salvage | The name on the claim tags ("Vessel off its registered mooring. Claimed for recovery. Hulljack Salvage.") |
| cabin nine | Abri's running complaint aboard Tallow (not a place the player visits) |
| a flood | The Okhari year ("sixty floods", "twenty floods back") |

### Enemies and bosses (`EnemyDef.displayName`)

| Id | Display | Note |
|---|---|---|
| `Podskimmer` | Podskimmer | |
| `SnagvineNode` | Snagvine Knot | **Changed from the brief's "Snagvine Node".** Everyone says "knots"; "anchor" is kept for the Canopy's anchor roots. |
| `ClaimDrone` | Claim Drone | |
| `Thornback` | Thornback | |
| `Reedstalker` | Reedstalker | |
| `Sporecaster` | Sporecaster | |
| `MireflySwarm` | Mirefly Swarm | |
| `Rootwarden` | Rootwarden | |
| `Seedthane` | THE SEEDTHANE | Epithet: `A seed-bearer nesting on a beached carrier`. Status while planted: `PLANTED  ·  BREAK ITS LEGS` |
| `Floodcrown` | THE FLOODCROWN | Epithet: `The storm's guest, keeping the Canopy's flood gates`. Status in phase one: `THE BASIN IS FLOODED  ·  BREAK A FLOOD GATE` |

The boss ids must be exactly `Seedthane` and `Floodcrown` for their barks to be found.

### Zones (`ZoneDef.displayName`)

| Id | Display |
|---|---|
| `XylosNearspace` | Xylos Nearspace |
| `Rootmarket` | Rootmarket |
| `BraidedRiver` | The Braided River |
| `WalkingCanopy` | The Walking Canopy |

### Floor signs and landmark labels

- Nearspace: `DELTA BEACON`, `WEATHER MIRROR`, `SEED BARGE`
- Rootmarket: `LANDING POOL`, `FERRY STAGE`, `SEED HOUSE`, `BINNA'S KITCHEN`, `THE PRESS`, `MOORING`, `SENN'S ISLAND`, `RIVER GATE`
- Braided River: `FIRST ARCH`, `MIDDLE SLUICE`, `SOUTH SLUICE`, `FERRY STAGE`, `MARROW-1`, `MARROW-2`, `ULMA'S ISLAND`, `SEEDFALL`, `MARROW-3`, `CANOPY MOORING`
- Walking Canopy: `ROOT HALL`, `FLOODING GALLERY`, `SEED VAULT`, `ROOT LADDER`, `THE BASIN`

### Prompts

`Haul the mooring line`, `Read the Compact notice`, `Look into the seed house`, `Look at the weather mirror`, `Open the middle sluice`, `Open the south sluice`, `Pick the red spice pods`, `Pick the blue spice pods`, `Pick the yellow spice pods`, `Set Senn's mooring post`, `Look at Jask's pole`, `Burn off the claim tag`, `Cut the anchor root`, `Set the anchor root`, `Look down through the slot`.

### Items

| Id | Display | Note |
|---|---|---|
| `AmberSap` | Amber Sap | From the brief |
| `Rootfibre` | Rootfibre | From the brief |

The script names no other item. It refers to "a drive from my spare rack" (Teo's reward), "lane food" (Binna's cooked stock) and the thing that "read the river for Ulma" (Senn's module), so those can be whatever the loot stream calls them. If names are wanted in the script's spirit: `Tide Reader` for Senn's module; `Red Pod Stew`, `Blue Pod Pickle` and `Yellow Pod Flatbread` for Binna's three.

## 6. Objective strings

In `ObjectiveHud`'s style: what to do, then why after a colon. These are the hand-written set. Where a zone takes its line from the main quest's current step instead, section 4's step text is what shows, and only the rows the steps can't say are needed from here (the fight lines, the counts, the chapter's last line).

| Zone | When | Label |
|---|---|---|
| TallowDrift | `SeedRoad` not started | See Keeper Abri: the cabins are asking about dinner |
| TallowDrift | started, not `xylos.charted` | Teach Pell's lantern the lane to Xylos |
| TallowDrift | `xylos.charted` | Launch for the Xylos warp |
| TallowApproach | `xylos.charted` | Take the warp to Xylos |
| XylosNearspace | fewer than three barges free | Free Teo Marrow's seed barges (1/3) |
| XylosNearspace | at a snagged barge | Cut the snagvine knots (2/3) |
| XylosNearspace | three free, not landed | Follow the delta beacon down to Rootmarket |
| XylosNearspace | `xylos.voteHeard` | Warp home to Tallow Approach |
| Rootmarket | before `RuvaArrival` | Find who speaks for Rootmarket's growers |
| Rootmarket | arrival seen, not `xylos.ropeHauled` | Haul the mooring line: Ruva is a back short |
| Rootmarket | `xylos.ropeHauled`, not `xylos.termsHeard` | Hear Ruva Okh out |
| Rootmarket | `xylos.termsHeard` | Take the river gate to the Braided River: three carriers are on the wrong bank |
| Rootmarket | `xylos.seedthaneGone` | Take the river gate: the Walking Canopy is sitting on the crossing |
| Rootmarket | `xylos.floodcrownGone`, not `xylos.voteHeard` | Tell Ruva Okh the crossing is open |
| Rootmarket | `xylos.voteHeard` | Launch from the landing pool |
| BraidedRiver | not `xylos.sluiceA` | Open the middle sluice on the north bank |
| BraidedRiver | `xylos.sluiceA`, a crew still shut in | Relieve the carrier crews (1/2) |
| BraidedRiver | both relieved, not `xylos.sluiceB` | Open the south sluice: Seedfall is past it |
| BraidedRiver | `xylos.sluiceB`, Seedthane not met | Cross to Seedfall: something is sitting on Marrow-3 |
| BraidedRiver | in the fight | Drive the Seedthane off Marrow-3 |
| BraidedRiver | `bossdown.Seedthane`, not `xylos.seedthaneGone` | Talk to Teo Marrow at Marrow-3 |
| BraidedRiver | `xylos.seedthaneGone` | Go east through the root arches to the Walking Canopy |
| BraidedRiver | `xylos.floodcrownGone` | Take the root bridge back to Rootmarket and tell Ruva Okh |
| WalkingCanopy | not `xylos.anchor1` | Work the anchor root in the root hall |
| WalkingCanopy | `xylos.anchor1` | Follow the flooding gallery to the second anchor root |
| WalkingCanopy | `xylos.anchor2` | Climb to the seed vault and work the last anchor root |
| WalkingCanopy | `xylos.anchor3` | Go down to the basin: something is working the flood gates |
| WalkingCanopy | fight, phase one | Break the flood gates on the basin's rim (1/3) |
| WalkingCanopy | fight, phase two | Drive the Floodcrown out with the water |
| WalkingCanopy | `xylos.floodcrownGone` | Take the root ladder back to the mooring: Ruva owes you a vote |
| any | `chapter3.complete` | Chapter 3 complete. Wendmire's reed weavers are next, and that lane isn't charted yet. |

Chapter-end card (`QuestToast`): heading `CHAPTER 3 COMPLETE`, title `CARRYING A PLACE`, body `Tallow has dinner, and growers aboard who'll vote on where it goes. Three more settlements have asked to follow, and the air aboard is going thick. Hadda says the reed weavers of Wendmire are the ones to ask. Until then nothing closes behind you, so keep exploring.`

## 7. Barks added

Existing entries are untouched. New keys were added to `Taren` and `Sela`, and three speakers are new (`Teo`, `Ruva`, `Jask`).

| Speaker | Key | Used by | Wiring |
|---|---|---|---|
| Taren, Sela | `bossphase.Seedthane`, `bossopening.Seedthane` | `BossController.Situation` | Automatic once the boss's `health.id` is `Seedthane` |
| Taren, Sela | `bossphase.Floodcrown`, `bossopening.Floodcrown` | `BossController.Situation` | Automatic for `Floodcrown`. The opening line says "break a flood gate": play it in phase one only. |
| Taren, Sela | `barge` | A barge comes free | Three lines in turn, none tied to a count. `BarkService.Play(active,"barge",true)`. |
| Taren, Sela | `anchor` | Anchor roots B and C | Three lines in turn. A has the node `SelaCanopyAnchor`. |
| Taren, Sela | `tag` | A claim tag burned off | `DiscoveryPoint.situation = "tag"` |
| Taren, Sela | `pods.red`, `pods.blue`, `pods.yellow` | A spice-pod plant picked | `DiscoveryPoint.situation` per plant. They answer Binna's description in `BinnaBasketsOffer`. |
| Teo | `hail.barge1`, `hail.barge2`, `hail.barge3` | That barge comes free | One `RadioHail` each: `needs` that barge's flag, its own `heardFlag`, a `range` that covers the lane. Each is about that barge, not a running count, so any order works. |
| Teo | `hail.landing` | Three barges free | `RadioHail` needing all three barge flags |
| Ruva | `rope` | The haul | Three lines in turn, one per pull, the last when the island seats |
| Jask | `hail.ferry` | Nearing the mid-river ferry stage | `RadioHail`: `needs xylos.sluiceA`, `silencedBy jask.ferryTalk` |

`barks.json` has no per-zone arrival lines (`PlayArrival` knows only `zone` and `zone.flight`), so none were added.

## 8. What the script assumes

**No new commands or dialogue features.** The rest is wiring, art and tests.

1. **Three portrait sets beyond the brief's six.** `DialogueLayoutTests.EverySpeakerShowsTheirOwnFaceInTheEmotionAsked` wants a set per speaker id, holding every emotion that speaker uses, and fails if two speakers share one. So `Patrol`, `Garo` and `Dabb` each need their own. `Patrol` uses only `Neutral`: a single Compact radio card will pass. Garo and Dabb use four and three emotions.
2. **Tests to extend.** `DialogueInputTests`: add `Ruva`, `Teo`, `Binna`, `Jask`, `Senn`, `Hadda`, `Patrol`, `Garo`, `Dabb` to the speaker-prefix list, and to `branches`: `RuvaRepeat` and `TeoRepeat` {world, world, world}; `BinnaFirst` {shop, world}; `HaddaFirst`, `HaddaRepeat`, `HaddaPost`, `HaddaAir` {bench, repair, shop}. `BinnaRepeat`, `BinnaBasketsOffer`, `BinnaBasketsReturn`, `BinnaAfterBaskets` and `BinnaPost` end in the shop. All five `QuestDef`s must exist before this file is copied in: `startQuest` throws on an unknown id.
3. **Two counters in Rootmarket.** Binna sells what you can chew and sends parts buyers to the press; Hadda's third option is her parts. That wants two inventories under the per-seller lookup now in `ShopUi.For`: `Rootmarket_Binna` and `Rootmarket_Hadda`. With only a `Rootmarket` inventory both open the same stock, which works but makes Binna's line untrue. The panel holds three options and no more (a fourth sits on the dialogue frame at 16:9), so Hadda's three are all services and there is no "leave" row: the press is first, and backing out of it costs nothing.
4. **The rope.** One interaction between `RuvaArrival` and `RuvaTerms` that sets `xylos.ropeHauled`. A `DiscoveryPoint` is enough.
5. **People in more than one place.** Teo: radio in nearspace; by the landing pool from `xylos.termsHeard`; beside Marrow-3 from `bossdown.Seedthane` until the vote; back in town after it. Jask: town ferry stage until the party first enters the Braided River, then the mid-river stage (he is seen waving from the island before Sluice A opens), then nowhere after `xylos.voteHeard`. Garo and Dabb appear when their carrier's encounter is cleared.
6. **Voices from off the spot.** Binna in `RuvaArrival`, Senn in `RuvaVote`, and every radio node. `ConversationStage` already copes with a speaker nobody is standing in for.
7. **Gates.** The Canopy mooring opens on `xylos.seedthaneGone`. `SelaSeedHouse` needs `xylos.termsHeard`. Spice plants, the claim tags, Ulma's ring and Jask's pole are offered only at their quest's step (`requiredQuest`), as Pell's lamp is.
8. **Order.** Talks that follow an event (`SelaCanopyAnchor`, `SelaSeedthaneGone`, `SelaFloodcrownGone`, `AbriDinnerCall`) want `Npc.Say` after it, not a point before it. The two sluice talks are written to work either side of the water dropping.

## 9. Where the script departs from the brief

- **The hauled island is Binna's spice garden**, stripped by the storm. It gives the arrival a named stake and is the reason she has three empty baskets.
- **Senn's wife is Ulma.** The mender took the name Hadda.
- **"Snagvine knots", not "vine anchors"**, in everything spoken and shown (see section 5).
- **Hadda's options have no "leave" row.** Press, repair and parts fill the three the panel holds (section 8, item 3).
- **Jask's third talk is his pole.** The brief has a talk "after the vote" and has him "nowhere to be found" at the vote. The third beat is the pole left at the town ferry stage with a seed tag of numbered questions and one line that isn't a question. Nobody says he has stowed away.
- **The Canopy is already on the crossing** when the party enters (beat 6). The map brief has anchor C "swing the island onto the crossing"; the lines only say the island lurches and the doors change. After the fight Sela says the east channel is running again and a carrier could cross, without saying how, so either staging holds.
- **"Convoy" is first said by Ruva, at the vote.** Abri picks it up in the closing call.
