#!/usr/bin/env python3
"""Chapter 9, The House That Can Leave: the Foundry Saint, the Debris Cutter, the Crawlers, Jun, Bryn Asta, and Nacre's,
the Terminator's and the Works' pieces (docs/chapters/ch09-the-house-that-can-leave.md,
docs/writing/pending/ch09-manifest.md).

The three portrait sheets (Dorrow, Tam, Jun) are tools/meshy/species_batch.py's; Bryn's is chapter 8's. Stages and
their order are tools/meshy/batch_stages.py's; tools/c16_intake.py takes the pieces from here.

  python tools/meshy/c16_home_batch.py refs --only crawl-cast           # Dorrow and Jun, two T-posed figures on one sheet
  python tools/meshy/c16_home_batch.py isolate --only crawl-cast
  python tools/meshy/c16_home_batch.py models --only DorrowBody
  python tools/meshy/c16_home_batch.py rig --only DorrowBody
  python tools/meshy/c16_home_batch.py reskin --only BrynBody
  python tools/meshy/c16_home_batch.py refs --only works-board
  python tools/meshy/c16_home_batch.py isolate --only works-board

Two new people share one reference: the Crawlers' body (Dorrow's, from his sheet) and Jun's, each cut out and built
and rigged on its own. Tam is a grade of Dorrow's coat at an adolescent's size; Bryn wears a coat on Corra's body; Oda
and Ilo are grades of Corra's and the Survivor's coats. The Foundry Saint is a press and one arm, so the fight can drop
the arms it releases; the Weld Arm is that arm under the override. The Interceptor is the Debris Cutter at half size in
a plainer grade, the Heat-scavenger a grade of the Ash Crawler (the same native family), the Bridle Fragment a grade of
the Bridle cage, the Tow-drone the claim drone in Compact white (all the intake script's).
"""
from __future__ import annotations

from batch_stages import BOARD_STYLE, BODY_FLAT, ROOT, Batch

GEN = ROOT / "art-src" / "Generated"
REFS = GEN / "C16-Home" / "refs"
POSE = (" Each figure: front-facing orthographic full-body view, neutral expression, head straight aligned with torso, "
        "both arms straight horizontally in a T pose, hands open with separated fingers, legs clearly apart with a white "
        "gap between them, feet shoulder-width apart and pointing forward. ")

CAST = (
    "Use case: stylized-concept. Asset type: 3D game-character modeling reference sheet for Coronach. The two attached "
    "portrait sheets are ONLY identity and style references. Wide landscape canvas with TWO separate full-body figures "
    "standing side by side, each centred in its own half, with a WIDE empty white gap between them so that no part of "
    "one, not even a fingertip, comes near the other; both drawn at the same scale and both entirely inside the image "
    "with white margin. No sheet of expressions, no extra views, no scenery, no text. "
    "LEFT figure, from the FIRST reference: Chief Engineer Dorrow, an old Crawler man of a moving town, a small, round, "
    "heavy-set people about five heads tall, a round body on short sturdy legs, short thick arms. Keep his exact head "
    "from the portrait: a battered brown leather cap, a dark grey-green face-wrap up to the nose, and round brass goggles "
    "with leaf-shutter lenses, the LEFT lens (the viewer's right) cracked across; no eyes, no skin showing on the face. "
    "Keep his thick quilted faded-ochre #B8913E canvas engineer's jacket stiff with black grease and the brass whistle "
    "on a cord. Extend naturally: the jacket to the hip with a broad belt of tool loops, sleeves to the wrist, heavy "
    "dark leather gloves, baggy dark grey-brown trousers wound with cloth strips at the shins, heavy round boots; black "
    "grease to the elbows. Soot, ochre and brass, reading on a dark floor. "
    "RIGHT figure, from the SECOND reference: Jun, a very old Vael woman of seventy, small, upright and spare, about seven "
    "heads tall. Keep her exact head from the portrait: warm grey skin, a small composed lined face, pale grey eyes, a "
    "faded blue-green #5E8E8A headcloth tied at the nape over thin silver fronds, faint green sync-lines. Keep her plain "
    "dark slate #3A4048 high-collared blouse and the worn cream #D8CBA8 canvas apron stained with pale yellow-green pear "
    "juice. Extend naturally: the apron to the knee, split at the sides, over a long dark skirt ending at mid-calf, the "
    "legs and plain flat dark shoes showing separately below it; long sleeves. No pear in her hands now; both hands "
    "open. No weapon, tool, bag or loose accessory on either." + POSE + BODY_FLAT.replace("ONE single isolated character only",
                                                                                         "exactly TWO isolated characters"))

