#!/usr/bin/env python3
"""Chapter 6, The Long Sleep (Hoarfell): the Kept, the frozen host, the Reach's creatures and Lastlight's kit
(docs/chapters/ch06-the-long-sleep.md). Idra's own bodies are tools/meshy/c13_idra_batch.py's; both share the folder.

The four portrait sheets (Tern, Hask, Iru, Vessa) are tools/meshy/species_batch.py's. Stages and their order are
tools/meshy/batch_stages.py's; tools/c13_hoarfell_intake.py takes the pieces from here.

  python tools/meshy/c13_hoarfell_batch.py refs --only Tern_Body          # look at it before the mesh
  python tools/meshy/c13_hoarfell_batch.py models --only TernBody
  python tools/meshy/c13_hoarfell_batch.py rig --only TernBody
  python tools/meshy/c13_hoarfell_batch.py reskin --only HaskBody,IruBody
  python tools/meshy/c13_hoarfell_batch.py refs --only hoarfell-board
  python tools/meshy/c13_hoarfell_batch.py isolate

The Kept are one body (Tern's, from her own sheet); Hask and Iru are retextures of it, a townsfolk herder a grade of
Hask's coat (the intake script's). Vessa is a grade of Hessa Korl's coat: she is a voice on a loudhailer in this chapter
and is first seen at Harrowgate. The Host Marshal is a new construct of the Ancelane guard's make at twice its height;
the Thawed Guard is the guard's third coat (after the Drowned Sentry), the Muster Excavator a coat on the husk the
Muster Sergeant wears. The Reach's three creatures and Lastlight's three kit pieces share one board.
"""
from __future__ import annotations

from batch_stages import BOARD_STYLE, BODY_FLAT, BODY_HEAD, HULL_TAIL, ROOT, Batch

GEN = ROOT / "art-src" / "Generated"
REFS = GEN / "C13-Hoarfell" / "refs"
POSE = (" Front-facing orthographic full-body view, neutral expression, head straight aligned with torso, both arms straight "
        "horizontally in a T pose, legs clearly apart with a white gap between them, feet shoulder-width apart and pointing "
        "forward. Entire figure and everything on both arms comfortably inside the image with white margin. ")

TERN = (
    BODY_HEAD + "Old Tern, a very old woman of the Kept, the furred herding people of an ice world. The Kept are short, "
    "stocky and barrel-chested, built round and solid against the cold: she stands about four and a half heads tall, "
    "broad and round in the body, with short thick arms and short sturdy legs; standing upright and solid, never frail, "
    "never stooped. Keep her exact head from the portrait: dense soft dun-brown fur drawn in big solid cel-shaded clumps, "
    "gone silver-white at the muzzle and throat; a broad round flat-fronted face with a short blunt muzzle, a broad dark "
    "leathery nose pad and round dark eyes; small rounded ears low on the sides of the head, half hidden in the fur; a "
    "long grizzled brow ruff drooping over her left eye; and her long silver cheek-whiskers braided into THICK plaits "
    "that hang close against the cheeks down to the collarbone, each tipped with a carved bone bead. Keep her three "
    "layered hides: a pale shaggy cream fleece cape over the shoulders and upper arms, over a brown hide tunic, over a "
    "dark charcoal-brown hide; and the carved bone counting-stick tucked flat through the hides across her chest. Extend "
    "the outfit naturally: the brown hide tunic belted with a plaited cord and ending at the hips, short hide sleeves to "
    "the elbow over thick furred dun-brown forearms, furred hands with dark palm pads and four short thick separated "
    "fingers, dark brown hide leggings bound with cord on two short clearly separate legs, broad hide boots with fur "
    "cuffs. Warm browns, cream and silver: darker and warmer than snow, the cream fleece bright. No staff, weapon, pack, "
    "tail or loose accessories; hands open." + POSE + BODY_FLAT)

