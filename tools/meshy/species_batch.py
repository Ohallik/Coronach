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

# A people's first approved sheet: the anatomy reference for everyone else of that people.
FIRST_OF = {ILU[0]: GEN / "C11-Wendmire/refs/Maru_Natural.png", RAUK[0]: GEN / "C13-Hoarfell/refs/Idra_Natural.png"}

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
        first = FIRST_OF.get(species[0])
        if first and first.exists() and first != out:
            refs = [first, STYLE_REFS[0]]
            prompt += (" The FIRST reference sheet shows another person of this same people: copy the species anatomy and the "
                       "way its expression instrument moves between cells from it, but this is a different person.")
        subprocess.run(MESHY + ["image2image", "--prompt", prompt, "--name", name, "--batch", batch, "--prompt-id", batch.split("-")[0],
                                "--aspect-ratio", "1:1", *sum((["--reference", str(r)] for r in refs), [])], check=True, cwd=ROOT)
    print("SPECIES_OK", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
