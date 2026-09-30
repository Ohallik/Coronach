# C6 — Dialogue layout, readability and portraits

2026-09-30, Claude Code. **C6 remains OPEN.** This covers the plan's presentation checks: long lines, speaker changes, expressions and options at 1280×720, 1920×1080 and 1920×1200, with no clipped text, small type, off-screen options or stale portraits. The input-behaviour half of C6 is not covered here (tap-to-reveal, rapid taps, cancel rules, device switching, handoffs, save/load around branches).

## The check

`DialogueLayoutTests` reads every line and choice the game can say from the Yarn scripts. It confirms the count against the compiled project's string table, so no line is skipped. It then measures them on the real dialogue panel.

1. **Every line and choice fits its box.** Each body line, speaker name and option label is measured with TextMeshPro's own layout at the panel's real box sizes. The instrument proves it can fail: an overlong line is rejected.
2. **On screen and readable at each resolution.** The real canvas is laid out at each screen's scaled size, as its `CanvasScaler` (1920×1080 reference, match 0.5) produces it. It checks that:
   - the panel, portrait and options stay on screen;
   - options never cover the panel or the portrait;
   - rendered glyphs stay clear of the frame border;
   - primary text renders at 18 px or more and hints at 16 px or more.

   Five stacked options, beyond any current node, must be caught colliding with the panel, so the overlap check can fail.
3. **Every speaker shows their own face in the emotion asked.** Each speaker's sets, both bodies for the heroes, must contain every emotion their lines are tagged with. No set may belong to two speakers.

## Runs

| Run | Result | Finding |
|---|---|---|
| First | 0/3 | The panel is built by the zone runtime, not `_Boot`; the test now loads the Decks. The portrait check was already red: **the Survivor speaks with Orrin's face and the Keeper with Hal's** (D040's stand-in) |
| Zone loaded | 1/3 | Every line and choice fits. **The "Continue" hint renders at 13.3 px at 1280×720.** Portraits still red |
| Hint 20 → 25 pt; two new portrait sets | 3/3 | Accepted, but the portrait smoke's 1080p captures showed the larger hint touching the frame's right border |
| Frame-border rule added | 2/3 | Red: "Continue runs into the frame border (30 from the right)" at all three sizes |
| Hint moved 35 units inward | **3/3** | Accepted; the recapture shows it clear of the border |

The panel, portrait and three-option sets fit on screen at all three resolutions. At 1920×1200 the canvas is 1821 units wide, around the 1760-unit panel.

## Smokes

- `portrait-smoke`: `PORTRAIT_SMOKE_OK count=24`. It now renders all ten speakers (it had stopped at six) in Neutral and Shocked, both bodies for the heroes.
- `ui-smoke`: `UI_SMOKE_OK` (dialogue, six menu pages, shop).

## Still open

- The input-behaviour checks listed above, with real input and `AutoAdvance` off.
- Button hints are fixed text ("A / E / ENTER"), not matched to the active device.
- Editorial and play review of the conversations in context.
