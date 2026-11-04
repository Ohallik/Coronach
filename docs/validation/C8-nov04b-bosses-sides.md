# November 4 evening: boss move sets and chapter 1 side stories (D198, D199)

Status: built and checked by targeted suites; the full suites, the chain and an in-player eye test follow with the batch.

## Built

| Boss | Phase 1 | Phase 2 | Phase 3 |
|---|---|---|---|
| Burrower (60/30%) | marked charge that sticks in rock (exposed), spoil circles, tail sweep | + hunt from below and eruption (exposed), double charge | + seismic rings |
| Bellows Below (50/30%) | breath with inhale pull and marked organ half, rib star | + pressure bubbles | rib crossing every other special |
| Cantor (66/33%, integrity only) | choir sweep / song as built | + spiral song | + coil lash |

- Every special marks its ground (`Telegraph`) and strikes only inside the marking; a dodge still evades. A Burrower charge passes through the party instead of stalling on a hero.
- Mid-fight phase shift: untouchable for exactly the 1.4 s shift, marked shove ring, two Scrapmites from the Burrower that leave when the fight ends.
- A wedged or surfacing Burrower is exposed (`Health.Expose`), the HUD reading "OPENING · STRIKE NOW".
- Boss bar (name, epithet, integrity with lagging chip, phase notches, break gauge) and title card.
- Side stories: Neve's parcel, Quiet ridges (four beacons staged by `SideStoryStaging`, NavMesh-reachable from the outpost, 1.35 m, collider-free), A berth for two, and the chapter's last beat on Tallow's beacon band.

## Checks

Red first, by one targeted defect per check (the code these tests describe did not exist before):
- `Builds/quality/C8/nov04b/playmode-boss-side-mutations-RED.xml`: 14 of 15 failed for their own reasons, for example strikes ignoring their marking, no opening, no pull, still bubbles, unturning spiral arms, no boss bar, a narrow move set, beacons answering before the job, a side beat taking a first meeting, rewards back in materials.
  - The seismic test failed early instead: three timed dodges were Flash Moves, which slow time, and its 6 s budget was too short. The budget is now 15 s.
- `playmode-rings-and-hail-mutations-RED.xml`: the rings with no damage, and the hail beat removed, fail as intended.

First green attempt (`playmode-boss-side-first-green-attempt-RED.xml`, 116/120):
- Two Bellows tests compared against a maximum the party controller resets to the hero's level. They now compare against the integrity before the attack, which the hero did lose (4978 and 4917 of 5000).
- **Real defect:** the shift's invulnerability outlasted the shift by 0.1 s, so a killing blow just after it was ignored and the mites lived on. It now ends with the shift.
- A generic NPC-tier fixture named "Keeper" met the real Keeper's new beat. It now uses a neutral speaker; its assertions are unchanged.

Green: the boss, side-story, break, collar, preview, Hushwell, core-play, evasion, replay, acknowledgement, dialogue-input, dialogue-layout and opening classes (120 tests) pass, with the final two classes rerun at 18/18. Measured in passing: the inhale drew a standing hero from 7.96 to 6.63 m, the wounded Burrower used SpoilToss, DrillCharge, Burrow and SeismicRings, and the spiral sang 48 shots.

Not judged yet: how the markings, bar and moves look and feel in play.
