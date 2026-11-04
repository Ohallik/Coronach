# November 4 batch: the opening, exteriors, Cantor coil, Tallow reel, balance (D194-D196)

Status:
- **188 EditMode and 470 PlayMode tests pass, with nothing skipped.**
- The chain passes on the published build ([chain](C7-nov04-chain.md)).
- The opening passed its in-player eye test by agent review.
- Sela's uphill fold is diagnosed but not fixed ([fold](C2-uphill-fold.md)).

## Built

**The first morning (D195)**

On a genuine New Game, the office-to-market bridge is closed for Taren's repair:
- Amber gates close the bridge and the service cover stands open.
- Mira's generated cart sits on the yellow line.
- Sela walks across the market as Taren arrives.
- The repair conversation plays, with a deck tremor and a creak on the line about the bridge moving.
- The gates then fold back, Sela walks on to wait for Orrin and Mira returns to her bay.

Rules around it:
- Orrin stays unavailable until the scene has played.
- Saves made before this change and controlled fixtures never see it.
- The chapter-01 route gains seven inserted steps; no earlier step changed.
- `docs/writing` held stale copies of `Decks`, `Halo` and `Sorrel` and was missing `SelaNursery`. It now matches the shipped dialogue, so a world rebuild cannot revert those lines.

**Places (D196)**

| Place | Change |
|---|---|
| Halo roofs | 41 collider-free pieces plus 39 details, 0 `ExteriorAudit` problems |
| Tallow refuge deck | Flat details under the flight ceiling, 0 problems |
| Hushwell lower ramp | Six-step floor ramp and seam crystals; the shaded floor no longer reads black |
| Gullet coil | Scour arcs fade out |
| Gullet exit | Grooves lead to the aperture; a ring of pores around it |

**Actors (D196)**
- The dormant Cantor rests coiled.
- Tallow's line is a generated braided rope, 9 Meshy credits ([ledger](../art/C8-nov04.json)).
- The line reels in once the party has observed it.

**Balance (D194)**
- Burst skills deal 2.6× output.
- The Gullet's ordinary enemies deal 1.2× damage. Hushwell was tried at 1.25, but Taren fell in the chain, so it stays at 1.

## Checks

**New checks were red on the committed scenes and code first** (`Builds/quality/C8/nov04/playmode-new-checks-on-committed-RED.xml`, `playmode-reel-length-on-committed-RED.xml`):
- the straight resting Cantor;
- the zone threat in Hushwell and the Gullet (Sorrel passing as the control);
- all four opening tests (no scene on the Decks);
- the line that never reels (paid-out length unchanged at 17.83 m).

**Retained failures:**
- The first reel test measured straight-line distance. The free end first moves away along the doubled-back bight, so the test was rewritten to measure paid-out length.

**Builder results:**
- Hushwell: 51 pieces, 0 problems; ramp crystals all placed.
- Gullet: 164 renderers, all below the flight ceiling.
- Opening staging: both civilians' paths are swept against the real scene each time it runs.
- The opening's cart is 1.35 m tall (D119).

**Stills opened:**
- `Builds/quality/look/nov04-places-01` and `-02`: ramp, exit, coil, refuge, Halo overview and roofs.
- `nov04-opening-01` to `-03`.

## In-player eye test: the opening

Reviewer: Claude (agent review of captured play; not Nathan).

Run: `Builds/quality/C8/nov04/eye-opening-01` (`51cdd87`), then `eye-opening-02` (`f931c48`). Both used the release player, ordinary chapter-01 input and full focus, with every step opened.

What the frames show:
- After docking, the objective reads "Reopen the market walkway".
- The amber barrier closes the bridge with the lid propped open.
- Sela arrives at the barrier from the market, and Mira stands at her cart.
- Once Sela is there, the prompt appears and the conversation plays.
- The gates fold and the lid closes; the objective moves to Orrin.
- Sela walks back along the walkway, and Orrin's conversation brings her aboard as before.

**Rejected in `-01`:** only a stub of the yellow line showed, because the deck runners covered it. `f931c48` raised it, and `-02` shows the line crossing the walkway under the cart.

**Not judged from stills:** the tremor, the creak, and the feet of a walking civilian. Sela's walk uses her measured stride profile; Mira has none.

## Not done

- **Resident interiors and town loops.** These need new generated resident bodies (the Meshy character pipeline). Only named characters have bodies today.
- **The Cantor's in-player preview composition.** Its eye test is still pending.
- **Human play.** Feel, sound and balance still need a person.
