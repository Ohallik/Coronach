# Flight direction and contact contract

September 27, 2026. Implemented baseline: `abaf4be`. C4 presentation/handling acceptance remains open. This records the actual current rules for checking drift and reversal, not a claim that physical-controller feel has passed.

| Input or state | Current behavior |
|---|---|
| Directional thrust | The nose follows the last nonzero movement input. Thrust accelerates in that direction; existing momentum can continue sideways or backwards during a turn. Releasing input retains the last nose direction and coasts. |
| Aiming and ordinary fire | Target selection does not rotate the ship. Aim assistance accepts a living target within 30 degrees of the nose. Fire releases from the actual banked nose socket and points toward the target body when that assistance remains valid; otherwise it follows the sampled input heading. Cover between the ship and muzzle also blocks release. |
| Boost | Acceleration is multiplied by 2.1 and the speed limit by 1.9. Zero directional input adds no thrust or exhaust; retained momentum alone does not light the engines. The ordinary thrust resource still follows the held boost input. |
| Brake | Exponential damping rises from 0.75 to 9 per second. Directional thrust remains available while braking, allowing slow approaches and deliberate counter-thrust. This is the ordinary input used by the replay's navigation steps. |
| Lunge / Ember Dash | A lunge turns toward its target, or the nose direction without one, and requests 8 m over 0.16 s. Ember requests 10 m. Collision limits the actual travel; damage follows the completed travel segments and the visible hull-sized edge, with cover and one hit per victim. The movement does not create contacts in untravelled space. |
| Roll | Directional roll requests 4 m of dash travel and a 0.3 s visual barrel roll. Banking and roll rotate the visible hull only. |
| Wall contact | Civil collision causes a bounce without damage. Combat collision deals 12 damage with the existing 0.6 s contact cooldown. Contacts with other combatants do not count as walls. |
| Collision and formation | Stable spheres contain the measured generated hull through banking: Taren 1.78 m, Sela 1.69 m, centred 0.8 m above the actor root. Matching damage bodies make the visible width vulnerable. Arrivals use 3.7 m separation; adjacent assistance retains its two-second hold. Landing restores the original ground capsule. |

[FlightMotor](../Lattice/Assets/_Project/Scripts/Combat/FlightMotor.cs) integrates thrust/drag in bounded substeps using average velocity for displacement. Matched 30/60/120 checks are recorded in the C4 integration evidence. Acceleration, drag and speed caps were preserved; correcting the old end-velocity calculation deliberately recovered about 9.8 cm of discarded 60 fps braking travel. The later hull footprint is a separate deliberate collision/damage change.

The compact generated silhouettes, measured muzzle/nozzle attachments and stable collision body are implemented. Ordinary player recaptures now show separate hulls after regrouping. The current exhaust is too faint at gameplay scale despite passing its attachment checks; the old shrinking form replacement and remaining flight skill/disable presentation also require work. Long civil/combat circuits, camera behavior during reversal/wall contact and all later edits require their own recorded results. Normal/slow video viewing, sound audition and physical Logitech feel remain UNVERIFIED.
