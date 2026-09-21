# P3 portrait evidence

`P3_PORTRAITS_OK` — 2026-09-21. Eight generated sheets, sixteen expressions each. Native source grids were mapped by eye, actually upscaled with Real-ESRGAN, assembled to 2304x2304, and imported together. All sixteen sprites in each sheet have full 576x576 rectangles.

The graphics-enabled development player ran the real dialogue presenter with Neutral and Shocked for Taren Natural/Shaped, Sela Natural/Shaped, Orrin, Mira, Hal and Neve. `PORTRAIT_SMOKE_OK count=16`. Every capture was taken twice across frames and checked for fresh, nonblank output. Missing sheet, wrong rectangle, stale capture or incomplete capture count rejects the smoke.

Opened all sixteen dialogue panels in `Builds/logs/portraits/dialogue-review-1.png` through `dialogue-review-4.png`, composed without altering content from the original 1080p captures. Also opened full-resolution Taren Natural Neutral and Sela Shaped Shocked. Judgement: correct identities/forms, obvious expression changes, preserved eyes/mouths, clean keyed silhouettes and readable body text. No adjacent-cell leakage or face clipping. These render on the current arena background; final zone art is reviewed separately.

Reproduce with `scripts/portrait-smoke.ps1`, then `python tools/portrait_evidence.py`. Source and exact prompts: `docs/art/portrait-catalog.json` and `art-src/Generated/P3-portraits/refs/`.
