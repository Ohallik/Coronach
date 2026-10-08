# Nearby companion flight framing — October 8

**C3/C4/C8 remain OPEN.** Nearby living and disabled companion ships now contribute their hull and visible flight meshes to the existing encounter/release bounds. The 30 m encounter range also bounds companion inclusion; distant followers cannot stretch the camera across the map. Renderer arrays are cached per party. Ground/travel framing, fixed angle, the bounded animal introduction and moving-hero return are unchanged.

The prior ordinary capture visibly clipped Sela at +1.50/+1.80. New checks reproduce the defect on `8f9b6fd`: nearby live craft reaches viewport Y=0.000751, and the disabled craft reaches −0.011756, against the unchanged 0.10–0.90 vertical bounds. The distant-companion case already passes. The repaired source passes **32 affected PlayMode checks**, zero skipped, in 265.152 seconds: three new companion, five camera-return, two flight-framing, seven release and fifteen collar checks. Existing assertions remain intact.

Both deliberate controls reject: removing the distance limit displaces the camera by 328.277 m against a 0.25 m bound; excluding disabled ships leaves the downed craft at viewport Y=−0.013128. Exact intended source is restored. Original/fault source, XML, logs and save checks remain under `Builds/quality/C4/companion-framing/`. No fresh full suite is claimed; the earlier full 173/269 result predates the later collar recovery corrections.

Both standard players build successfully. Exact `8f9b6fd` players are archived as `prior-Windows` and `prior-WindowsDev` in that folder. `source.json` binds 47 inputs; `packages.json` hashes both complete replacements and verifies both archived predecessors. [Machine-readable report](C4-companion-framing.json) binds the evidence inventory.

## Earned route and actual observations

The same earned chapter-09 saves and chapter-10 gameplay steps/assertions pass in **797.269 seconds, 47,815/47,815 focused samples**, process exit 0. Live PNG capture stays deferred in the existing video-only route. All three links are cut in order and the final lock resolves; the logged fight is 87.0 seconds. Both level-7 heroes autosave at **TallowApproach/Arrival**, Taren 87.3672 integrity and Sela 188.86499. This is not docking, Keeper dialogue or chapter completion. Different display/timing conditions preclude an isolated balance comparison with the preceding 92.2-second fight.

Ten extracted frames were actually opened: four combat views and six release/return views. Both ships are fully inside the screen at +0.20, +0.85, +1.50, +1.80, +2.75 and +3.20, resolving the sampled bottom clipping. The whole animal is visible through selected +1.50; its head starts cropping at the top by +1.80 after the existing bounded introduction, and it leaves view during return. These offsets use the last sample of the resolving checkpoint (687.738776–687.905325), not an exact lethal timestamp. Continuous camera quality remains **UNVERIFIED**, not accepted by these samples.

**Companion/body overlap remains rejected:** Sela is partly obscured by Cantor in the pursuit and two-links combat views. The return bark crosses the ships in some stills. The gold bands remain explicit blockout geometry. Continue these supported presentation/spacing diagnoses before final art and preview/story integration.

The earlier repeated “You missed. I noticed.” text is Sela's authored **successful Flash-dodge bark**, invoked with priority and therefore bypassing the normal four-second cooldown. It is not evidence that her attacks missed. Reproduce the actual repetition before changing its arbitration; preserve urgent cover dialogue.

## Timing, preservation and limits

Both display endpoints report **off** before and after; explicit VSync 0 makes this a capture diagnostic, excluded from clean C1 timing and physical acceptance. Median 16.6691 ms, p95 17.1618 ms. Every interval above 25 ms remains:

| Step | Elapsed seconds | Interval ms |
|---|---:|---:|
| 5, loading | 2.939777 | 218.4661 |
| 5, loading | 2.981324 | 41.5474 |
| 5, loading | 3.384556 | 34.0376 |
| 44 | 219.718194 | 71.9148 |

The step-44 hitch has no established cause. It neither repairs nor explains the older awake Decks failures. All 76,539,904 accepted audio floats are written, with peak queue 2/32 and the fixed 4,325,376-byte sample buffer; listening remains **UNVERIFIED**. Use `Review companion framing.html` for retained normal/quarter-speed playback. No continuous video/audio or physical/controller observation is claimed.

Nathan's current saves and all 243 frozen `a3aaad6` files/profile match after tests, controls, builds and capture. Nine known Unity import/package side effects were archived and restored. e93bf13 knee/terrain repairs, paid-asset exclusions, prior failures and every unmet gate remain intact. No presentation candidate is published as validated. Continue actionable C1/C2 diagnosis and dependency-safe C3–C10 production.
