#!/usr/bin/env python3
"""Chapter 3, Carrying a Place (Xylos): the generated art, in reviewable stages (docs/chapters/ch03-carrying-a-place.md).

Each stage skips any output that already exists, so a batch can be rerun after a failure. The bridge locks the
manifest, so stages may run beside another batch's tasks.

  portraits   eight sixteen-expression atlases: seven Okhari (Ruva, Teo, Binna, Jask, Senn, Garo, Dabb) and Hadda, a Vael
  refs        reference images for the bodies, creatures, bosses and kit pieces, and the patrol's radio emblem
  isolate     cut the design boards into isolated references (local, free)
  models      image-to-3D for each reviewed reference
  rig         Meshy's biped rig for the Okhari body
  reskin      retextures of rigged bodies (townsfolk and the named Okhari on the Okhari body; Hadda on a Vael body)

  python tools/meshy/c10_xylos_batch.py portraits --only Ruva_Natural
  python tools/meshy/c10_xylos_batch.py refs --only Okhari_Body        # look at it before the mesh
  python tools/meshy/c10_xylos_batch.py models --only OkhariBody,Thornback
  python tools/meshy/c10_xylos_batch.py reskin --only TeoBody,Reedstalker

A rejected output is moved into rejected/ by hand (three first coats are there, each noted on its ledger row); the
stage then makes it again from the corrected prompt. tools/c10_intake.py takes the finished pieces from here: review
renders, colour grades, intake rows, and the portrait sheets' assembly.
"""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from c9_lantern_batch import PORTRAIT_HEADER, VAEL  # noqa: E402  (the atlas layout and the Vael anatomy are shared)

ROOT = Path(__file__).resolve().parents[2]
BATCH = "C10-Xylos"
GEN = ROOT / "art-src" / "Generated" / BATCH
MESHY = [sys.executable, str(ROOT / "tools/meshy/meshy.py")]
MANIFEST = ROOT / "docs" / "art" / "gen-manifest.json"

OKHARI = ("Okhari alien (a different species from the Vael in the references)",
          "Okhari anatomy: a short, broad, amphibious river people. Smooth matte amphibian skin with no scales, warts or "
          "slime, in one flat local colour with a paler cream throat and jawline; a wide flat face with a very broad "
          "expressive mouth and full lips, two small flat nostril slits and no protruding nose; large wide-set round "
          "eyes with dark irises ringed in bright gold on a pale sclera, heavy soft brow ridges; no hair, no external "
          "ears, no horns. From each side of the head, behind the jaw where ears would be, grows one large fan-shaped "
          "GILL-FRILL: a thin ribbed membrane like a fish's fin, as tall as the face, with a brighter coloured rim. The "
          "two frills are the main expression instrument and MUST change clearly between cells: flared wide open when "
          "happy, shocked or laughing; drooped low when sad or worried; pinned flat back against the skull when angry "
          "or determined; one raised and one lowered when curious or smug; half-curled and fluttering when amused or "
          "playful. A thick short neck and sturdy rounded shoulders. Friendly, grounded and likeable, never grotesque: "
          "appealing storybook frog-folk proportions with intelligent eyes. Expressiveness comes from the frills, the "
          "eyes, the brow ridges and the wide mouth.")
NOT_REF = ("NOT any character in the references; only the art style and the sheet layout come from them. ")

