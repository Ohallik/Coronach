# C5 combat and menu cue wiring — September 28

**C5 OPEN.** The staged palette's swing, impact, needle, UI and dialogue clips now follow actual game events. This is cue *correctness*, not an audition: nobody has listened to the result, and the three captured 60-second mixes remain open. Implementation, tests and this record are by the Claude Code session. Raw results are under `Builds/quality/C5/`.

## What changed

| Event | Before | After |
|---|---|---|
| Melee contact window opens | `impactMetal_000`, even on a miss | `swing` family at the blade (`CombatAudio.Swing`) |
| Damage actually resolved (`Health.Receive`) | no impact cue | `hit_soft`, or `hit_armor` when shielded and unbroken, resisted, guarded or a ship hull; at most 3 impacts start per 50 ms |
| Hero ground and ship shots, Static Net launch | shared `laserSmall_000` | `needle_taren` / `needle_sela` per hero |
| Enemy ranged fire (Spitter, Serpent, Chorister Drifter) | silent | `laserSmall_000` at pitch 0.78, owned by the shooter |
| Menu button | silent | confirm after the action, unless it reported an error or cancel |
| Refused purchase, sale, craft or gear operation | silent | `ui_error`, no confirm |
| Back buttons, B/cancel in shop, pause, title, save picker and rebinding | silent | `ui_cancel` |
| Selection moves between two live controls | silent | `ui_move`; opening a menu or rebuilding under the same focus stays silent |
| Dialogue advance | silent | `dialogue_tick` |

The rules are recorded in DECISIONS D112. Skill-specific voices (Cleave, Pulse, Overdrive, Refract) keep their original single Sci-Fi clips; making every skill distinct remains open.

## Results (all zero skips)

| Run | Result | Notes |
|---|---|---|
| `cues-red01` | **0/4, rejected as intended** | New checks against the committed runtime. The miss played `impactMetal_000` with no swing, Sela's needle and Taren's ship shot played `laserSmall_000`, and a refused purchase was silent. |
| `cues-green01` | 27/28 | Wiring plus affected suites. The menu move check failed: diagnostics showed one watcher and a real ShopRow1→ShopRow2 change, but batch-mode frames arrive milliseconds apart and the preceding move had started the 35 ms UI cooldown. The fixture now spaces inputs by 100 ms, like a person, and also asserts that a same-focus rebuild is silent. The cooldown is unchanged. |
| `cues-green02` | **28/28** | Cue tests plus settings UI, voices, melee/ranged contact and flight emitter suites. |
| `full-integration02` | **56/56 EditMode, 158/158 PlayMode** | Full suites with the wiring and the rebuilt Cinder ring. |
| `net-cue-red01` | **2/2 Net assertions rejected as intended** | The Static Net launch still borrowed the bare laser, which is now the hostile shot, on ground and in flight. |
| `net-cue-green01` | **21/21** | Net fix plus cue tests and the ground/flight skill suites. The full suites above predate only this two-call change. |

Nathan's saves are byte-identical before and after every run (the headless save shield plus hash comparison). No music, sound source, licence or previously staged derivative changed.

## Lifecycle and defence cues (second checkpoint)

| Event | Before | After |
|---|---|---|
| Non-boss enemy death | `explosionCrunch_000` at the killer, for every body | `death_creature` or `death_mech` at the body; Sentinel and Mine archetypes are machines |
| Boss defeat | killer-side crunch | the same crunch, owned by the boss body; a designed finish is still open |
| Hero downed | silent | `hero_down` on the ground; a machine failure for a disabled craft |
| Hero revived | silent | `hero_revive` |
| Ground dodge | ship `thrusterFire_000` | `dodge` family; the flight roll keeps the thruster |
| Refract interception | silent (only the counter shot) | `deflect` family |

| Run | Result | Notes |
|---|---|---|
| `lifecycle-cues-red01` | **2/2 new checks rejected as intended** | Wiring set aside: the hound's death played only the killer-side crunch, and the ground dodge played the thruster. The palette test passed with all 45 staged clips matched to their two provenance records. |
| `lifecycle-cues-green01` | **37/37** | Cue, palette, voice, death lifecycle, creature defeat, ground buff, flight disable and footstep suites. |
| `full-integration03` | **56/56 EditMode, 160/160 PlayMode** | Full suites with the lifecycle wiring and the provenance-based palette check. |

## Limits

- Listening: **UNVERIFIED**. The checks establish which cue starts, when and for whom. They cannot establish that the palette sounds good, that levels balance, or that repeated impacts avoid fatigue.
- The dialogue tick has no automated check; it is called only from the panel's accepted-advance path.
- Continuous video and physical Logitech feel: **UNVERIFIED**.
