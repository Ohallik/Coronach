#!/usr/bin/env python3
"""Idra Sorn, the third hero (docs/chapters/ch06-the-long-sleep.md): her two bodies and her rescue cutter.

Her portrait sheets (tools/meshy/species_batch.py) are the identity references. The Natural body is made first and looked
at; it then joins the Braced body's references, so both are the same woman at the same proportions. Stages and their
order are tools/meshy/batch_stages.py's. tools/c13_idra_intake.py takes the pieces from here.

  python tools/meshy/c13_idra_batch.py refs --only Idra_Natural_Body
  python tools/meshy/c13_idra_batch.py models --only IdraNatural
  python tools/meshy/c13_idra_batch.py rig --only IdraNatural
"""
from __future__ import annotations

from batch_stages import BODY_FLAT, BODY_HEAD, ROOT, Batch

GEN = ROOT / "art-src" / "Generated"
REFS = GEN / "C13-Hoarfell" / "refs"

RAUK_BUILD = ("The Rauk are a broad, powerfully built mountain people: she stands a head shorter than a tall human and half "
              "again as wide, about six heads tall, with heavy square shoulders, a deep chest, thick strong arms, a solid "
              "waist and sturdy legs; plainly a woman, and built like a stonemason. ")
IDRA_HEAD = ("Keep her exact head from the portrait: ash-grey slate skin, fine-grained and matte, with angular facets at the "
             "cheekbones and jaw, thin ember-orange heat-seam lines down the cheeks and throat, pale silver eyes, an old pale "
             "burn scar across the left brow plate, no hair and no ears; and her CREST: one row of seven broad flat "
             "horn-plates like overlapping roof slates standing upright along the midline of the skull from brow to nape, "
             "tallest at the crown, each a separate thick slab with a pale chipped edge, large and clearly visible. ")
POSE = (" Front-facing orthographic full-body view, neutral expression, head straight aligned with torso, broad heavy "
        "proportions (not slender, not tall, not a human fashion figure), both arms straight horizontally in a T pose, legs "
        "clearly apart with a white gap between them, feet shoulder-width apart and pointing forward. Entire body, the whole "
        "crest and everything on both arms comfortably inside the image with white margin. ")

NATURAL = (
    BODY_HEAD + "Idra Sorn, an adult Rauk alien woman in her thirties, a rescue-cutter officer of a frontier guard, off duty. "
    + RAUK_BUILD + IDRA_HEAD +
    "Keep her rescue-orange high collar with its steel clasp over plain mid-grey fatigues, and the coil of orange rescue "
    "line: worn like a bandolier from one shoulder across the chest to the opposite hip, lying close against the body, not "
    "hanging loose. Extend the outfit naturally: a fitted mid-grey fatigue jacket with one orange stripe down each sleeve, "
    "the sleeves rolled to the elbow over bare slate-grey forearms with ember seams at the knuckles, a broad dark belt with "
    "a steel buckle, a flat folded steel-and-orange pack worn high on the back (her folded rescue frame) whose two straps "
    "show at the shoulders, mid-grey fatigue trousers with orange knee patches, heavy dark grey boots with orange toe "
    "caps. Mid-tone greys and strong orange, lighter than a dark floor. No weapon, shield, helmet, cape or tools; hands "
    "open with separated fingers." + POSE + BODY_FLAT)

