# Coronach — combat animation direction

2026-09-23. Combat is the next production priority. The current pass replaces the slice's repeated walk/swing animation, but it does not establish final combat quality. Expand the campaign only after the first pair can sustain an enjoyable five-minute encounter.

## Implemented foundation

- Separate walk, jog and sprint clips, selected and timed from actual motor velocity. Civil walking is 2.6 m/s; holding sprint reaches 5.4 m/s. Combat retains its faster traversal.
- Taren has three different ordinary swings, a stronger third hit, a short combo input buffer, a held guard pose, roll, stagger, cleave, dash and cast motions. Sela has a mirrored left-arm ranged stance and shot plus skill poses.
- Ground damage waits for the attack wind-up. Dodge and guard cancel a pending swing; pause freezes it. Skills also have a wind-up. The contact window is currently a tuned fraction of action duration, not an authored per-frame weapon trace.
- The generated Shaped vanes now attach to Chest/Hips. Unity previously rejected parenting into the imported model instance, leaving them behind during torso motion. Intake now unpacks that hierarchy before attaching them and verifies the parent.
- Flight uses two dedicated generated spacecraft: Taren's broad ivory/black/amber hull and Sela's narrow cyan/violet swept hull. Banking, boost pitch and barrel roll act on the visible ship, leaving the gameplay collider stable. The transition collapses one form and expands the next over 0.6 seconds; it is not yet a skeletal panel-folding transformation.

Animation sources are the owned CC0 Quaternius Universal Animation Libraries 1 and 2, stripped to skeletons and clips. All visible character and ship geometry remains generated. This is a retargeted working moveset; a library's clip count is not a substitute for authored choreography.

## Next choreography targets

| Fighter / move | Silhouette and purpose | Required mechanical distinction |
|---|---|---|
| Taren — three-cut chain | Short diagonal opener, reverse cut across the body, committed overhead finisher | Clearly separate anticipation/contact/recovery; finisher breaks posture and exposes a punish window. |
| Taren — Rift Step | Shoulder leads a low dash; blade follows in a single long arc | Reposition through a gap, never a teleport through solid walls. |
| Taren — Anchor Break | Both feet plant, vanes brace, a rising heavy strike opens the chest | Slower anti-armour move with a readable charge and interrupt point. |
| Taren — Halo Reap | One complete waist-height sweep, then a firm planted recovery | Crowd control with limited forward reach; no repeated damage on one target from one sweep. |
| Taren — counter riposte | Small guard catch followed by a short, direct thrust | Reward a successful Flash Guard; do not require a long cinematic pause. |
| Sela — needle string | Three left-emitter shots with a progressively stronger braced pose | Reliable movement firing, then a briefly planted precision finish. |
| Sela — Scatter Bloom | Arm crosses the torso, opens outward, five projectiles fan from the hand | Close crowd control with a visible spread, distinct from a piercing line. |
| Sela — Thread Lance | Vanes align and the wrist locks on a target before release | Strong piercing beam with a clear aiming commitment. |
| Sela — Static Net | Throw from a low coil into an open hand, then recoil | Persistent area denial; field placement and activation must be readable. |
| Sela — Refract | Forearm turns a shot aside; hips follow into a return shot | A timing tool whose return animation follows actual interception. |
| Both — ship attacks | Nose-mounted fire; side-mounted energy edge on lunge; barrel roll on evade | Muzzles must follow ship geometry. Boost, braking, lunge and roll each need distinct trails and sound. |

Names and future mechanics above are proposals. Existing four-skill data remains authoritative until each move is implemented and tested. Do not advertise these as an unlocked attack roster.

## Acceptance for the next combat milestone

1. Review at gameplay camera distance and quarter speed: planted feet, no knee inversion, no wrist/weapon separation, no floating vanes and no snap back to idle between chained attacks.
2. Author contact/recovery events per move. Place the visible edge and damaging volume together; reject a hit that arrives before the edge or behind the fighter.
3. Add directional starts, stops and turns, then strafe/backpedal for lock-on. Match stride displacement to ground speed; stop using one forward jog for every fighting direction.
4. Tune cancellation, hit stop, impact sound, enemy recoil and camera response together. Keep the target visible and reserve large flashes for significant hits.
5. Review a full five-minute ground encounter and a flight encounter on the physical Logitech, including rapid taps, movement while attacking, guard/dodge cancellation and character swap. Automated route completion and static pose renders cannot close that feel check.

The current visual-quality rejection remains open. Environment composition, detailed hand/cloth deformation, character-specific movement and attack effects still need work.
