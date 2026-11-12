# Coronach - shipping plan (D223, November 11, 2026)

Nathan's instruction (November 11): review everything that would make this a game someone pays for, decide the best way to finish it, and finish it without check-ins. The only thing left at the end should be new music tracks, handed over as melody seeds. This file is the plan that instruction produced. It supersedes the scale of [WORLD_ATLAS.md](WORLD_ATLAS.md) (52 maps, 40-46 hours) and keeps its story, peoples and bosses. [STORY_CAMPAIGN.md](STORY_CAMPAIGN.md) remains the story bible.

## 1. Where the game stood

| Area | State on November 11 | Verdict |
|---|---|---|
| Length | Chapters 1-2 of a six-movement story: 13 scenes, 4 bosses, about an hour for the replay driver | A long demo. No ending. |
| Story | 625 lines of dialogue in a good, consistent voice; Meret Venn has not appeared | Needs the rest of the story, and Meret on screen |
| Combat | Fair and readable after the QA pass; no hit-stop, no camera response to a hit, no EXTRA move | Needs weight |
| Progression | 13 parts, 1 affix, 2 consumables, 16 skills ending at Sync 12 of 40 | Far too thin for "tech parts are the loot" |
| Places | Chapter 1 is dressed; chapter 2's canyon floors are sparse; towns have few residents | Needs density and life |
| Party | Taren and Sela | The pillar says up to four |

## 2. The shape of the finished game

Ten chapters in the atlas's six movements. Every region Nathan asked for on September 28 (the war-state and its forge world, the ancient town, the ancient near-magical planet, ice and swamp worlds with villages, the moving twilight town, distinctive space areas) is kept, each as one compact chapter: **a space approach with its own rule, a settlement, one combat map or dungeon, one boss.** Veyr, Merrow, Pale Exchange, the optional destinations and a fourth hero are cut; their story jobs are moved (see section 4). Target: 10-12 hours for a first play, Sync 1 to about 30.

| Ch | Title | Places (new scenes in bold) | Boss | Sync out | Music (existing tracks) |
|---|---|---|---|---|---|
| 1 | A Repair With Nobody Home | Cinder Halo, the Decks, Sorrel, Hushwell, the Gullet, Tallow | Burrower, Bellows, Cantor | 7 | built |
| 2 | The Lantern Road | Wick Reach, Lanternwick, Lantern Gorge, Sunken Relay | Hessa Korl | 10 | built |
| 3 | Carrying a Place | **XylosNearspace**, **Rootmarket**, **BraidedRiver**, **WalkingCanopy** | Seedthane (mini), Floodcrown | 13 | Starfield Frontier, Seed Shakers of Xylos, Jungle Planet Groove, Riverbed Stomp, Shop Theme |
| 4 | Breathing Room | **SporeVeil**, **Driftreed**, **DrownedWaystation** | Rotbloom | 15 | Starfield Frontier, Despano Lopo, Swamp Sprint |
| 5 | Many Hands | **ManyHands** (hub ship), **ManyHandsHull** (flight arena) | Hulljack (flight, then ground) | 16 | Starward Journey, Glitch Boss, Regal Alien Town (Meret) |
| 6 | The Long Sleep | **AuroraShear**, **Lastlight**, **FrozenHost** | Host Marshal | 19 | Starfield Frontier, Village Circle, March of the Frozen Host, Boss of the Ages |
| 7 | The Cage Makers | **Orrery**, **OrielSkyfields**, **GlassChoir** | The Last Warden | 22 | Glass Ruans, Boss of the Ages |
| 8 | Harrowgate | **PicketOfTeeth**, **ScoriaFlows**, **BridleFoundry**, **Harrowgate** | Slag Colossus, Warmaster Drask | 25 | Galactic Conquest, Starfight, Alien Pulse, Glitch Boss, Alien Boss Battle |
| 9 | The House That Can Leave | **NacreGrave**, **MeridianCrawl**, **Terminator**, the Decks on separation day, **SeparationWorks**, **BrokenRing** | Foundry Saint, Debris Cutter | 27 | Endless Flight, Dventure at Dusk, Hub Town Groove, Glitch Boss, Starfight |
| 10 | A Route With Room To Turn Back | **SorrelSecondSky**, **LastMooring**, **Continuance**, **StillwaterChamber**, **ReturningPassage**, **NeighbourhoodAfloat** | Mother of Doors, Tollkeeper, Meret Venn | 30 | Adventure Awaits, Boss of the Ages, Glitch Boss, Final Stand, Endless Flight, Title Theme (credits) |

