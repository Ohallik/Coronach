#!/usr/bin/env python3
"""Chapter 4, Breathing Room (Wendmire): the generated bodies, creatures and kit (docs/chapters/ch04-breathing-room.md).

The five portrait sheets are tools/meshy/species_batch.py's. Stages and their order are tools/meshy/batch_stages.py's;
tools/c11_intake.py takes the pieces from here.

  python tools/meshy/c11_wendmire_batch.py refs --only Maru_Body          # look at it before the mesh
  python tools/meshy/c11_wendmire_batch.py models --only MaruBody
  python tools/meshy/c11_wendmire_batch.py rig --only MaruBody
  python tools/meshy/c11_wendmire_batch.py reskin --only OnaBody,DovBody

The Ilu are one body (Maru's, from her own sheet); Ona, Dov and Quill are retextures of it and Pim a colour grade of
Maru's own coat (the intake script's). The Ancelane guard is one rigged construct that three chapters dress: chapter 4's
Drowned Sentry is its first coat. The Lurelight is the pulse beacon in a sickly grade.
"""
from __future__ import annotations

from batch_stages import BODY_FLAT, BODY_HEAD, CREATURE_HEADER, HULL_TAIL, KIT_HEADER, ROOT, Batch

GEN = ROOT / "art-src" / "Generated"
REFS = GEN / "C11-Wendmire" / "refs"
POSE = (" Front-facing orthographic full-body view, neutral expression, head straight aligned with torso, both arms straight "
        "horizontally in a T pose, legs clearly apart with a white gap between them, feet shoulder-width apart and pointing "
        "forward. Entire figure and everything on both arms comfortably inside the image with white margin. ")

MARU = (
    BODY_HEAD + "Maru Wend, an adult Ilu alien woman in her thirties, a raft pilot. The Ilu are a tall, slender, long-necked "
    "wading people: she is very tall and lean, about eight heads tall, with a long slender neck a full head long, narrow "
    "sloping shoulders, long slim arms and very long slim legs like a heron's; graceful, never insect-like. Keep her exact "
    "head from the portrait: sun-faded lilac matte skin, paler on the face and throat, a long gentle face narrowing to a "
    "short soft rounded muzzle, very large dark almond eyes, no hair and no ears, a row of small pale cyan glow-spots "
    "along each brow ridge and cheekbone and down both sides of the neck; and her two ANTENNAE: long feathered plumes "
    "like a moth's rising from the brow in an upright V, each as long as her head is tall and as thick as her wrist, the "
    "left one kinked halfway up; both large, solid and clearly visible against the white background. Keep her sleeveless "
    "oiled-cloth pilot's vest in faded teal over a high woven-reed collar, and the tide-cord of knotted teal and cream "
    "rope looped twice around her neck. Extend the outfit naturally: the vest belted at the waist with a broad woven-reed "
    "sash holding a small coil of cream line, loose straw-coloured reed-cloth trousers that end just below the knee on "
    "two clearly separate legs, long bare lilac arms with a row of cyan glow-spots down the outside of each forearm and a "
    "knotted tide-cord at one wrist, bare lilac shins and long bare narrow feet. Light, clear colours: lilac, faded teal "
    "and straw. No weapon, pole, pack or loose accessories; hands open with long separated fingers." + POSE + BODY_FLAT)

GUARD = (
    "Use case: stylized-concept. Asset type: single 3D game-character modeling reference for Coronach, in EXACTLY the art "
    "style, line weight, cel shading and carved-stone-and-bronze materials of the FIRST reference, a design board (its "
    "lantern tower, arch and column are the same makers' work). The SECOND reference is a different character in a "
    "different style, shown for ONE thing only: how a shield and a tool are mounted ALONG the forearms in a T pose; copy "
    "that mounting and nothing else from it, no orange, no armour plates, no face. Create exactly ONE full-body figure, "
    "no sheet, no extra views, no scenery. An ANCELANE GUARD: an ancient sentry construct, a tall broad humanoid statue that walks. It is carved from "
    "warm red-gold sandstone #C98A52 in stacked rounded drums and blocks with darker sandstone #8E5A36 in the seams and a "
    "thin bronze #A7743A band at every joint, like the lantern tower in the reference; nothing organic, no skin, no "
    "cloth. A clear humanoid build: a smooth domed bronze helm for a head with one horizontal eye slit holding a glowing "
    "amber #FFC46B crystal, a short thick neck, a broad chest block bearing a round disc carved with concentric circle "
    "glyphs around a small amber crystal, big round shoulder drums, a narrower waist of stacked rings, cylindrical arms "
    "and legs in stacked drums, heavy square bronze-shod feet, bronze hands with thick fingers. Strapped flat along the outside "
    "of ITS LEFT forearm (the viewer's right), a TALL SHIELD: a long rectangular slab of carved sandstone with a bronze "
    "rim and three concentric-circle glyphs in a row. In this T pose the shield is HORIZONTAL: it lies along the "
    "outstretched arm like a plank, its long edges parallel to the arm, reaching from the elbow to two hand-lengths past "
    "the fingertips, three times as long as it is wide, its carved face toward the viewer. It is NOT upright, NOT "
    "vertical, and no part of it rises above the shoulder or hangs below the elbow's height by more than its own half "
    "width. (When the arm is lowered the shield will hang down along the leg; that is why.) ITS RIGHT forearm (the viewer's left) carries a "
    "SPEAR: a straight bronze lance fixed along the top of the forearm, with a long leaf-shaped blade that projects a "
    "full forearm's length beyond the fist. Shield and spear are parts of the body, not held and not loose. Warm mid-tone "
    "stone and bronze, clearly lighter than a dark floor." + POSE.replace("neutral expression, head straight", "head straight")
    + "Flat matte toon-color illustration, crisp restrained linework, only subtle local shading, no cast or ground shadows, "
    "highlights, gloss, ambient occlusion, bloom or dramatic lighting; uniform pure white background, no text, labels, "
    "dividers or watermark. This is ONE single isolated figure only.")

