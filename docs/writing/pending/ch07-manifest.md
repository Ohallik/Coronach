# Chapter 7 script manifest - The Cage Makers (Oriel)

For the engineers wiring chapter 7. Script: [Oriel.yarn](Oriel.yarn). Barks: [barks.json](../barks.json). Brief: [ch07-the-cage-makers.md](../../chapters/ch07-the-cage-makers.md). Earlier manifests, in the same shape: [chapter 3](../ch03-manifest.md), [chapter 4](ch04-manifest.md), [chapter 5](ch05-manifest.md), [chapter 6](ch06-manifest.md).

**What is here:** 33 nodes, 147 dialogue lines, 2 option labels (149 strings). Compiled clean with the project's own compiler (Yarn Spinner 3.2.4, the nine live files and the five pending ones together: 0 errors, 395 nodes, no duplicate titles). No new Yarn commands: `setFlag`, `startQuest`, `shop`.

**This file waits in `pending/`** until the chapter's people and quests are defined: two new portrait sets (Lume, the Warden), four `QuestDef`s, and the test lists in section 8.

**It is the shortest script so far on purpose.** The chapter is the quiet one. Where this manifest asks for silence (section 8, item 3) the silence is part of the script.

## 1. Conventions this file follows

- Every title starts with its owning speaker id: `Ishen…`, `Lume…`, `Warden…`, `Idra…`, `Meret…`, the returning `Abri…` (speaker `Keeper`), `Ruva…`, `Pell…`, and `Sela…` for her one line at the Glass Choir.
- Each quest is started by exactly one node. Talks that complete a step set a flag for a `Flag` step; talks at a point offered only at a quest's step (`IdraSealedGarden`, `IdraFarLantern`) set nothing, so the point's `report` reaches its `Interact` step.
- **Ishen walks with the party** from the landing to the reading. He has lines at the junction, the gardens, the clamp and both Warden talks. He is never in a fight.
- **Sela goes quiet.** She has 8 lines in the chapter and 51 words. After the landing, on the main path, she says: "I'm looking." / "This is what a held road looks like." / "We've come to open it." and then nothing until Meret calls. Nobody is given a line to fill the gap. Her barks on the ground are cut to match (section 7).
- **Idra says "Archivist"**, never his name, and "Irun" and "Vosk" as before.
- Checked against the word tests: no retired phrase, no id-shaped word.

### The release (canon from here on)

Cut on the housing of every clamp, in the maker's own hand. Ishen reads it in `IshenFirstClamp`; Taren has already read the same thing off the lock.

| # | The words | What a crew does |
|---|---|---|
| 1 | **Take its weight.** | Hands in the two seats, lean in, until the lock is carrying nothing. Nobody opens a loaded lock. |
| 2 | **Wait for the slack.** | What is held pulls, then eases. Open on the ease. Never against the pull, never forced. |
| 3 | **Let go together.** | Every clamp at once, on one call. Open one corner first and the others tear what they still hold. |

And under those, smaller: **"Then give it room."** It is not a step. It is what you do afterwards. Nobody remarks on it in this chapter; it is there for a later one to pick up.

- The three are always said in these words and this order. Taren says them back as the chapter's second-to-last line; Ishen has them "in four copies", one in the galley.
- Taren's plain version, for anyone who has to explain it: "You lean on a loaded clamp till it's carrying nothing. It pulls and it eases, so you open it on the ease. And all four let go at once."
- Chapter 7: four clamps, each opened in a fight opening by a hold (steps 1 and 2). Each opens onto its catch; the four let go as one when the fourth opens (step 3).
- Chapter 8: the juvenile, with Tovra's smiths taking the weight. Chapter 9: the Halo's joints. Chapter 10: Stillwater's anchor. Meret says in `MeretOrielCall` that her anchor has a release as well (section 9).
- Taren's existing `release` bark, from the Cantor's collar in chapter 1, already reads "Lock's open. Give it room." He has been saying the maker's last line all along. The script does not point at it.

### The Warden's register

Formal Ancelane, as English. Five rules, kept in every line it has:

1. Nobody does anything. Things are held, opened, cleared, recorded, given: the passive, always.
2. No "I", no "you", no names. People are offices. It calls itself "this warden", once.
3. No contractions, no questions, and one adjective in the whole part ("long").
4. Short statements, three or four to a box. An order is "is to be".
5. Courtesy is a formula stated as a fact: "Regret is recorded."

Ishen renders each a beat late and plainly, with "It says" and "It asks". He has only ever seen the language written. Twice the scene moves on before he finishes.

