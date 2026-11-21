#!/usr/bin/env python3
"""Chapter 7, The Cage Makers (Oriel): the Last Warden, the gardeners, Lume, and the Orrery's, the Skyfields' and the
Glass Choir's pieces (docs/chapters/ch07-the-cage-makers.md, docs/writing/pending/ch07-manifest.md).

The two portrait sheets (Lume, the Warden's mask) are tools/meshy/species_batch.py's. Stages and their order are
tools/meshy/batch_stages.py's; tools/c14_intake.py takes the pieces from here.

  python tools/meshy/c14_oriel_batch.py refs --only Last_Warden        # look at it before the mesh
  python tools/meshy/c14_oriel_batch.py models --only LastWarden
  python tools/meshy/c14_oriel_batch.py rig --only LastWarden
  python tools/meshy/c14_oriel_batch.py reskin --only LumeBody
  python tools/meshy/c14_oriel_batch.py refs --only oriel-board
  python tools/meshy/c14_oriel_batch.py isolate

The Last Warden and the gardener are new bodies of the Ancelane make (the guard's reference is their style), both
rebuilt as drawn so that the staff and the rake survive. The Pruner is a gardener set to prune: the same body in a
grade with its lamp gone red (the intake script's), as the script means it to be read. The Glass Sentinel is the
guard cast in glass, a grade of the guard's own coat; the Glass Choir's ribs are Lastlight's rib arch in a glass grade.
Lume wears a coat on Pell's body: both are Aumi's apprentices. Fibre Wisps are motes, drawn in code like chapter 6's.
"""
from __future__ import annotations

from batch_stages import BOARD_STYLE, BODY_FLAT, BODY_HEAD, ROOT, Batch

GEN = ROOT / "art-src" / "Generated"
REFS = GEN / "C14-Oriel" / "refs"
GUARD_REF = GEN / "C11-Wendmire/refs/Ancelane_Guard.png"
POSE = (" Front-facing orthographic full-body view, head straight aligned with torso, both arms straight horizontally in a "
        "T pose, legs clearly apart with a white gap between them, feet shoulder-width apart and pointing forward. Entire "
        "figure and everything on both arms comfortably inside the image with white margin. ")
CONSTRUCT_HEAD = (
    "Use case: stylized-concept. Asset type: single 3D game-character modeling reference for Coronach, in EXACTLY the art "
    "style, line weight, cel shading and finish of the FIRST reference, which is an Ancelane guard: the same ancient "
    "makers built this figure, but it is a different figure. Create exactly ONE full-body figure, no sheet, no extra "
    "views, no scenery. ")
CONSTRUCT_FLAT = ("Flat matte toon-color illustration, crisp restrained linework, only subtle local shading, no cast or ground "
                  "shadows, highlights, gloss, ambient occlusion, bloom or dramatic lighting; uniform pure white background, "
                  "no text, labels, dividers or watermark. This is ONE single isolated figure only.")

WARDEN = (
    CONSTRUCT_HEAD + "THE LAST WARDEN, the guardian of the first Bridle, a thousand years on watch: a TALL, slender, "
    "long-limbed Ancelane guardian construct of bronze and glass, far taller and leaner than the stocky guard, about nine "
    "heads tall, regal and still. Its head IS the mask of the SECOND reference, exactly: a long bronze visor clearly "
    "taller than wide tapering to a blunt chin, no mouth, no nose, a raised brow band stamped with a ring crossed by one "
    "bar, and one single LEVEL SLOT of thick glass across the face with hard white light behind it. Its body: dark aged "
    "bronze #7A5A2E plates with warmer bronze #A7743A edges and bright verdigris #4FA58C crusted in every seam; tall "
    "narrow shoulder plates; panels of cloudy pale glass #CFE8F0 set in the chest, the upper arms and the thighs, with "
    "fine glowing white fibre-threads running inside the glass; a slim waist of stacked bronze rings; a tabard of long "
    "overlapping bronze scales hanging from the waist to the knees, split at the front and both sides so the two legs show "
    "separately; long bronze legs and narrow armoured feet. Fixed along the top of ITS RIGHT forearm (the viewer's left), "
    "its STAFF: a long straight bronze staff, part of the arm, projecting a full arm's length beyond the fist, ending in a "
    "tall open bronze fork like a lyre's frame with three taut glowing white fibre strings between its prongs. In this T "
    "pose the staff lies HORIZONTAL along the outstretched arm, NOT upright. ITS LEFT hand open, long bronze fingers. "
    "Mid-tone bronze, verdigris and pale glass, lighter than a dark floor." + POSE + CONSTRUCT_FLAT)

GARDENER = (
    CONSTRUCT_HEAD + "An Ancelane GARDENER, one of the servant automatons that still tend the empty gardens: SLIGHT and "
    "slender, a little taller than a tall man, narrow shoulders, long thin arms and legs, gentle and careful, never a "
    "soldier. Bronze gone green: pale weathered bronze #9C8A5A heavily patched with soft verdigris #6FB39A, and panels of "
    "CLOUDY milky glass #DCE8E4. Where a face would be, a smooth rounded bronze hood holding ONE large round LAMP of cloudy "
    "glass that glows a soft warm white #FFF4D6: no eyes, no mouth, just the lamp. A thin neck of stacked rings, a slim "
    "chest with a small round glass window, a short skirt of hanging bronze slats to mid-thigh, split so both legs show "
    "separately, thin jointed legs and narrow feet. Its HANDS are long and delicate, with five very long thin jointed "
    "fingers each, too fine to grip. Fixed along the outside of ITS RIGHT forearm (the viewer's left), its TOOL, part of "
    "the arm: a long thin pole projecting a full forearm's length beyond the hand and ending in a broad flat bronze blade "
    "with a row of short teeth along its edge, a garden rake that is also a pruning blade; in this T pose it lies "
    "HORIZONTAL along the arm. Quiet, pale and green, clearly lighter than a dark floor." + POSE + CONSTRUCT_FLAT)

