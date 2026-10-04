# Ground boss visibility — October 4

**Pixel, focused regression and ordinary release recaptures PASS.**
The accepted earned-loadout Burrower fight (`c7-burrower-03`, frame 027) hides
Taren behind the generated body at a normal melee bearing. The fixed ground
camera stays at its authored angle and distance. Boss collision, attacks and
health are unchanged.

`BossOcclusion` uses the existing owned toon shader's compact screen ellipse.
Only the portion of a blocking Burrower or Bellows over the controlled ground
hero fades. It eases back to opaque when clear. Both owned materials retain the
shader variant in release builds; fresh generated intake preserves that setting.

The actual raster probe renders both generated heroes behind both generated
bosses at the ground camera's angle, field of view and distance. A magenta hero
mask measures visible pixels, rather than trusting a shader property alone.

| Hero / boss | Original visible body | Repaired visible body |
| --- | ---: | ---: |
| Taren / Burrower | 19.1% | 84.8% |
| Taren / Bellows | 0.0% | 81.1% |
| Sela / Burrower | 17.1% | 84.5% |
| Sela / Bellows | 1.4% | 81.8% |

All four original cases fail the unchanged 55% minimum. The repaired images
change zero pixels outside the local ellipse. Removing the fade fails visibility;
expanding it across the frame fails preservation of the rest of the boss.
These diagnostic masks are not gameplay artwork or a substitute for the player view.

Two PlayMode tests first fail against the original runtime. Removing the flight
guard fails the third test. The final combined visibility, flight framing and
encounter-entry run passes **9/9**, zero skipped. A first pixel attempt caught an
invalid Unity property-block field initializer; allocation now occurs on use.
The first combined run also exposes a fixture error: a live character controller
can restore its cached pose after a direct test teleport. Resetting that controller
around the fixture move, and asserting the destination, establishes a real clear
sightline. The fade assertions and camera limits are unchanged. Both rejected
runs are preserved. Nathan's original save hashes still match.

Raw captures, fault controls, XML and logs are under
`Builds/quality/workshop/boss-*`; [file hashes](C3-boss-visibility-evidence.json).


## Ordinary release recaptures

Release build from `f0a601d`, original route inputs and tolerances:

- `c7-burrower-visibility-01`: **185.558 s, 11,115/11,115 focused frames**.
  Actual Title/Continue loads the accepted `c7-drill-equipment-01` slot1.
  Burrower falls in 53.7 s; both heroes survive, key and autosave secured.
  Opened 025 shows Taren through the blocking head; 027/032 show the body opaque
  after separation. This is earned-loadout evidence, with no state edits.
- `hushwell-boss-visibility-01`: **397.867 s, 23,871/23,871 focused frames**.
  Ordinary input from the documented gullet loadout fixture clears both galleries,
  both live pressure chambers and organs, then Bellows in 79.3 s. Both heroes
  survive, discover the nursery and return by lift. Opened 073 shows the local
  hero window; 078 shows the intact unobstructing body; 104 shows nursery arrival.
  This fixture run is not an earned New Game continuation.

Both runtime reports and independent offline analyses pass. The fixed ground
camera and all combat tuning remain unchanged. These opened captures establish
visibility, not continuous motion/audio or physical-controller acceptance. Final
integrated suites and remaining map polish still belong to the C7 gate.
