# Zone light and grade (D189)

2026-11-03. Status: **applied to all nine zone scenes; editor stills reviewed; in-player/continuous review and C1 timing UNVERIFIED.**

## What changed

`Lattice.EditorTools.ZoneLook.Apply` edits only each scene's directional light (colour, intensity, shadow strength; Sorrel pitch 44°), `RenderSettings` ambient (Trilight) and fog, and the zone's volume profile (ColorAdjustments, SplitToning, Bloom, Vignette; Tonemapping disabled; any DepthOfField disabled). Specs live in `ZoneLook.Specs`.

| Zone | Intent |
|---|---|
| Sorrel_Ridges, Arena_Ground | warm afternoon sun, cool sky fill so shadows read blue-violet on ochre ground, dusty distance fog 60-260 m |
| Hub_Decks, TallowDrift | near-neutral hull light, gradient fill, slightly more contrast, trim bloom; no split toning |
| Hub_CinderHalo, TallowApproach, Arena_Flight | hard warm star, deep blue fill |
| Gullet_Tunnel | cold key, violet fill, stronger vein bloom |
| Hushwell | practical lights unchanged; deeper fill, crystal bloom |

## Review

`LookDev.Render` (read-only) renders gameplay-angle stills with both heroes placed, through the private biome pipeline. Runs under `Builds/quality/look/`:

- `baseline-01`: before.
- `preview-01`: **rejected.** Neutral tonemapping plus strong split toning turned the Decks monochrome blue and Tallow muddy brown.
- `preview-02`: accepted spec (identical to what `Apply` wrote). Side-by-side crops `cmp2-*.jpg` were opened: Sorrel gains warm sunlit ground with cooler shadows and better hero separation; Decks/Tallow are essentially unchanged to slightly richer.

These are editor stills with static heroes, not ordinary play.

## Checks

`ZoneLookTests` (EditMode): `EveryZoneVolumeIsSharpAndGraded` **failed** before `Apply` (`Arena_FlightVolume.asset has no authored grade`, `checks/zonelook-before-apply.xml`) and passes after; `TheCheckerRecognisesABlurringProfile` is the built-in negative control. 2/2 pass.

## Limits

No C1 timing re-measurement yet (grade is LUT-based; fog/ambient are per-pixel constants). The interiors' sameness is a layout/dressing problem this pass does not address.