PORTRAITS = {
    "Ruva_Natural": (OKHARI, "Character: Ruva Okh, speaker of Rootmarket's seed house, a new old adult Okhari woman, " + NOT_REF +
        "River-clay brown skin with a cream throat, deep creases at the corners of her mouth and under her eyes, a "
        "pale healed scar running across her left frill, frill rims dull ochre yellow, steady unimpressed gold-ringed "
        "eyes. A woven leaf-fibre shawl in faded teal over the shoulders and a heavy collar of many cords of small "
        "seed beads in cream, rust and black, one cord for every harvest she has called. Blunt, warm, shrewd; she never "
        "flatters. Civilian clothing."),
    "Teo_Natural": (OKHARI, "Character: Teo Marrow, a seed-carrier captain, a new middle-aged adult Okhari man, " + NOT_REF +
        "Weathered moss-green skin with a pale throat, sun-faded across the brow, squint lines, his right frill has "
        "a V-shaped notch torn from its rim, frill rims dull orange, tired amused gold-ringed eyes. A high-collared "
        "mustard-yellow oilcloth pilot's coat, patched at one shoulder, and a small brass pilot's whistle on a cord "
        "around his neck. Fatalistic and dry, fond of his boats. Civilian clothing."),
    "Binna_Natural": (OKHARI, "Character: Binna, Rootmarket's cook and provisioner, a new young adult Okhari woman, " + NOT_REF +
        "Bright spring-green skin with a cream throat, round cheeks, frill rims coral pink, quick bright gold-ringed "
        "eyes, a dusting of pale flour on one cheek. A leaf-fibre cook's apron in warm rust red over a cream wrap, a "
        "cord of dried red spice pods around her neck, and the handle of a wooden ladle showing over one shoulder. "
        "Fast-talking, generous and nosy. Civilian clothing."),
    "Jask_Natural": (OKHARI, "Character: Jask, a ferry pole-hand, a new adolescent Okhari boy, clearly younger and smaller "
        "than an adult, " + NOT_REF +
        "Pale yellow-green skin with a cream throat, a narrow young face, very large eager gold-ringed eyes, frills "
        "noticeably too big for his head with bright sky-blue rims. A coiled rope sling worn across the chest over a "
        "sleeveless undyed fibre tunic, a river-reed whistle tucked behind one frill. Earnest, curious, never still. "
        "Civilian clothing."),
    "Senn_Natural": (OKHARI, "Character: Old Senn, a grower who is not leaving, a new very old Okhari man, " + NOT_REF +
        "Dark bark-brown skin gone grey at the throat, deeply creased all over, heavy drooping brow ridges, thin "
        "ragged frills with pale grey rims and one small tear, calm half-lidded gold-ringed eyes. A simple undyed "
        "heavy fibre wrap fastened with one carved wooden toggle. Dry, unhurried, immovable, quietly kind. Civilian clothing."),
    "Hadda_Natural": (VAEL, "Character: Hadda, Rootmarket's mender, a new middle-aged adult Vael woman who married into an "
        "Okhari town, NOT any character in the references; only the art style, sheet layout and species anatomy come "
        "from them. Practical square face with laugh lines, warm amber ring irises, keratin fronds dyed moss green and "
        "cropped short above the collar, soft amber sync-lines at the temples, slate-blue skin. Grower's woven "
        "leaf-fibre wraps in green and cream over a visible metal collar node, a brass jeweller's loupe pushed up on "
        "her brow, a cord of seed beads at the throat. Practical, capable, amused by heroes. NATURAL civilian form."),
    # The script (docs/writing/Xylos.yarn) added two carrier hands, and every speaker needs a set of their own.
    "Garo_Natural": (OKHARI, "Character: Garo, mate of the seed carrier Marrow-1, a new middle-aged adult Okhari, wiry and "
        "lean for their people, neither clearly a man nor a woman, " + NOT_REF +
        "Teal-green skin with a cream throat, a longer narrower face than most Okhari, tired sardonic half-lidded "
        "gold-ringed eyes, frill rims dull red, two small brass rings through the upper edge of the left frill. A "
        "faded red neckerchief knotted at the throat over a sleeveless weathered mustard-yellow oilcloth deck vest, "
        "and a small brass tally-counter hanging on a cord. Dry, worn out, loyal to the boat. Civilian clothing."),
    "Dabb_Natural": (OKHARI, "Character: Dabb, pump hand of the seed carrier Marrow-2, a new young adult Okhari man, big "
        "and round, " + NOT_REF +
        "Olive-yellow skin with a cream throat, a broad round full-cheeked face, wide cheerful gold-ringed eyes, frill "
        "rims pale orange. A patched grey pump-hand's smock with rolled sleeves, and the handle of a wooden bailing "
        "scoop showing over one shoulder, tucked in a belt strap. Earnest, cheerful, tired but game. Civilian clothing."),
}
# Not a face: the radio card of the Compact rescue patrol Steady Hand (speaker id Patrol). One emblem, made by the refs
# stage as Patrol_Emblem; tools/c10_intake.py tiles it into the sixteen cells a portrait set needs.
PATROL_EMBLEM = (
    "OPAQUE WHITE PAPER BACKGROUND, solid white #FFFFFF in every empty region, no transparency, gradient, vignette or "
    "shadow. ONE flat emblem centred with a wide white margin all round, drawn as crisp cel-shaded anime line art with "
    "clean dark teal outlines and flat colours, in the style of a game's dialogue portrait icon: the roundel of a "
    "civil rescue patrol. A clean round badge in two colours only, deep teal #1F7A82 and white: a thick outer ring "
    "of teal anchor chain links, inside it a solid deep teal disc, and on the disc a stylised open hand in white, held "
    "flat and level, palm up, fingers together, seen from the side, steady and calm. A thin white rim between chain "
    "and disc carries small evenly spaced teal tick marks like a compass bezel. No lettering, no numbers, no words, "
    "no stars, no wings, no weapons, no face, no 3D rendering, no gloss.")
