# Coronach workshop - November 3 candidate

**To play:** double-click `Builds/Workshop-Nov03/Play Coronach.cmd` (or `powershell -ExecutionPolicy Bypass -File scripts/workshop.ps1 -Package Workshop-Nov03`) and choose **New Game**. Saves and sound settings stay in `Builds/Workshop-Nov03/Profile`. Your LocalLow saves and the frozen October 4 chapter (`Builds/Workshop-Chapter`) are untouched and still playable.

Built from `a7409db`; `player-manifest.json` hashes all 244 files. The route is the same chapter: Decks → Sorrel → Hushwell nursery → Halo → the Gullet → Tallow, about 40 minutes when played directly.

## New since the October 4 chapter

- **Light and colour.** Each zone now has its own sun colour, gradient ambient and colour grade. Sorrel reads as warm afternoon ground with cool shadows, the Gullet's veins and the station trim glow, and the people you can talk to on the Decks and at Tallow stand in warm pools of light. No blur (D060).
- **The Gullet's boss.** Cantor wears a three-link collar you cut link by link (each cut changes its sweep, song or volley), then the final lock. It is visible, dormant, before the fight, and when released it swims out through the Gullet's exit instead of exploding. The camera keeps both ships and the animal in frame and eases back afterwards.
- **Your companion in flight** keeps clear firing lanes, steers around the tunnel walls, dodges real incoming shots and gives the departing animal room.
- **The Burrower** now powers down mechanically instead of exploding.
- **Tallow** is a moving refuge: Sela can inspect the slack mooring line and its drifting marker (now with a proper coupler at the free end), and the Keeper acknowledges what you found.
- **Movement:** knee snapping, slope balance, idle crouch and turning on terrain were all repaired; holding Brake/Boost/Sprint through a conversation no longer swallows the input afterwards.
- One Drifter fan can no longer hit the same ship three times at once.

## Verified, and what is not

An agent-driven virtual gamepad completed New Game → Tallow in 41:32 with both heroes alive on the preceding build (`729aae0`), and the two changed segments (Decks opening, Tallow) passed again on this exact build ([evidence](validation/C7-nov03-chain.md)). 175 EditMode / 455 PlayMode checks passed on `729aae0`; after the final lighting fix the 3 look checks and 93 affected PlayMode checks pass. That is not human play. Not yet judged by a person: how it feels on your Logitech pad, combat difficulty, sound mix, and whether motion looks smooth on your screen. With displays awake the Decks have shown occasional 34–62 ms frames, still unexplained.

Known rough edges: the Decks and Tallow rooms are still sparse grids; Sorrel has wide empty stretches; Sela can step high on slopes; the Tallow line has no reeling animation; nursery cradles repeat.