That is 30 new scenes and 13 new boss fights on top of 13 scenes and 4 bosses. Chapters are built in story order, each finished (played, looked at, tested, built, committed, pushed) before the next. **If the run has to stop early, chapters 9 and 10 are never the ones cut**: fold a middle chapter's job into its neighbour first (4 into 5, 7 into 8).

Music with no existing fit is listed as it is found and becomes melody seeds at the end (expected: the Coronach leave-taking, the launch, Last Mooring, the Continuance, the epilogue).

## 3. Systems pass (before and alongside chapter 3)

| Stream | What it adds | Done when |
|---|---|---|
| S1 Impact | Hit-stop by hit weight, camera impulse, victim flash and recoil, knockback on light enemies, heavier finishers, kill and break bursts, layered hit sounds, a damage vignette; a Settings toggle for screen shake | A swing reads as landing in a capture; tests and the replay chain still pass |
| S2 Loot | Parts in every slot through the tiers, affixes that change how you play, upgrades +1 to +5 at benches, consumable tiers with carry caps, region materials and recipes, a shop in every settlement, scrip sinks | Every chapter hands the player a choice of build |
| S3 Skills | Three more skills per hero (Sync 16, 20, 24) and an EXTRA move per hero on its own meter | The Skills page keeps offering something through Sync 24 |
| S4 Route chart | Sela's moving chart as a screen: the regions reached, the current objective's region, fast travel between settlements already visited | A player can go back to any town for its shop and side stories |
| S5 Life and density | Residents on walking loops in every town, chapter 2's bare floors dressed, uphill gait | No screen reads as an empty plane |
| S6 Third hero | Idra Sorn: natural body, cutter frame, flight form, two portrait sheets, eight skills, an EXTRA move; party of three with two deployed | She plays as differently from Taren and Sela as they do from each other |

## 4. Story changes that follow from the cuts

