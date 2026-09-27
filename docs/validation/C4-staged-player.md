# C4 staged forms and rescue in the packaged player

September 27, 2026. **C4 remains OPEN.** Both Windows packages were rebuilt from `a759b1d`. Full integrated verification passed **50/50 EditMode (0.2551 s) and 137/137 PlayMode (852.1867 s), zero skips**. Raw XML and both build logs are retained in `Builds/quality/C4/staged-integration01/`; hashes, run manifests and actually opened images are in [C4-staged-player.json](C4-staged-player.json).

The release captures use the ordinary Input System with starter equipment, live partner behavior, unchanged damage and isolated saves. Every sample in all nine captures retained focus; none records a simulation pause. Capture overhead excludes these runs from clean C1 timing.

| Accepted ordinary capture | Checks | Focused samples | Duration | Result |
|---|---:|---:|---:|---|
| `staged-docking-taren-02` | 10 | 1127/1127 | 18.844 s | Dock, Natural arrival, launch, CivilFlight return; both heroes 120 HP |
| `staged-docking-sela-02` | 12 | 1152/1152 | 19.279 s | Same round trip with ordinary swap to Sela; both heroes 120 HP |
| `flight-disabled-taren-01` | 9 | 1116/1116 | 18.571 s | Wall damage disables Taren at 6.289 s; proximity revive at 10.088 s; swap back and fly away; end Taren 42/Sela 108 HP |
| `flight-disabled-sela-01` | 11 | 1109/1109 | 18.454 s | Wall damage disables Sela at 6.537 s; proximity revive at 10.338 s; swap back and fly away; end Sela 30/Taren 120 HP |
| `form-ordinary-05` | 35 | 3873/3873 | 64.525 s | Repeated safe-boundary crossings, return inputs, swap and Natural return for both heroes |

Earlier rejected evidence remains intact. Both docking `01` runs complete the actual round trip but omit `expectedScene` on two interior fixture steps, incorrectly expecting the exterior. The corrected fixtures specify Hub_Decks and retain identical gameplay input and other assertions. `form-ordinary-03` rejects six navigation checks because its pre-fold reset windows are too short; `04` rejects one, stopping 0.02598 m from a 0.02 m goal after companion contact. `05` allows three seconds for each reset and five seconds for Sela's initial approach. Goals, 0.02 m tolerances, short reversal inputs and form assertions remain unchanged; no gameplay timing or collision was relaxed. Current reusable routes are under `docs/quality/routes/`.

Codex opened 23 actual stills: ten extracted docking stages across the two heroes, eight extracted disable/rescue/recovery frames, four safe-boundary close/open frames, and one interior-arrival screenshot from the first rejected Taren fixture. At normal gameplay scale, ship wings remain attached through folding, bodies stay full size, and arrival opening is visible beneath the scene fade. The rescue ring clearly distinguishes amber disabled and cyan restoration states; hulls remain visibly separated beside the tunnel wall, then the cue clears. These support the candidate geometry and cue readability. Single frames do not establish smooth topology exchange or normal/slow continuous-motion quality.

The rescue frames also expose inappropriate casual swap banter after a partner falls (for example Sela's competitive swap line). This is an open state/context defect for C6. Sorrel is still blockout 06; Gullet's repetitive tube remains MAP_EYE_TEST rejected. Rear Cinder ring panel edges still require cleanup, long civil/combat circuits require acceptance, and enemy flight failure remains unfinished. No map, full C4, audio or workshop acceptance is inferred from these short routes.

Original save hashes were unchanged after the full suites and before these isolated player captures. Music originals and assignments were not modified. Normal/slow video observation, sound audition and physical Logitech feel remain **UNVERIFIED**. Runnable packages: `Builds/Windows/Coronach.exe` and `Builds/WindowsDev/Coronach.exe`, runtime `a759b1d`.
