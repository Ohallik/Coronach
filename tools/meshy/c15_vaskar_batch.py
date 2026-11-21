#!/usr/bin/env python3
"""Chapter 8, Harrowgate (the Vaskar Reach): the Slag Colossus, Drask Vaal's war-frame, the Unsworn, and the Picket's,
the Flows' and the Foundry's pieces (docs/chapters/ch08-harrowgate.md, docs/writing/pending/ch08-manifest.md).

The five portrait sheets (Drask, Tovra, Odrun, Pip, Bryn) are tools/meshy/species_batch.py's. Stages and their order
are tools/meshy/batch_stages.py's; tools/c15_intake.py takes the pieces from here.

  python tools/meshy/c15_vaskar_batch.py refs --only SlagColossus      # look at it before the mesh
  python tools/meshy/c15_vaskar_batch.py models --only SlagColossus
  python tools/meshy/c15_vaskar_batch.py refs --only Drask_Frame
  python tools/meshy/c15_vaskar_batch.py models --only DraskVaal
  python tools/meshy/c15_vaskar_batch.py rig --only DraskVaal
  python tools/meshy/c15_vaskar_batch.py reskin --only TovraBody,DraskBody
  python tools/meshy/c15_vaskar_batch.py refs --only scoria-board
  python tools/meshy/c15_vaskar_batch.py isolate

Drask's war-frame is one new body, rebuilt as drawn so the lance and the shield survive; the Muster Pikeguards are the
same frame in plainer colours (the intake script's grade), because they are his guard wing. Tovra and Drask out of the
frame are coats on Brekk's smith's body, Odrun a grade of it; Pip is a grade of Hessa's. The Warmaster's skiff is the
Lancer in his colours, the Forge Hound a grade of the Bridle-hound it copies, the Sensor Buoy a grade of the survey
beacon, the Flak Gate a grade of Oriel's rail-gate, Harrowgate's houses a basalt grade of Lanternwick's canyon house.
Bryn Asta is heard only by radio here; her body is chapter 9's.
"""
from __future__ import annotations

from batch_stages import BOARD_STYLE, BODY_FLAT, BODY_HEAD, HULL_TAIL, ROOT, Batch

GEN = ROOT / "art-src" / "Generated"
REFS = GEN / "C15-Vaskar" / "refs"
POSE = (" Front-facing orthographic full-body view, neutral expression, head straight aligned with torso, both arms straight "
        "horizontally in a T pose, legs clearly apart with a white gap between them, feet shoulder-width apart and pointing "
        "forward. Entire figure, both crest-sails and everything on both arms comfortably inside the image with white margin. ")
SINGLE_HEAD = (
    "Use case: stylized-concept. Asset type: ONE isolated 3D game-model production reference for Coronach, for Meshy "
    "image-to-3D reconstruction. OPAQUE PURE WHITE background, the whole object centred with clear white margin, filling "
    "80 percent of the frame. Flat cel-shaded game art with broad matte colour regions, crisp restrained linework and "
    "black panel lines, in exactly the art style, line weight and finish of the references. No ground, cast shadow, "
    "gloss, highlights, reflections, bloom, gradients, particles, sparks, flame, smoke, labels, text, inset or "
    "turnaround. All parts physically attached; nothing floats. ")

COLOSSUS = (
    SINGLE_HEAD + "Drawn in a single three-quarter FRONT view from 40 degrees above, so its top shows. NOT anything in the "
    "references: the first is the raiders' own camp gear, shown for their materials, the second a different machine "
    "shown only for its industrial finish. THE SLAG COLOSSUS, a walking crucible war-engine that guards a foundry's "
    "heart: a huge round iron CRUCIBLE for a body, like a giant cauldron, brimming at its open top with glowing molten "
    "metal #FFB347 to #FF6A1E; carried on FOUR massive short stumpy iron legs with broad round feet, splayed for weight; "
    "a furnace grille in its front glowing orange; and TWO long heavy POUR-ARMS reaching forward from its shoulders, each "
    "a jointed iron arm ending in a deep tilted LADLE spout that drips glowing slag. Armour plates of dark iron #3A3634 "
    "with rust-red #8A3A26 edges and black-and-yellow hazard stripes on the arms, every seam between plates glowing "
    "red-hot #E2501E; heavy rivets, chains, a stack of three short chimneys behind the crucible. From above it reads as "
    "a glowing round mouth of molten metal with two long arms forward and four stumps. About 7 m tall, menacing and "
    "slow.")

