# Remaining ordinary workshop routes — October 3, 2026

Codex, agent-directed virtual gamepad replay through the ordinary Input System.
**Traversal PASS; overall visual acceptance remains OPEN.** No human play,
continuous-video viewing, audio audition or physical-controller feel is claimed.
Each player capture ran alone as a blocking call on Nathan's RTX 5070 / Ryzen
9800X3D, release player, Ultra, 1920×1080, 60 fps cap, VSync off. These recorded
runs are traversal evidence, not clean C1 timing.

| Route / folder under `Builds/quality/workshop/` | Result | Evidence |
|---|---|---|
| `sorrel-service-01` | PASS | 260.13 s; 15,608/15,608 frames focused; analyzer valid. Level 3 moon fixture, two Scrapmite packs and two Sentinels (`Sorrel_8..11`), service trail both ways, repair/save on return. The nearby haul hounds also followed and were cleared. Taren fell; Sela finished and revived him. |
| `sorrel-hushwell-01` | REJECTED | The first route stopped short of encounter 6's trigger and then asked to walk through the solid anvil. Both navigation and the missing encounter flag rejected it, despite reaching Hushwell. |
| `sorrel-hushwell-02` | PASS | 208.71 s; 12,518/12,518 focused; analyzer valid. Reachable level 5 warp-key fixture, all four haul encounters (`Sorrel_4..7`), anvil's south operator stance and west bypass, cleared Burrower excavation, bore interaction and first Hushwell waypoint. |
| `gullet-gate-coil-exit-01` | REJECTED | All three chambers cleared, but after lateral rolls the replay fired sideways. Taren fell, Sela stopped damaging Cantor at 5,623 HP, then also fell. Later input returned to title; recorder validation threw on the absent party and timed out. Raw incomplete video and log retained; no run report or timing acceptance. |
| `gullet-gate-coil-exit-02` | PASS | 784.98 s; 47,093/47,093 focused; analyzer valid. Reachable level 5 warp-key fixture, all three chamber flags, Cantor in 80.3 s, Taren down/Sela finish, revive, exit valve and TallowApproach in CivilFlight. Sela contacted the throat wall three times during the earlier section; the route still completed with both alive. |

Routes are reproducible with `python tools/workshop_routes.py`. Existing chamber
circuit geometry comes from `tools/gullet_routes.py` and `GulletProfile`.
The route fixtures retain their encounter, focus, scene and form assertions.
The successful Sorrel runs used the rebuilt `b7eb524` runtime; the Gullet success
adds the recorder fixes below. Per-run `build.json` records assembly and packaged
asset hashes even while the source change was not yet committed.

## Recorder defects: red before green

`ContinuousReviewTests` reproduces both defects:

- With no party on the title screen, checkpoint validation must record an explicit
  rejection and preserve the evidence-writing path. The old code throws instead.
- After a lateral roll, a flight chase within emitter range must turn back toward
  its target through actual stick input and land projectiles. The old chase stops
  steering once in range. A short 0.25 stick correction now clears the input dead
  zone and motor threshold, then releases when aligned within 5 degrees. It does
  not rotate actors directly or change player aiming/combat rules.

The first test fixture also needed to release the scene's UI input latch before
holding fire. After correcting that setup, disabling only the aim correction
again failed with fire held, facing east, target north, and target health still
1,000/1,000. Both corrected tests pass. Evidence:
`Builds/quality/workshop/replay-regressions/{red,aim-mutation-red,green}.{xml,log}`.
The initial compile failure and latch-bound attempts are not accepted negative
controls for aiming; the final isolated mutation is.

The first full PlayMode run was **187/191**, rejected. The new replay fixture
had never been active, so Unity did not call its `OnDestroy`; its synthetic
gamepad remained on the input bus holding confirm and broke four later input
tests. Teardown now explicitly removes that device. Running the replay, core
and pad-support classes together then passes **32/32**, including all four
failures. The original full-suite rejection is retained in
`replay-regressions/full-red/`; the combined rerun is `cleanup-green.{xml,log}`.
A later full-suite run remains required before the integrated workshop gate.
The full EditMode suite passes **56/56** after this correction.

## Actual opened views and remaining presentation work

Opened service views `004`, `030`, `044`, `061`; bore views `057`, `060`, `062`,
`063`; Gullet views `095`, `098`, `099`, `107`, `117`, `155`, `157`, `159`.

- The service route has working equipment along its side and traversable bends,
  but large stretches are bare, uniform orange ground. Grass and terrain-surface
  variation should reinforce the landscape and travel line, keeping fights clear.
- The anvil has an accessible southern operator stance and a western passing
  lane. The bore prompt works and leads to the cave, but the scaffold appears to
  stand over intact sand. It needs a visible excavation/opening.
- The nursery-to-coil passage and final valve are traversable. The final lips
  frame a clear exit object and prompt, followed by the station approach.
- **The coil fails combat framing:** Cantor's head is off the top of the frame in
  `099`/`107` at ordinary firing range. The visible chamber often reads as a flat
  membrane field with its meaningful edges outside the camera. A green route
  does not accept this; correct flight framing and recapture before closing C4
  or the Gullet's full final MAP_EYE_TEST.

Nathan's two ordinary save files were SHA-256 checked against the pre-run
manifest after the captures and are unchanged. Manifest:
`Builds/quality/session-2026-10-03/save-hashes-before.json`.
Small artifact/source hash index: [MAP-remaining-routes-evidence.json](MAP-remaining-routes-evidence.json).

## Landscape recapture (October 3, later)

D131 adds low grass to Sorrel and a real compact terrain cut below the bore scaffold.
The final service route (260.149 s) and bore-to-Hushwell walk (208.641 s) pass again
with full focus. Hushwell's complete descent, nursery and lift also pass after
D133 adds ordinary dodge input to its Bellows rounds (410.834 s). Opened captures
confirm dry paths around the new nursery water. [Surface evidence](C8-landscape-surfaces.md)
records the rejected runs and exact scope. Broad bare stretches of Sorrel remain
chapter-polish work; Cantor framing remains open.

## Flight framing recapture (October 3, later)

The off-screen Cantor defect is repaired in D134. The complete Gullet route
passes again in 783.852 s, 47,025/47,025 frames focused; both heroes survive.
Actual mesh viewport checks also cover eight bearings and the hero swap that
initially reset the composition. Opened fight images retain the full boss body
and readable ships. The coil's uniform floor and weak clamps remain open visual
work. [Framing evidence](C4-flight-framing.md) separates that scope from traversal.
