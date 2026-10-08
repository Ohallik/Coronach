# C3/C8 Cantor collar gameplay blockout — October 8

**C3/C4/C8 remain OPEN. The collar gameplay blockout passes its earned ordinary-input route, but opened frames reject complete-party release framing: Sela is partly clipped.** Final collar art, approach preview and continuous audiovisual quality are not accepted. [Exact source, packages, tests and capture evidence](C3-cantor-collar.json). The first integration passes 173 EditMode / 269 PlayMode tests, zero skipped; later recovery and ownership corrections pass 37 and 24 separately identified affected checks. Both players build successfully, with exact prior `08f4936` players archived. Frozen `a3aaad6` and Nathan's saves remain protected.

The atlas requires independently cuttable collar links, changed attacks after each cut, and a final lock that releases the animal. The prior shared-health fight fails both new original-state checks: a protected-head hit drains 22,000 integrity to zero, and no independent links exist. These failures and their exact source remain under `Builds/quality/C8/cantor-collar/original*`.

Three separate moving restraints now attach to the first, third and fifth body segments. Each has 5,500 integrity; the protected head lock has the remaining 5,500. The original 22,000 raw integrity budget is retained. This does not imply equal damage throughput, difficulty or fight duration. Old anatomy hurtboxes cannot bypass the lock. Normal target finding/cycling skips it until every link is severed, while the objective names the remaining work. Targeting a link still frames the whole animal.

| Link | Actual attack change after the cut |
|---|---|
| Sweep | Marked/damaging sweep radius contracts from 8 m to 5 m. |
| Chorus | Song radius contracts from 14 m to 10 m; base damage falls from 12 to 8 and the hit no longer staggers. |
| Volley | Three shots at −12/0/+12 degrees become one. Cantor's individual shots use 55% of the existing packet amount. |

Cuts advance the established three-phase fight; later phases alternate sweep and song. Link damage contributes to the carrier's break meter and interrupts its actual tell without draining the lock. No link awards loot, XP, quest completion or an animal death. The final lock alone runs the existing once-only outcome and bounded animal departure. An incomplete encounter retains the existing checkpoint reset behavior; there is no new partial-link save schema or fabricated link history for old completed saves.

The visible bands are explicitly temporary blockout geometry. They open into bounded falling plates. An initial version kept spent plates beneath the animal: the existing whole-body exit check measured a trailing renderer at z=808.947 instead of beyond 905, and the ordinary-view test still saw a collar plate. Both failures are retained. Separate owned equipment roots now follow attached joints, freeze at a cut, retire their plates after 1.2 seconds, and are disposed with their carrier. The existing whole-animal assertions are unchanged. A paused chain also retained a small tangent update despite zero delta; an explicit pause guard fixes that drift without touching the knee/terrain solver.

Evidence completed before integration:

| Run | Outcome |
|---|---|
| Original runtime | 0/2: protected-head bypass and missing links reproduce. |
| Candidate 01 | 3/3, including the unchanged moving-segment/three-phase check. |
| Candidate 02 | 9/10: 0.187 mm paused plate drift exceeds the 0.1 mm bound. |
| Affected 01 | 36/38: the two spent-equipment/body geometry failures above. |
| Geometry 01 | 21/21: all 12 collar checks, seven existing release checks and two flight-framing checks. |
| Deliberate controls | Twelve fault variants reject in 14 test cases; mutated runtime files restored byte-exact. |
| Full EditMode | 173/173, zero skipped. |
| Full PlayMode | 269/269, zero skipped, before the delayed-recovery correction below. |
| Delayed recovery original | 0/2: fresh link colliders become disabled and the carrier brain stays shut down. |
| First recovery repair | 36/37: restarting the body wave changes the restored stationary chain by 0.123 m; the old 1 mm pose bound rejects. |
| Revised recovery | 37/37 affected checks, zero skipped; the preceding failed repair and full suite remain separate. |
| Frozen recovery motion | Deliberate fault rejects at zero local chain movement after a carrier turn, against the >0.02 m assertion; exact source restored. |
| Inactive ownership original | 0/1: separate equipment remains visible after the carrier is deactivated. |
| Final ownership/collar/release/framing | 24/24, zero skipped. |

The initial twelve new tests cover all six cut orders, protected/final lock behavior, exactly-once rewards, normal target cycling, a real projectile hitting a moving link, shared break/tell cancellation, pause/recovery, whole-animal framing from each link, equipment cleanup, and the three emitted attack changes. Deliberate controls reopen the lock early, award a link reward, include protected targets, lose shared break, rotate the paused chain, omit recovery, preserve old attacks, add integrity, misdirect projectile damage, frame only a link, or orphan equipment. The target-filter control selects the disabled fixture's Chorister Dart; it rejects the protected-target filter, not specifically a root-lock selection. Original failures and controls remain individually identified.

