#!/usr/bin/env python3
"""The peoples of chapters 4 to 10 and their named faces: sixteen-expression portrait atlases, by chapter batch.

One species block per people, so every chapter's batch draws the same anatomy. The first sheet of a new people is
ordered alone and looked at before the rest (it then joins the references for its people).

  python tools/meshy/species_batch.py --only Maru_Natural,Idra_Natural
  python tools/meshy/species_batch.py --chapter 4
"""
from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from c9_lantern_batch import PORTRAIT_HEADER, VAEL, VASKAR  # noqa: E402,F401

ROOT = Path(__file__).resolve().parents[2]
GEN = ROOT / "art-src" / "Generated"
MESHY = [sys.executable, str(ROOT / "tools/meshy/meshy.py")]
STYLE_REFS = [GEN / "P3/portraits/Mira_Natural/Mira_Natural.png", GEN / "P3/portraits/Orrin_Natural/Orrin_Natural.png"]
NOT_REF = "NOT any character in the references; only the art style and the sheet layout come from them. "

ILU = ("Ilu alien (a different species from the Vael in the references)",
       "Ilu anatomy: a tall, slender, long-necked wading people. Smooth matte skin in one flat local colour from dusk "
       "violet to grey-lilac, paler on the face and throat; a long gentle face that narrows to a short soft rounded "
       "muzzle with two small nostrils and a small expressive mouth; very large dark almond eyes with a thin pale "
       "ring; no hair, no external ears, no horns. Two long FEATHERED ANTENNAE, soft and plume-like as a moth's, rise "
       "from the brow. They are the main expression instrument and MUST change clearly between cells: perked upright "
       "when alert or happy; drooped down beside the face when sad or worried; swept flat back over the skull when "
       "angry or determined; one curled forward when curious or smug; both curled into loose spirals when amused or "
       "playful. A row of small round GLOW-SPOTS runs along each brow ridge and cheekbone and down the sides of the "
       "long neck: pale cyan dots in flat colour with no bloom, bright when happy, shocked or laughing and dim when "
       "sad or tired. A long slender neck and narrow sloping shoulders. Elegant, gentle and likeable, never insectoid "
       "or frightening: a graceful heron-and-moth storybook people with warm intelligent eyes.")
RAUK = ("Rauk alien (a different species from the Vael in the references, and NOT human)",
        "Rauk anatomy: a broad, powerfully built mountain people who look carved from stone. Skin like cut slate: matte, "
        "fine-grained, one flat local colour from charcoal to ash grey, slightly paler on the face, with a few clean "
        "angular FACET lines at the cheekbones and jaw as if the face were chiselled. Instead of hair, a single CREST OF "
        "SEVEN TO NINE BROAD FLAT HORN-PLATES: rigid mineral slabs like overlapping roof slates or the back plates of a "
        "stegosaur, standing upright along the midline of the skull from brow to nape, tallest at the crown, each plate "
        "clearly a separate thick slab with a pale chipped edge. The skull on either side of the crest is bare smooth "
        "slate. Absolutely no hair, no braids, no cornrows, no strands, no stubble. No external ears: one small smooth "
        "horn nub where each ear would be. Two heavy BROW PLATES, separate slabs of horn above the eyes, are the main "
        "expression instrument and MUST change clearly between cells: raised high when shocked or happy, lowered hard "
        "when angry or determined, one cocked when curious or smug, tilted together when worried or sad. A broad flat "
        "nose whose bridge runs up into the brow plates, and a short guard of horn along the chin. Thin HEAT-SEAMS, "
        "narrow ember-orange lines like cracks in cooled lava, run from the brow plates down the cheeks and throat, in "
        "flat saturated colour with no bloom. Deep-set eyes with pale silver ring irises on a pale sclera, a wide firm "
        "mouth. Stoic, solid and quietly warm; striking and likeable, never brutish and never simply a grey human.")