CREATURES = (
    CREATURE_HEADER +
    "(1) ROTBLOOM, a boss: a floating predatory bloom colony as big as a house, seen so its top shows. A broad low "
    "rosette of twelve thick fleshy petals in pale bone-white #EFE3D0 flushed pale pink at the tips, like a giant water "
    "lily crossed with a sea anemone, resting on a ring of pale swollen float-bladders. At its centre one wide round "
    "THROAT, livid magenta-crimson #C2185B darkening to purple-black, ringed with pale tooth-like stamens. Under the "
    "lifted outer petals four smaller FEEDING MOUTHS gape outward around its rim, each a livid crimson maw with pale "
    "teeth. Six thick pale root-lashes trail from beneath it, lying curled on the ground. Unmistakably a predator: the "
    "pale petals and the livid throat read from far above against dark water; "
    "(2) LURE STALK, a tall lure grown by the same colony: one thick pale bone-white fleshy stalk with livid magenta "
    "veins, rising from a splayed foot of five thick roots, three times as tall as it is wide, curving over at the top "
    "like a shepherd's crook to hang one round glowing amber #FFC46B bulb inside a cage of six thin pale petal-ribs, a "
    "living copy of a caged street lamp; a collar of short thorns below the crook; "
    "(3) BOGSTRIDER, a long-legged swamp wader: a compact heavy oval body in pale sand-cream #E6D3A8 carried high on "
    "four very long thick stilt legs whose lower halves are burnt orange #D9662B like stockings, each ending in a broad "
    "splayed three-toed mud foot; a strong neck curving forward and down to a heavy wedge head with a blunt shovel beak, "
    "a bright coral-red #E2504A throat sac, a low ridge of rounded plates down its back; it stands twice as tall as its "
    "body is long; "
    "(4) VEIL RAY, a large gliding predator of open space, drawn from above and slightly in front so its whole back "
    "shows: a great manta with broad pointed wings, two forward-curving horn fins beside a wide mouth, and a long whip "
    "tail ending in a flat blade; its back pale gold #F1D58A with a bold pattern of amber-orange #F08A2E chevrons, a "
    "cream leading edge to each wing and a row of six round glowing amber eyespots along each wing's trailing edge; "
    "hovering with nothing beneath it, elegant and dangerous; "
    "(5) ROT POD, a rooted spore turret: a fat upright bulb like a giant tulip bud on a squat foot of thick pale roots, "
    "its four thick pale bone-white petals half open like jaws to show a livid magenta-crimson throat holding one round "
    "pale yellow spore ball; livid veins on the outside of the petals, a ring of short thorns round its base; as tall as "
    "a person; "
    "(6) GLOW FUNGUS, a cluster of five thick-stemmed swamp mushrooms of different heights growing from one small mound "
    "of dark peat, with broad pale lilac caps and glowing pale cyan #8FEFF2 gills and spots. "
    "The colony family (1, 2 and 5) shares pale bone-white flesh and a livid magenta throat. Nothing dark green, dark "
    "brown or black: every creature must stand out against dark swamp water and a dusk-violet sky.")