PORTRAIT_REFS =[GEN.parent / "P3/portraits/Mira_Natural/Mira_Natural.png", GEN.parent / "P3/portraits/Orrin_Natural/Orrin_Natural.png"]
# Once Ruva's sheet is approved it joins the references, so the other four Okhari share her anatomy.
OKHARI_REF = GEN / "refs" / "Ruva_Natural.png"

# --- The Okhari body: the speaker-body path of tools/meshy/qa_body_batch.py. One mesh, rigged once, carries every Okhari:
# the named four and the growers are retextures of it, so the shape below stays plain enough to wear anyone's clothes.
BODY_HEAD = ("Use case: stylized-concept. Asset type: single 3D game-character modeling reference for Coronach. The attached "
             "portrait sheet is ONLY an identity and style reference: create exactly ONE full-body figure of its character, no "
             "sheet, no extra views. ")
BODY_TAIL = (" No weapon, backpack, cape, tools or floating accessories. Front-facing orthographic full-body view, neutral "
             "expression, head straight aligned with torso, short broad stocky proportions about four heads tall (not tall, "
             "not slender, not human proportions), both arms straight horizontally in a T pose, hands open with separated "
             "fingers, legs clearly apart with a wide white gap between them from the hem down, feet shoulder-width "
             "separated and pointing forward. Entire body, both frills and all fingertips comfortably inside image with "
             "white margin. Flat matte toon-color illustration, crisp restrained linework, only subtle local shading, no "
             "cast or ground shadows, highlights, gloss, ambient occlusion, bloom or dramatic lighting; uniform pure white "
             "background, no scenery, text, labels, dividers or watermark. This is ONE single isolated character only.")
BODIES = {
    # name: (portrait, reference file stem, body description, rig height in metres, short mesh prompt)
    "Okhari": ("Ruva_Natural", "Okhari_Body",
        "Ruva Okh, an old adult Okhari alien woman, speaker of a river town's seed house. The Okhari are a short, broad, "
        "amphibious river people: a large round head on a thick short neck, sturdy rounded shoulders, a wide barrel "
        "torso, short strong arms and short sturdy legs. Keep her exact head from the portrait: a wide flat face of "
        "river-clay brown smooth matte skin with a cream throat and jawline, a very broad mouth, two small nostril slits "
        "and no nose, large wide-set round eyes with dark irises ringed in gold, no hair and no ears; and on EACH side of "
        "the head, where ears would be, one LARGE fan-shaped ribbed GILL-FRILL membrane like a fish's fin, as tall as the "
        "face, spread wide open and flat out to the side, with a dull ochre-yellow rim. Both frills are big, thick enough "
        "to model, symmetrical and clearly visible against the white background above the shoulders. Keep her faded "
        "teal woven leaf-fibre shoulder wrap, worn short and close on the shoulders like a wide collar and NOT hanging "
        "over the arms, and her heavy collar of cords of small seed beads in cream, rust and black. Extend the outfit "
        "naturally: a belted wrap tunic of woven leaf fibre in warm straw-tan that ends at mid-thigh, a broad rust-red "
        "woven sash at the waist, loose knee-length fibre trousers of a darker tan on two clearly separate legs, bare "
        "clay-brown forearms, four-fingered hands with webbing between the fingers, bare clay-brown shins and broad bare "
        "webbed feet with three toes. Warm mid-tone colours, clearly lighter than a dark green-brown jungle floor.",
        1.4, "Ruva Okh, short broad Okhari amphibian alien woman, clay-brown skin, two large fan-shaped gill-frills at the "
             "sides of the head, teal shoulder wrap, seed-bead cords, straw fibre tunic, bare webbed feet, T pose"),
}

# --- Design boards: several pieces on one sheet, as chapter 2 did (tools/meshy/c9_lantern_batch.py).
BOARD_STYLE = (
    "OPAQUE WHITE PAPER BACKGROUND, fully opaque RGB image with solid white #FFFFFF in every empty region. No "
    "transparency, gradient, vignette or halo. CORONACH production design board in EXACTLY the same art style, line "
    "weight, cel shading and materials as the reference boards: crisp cel-shaded anime game art, flat local colors, "
    "even unshadowed studio illumination. No gloss, specular highlights, reflections, rim light, cast shadows, ground "
    "shadows, ambient occlusion, bloom, scenery, floor, water, particles, text, letters, labels, logos, watermark or "
    "borders. Emissive parts are opaque saturated color, not illumination. Every part is visibly attached to its body; "
    "nothing floats, nothing loose. ")
