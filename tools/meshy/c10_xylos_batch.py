#!/usr/bin/env python3
"""Chapter 3, Carrying a Place (Xylos): the generated art, in reviewable stages (docs/chapters/ch03-carrying-a-place.md).

Each stage skips any output that already exists, so a batch can be rerun after a failure. The bridge locks the
manifest, so stages may run beside another batch's tasks.

  portraits   six sixteen-expression atlases: five Okhari (Ruva, Teo, Binna, Jask, Senn) and Hadda, a Vael
  refs        reference images for the bodies, creatures, bosses and kit pieces
  models      image-to-3D for each reviewed reference
  rig         Meshy's biped rig for the Okhari body
  reskin      retextures of rigged bodies (townsfolk and the named Okhari on the Okhari body; Hadda on a Vael body)

  python tools/meshy/c10_xylos_batch.py portraits --only Ruva_Natural
"""
from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from c9_lantern_batch import PORTRAIT_HEADER, VAEL  # noqa: E402  (the atlas layout and the Vael anatomy are shared)

ROOT = Path(__file__).resolve().parents[2]
BATCH = "C10-Xylos"
GEN = ROOT / "art-src" / "Generated" / BATCH
MESHY = [sys.executable, str(ROOT / "tools/meshy/meshy.py")]

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
}
PORTRAIT_REFS = [GEN.parent / "P3/portraits/Mira_Natural/Mira_Natural.png", GEN.parent / "P3/portraits/Orrin_Natural/Orrin_Natural.png"]
# Once Ruva's sheet is approved it joins the references, so the other four Okhari share her anatomy.
OKHARI_REF = GEN / "refs" / "Ruva_Natural.png"


def run(args: list[str]) -> None:
    subprocess.run(MESHY + args, check=True, cwd=ROOT)


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


STAGES = {"portraits": portraits}


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
