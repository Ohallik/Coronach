#!/usr/bin/env python3
"""Chapter 5, Many Hands: the generated bodies and machines (docs/chapters/ch05-many-hands.md).

The two portrait sheets (Meret, Dessik) are tools/meshy/species_batch.py's. Stages and their order are
tools/meshy/batch_stages.py's; tools/c12_intake.py takes the pieces from here.

  python tools/meshy/c12_many_hands_batch.py refs --only Meret_Body      # look at it before the mesh
  python tools/meshy/c12_many_hands_batch.py models --only MeretBody
  python tools/meshy/c12_many_hands_batch.py rig --only MeretBody
  python tools/meshy/c12_many_hands_batch.py reskin --only DessikBody

Meret is a new body from her own sheet: the antagonist is seen often and no body in the game is hers (her chapter 10
machine frames are separate orders). Dessik is a coat on the Survivor's rigged miner, the nearest working Vael body;
he is seen out of the suit only in custody. Hulljack, the boss, is one static machine that code animates; part two
(DessikVarn, the operator exposed) is the same mesh in a scorched grade (the intake script's). The ship-town itself is
built from kits that exist, so no kit is ordered here.
"""
from __future__ import annotations

from batch_stages import BODY_FLAT, BODY_HEAD, ROOT, Batch

GEN = ROOT / "art-src" / "Generated"
REFS = GEN / "C12-ManyHands" / "refs"
POSE = (" Front-facing orthographic full-body view, neutral expression, head straight aligned with torso, both arms straight "
        "horizontally in a T pose, legs clearly apart with a white gap between them, feet shoulder-width apart and pointing "
        "forward. Entire figure and everything on both arms comfortably inside the image with white margin. ")

MERET = (
    BODY_HEAD + "Meret Venn, an adult Vael alien woman in her sixties, director of the Anchor Compact and a public route "
    "engineer. Tall, upright and spare, about seven and a half heads tall, narrow square-held shoulders, composed and "
    "still; slim, never frail. Keep her exact head from the portrait: cool slate-grey matte skin, lighter on the face and "
    "throat, a long composed face with fine age lines at the eyes and mouth, pale steady grey-green ring irises, faint dim "
    "teal sync-lines at the temples, an elongated swept-back cranium with silver-white flat keratin fronds cropped close "
    "to the skull and swept back; no human hair and no ears. Keep her white field coat with teal piping and its high plain "
    "stand collar, the small teal ring-shaped pin at the collar and the brass stylus clipped at the collar's edge. Extend "
    "the outfit naturally: the coat fitted and single-breasted, closed down the front, teal piping along every edge and "
    "seam, long fitted sleeves with teal cuffs worn soft at the edges, a slim slate-teal belt at the waist; the coat's "
    "skirt ends at MID-THIGH and is split at both sides, so both legs show separately below it; slim slate-blue-grey "
    "trousers on two clearly separate legs; plain soft grey ankle boots. Clean white, teal and slate only, bright against "
    "a warm dark floor. No weapon, tablet, bag, cape or loose accessories; hands open with long separated fingers."
    + POSE + BODY_FLAT)

SINGLE_HEAD = (
    "Use case: stylized-concept. Asset type: ONE isolated 3D game-model production reference for Coronach, for Meshy "
    "image-to-3D reconstruction. OPAQUE PURE WHITE background, the whole object centred with clear white margin, filling "
    "80 percent of the frame. Flat cel-shaded game art with broad matte colour regions, crisp restrained linework and "
    "black panel lines, in exactly the art style, line weight and finish of the FIRST reference. No ground, cast shadow, "
    "gloss, highlights, reflections, bloom, gradients, particles, sparks, flame, exhaust, smoke, labels, text, inset or "
    "turnaround. All parts physically attached; nothing floats. ")

HULLJACK = (
    SINGLE_HEAD + "Drawn in a single three-quarter FRONT view from 40 degrees above, so its top and all four feet show. "
    "NOT the craft in the first reference: it is shown only for style and industrial finish. The SECOND reference is the "
    "man who drives it, shown for his colours (hazard yellow and gunmetal) and for his face only. HULLJACK, a heavy "
    "salvage exosuit that walks across the outside of a starship's hull: a squat armoured body like a crab's, hunched and "
    "wide, on FOUR thick jointed legs splayed far out to the four corners like a crab's or a spider's, each leg bent up at "
    "the knee and down to a huge flat round MAGNET FOOT: a broad thick disc as wide as the body is tall, with a bright "
    "cyan #5FE8F0 magnet face ring around its rim. In front, an open cockpit cage of heavy roll bars set in the chest, "
    "and in it the operator, a heavy-set grey-skinned man in a hazard-yellow harness, seen from the waist up behind the "
    "bars. Two long heavy ARMS reach forward from the shoulders, splayed wide apart: ITS LEFT arm (the viewer's right) "
    "ends in a big three-fingered GRAPPLE CLAW with a cable drum on the forearm; ITS RIGHT arm (the viewer's left) ends "
    "in a long CUTTING TORCH, a thick nozzle barrel with a fuel tank on the forearm and a glowing orange #FF7A2A tip. On "
    "its back a boxy engine pack with two short exhaust stacks. Thick plates in hazard yellow #F2B518 with black chevron "
    "stripes on the leg guards, over a dark gunmetal #3A4048 frame and pistons, worn steel #8E969E edges, scuffs, a row "
    "of rivets. From far above it reads as four big round cyan-ringed feet at four corners, two long arms forward and a "
    "yellow body between them: a bold X shape. About 4.5 m tall, the feet spread 7 m corner to corner.")