KIT_HEADER = (BOARD_STYLE + "Six separated concept cells in a 3x2 grid, ONE isolated object per cell, three-quarter view "
              "from slightly above, substantial white gutters between cells, each object standing directly on the white ground: ")
CREATURE_HEADER = (
    BOARD_STYLE + "These are 3D model references for game enemies seen by a camera high above them, so each has ONE "
    "bold simple silhouette, thick sturdy limbs, big flat colour shapes and no thin wisps, hair or fur. Six separated "
    "concept cells in a 3x2 grid, ONE whole isolated creature per cell, each drawn large in its cell in a three-quarter "
    "view from slightly above with its head towards the lower left, substantial white gutters between cells, no "
    "creature touching or overlapping another: ")
BOARDS = {
    "river-creature-board": (
        CREATURE_HEADER +
        "(1) THORNBACK, an armoured river grazer built to charge: a low heavy four-legged beast like a boar crossed "
        "with an ankylosaur, a broad wedge head ending in a blunt bony ram plate, small eyes, one big domed back shell "
        "of coral-red plates carrying three rows of short thick cream thorn spines, dusty plum-violet hide on the "
        "belly and four stubby legs, a short club tail; "
        "(2) SPORECASTER, a squat fungal lobber: a fat round toad-like fungus body of pale lilac-mauve with a ring of "
        "frilled cream gills around its base, four short stump feet, two tiny dark eyes, and rising from its back ONE "
        "big upright spore sac like a raised mortar, a swollen saffron-yellow bulb with an open puckered mouth at the "
        "top and orange speckles; "
        "(3) PODSKIMMER, a small fast seed-thief that flies in orbit: a flat manta-like body with two wide swept "
        "fin-wings, a broad open scoop mouth like a shovel at the front, a round amber crop bulging on its back full "
        "of stolen seeds, a short forked tail fin; bright coral-orange shell on top with cream wing edges and a cream "
        "underside, drawn hovering with nothing beneath it; "
        "(4) SEEDTHANE, a TALL many-legged seed-bearer: a compact oval seed-husk body carried high in the air on six "
        "long thick jointed stilt legs like pale buttress roots with rust-red knee joints, a small wedge head with two "
        "amber eyes, a cluster of seven round amber-orange seed pods hanging on short thick stems beneath the body "
        "between the legs, and two large crimson thorn-fans, spread like open hand-fans of long thorns, raised from its "
        "shoulders; bone-cream body and legs; it stands twice as tall as it is wide; "
        "(5) FLOODCROWN, an amphibious giant boss: a huge heavy low-slung body like a giant salamander crossed with a "
        "hippo, four thick short legs with broad webbed feet, a wide flat head with a very broad mouth and two thick "
        "whisker barbels, a thick paddle tail, storm-indigo blue back with a pale cream belly and a row of round amber "
        "spots along each flank; on its head a BROAD flat antler crown, wider than its shoulders, of thick branching "
        "palmate antlers like a moose's in bright golden yellow with glowing pale amber tips; "
        "(6) SNAGVINE NODE, a knot of orbital weed: a tight ball-shaped knot of thick ropey vines in vivid "
        "magenta-violet, a glowing chartreuse-yellow bulb heart showing in the middle, and five thick short barbed "
        "tendrils curling outward tipped with pale yellow hooked barbs, drawn alone with nothing beneath it. "
        "Colours: coral red #D9583F, plum hide #7A5A86, cream bone #EBDDB0, lilac-mauve #B79AD0, saffron #F2B632, "
        "coral orange #F0683C, amber seed #FFB347, crimson #C92F45, rust red #A8442E, storm indigo #3F4C8C, golden "
        "crown #FFC933, magenta-violet #B13FA6, chartreuse #D6F23A. No greens and no browns on any creature: they must "
        "stand out against green-brown jungle ground and teal water."),
    "xylos-kit-board": (
        KIT_HEADER +
        "(1) a SEED BARGE, a big cargo vessel grown from one giant seed pod: a long boat-shaped ribbed woody husk hull, "
        "pointed at both ends like a canoe, with a raised keel ridge, open along the top so a deck of big round cream "
        "seeds shows inside, lashed around the hull with three thick bands of green vine cable knotted on top, a "
        "curled stem prow and a small leaf-shaped rudder fin at the stern; it lies level on the ground; "
        "(2) a SEED HOUSE, the tall granary hall of a river town: a tall round tower-like hall woven from thick roots "
        "like a giant upright basket, wider at the base, with a steep conical roof thatched in big overlapping leaves, "
        "an arched doorway with a plank door at the front, two small round windows, and one big glowing amber "
        "seed-lantern, a hollow seed pod with cut-out holes, hanging on a short cord from a curved root beam above "
        "the door; "
        "(3) a ROOT ARCH: two thick twisted buttress roots rising from wide splayed feet and fusing overhead into one "
        "heavy natural arch tall enough to walk under, bark ridges, a few patches of moss and three short hanging "
        "vines under the span; "
        "(4) a MARKET STALL with a leaf awning: a sturdy wooden counter with baskets of red and yellow pods and round "
        "fruit on it, four thick pole posts lashed with cord, and a slanted awning roof made of five huge overlapping "
        "broad green leaves with yellow rims; "
        "(5) a SLUICE GATE for a river channel: a heavy gate of vertical timber planks bound with rust-red cord, held "
        "between two thick living root posts, a timber crossbeam on top, and a big wooden spoked crank wheel mounted "
        "on one post with a coil of rope; "
        "(6) a SPICE POD PLANT: a knee-high bushy jungle plant with broad green leaves and a dozen bright red and "
        "orange chilli-shaped pods hanging in clusters, growing from a small mound of earth. "
        "Materials: root bark #7A5638, pale root wood #B98A5A, woven straw fibre #D6B36A, husk amber-tan #C8964E, leaf "
        "green #5E9A4A, deep leaf #3F6E3A, vine cable green #6FA046, cream seeds #EBDDB0, amber seed light #FFC46B, "
        "rust-red cord #A8442E, pod red #D9483A, pod yellow #F2B632."),
}
BOARD_REFS = {
    "river-creature-board": [GEN.parent / "C9-Lantern/refs/lantern-kit-board.png", GEN.parent / "P41-BellowsBelow/refs/bellows-below.png"],
    "xylos-kit-board": [GEN.parent / "C9-Lantern/refs/lantern-kit-board.png", GEN.parent / "C9-Lantern/refs/muster-camp-board.png"],
}
BOARD_PIECES = {
    "river-creature-board": ["Thornback", "Sporecaster", "Podskimmer", "Seedthane", "Floodcrown", "SnagvineNode"],
    "xylos-kit-board": ["SeedBarge", "SeedHouse", "RootArch", "MarketAwning", "SluiceGate", "SpicePodPlant"],
}
# A piece whose board cell isn't good enough gets a sheet to itself: made only when named with --only, and it then
# replaces the isolated cell as the model's reference.
SINGLE_STYLE = (
    "OPAQUE WHITE PAPER BACKGROUND, fully opaque RGB image with solid white #FFFFFF in every empty region. No "
    "transparency, gradient, vignette or halo. Drawn in EXACTLY the same art style, line weight, cel shading and "
    "materials as the reference images: crisp cel-shaded anime game art, flat local colors, even unshadowed studio "
    "illumination. No gloss, specular highlights, reflections, rim light, cast shadows, ground shadows, ambient "
    "occlusion, bloom, scenery, floor, water, particles, text, labels, watermark or borders. Emissive parts are opaque "
    "saturated color, not illumination. Every part is visibly rooted in its body; nothing floats. Use case: 3D model "
    "reference. ONE isolated subject, three-quarter front view from slightly above, whole body centred with wide "
    "white margins. ")
