# P3 portrait evidence

`P3_PORTRAITS_OK` — 2026-09-21. Eight generated sheets, sixteen expressions each. Native source grids were mapped by eye, actually upscaled with Real-ESRGAN, assembled to 2304x2304, and imported together. All sixteen sprites in each sheet have full 576x576 rectangles.

The graphics-enabled development player ran the real dialogue presenter with Neutral and Shocked for Taren Natural/Shaped, Sela Natural/Shaped, Orrin, Mira, Hal and Neve. `PORTRAIT_SMOKE_OK count=16`. Every capture was taken twice across frames and checked for fresh, nonblank output. Missing sheet, wrong rectangle, stale capture or incomplete capture count rejects the smoke.

Opened all sixteen dialogue panels in `Builds/logs/portraits/dialogue-review-1.png` through `dialogue-review-4.png`, composed without altering content from the original 1080p captures. Also opened full-resolution Taren Natural Neutral and Sela Shaped Shocked. Judgement: correct identities/forms, obvious expression changes, preserved eyes/mouths, clean keyed silhouettes and readable body text. No adjacent-cell leakage or face clipping. These render on the current arena background; final zone art is reviewed separately.

Reproduce with `scripts/portrait-smoke.ps1`, then `python tools/portrait_evidence.py`. Source and exact prompts: `docs/art/portrait-catalog.json` and `art-src/Generated/P3-portraits/refs/`.

## Every current speaker, September 30

The Survivor and the Keeper had borrowed Orrin's and Hal's sets since D040, so the Sorrel survivor spoke with Orrin's face. Each now has their own 16-expression Natural set: `Survivor_Natural`, a worn Sorrel miner, and `Keeper_Natural`, Tallow Drift's older host. They were made in the house template the same way as Oda and Ilo: Meshy image-to-image, 12 credits each, 3,788 left. Both native sheets and keyed atlases were opened. Each has sixteen cells in template order, one identity throughout, and no text or marks. The species palette in the template overrode the requested skin and frond colours; both stay within the Vael look. `PortraitIntake.ImportNamed` rebound both character definitions.

The portrait smoke now renders all ten speakers, Oda, Ilo, the Survivor and the Keeper included: `PORTRAIT_SMOKE_OK count=24`. The Survivor Neutral, Keeper Shocked and Oda Neutral captures were opened at 1080p. `DialogueLayoutTests` now fails any speaker who shows another speaker's face, or a line whose tagged emotion its set lacks. Without that check, `PortraitSet.Get` silently falls back to Neutral.

The Survivor and Keeper **bodies** still reuse Orrin's and Hal's generated models (D040).