# The Lastlight herders of Hoarfell (chapter 6): they call themselves the Kept.
KEPT = ("Kept alien (a furred people of their own, a different species from the Vael in the references, and NOT human)",
        "Kept anatomy: a short, stocky, barrel-chested herding people of an ice world, built round and solid against the "
        "cold. The whole head is covered in dense soft FUR drawn in clean cel-shaded clumps, one flat local colour with "
        "a paler muzzle and throat. A broad, round, flat-fronted face; a short blunt muzzle only a little forward of the "
        "cheeks, with a broad dark leathery nose pad and a small soft mouth with dark lips (no split lip, no fangs, no "
        "teeth on show except when laughing); round dark eyes with pale ring irises, set deep; SMALL rounded ears set low "
        "on the sides of the head, half hidden in the fur. No hair on the scalp, no human nose, no beard. Two features "
        "are the expression instrument and MUST change clearly between cells: a THICK BROW RUFF, a heavy shelf of long "
        "coarse fur right across the forehead above the eyes, and long CHEEK-WHISKERS, a bundle of long thick whiskers "
        "growing from each cheek, long enough to braid. Ruff and whiskers move together: whiskers fanned up and "
        "forward with the ruff lifted when happy, playful or laughing; whiskers hanging straight down with the ruff "
        "sagging low over the eyes when sad or tender; whiskers bristled stiff straight out sideways with the ruff "
        "pulled down hard and low when angry, intense or determined; whiskers flared rigid and the ruff shot up high "
        "when shocked; whiskers drawn back flat against the cheeks with the ruff bunched up in the middle when worried "
        "or flustered; one side's whiskers lifted and the ruff raised on that side only when curious, smug or amused; "
        "whiskers level and still and the eyes half closed when synced. A thick short neck lost in fur, round heavy "
        "shoulders under layered hides and carved bone. They are their own people: NOT cats (no pointed ears, no slit "
        "pupils), NOT bears (no long snout, no round ears on top of the head), NOT dogs or seals, and NOT humans with "
        "fur. Weathered, patient and likeable; soft-spoken herders with kind, knowing eyes.")
# The Meridian Crawlers (chapter 9). Their eyes are never seen: the goggles' shutters carry every expression.
# Dorrow's first sheet (Dorrow_Natural_v1_rejected.png) had working shutters, but its Happy was two wide lenses and nothing
# else, the same face as Shocked, and it was painted with grunge texture instead of the house's flat cel-shading. The
# block now draws Happy as smiling crescents and asks for flat colour.
CRAWLER = ("Crawler alien (a small goggled people of their own, a different species from the Vael in the references, and "
           "NOT human)",
           "Crawler anatomy: a small, round-headed, stocky people of a moving engine-town, about half as tall as a Vael, "
           "with a big round head as wide as the narrow sloping shoulders. Drawn in the SAME clean flat cel-shading as the "
           "references, flat colour areas and crisp outlines, no painterly or grunge texture: soot and grease are a few "
           "flat dark shapes. Smooth soot-dark charcoal skin; no hair, no visible ears (a close leather cap covers the skull). "
           "Every Crawler is WRAPPED TO THE NOSE: a thick layered cloth face-wrap covers the chin, mouth and nose in EVERY "
           "cell, so no mouth and no nose are ever seen, and over the eyes in every cell sit big round BRASS GOGGLES with "
           "thick dark leather cups and strap. Each lens has a LEAF SHUTTER: a ring of thin overlapping curved brass "
           "leaves like a camera's iris, which closes in from the rim to leave an opening of dark glass in the middle. "
           "The two shutters are the WHOLE of a Crawler's expression and MUST change clearly between cells; nothing else "
           "on the face moves. In the expression list below every word about mouths, brows and eyes means the shutters, "
           "exactly so: Neutral both half open; Happy both wide open while the cheeks rise under the wrap and push thick "
           "creased folds of wrap and leather up over the LOWER edge of both goggles, so each lens shows as an upturned "
           "crescent like smiling eyes, head tilted and shoulders lifted (Happy must never look like Shocked); Sad both "
           "fully shut, head bowed; Angry both closed to narrow slits slanting down toward the middle; Shocked both "
           "fully open and perfectly round, head pulled back, the brass rims catching a hard white highlight; Worried "
           "both narrowed, unevenly, one more than the other; Intense both closed to small round pinholes; Synced both "
           "half open with a bright clean glint on each lens; Amused one almost shut to a pinhole and the other half open, "
           "a wink that stays; Smug one half open, the other a level slit, head tilted back; Curious one wide open and "
           "one half open, head tilted; Flustered both lenses FOGGED, the glass misted milky white; Tender the ONLY cell "
           "with eyes: the goggles pushed up onto the forehead, showing two soft round dark eyes with pale ring irises "
           "inside two pale clean rings of skin where the goggles kept the soot off; Determined both narrowed to level "
           "horizontal slits; Playful one wide open and one fully shut, head cocked; Special both squeezed shut with the "
           "leather creased hard at the cheeks, head thrown back laughing. Not robots, not humans in welding gear: a "
           "cheerful, sturdy, likeable small people, practical and grimy.")