| Node | The Warden | Ishen, a beat late |
|---|---|---|
| `WardenFirst` | The road is held. The road has always been held. State your office. | That's the court speech. I have only ever read it. It says the road is held. It asks your office. What you are for. |
| `WardenFirst` | That is no office. The hall is to be cleared. Regret is recorded. | It says that isn't a job. It says the hall is to be cleared, and that it's sorry. Oh. The hall. That's us. |
| `WardenAsks` | Four clamps are opened. Nothing is broken. State your office. | (Taren answers first.) It's asking your office again. It says nothing broke. Oh. He's answered. |
| `WardenAsks` | Repairs. That office is known. The road is given into repair. | (not rendered) |
| `WardenAsks` | Let it be written that no clamp failed. They were opened. The watch was long. This warden stands down. | It wants it on the record that nothing failed. That they were opened. And that it was a long watch. I'll write it. I'll write it first. |

In the fight it counts, and nothing else: "One clamp is opened." "Two clamps are opened." "Three clamps are opened." (bark `clamp.open`). The fourth is the line that opens `WardenAsks`.

### What the gardener does

It never speaks, has no portrait, and should have no bark and no chirp. Everything the player learns is something one of the four reads off it.

| What it does | Who reads it | Where |
|---|---|---|
| Stands by the wall with a rake and does not move; turns its head once | Idra, then Ishen | `IshenLanding` |
| Others weed and rake a bed of bare stone, carefully | Idra | `IdraEmptyGarden` |
| Follows twelve paces behind; stops when the party stops | Idra, who has counted | `IdraGardenerFollows` |
| Stops ahead for the first time; rake down, hands open at a frayed collar; its light goes low and steady | Taren, Sela ("It's asking."), Ishen a beat late | `IdraGardenerWaits` |
| Its light comes up when a junction is seated; it rakes again | Whoever is deployed | bark `mend` |
| One sweeps a stair that ends in air | Idra | `IdraSweptStair` |
| Walks in front the whole way, without looking back; has waxed a door shut and kept it so; holds out a jar; then follows the jar | Idra, Ishen, Taren | `IdraSealedGarden` |
| Will not cross the Choir's doorstep | Idra | `IdraChoirDoor` |
| Aboard: stands by one bed all night with its light low | Ruva | `RuvaGardener` |

**Low and steady light is a request.** Ishen: "They couldn't give an order. They could only ask." Bright and level is rest. The Warden's mask uses the same low light for its last line (section 5).

### Numbers and names the script fixes

| What | Value | Said by |
|---|---|---|
| Days left, at the start | Thirty-three | Ishen, quoting Abri (`IshenArrives`) |
| Days left, at the end | "Thirty days are left of the forty. I would not plan on all of them." | Meret (`MeretOrielCall`) |
| Ishen's books | Fifty-two, and one change of clothes | Ishen, Abri |
| The gardener's distance | Twelve paces | Idra, every time |
| Lanterns in the chain | 4,012 in Ishen's count (his chapter 2 figure); 4,013 once the far end is lit | Lume (`LumeLanternReturn`) |
| The moons | Nine: the Steward, the Weigher, the Clerk of Roads (the broken one), the Lamplighter, the Reeve, the Second Gardener, the Bellfounder, the Almoner, the Warden ("it rides last, behind all the others") | Ishen: six in `IshenMoonNames`, three by radio (`hail.moon`) |
| The named gates | The Steward's, the Weigher's, the Lamplighter's | Ishen (`IshenMoonsReturn`, `hail.survey`) |
| How long | "A thousand years", for the machine, the seed and the watch | Ishen, Idra |

## 2. Nodes

Lines are dialogue lines (options not counted). "Compulsory" marks the main path; the longest, `IshenReads`, is 9 lines and 45 seconds by the ninety-second test's arithmetic.

### Many Hands, before leaving

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IshenArrives` | 9 | Ishen, Sela, Idra, Taren | Ishen | **Compulsory.** Crossing point on the dock, `chapter6.complete` and `CageMakers` not started. Ishen and a handcart of books. What a Bridle is, in one breath; why Oriel. | `oriel.charted` | `startQuest CageMakers` |
| `AbriTooManyBooks` | 1 | Keeper | | Abri, `oriel.charted` and not `oriel.landed` | | |

### The Orrery (flight; radio)

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IshenMoonNames` | 9 | Ishen, Sela, Idra | Ishen | **Compulsory.** Crossing point short of the first rail-gate. Ishen by radio from the courier pod in Sela's wake. He names six moons; Idra stops him; then he teaches the lights. | *oriel.lightsRead* | |

