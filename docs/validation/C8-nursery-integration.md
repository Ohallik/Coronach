# Nursery joins the chapter

October 4, 2026. **Connected ordinary progression PASS; C8 remains OPEN.** The frozen
[workshop candidate](C7-candidate.md) is separate and still uses the previous
optional-cave flow. The new chapter runtime is `e989a3b` in the release/development
build folders; the frozen workshop has not been replaced.

Fresh profiles now need the nursery chart and the recovered survey key before
the Halo beacon admits them to the Gullet. Sorrel's objective leads a key owner
to the bore, then back to the Halo after discovery. Orrin's missing deliveries,
the Survivor's deliberate transmitter shutdown and Sela's moving chart connect
the places. The bore still opens after the Burrower; the conversation works even
if the player explores it before collecting the key.

The seven-line Taren/Sela discovery uses the normal dialogue panel and input.
Its completion callback grants the existing discovery flag, quest reward and
autosave only after successful completion in the same loaded state. Internal
cancellation, scene unload or replacing the state leaves it unclaimed. Back
and Pause retain their C6 rule: they preserve a pending story conversation.
The lift and the Survivor's existing nursery response remain available.

Format version 2 makes compatibility explicit. A version-1 key owner retains
earned access through `legacy.gulletAccess`, without a fictional nursery visit
or duplicate reward. A pre-key save receives no such privilege. Loading migrates
only in memory; saving remains an ordinary explicit/autosave action. Missing,
text and unsupported versions are rejected. The workshop launcher uses its own
profile, so its older runtime cannot accidentally load a chapter test save.

`hushwell` and `gullet` development loadouts now represent separate progression
points. The cave fixture retains its boss and discovery. The tunnel fixture has
completed the cave. The ordinary chapter routes use neither fixture.

Broken-state evidence under `Builds/quality/C8/nursery-regressions/`:

| Check | Rejected state |
| --- | --- |
| Fresh routing, gated beacon, deferred discovery, cancellation/unload/state ownership, distinct fixtures | All eight chapter tests fail on the original runtime |
| Format and migration, six old-save locations | Eleven original failures; the four unsupported-version cases already passed |
| Unsupported versions | Deliberately accepting them fails all four rejection cases |
| No write while loading | A deliberate whitespace write fails byte preservation in all six locations |
| No completion after cancel, unload or state replacement | Each deliberate fault fails its intended assertion |
| Completed discovery stays unavailable | Removing its flag guard fails the repeat-discovery check |
| Slice includes the cave | The old 24-marker slice log fails the new 30-marker contract at Hushwell entry |
| Exact continuation across supported formats | The old helper rejects version 2 and accepts `true`/`1.0` as version 1; both new controls fail before its update |

The first migration implementation reformatted timestamps through automatic JSON
date parsing; six cases rejected it. Date parsing is now disabled for this
read. The first integrated run exposed a hidden base `OnDisable`: destroyed
discovery points remained registered as interaction prompts. The override now
calls base cleanup. Both rejected implementations and their logs are retained.
The initial test compilation rejection was a missing explicit Newtonsoft DLL
reference in the test assembly, not game behavior. A first write-during-load
control was caught by timestamp preservation before the byte comparison; a
second control isolates the byte rule without changing parsed values.

Current checks: **15/15 migration**, **31/31 combined chapter/dialogue/layout**,
then **84/84 full EditMode**, all with zero skips. The compiled story inventory
has **29 nodes / 36 outcomes**, exercised through rapid ordinary confirms and
save/reload. Six deliberate mutations are rejected and exact source bytes are
restored after each. All **115 offline checks** pass, including ten continuation
controls. That helper accepts integer versions 1 and 2 without migration or
formatting changes, and rejects other types/versions before copying. The final
full PlayMode suite passes **226/226**, zero skipped, with `-TimeoutSec 3600`.
Nathan's two original save files are restored with exact baseline hashes.
The [evidence index](C8-nursery-evidence.json) records 1,242 retained artifacts,
including accepted and rejected runs. The suites precede the subsequent
development-smoke-only lift approach correction; both builds and the full smoke
validate that correction.