# A people's first approved sheet: the anatomy reference for everyone else of that people.
FIRST_OF = {ILU[0]: GEN / "C11-Wendmire/refs/Maru_Natural.png", RAUK[0]: GEN / "C13-Hoarfell/refs/Idra_Natural.png",
            KEPT[0]: GEN / "C13-Hoarfell/refs/Tern_Natural.png", CRAWLER[0]: GEN / "C16-Home/refs/Dorrow_Natural.png",
            VASKAR[0]: GEN / "C9-Lantern/refs/Hessa_Natural.png"}
# A sheet that should be drawn from one particular person of its people instead of the first (a sibling, a build).
ANATOMY_REF = {"Drask_Natural": GEN / "C9-Lantern/refs/Brekk_Natural.png"}

# name: (chapter, batch, species, character)
PORTRAITS = {
    "Maru_Natural": (4, "C11-Wendmire", ILU, "Character: Maru Wend, a raft pilot of Driftreed, a new adult Ilu woman in her "
        "thirties, " + NOT_REF + "Lean even for her people, sun-faded lilac skin with a pale throat, her left antenna "
        "kinked halfway up from an old break, sharp amused dark eyes, bright glow-spots. A sleeveless oiled-cloth "
        "pilot's vest in faded teal over a high woven-reed collar, and a tide-cord of knotted teal and cream rope "
        "looped twice around her long neck. Restless, sharp-tongued, proud of her trade. Civilian clothing."),
    "Idra_Natural": (6, "C13-Hoarfell", RAUK, "Character: Idra Sorn, a rescue-cutter officer of the Veyr frontier guard, a "
        "new adult Rauk WOMAN in her thirties, " + NOT_REF + "She must read unmistakably as a woman of her people: a "
        "narrower chin and finer jaw than a Rauk man's though still strong, high cheek facets, fuller lips, larger "
        "eyes, a longer neck, and a slimmer crest whose plates sweep slightly back. Ash-grey skin, an old pale burn "
        "scar across her left brow plate and temple that has chipped the plate's edge, steady pale silver eyes that "
        "miss nothing, the faintest dry humour at one corner of her mouth. A rescue-orange high collar with a steel "
        "clasp over plain grey fatigues, and a coil of orange rescue line worn over her right shoulder. Dry, exact, "
        "impatient with speeches, deadpan; kind in practical ways. NATURAL off-duty form, no armour, no helmet."),
    "Meret_Natural": (5, "C12-ManyHands", VAEL, "Character: Meret Venn, director of the Anchor Compact and a public route "
        "engineer, a new adult Vael woman in her sixties, NOT any character in the references; only the art style, sheet "
        "layout and species anatomy come from them. A long composed face with fine age lines at the eyes and mouth, pale "
        "steady grey-green ring irises, silver-white keratin fronds cropped close to the skull and swept back, faint dim "
        "teal sync-lines at the temples, cool slate-grey skin. A white field coat with teal piping and a high plain "
        "collar, worn soft at the edges, a small teal ring-shaped pin at the collar, an engineer's brass stylus clipped "
        "at the collar's edge. Courteous, exact, tired and dry, with kind eyes. Her expressions are RESTRAINED and never "
        "theatrical: Happy is a small real smile, Angry is cold stillness with a set mouth, Shocked is a lifted brow and "
        "parted lips, Sad is a lowered gaze, Special is a quiet closed-eyed breath of a laugh. NATURAL civilian form."),
    "Dessik_Natural": (5, "C12-ManyHands", VAEL, "Character: Dessik Varn, called Hulljack, a salvage boss, a new heavy-set "
        "adult Vael man in his fifties, NOT any character in the references; only the art style, sheet layout and "
        "species anatomy come from them. A broad jowled face on a thick neck, small sharp amber ring irises, keratin "
        "fronds shaved down to short grey stubble ridges, an old pale burn scar across the right side of the jaw, dull "
        "orange sync-lines, ruddy warm-grey skin. A hazard-yellow and gunmetal salvage harness collar over a stained "
        "grey work shirt, and a fine brass chain at the collar leading down out of frame to a ledger. A clerk's "
        "patience on a brawler's frame: reasonable, petty, tired, never a pirate in his own eyes. NATURAL civilian form."),
    "Ona_Natural": (4, "C11-Wendmire", ILU, "Character: Ona Sedge, mistress of Driftreed's weaving hall, a new adult Ilu woman "
        "in her fifties, " + NOT_REF + "Deep violet skin with a paler face, a composed, stern, careful face with fine "
        "lines at the eyes, long straight antennae held very still. IMPORTANT: her glow-spots are DIM, barely lit, in "
        "the Neutral, Sad, Angry, Worried, Intense, Amused, Smug, Curious and Determined cells, and bright only in "
        "Happy, Tender, Playful, Synced and Special: she is grieving and hiding it. A high layered collar of woven "
        "reed in undyed straw and indigo, with a carved bone weaver's comb pinned at the throat. Exact, unhurried, "
        "unflattered; her warmth is rare. Civilian clothing."),
    "Dov_Natural": (4, "C11-Wendmire", ILU, "Character: Dov Hale, Driftreed's smokehouse keeper and trader, a new adult Ilu man "
        "in his forties, " + NOT_REF + "Broad and solid for his people, smoke-greyed lilac skin, a wide cheerful face "
        "with crinkled eyes, short thick antennae that never keep still, bright glow-spots. A stiff brine-stained "
        "leather apron bib over a rust-brown oiled shirt, a neckerchief of smoked reed-cloth. Loud, cheerful, a gossip "
        "who is usually right. Civilian clothing."),
    "Quill_Natural": (4, "C11-Wendmire", ILU, "Character: Quill, Driftreed's old pump-wright, a new very old Ilu man, " + NOT_REF +
        "Stooped, with pale washed-out lilac skin, thin wispy antennae that tremble, small round brass-rimmed lenses "
        "perched on his muzzle, soft faded glow-spots. A patched grey waistcoat whose many small pockets bulge with "
        "brass fittings, a loop of fine chain at the collar. Delighted by machinery, vague about people, gentle. "
        "Civilian clothing."),
    "Pim_Natural": (4, "C11-Wendmire", ILU, "Character: Pim, an apprentice weaver of Driftreed, a new adolescent Ilu, clearly "
        "younger and smaller than an adult and neither clearly a boy nor a girl, " + NOT_REF + "Light lilac skin, a "
        "narrow young face with very large dark eyes, short antennae not yet grown long, very bright glow-spots. A "
        "plain undyed reed-cloth tunic with a half-woven strip of mat worn as a sash over one shoulder and a belt cord "
        "tied in a careful knot. Frightened but practical, earnest, a little embarrassed. Civilian clothing."),
    "Idra_Braced": (6, "C13-Hoarfell", RAUK, "Character: Idra Sorn again, the SAME woman as in the first reference sheet (same "
        "face, same burn scar across the left brow plate, same crest, same pale silver eyes), now BRACED in her cutter "
        "frame, a rescue rig that has unfolded around her. Heavy hazard-orange and brushed-steel collar and shoulder "
        "plates rise around her neck, with black panel lines and a row of rivets; a steel visor is pushed UP onto her "
        "brow above the brow plates so her whole face shows; the plates of her crest are capped with thin steel edge "
        "guards; her heat-seams are a little brighter. A coil of orange rescue line is clipped at the right shoulder "
        "plate. Same dry, exact, deadpan character. No helmet over the face, no weapon in frame."),
    # Chapter 6, Lastlight: the Kept (Old Tern first, alone; she is the Kept reference), and Hessa Korl's elder sister.
    "Tern_Natural": (6, "C13-Hoarfell", KEPT, "Character: Old Tern, herd-reader of Lastlight, a new VERY OLD Kept woman, " +
        NOT_REF + "Small and bowed with age, seated and bundled in three layered hides (a pale shaggy fleece over a brown "
        "hide over a dark one), only her head and shoulders showing. Warm dun-brown fur gone silver-white at the muzzle "
        "and throat; a long grizzled brow ruff so long that it droops down over her LEFT eye in every cell; long silver "
        "cheek-whiskers braided into thin plaits, each plait tipped with small carved bone beads; bright, amused, very "
        "alive dark eyes in a deeply weathered face. A carved bone counting-stick, notched along its length, tucked "
        "through the hides across her chest. Unhurried, exact, teasing; she tells things once. Her most used faces are "
        "Amused (a slow sly half-smile, eyes crinkled), Neutral, Tender, Worried and Determined: make those clearly "
        "readable. Civilian clothing."),
    "Hask_Natural": (6, "C13-Hoarfell", KEPT, "Character: Hask, lead herder of Lastlight, a new adult Kept man in his "
        "forties, " + NOT_REF + "Broad and heavy even for his people, thick-necked. Grey-brown fur weathered pale, almost "
        "cream, at the brow ruff; cheek-whiskers cropped SHORT for work, a stiff blunt brush on each cheek that still "
        "lifts, droops and bristles with his mood; steady practical dark eyes. His lips are slightly parted in most "
        "cells, as if counting under his breath. A heavy hide coat with a high shaggy collar fastened with carved bone "
        "toggles, and a herding whistle carved from a curved rib bone hung on a thong at his throat. Practical, plain, "
        "patient. Amused, Neutral, Determined and Worried are his most used faces. Civilian clothing."),
    "Iru_Natural": (6, "C13-Hoarfell", KEPT, "Character: Iru, keeper of Lastlight's lamp shrine, a new young adult Kept "
        "woman, " + NOT_REF + "Smaller and lighter-built than the older herders, with dark, sleek, glossy near-black "
        "brown fur, a clear pale cream BLAZE running up through the middle of her brow ruff, and very long fine "
        "cheek-whiskers. A smudge of black lamp soot on her left ear. In the calmer cells her eyes look past the viewer "
        "into the middle distance. A short hide jerkin with a high soft collar, a string of small carved bone "
        "lamp-hooks worn across her chest like a bandolier, and the bone handle of a wick-knife at her collar strap. "
        "Quick and shy, then not shy. Her used faces are Determined, Flustered, Curious, Worried, Happy, Tender and "
        "Shocked: make those clearly readable. Civilian clothing."),
    "Vessa_Natural": (6, "C13-Hoarfell", VASKAR, "Character: Vessa Korl, a Muster dig-master, a new adult Vaskar woman, "
        "the ELDER SISTER of Hessa Korl, who is the woman on the first reference sheet: give Vessa the same family face "
        "(the same long narrow face, plate pattern, cheekbones and amber eyes), a few years older, leaner and harder, "
        "but she is NOT Hessa and looks nothing like her in colouring or manner. Her plates are the dull cold grey of "
        "slag, with fine white FROST rime along every plate edge; her two crest-sails are kept FURLED TIGHT, folded close "
        "and flat along the back of the skull, never flared; no earring, no sash, no jewellery or ornament of any kind. "
        "The plain high padded collar of a dark gunmetal cold-weather dig coat, and a small dark dig-master's writing "
        "slate in a plain frame hung on a cord at her chest. Eyes that weigh things. Cold where Hessa is proud; she "
        "thinks in tonnage. Her expressions are RESTRAINED and economical: Neutral, Determined and Smug are almost the "
        "same cold level face (Determined a fraction narrower in the eyes, Smug one corner of the mouth lifted a little); "
        "the other cells move more, but she never looks warm, open or theatrical, and her sails only ever loosen "
        "slightly. Each cell must still be distinguishable. NATURAL form, no armour, no helmet."),
    # Chapter 7, Oriel.
    "Lume_Natural": (7, "C14-Oriel", VAEL, "Character: Lume, apprentice lamp-keeper of Lanternwick, a new YOUNG Vael woman "
        "of about eighteen, NOT any character in the references; only the art style, sheet layout and species anatomy "
        "come from them. A narrow young face, cool pale blue-grey skin, HEAVY-LIDDED eyes with grey-gold ring irises that "
        "have already seen what you are about to do, short dark charcoal keratin fronds cropped short and tied back at "
        "the nape with a length of pale lamp wick, faint amber sync-lines at the temples. A plain soot-grey keeper's "
        "smock with a high round collar, a coil of thin light rope over her left shoulder, and the small bone handle of "
        "a wick-knife in a sheath on the smock's neck strap. Dry beyond her years; her resting face is patience. Her "
        "expressions are dry and small: Amused is NO smile at all, only one eyelid lower than the other; Happy is the "
        "one real smile on the sheet, eyes wide, caught off guard as if it surprised her; Neutral is calm patience. "
        "NATURAL civilian form."),
    # Chapter 8, Harrowgate: four Vaskar (Drask drawn from Brekk's build) and a Vael tug captain.
    "Drask_Natural": (8, "C15-Vaskar", VASKAR, "Character: Warmaster Drask Vaal, commander of the Muster, a new adult "
        "Vaskar man in his fifties, " + NOT_REF + "The largest person in any room and the stillest: a huge heavy head and "
        "very broad shoulders. Plates the warm brown of old bronze, worn smooth at the brow by thirty years of helmets; "
        "full broad crest-sails in dark bronze with small nicks along the trailing edges, carried HALF OPEN and at ease. "
        "Deep-set calm amber eyes that rest on the viewer as if nobody else were on the field. A plain rust-red "
        "soldier's sash across a plain dark undershirt, with two small brass Warmaster's tabs on the sash at the "
        "shoulder, and NOTHING else: no armour, no medals, no cloak. Warm to his own, patient, certain; his resting face "
        "is a host's, welcoming. IMPORTANT: he must never look as if he could be angry. The Angry cell is NOT angry: it "
        "is a cold stillness, the warmth gone out of his eyes, mouth closed and level, no bared teeth, no knitted brow, "
        "sails unmoved. Determined is calm certainty, not heat; Intense is a level, patient, unblinking regard; Happy "
        "and Tender are warm and fatherly. NATURAL form, no war-frame, no helmet, no weapon."),
    "Tovra_Natural": (8, "C15-Vaskar", VASKAR, "Character: Tovra Kesh, forewoman of the Bridle Foundry, a new adult "
        "Vaskar woman in her forties, " + NOT_REF + "A forewoman's build, heavy shoulders and a thick neck, yet she reads "
        "clearly as a woman of her people. Plates of dark iron-brown dulled by soot that no longer washes out, small "
        "pale pitted spatter-burn scars on her neck and shoulder plates, grey slag caught in the seams between plates; "
        "her crest-sails folded FLAT down the back of the head and tied together with a leather thong so they don't "
        "catch sparks. Amber eyes ringed DARK with exhaustion, the first thing anyone notices: she has not slept "
        "properly in a long time. A heavy scorched leather smith's apron with a bib over a sleeveless grey shirt, and the "
        "long iron handles of a pair of tongs pushed through the apron's bib strap at her shoulder. Blunt, exhausted, "
        "exact about metal. Determined (a hard tired set jaw) and Neutral (flat, worn out) are her most used faces; "
        "Smug is brief and bitter; Flustered is caught off guard at being told what to do. Civilian work clothes."),
    "Odrun_Natural": (8, "C15-Vaskar", VASKAR, "Character: Mother Odrun, who keeps the hidden kitchen of the Unsworn "
        "quarter, a new VERY OLD Vaskar woman, " + NOT_REF + "Short for her people and broad, with a heavy round face. "
        "Plates gone PALE and CHALKY with age, almost bone-white grey, with deep seams; her RIGHT crest-sail torn long ago "
        "in a ragged split that was never mended, and her LEFT crest-sail folded down round her shoulders and wrapped "
        "there like a shawl; sharp pale amber eyes that look at everyone as if they were late. A plain faded brown work "
        "dress with the sleeves rolled up, an old cream kitchen apron, white flour dusting her shoulders and collar, and "
        "a long wooden ladle tucked upright in the apron's bib strap so its bowl stands beside her face, in every cell "
        "except Determined, where the ladle has been put down and is gone. Unimpressed by everyone. Her expressions are "
        "FLAT and dry: Neutral is her most used face, a hard waiting look; Amused is a flat mouth and one raised brow "
        "plate, no smile; Determined is the same face as Neutral. Civilian clothing."),
    "Pip_Natural": (8, "C15-Vaskar", VASKAR, "Character: Pip, a Muster cadet, a new Vaskar BOY of twelve or thirteen, "
        "clearly a child, much younger and smaller than an adult, " + NOT_REF + "All elbows: a thin neck and narrow bony "
        "shoulders. Plates still PALE sandy grey and soft-edged, not yet hardened; crest-sails far too big for his head, "
        "pale with pink-orange edges, never quite folded, sticking out a little unevenly; big eager amber eyes. A cadet's "
        "plain grey uniform shirt with a high collar and a grey sash pressed very flat across it, one small brass pip on "
        "the rank tab at the shoulder. Proud of the wrong things, eager, earnest. Happy is his most used face (a big "
        "open boyish grin); Smug is a child reciting something he was told, chin up and eyes half shut; Worried, Curious "
        "and Flustered must read clearly. No armour, no weapon."),
    "Bryn_Natural": (8, "C15-Vaskar", VAEL, "Character: Captain Bryn Asta of the tug Welcome Back, a new adult Vael woman "
        "in her forties, NOT any character in the references; only the art style, sheet layout and species anatomy come "
        "from them. A square practical face with laugh lines she isn't using and tired shadows under the eyes (she has "
        "not slept), a controller's level gaze with grey ring irises, dark copper-brown keratin fronds cut SQUARE and "
        "short under a tug crew's headset (a band over the crown, one ear cup and a small boom microphone at the left "
        "jaw), faint orange sync-lines, warm grey-tan skin. The high collar of a tug captain's heavy navy-blue work "
        "jacket, with a diagonal hull stripe of signal orange edged in white across the collar. Traffic-controller calm "
        "over real fear. Her expressions are level and tired: Amused is gallows humour, the mouth moves and the eyes "
        "don't; Sad looks like someone doing arithmetic; Tender is quiet; Neutral is a controller's level gaze. NATURAL "
        "civilian form."),
    # Chapter 9, the Meridian Crawl and the Halo: the Crawlers (Dorrow first, alone; he is the Crawler reference) and Jun.
    "Dorrow_Natural": (9, "C16-Home", CRAWLER, "Character: Chief Engineer Dorrow of the Meridian Crawl, a new old Crawler "
        "man in his sixties, " + NOT_REF + "Round and heavy-set, a round head on round shoulders. A dark grey-green "
        "face-wrap, a battered brown leather cap, and old brass goggles whose LEFT lens (on the viewer's right) is "
        "CRACKED straight across with a clear white crack line in every cell, never replaced; black engine grease smeared "
        "on the wrap, collar and shoulders. A thick quilted canvas engineer's jacket in faded ochre, stiff with grease, "
        "and a brass chief's whistle on a cord round his neck. Cheerful about catastrophe: Happy is his most used face "
        "and must read as unmistakably cheerful from the shutters and the creased leather alone. Civilian work clothes."),
    # Tam's first sheet (Tam_Natural_v1_rejected.png) copied the old man's cracked lens from the reference onto Tam's own
    # goggles, which blurs Dorrow's one identifying mark, and showed the strap only once round.
    "Tam_Natural": (9, "C16-Home", CRAWLER, "Character: Tam, the Chief Engineer's apprentice, a new ADOLESCENT Crawler, "
        "clearly younger and smaller than an adult, " + NOT_REF + "A slighter young build: thin narrow shoulders and a "
        "neck that shows, a smaller round head. The same kind of brass goggles but a size TOO BIG for Tam's head, "
        "sitting wide of the face, and the leather strap wound TWICE round the head, two parallel straps clearly visible "
        "across the cap. Both lenses are WHOLE and clean in every cell: NO crack, no chip, no scratch on either lens (the "
        "cracked lens belongs to the old man on the first reference sheet, never to Tam). A rust-red face-wrap, a close "
        "grey leather cap, a patched grey-blue canvas work smock, and a "
        "small battered notebook hanging on a string round the neck. Wonder, then seasickness: Curious is the most used "
        "face; Flustered, with the lenses fogged, is seasickness; Shocked, Determined and Tender must read clearly. "
        "Civilian work clothes."),
    "Jun_Natural": (9, "C16-Home", VAEL, "Character: Jun, who has sold fruit on the Cinder Halo's promenade for "
        "thirty-one years, a new VERY OLD Vael woman of seventy or more, NOT any character in the references; only the "
        "art style, sheet layout and species anatomy come from them. Small and very upright, a small composed old face "
        "with fine deep lines, pale grey ring irises, thin pale silver keratin fronds mostly hidden under a faded "
        "blue-green headcloth tied at the nape, faint dim green sync-lines, warm grey skin. A worn canvas apron stained "
        "with thirty-one years of pear juice over a plain dark high-collared blouse, and in every cell one small old "
        "hand holds a ripe yellow-green PEAR up at the bottom of the frame (the only hand on the sheet). Her face hardly "
        "moves: every expression is small and restrained, though each cell must still be distinguishable; Neutral is "
        "her face, and Tender is the same face as Neutral with the eyes lowered. NATURAL civilian form."),
}

