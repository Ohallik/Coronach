# Coronach presentation pass — 2026-09-23

**September 26 follow-up:** Nathan reports station traversal performance problems, right-facing combat runs, weak SFX and inadequate deaths. These remain open and are addressed by [PRODUCTION_PLAN.md](PRODUCTION_PLAN.md). The records below describe September 23's limited checks; they do not establish station frame pacing, all-direction continuous locomotion or death/audio quality.

The slice now has sharp native-resolution surroundings, a wider animation vocabulary, distinct generated ship forms and Nathan's location music. **The professional-quality gate remains open.** This pass addresses the reported defects; it does not accept the existing environment composition, menus or combat effects as finished.

## What was inspected and played

The editor rendered 116 key poses/views from the actual imported hero prefabs, including four phases of each selected motion and top/three-quarter views of the ships. Opened contact sheets cover both heroes' walking/running, Taren's combo and skills, Sela's ranged/cast poses, and both spacecraft. Evidence is in `Builds/logs/presentation/`.

The rebuilt development player was then played through ordinary virtual-gamepad events, observing each captured frame before choosing the next input. Simulation pauses between decisions. This is **not** continuous human play on the physical Logitech.

- `Builds/logs/review-coronach-ground/000–007`: approach a live Ridgehound pack; perform all three ordinary Taren strikes, roll away, return and use Cleave. The pack dies through real attacks and companion fire. Ground textures and objects remain sharp away from the player at each camera position.
- `Builds/logs/review-coronach-flight/000–003`: inspect both hulls at gameplay scale, move/fire at Darts, turn and roll, brake and swap to Sela. Ships remain horizontal with a readable nose and wing silhouette; no dangling humanoid limbs. These few steps do not complete a flight zone.
- `Builds/logs/review-coronach-town/000–013`: walk and sprint from the office to the outfitter, navigate around the divider and bench, talk to Mira, open the actual shop and close it. The log records Hub Town Groove → Moonbase Market → Hub Town Groove. Step 007 lost window focus and is excluded as movement evidence; restoring focus made step 008 move normally.

These sessions use the isolated developer starter loadout. They do not touch the normal save directory. Title and moon cue routing, active audio samples, interrupted fades and return-to-town volume are also exercised in PlayMode. The four runtime OGG files are exact copies of Nathan's originals. Offline loudness analysis found the seven sources within about 1 LUFS of one another; no source audio was rewritten.

## Corrections found during verification

1. Retargeting initially retained the donor's root orientation and faced the wrong way. The corrected imports bake body orientation and were rerendered.
2. Unity rejected vane parenting inside imported model instances. Intake now unpacks the generated body before parenting, checks the result and produces vanes that follow the torso through deep bends.
3. A full scripted route failed at Burrower with Taren down and 6,049 of 14,000 boss Integrity remaining. Companion AI now reacts to imminent telegraphs using the same dodge action as the player; ranged Sela keeps more distance. Further diagnosis found Burrower suspended at y=1.96: gravity only ran while chasing, so it could step onto a body and remain elevated at firing range, making shots pass underneath. A focused regression reproduced the defect (y stayed exactly 2.0), then gravity was moved to every unpaused ground-enemy update, including waiting, telegraphing and special attacks. Boss Integrity and the 150-second encounter deadline are unchanged. Rejected evidence: `Builds/logs/coronach-route-partner-red.log` and `Builds/logs/coronach-ground-gravity-red.xml`.
4. Two test fixtures counted shots started by the companion during zone entry. Those unrelated shots could hit or recycle during the assertions. The tests now finish those wind-ups and clear their shots before measuring the action under test; damage timing and volley-count assertions are retained.

## Final verification

Both release and development Windows players were rebuilt after the companion and gravity corrections. **35 EditMode and 28 PlayMode tests passed, with zero skips.** The full scripted route passed in **542.1 seconds**, reached level 6, defeated Burrower in **45.7 seconds** and Cantor in **87.9 seconds**, docked at Tallow Drift and saved `sliceComplete=true`. The real Logitech attached in this run and in the release-title smoke check. Opened the final completion screenshot and release title; neither is a claim of human controller play.

The final world run captured all eighteen 1080p views across six scenes. Opened the town, combat and Tallow contact sheets and full-size moon/cave views. Surfaces remain sharp across the image. The native-resolution Gullet benchmark, also opened, sustained at least twelve live enemies for **30.018 seconds / 26,333 samples** on the **RTX 5070**: uncapped mean **877.2 fps**, median **1.103 ms**, p95 **1.585 ms**, p99 **1.854 ms**. Normal gameplay is capped at 60 fps; this benchmark does not predict performance for the planned larger maps.

Numerical results, source-music hashes, release-package inventory hash and evidence paths are recorded in [coronach-presentation.json](validation/coronach-presentation.json). All four integrated tracks match the supplied originals byte for byte. All seven originals are preserved. The two new generated ships bring Meshy spending to **795/1,200 credits**, with **405 remaining**.

## Still to improve

- Weapon edges, trails and enemy reactions need stronger contact readability. The Gullet's darkest enemies still blend into its floor. Library-derived poses do not replace bespoke attack choreography.
- Lock-on movement needs directional strafes, stops and turns. Cloth, fingers and the generated skin weights need closer deformation work.
- The form transition currently collapses/swaps/expands meshes inside an effect. A detailed mechanical folding animation remains a separate task.
- Decks rooms and terrain remain repetitive; the clearer image makes that easier to see. The full-world atlas is planning, not a claim those spaces have been built.
- Physical Logitech comfort, the music mix heard through the user's speakers and a continuous full playthrough remain unverified. Hardware attachment and automated route completion are separate evidence.

See [ANIMATION_DIRECTION.md](ANIMATION_DIRECTION.md) for the next choreography milestone and [PLAY_REVIEW.md](PLAY_REVIEW.md) for the broader quality rejection.
