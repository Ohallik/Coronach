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

## Device-matched hints and readable type (September 30, later)

- **Hints follow the active device (D126).** The dialogue "Continue" hint, the title menu hint, the shop's page switch and the pause menu's sound-controls note were fixed text naming both devices ("A / E / ENTER"). They now compose from the prompt service's own bindings for the last-used device, and re-render when it changes, even with the surface open. A keyboard rebind in progress is never redrawn underneath.
  - `DeviceHintTests` compares glyph tokens against the service's own rendering for each device: the surface must show its device's glyphs and none of the other's. It was **red on the old UI** (the pad dialogue hint named ENTER, the pad title hint lacked the stick) and is green now.
  - The prompt sprite sheet is absent, so tags fall back to plain labels; key pairs are now joined with " / " so they do not run together.
- **Every surface reads at 720p.** `ReadableTypeTests` audits every visible text on the title, title settings, the town and combat HUD on both devices, a downed partner, dialogue, shop and all six pause pages. Each text must render at 16 px or more at 1280×720 and fit its box. Framed HUD content must clear the frame's artwork, measured from the panel texture: 34 units in at the sides, 30 at the top and the 42.5-unit slice band at the bottom, plus about 10 units of air. Each rule is proven able to fail with a planted probe.
  - **First run, red:** the HUD's bar labels rendered at 10 px, the partner line at 12, the hint row at 14, the objective and title subtitle at 15.3, and the title hint at 12.7.
  - **Fix:** the vitals panel was relaid out at 24-unit type, with a row per label, longer bars and padding to clear the artwork. Skill icons and labels went from 120 to 150 units apart. A downed partner's line drops its 0/max so it stays on one row, and the dialogue speaker name moved 4 units down from the border.
  - `InputProbe` is excluded: it is a development-build diagnostic. World-space nameplates (the small "Survivor" label) are not yet audited.
- **Seen.** 1280×720 captures of all six zones (`Builds/logs/look/hud-720c`) were opened. The vitals, objective and hint rows read clearly and sit inside their frames.

## Input behavior (October 3)

The focused input suite now covers all 28 compiled story nodes and 35 outcomes with AutoAdvance off, held/rapid confirms, live device changes, option availability, UI focus, handoffs and save/reload consequences. Eleven tests pass after three cancellation defects were reproduced and fixed. Nine deliberate mutations prove the checks reject broken behavior. Full integrated rerun, OS focus/physical input and contextual editorial review remain open. [Input evidence](C6-dialogue-input.md).