DRASK = (
    BODY_HEAD + "The FIRST reference is his portrait sheet; the SECOND shows another man of the same people out of armour: "
    "copy from it how a Vaskar is built and how the crest-sails sit on a full figure, nothing else. Warmaster Drask Vaal, "
    "an adult Vaskar man in his fifties, the largest person in any room and the stillest, in his WAR-FRAME. The Vaskar are "
    "tall and heavy-boned with skin grown in large overlapping plates. Keep his exact head from the portrait, BARE, no "
    "helmet: plates the brown of old bronze worn smooth at the brow, deep-set amber eyes, a host's calm face, and his two "
    "tall membranous CREST-SAILS rising from the back of the skull, nicked along the trailing edge, carried half open. The "
    "war-frame: heavy powered armour of dark iron #3C3A38 plates with old-bronze #9A7440 edging and rivets, big rounded "
    "pauldrons, a deep armoured chest, a broad belt, armoured thighs and shins, heavy square boots; across the chest his "
    "plain crimson #8A2A20 soldier's sash with two brass Warmaster's tabs. On HIS LEFT forearm (the viewer's right) a "
    "heavy oval SHIELD of dark iron with a bronze rim, bolted flat along the outside of the forearm; in this T pose it "
    "lies HORIZONTAL along the arm from the elbow to a hand's length past the fingertips. Fixed along the top of HIS RIGHT "
    "forearm (the viewer's left) his LANCE: a long straight dark-iron war-lance, part of the frame, projecting a full arm's "
    "length beyond the fist and ending in a long broad leaf blade. Both hands open in armoured gauntlets. Dark iron, old "
    "bronze and one crimson sash, lighter than a black floor." + POSE + BODY_FLAT)

BOARD = (
    BOARD_STYLE + "Six separated concept cells in a 3x2 grid, ONE whole isolated object per cell, each drawn large in its "
    "cell in a three-quarter view from slightly above, standing directly on the white ground, with substantial white "
    "gutters between cells and nothing touching or overlapping another cell. 3D model references for a game seen by a "
    "camera high above, on dark volcanic rock and in a dark forge, so each has ONE bold simple silhouette, big flat colour "
    "shapes and glowing parts that read in the dark. The raiders' machines are dark gunmetal #3A3E44 and rust-red "
    "#8A3A26 with black-and-yellow hazard stripes, as in the first reference. TOP ROW: "
    "(1) MUSTER LANCER, a raider hunter-wing fighter craft: a narrow dark gunmetal hull with rust-red panels, short swept "
    "wings, twin rear thrusters glowing orange, and projecting from its nose ONE very long straight RAM-LANCE, a spike as "
    "long as the hull itself; from above it reads as a winged spear; "
    "(2) MUSTER MORTAR, a walking slag-mortar: a squat armoured gunmetal body on four short thick jointed legs, carrying "
    "one short fat mortar barrel angled up, glowing orange inside its mouth, and a hopper of round glowing slag shells "
    "on its back, hazard stripes on the barrel; knee-high to a tall man's chest; "
    "(3) ASH CRAWLER, a native heat-scavenger of a volcanic world: a low flat armoured creature like a giant woodlouse "
    "crossed with a salamander, overlapping charcoal-black #2A2624 plates with glowing orange-red #FF5A1E seams between "
    "them, a wide flat shovel head with a hooked lunging jaw, six short strong legs, a short tail; "
    "BOTTOM ROW: "
    "(4) POUR DRONE, a foundry drone that runs on an overhead rail: a boxy gunmetal carriage with a wheeled trolley on "
    "top (it hangs from a rail), and slung beneath it on two arms a deep iron CRUCIBLE BUCKET brimming with glowing molten "
    "metal #FFB347, tilted a little as if about to pour, hazard stripes on the carriage; "
    "(5) BRIDLE CAGE, a finished cage for a living space-road: an enormous cage of six heavy curved dark iron RIBS rising "
    "from a round iron base and closing above like the bars of a lantern or a ribcage, the ribs joined at four places by "
    "massive iron CLAMPS, each clamp with two hand-shaped seats in its face; hot orange glowing seams where the ribs meet "
    "the base, heavy chains; the inside of the cage is empty; "
    "(6) ROUTE LOCK, a barrier across a space lane built by the other side, the Compact, in clean white #E8EDEE and dark "
    "teal #1E6E78: a big square frame of four heavy white beams with teal stripes, joined at the corners by bolted "
    "brackets, a thick KEEPER BAR across the middle locked into both sides, blinking teal warning lamps on each corner; "
    "clean, orderly and very solid. The second reference is for the creature's drawing style, the third for the craft's.")