MARSHAL = (
    "Use case: stylized-concept. Asset type: single 3D game-character modeling reference for Coronach, in EXACTLY the art "
    "style, line weight, cel shading and carved-stone-and-bronze materials of the reference, which is an Ancelane GUARD, "
    "a sentry construct. Create exactly ONE full-body figure, no sheet, no extra views, no scenery. The HOST MARSHAL: the "
    "commander of the same ancient army, plainly built by the same makers as the guard, of the same red-gold sandstone "
    "#C98A52 in stacked rounded drums and blocks, darker sandstone #8E5A36 in the seams, bronze #A7743A bands at every "
    "joint and the same concentric-circle glyphs; but a far bigger and heavier figure than the guard: massive broad "
    "shoulders, a deep barrel chest, thick stacked-drum arms and legs, heavy square bronze-shod feet. Its head is a tall "
    "domed bronze helm with a horizontal amber #FFC46B eye slit like the guard's, crowned by a tall fan-shaped CREST of "
    "five upright bronze plates, a commander's crest. Over both shoulders a heavy MANTLE of overlapping carved stone "
    "plates like a short stone cape, no cloth. Its chest bears a large round bronze disc of concentric-circle glyphs "
    "around a big amber crystal. FROST is in all of its joints: pale blue-white rime #DDF0FA and short icicles in the "
    "seams at the neck, shoulders, elbows, waist rings and knees, and a crust of ice on top of the helm, crest and "
    "mantle. Strapped flat along the outside of ITS LEFT forearm (the viewer's right), a CAGE-SHIELD: a long rectangular "
    "frame of thick bronze bars in an open square lattice, like a portcullis, with a sandstone boss at its centre. In "
    "this T pose the shield is HORIZONTAL: it lies along the outstretched arm like a plank, its long edges parallel to "
    "the arm, from the elbow to two hand-lengths past the fingertips, three times as long as it is wide, its face toward "
    "the viewer; NOT upright, no part of it rising above the shoulder. ITS RIGHT forearm (the viewer's left) carries the "
    "STANDARD-SPEAR: a heavy straight bronze spear fixed along the top of the forearm, its long leaf-shaped blade "
    "projecting a full forearm's length beyond the fist, and just behind the blade a round flat bronze standard-disc "
    "carved with glyphs, set flat on the shaft like a collar. Shield and spear are parts of the body, not held and not "
    "loose; hands open with thick bronze fingers. Warm mid-tone stone and bronze with pale frost, clearly lighter than a "
    "dark floor." + POSE.replace("neutral expression, head straight", "head straight") +
    "Flat matte toon-color illustration, crisp restrained linework, only subtle local shading, no cast or ground shadows, "
    "highlights, gloss, ambient occlusion, bloom or dramatic lighting; uniform pure white background, no text, labels, "
    "dividers or watermark. This is ONE single isolated figure only.")

BOARD = (
    BOARD_STYLE + "Six separated concept cells in a 3x2 grid, ONE whole isolated object per cell, each drawn large in its "
    "cell in a three-quarter view from slightly above, standing directly on the white ground, with substantial white "
    "gutters between cells and nothing touching or overlapping another cell. These are 3D model references for a game "
    "seen by a camera high above, on blue-white snow and ice, so each has ONE bold simple silhouette and big flat colour "
    "shapes; fur is drawn as a few big solid clumps, never thin wisps or hair strands. TOP ROW, three creatures, each "
    "with its head towards the lower left: "
    "(1) RIME-STALKER, an ice predator: long and low, a lean body slung between four thick splayed legs with broad "
    "snowshoe paws, a long flat wedge head with a heavy jaw and pale ice-blue crystal teeth, a long thick tapering tail; "
    "its hide pale blue-white #DCEBF5 with a paler belly, and down its whole back from skull to tail tip a bold DARK "
    "slate-navy #2B3A55 SPINE of jagged crystal plates, so it reads on snow from above; pale glowing ice-blue #9FE3FF "
    "eyes; its body three times as long as it is tall; "
    "(2) BRIDLE-HOUND, an ancient hunting construct of the same make as the stone-and-bronze guard in the third "
    "reference: lean and built to leap, a long narrow bronze head like a hound's skull with one amber #FFC46B crystal "
    "eye slit, a deep narrow chest of red-gold sandstone #C98A52 drums banded in bronze #A7743A, a thin waist of stacked "
    "bronze rings, long powerful hind legs bent ready to spring, slim forelegs ending in bronze claws, a short stiff "
    "tail of three bronze segments, a bronze collar carved with concentric-circle glyphs, pale frost in its joints; "
    "(3) RIME-BEAST, a placid shaggy herd animal: a big round barrel body on four short thick legs, like a musk ox "
    "crossed with a yak, a broad low head with two wide flat horns curling down and forward, a long shaggy coat of warm "
    "dun-brown #8C6A4C in big solid clumps reaching nearly to the ground, a thick cream-white #EDE6D6 mane over the "
    "shoulders and neck, a dark brown face, a pale blue-grey nose; half as tall as it is long. "
    "BOTTOM ROW, three pieces of an ice-world village built inside the ribcage of a giant frozen creature: "
    "(4) RIB ARCH: ONE enormous curved rib bone of that creature standing as a tall arch over a street: a single thick "
    "smooth bone curve rising from a broad cracked foot set in a mound of packed snow on the left, arching high over and "
    "coming down to a narrower tip buried in a snow mound on the right; twice as tall as it is wide; old ivory bone "
    "#E8DCC4 with darker grey-brown age bands and faint pale-blue translucent crystal veins, a crust of blue-white ice "
    "#CFE6F2 along its top and three thick short icicles under its crown; "
    "(5) KEPT LONGHOUSE: a low herders' longhouse, long and low: walls of stacked grey stone and packed turf to waist "
    "height, a long low rounded roof of overlapping brown and cream hides stretched over curved bone rafters whose ends "
    "cross above the ridge, a round door at the gable end hung with a hide flap and carved bone charms, one small round "
    "window glowing warm amber, and on the roof ridge a squat round stone VENT chimney with a glowing warm orange mouth; "
    "snow on the roof's edges; "
    "(6) THAW RIG, a raiders' heating rig set up on the ice: a heavy squat machine of dark gunmetal #3A3E44 and rusty "
    "red #8A3A26 plates on four short braced legs; an upright cylindrical CORE in a cage of bars glowing orange-hot "
    "#FF7A2A through its vent slots; thick insulated pipes looping out of its sides down to the ground; and on its front "
    "one big pipe COUPLING gripped by a massive bolted steel CLAMP with hazard-yellow and black stripes and a heavy "
    "release lever: the obvious thing to break. "
    "The first reference board is for the creatures' drawing style, the second for the raiders' machines, the third for "
    "the ancient stone-and-bronze make. Nothing white-on-white: every object is clearly darker or more coloured than "
    "snow.")