Read-only review during integration exposed a gap in the early recovery assertion. Two additional tests wait beyond the complete 1.15-second recovery: the presentation restores the disabled collider snapshot captured after all links were cut, and the enemy/body/boss remain shut down. Both failures reproduce on the integrated source. A completion notification now lets the collar restore its live-link collision after that snapshot, restart the segmented body and reset the existing brains' attack timers, phase and encounter cue. The first repair passes those new checks but violates the existing stationary pose restoration. The revised chain holds the restored pose until the carrier actually moves or turns, then resumes ordinary propagation. Every old pose assertion stays unchanged. The added checks require a real projectile to hit the restored link, actual chain movement after a turn, and an actual marked special from the recovered carrier. The original full-suite result remains bound to its preceding source; it is not relabelled as testing this later correction.

A fifteenth collar check then reproduces separate equipment leaking visibility when its carrier is deactivated. Owner enable/disable now controls the equipment roots without resetting link damage or restarting spent-plate lifetime. Destroyed-owner cleanup remains covered separately. The final 24-check run covers all fifteen collar checks, seven existing release checks and two flight-framing checks. Its exact source is retained separately from the preceding 37-check recovery run and the initial full suite.

Existing release, lifecycle, music and break fixtures now sever links before testing final-lock resolution. Their prior assertions remain. Full camera return, release path, e93bf13 knee/terrain repairs and preceding C2 support behavior are preserved. No offline production tool changed; the earlier 202 offline result remains prior evidence, not a fresh run.

No new paid art task has been submitted. The live Meshy balance was 2,413 credits; the 365-credit difference from the previous 2,778 record is unattributed. Final generated equipment should match the existing Compact mooring kit and remain visibly distinct from the animal's organic resonator. The gameplay blockout and ordinary reachability come first.

The development capture uses the same earned chapter-09 saves and every chapter-10 Gullet gameplay step/assertion. Only live PNG requests are deferred, as in the preceding camera comparison; video and listener audio remain. Runtime and independent analysis pass with process exit **0**, **801.840 seconds** and **48,097/48,097 focused samples**. Both Windows display endpoints are on before/after, with **VSync 1**. This recording is excluded from clean C1 timing and physical-presentation acceptance.

Ordinary input cuts sweep, chorus and volley in that order, then resolves the lock. The recorded first-damage-to-resolution duration is **92.2 seconds**. During the engagement, Taren moves from 135.3 to 93.6 integrity and Sela remains at 188.9; neither recorded actor state enters down/recovery. The final autosave has both living level-seven heroes at **TallowApproach/Arrival**, with 93.555 and 188.865 integrity. This does not include docking, the Keeper or chapter completion. The prior camera capture's 80.6-second fight began with different health and display/VSync conditions, so it is retained as context rather than an isolated difficulty comparison. One successful input sequence establishes reachability, not final balance or feel.

All intervals remain: median **16.669 ms**, p95 **17.198 ms**, worst **244.186 ms**. The complete >25 ms set is 244.186, 36.986, 27.262 and 35.025 ms in loading step 5, plus **27.339 ms** in step 47, “second chamber recovery 1.” This neither explains nor erases the earlier awake Decks failures. The bounded listener writes all **76,978,176 accepted float samples**, peaks at 2/32 queued blocks within its existing 4,325,376-byte sample buffer, and reports completed writes without errors. Audio quality was not auditioned.

Thirteen ordinary gameplay frames were actually opened. The bands, separate HUD names and progress from 0/3 through the exposed lock are legible in those selected views. Head-lock plates separate at +0.20/+0.85; the bands are gone by +1.50, and the connected animal remains visible through +1.80. **Sela is partly clipped at the bottom at +1.50 and +1.80.** Both heroes are visible again in the +2.75/+3.20 camera return. Preserve that presentation rejection and repair companion framing next. The offsets use the last resolving `stopWhen` sample, not an exact lethal timestamp. Several combat stills also show partial companion/body overlap and the same miss bark; investigate their actual firing-position/frequency behavior rather than infer continuous behavior from stills. The flat gold bands remain obvious stand-ins and are not final art. Use `Builds/quality/C8/cantor-collar/Review collar blockout.html` for later continuous review; none has been claimed.

The evidence binds 45 source files, both complete package inventories, 13 test-result XMLs and 542 retained artifacts. The nine known Unity import/private-package side effects are archived and restored. Runnable candidates are `Builds/Windows/Coronach.exe` and `Builds/WindowsDev/Coronach.exe`. Nathan's saves, all 243 frozen chapter files and the frozen profile state match.

Continuous motion/audio, physical scan-out and controller feel remain **UNVERIFIED**. Companion release framing is **REJECTED** by the opened views. C1's original awake Decks hitches and invalid Unity GPU values, C2 movement concerns, the Gullet preview/story and every unmet C1–C10 gate stay OPEN. No presentation candidate is published as validated.