KIT_TAIL = (". Original UV islands; flat clean cel-shaded game texture, no baked lighting.")
BREKK_BODY = "art-src/Generated/QA-Hessa/BrekkBody_clean.fbx"

BATCH = Batch(
    name="C15-Vaskar", prompt_id="C15",
    bodies={
        # He stands in the frame at 2.5 m, a head over Brekk.
        "DraskVaal": ([REFS / "Drask_Natural.png", GEN / "QA-Hessa/refs/Brekk_Body.png"], "Drask_Frame", DRASK, 2.5,
                      "Warmaster Drask Vaal, huge Vaskar man with bronze plated skin and two crest-sails, bare head, in a "
                      "dark iron and bronze war-frame with a crimson sash, oval shield along the left forearm, long lance "
                      "along the right forearm, T pose"),
    },
    as_drawn=("DraskVaal",),
    singles={"SlagColossus": (COLOSSUS, [GEN / "C9-Lantern/refs/muster-camp-board.png", GEN / "C12-ManyHands/refs/Hulljack.png"], "1:1")},
    boards={
        "scoria-board": (BOARD, [GEN / "C9-Lantern/refs/muster-camp-board.png", GEN / "C11-Wendmire/refs/mire-creature-board.png",
                                 GEN / "C13-Hoarfell/refs/IdraFlight.png"],
                         ["MusterLancer", "MusterMortar", "AshCrawler", "PourDrone", "BridleCage", "RouteLock"]),
    },
    models={
        "SlagColossus": ("hero", 15000),
        "MusterLancer": ("hero", 7000), "MusterMortar": ("prop", 6000), "AshCrawler": ("hero", 7000),
        "PourDrone": ("prop", 5000), "BridleCage": ("hero", 9000), "RouteLock": ("prop", 6000),
    },
    reskins={
        # The Unsworn forewoman: Brekk's smith's body and apron in her soot and leather.
        "TovraBody": (BREKK_BODY,
            "Tovra Kesh, a Vaskar forewoman in her forties: heavy shoulders, plated skin of soot-dark brown-grey #4A403A "
            "dulled by soot, the two crest-sails painted dark and folded flat, tired amber eyes ringed dark; a heavy "
            "leather smith's apron #5A3A24 over a dark grey shirt, sleeves rolled, forearms pitted with small pale spatter "
            "burns, tongs through the belt, slag specks in every seam, dark trousers, heavy dark boots. Soot, leather and "
            "dark grey only", KIT_TAIL),
        # The Warmaster out of his frame: in the oath hall, in custody and aboard.
        "DraskBody": (BREKK_BODY,
            "Drask Vaal, a Vaskar Warmaster in his fifties, out of armour: plated skin the brown of old bronze #8A6A40, the "
            "two crest-sails bronze-brown and nicked at the edge, calm amber eyes; a plain dark grey #3E4246 soldier's tunic "
            "to the knee (the apron painted as its front panel, grey cloth, no leather), a crimson #8A2A20 sash across the "
            "chest with two brass Warmaster's tabs, dark grey trousers, black boots. Bronze, dark grey and one crimson sash",
            KIT_TAIL),
    },
)

if __name__ == "__main__":
    raise SystemExit(BATCH.main())