KIT = (
    KIT_HEADER +
    "(1) a REED RAFT section from a floating village: a square flat deck of tightly woven golden reed matting lashed with "
    "rope onto a frame of thick bundled-reed floats, perfectly square in plan with straight clean edges so that sections "
    "can sit edge to edge, a low rail of bundled reeds a hand high along ONE side only, nothing standing on the deck; "
    "(2) a WEAVING HALL, the landmark of the raft village: a long hall whose roof is a steep pointed barrel arch of thick "
    "bundled-reed ribs covered in reed thatch, an open arched gable front with a doorway and one caged amber lamp hung "
    "above it, standing on its own woven raft platform; along its far side three TALL drying racks of lashed poles, "
    "taller than the roof, each hung with broad woven mats in straw, indigo and faded teal stripes; "
    "(3) a PULSE BEACON, the village's lamp-post: a tall slender pole of bundled reeds bound with five rope bands, "
    "standing on a small round weighted reed-basket foot, with a short curved arm at the top from which hangs one round "
    "lamp inside an open woven-reed cage, the lamp glowing warm amber, and one short knotted cord of teal and cream "
    "tied below the arm; four times as tall as it is wide; "
    "(4) an ANCELANE PUMP, ancient machinery in the carved style of the reference board's lantern tower: a chest-high "
    "drum of red-gold sandstone in stacked rings with thin bronze bands and a band of concentric circle glyphs, a big "
    "bronze spoked hand-wheel mounted upright on its front, a thick bronze pump cylinder rising from its top with a "
    "curved bronze spout pipe, and one small amber crystal set in the stone above the wheel; "
    "(5) a SMOKEHOUSE hut of the raft village: a small round hut with walls of upright bundled reeds and a domed reed "
    "thatch roof with a short clay smoke-pot chimney, a low doorway hung with a woven mat, a rack of four split fish "
    "drying beside the door, on its own small raft platform; "
    "(6) a MOORING POST: three thick reed-bundle poles lashed together into a short tripod post with a heavy coil of "
    "cream rope hung on it and an iron mooring ring. "
    "Materials: golden reed #D9B45A, pale dry reed #E8D08A, dark wet reed #8C6B32, rope cream #E6DCC0, indigo cloth "
    "#3E4A8C, faded teal #5E9A98, amber lamp #FFC46B, red-gold sandstone #C98A52, darker sandstone #8E5A36, bronze #A7743A.")

# Ordered after the chapter (November 12): the Spore Veil's floating pod had been borrowing the rooted Rot-pod's mesh.
SPORE = (
    "Use case: stylized-concept. Asset type: ONE isolated 3D game-model production reference for Coronach, for Meshy "
    "image-to-3D reconstruction, in exactly the art style, line weight, cel shading and finish of the reference board, "
    "whose creatures live in the same swamp sky. NOT any creature on the board. OPAQUE PURE WHITE background, the object "
    "centred with clear white margin, filling 75 percent of the frame, drawn in one three-quarter view from slightly "
    "above. No ground, cast shadow, gloss, highlights, bloom, particles, spores in the air, labels or text. A SPORE POD, "
    "a free-floating spore sac that drifts in a spore nebula: one swollen, taut, EGG-SHAPED pod, a little taller than it "
    "is wide, its smooth membrane stretched tight, pale cream-green #E4EBC8, paler near the crown, with a network of "
    "darker violet #5A3A7A veins branching up from the bottom over its whole surface; just below the crown a ring of "
    "seven round soft GLOWING SPOTS, lime-green #B6F27A with pale centres, evenly spaced; the crown closed in a small "
    "puckered vent; and hanging from the bottom one SHORT frayed stalk, a quarter of the pod's height, ending in a few "
    "ragged torn fibres, trailing straight down. NO roots, NO tentacles, NO petals, NO mouth, NO legs, nothing on the "
    "ground: it floats. One bold simple silhouette that reads on a dark violet sky from far above.")

SKIN_TAIL = (". Bare skin everywhere outside the clothes: face, neck, antennae stems, arms, hands, shins and bare feet are "
             "all this one skin colour with small pale cyan glow-spots; no gloves, no boots, no black. Original UV islands; "
             "flat clean cel-shaded game texture, no baked lighting.")
ILU = "a tall slender long-necked Ilu with two feathered lilac antennae and very large dark eyes"
MARU_BODY = "art-src/Generated/C11-Wendmire/MaruBody_clean.fbx"   # tools/c11_intake.py strips the rigged body to this
GUARD_BODY = "art-src/Generated/C11-Wendmire/AncelaneGuard_clean.fbx"