CUTTER = (
    SINGLE_HEAD.replace("of the FIRST reference", "of the FIRST reference (a different ship)") +
    "Drawn in a single three-quarter FRONT view from 35 degrees above. Wide landscape canvas. NOT the craft in the "
    "first reference: it is shown only for style. The SECOND reference is the Anchor Compact's emblem, shown for its teal "
    "and its open-hand-in-a-ring device only. A COMPACT CUTTER, a small one-crew rescue craft of the Anchor Compact, "
    "built to latch onto the hull of a damaged ship and seal it: a clean, smooth, well-made, slightly clinical machine, "
    "the opposite of rusty salvage. Length 4 m, width about 0.6 of its length, height about 0.3 of its length. A rounded "
    "teardrop hull like a smooth white lifeboat with a blunt rounded nose, a wide wraparound dark teal #1E6E78 canopy "
    "band across the front, two short swept-back stub wings, each ending in a round engine pod with a pale cyan "
    "#8FF4F8 glowing intake ring, and a short upright tail fin. Underneath, a broad flat RING-SHAPED DOCKING COLLAR and "
    "four short folded latching clamps like a crab's legs tucked against the belly. Hull in clean gloss-free white "
    "#EEF2F2 with dark teal #1E6E78 bands along the flanks and wing edges, light grey #B8C2C6 panel seams, one teal "
    "open-hand-in-a-ring emblem on the upper hull, and a row of three small pale cyan running lights on each flank. "
    "From above it reads as one white teardrop with teal bands and two cyan-ringed pods, clearly rounder, smaller and "
    "cleaner than a rusty salvage skiff.")

SKIN_TAIL = (". Bare skin on face, throat and neck. Original UV islands; flat clean cel-shaded game texture, no baked "
             "lighting.")
SURVIVOR = "art-src/Generated/P44-NPCBodies/SurvivorNatural_clean.fbx"

BATCH = Batch(
    name="C12-ManyHands", prompt_id="C12",
    bodies={
        # She stands about 1.85 m; her fronds lie flat, so the rig height is the crown's.
        "MeretBody": ([REFS / "Meret_Natural.png"], "Meret_Body", MERET, 1.85,
                      "Meret Venn, tall spare Vael alien woman in her sixties, slate-grey skin, cropped silver fronds, white "
                      "field coat with teal piping and high collar to mid-thigh, slate trousers, grey boots, T pose"),
    },
    singles={
        "Hulljack": (HULLJACK, [GEN / "C13-Hoarfell/refs/IdraFlight.png", REFS / "Dessik_Natural.png"], "1:1"),
        "CompactCutter": (CUTTER, [GEN / "Coronach-flight/refs/SelaFlight.png", GEN / "C10-Xylos/refs/Patrol_Emblem.png"], "3:2"),
    },
    models={"Hulljack": ("hero", 15000), "CompactCutter": ("hero", 10000)},
    reskins={
        # The Survivor's miner (P44) is the nearest working Vael body: coverall, belt, boots. None of the Vael men is
        # heavy-set, so Dessik is the height of the tallest of them and his weight is in the coat. The first coat
        # (rejected/) gave a peach-faced, white-haired mechanic with one brown strap and no yellow at all: this one asks
        # for the harness as large yellow areas and names the skin's grey outright.
        "DessikBody": (SURVIVOR,
            "Dessik Varn, a heavy Vael alien salvage boss in his fifties. Skin warm stone-grey #8C8681 wherever it shows, "
            "never peach or pink; the fronds on his head painted short and dark grey #55555A. A bright hazard-yellow "
            "#F2B518 salvage harness over a grey work shirt: two broad yellow straps over the shoulders, a broad yellow "
            "band across the chest, a yellow belt with black buckles, yellow-and-black chevron patches on the shoulders. "
            "Stained mid-grey #6E7378 shirt and trousers, gunmetal #3A4048 gloves and knee pads, dark boots, gunmetal "
            "headlamp band. Large clear yellow areas on chest and shoulders; grey, yellow and gunmetal only: no orange, "
            "no brown, no white hair", SKIN_TAIL),
    },
)

if __name__ == "__main__":
    raise SystemExit(BATCH.main())
