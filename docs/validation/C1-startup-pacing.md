# Ordinary startup pacing — October 4

**Historical configuration.** Nathan's October 6 tearing report prompted a synchronized-presentation repair; see [current comparison and validation](C1-C2-presentation-motion.md). Retain the quarter-second failures below. The earlier VSync-off repair is no longer the production policy.

**A real startup defect was reproduced and corrected. C7 remains open.** The
ground arena smoke killed all three targets, then failed its unchanged Flash/
damage assertions. Diagnosis found an accepted dodge followed by a 246–248 ms
delay before a hit intended after 50 ms; it arrived outside the dodge window.

This was not attack recovery or slow sound preparation. The dodge call took
0.44–0.47 ms. A rolling frame trace showed repeated 248–264 ms frames while the
window retained focus. Ordinary startup used Ultra's VSync count 1, despite
`Application.targetFrameRate=60`. Every quality replay had instead forced VSync
off. Unity documents that nonzero VSync overrides the standalone frame cap:
[VSync API](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/qualitysettings/vsynccount).
The exact driver/compositor reason for the quarter-second waits is not established.

An isolated control changes only VSync to zero in the arena. Its final twelve
frames are 16.66–16.67 ms, the hit arrives after 66.7 ms, and the original three
kills, swap, Flash and unchanged-health assertions pass. That temporary control
switch and per-frame diagnostics are removed from the final source.

Ordinary 60 fps replays now preserve the real startup pacing. Only explicit
uncapped or 30/120 fps diagnostic routes set a different policy. Before fixing
startup, the actual Decks route consequently reproduces the defect:

| Same development Decks route | Broken startup | Corrected startup |
| --- | ---: | ---: |
| Duration | 129.022 s | 124.608 s |
| Focused samples / total | 839 / 839 | 7,477 / 7,477 |
| p95 / p99 frame interval | 266.113 / 277.600 ms | 17.023 / 17.172 ms |
| Worst frame | 279.238 ms | 25.984 ms |
| Frames over 33.3 ms | 488 | 0 |
| Shop and traversal | Rejected: missed inputs and destinations | All pass |

`GameServices` now explicitly selects VSync off with the 60 fps cap at normal
startup. This changes neither graphics quality nor resolution. Both arena smokes
pass in the resulting development player, with no relaxed timing, health, kill
or swap assertion. Their failure message now reports the actual delayed hit.

The offline performance gate also rejects the wrong cap, nonzero VSync or missing
pacing metadata. All three new controls fail before implementation; all **113**
offline controls pass afterwards. Earlier C1 measurements remain valid for their
documented VSync-off configuration, but did not establish ordinary startup parity.

Raw directories are `Builds/quality/workshop/pacing-decks-default-{red,green}` and
`Builds/quality/workshop/candidate-57147a1/arena-*`. Preserved smoke attempts include
one incorrect release invocation, which reached Title because release ignores
`-scene`; it is not a runtime arena failure. The development failure, diagnostic
failure, frame trace and single-setting control are retained separately.

Final release, both stations' full first/warm runs, headroom, the integrated suite
and remaining workshop checks follow this checkpoint. The existing frozen
`57147a1` workshop copy must be replaced before handoff. Continuous viewed motion,
audio audition and physical-controller feel remain UNVERIFIED.

[Source and evidence hashes](C1-startup-pacing-evidence.json).