BATCH = Batch(
    name="C11-Wendmire", prompt_id="C11",
    bodies={
        # Rig heights include what stands above the crown: the Ilu's antennae add about a ninth to her 2.1 m.
        "MaruBody": ([REFS / "Maru_Natural.png"], "Maru_Body", MARU, 2.35,
                     "Maru Wend, very tall slender long-necked Ilu alien woman, lilac skin, two feathered antennae, faded teal "
                     "pilot's vest, reed collar, straw knee trousers, barefoot, T pose"),
        # The first reference came back with the shield upright at the hand, which would swing through the hips as soon as
        # the arm dropped; Idra's braced body, whose brace lies along her forearm, now shows the mounting.
        "AncelaneGuard": ([GEN / "C9-Lantern/refs/lantern-kit-board.png", GEN / "C13-Hoarfell/refs/Idra_Braced_Body.png"],
                          "Ancelane_Guard", GUARD, 2.4,
                          "Ancelane guard, tall humanoid sentry construct of carved red-gold sandstone drums with bronze bands, "
                          "domed bronze helm with an amber eye slit, tall shield along the left forearm, bronze lance along "
                          "the right forearm, T pose"),
    },
    as_drawn=("AncelaneGuard",),
    boards={
        "mire-creature-board": (CREATURES, [GEN / "C10-Xylos/refs/river-creature-board.png", GEN / "P41-BellowsBelow/refs/bellows-below.png"],
                                ["Rotbloom", "LureStalk", "Bogstrider", "VeilRay", "RotPod", "GlowFungus"]),
        "driftreed-kit-board": (KIT, [GEN / "C10-Xylos/refs/xylos-kit-board.png", GEN / "C9-Lantern/refs/lantern-kit-board.png"],
                                ["ReedRaft", "WeavingHall", "PulseBeacon", "AncelanePump", "Smokehouse", "MooringPost"]),
    },
    models={
        "Rotbloom": ("hero", 15000), "LureStalk": ("prop", 5000), "Bogstrider": ("hero", 10000), "VeilRay": ("hero", 10000),
        "RotPod": ("prop", 5000), "GlowFungus": ("prop", 3000),
        # AncelanePump was left unmodelled when the chapter closed; the chapter 5-6 run made it from the same isolated cell.
        "ReedRaft": ("prop", 5000), "WeavingHall": ("hero", 8000), "PulseBeacon": ("prop", 4000), "AncelanePump": ("prop", 5000),
        "Smokehouse": ("prop", 6000), "MooringPost": ("prop", 3000),
        "SporePod": ("prop", 5000),
    },
    singles={"SporePod": (SPORE, [GEN / "C11-Wendmire/refs/mire-creature-board.png"], "1:1")},
    reskins={
        # Each matches its portrait sheet's clothes; all five Ilu share one lilac, Ona's a shade deeper.
        "OnaBody": (MARU_BODY,
            "Ona Sedge, mistress of a weaving hall, in her fifties, " + ILU + ": deep violet skin, paler on the face. A long "
            "indigo #3E4A8C weaver's coat covers the vest and trousers to the knee, with a high layered collar of woven reed "
            "in straw and indigo and a carved bone comb pinned at the throat, a plain straw sash. Dark indigo and straw only: "
            "no teal, no red", SKIN_TAIL),
        "DovBody": (MARU_BODY,
            "Dov Hale, a smokehouse keeper in his forties, " + ILU + ": smoke-greyed lilac skin. A rust-brown #9A4B2E oiled "
            "shirt with a stiff dark brown leather apron bib over the chest and belly, brine-stained pale at the hem, a "
            "brown smoked-cloth neckerchief, dark brown knee trousers. Warm browns and rust only: no teal, no indigo",
            SKIN_TAIL),
        "QuillBody": (MARU_BODY,
            "Quill, a very old pump-wright, " + ILU + ": pale washed-out lilac skin. A patched mid-grey waistcoat with many "
            "small pockets and brass fittings over a cream shirt, a fine brass chain at the collar, small round brass-rimmed "
            "lenses on the muzzle, cream knee trousers. Grey, cream and brass only: no teal, no indigo, no rust", SKIN_TAIL),
        # Chapter 4's coat on the shared construct. Chapters 6 and 7 add theirs (Thawed Guard, Glass Sentinel).
        "DrownedSentry": (GUARD_BODY,
            "Drowned Sentry, the same carved sandstone and bronze guard after centuries under swamp water: the stone stained "
            "pale grey-green with darker tide lines and patches of pale barnacle, the bronze bands, helm, shield rim and lance "
            "gone to pale verdigris teal #6FB3A8, strands of dark wet weed and pale root hanging from the shoulders, the "
            "shield and the lance, the eye slit and chest crystal glowing sickly pale green-cyan. Pale and cold, to stand out "
            "against dark water and dark stone; no warm orange, no dry sand", HULL_TAIL),
    },
)

if __name__ == "__main__":
    raise SystemExit(BATCH.main())