SINGLES: dict[str, tuple[str, list[Path]]] = {}

# name: (band, target polycount). Bosses and the hull keep more triangles; small pieces need few.
MODELS = {
    "Thornback": ("hero", 8000), "Sporecaster": ("hero", 8000), "Podskimmer": ("prop", 5000),
    "Seedthane": ("hero", 12000), "Floodcrown": ("hero", 15000), "SnagvineNode": ("prop", 5000),
    "SeedBarge": ("hero", 8000), "SeedHouse": ("hero", 8000), "RootArch": ("prop", 6000),
    "MarketAwning": ("prop", 5000), "SluiceGate": ("prop", 5000), "SpicePodPlant": ("prop", 3000),
}

# Meshy's retexture prompt holds 800 characters at most, tail included: reskin() checks before it spends.
# The first coat (Teo) came back in black gloves and boots: the Okhari go barehanded and barefoot, so the tail says so.
SKIN_TAIL = (". Bare skin everywhere outside the clothes: face, frills, arms, bare hands, shins and bare webbed feet are all "
             "this one skin colour; no gloves, no boots, no black. Original UV islands; flat clean cel-shaded game texture, "
             "no baked lighting.")
HULL_TAIL = ". Preserve palette semantics and original UV islands; flat clean cel-shaded game texture, no baked lighting."
OKHARI_SKIN = "a short broad amphibian Okhari with a big fan-shaped gill-frill on each side of the head and gold-ringed dark eyes"
OKHARI_BODY = "art-src/Generated/C10-Xylos/OkhariBody_clean.fbx"   # tools/c10_intake.py strips the rigged body to this
RESKINS: dict[str, tuple[str, str, str]] = {
    # name: (source clean fbx, prompt, tail). Each of the named four matches its portrait sheet's colours and clothes.
    "TeoBody": (OKHARI_BODY,
        "Teo Marrow, a middle-aged river captain, " + OKHARI_SKIN + ": weathered moss-green olive skin, pale cream throat, "
        "frills the same olive green with thin dull orange rims. A golden-yellow #D9A520 oilcloth pilot's coat with a high "
        "collar covers the shoulders, chest and tunic, one square brown shoulder patch, a dark brown leather belt, a small "
        "brass whistle on a cord; dark brown knee trousers. Yellow, not orange; no teal, no red sash, no bead necklaces",
        SKIN_TAIL),
    "BinnaBody": (OKHARI_BODY,
        "Binna, a young cook, " + OKHARI_SKIN + ": bright spring-green skin, cream throat, green frills with coral-pink "
        "rims. A warm rust-red cook's apron with shoulder straps covers the chest and the front of a cream wrap tunic, a "
        "cream cloth tie at the waist, a necklace of small dried red spice pods, cream knee trousers. No teal, no mustard, "
        "no brown skin", SKIN_TAIL),
    "SennBody": (OKHARI_BODY,
        "Old Senn, a very old grower, " + OKHARI_SKIN + ": dark bark-brown creased skin, ash-grey throat, thin grey-brown "
        "frills with pale grey rims. A simple heavy undyed fibre wrap in pale grey-beige covers the shoulders, chest and "
        "tunic, one carved dark wooden toggle at the chest, a plain rope belt, grey-beige knee trousers. Muted and plain: no "
        "dye, no teal, no red, no beads", SKIN_TAIL),
    "JaskBody": (OKHARI_BODY,
        "Jask, an adolescent ferry hand, " + OKHARI_SKIN + ": pale yellow-green skin, cream throat, pale yellow-green "
        "frills with bright sky-blue rims. A sleeveless undyed cream fibre tunic, a thick tan braided rope coiled around the "
        "shoulders and across the chest, a tan rope belt, pale tan knee trousers. Light, plain and young: no teal, no red, "
        "no beads", SKIN_TAIL),
    "OkhariGrower": (OKHARI_BODY,
        "A river grower in plain work clothes, " + OKHARI_SKIN + ": sage-green skin, cream throat, sage-green frills with "
        "pale yellow rims. A plain undyed straw-coloured work tunic and knee trousers, earth-stained at the hems, a faded "
        "leaf-green woven shoulder wrap, a tan rope belt and one single cord of cream seed beads. Ordinary working clothes: "
        "no red sash, no teal", SKIN_TAIL),
    # Her first coat came back blonde in orange leggings: "keratin fronds" and "tan" meant nothing to the painter, so the
    # second says hair, gives the greens as numbers and names the colours it may not use.
    "HaddaBody": ("art-src/Generated/P3-Mira/Mira_clean.fbx",
        "Hadda, a middle-aged Vael alien mender woman who lives with river growers. Long hair of thick flat strands, all "
        "dark moss green #4F6F3C from root to tip. Muted slate grey-blue skin #8090A8 with thin amber lines on cheeks, "
        "throat and arms. A brown leather headband, a dark metal collar with one small amber light, a cord of seed beads. "
        "Growers' woven leaf-fibre wraps: a cream chest wrap with a moss-green shoulder sash, a cream and moss-green wrap "
        "skirt, dark moss-green leggings, soft brown boots, brown wrist wraps. Only green, cream, brown and slate blue: no "
        "blond or white hair, no orange, no red",
        ". Face, neck, arms and hands are all this skin colour" + HULL_TAIL),
    # Creatures and machines that keep an existing rig or hull: only what the old silhouette can honestly carry.
    "Reedstalker": ("art-src/Generated/P3-Ridgehound/Ridgehound_clean.fbx",
        "Reedstalker, a river ambush predator that lies under water among reeds: smooth sleek hide in pale reed-straw yellow "
        "with long darker gold-olive stripes along the flanks like reed stems, every crest plate and back ridge painted as "
        "bundled dry reeds in cream and pale straw, a dark teal-black belly, legs and muzzle as if soaked, small glowing "
        "amber eyes, pale bone claws. Pale and striped, to stand out on dark green-brown mud and teal water; no orange "
        "armour, no rust, no fur", HULL_TAIL),
    # Its first coat was asked for silver-grey driftwood and came back as a stone golem with a teal back: cold colours on
    # a machine's plates read as rock or steel. The second is warm wood from end to end.
    "Rootwarden": ("art-src/Generated/P3-SentinelHusk/SentinelHusk_clean.fbx",
        "Rootwarden, a guardian grown from living tree roots, made only of wood: every armour plate is a slab of rough "
        "bark in warm pale driftwood tan #C9B28A with deep brown vertical bark furrows and growth rings on the flat faces, "
        "the joints and limbs between plates are bundles of twisted rust-red roots, bright green moss on the shoulders, "
        "head and upper back, small glowing amber sap beads in the seams and one amber eye slit. Warm wood colours only: "
        "no grey, no blue, no teal, no steel, no stone, no paint", HULL_TAIL),
    "ClaimDrone": ("art-src/Generated/QA-Drone/SurveyDrone_clean.fbx",
        "Hulljack salvage company claim drone, a hostile industrial machine: hazard-yellow painted hull plates with bold "
        "black warning chevrons and chipped paint at the edges, dark gunmetal seams, ring, struts and fins, one large round "
        "glowing red-orange emitter lens on the front (a claim-tag projector), a small white stencilled tally mark, a "
        "red-orange underglow vent. High contrast and crisp; no cream, no cyan", HULL_TAIL),
}