BOARD = (
    BOARD_STYLE + "Six separated concept cells in a 3x2 grid, ONE whole isolated object per cell, each drawn large in its "
    "cell in a three-quarter view from slightly above, standing directly on the white ground, with substantial white "
    "gutters between cells and nothing touching or overlapping another cell. 3D model references for a game seen by a "
    "camera high above, so each has ONE bold simple silhouette and big flat colour shapes. Two makers here: the old "
    "station, Cinder Halo, built of soot-dark iron #3A3A3C with faded cream #D9CFB4 painted panels, brass #B08A3E rivets "
    "and bolted ring COLLARS at every joint; and the Compact, which builds in clean white #EEF2F2 with dark teal #1E6E78 "
    "bands and pale cyan #8FF4F8 lights, as in the first reference. TOP ROW: "
    "(1) FOUNDRY SAINT, the Halo's ancient separation press, WITHOUT its arms: a very tall heavy iron frame like a forge "
    "press crossed with a cathedral organ, a massive square press head hanging in the top of the frame on two thick "
    "pistons, and on the frame's two sides SIX empty round SHOULDER SOCKETS, three a side, each ringed by a heavy bolted "
    "brass collar; clamped high on its back a Compact OVERRIDE RIG, a white-and-teal box with glowing cyan lights and "
    "cables plugged into the iron; soot iron, cream panels, brass; "
    "(2) SAINT ARM, ONE heavy jointed arm of that press, lying loose on the ground: a round bolted brass COLLAR at its "
    "shoulder end, a thick soot-iron upper arm with a hydraulic piston, an elbow joint, and a forearm ending in a broad "
    "flat square SLAM PLATE; cream panels with black-and-yellow stripes near the plate; "
    "(3) DEBRIS CUTTER, a Compact interception craft: a sleek aggressive white-and-teal arrowhead hull, clean and "
    "clinical, carrying THREE clearly different weapon pods: on its nose a long CUTTING-BEAM barrel, on its left flank a "
    "drum MINE-LAYER full of round teal-lit mines, on its right flank a ring-shaped TRACTOR emitter glowing cyan; from "
    "above it reads as a white arrowhead with three distinct pods; "
    "BOTTOM ROW: "
    "(4) SLAG GULL, a scavenger bird of a twilight world: long narrow pointed wings spread in a glide, a dark ash-grey "
    "#4A4644 body and wings with ember-orange #FF6A1E wingtips, a long hooked bone beak and a glowing orange throat pouch "
    "full of molten spit; hovering with nothing beneath it; "
    "(5) OVERRIDE RIG, a Compact control drone clamped to machinery: a squat white-and-teal box drone with four short "
    "clamp legs gripping a stub of soot-iron pipe, one domed lens glowing cyan, two short antennae and thick cables; "
    "(6) CRAWL TREAD SECTION, one section of a town that rolls on enormous treads: a long block of a small town, three "
    "narrow houses of riveted brass and soot-dark plating with lit amber windows, a chimney and railed walkways, all "
    "built on top of two ENORMOUS caterpillar treads twice a person's height, the treads' links clearly drawn; warm "
    "lamplight against soot. The second reference is for the station's iron, the third for the board's finish.")

KIT_TAIL = (". Original UV islands; flat clean cel-shaded game texture, no baked lighting.")
CORRA_BODY = "art-src/Generated/QA-Hessa/CorraBody_clean.fbx"

BATCH = Batch(
    name="C16-Home", prompt_id="C16",
    bodies={
        # Figures on the crawl-cast board: no reference of their own (batch_stages: a body with no prompt is cut out).
        "DorrowBody": ([], "DorrowBody", None, 1.3,
                       "Chief Engineer Dorrow, short round heavy-set Crawler man, leather cap, face-wrap, round brass "
                       "goggles, quilted ochre engineer's jacket, gloves, wrapped trousers, round boots, T pose"),
        "JunBody": ([], "JunBody", None, 1.6,
                    "Jun, small upright very old Vael woman, grey skin, blue-green headcloth, dark high-collared blouse, "
                    "cream canvas apron to the knee, long dark skirt, flat shoes, T pose"),
    },
    boards={
        "crawl-cast": (CAST, [REFS / "Dorrow_Natural.png", REFS / "Jun_Natural.png"], ["DorrowBody", "JunBody"]),
        "works-board": (BOARD, [GEN / "C12-ManyHands/refs/CompactCutter.png", GEN / "C9-Lantern/refs/muster-camp-board.png",
                                GEN / "C10-Xylos/refs/xylos-kit-board.png"],
                        ["FoundrySaint", "SaintArm", "DebrisCutter", "SlagGull", "OverrideRig", "CrawlTread"]),
    },
    models={
        "FoundrySaint": ("hero", 12000), "SaintArm": ("prop", 6000), "DebrisCutter": ("hero", 10000),
        "SlagGull": ("hero", 7000), "OverrideRig": ("prop", 5000), "CrawlTread": ("hero", 12000),
    },
    reskins={
        # The tug captain: Corra's body (its copper fronds are hers already) in the Welcome Back's jacket.
        "BrynBody": (CORRA_BODY,
            "Bryn Asta, a Vael tug captain in her forties: blue-grey skin, lighter face and throat, the fronds on her head "
            "copper-red and cut square and short, a black tug crew's headset over them, level grey eyes, faint orange "
            "sync-lines. A dark navy #1E2A44 tug captain's work jacket with a high collar and a bold orange-and-white hull "
            "stripe running diagonally across the chest and down one sleeve, navy work trousers, dark grey boots, a "
            "utility belt. Navy, orange stripe and copper only: no green, no cream, no tan", KIT_TAIL),
    },
)

if __name__ == "__main__":
    raise SystemExit(BATCH.main())
