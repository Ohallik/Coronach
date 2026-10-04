# C6 dialogue input — October 3, Codex

**Focused automated input coverage passes, 11/11; full integrated suite and workshop play review remain due.** This complements [layout, portraits and readable type](C6-dialogue-layout.md). It does not substitute for physical-controller, OS focus/disconnect or continuous editorial review.

## Exercised behavior

`DialogueInputTests` loads the real project, panel, Yarn runner and EventSystem. `AutoAdvance` is off. All reveals, advances and selections use queued virtual keyboard/gamepad state through the ordinary Input System; tests do not invoke button callbacks or presenter selection callbacks. Test setup may choose a node or load a state directly, so this is branch/input evidence, not an ordinary map-traversal claim.

The compiled project supplies the node inventory. All **28 story nodes / 35 node-choice outcomes** complete under rapid input: Orrin, Mira, Hal, Neve, Survivor, Keeper, Oda, Ilo, Sela's waiting exchange, the arena guide and the recorded cache. Every branch saves and reloads state before and after, comparing flags, inventory, quest steps/counts, money and party IDs. Shop, bench, repair and world outcomes are asserted separately. The internal runtime-preparation node is excluded from story coverage.

- A tap during typewriter reveal shows the current line; a new press advances. Holding confirm does not cross into the next line. Each rapid press advances at most one line.
- Back and Pause preserve a story line/choice. Explicit leave options finish the conversation. Moving from gamepad to keyboard and back updates hints without consuming a line.
- Unavailable options are omitted; the selected result keeps its original Yarn option ID. All-unavailable returns no selection and creates no focus target.
- Losing UI selection at a choice recovers an active option through navigation. This is **UI selection loss**, not an OS window-focus test.
- Holding the option-confirm through shop/bench handoff neither activates the next UI nor purchases/crafts anything. The new UI has valid focus. Closing with Back clears the UI but suppresses the same held button from world dodge until release.
- Orrin adds exactly one Sela in both saved state and the live party; reload/repeat does not add another. Hal's 12 Alloy / 3 Filament gift persists and is not duplicated. Mira recognizes crystals collected before accepting her job, awards once, and never repeats scrip/XP after reload and revisit.
- Internal cancellation selects no branch, closes the panel, unblocks input, emits no shop/bench request, and grants no completion-only Survivor quest credit. Its pending line/option tasks finish before Yarn releases its cancellation state.

These tests use isolated temporary slots. The outer PlayMode runner also stashes/restores Nathan's save directory. Synthetic devices and temporary editor input settings are restored in teardown, including when a test fails.

## Defects found and repaired

1. `OptionsPresenter` returned option zero when its wait was cancelled. `first-red` caught a non-null choice. Returning null removes implicit consent.
2. Returning null exposed a Yarn completion race: `Stop()` disposed the cancellation state before the options continuation returned, which then tried selecting in a stopped VM. `yarn-stop-red` reproduces the exception. Completion now waits for the pending presenter task and its continuation.
3. An aborted Survivor line still reported `TalkTo` and advanced the Sorrel quest. `cancel-quest-red` shows step 1 becoming 2. Interrupted conversations now discard deferred joins/UI requests and skip completion-only quest credit.
4. The integrated rerun finished **200/203**, with no skipped tests. Its three failures expose completion/unload integration: `IloRepeat` starts against the preceding runner's completion state and hangs; the bench test sees the VM stopped before its deferred UI command; an unloading nursery conversation tries hiding a destroyed panel. The preserved full result is `Builds/quality/workshop/biomes/regressions/integrated-203-playmode.{xml,log}`. Dialogue ownership now stays active through presenter and command completion, and presenters stop touching an unloaded view. Focused rerun results follow; the full workshop gate remains due.

The keyboard test initially failed because headless Unity routed virtual keyboard events away from the unfocused Game view. The fixture now explicitly routes test input to the Game view and restores that setting afterwards; it asserts the virtual key actually arrived. No prompt-service workaround was added. The first quest-reward fixture also gave inventory without the normal collection report; it now exercises the real supported case of accepting Mira's quest with previously collected crystals. Neither fixture failure is presented as a game fix.

## Negative controls and evidence

[SHA-256 index of the preserved red/green logs and XML](C6-input-evidence.json).

Under `Builds/quality/C6/input-regressions/`, each deliberate mutation has raw XML, Unity log and launcher output. The harness restored the exact original source bytes after each run.

| Deliberately broken behavior | Required rejection |
|---|---|
| Treat held confirm as a new press | First line advances during reveal/hold |
| Disable device watching | Hint remains on wrong device |
| Include unavailable options | Three buttons instead of two |
| Include all-unavailable options | Wait never returns no-choice |
| Remove closing-button suppression | Back leaks into world dodge |
| Suppress deferred joins | Sela/party count stays one |
| Route bench request to shop | ArenaGuide branch opens wrong UI |
| Suppress quest scrip reward | 120 instead of 220 scrip |
| Disable UI selection recovery | No usable selected option |

All nine controls fail their intended assertion. The actual cancellation defects above provide the other red cases. `eleven-green` is the first clean result; `final-eleven-green` is the clean rerun after all mutations. Both contain 35 `C6_BRANCH_OK` outcomes. Build hashes and the later full-suite result belong to the integrated workshop report.

After the full-suite completion/unload failures, the combined Cinder resident,
core, Hushwell and C6 input rerun passes **44/44**, zero skipped, in 230.2 seconds.
`completion-integration-44-green.{xml,log}` includes each previously failed case
and all eleven input tests / 35 story outcomes. No assertion was weakened. The
complete suite now passes **213/213**, zero skipped, on workshop source `ee24597`
(October 4). All completion/unload cases pass in that full run; the original save
hashes match after restoration. [Integrated evidence](C7-integrated-tests-evidence.json).

## Remaining evidence

The ordinary route captures cover speaker interactions in their maps, but do not yet replace a single New Game-to-Tallow workshop playthrough. OS window-focus loss, physical disconnect/reconnect, physical Logitech comfort, and continuous audiovisual/editorial review remain **UNVERIFIED**. No claim of Nathan approval or physical validation is made. A controller-window replay still rejects every focus loss.