def run(args: list[str]) -> None:
    subprocess.run(MESHY + args, check=True, cwd=ROOT)


def wanted(name: str, only: list[str] | None) -> bool:
    return not only or name in only


def portraits(only: list[str] | None) -> None:
    out = GEN / "refs"
    for name, (species, character) in PORTRAITS.items():
        if only and name not in only:
            continue
        if (out / f"{name}.png").exists():
            print("C10_SKIP", name, flush=True)
            continue
        refs = list(PORTRAIT_REFS)
        if species is OKHARI and name != "Ruva_Natural" and OKHARI_REF.exists():
            refs = [OKHARI_REF, PORTRAIT_REFS[0]]
        prompt = PORTRAIT_HEADER.format(species=species[0], anatomy=species[1]) + character
        if species is OKHARI and name != "Ruva_Natural" and OKHARI_REF.exists():
            prompt += (" The FIRST reference sheet shows another Okhari (an old woman): copy the species anatomy, the frill "
                       "shape and the way the frills move between expressions from it, but this is a different person.")
        run(["image2image", "--prompt", prompt, "--name", name, "--batch", BATCH, "--prompt-id", "C10",
             "--aspect-ratio", "1:1", *sum((["--reference", str(r)] for r in refs), [])])
    print("C10_PORTRAITS_OK", flush=True)