# Ordered after the chapter (November 12): the Aurora Shear's Muster Tender had been flying as the generic Hauler hull.
TENDER = (
    "Use case: stylized-concept. Asset type: ONE isolated 3D game-model production reference for Coronach, for Meshy "
    "image-to-3D reconstruction, in exactly the art style, line weight, cel shading and finish of the references, which "
    "are design boards of the same raiders' machines: their dark gunmetal and rust-red plating, hazard stripes and "
    "orange-hot thaw cores. NOT any object on those boards. OPAQUE PURE WHITE background, the craft centred with clear "
    "white margin, filling 80 percent of the frame, drawn in ONE three-quarter FRONT view from HIGH above (about 55 "
    "degrees), so its whole top shows. No ground, cast shadow, gloss, bloom, flame, exhaust, smoke, particles, labels or "
    "text; all parts attached. A MUSTER TENDER, a raider support craft that hauls thaw-rig parts and lobs thaw charges: "
    "SQUAT, HEAVY and slow, NOT a fighter, built like a flying barge or a tug: a broad boxy flat-topped hull about 5 m "
    "long, two thirds as wide as it is long and a quarter as tall, with a short armoured cab at the front with a narrow "
    "dark visor slit, and four stubby round thruster pods, one at each corner. In the middle of its flat top, under a "
    "heavy round grille of bars, ONE big round THAW CORE glowing orange-hot #FF7A2A, clearly visible from above; along "
    "both sides of the deck two RACKS of fat cylindrical thaw CHARGES standing upright in rows, each charge banded in "
    "hazard yellow with a glowing orange cap; a stubby lobbing mortar tube on a swivel behind the cab. Plating in dark "
    "gunmetal #3A3E44 with rust-red #8A3A26 panels, black-and-yellow hazard stripes on the bow and corners, soot, "
    "rivets, frost on the cab roof. From far above it reads as one dark rectangle with a glowing orange round core in "
    "the middle, two orange-dotted racks either side and four round pods at the corners.")

KEPT_TAIL = (". Fur on face, forearms and hands. Original UV islands; flat clean cel-shaded game texture, no baked "
             "lighting.")
KEPT = "a short, stocky, furred Kept herder with a round flat face and braided cheek-whiskers"
TERN_BODY = "art-src/Generated/C13-Hoarfell/TernBody_clean.fbx"   # tools/c13_hoarfell_intake.py strips the rigged body to this
GUARD_BODY = "art-src/Generated/C11-Wendmire/AncelaneGuard_clean.fbx"
HUSK_BODY = "art-src/Generated/P3-SentinelHusk/SentinelHusk_clean.fbx"   # the Muster Sergeant's mesh

