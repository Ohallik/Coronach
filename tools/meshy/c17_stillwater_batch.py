#!/usr/bin/env python3
"""Chapter 10, A Route With Room To Turn Back: Mother of Doors, the Tollkeeper, Meret's three fights, the Compact's crews,
and Stillwater's clamp and chains (docs/chapters/ch10-a-route-with-room-to-turn-back.md,
docs/writing/ch10-manifest.md).

Nobody new speaks in this chapter. Stages and their order are tools/meshy/batch_stages.py's; tools/c17_intake.py takes
the pieces from here.

  python tools/meshy/c17_stillwater_batch.py refs --only compact-frames     # Meret's gantry frame and a Compact Warden, one sheet
  python tools/meshy/c17_stillwater_batch.py isolate --only compact-frames
  python tools/meshy/c17_stillwater_batch.py models --only MeretGantry,CompactWarden
  python tools/meshy/c17_stillwater_batch.py rig --only MeretGantry,CompactWarden
  python tools/meshy/c17_stillwater_batch.py reskin --only MeretWell
  python tools/meshy/c17_stillwater_batch.py refs --only anchor-board
  python tools/meshy/c17_stillwater_batch.py isolate --only anchor-board

Mother of Doors is the Cantor's kind grown old: a new head here, and the Cantor's body and tail segments in her grade
(the intake script's). The Tollkeeper is a hull and one outrigger the fight places round it and moves; the Continuance
is the Tollkeeper's hull in Meret's own colours at twice the size, the anchor's four clamps Oriel's Bridle clamp in
Compact white. Meret's well frame is a coat on her own body, so the duel is plainly her. The Compact Wardens are
chapter 9's crews too.
"""
from __future__ import annotations

from batch_stages import BOARD_STYLE, BODY_FLAT, ROOT, Batch

GEN = ROOT / "art-src" / "Generated"
POSE = (" Each figure: front-facing orthographic full-body view, head straight aligned with torso, both arms straight "
        "horizontally in a T pose, legs clearly apart with a white gap between them, feet shoulder-width apart and pointing "
        "forward. ")
COMPACT = ("The Anchor Compact builds in clean white #EEF2F2 with dark teal #1E6E78 bands, pale cyan #8FF4F8 lights and "
           "brass-gold #C8A24A director's trim, clean, well-made and slightly clinical, as in the references. ")

FRAMES = (
    "Use case: stylized-concept. Asset type: 3D game-character modeling reference sheet for Coronach. Wide landscape "
    "canvas with TWO separate full-body figures standing side by side, each centred in its own half, with a WIDE empty "
    "white gap between them so that nothing of one comes near the other; both entirely inside the image with white "
    "margin. No extra views, no scenery, no text. " + COMPACT +
    "LEFT figure: DIRECTOR MERET VENN IN HER HEAVY ENGINEERING FRAME, a walking maintenance exoframe half again a tall "
    "person's height. Meret herself, the woman of the FIRST reference (a composed Vael woman in her sixties, slate-grey "
    "skin, silver fronds cropped close, her white coat with teal piping), stands INSIDE the frame's open cockpit cage, "
    "visible from the knees up behind thin bars. The frame round her: broad white armoured shoulders with teal bands and "
    "gold trim, a heavy chest housing with one round cyan core light, thick armoured legs with wide flat feet, and TWO "
    "huge mechanical ARMS held straight out: ITS LEFT arm (the viewer's right) ends in a massive square hydraulic PRESS "
    "hammer, ITS RIGHT arm (the viewer's left) ends in a long WELDING nozzle with a cyan-lit tip; at each shoulder a big "
    "round bolted ARM COUPLING ringed in teal. "
    "RIGHT figure: a COMPACT WARDEN, one of Meret's enforcement crew in a sealed frame: a person of ordinary size entirely "
    "enclosed in smooth white armour with teal bands, a rounded helmet with one wide dark teal visor and no face, tidy "
    "and orderly; strapped flat along the outside of ITS LEFT forearm (the viewer's right) a tall rectangular white RIOT "
    "SHIELD with a teal stripe, lying HORIZONTAL along the arm in this T pose; fixed along ITS RIGHT forearm (the viewer's "
    "left) a short STUN-BATON with a glowing cyan tip, projecting a hand's length past the fist. "
    "The second reference is the Compact's own rescue craft, for the colours and finish." + POSE +
    BODY_FLAT.replace("ONE single isolated character only", "exactly TWO isolated figures"))