def refs(only: list[str] | None) -> None:
    out = GEN / "refs"
    for name, (portrait, stem, body, _height, _short) in BODIES.items():
        if not wanted(stem, only) or (out / f"{stem}.png").exists():
            continue
        run(["image2image", "--prompt", BODY_HEAD + body + BODY_TAIL, "--name", stem, "--batch", BATCH, "--prompt-id", "C10",
             "--reference", str(out / f"{portrait}.png"), "--aspect-ratio", "1:1"])
    for name, prompt in BOARDS.items():
        if not wanted(name, only) or (out / f"{name}.png").exists():
            continue
        run(["image2image", "--prompt", prompt, "--name", name, "--batch", BATCH, "--prompt-id", "C10",
             "--aspect-ratio", "3:2", *sum((["--reference", str(r)] for r in BOARD_REFS[name]), [])])
    if wanted("Patrol_Emblem", only) and not (out / "Patrol_Emblem.png").exists():
        run(["text2image", "--prompt", PATROL_EMBLEM, "--name", "Patrol_Emblem", "--batch", BATCH, "--prompt-id", "C10",
             "--aspect-ratio", "1:1"])
    for name, (prompt, references) in SINGLES.items():
        if not only or name not in only or (out / f"{name}.png").exists():
            continue
        run(["image2image", "--prompt", SINGLE_STYLE + prompt, "--name", name, "--batch", BATCH, "--prompt-id", "C10",
             "--aspect-ratio", "1:1", *sum((["--reference", str(r)] for r in references), [])])
    print("C10_REFS_OK", flush=True)


def isolate(only: list[str] | None) -> None:
    for board, names in BOARD_PIECES.items():
        sheet = GEN / "refs" / f"{board}.png"
        if not wanted(board, only) or not sheet.exists():
            continue
        if all((GEN / "isolated" / f"{n}.png").exists() for n in names):
            print("C10_ISOLATE_SKIP", board, flush=True)
            continue
        subprocess.run([sys.executable, str(ROOT / "tools/isolate_board.py"), str(sheet), str(GEN / "isolated"), *names],
                       check=True, cwd=ROOT)
        for name in names:
            drop_neighbours(GEN / "isolated" / f"{name}.png")
    print("C10_ISOLATE_OK", flush=True)


