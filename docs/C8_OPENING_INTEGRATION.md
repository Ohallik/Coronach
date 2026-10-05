# Opening chapter integration

October 4, 2026. Implementation underway; **not a completed chapter**. The
[nursery increment](validation/C8-nursery-integration.md) passes both full suites,
the expanded smoke and the fresh eleven-segment ordinary route to Tallow. The C7 workshop
player is frozen separately under `Builds/Workshop/Player`. This plan implements
the nursery connection in [STORY_CAMPAIGN](STORY_CAMPAIGN.md), then makes room for
the connected opening and Cantor release. It does not turn those later scenes
into claims about the current build.

The first playable increment connects the existing places:

1. Orrin's missing deliveries lead to the silent Sorrel outpost. The Survivor
   explains that the transmitter was shut down because the Burrower follows it.
2. Disabling the Burrower opens the cradle and bore. The recovered survey key
   contains an incomplete route; the objective leads into Hushwell.
3. The cave's regular grooves become the egg cradles. After the Bellows, a short
   Taren/Sela conversation connects the grooves to Sela's chart. Finishing that
   conversation records the discovery, calibrates the route and saves it.
4. The service lift returns to Sorrel. The surface objective leads to launch,
   then the Halo beacon opens the Gullet. Returning to the Survivor acknowledges
   the nursery and the decision to leave the drill silent.

The discovery must happen through the ordinary interact and dialogue controls.
Reveal, rapid confirms and cancel follow C6 rules. Cancellation or scene unload
must leave the discovery available, without a reward, calibration or completion
save. A completed discovery is idempotent and survives Continue. The route must
explain why the key alone is insufficient before the player returns to orbit.

Save compatibility is explicit: introduce a new format version, migrate old
states in memory and write only on a normal save action. Preserve earned party,
gear, quests and map clears. Older saves which already own Gullet access retain
it; do not invent a nursery visit or replay rewards to justify that access.
Fresh profiles must discover the nursery. Keep an unmigrated copy and compare
bytes before and after read-only loading in regression fixtures. Nathan's actual
saves remain shielded and are never used as migration write targets.

Before implementation, add broken-state checks for fresh-key bypass, missing
discovery dialogue, cancelled discovery, duplicate completion and legacy access.
Exercise real compiled story nodes and the actual beacon/objective handoff;
avoid tests that merely mirror a new helper's booleans. Cover pre-key, key-only,
mid-Hushwell, discovery-complete, Gullet and Tallow saves, including future-version
rejection. Update the no-pack clone check only if dependencies change.

The first ordinary evidence starts from New Game and carries earned saves through
the cave and back to the Halo. Development loadouts remain named fixtures. A
Hushwell fixture must not pre-grant the discovery it claims to exercise. Retain
the C7 route unchanged as historical evidence and author a distinct C8 route.
Recheck dialogue fit, whole-party gates, boss visibility, both suites and the
changed maps' performance after integration.

Still separate chapter work: the opening walkway repair and Sela's arrival;
more purposeful resident/interior interactions; Burrower's disable finish;
Gullet preview pocket, working collar connections and Cantor release; the
moving-refuge story at Tallow. These need playable implementations, map review
and ordinary-input evidence before `C8_FIRST_CHAPTER_OK`.