Both player builds succeed from `e0c290e`. The first expanded smoke clears the
Burrower, both cave organs and the Bellows, then completes the nursery conversation
and reward. It is retained as a rejection in `Builds/quality/C8/slice-01`: the
development driver tries to path to the lift's elevated pivot, outside its floor
sampling radius. The corrected approach uses the ordinary route's walkable stance
before interacting. Both players rebuild from `e989a3b`; the repeat in `slice-02`
passes all 30 ordered markers in **654.7 seconds**, reaching Tallow at level seven.
No gameplay or assertion is relaxed.

The chapter route has eleven separately resumable segments, retaining New Game,
real shop/bench/equipment/save menus and defeat/Retry. It adds the actual bore
walk, cave combat, readable pauses on every discovery line, lift return and the
calibrated beacon before continuing to Tallow. All eleven accepted release runs
pass runtime and offline checks: **2,317.642 seconds (38:37), 138,906/138,906
focused frames**. Each continuation copies exact slots from an accepted predecessor.
No development loadout, health injection or gameplay-state edit is used. These
captures include loading, dialogue and intentional menu pauses; they are not clean
performance evidence.

| Segment / evidence directory under `Builds/quality/C8/` | Seconds | Focused frames |
| --- | ---: | ---: |
| `chapter-01` — New Game, Decks, shop/bench/equipment/save | 102.879 | 6,158 |
| `chapter-02` — Continue, Sorrel, Survivor, repair | 132.495 | 7,936 |
| `chapter-03` — defeat and Retry | 61.622 | 3,685 |
| `chapter-04` — haul combat, mining and return | 231.121 | 13,853 |
| `chapter-05` — drill bench, gear and manual save | 111.706 | 6,689 |
| `chapter-06` — Burrower and survey key | 186.039 | 11,149 |
| `chapter-07-resupply-02` — earned gels and actual bore entry | 163.108 | 9,773 |
| `chapter-08-prepared` — cave, discovery and lift | 447.515 | 26,836 |
| `chapter-09` — Continue, launch and calibrated beacon | 43.894 | 2,620 |
| `chapter-10` — full Gullet, gate, coil, Cantor and exit | 789.971 | 47,382 |
| `chapter-11` — Tallow docking, Keeper, repair and save | 47.292 | 2,825 |

Retain the first `chapter-08` rejection: the inherited cave fixture route entered
with only two earned gels, lost Taren in chamber three and waited away from his
body instead of approaching to revive. The capture shows the live revival hint.
The resulting missing organs/discovery are downstream failures, not separate
story bugs. The prepared route crafts twelve gels from 24 of the party's 33 earned
scrap and uses the existing ordinary dodge inputs during regular encounters.
The first resupply run also rejects a waypoint through the anvil's footprint;
the corrected run returns through the proven west lane. No rejected save is used
as a continuation source, and outcome assertions remain unchanged.

The accepted cave run uses eight gels, defeats both organs and the Bellows, and
returns both heroes alive. Every discovery line has a 3.5-second readable pause.
Opened captures `114`, `118` and `134` show fitting text and the intended speaker
and emotion portraits. The final version-2 Tallow autosave has nursery, Bellows,
Cantor and slice completion, both heroes level seven with 228 integrity/100 charge,
six gels remaining, and no `legacy.gulletAccess`. Nathan's two original save
hashes and file count match the baseline after the whole chain.

Still open beyond this increment: broad flat stretches of Sorrel; the nursery's
overly regular stone ring; the opening walkway repair and Sela's arrival; town
interiors/residents; the Burrower disable finish; Gullet preview/collar release;
and Tallow's moving-refuge story. MAP_EYE_TEST, continuous audiovisual review
and physical-controller feel remain separate from automated success.