def drop_neighbours(path: Path) -> None:
    """A cell's box can catch the tip of the next creature's tail or tendril, and image-to-3D would model the scrap.
    Keep the one large shape in the cell and paint everything else white."""
    import numpy as np
    from PIL import Image
    from scipy import ndimage
    image = Image.open(path).convert("RGB")
    rgb = np.asarray(image).copy()
    labels, count = ndimage.label(ndimage.binary_dilation((rgb < 235).any(axis=2), iterations=9))
    if count < 2:
        return
    areas = ndimage.sum(labels > 0, labels, range(1, count + 1))
    rgb[labels != int(np.argmax(areas)) + 1] = 255
    Image.fromarray(rgb).save(path)
    print("C10_ISOLATE_CLEANED", path.name, f"dropped {count - 1}", flush=True)


def reference_for(name: str) -> Path:
    single = GEN / "refs" / f"{name}.png"
    return single if single.exists() else GEN / "isolated" / f"{name}.png"


def img3d_task_from_manifest(file_name: str) -> str | None:
    """A later run can rig a mesh an earlier run generated: the provenance ledger knows the task."""
    for row in reversed(json.loads(MANIFEST.read_text(encoding="utf-8-sig"))):
        if row.get("mode") != "img3d":
            continue
        if any(path and Path(path).name.lower() == file_name.lower() for path in (row.get("downloaded") or []) + [row.get("output_path") or ""]):
            return row.get("task_id")
    return None


def models(only: list[str] | None) -> None:
    for name, (_portrait, stem, _body, _height, short) in BODIES.items():
        body = f"{name}Body"
        if not wanted(body, only) or (GEN / f"{body}_Meshy.fbx").exists():
            continue
        run(["img3d", "--image", str(GEN / "refs" / f"{stem}.png"), "--band", "humanoid", "--target-polycount", "12000",
             "--batch", BATCH, "--prompt-id", "C10", "--name", f"{body}_Meshy", "--riggable", "--prompt", short])
    for name, (band, polycount) in MODELS.items():
        if not wanted(name, only):
            continue
        if (GEN / f"{name}.fbx").exists():
            print("C10_MODEL_SKIP", name, flush=True)
            continue
        run(["img3d", "--image", str(reference_for(name)), "--name", name, "--batch", BATCH, "--prompt-id", "C10",
             "--band", band, "--target-polycount", str(polycount)])
    print("C10_MODELS_OK", flush=True)


def rig(only: list[str] | None) -> None:
    for name, (_portrait, _stem, _body, height, _short) in BODIES.items():
        body = f"{name}Body"
        if not wanted(body, only) or (GEN / f"{body}_Rigged.fbx").exists():
            continue
        task = img3d_task_from_manifest(f"{body}_Meshy.fbx")
        if not task:
            sys.exit(f"C10_NO_MESH_TASK {body}")
        run(["rig", "--input-task-id", task, "--height", str(height), "--batch", BATCH, "--prompt-id", "C10",
             "--name", f"{body}_Rigged"])
    print("C10_RIG_OK", flush=True)


def reskin(only: list[str] | None) -> None:
    for name, (model, prompt, tail) in RESKINS.items():
        if not wanted(name, only):
            continue
        if (GEN / f"{name}.fbx").exists():
            print("C10_RESKIN_SKIP", name, flush=True)
            continue
        if len(prompt + tail) > 800:
            sys.exit(f"C10_RESKIN_PROMPT_TOO_LONG {name}: {len(prompt + tail)} of 800")
        source = ROOT / model
        if not source.is_file():
            sys.exit(f"C10_RESKIN_NO_SOURCE {name}: {model} (tools/c10_intake.py strips the rigged body first)")
        run(["retexture", "--model", str(source), "--prompt", prompt + tail, "--name", name, "--batch", BATCH, "--prompt-id", "C10"])
    print("C10_RESKIN_OK", flush=True)


STAGES = {"portraits": portraits, "refs": refs, "isolate": isolate, "models": models, "rig": rig, "reskin": reskin}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("stage", choices=sorted(STAGES))
    parser.add_argument("--only", help="comma-separated names")
    args = parser.parse_args()
    GEN.mkdir(parents=True, exist_ok=True)
    STAGES[args.stage](args.only.split(",") if args.only else None)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