BATCH = Batch(
    name="C13-Hoarfell", prompt_id="C13",
    bodies={
        "TernBody": ([REFS / "Tern_Natural.png"], "Tern_Body", TERN, 1.45,
                     "Old Tern, short stocky round furred Kept alien woman, dun-brown fur silver at the muzzle, braided "
                     "whiskers, cream fleece cape over brown hides, hide leggings and boots, T pose"),
        # Twice the guard's 2.4 m. Made as drawn, as the guard was: Meshy's T-pose mode dropped the guard's shield and
        # spear.
        "HostMarshal": ([GEN / "C11-Wendmire/refs/Ancelane_Guard.png"], "Host_Marshal", MARSHAL, 4.8,
                        "Host Marshal, huge humanoid construct commander of carved red-gold sandstone drums with bronze "
                        "bands, crested bronze helm with an amber eye slit, stone mantle, frost in the joints, bronze cage "
                        "shield along the left forearm, standard-spear along the right forearm, T pose"),
    },
    as_drawn=("HostMarshal",),
    boards={
        "hoarfell-board": (BOARD, [GEN / "C11-Wendmire/refs/mire-creature-board.png", GEN / "C9-Lantern/refs/muster-camp-board.png",
                                   GEN / "C11-Wendmire/refs/Ancelane_Guard.png"],
                           ["RimeStalker", "BridleHound", "RimeBeast", "RibArch", "KeptLonghouse", "ThawRig"]),
    },
    models={
        "RimeStalker": ("hero", 9000), "BridleHound": ("hero", 9000), "RimeBeast": ("prop", 6000),
        "RibArch": ("prop", 5000), "KeptLonghouse": ("hero", 8000), "ThawRig": ("prop", 6000),
        "MusterTender": ("hero", 8000),
    },
    singles={"MusterTender": (TENDER, [GEN / "C13-Hoarfell/refs/hoarfell-board.png", GEN / "C15-Vaskar/refs/scoria-board.png"], "3:2")},
    reskins={
        # Each matches its portrait sheet; the herder for townsfolk is a grade of Hask's coat (the intake script's).
        "HaskBody": (TERN_BODY,
            "Hask, lead herder in his forties, " + KEPT + ": grey-brown fur weathered pale cream at the brow ruff, a "
            "paler muzzle and throat, dark nose pad; the whisker plaits painted as short grey-brown fur. A heavy dark "
            "brown-black #3A2E26 hide coat over the whole body, its shoulder fleece painted as a high shaggy grey collar, "
            "carved bone toggles down the front, a curved rib-bone whistle on a thong at the throat, dark brown hide "
            "leggings, dark boots. Charcoal-brown and grey-brown only: no cream fleece, no silver", KEPT_TAIL),
        # The first coat (rejected/) painted a bare pink human face on her: this one says fur over the whole face.
        "IruBody": (TERN_BODY,
            "Iru, a young Kept woman, the same short furred herding people. Her whole head and FACE are covered in dark "
            "sleek near-black brown fur #2E241E like an otter's, with a small pale cream muzzle, a black nose pad, dark "
            "eyes, a cream blaze up the middle of the brow ruff and a black soot smudge on the left ear; no bare skin, "
            "no pink, no human face. Long dark whisker plaits. A short brown #6B4A32 hide jerkin with a soft high "
            "collar, the shoulder fleece painted as brown hide, a string of small cream bone lamp-hooks across the chest "
            "like a bandolier, dark leggings, brown boots. Near-black fur, brown hide and bone-cream only",
            KEPT_TAIL),
        # The guard's third coat (the Drowned Sentry was chapter 4's).
        "ThawedGuard": (GUARD_BODY,
            "Thawed Guard, the same carved sandstone and bronze Ancelane sentry construct just cut out of glacier ice: "
            "its red-gold sandstone and bronze still show, but crusted with pale blue-white frost #DDF0FA on the tops of "
            "the helm, shoulders, arms, shield and lance, thick patches of translucent blue ice #8CC8E6 still clinging "
            "around the knees, hips, waist rings and the shield's lower edge, thin white rime along every seam; the eye "
            "slit and chest crystal glowing warm amber #FFC46B as if waking. Half warm stone, half pale ice", HULL_TAIL),
        # The husk's thick forearm becomes the lance housing; the beam itself is drawn in code.
        "MusterExcavator": (HUSK_BODY,
            "Muster Excavator, a heavy armoured dig trooper in a thaw suit: thick plates of soot-blackened gunmetal "
            "#2E3136 with scorched rust edges and hazard-yellow warning stripes on the shoulder plates, every plate "
            "streaked with soot; heat vents on chest, back and shoulders glowing orange-hot #FF7A2A; the larger forearm "
            "painted as a THAW-LANCE housing, a scorched barrel wound with glowing orange-hot coils ending in an "
            "orange-white glowing muzzle at the hand; the other gauntlet a dark claw; the visor a narrow orange-lit slit. "
            "Black, soot and orange heat; no red armour, no cyan", HULL_TAIL),
    },
)

if __name__ == "__main__":
    raise SystemExit(BATCH.main())