### Ishen's camp, at the landing

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IshenLanding` | 9 | Ishen, Taren, Idra, Sela | Ishen | **Compulsory.** Crossing point at the landing. Opens on the brief's line. A book slides off his crate and falls sideways at the wall; then the rule is said. A gardener is watching. | `oriel.ruleSeen` | |
| `IshenCamp` | 1 | Ishen | | Ishen at the camp, his repeat until `oriel.coronachHeard` | | |
| `IshenMoonsOffer` | 4 | Ishen, Idra | | Ishen, `oriel.ruleSeen`, `MoonNames` not started | | `startQuest MoonNames` |
| `IshenMoonsReturn` | 3 | Ishen | | Ishen, `MoonNames` at step 1. Reads the same at the camp or aboard. | `ishen.moonsRead` | |
| `LumeFirst` | 5 | Lume, Taren | | Lume at the stall, first node. Options: the stall, or later. | | `shop` (option 1) |
| `LumeRepeat` | 1 | Lume | | Lume's repeat | | `shop` |
| `LumeLanternOffer` | 4 | Lume, Taren | | Lume, met, `LumesLantern` not started | | `startQuest LumesLantern`, `shop` |
| `LumeLanternReturn` | 3 | Lume | | Lume, `LumesLantern` at step 1 | `lume.lanternLit` | `shop` |

### The Skyfields

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IdraGlassForest` | 4 | Taren, Idra, Ishen | Idra | Interaction at the first stand of glass trees. Optional. Taren taps one. | *oriel.forestHeard* | |
| `IdraEmptyGarden` | 5 | Idra, Taren, Ishen | Idra | **Compulsory.** Crossing point beside the first walled garden: two gardeners tending bare stone. | *oriel.gardenSeen* | |
| `IshenJunction` | 6 | Taren, Ishen, Idra | Taren | **Compulsory.** Crossing point at the first junction. Teaches cut and seat, and that either one crosses. | *oriel.junctionRead* | |
| `IdraGardenerFollows` | 5 | Idra, Taren, Ishen | Idra | **Compulsory.** Crossing point two islands on from the first garden. | *oriel.followerSeen* | |
| `IdraGardenerWaits` | 6 | Idra, Taren, Sela, Ishen | Idra | Interaction at the first frayed garden junction, where the gardener has stopped ahead of the party. Optional. It is the quest's giver. | *oriel.gardenerAsked* | `startQuest EmptyGardens` |
| `IdraSweptStair` | 5 | Idra, Taren | Idra | Interaction at a stair that ends in air, a gardener sweeping it. Optional; needs *oriel.gardenSeen*. | *oriel.stairSeen* | |
| `IdraSealedGarden` | 7 | Idra, Ishen, Taren | Idra | Interaction at the sealed garden's door, `EmptyGardens` at step 1 (`report SealedGarden`). The gardener has led the way. | | |
| `IdraFarLantern` | 3 | Taren, Idra | Idra | Interaction at the worn seat on the highest island, `LumesLantern` at step 0 (`report LanternSeat`) | | |
| `IdraChoirDoor` | 2 | Idra, Ishen | Idra | **Compulsory.** Crossing point at the Glass Choir's door. The gardener stops here and sits. | *oriel.doorReached* | |

### The Glass Choir

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `SelaGlassChoir` | 1 | Sela | Sela | **Compulsory.** Crossing point well inside the nave, after the body has been in view for a while and nobody has spoken (section 8, item 3). | `oriel.choirSeen` | |
| `IshenFirstClamp` | 8 | Taren, Ishen, Idra | Taren | **Compulsory.** Crossing point at the first clamp's works, on the way down the body. The brief's line opens it. The release is read here. | `oriel.releaseLearned` | |
| `WardenFirst` | 5 | Warden, Ishen, Sela | Warden | **Compulsory.** As the party enters the engine hall. The fight follows. | *oriel.wardenMet* | |
| `WardenAsks` | 6 | Warden, Taren, Ishen | Warden, then Taren | **Compulsory.** When the fourth clamp opens (`Npc.Say`). It lowers its staff and asks again. Nothing is said after the node closes: the Bridle lets go and the light moves. | `oriel.wardenStoodDown` | |
| `IshenReads` | 9 | Ishen, Taren | Ishen | **Compulsory.** Ishen at the engine hall's record wall, `oriel.wardenStoodDown`. The Coronach is named and explained for the first time in the game. | `oriel.coronachHeard` | |