BOARD = (
    BOARD_STYLE + "Six separated concept cells in a 3x2 grid, ONE whole isolated object per cell, each drawn large in its "
    "cell in a three-quarter view from slightly above, standing directly on the white ground, with substantial white "
    "gutters between cells and nothing touching or overlapping another cell. 3D model references for a game seen by a "
    "camera high above, so each has ONE bold simple silhouette and big flat colour shapes, no thin wisps. Everything here "
    "is from Oriel, an ancient world of bronze, verdigris and glass, in the carved bronze-and-stone style of the "
    "references. TOP ROW, three creatures and machines: "
    "(1) GLASSBACK, a grazing creature that has grown plates of glass: a heavy low four-legged grazer like a tortoise "
    "crossed with an ox, thick short legs, a long low blunt head held down, a soft moss-green #7C9A5C hide with a cream "
    "belly, and its whole back covered in a ridge of big jagged upright plates of clear pale cyan glass #BFEFF5, each "
    "plate as tall as its head, catching the light; placid, twice as long as it is tall; "
    "(2) CLAMP TENDER, a small fragile maintenance construct: a round bronze body like a kettle on three thin jointed "
    "legs, two long thin arms ending in a wrench-hand and a small cutting tool, one round amber #FFC46B lamp eye, "
    "verdigris in its seams; delicate, half a person tall; "
    "(3) RAIL WARDEN, a drone that rides an orbital light-rail: a long bronze body like a lantern or a bead on a wire, "
    "pierced lengthwise by a round bore that a rail runs through, two short swept fins, a ring of glass panels round its "
    "middle, one forward glass lens glowing hard white, verdigris in the seams; seen from above it reads as a bronze "
    "spindle with a bright eye. "
    "BOTTOM ROW, three set pieces: "
    "(4) BRIDLE CLAMP, an enormous ancient clamp that has held a dead giant creature still for a thousand years: a "
    "massive bronze C-shaped jaw gripping a thick column of clear pale cyan glass, its two jaws pinned by huge bronze "
    "bolts, its housing carved with three lines of glyphs, and on its front two smooth worn HAND SEATS, hollows shaped for "
    "two human hands; dark bronze, bright verdigris, pale glass; "
    "(5) RAIL GATE, a gate of an orbital machine: a huge upright bronze RING standing on a short squat pylon, a thick "
    "glowing light-rail running straight through its centre, its inner rim lined all the way round with a band of square "
    "lamps, the left half glowing green #7BEA8A and the right half amber #FFB62E, a carved keystone at the top of the ring "
    "with a moon emblem; "
    "(6) GLASS TREE, a tree grown entirely of glass where a frayed road leaked into the ground: a twisting trunk and "
    "spreading branches of clear pale cyan-white glass #D6F4F8 with fine glowing white threads inside, leaves like thin "
    "flat glass shards in clusters, on a small mound of pale gold stone; elegant and strange. "
    "Materials: dark bronze #7A5A2E, warm bronze #A7743A, verdigris #4FA58C, pale glass #CFE8F0, amber #FFC46B. The first "
    "reference is a design board of the same makers' work, the second the same makers' guard.")

KIT_TAIL = (". Original UV islands; flat clean cel-shaded game texture, no baked lighting.")
PELL_BODY = "art-src/Generated/QA-Hessa/PellBody_clean.fbx"

BATCH = Batch(
    name="C14-Oriel", prompt_id="C14",
    bodies={
        "LastWarden": ([GUARD_REF, REFS / "Warden_Mask.png"], "Last_Warden", WARDEN, 3.6,
                       "Last Warden, tall slender bronze and glass guardian construct with a long mask-visor and a glowing "
                       "glass slot, verdigris, scale tabard, long staff with a lyre fork along the right forearm, T pose"),
        "Gardener": ([GUARD_REF], "Gardener", GARDENER, 2.2,
                     "Ancelane gardener, slight slender bronze and verdigris automaton with a round glowing lamp for a face, "
                     "long thin fingers, a long rake fixed along the right forearm, T pose"),
    },
    # Both carry a tool along a forearm, which Meshy's T-pose mode dropped from the guard.
    as_drawn=("LastWarden", "Gardener"),
    boards={
        "oriel-board": (BOARD, [GEN / "C9-Lantern/refs/lantern-kit-board.png", GUARD_REF],
                        ["Glassback", "ClampTender", "RailWarden", "BridleClamp", "RailGate", "GlassTree"]),
    },
    models={
        "Glassback": ("hero", 9000), "ClampTender": ("prop", 5000), "RailWarden": ("prop", 5000),
        "BridleClamp": ("hero", 8000), "RailGate": ("hero", 8000), "GlassTree": ("hero", 8000),
    },
    reskins={
        # Aumi's other apprentice: Pell's slight young body in Lume's own clothes and colours.
        "LumeBody": (PELL_BODY,
            "Lume, a young Vael woman of eighteen, keeper of a lantern: pale blue-grey skin, lighter face and throat, "
            "thin orange sync-lines on the cheeks, heavy-lidded amber eyes; the fronds on her head painted dark slate-blue "
            "#3E4A5E, tied back with a pale wick cord. A soot-grey #4A4C52 keeper's smock buttoned to a high collar, the "
            "cuffs rolled, a coil of pale rope over one shoulder, a bone-handled wick-knife at the belt, dark grey trousers, "
            "soft dark boots. Soot greys, slate and pale rope only: no ochre, no tan, no white hair", KIT_TAIL),
    },
)

if __name__ == "__main__":
    raise SystemExit(BATCH.main())
