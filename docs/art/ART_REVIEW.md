# P3_ART_OK — 2026-09-21

All 48 generated production model prefabs are imported and visually accepted. Meshy spend is **765 / 1,200 credits**, summed from `consumed_credits` in `gen-manifest.json`; the 27-environment batch succeeded for 405 credits. No paid work remains queued.

## Evidence opened

- `Builds/logs/renders/contact_sheet.png`: all 48 models in framed, evaluated Idle views. The four `model-review-page-*.png` pages were also opened at readable size.
- `Builds/logs/renders/true-scale-row.png`: all models at their normalized intake scale. The first 48-model row was rejected because the camera far plane clipped the entire lineup; the corrected row is visible and accepted.
- Each hero's Natural/Shaped Idle, two Walk phases and Attack; `Taren-Flight.png`, `Sela-Flight.png` and both overhead Flight captures. Four separate generated vanes unfold outward on each Shaped body.
- Sentinel Idle, both Walk phases and Attack after the local skeleton/weight repair and Unity rest-pose reset. Ridgehound uses the skeleton-only Fox donor and visibly alternates strides; its attack pose was opened.
- `Cantor-chain.png`: the generated head, six repeated body sections and tail connect at a consistent centre height and forward axis. Front/side segment renders were used to correct orientation.
- All three ships, four civilian variants and 27 environment pieces were inspected in their framed tiles. Ships face runtime +Z. Tallow hull additionally reviewed front/side/back.
- All eight portrait sheets were rendered in real dialogue at Neutral and Shocked: sixteen accepted captures. See `PORTRAIT_REVIEW.md`.
- Generated material, sky, planet and UI sources retain their original prompts and native files. Five panoramas/maps now have 4096x2048 runtime derivatives from actual 4x Real-ESRGAN; `world-upscale.json` distinguishes native 1774x887 output from the production derivative.

## What makes the gates reject

`GenIntake` rejects empty/duplicate rows, missing models or albedo, embedded textures, oversized albedo, non-human biped avatars, fewer than four separate hero vanes, root-only animation and frozen visible skin. `ArtReview` requires graphics, two camera renders and non-uniform pixels, as well as minimum file size. The blank far-clip row is archived as `true-scale-row-rejected-farclip.png` and has zero per-channel range; file size alone had failed to catch it.

The accepted prefab inventory is `intake.json`; measured bounds and humanoid flags are in `Builds/logs/renders/model-review.json`. Reproduce with `GenIntake.Build`, graphics-enabled asynchronous `ArtReview.Render`, then `tools/model_contact_sheet.py`. Final scene composition, route balance and performance belong to P4/P5, not this model intake gate.