### The camp, afterwards

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `MeretOrielCall` | 10 | Meret, Sela, Taren | Meret | **Compulsory.** Crossing point on coming back into the camp, `oriel.coronachHeard`. Meret by radio: her portrait, no body. | `oriel.meretHeard` | |
| `LumeBrekksWord` | 7 | Lume, Idra, Taren, Ishen | Lume | **Compulsory, chapter end.** Lume at the last lantern, straight after the call. She reads Brekk's lamp aloud. | `oriel.brekkHeard`, `chapter7.complete` | |
| `LumePost` | 1 | Lume | | Lume's post (`oriel.coronachHeard`) | | `shop` |
| `IshenOrielPost` | 1 | Ishen | | Ishen at the camp, `oriel.coronachHeard` | | |

### Many Hands, coming home

| Node | Lines | Speakers | Carries it | Where and when | Sets | Runs |
|---|---|---|---|---|---|---|
| `IshenAboard` | 2 | Ishen | | Ishen in his corridor, `chapter7.complete` | | |
| `RuvaGardener` | 2 | Ruva | | Ruva, `chapter7.complete` and `quest.EmptyGardens.complete`: it takes the place of her earlier post line | | |
| `IdraGardenerAboard` | 2 | Idra | Idra | Interaction at the seed garden, where the gardener is working a planted bed. Same condition. The side story's last word. | *oriel.gardenerHome* | |
| `PellOrielLantern` | 1 | Pell | | Pell on the lamp gallery, `chapter7.complete` and `quest.LumesLantern.complete` | | |

### Suggested routing (first match wins)

| Speaker | Beats, in order | Npc tiers |
|---|---|---|
| Ishen (camp) | `IshenMoonsReturn`, `IshenMoonsOffer` | repeat `IshenCamp`; post `IshenOrielPost` (`postFlag oriel.coronachHeard`). At the record wall he is the speaker for `IshenReads`. |
| Ishen (aboard) | `IshenMoonsReturn`, `IshenMoonsOffer` | `IshenAboard` |
| Lume | `LumeBrekksWord` (`oriel.meretHeard`, not `chapter7.complete`), `LumeLanternReturn`, `LumeLanternOffer` | first `LumeFirst`; repeat `LumeRepeat`; post `LumePost` |
| Keeper | `AbriTooManyBooks`, then chapter 6's | |
| Ruva | `RuvaGardener`, then her earlier lines | |
| Pell | `PellOrielLantern`, then her earlier lines | |

## 3. Flags