- **Idra Sorn** (was Idra Pell; the name clash with Lanternwick's Pell is settled in Pell's favour). A Rauk rescue officer from the Veyr frontier guard, tracking Muster raiders. She joins at Hoarfell (chapter 6) after an operation run on her plan. Veyr itself is spoken of, not visited: her crews' cutters bring the engines the convoy needs. The Rauk don't shape Lattice: she wears a **cutter frame**, a rescue rig that unfolds around her. Heavy brace-and-counter fighter; broad rescue-cutter flight form with a tow line.
- **Oru Mere and Merrow** are cut. The "whose homes does the chart omit" thread stays with Ilo (the Compact surveyor on the Halo) and with Oda's line-904 berth: Ilo defects to the convoy in chapter 5 with the footnotes.
- **Taren's route lock** (the exclusionary lock he installed) moves to the Vaskar border: it is a Compact lock on the Picket approach, and Idra directs its removal in chapter 8.
- **Pale Exchange** is cut. The Nacre disaster (caused by Drask's standing order) opens chapter 9 at Nacre Grave, where the party pulls Oda's hauler out; the navigators rescued there, with the Wick-folk lantern chain, become the convoy's distributed navigation.
- **Chapter 2's cast travels.** Pell keeps Tallow's lamps; Ishen comes to Oriel to read the Bridle plans; Brekk leads the party to the Unsworn on Scoria; Hessa Korl is seen again at Harrowgate.
- **Meret Venn** is heard from chapter 3 (Compact notices and a rescue patrol's radio), met in chapter 5, argued with in chapters 6-8 over the radio and in person, and fought in chapter 10.

## 5. Chapter briefs

Each chapter gets a full brief (map sketches, encounter plan, cast and script outline, asset list) written when it starts. These are the fixed points.

**3 Carrying a Place - Xylos.** Tallow's cabins need food. The river growers of Rootmarket won't sell a bulk harvest that would uproot their farms; a storm has stranded their seed carriers on the wrong bank. Space rule: *moving cargo* (drifting seed barges are cover, and snagged ones must be cut free). The Braided River's sluices change which channels can be waded. The Walking Canopy is a storm-driven root island whose exits change between three anchor points. Floodcrown is fought across three rooted arenas and leaves with the draining water. Outcome: growers join as partners with a vote, and their seed garden goes aboard. First Compact rescue patrol heard on the radio.

**4 Breathing Room - Wendmire.** The growing convoy can't keep its air and water clean. Space rule: *visibility is the resource* (fly dark between Driftreed's pulse beacons; fire and boost attract spore swarms). Driftreed is a woven raft village; Maru Wend has never lived on anything that stays still. Rotbloom, a predator whose lures copy Driftreed's lamps, is killed in a half-sunk Ancelane waystation that drains in stages. Outcome: Driftreed joins as a floating district that keeps its rafts; reed air-mats.

**5 Many Hands.** The convoy becomes a town: a ship assembled from mismatched vessels, with the garden, the raft bay, a galley, every shop, and people from each chapter so far. Outsiders ask to join. Hulljack's salvage crew attacks under a seizure claim: sever its magnetic feet in flight, then fight the exposed operator in the freight hall. A hull breach; Meret's patrol saves a residential section before the party can. Dinner in the galley: she explains Stillwater in practical terms and Sela half agrees. **Act turn:** the expendable moon is Sorrel. Meret hands over the drawings and asks for a better tested plan before her deadline. Ilo brings the footnotes.

**6 The Long Sleep - Hoarfell.** The nursery will have to cross a cold passage. Space rule: *current-riding* (aurora currents give speed one way, drag and Thrust drain the other). Lastlight is a village inside the ribcage of a frozen adult Choir. The Muster is thawing an Ancelane army rank by rank. Idra Sorn's operation: the party follows her plan and she joins. Host Marshal is put back to sleep by breaking the thaw rig's couplings. Old Tern stays; her herders travel.

**7 The Cage Makers - Oriel.** The Bridles come from Oriel's plans. Space rule: *one readable machine* (moons on light-rails, rings that open in sequence). On the Skyfields, weight and light follow the live route fibres. The Glass Choir is a cathedral around the adult Choir the first Bridle killed. The Last Warden's attacks follow the fibre lines and it stands down once shown the release. Ishen reads the plans. Outcome: the release principle, as a thing crews can do with their hands.

**8 Harrowgate - the Vaskar Reach.** The Muster cages a route toward Sorrel. Space rule: *staged detection* (warning, hunter wing, carrier). Scoria's lava crust cycles by colour; the Unsworn camps are the safe pocket and Tovra Kesh their forewoman. The Bridle Foundry's Slag Colossus is cooled, broken and destroyed, and a caged juvenile is freed with Oriel's release. At Harrowgate, a town where people live ordinary lives inside an institution of war, Drask is fought on the muster field and then in flight over the war harbour; the Unsworn depose him. **Act turn:** his standing order bridles the Nacre route. The thrashing Choir wrecks the Nacre berth, and Meret launches Stillwater early.

**9 The House That Can Leave.** Nacre Grave: pull Oda's hauler from the wrecks ("if that door jams, find me"). Duskline: the Meridian Crawl, a town on treads that has never stopped, and a run against the dawn to recover its stalled tread section; its engineers come home with the party. Cinder Halo: Meret offers a guaranteed berth if the Halo cuts the convoy loose, and the town meeting is short (Hal asks how many apprentices the berth includes; Mira demands the manifest). The party reopens the station's separation joints; the Foundry Saint, the old separation press under a Compact override, tries to weld the districts back. **The Coronach:** an unhurried walk through the town while neighbours say what the place did for them. Then the launch, and the Debris Cutter in the Broken Ring.

**10 A Route With Room To Turn Back.** Sorrel's second sky: Mother of Doors is freed, not killed, by opening the clamps around the nursery. Last Mooring: the Tollkeeper's cage. The Continuance: every ally has a visible job (Lastlight's herders keep the young Choir dormant; Driftreed's mats keep a breached section breathing; the Unsworn's release gear opens the anchor; the Crawl's engineers keep the districts together; the lantern chain regroups the fleet). Meret is fought ship to ship, on the moving exterior, and in her emergency frame, with a checkpoint between phases and no fourth health bar. Then the Returning Passage: escort the rear, turn back for a disabled tug, make the rendezvous. Epilogue: Neighbourhood Afloat, where the fruit seller has moved her stall across Taren's safety line again, under an unfamiliar sky. Credits.

## 6. How it is built

- **One lead, parallel streams.** The lead owns the plan, Unity (only one Unity may own `Lattice/`), integration and every look review. Streams own files, not topics: a writer (`docs/writing/*.yarn`), a world builder (the region's builder file), a combat engineer (the chapter's enemies and boss), a systems engineer, and an art runner (Meshy, one task at a time). Streams type-check with `python tools/typecheck.py` and never start Unity. [CHAPTER_GUIDE.md](CHAPTER_GUIDE.md) is the how-to.
- **A chapter is done when** it plays from its first scene to its last on the replay driver, its captures have been looked at frame by frame and fixed, its focused tests pass, the earlier chapters' chain still passes, and a build exists. The full suite and the whole-game chain run at chapters 5 and 8 and at the end.
- **Greybox first.** Each map is walked in blockout (routes, sightlines, travel time, encounters) before its generated kit is ordered. Meshy balance at the start: 2,372.
- **The last pass** is a whole-game QA in the manner of [TEST_PLAN.md](TEST_PLAN.md): play it through, look at all of it, fix what is found, rebalance from the chain, then the music gap list and its melody seeds.

## 7. Progress

| Item | State (November 12, 02:00) |
|---|---|
| November 10 open-issue work | landed (`ff99a7a`): 28 fixed, 6 much better, 5 open |
| Briefs and scripts for chapters 3-10 | written and committed (`docs/chapters/`, `docs/writing/`, `docs/writing/pending/`; about 1,900 lines of dialogue) |
| Portrait sheets | approved for every speaker in chapters 3-10 |
| Music | thirteen existing tracks placed; six new cues wait on Suno (`docs/music/NEW_TRACKS.md`) |
| S1 Impact (hit weight, hit-stop, camera, flash, knockback, 19 sounds, settings) | built, its tests pass; uncommitted |
| S2 Loot (affixes, five tiers, modules, consumables, per-seller stalls, caches) | built, its tests pass; uncommitted |
| S3 Skills and EXTRA moves | built; four regressions with the skills engineer; uncommitted |
| S4 Route chart, difficulty, chapter cards | built, tests pass; uncommitted |
| S5 Ground kit (D246) | Lantern Gorge, Lanternwick, Sorrel done; Hushwell 10 of 17 stills; uncommitted |
| S5 Town life, uphill gait | not started |
| S6 Third hero (Idra) | bodies and portraits in; kit and party-of-three machinery in progress |
| Framework (content by data, test book, scaffold, replay driver's new hands) | built; two ReplayHands failures open; uncommitted |
| Test mirrors (D247) | committed (`035bd75`); suite `nov12a` running on a frozen copy |
| Chapter 3 | combat written (eight enemies, two bosses); approach and town built, look pass sent back; river and Canopy in progress; tests and routes being written |
| Chapter 4 | art in; three engineers started (space and town, dungeon, combat) |
| Chapters 5-6 | art in (20 pieces, six portrait sets); not started |
| Chapters 7-10 | art ordered; not started |

Meshy ledger: 2,372 at the start; 891 left after chapters 3-6, Idra and every portrait sheet; chapters 7-10 are capped at 720 with 45 more allowed for chapter 3's space backdrop.
