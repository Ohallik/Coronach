# Nursery joins the chapter

October 4, 2026. **Implementation in validation; C8 remains OPEN.** The frozen
[workshop candidate](C7-candidate.md) is separate and still uses the previous
optional-cave flow. No new ordinary chapter completion is claimed here yet.

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
The [evidence index](C8-nursery-evidence.json) records the retained artifacts.
Development/release builds, the expanded smoke and the fresh
earned-save chapter route remain due.

The chapter route has eleven separately resumable segments, retaining New Game,
real shop/bench/equipment/save menus and defeat/Retry. It adds the actual bore
walk, cave combat, readable pauses on every discovery line, lift return and the
calibrated beacon before continuing to Tallow. These are authored inputs,
not evidence until the runs pass.

Still open beyond this increment: broad flat stretches of Sorrel; the nursery's
overly regular stone ring; the opening walkway repair and Sela's arrival; town
interiors/residents; the Burrower disable finish; Gullet preview/collar release;
and Tallow's moving-refuge story. MAP_EYE_TEST, continuous audiovisual review
and physical-controller feel remain separate from automated success.
