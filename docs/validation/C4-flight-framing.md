# Flight encounter framing - October 3

Reviewer: Codex. **Combat framing PASS; full Gullet visual acceptance OPEN.**

The camera now fits visible geometry for the active ship and a living locked
target within 30 m. It retains the authored camera angle and FOV, eases distance,
preserves composition during hero swaps, and restores travel framing afterward.
Ground composition is unchanged. Renderer lists are cached and transient
telegraphs are excluded from the bounds.

## Regressions

Raw results: `Builds/quality/workshop/camera-regressions/`.

- `viewport-red`: the original camera clips Cantor at bearing 0, viewport
  y=1.08243752 against the unchanged maximum .90.
- `ground-and-swap-red`: the original swap reset clips the boss at the same y.
  Independently removing the flight-only centre guard moves the fixed ground
  camera 153.334 m. Exact source bytes were restored after the deliberate fault.
- `framing-nine-green`: 9/9 pass, 40.74 s: two flight framing checks, four station
  motion checks, three continuous replay checks. The full Cantor body and hero
  hull stay within x=.06..94, y=.10..90 at 17 m in all eight bearings, through
  15 frames following a swap, and travel distance returns after target removal.

These are settled-composition and swap checks; they do not claim that a newly
acquired target can never cross the margin during the camera's easing interval.

## Ordinary release capture

`Builds/quality/workshop/gullet-framing-01`: whole gate/coil/exit route passes,
783.8515357 s, 47,025/47,025 frames focused. All three chambers and Cantor clear,
then the exit reaches TallowApproach. Cantor lasts 79.1 s. Both heroes survive;
the route's recovery checkpoint adds no new swap/revival evidence in this run.

Opened images: 098, 099, 107, 117, 155, 157. The complete boss body and both ships
are visible in the fight; travel scale returns afterward, and the exit object
and prompt remain clear. The broad uniform blue membrane floor still dominates
the coil. The small clamps and missing collar-release story remain open.
No independent viewer, continuous-motion audition or Nathan approval is claimed.

Captured p95/p99/worst are 17.1504/17.3177/69.9572 ms; capture overhead excludes
this run from clean C1 timing evidence. Nathan's autosave and backup hashes match
the pre-session manifest. The final full integrated PlayMode suite remains due.

Source/artifact hashes: [C4-flight-framing-evidence.json](C4-flight-framing-evidence.json).