BRACED = (
    BODY_HEAD + "The FIRST reference is her portrait sheet in the frame; the SECOND reference is the same woman's whole body "
    "out of the frame: keep exactly that build, height and face. Idra Sorn, an adult Rauk alien woman, now BRACED in her "
    "cutter frame, a rescue rig that has unfolded around her into heavy working armour. " + RAUK_BUILD + IDRA_HEAD +
    "A steel visor band is pushed UP onto her brow above the brow plates, so her whole face shows; no helmet. The frame: "
    "thick hazard-orange plates with brushed-steel edges, black panel lines and rows of rivets over the shoulders (big "
    "square pauldrons that make her the widest figure in any room), the chest and collar, the forearms and the shins, over "
    "a dark slate under-suit; a broad steel belt; heavy steel-and-orange boots; the frame's folded pack, a squat steel box "
    "with orange corners, on her back, showing above the shoulders. On HER LEFT forearm (the viewer's right) the BRACE: a "
    "tall rectangular tower shield bolted flat along the outside of the forearm; in this T pose it lies along the "
    "outstretched arm from the elbow to a hand's length past the fingertips, twice as long as it is wide, its hazard-orange "
    "face with black chevron stripes and a steel rim turned toward the viewer. On HER RIGHT forearm (the viewer's left) the "
    "BREACHING HAMMER: a thick steel hydraulic piston housing built along the top of the forearm, ending in one heavy "
    "square steel hammer head that projects a hand's length beyond her fist, with an orange band and two short hydraulic "
    "lines. Shield and hammer are parts of the frame, fixed to the forearms, not held and not loose; both hands open. A "
    "coil of orange rescue line is clipped at one shoulder plate. Strong orange and steel, lighter than a dark floor."
    + POSE + BODY_FLAT)

CUTTER = (
    "Use case: stylized-concept. Create ONE isolated complete compact spacecraft as a production reference for Meshy "
    "reconstruction, in a single three-quarter FRONT view from 35 degrees above. Wide landscape canvas; entire vehicle "
    "centered with clear white margin, filling 80 percent of frame. OPAQUE PURE WHITE background. Flat cel-shaded game art "
    "with broad matte color regions, in exactly the art style, line weight and finish of the two reference craft. No "
    "ground, cast shadow, gloss, highlights, reflections, bloom, gradients, particles, exhaust, labels, text, inset or "
    "turnaround. All parts physically attached. NOT either craft in the references: those are two sleek sister ships, "
    "shown only for style and scale. This is the third of the three, and the tug among them: Idra's RESCUE CUTTER, a "
    "one-person frontier rescue craft, broad, blunt and heavy, built like a tugboat crossed with an icebreaker. Length "
    "3.6 m, width about 0.7 of its length, height about 0.3 of its length: a wide flat wedge-shaped hull with NO wings, "
    "only two short thick stub fins at the stern. At the front a massive blunt RAM PROW: one thick steel wedge plate like "
    "a snowplough blade, as wide as the hull, painted in black and hazard-orange chevron stripes. Behind it a low armoured "
    "cab with one narrow dark visor slit and no visible pilot. Along each flank one folded CUTTING ARM: a thick jointed "
    "steel arm lying flat against the hull, ending in a closed shear claw beside the prow. At the stern a round TOW WINCH "
    "drum wound with orange cable, with a steel tow hook, sits on the centre line between two large round recessed dark "
    "engine nozzles. Hull plates in hazard orange #E8622A and brushed steel #9AA3AD over dark slate #3B4450, black panel "
    "lines, rivets, and one row of four small amber #FFB62E running lights along each flank. Tough, honest and "
    "industrial; from above it reads as one broad orange-and-steel wedge, clearly wider and blunter than the two "
    "reference craft.")

BATCH = Batch(
    name="C13-Hoarfell", prompt_id="C13",
    bodies={
        "IdraNatural": ([REFS / "Idra_Natural.png"], "Idra_Natural_Body", NATURAL, 1.75,
                        "Idra Sorn, broad heavy Rauk alien woman, slate-grey skin, crest of stone horn-plates, grey rescue "
                        "fatigues with an orange collar, coil of orange line, T pose"),
        "IdraBraced": ([REFS / "Idra_Braced.png", REFS / "Idra_Natural_Body.png"], "Idra_Braced_Body", BRACED, 1.85,
                       "Idra Sorn, broad heavy Rauk alien woman in hazard-orange and steel rescue armour, tower shield on "
                       "the left forearm, hydraulic hammer on the right forearm, visor up, T pose"),
    },
    singles={"IdraFlight": (CUTTER, [GEN / "Coronach-flight/refs/TarenFlight.png", GEN / "Coronach-flight/refs/SelaFlight.png"], "3:2")},
    models={"IdraFlight": ("hero", 12000)},
)

if __name__ == "__main__":
    raise SystemExit(BATCH.main())
