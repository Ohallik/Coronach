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
        subprocess.run(MESHY + ["image2image", "--prompt", prompt, "--name", name, "--batch", batch, "--prompt-id", batch.split("-")[0],
                                "--aspect-ratio", "1:1", *sum((["--reference", str(r)] for r in STYLE_REFS), [])], check=True, cwd=ROOT)
    print("SPECIES_OK", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
