# C3 enemy-break interruption checkpoint

September 26. C3 remains OPEN. Two real regressions reject the former implementation: a pending contact deals 10 damage after its owner breaks, and Sentinel/Ridgehound/Chorister Drifter retain the old attack warning throughout break. Sentinel/Ridgehound hit only 16.7/33.3 ms after recovery without a new wind-up; Drifter launches its old shot without a new warning.

Break now cancels enemy attack coroutines, pending contact volumes, animation attack state and warnings, and drops Sentinel's shield. Recovery starts a fresh full tell. The unchanged three-enemy check passes: Sentinel and Ridgehound wait approximately 667 ms from recovery to contact; Drifter warns again before its projectile arrives. An already-released projectile remains in flight and still hits, as the positive boundary check requires. Scrapmite's named death-attack exception is retained.

A separate red test shows both bosses retain their special attack state after break. Their special coroutine/tell now cancels immediately, with a fresh special allowed only after recovery. The combined targeted suite passes **6/6, zero skips**, including existing resumed-melee/dodge and dead-owner lunge checks. [Exact source and evidence hashes](C3-enemy-break.json) retain all red/green runs.

These are direct-action PlayMode regressions. Body recoil/break poses, sustained shield presentation, ordinary-input recording, full integrated suites and rebuilt players remain pending for this change. The current packages still contain `ec0ac23`. Normal save and backup hashes are unchanged. No art spending or music changes.
