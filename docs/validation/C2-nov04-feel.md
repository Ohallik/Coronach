# November 4 feel fixes: civil gait, sprint, and the opening's NPCs (D197)

Nathan's report (November 4, Logitech Precision): walking and running "through a swamp", sprint turns falling to walking pace, NPCs that would not talk.

## What the evidence showed

- **NPCs.** His Player.log (`LocalLow/Nathan/Coronach`) shows the pad's south button reaching `PromptService.TryInteract` (two docks, two launches). On the Decks the opening had disabled Orrin, Mira and Sela, and only offered the repair after Sela finished crossing the market.
- **Gait.** The C2 sole trajectories (`Builds/quality/C2/sole-trajectories/feet.csv`) put the owned walk at ~1.0 m/s (1.5 steps/s) and the owned jog's planted-foot speed at ~6 m/s (2.1 steps/s, long flight phase). Civil travel at 2.6 m/s pinned the walk at 1.7× cadence with a 1.39× step and up to 23 cm of hip drop; the 5.4 m/s sprint played the jog at 0.85× (slow motion).
- **Sprint.** Thrust drained 34/s while regenerating 17/s; once empty the sprint flickered on for one frame in three. The motor blended velocity in a straight line, so a 90-degree turn passed through 71% of its speed.

## Checks (red on the committed code first)

`Builds/quality/C2/nov04-feel/playmode-new-checks-on-committed-RED.xml`: all seven new checks failed for the reported reasons.

| Check | Committed | Fixed |
|---|---|---|
| Orrin / Mira / Sela talk during the opening (holding line, nobody marked met, no quest) | unavailable | pass |
| The job is offered once Sela sets off; she finishes her walk under its dialogue | not offered | pass |
| Civil sprint pace after 5.5 s held | 5.4 → 4.54 m/s | 6.60 → 6.60 |
| 90-degree combat sprint turn, slowest speed | 7.35 of 10.39 m/s | 10.38 of 10.39 (reversal still brakes: 0.45) |
| Spent combat sprint, fastest while refilling | 7.81 (flicker) | 6.70; resumes at 29.8 Thrust, 10.39 m/s |

Gait regressions now run at the new civil paces (1.5 / 4.5 / 6.6 m/s). The first run at a 1.6 and then 1.5 m/s walk failed the knee-snap bound on flat ground (1220-1245 deg/s, kept as `editmode-civil-walk-1.5-without-bend-reserve-RED.xml`): at the owned stride the landing leg locked straight. With a 2 cm walking bend reserve the peaks are 1010 (Taren) and 915 (Sela) deg/s; the civil jog peaks at 1192 / 1151 (bound 3000). Rendered soles stay within 3 mm while walking, jogging, firing on the move and on 10/39-degree slopes.

Results: EditMode 194/194 (`editmode-green.xml`); targeted PlayMode 17/17 (`playmode-feel-green.xml`). Full PlayMode and the chain follow with the batch.

Not judged: how the new gait looks in motion on Nathan's screen, and how the Precision feels; both need a person.

**Full PlayMode on `a7f25e0`: 472/475** (`playmode-full-on-a7f25e0-RED.xml`). Two Taren slope cases regressed from the walking bend reserve (downhill braking 1225 deg/s, uphill start 6.1 m/s on 39 degrees: steep support already keeps 4 cm of bend in the solver), and the dialogue sweep's outcome table assumed every Mira node opens her stock. The reserve now fades out with the terrain blend, and `MiraOpening` is listed as returning to the world. EditMode 194/194; the stride and dialogue-input classes pass (`playmode-fixup-green.xml`).