# Not a face: the Last Warden (chapter 7) is a mask. One text2image of it in its level white light; tools/warden_sheet.py
# then tiles it into the sixteen cells and re-lights the glass low and amber for Tender, as ch07-manifest.md asks.
# The first mask (Warden_Mask_v1_rejected.png) had the right design but came back as rendered metal, grainy and glossy,
# beside portraits that are all flat cel line art; the style now leads the prompt and names the render look it may not use.
MASKS = {
    "Warden_Mask": (7, "C14-Oriel",
        "FLAT 2D ANIME CEL ILLUSTRATION, NOT a 3D render: drawn exactly like the armour in a cel-shaded anime game's "
        "dialogue portraits, with thick clean dark ink outlines around every shape, flat bronze colour fills, ONE flat "
        "darker shadow tone and at most a few flat pale highlight shapes. No metal texture, grain, noise, scratches, "
        "hammered surface, specular gloss, reflections or soft gradients. OPAQUE WHITE PAPER BACKGROUND, solid white "
        "#FFFFFF in every empty region, no transparency, vignette, floor or shadow. Use case: a game dialogue portrait. "
        "ONE object only, centred, seen straight from the front, filling about three quarters of the "
        "height with a wide white margin all round: THE LAST WARDEN, an ancient guardian that is only a mask with nothing "
        "behind it. A bronze-and-glass guardian's mask, clearly TALLER than it is wide, like the long narrow face of a "
        "knight's helm with no head inside: a long smooth bronze visor tapering to a blunt chin, with NO mouth, no nose "
        "and no eyes; a raised bronze brow band across the top, stamped with a simple emblem of a ring with one "
        "horizontal bar through it; and where eyes would be, ONE single long LEVEL SLOT of thick clear glass running "
        "straight across the face, framed all round by bronze and stopping short of the mask's sides, with hard-edged "
        "cold WHITE fibre-light shining evenly behind the glass. Dark aged bronze #7A5A2E, warmer bronze #A7743A on the "
        "raised edges, crusted bright green-teal VERDIGRIS #4FA58C in every seam, rivet and engraved line. The glass "
        "slot is the only clean thing on it. A few short broken bronze fixings at the back edge show it is hollow. No "
        "body, neck, shoulders, hands, stand, cables, text, letters, numbers or glow halo outside the slot."),
}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", help="comma-separated names")
    parser.add_argument("--chapter", type=int)
    a = parser.parse_args()
    only = a.only.split(",") if a.only else None
    for name, (chapter, batch, species, character) in PORTRAITS.items():
        if (only and name not in only) or (a.chapter and chapter != a.chapter):
            continue
        out = GEN / batch / "refs" / f"{name}.png"
        if out.exists():
            print("SPECIES_SKIP", name, flush=True)
            continue
        prompt = PORTRAIT_HEADER.format(species=species[0], anatomy=species[1]) + character
        refs = list(STYLE_REFS)
        first = ANATOMY_REF.get(name, FIRST_OF.get(species[0]))
        if first and first.exists() and first != out:
            refs = [first, STYLE_REFS[0]]
            prompt += (" The FIRST reference sheet shows another person of this same people: copy the species anatomy and the "
                       "way its expression instrument moves between cells from it, but this is a different person.")
        subprocess.run(MESHY + ["image2image", "--prompt", prompt, "--name", name, "--batch", batch, "--prompt-id", batch.split("-")[0],
                                "--aspect-ratio", "1:1", *sum((["--reference", str(r)] for r in refs), [])], check=True, cwd=ROOT)
    for name, (chapter, batch, prompt) in MASKS.items():
        if (only and name not in only) or (a.chapter and chapter != a.chapter):
            continue
        if (GEN / batch / "refs" / f"{name}.png").exists():
            print("SPECIES_SKIP", name, flush=True)
            continue
        subprocess.run(MESHY + ["text2image", "--prompt", prompt, "--name", name, "--batch", batch, "--prompt-id",
                                batch.split("-")[0], "--aspect-ratio", "1:1"], check=True, cwd=ROOT)
    print("SPECIES_OK", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
