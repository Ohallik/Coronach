# Coronach QA issues

Format and severity: [TEST_PLAN.md](../TEST_PLAN.md) section 2 and section 5. Evidence folders are under `Builds/quality/QA/`.

| ID | Sev | Area | Zone | Summary | Repro (route/step or steps) | Status | Fix commit |
|---|---|---|---|---|---|---|---|
| QA-001 | Blocker | Combat | Any | A level-up from the killing blow raises a downed hero's integrity without a revive: they read as alive at +18 but stay frozen, and the partner can't revive them | `base/s06`, chapter-06 steps 60-65; `PartyController.RefreshStats` | Open | |
| QA-002 | Major | UI | All | Interaction prompts, barks and control hints have no backing and wash out on bright floors | `smoke/s01/006`, `base2/s07/055` | Open | |
| QA-003 | Major | UI | Gullet, Hushwell | The boss bar says BELOW GROUND for any untouchable boss, including the Cantor in space while its collar holds | `base2/s10/108` | Open | |
| QA-004 | Major | UI | All | No way to leave a running game: no return to title or quit from the pause menu | `smoke/s02b` tabs; `PauseMenu.cs` | Open | |
| QA-005 | Major | UI | All | The level-up and task card's text collides with its frame: heading over the top border, body past the left edge | `base/s04/049` | Open | |
| QA-006 | Major | UI | Hushwell | Pressure organ target line reads BREAK 124 / 100000 | `base2/s08/064` | Open | |
| QA-007 | Minor | Barks | All | Every zone load plays the same line, "Same body. Different problem." | Any `chapter-*` step after a load | Open | |
| QA-008 | Minor | UI | Title | The save picker leaves the main menu showing behind it, overlapping its heading | `smoke/s02/003` | Open | |
| QA-009 | Minor | Art | Sorrel | Ridge crystals render as flat overexposed cyan with no facets | `base/s05/009` | Open | |
| QA-010 | Major | UI | All | The selected menu row barely differs from the others on a pad | `smoke/s02/005`, `smoke/s02/017` | Open | |
| QA-011 | Minor | Tooling | Sorrel | chapter-03 fails when the party falls earlier than its legs expect | `base/s03` | Open | |
| QA-012 | Major | Balance | Sorrel | The Burrower beat the replay party in 2 of 4 attempts this pass | `base/s06b`, `base2/s06` | Open | |

## Ideas for Nathan