BOARD = (
    BOARD_STYLE + "Six separated concept cells in a 3x2 grid, ONE whole isolated object per cell, each drawn large in its "
    "cell in a three-quarter view from slightly above, with substantial white gutters between cells and nothing touching "
    "or overlapping another cell. 3D model references for a game seen by a camera high above, often against dark space, "
    "so each has ONE bold simple silhouette, big flat colour shapes and lit parts. " + COMPACT + "TOP ROW: "
    "(1) MOTHER OF DOORS' HEAD, the head of a vast old space-faring creature, an adult of the young creature in the "
    "first reference, grown huge and ancient: a long armoured serpent head with a crown of tall swept-back crystal "
    "plates, deep violet-blue #3E3A7A armour gone pale and pearly #CFC8E8 at the edges with age, glowing cyan seams, a "
    "heavy jaw, and along both sides of the head rows of great arched DOOR-LIKE apertures lit warm gold from within, as if "
    "she carried a town's doors; ending in a round neck socket where her body continues; frightened, not cruel; "
    "(2) TOLLKEEPER HULL, an anchor-fleet carrier: a long heavy armoured white Compact capital ship, blunt and "
    "fortress-like, with four big round empty OUTRIGGER MOUNTS along its flanks where arms attach, rows of teal-lit "
    "windows, a raised bridge tower with gold trim, gun turrets along its spine; "
    "(3) TOLLKEEPER OUTRIGGER, ONE detached outrigger arm of that carrier: a long jointed white boom with a round mount at "
    "one end and a heavy CAGE-GATE BRACE at the other (a lattice of white bars with a teal-lit GATE MOTOR housing), "
    "black-and-yellow warning stripes on its joints; "
    "BOTTOM ROW: "
    "(4) MERET'S CRAFT, the Director's own small armoured craft: a compact elegant white-and-gold hull like a broad "
    "wedge, a teal canopy, and THREE round SHIELD PROJECTORS on short pylons around it (one at the nose, one on each "
    "wing), each a ring glowing cyan; orderly and formidable; "
    "(5) STILLWATER CLAMP, an immense engineering clamp built to pin a living space-road: a huge white C-shaped frame "
    "with four great curved JAWS closing inward like a hand, braced by heavy struts, teal bands, gold rivets, rows of "
    "cyan warning lights, standing on a broad base; "
    "(6) ANCHOR CHAIN, three links of a colossal anchor chain lying in a gentle curve: each link a massive white oval "
    "ring as tall as a house, with teal bands and gold collars, worn and scored where it has pulled. "
    "The second reference is the Compact's own rescue craft, for the colours and finish.")

TAIL = (". Original UV islands; flat clean cel-shaded game texture, no baked lighting.")
MERET_BODY = "art-src/Generated/C12-ManyHands/MeretBody_clean.fbx"

BATCH = Batch(
    name="C17-Stillwater", prompt_id="C17",
    bodies={
        # Figures on the compact-frames sheet (batch_stages: a body with no prompt is cut out of a board). Both carry
        # their tools fixed along a forearm, so both are rebuilt as drawn.
        "MeretGantry": ([], "MeretGantry", None, 3.2,
                        "Director Meret Venn in a heavy white and teal engineering exoframe, open cockpit cage, press hammer "
                        "arm and welding arm, round arm couplings at the shoulders, T pose"),
        "CompactWarden": ([], "CompactWarden", None, 1.95,
                          "Compact Warden, person in smooth sealed white armour with teal bands and a dark visor, riot shield "
                          "along the left forearm, stun baton along the right forearm, T pose"),
    },
    as_drawn=("MeretGantry", "CompactWarden"),
    boards={
        "compact-frames": (FRAMES, [GEN / "C12-ManyHands/refs/Meret_Body.png", GEN / "C12-ManyHands/refs/CompactCutter.png"],
                           ["MeretGantry", "CompactWarden"]),
        "anchor-board": (BOARD, [GEN / "P1-production/refs/cantor.png", GEN / "C12-ManyHands/refs/CompactCutter.png"],
                         ["MotherOfDoors", "Tollkeeper", "TollkeeperOutrigger", "MeretShip", "StillwaterClamp", "AnchorChain"]),
    },
    models={
        "MotherOfDoors": ("hero", 12000), "Tollkeeper": ("hero", 12000), "TollkeeperOutrigger": ("prop", 6000),
        "MeretShip": ("hero", 9000), "StillwaterClamp": ("hero", 10000), "AnchorChain": ("prop", 6000),
    },
    reskins={
        # The duel: Meret in her compact emergency frame, painted on her own body so that she is plainly herself.
        "MeretWell": (MERET_BODY,
            "Director Meret Venn in a compact emergency frame: her slate-grey Vael face and cropped silver fronds bare; over "
            "her white coat a light exoframe of brushed white armour plates with dark teal bands and thin gold trim on the "
            "shoulders, chest, forearms and shins, a round cyan core light at the sternum, teal-lit joints at the elbows and "
            "knees, slate-blue trousers, white armoured boots. White, teal, gold and slate only", TAIL),
    },
)

if __name__ == "__main__":
    raise SystemExit(BATCH.main())