**From the brief, and who sets them:** `oriel.charted` (`IshenArrives`), `oriel.crossed` (world), `oriel.landed` (world), `oriel.ruleSeen` (`IshenLanding`), `oriel.junction1..3` (world), `oriel.choirSeen` (`SelaGlassChoir`), `oriel.releaseLearned` (`IshenFirstClamp`: earlier than the brief's order, see section 9), `oriel.clamp1..4` (world), `oriel.wardenStoodDown` (`WardenAsks`), `oriel.meretHeard` (`MeretOrielCall`), `chapter7.complete` (`LumeBrekksWord`).

**Added by the script:** `oriel.coronachHeard`, `oriel.brekkHeard`, `ishen.moonsRead`, `lume.lanternLit`.

**Suggested for the world to own:** the italic flags in section 2.

## 4. Quests

Kinds are `ObjectiveKind` (TalkTo 0, Reach 1, Kill 2, Collect 3, Interact 4, Flag 5). Rewards are the brief's. `CageMakers` is the main quest (`main: true`, `chapter: 7`).

### CageMakers - The Cage Makers (main)

Description: `The Muster left Hoarfell with the plans for a cage. Ishen says they were first drawn on Oriel, and that the way to open one is there if it is anywhere.`

| # | Kind | Target | Step text | Points at |
|---|---|---|---|---|
| 0 | Reach | `Orrery` | Take the lane to Oriel | Orrery, from ManyHandsHull or the chart |
| 1 | Flag | `oriel.crossed` | Cross the Orrery on its own time: wait in a moon's lee, go on green | Orrery, the far rail-gate |
| 2 | Flag | `oriel.ruleSeen` | Land at Ishen's camp | OrielSkyfields, the landing |
| 3 | Reach | `GlassChoir` | Cross the Skyfields by lift node and junction | OrielSkyfields, the Choir's door |
| 4 | Flag | `oriel.choirSeen` | Walk the nave | GlassChoir |
| 5 | Flag | `oriel.releaseLearned` | Follow the body down to the first clamp | GlassChoir, the first clamp's works |
| 6 | Interact x4 | `Clamp` | Open the four clamps in the engine hall: take the weight, wait for the slack | GlassChoir, report `Clamp`, encounter `LastWarden` |
| 7 | Flag | `oriel.wardenStoodDown` | Answer the Warden | GlassChoir, speaker `Warden` |
| 8 | Flag | `oriel.coronachHeard` | Hear Ishen read the court's record | GlassChoir, speaker `Ishen` |
| 9 | Flag | `oriel.meretHeard` | Go back to the camp | OrielSkyfields, the landing |
| 10 | Flag | `oriel.brekkHeard` | Read the lantern with Lume | OrielSkyfields, speaker `Lume` |

If the clamps set `oriel.clamp1..4` and report nothing, step 6 can be a `Flag` step on `oriel.clamp4`. Reward: XP, scrip, a T4 part choice. The release is not an item: it is three lines the party can say.

### EmptyGardens - The Empty Gardens (the gardener)

Description: `A gardener has followed twelve paces behind since the landing. At a frayed junction, under a garden that was sagging, it put down its rake and held out its hands.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `GardenJunction` | Seat the three frayed garden junctions, and cut none of them | Seat the garden junctions |
| 1 | Interact | `SealedGarden` | Follow the gardener | Follow the gardener |

Reward: a module, and the gardener aboard Many Hands (`RuvaGardener`, `IdraGardenerAboard`). The jar is not an item.

### MoonNames - The Names of the Moons (Ishen)

Description: `Three of the Orrery's rail-gates have their moon's name cut on the keystone, and Ishen has brought a pencil.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact x3 | `NamedGate` | Pass the three named rail-gates on green, slowly enough to be read | Pass the named gates on green |
| 1 | Flag | `ishen.moonsRead` | Let Ishen show you what he took down | Go back to Ishen (waits on `Ishen`) |

Reward: a drive part ("It came off the pod's drive"), and the Orrery's timing chart: every gate's turn is shown ahead of time on return.

### LumesLantern - The Far End (Lume)

Description: `The lantern chain once ended on the highest island of the Skyfields. Lume can't leave the lantern she keeps, so she has lit its twin from it.`

| # | Kind | Target | Step text | Panel brief |
|---|---|---|---|---|
| 0 | Interact | `LanternSeat` | Carry the lantern to the highest island and set it in the old seat | Carry the lantern up |
| 1 | Flag | `lume.lanternLit` | Tell Lume it's lit | Tell Lume it's lit (waits on `Lume`) |

Reward: an emitter part, and a lantern on Many Hands' lamp gallery lit from Oriel (`PellOrielLantern`).

## 5. Display names

### New speakers (`CharacterDef.displayName`)

| Speaker id | Display name |
|---|---|
| `Lume` | Lume |
| `Warden` | The Last Warden |

Ishen, Meret, Idra, Keeper, Ruva and Pell speak on sheets they have or are already owed. Nobody uses an expression new to them: Idra uses five of her six (no Flustered), Meret three of her five.

### Sheets, for the artist

**Lume.** Young Vael woman, eighteen at most, and the driest person at the camp. Crest fronds cropped short and tied back with a length of lamp wick; a keeper's soot-grey smock with the cuffs rolled; a wick-knife; a coil of light line over one shoulder, whose other end is tied to Ishen. Heavy-lidded eyes that have already seen what you are about to do. Her resting face is patience. 16 lines: Neutral 5, Amused 5, Tender 2, Determined 1, Happy 1, Curious 1, Worried 1. Her Amused is no smile, only one eyelid lower than the other. Her Happy (the far lantern answering) is the one real smile on the sheet, and it should look as if it surprised her.

**The Last Warden.** A bronze-and-glass guardian's mask, taller than it is wide, with nothing behind it. A long bronze visor with no mouth; a brow band stamped with the ring and bar; where the eyes would be, one level slot of thick glass with fibre-light behind it. Verdigris in every seam. The glass is the only clean thing on it. **It is one face in every cell.** Only the light changes, and it uses two:

| Cell | Light behind the glass | Lines |
|---|---|---|
| Neutral | White and level, hard-edged. An order. | 4 |
| Tender | Low, amber and steady: the light the gardeners use to ask for something | 1: "Let it be written that no clamp failed. They were opened. The watch was long. This warden stands down." |

Every other cell can repeat Neutral.

**Ishen** has his sheet. 49 lines, the most he has had: Happy 15, Sad 7, Tender 7, Flustered 5, Determined 3, Neutral 3, Shocked 3, Curious 3, Worried 2, Amused 1.

**The gardeners have no sheet.** For the modeller: slight, a head taller than Taren, bronze gone green and glass gone cloudy; a lamp where a face would be; long careful hands that cannot grip a collar. The one that follows carries a rake.

### People named but never speaking

| Name | Who |
|---|---|
| Aumi | Sent Lume; "Aumi will say it's nothing." Her lamp at Lanternwick is the one Brekk works. |
| Brekk | The smith who walked out of the Muster's cage works (chapter 2). His word comes up the lantern chain and Lume reads it aloud: slow, stopping and starting. He has no line and no portrait is shown. |
| the gardener | Never named, never "he" or "she": always "it". |

### Other words the script introduces

| Word | What |
|---|---|
| the Coronach | First said and first explained in `IshenReads`. Lanternwick's word; Taren: "So does the Halo." |
| office | What the Warden asks for. A job, not a name. "Repairs" is one; "We've come to open it" is not. |
| the court, the clerks | Oriel's rulers and their record-keepers. The clerks logged the leave-takings as a lost working day. |
| the release | Section 1 |
| Archivist | What Idra calls Ishen |
| a fibre; a junction; a collar | A severed strand of the old road; where two meet; the fitting that joins them. A junction is cut or seated. |
| the pod's glass | The courier pod's screen, as "the rig's glass" in chapter 6 |
| a lamp line | Idra's word for the lantern chain |
| the keystone | Where a rail-gate carries its moon's name |
| the moons' names | Section 1 |
| Scoria | Where the Muster's foundry is, and the Unsworn who sent word (chapter 8) |

### Enemies and boss (`EnemyDef.displayName`)

| Id | Display | Note |
|---|---|---|
| `RailWarden` | Rail Warden | Spoken of as "the rails" |
| `Moonshard` | Moonshard | Hazard on a marked track |
| `Pruner` | Pruner | Spoken of as "shears" |
| `FibreWisp` | Fibre Wisp | |
| `Glassback` | Glassback | |
| `GlassSentinel` | Glass Sentinel | |
| `ClampTender` | Clamp Tender | "the tender" |
| `LastWarden` | THE LAST WARDEN | Epithet: `Guardian of the first Bridle, a thousand years on watch`. Status in the fight: `THE ROAD IS HELD  ·  OPEN A CLAMP`. Targets: `Bridle clamp`. |

The boss id must be exactly `LastWarden` for its barks to be found.

### Zones (`ZoneDef.displayName`)

| Id | Display |
|---|---|
| `Orrery` | The Orrery |
| `OrielSkyfields` | The Skyfields |
| `GlassChoir` | The Glass Choir |

### Floor signs and landmark labels

Few, because nobody is left to put up a sign.

- The camp: `ISHEN'S CAMP`, `LAST LANTERN`, `LUME'S STALL`
- The Skyfields: `LIFT NODE`, `WALLED GARDEN`, `SEALED GARDEN`, `HIGHEST ISLAND`
- The Glass Choir: `THE NAVE`, `FIRST CLAMP`, `ENGINE HALL`

### Prompts

`Ride the lift node`, `Cut the junction`, `Seat the junction`, `Tap the glass`, `Look at the stair`, `Set the lantern in the seat`, `Go in after the gardener`, `Take the clamp's weight`, `Look at the record wall`.

### Items

The script names two, both existing: Repair Gel and Charge Cells, as Lume's stock. The lantern and the seed jar are carried in the story and are not inventory items.

## 6. Objective strings

Where the panel takes its line from the main quest's current step, section 4's step text is what shows. These are the rows the steps can't say.

| Zone | When | Label |
|---|---|---|
| Orrery | a moon's lights standing amber | Wait in the moon's lee: the gate is shut |
| Orrery | the lights running green | Go on green |
| Orrery | a gate forced | Get off the rail: the Rail Wardens are awake |
| OrielSkyfields | at a junction | Cut the junction or seat it: either one crosses |
| OrielSkyfields | a garden damaged | Leave the garden: the Pruners have turned |
| GlassChoir | the fight, clamps left | Open a clamp while the staff is down (1/4) |
| GlassChoir | a hand on a clamp | Take its weight: hold until the slack |
| GlassChoir | a Clamp Tender at an open clamp | Stop the Clamp Tender: it is closing the clamp |
| any | `chapter7.complete` | Chapter 7 complete. The first Bridle is open, and there is something alive in the Muster's. |

Chapter-end card (`QuestToast`): heading `CHAPTER 7 COMPLETE`, title `THE CAGE MAKERS`, body `Take its weight, wait for the slack, let go together: the first Bridle is open, a thousand years late, and the Last Warden has stood down. Ishen has read where the Coronach comes from. On Scoria the Muster's cage is nearly built, and there is something alive in it. Until then nothing closes behind you, so keep exploring.`

## 7. Barks added

Existing entries are untouched. Every new hero key is there for Taren, Sela and Idra alike.

| Speaker | Keys | Used by | Wiring |
|---|---|---|---|
| Taren, Sela, Idra | `bossphase.LastWarden`, `bossopening.LastWarden` | `BossController.Situation` | Automatic for id `LastWarden`: the hall's lines thin as a clamp opens; the staff comes down |
| Taren, Sela, Idra | `rail` | A moon's lights change in the Orrery | Three lines in turn: green, amber, green |
| Taren, Sela, Idra | `rail.forced` | A rail-gate is forced and the Rail Wardens wake | |
| Taren, Sela, Idra | `moonshard` | A moon sheds onto a marked track | Sela's names the moon |
| Taren, Sela, Idra | `lift` | A lift node carries the party | Three in turn |
| Taren, Sela, Idra | `junction.cut`, `junction.seat` | A junction is cut, or seated | Two in turn each |
| Taren, Sela, Idra | `garden` | A garden is damaged and the Pruners turn | |
| Taren, Sela, Idra | `wisp` | A fibre lights under a Fibre Wisp | |
| Taren, Sela, Idra | `glassback` | A Glassback sheds its plates | |
| Taren, Sela, Idra | `sentinel` | A Glass Sentinel throws a shot back from its front | No damage type is named, as in every earlier bark |
| Taren, Sela, Idra | `tender` | A Clamp Tender starts closing a clamp | |
| Taren, Sela, Idra | `clamp` | A hero puts a hand on a clamp in the fight | Four in turn, one a clamp. They are the drill, not a count: the Warden does the counting. |
| Taren, Sela, Idra | `mend` | A garden junction is seated for `EmptyGardens` | `DiscoveryPoint.situation`; three in turn. They say what the gardener does. |
| Ishen | `hail.moon` | The Orrery, after `IshenMoonNames`: the three names he was not allowed | `RadioHail`, speaker `Ishen`; three in turn, one at each of three later gates |
| Ishen | `hail.survey` | A named gate is passed on green for `MoonNames` | `DiscoveryPoint.situation` or `RadioHail`; three in turn, in the order Steward, Weigher, Lamplighter |
| Lume | `hail.pod` | The Orrery, before `IshenMoonNames`: the courier pod falls in behind Sela | `RadioHail`, speaker `Lume`. Her first line in the game. |
| Warden | `clamp.open` | A clamp opens in the fight | `BarkService.Play("Warden", "clamp.open", true)`; three in turn. The fourth clamp opens `WardenAsks` instead. |

**Sela's ground barks are short on purpose** ("Step on.", "Cut.", "From the side."). In the Orrery, before the landing, she still sounds like herself.

## 8. What the script assumes

**No new commands or dialogue features.**

1. **Two portrait sets.** Lume, and the Warden's mask (one face, two lightings). Section 5 describes both. Idra's two sets and Meret's are owed from chapters 6 and 5 and need nothing added.
2. **Tests to extend.** `DialogueInputTests`: add `Lume` and `Warden` to the speaker-prefix list; to `branches`: `LumeFirst` {shop, world}. `LumeRepeat`, `LumeLanternOffer`, `LumeLanternReturn` and `LumePost` end in the shop. `QaPlanWorldTests`: the quest-starter table gets (`CageMakers`, `IshenArrives`), (`MoonNames`, `IshenMoonsOffer`), (`LumesLantern`, `LumeLanternOffer`), (`EmptyGardens`, `IdraGardenerWaits`); the ninety-second list gets the nodes marked compulsory. `PeopleMarks.Repeats` gets `AbriTooManyBooks`, `IshenCamp`, `IshenOrielPost`, `LumePost`, `IshenAboard`, `RuvaGardener`, `PellOrielLantern`.
3. **Three silences.** They are the chapter.
   - **Entering the Glass Choir.** No `zone` bark (Sela's would be "Nice place. Who broke it?"), no `discovery` bark, no enemy for the first stretch of the nave. The body is in view, and nobody speaks, for a good while before `SelaGlassChoir`: long enough that the player notices nobody has. Then her one line, and nothing after it.
   - **When the Bridle opens.** `WardenAsks` ends on Ishen. Then the four clamps let go, the light moves through the glass, and nobody says so. No `boss` bark, no `release` bark.
   - **The gardener.** No voice, no bark, no chirp, anywhere.
4. **Ishen in the field.** A follower from the landing to the record wall, on Lume's line. In the engine hall he stays on the stair. At the camp and aboard he is an Npc.
5. **The gardener that follows.** From `IshenLanding`: twelve paces behind, stopping when the party stops, never in a fight. It goes ahead once, to the first frayed garden junction, and waits there with its light low until it is spoken to. With all three junctions seated it leads to the sealed garden, in front, without turning round. It sits down at the Choir's door and is still there when the party comes out. If `EmptyGardens` is complete it boards with the party and works the seed garden.
6. **The rule is shown with a book.** One of Ishen's slides off his crate and falls sideways at the wall. A stone does as well if a book is a cost: then Taren's line becomes "Mind that stone. It's sliding."
7. **Garden junctions are not path junctions.** The three for `EmptyGardens` are their own frayed collars under sagging gardens, offered only at the quest's step 0 and seat-only, so the quest cannot be lost by cutting a path junction (`oriel.junction1..3`). `IshenJunction` says only that seating "keeps whatever is on top of it".
8. **The clamps in the fight.** A hold is steps 1 and 2 of the release: a hand in the seat, and a pull that eases. Each clamp opens onto its catch and re-routes the hall as the brief has it. The Bridle as a whole lets go only when the fourth opens. That is "Let go together", and it is what the Warden sees.
9. **Order in the engine hall.** `WardenFirst`, the fight, the Warden's three counts, `WardenAsks` at the fourth clamp, the silence, then Ishen walks to the wall for `IshenReads`.
10. **Meret is a voice.** `MeretOrielCall` is a dialogue with her portrait, not a hail. `LumeBrekksWord` follows it at once: Lume calls the party to the last lantern.
11. **One shop**, Lume's, under the zone's own inventory. The bench and the repair cradle are world objects at the camp, with no speaker.
12. **If the side quests are left undone**, nothing in the main path mentions them. The gardener stays at the Choir's door. Lume's chain still carries Brekk's word, from her own lantern.

## 9. Where the script departs from the brief

- **The release is found before the fight, and nobody has to work it out.** It is cut on every clamp by the person who built it. Taren reads it off the lock ("There's a seat for a hand here, and here. Nobody cuts those into a thing they mean to keep shut.") while Ishen is still translating. So `oriel.releaseLearned` is set at the first clamp, ahead of `oriel.clamp1..4` and not after the Warden as the brief's flag order has it. The fight is the party doing it; the reading afterwards is about something else.
- **Three steps and one line more.** "Then give it room." is on the housing, smaller, and nobody comments on it. Strike it if later chapters have no use for it; nothing else leans on it.
- **A book, not a stone** (section 8, item 6).
- **Nobody speaks when the Bridle opens.** The brief has the light move; the script leaves that to the screen.
- **The Warden has five lines and a count of three.** "Regret is recorded." is its courtesy and "The watch was long." is its exhaustion. It asks for one thing, that the record show nothing failed, and Ishen promises to write that first.
- **Taren can answer because he has heard the question before.** Ishen translates "State your office" the first time; the second time Taren says "Repairs." before Ishen has started.
- **Ishen reads the court's own record**, not a memorial. The clerks wrote the leave-takings down as a lost working day and took a few of the things people said at doors. That is how the Coronach is explained without ceremony. Taren recognises it from the Halo ("I thought it was only what you did."), which is chapter 9's handle.
- **Meret says her anchor has a release as well.** "I would not draw a lock I couldn't open." Sela: "Neither did they." Meret: "No. They didn't. That is the page I reread." This is where Sela's half-agreement is seen to be over, in three words, and Meret concedes the point without changing her mind. It also makes chapter 10's release on Stillwater's anchor something Meret drew. If the anchor must not have one, cut those three lines and Taren's before them; the call still stands.
- **The day count** is thirty-three at the start and thirty at the end.
- **Lume flies the courier pod**, and is heard once by radio before she is met at the camp.
- **Brekk has no line.** His word comes up the chain in lamp flashes and Lume reads it. One line says who he is, for players who never fed him in chapter 2.
- **Two flags added to the main path**: `oriel.coronachHeard` for the reading, `oriel.brekkHeard` for the last step.
- **The gardener's quest is given by a talk**, since it cannot give one. It ends with a jar of seed a thousand years old that will not grow, which the gardener then follows aboard. Ruva plants one. The script never says whether it comes up.
- **Three talks the brief does not list**: `IdraGlassForest` (Idra and anything old and clean), `IdraSweptStair` (why she can't shake the gardeners, and her count of Sela's words), `IdraChoirDoor` (where the gardener stops).
- **Nine moons are named**, the ninth the Warden. Three have gates the side quest reads.
