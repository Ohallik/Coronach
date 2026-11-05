#!/usr/bin/env python3
"""C9 Lantern Road art batch (D202): the kits, faces and reskins chapter 2 actually needs.

Stages run one image task at a time (parallel image tasks race on the manifest) and skip any output that
already exists, so a stage can be re-run after a failure.

  refs      two design boards (Ancelane town kit, Muster camp and canyon kit), six portrait atlases and
            two canyon ground textures.  Review every isolated reference before spending on 3D.
  isolate   cut the boards into isolated references (local, free).
  models    image-to-3D for each reviewed reference.
  reskin    original-UV retextures of existing rigged bodies (enemies and NPC bodies).
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BATCH = "C9-Lantern"
GEN = ROOT / "art-src" / "Generated" / BATCH
MESHY = [sys.executable, str(ROOT / "tools/meshy/meshy.py")]

BOARD_HEADER = (
    "OPAQUE WHITE PAPER BACKGROUND, fully opaque RGB image with solid white #FFFFFF in every empty region. No "
    "transparency, gradient, vignette or halo. CORONACH production design board in EXACTLY the same art style, line "
    "weight, cel shading and materials as the reference boards: crisp cel-shaded anime game art, flat local colors, "
    "even unshadowed studio illumination. No gloss, specular highlights, reflections, rim light, cast shadows, ground "
    "shadows, ambient occlusion, bloom, scenery, floor, particles, text, letters, labels, logos, watermark or borders. "
    "Emissive parts are opaque saturated color, not illumination. Every part is visibly attached to its body; nothing "
    "floats, nothing loose. Six separated concept cells in a 3x2 grid, ONE isolated object per cell, three-quarter view "
    "from slightly above, substantial white gutters between cells, each object standing directly on the white ground: ")
BOARDS = {
    "lantern-kit-board": (
        BOARD_HEADER +
        "(1) an ancient lantern tower, very tall and slender, carved from warm red-gold sandstone in stacked rounded "
        "drums with thin bronze bands, crowned by a large faceted crystal lantern in an open bronze cage, the crystal "
        "glowing pale amber; (2) a great carved gateway arch of the same sandstone, wide enough for a street, the arch "
        "face cut with simple concentric circle glyphs and two small crystal lamps set into its shoulders; (3) a small "
        "canyon dwelling carved into a block of sandstone, a round wooden door, one round window with warm amber light, "
        "a slanted wooden awning on carved brackets and a little hanging lamp; (4) a relay mirror: a waist-high carved "
        "sandstone pedestal holding a round crystal mirror disc in a tilting bronze ring, the crystal dark and unlit "
        "smoky blue; (5) a broken ancient column, sandstone drums with a band of circle glyphs, snapped off at two "
        "thirds height with a few fallen drum pieces resting at its foot; (6) a great relay lantern: a large faceted "
        "crystal the size of a person held in a bronze cage on a stepped round sandstone plinth, the crystal glowing "
        "pale amber with faint cyan circle patterns inside. Materials: red-gold sandstone #C98A52, darker sandstone "
        "#8E5A36, bronze #A7743A, warm wood #6B4A2E, amber crystal light #FFC46B, cyan glyph light #22F5FF, smoky blue "
        "unlit crystal #4E5D7A."),
    "muster-camp-board": (
        BOARD_HEADER +
        "(1) a military ration crate of the Muster: an angular dark gunmetal box with riveted edges, rust-red painted "
        "corner plates and a simple red stencil stripe, no lettering; (2) a scavengers' lean-to shelter: a slanted "
        "dark canvas sheet on three angular black iron poles with guy ropes staked to the ground; (3) a scavenger "
        "cutting rig: a heavy black iron tripod holding a long cutting torch arm with a glowing red-orange tip and a "
        "hose coiled to a small fuel cylinder on the ground; (4) a small gnarled canyon orchard tree with a twisted "
        "pale trunk, round silver-green leaves and a few round pale-gold fruit; (5) a carved sandstone lamp post, chest "
        "high, holding a small amber crystal lamp in a bronze cup; (6) a fallen lantern crown fragment: a broken chunk "
        "of faceted crystal still partly held in a bent bronze cage, lying on its side, the crystal dark smoky blue. "
        "Materials: gunmetal #3D434D, rust red #9C3A2A, dark canvas #4A4237, black iron #23262B, red-orange torch light "
        "#FF5A2E, red-gold sandstone #C98A52, bronze #A7743A, silver-green leaves #8FAF8A, pale gold fruit #E8C46A, amber "
        "lamp #FFC46B, smoky blue crystal #4E5D7A."),
}
BOARD_REFS = [GEN.parent / "C8-Dressing3/refs/kit3-board.png", GEN.parent / "P40-Hushwell/refs/hushwell-kit-board.png"]
BOARD_PIECES = {
    "lantern-kit-board": ["LanternTower", "AncelaneArch", "CanyonHouse", "RelayMirror", "AncelaneColumn", "GreatLantern"],
    "muster-camp-board": ["MusterCrate", "MusterLeanTo", "MusterCutterRig", "OrchardTree", "LampPost", "LanternShard"],
}

PORTRAIT_HEADER = (
    "Use case: stylized-concept. CORONACH dialogue portrait atlas. Produce exactly ONE square sheet containing FOUR "
    "equal-width columns and FOUR equal-height rows of head-and-shoulders portraits of the SAME adult {species}, "
    "sixteen separate cells total. Crisp professional cel-shaded anime line art, restrained flat colors, soft simple "
    "shading, no photorealism. Opaque pure white #FFFFFF background in every cell, no panel dividers, text, labels, "
    "numbers, borders, symbols, scenery, hands, extra figures or transparency. Each portrait is centered in its own "
    "quarter-width cell, complete crown and both shoulders within the cell, with 8-12 percent empty white margin all "
    "around. Faces all at matching scale, same character identity, same outfit and exact same head shape and markings. "
    "{anatomy} Readable pale sclera and ring irises.\n"
    "Read cells left to right, top to bottom. The sixteen expressions MUST be in this exact order:\n"
    "Row 1: Neutral (calm closed mouth); Happy (warm open smile); Sad (downturned mouth and eyelids); Angry (knitted "
    "brows, clenched teeth).\n"
    "Row 2: Shocked (very wide eyes and clearly open round mouth); Worried (pinched raised inner brows, tight mouth); "
    "Intense (sharp fixed stare, serious mouth); Synced (serene concentration, eyes bright and markings vivid).\n"
    "Row 3: Amused (knowing half-smile); Smug (one raised brow, self-satisfied crooked grin); Curious (questioning "
    "head tilt, attentive eyes); Flustered (averted eyes, embarrassed uneven smile, subtle skin blush only).\n"
    "Row 4: Tender (soft affectionate eyes and gentle closed smile); Determined (firm jaw and focused brows); Playful "
    "(one eye wink, broad mischievous grin); Special (joyful uninhibited laugh, eyes squeezed closed).\n"
    "Do not rearrange or merge cells. No sweat drops, floating hearts, thought bubbles or decorative expression marks. "
    "Full square atlas 2048x2048 or largest native square.\n\n")
VAEL = ("Vael alien", "Matte skin, lighter face and throat, elongated swept-back cranium, flat broad keratin FRONDS "
        "rather than hair, no external ears or lobes, no human hair, no furry eyebrows. Expressiveness comes from eyes, "
        "fleshy brow ridges, cheeks and mouth. Sync markings remain thin saturated flat local colors, no bloom.")
VASKAR = ("Vaskar alien (a different species from the Vael in the references)",
          "Vaskar anatomy: a tall heavy-boned people with matte skin grown in large overlapping plates across the "
          "brow, cheeks, jaw and neck like layered armor, seams between plates slightly darker; two tall membranous "
          "crest-sails rising from the back of the skull like folded fans, ribbed, their edges flushing with color; "
          "deep-set eyes with dark grey sclera and bright amber ring irises; small slit nostrils, a wide mouth with a "
          "strong plated jaw; no hair, no external ears, no human nose. Expressiveness comes from eyes, brow plates, "
          "crest-sails and mouth.")
PORTRAITS = {
    "Aumi_Natural": (VAEL, "Character: Keeper Aumi of Lanternwick, a new older adult Vael woman, NOT any character in "
        "the references; only the art style, sheet layout and species anatomy come from them. Long narrow face with "
        "high cheek ridges and a few fine age lines, sharp watchful eyes with pale gold ring irises, very long "
        "silver-grey keratin fronds falling past the shoulders and gathered with a bronze ring, fine amber sync-lines "
        "at the temples, warm dusk-grey skin. A high-collared deep rust-red keeper's robe with a bronze lantern clasp "
        "and a smoky crystal pendant. Wary, exacting and protective; her warmth is rare and real. NATURAL civilian form."),
    "Pell_Natural": (VAEL, "Character: Pell, Aumi's apprentice lamp-keeper, a new young adult Vael, barely grown, NOT any "
        "character in the references; only the art style, sheet layout and species anatomy come from them. Round open "
        "face, big bright eyes with green ring irises, short springy sand-colored keratin fronds sticking up at the "
        "back, a smudge of lamp soot on one cheek, faint cyan sync-lines, light slate skin. A patched ochre work tunic "
        "with rolled sleeves and a small brass wick-trimmer hanging on a cord. Eager, talkative and restless. NATURAL civilian form."),
    "Ishen_Natural": (VAEL, "Character: Ishen, Lanternwick's archivist, a new elderly Vael man, NOT any character in the "
        "references; only the art style, sheet layout and species anatomy come from them. Long gentle face with deep "
        "laugh lines, small round brass reading lenses perched low, pale blue ring irises, thin white keratin fronds "
        "combed neatly back, faded violet sync-lines, cool grey-blue skin. A layered sand-colored scholar's coat with "
        "many small pockets and a bundle of record slips tucked in the collar. Warm, precise and easily delighted. NATURAL civilian form."),
    "Corra_Natural": (VAEL, "Character: Corra Vey, a trader at Lanternwick's arch market, a new adult Vael woman, NOT any "
        "character in the references; only the art style, sheet layout and species anatomy come from them. Strong "
        "square face, quick dark-bronze ring irises, thick copper-red keratin fronds tied up high with a green cloth, "
        "orange sync-lines along the jaw, warm umber skin. A practical dark green market vest over a cream shirt, a "
        "leather coin belt and a stub of chalk behind one frond. Brisk, shrewd and secretly soft-hearted. NATURAL civilian form."),
    "Brekk_Natural": (VASKAR, "Character: Brekk Tamar, an Unsworn deserter from the Muster, a new adult Vaskar man, NOT "
        "any character in the references; only the art style and sheet layout come from them. Broad heavy face, "
        "weathered olive-grey plates with a long healed scar across one cheek plate, dark grey crest-sails edged with "
        "dull rust, tired amber eyes. A plain worn brown leather smith's apron over a coarse grey shirt, soot on his "
        "plates, no armor and no weapons. Quiet, ashamed and gentle. Civilian clothing."),
    "Hessa_Natural": (VASKAR, "Character: Hessa Korl, a Muster scavenger captain, a new adult Vaskar woman, NOT any "
        "character in the references; only the art style and sheet layout come from them. Narrow fierce face, dusky "
        "rust-bronze plates with fine black seams, tall crimson crest-sails, bright hard amber eyes, a thin cut-glass "
        "earring hung from a crest spine. Dark gunmetal salvage armor collar with rust-red plates and a ragged crimson "
        "captain's sash across one shoulder. Proud, sardonic and dangerous."),
}
PORTRAIT_REFS = [GEN.parent / "P3/portraits/Mira_Natural/Mira_Natural.png", GEN.parent / "P3/portraits/Orrin_Natural/Orrin_Natural.png"]

TEXTURES = {
    "canyon-floor": ("Seamless tileable game texture, exactly top-down orthographic, filling the entire square edge to "
        "edge: a dry canyon floor of packed red-gold sand and flat worn sandstone slabs with fine cracks, a few small "
        "pale pebbles, soft hand-painted cel style with flat color regions, colors red-gold #C98A52, dusty rose "
        "#B07650 and pale sand #DDB98A, no shadows, no lighting, no objects, no text, perfectly tileable on all edges."),
    "canyon-cliff": ("Seamless tileable game texture, side view, filling the entire square edge to edge: layered "
        "sandstone cliff strata in horizontal bands of red-gold #C98A52, deep rust #8E5A36 and pale cream #E3C9A0, with "
        "worn vertical cracks, soft hand-painted cel style with flat color regions, no shadows, no lighting, no objects, "
        "no text, perfectly tileable on all edges."),
}

RESKINS = {
    # name: (source clean fbx, prompt)
    "Gloomgrazer": ("art-src/Generated/P3-Ridgehound/Ridgehound_clean.fbx",
        "night canyon grazer beast that hunts in darkness: soot-black and deep violet hide, pale bone-colored back "
        "plates, small glowing amber eyes, dusty paws; light-shy predator"),
    "MusterScav": ("art-src/Generated/P3-SentinelHusk/SentinelHusk_clean.fbx",
        "Muster scavenger soldier of the Vaskar: rust-red and dark gunmetal salvage armor plates bolted over a dark "
        "leather harness, a narrow red visor slit, scraped and welded patches, small red warning lights"),
    "HessaKorl": ("art-src/Generated/P3-SentinelHusk/SentinelHusk_clean.fbx",
        "Muster scavenger captain of the Vaskar: dark bronze and black lacquered armor plates with fine rust-red trim, "
        "a long ragged crimson captain's sash, a glowing amber crystal shard bound to the chest plate"),
    "BrekkBody": ("art-src/Generated/P3-SentinelHusk/SentinelHusk_clean.fbx",
        "Vaskar smith in civilian clothes: a plain worn brown leather apron over a coarse grey shirt and trousers, dull "
        "olive-grey skin plates, soot smudges, no armor, no weapons, no glowing parts"),
    "MusterSkiff": ("art-src/Generated/P3-PatrolCutter/PatrolCutter_clean.fbx",
        "Muster raider skiff: angular dark gunmetal hull with rust-red armor plates, crude welded salvage patches, red "
        "running lights, scorched engine nozzles"),
    "AumiBody": ("art-src/Generated/P3-Neve/Neve_clean.fbx",
        "older Vael lantern-keeper woman: a high-collared deep rust-red robe to the ankles with a bronze lantern clasp, "
        "long silver-grey keratin fronds, warm dusk-grey skin, fine amber sync-lines"),
    "CorraBody": ("art-src/Generated/P3-Mira/Mira_clean.fbx",
        "Vael market trader woman: a dark green market vest over a cream shirt, brown trousers, leather coin belt, "
        "copper-red keratin fronds tied up with green cloth, warm umber skin"),
    "IshenBody": ("art-src/Generated/P3-Hal/Hal_clean.fbx",
        "elderly Vael archivist man: a layered sand-colored scholar's coat with many small pockets, cream trousers, "
        "small brass reading lenses, thin white keratin fronds, cool grey-blue skin"),
    "PellBody": ("art-src/Generated/P44-NPCBodies/SurvivorNatural_clean.fbx",
        "young Vael lamp apprentice: a patched ochre work tunic with rolled sleeves, brown trousers, soot smudges, "
        "short sand-colored keratin fronds, light slate skin, a small brass wick-trimmer on a cord"),
}


def run(args: list[str]) -> None:
    print(">>", " ".join(a if len(a) < 80 else a[:77] + "..." for a in args), flush=True)
    subprocess.run(MESHY + args, check=True, cwd=ROOT)


def refs() -> None:
    out = GEN / "refs"
    for name, prompt in BOARDS.items():
        if not (out / f"{name}.png").exists():
            ref_args = sum((["--reference", str(r)] for r in BOARD_REFS), [])
            run(["image2image", "--prompt", prompt, "--name", name, "--batch", BATCH, "--prompt-id", "C9",
                 "--aspect-ratio", "3:2", *ref_args])
    for name, (species, character) in PORTRAITS.items():
        if not (out / f"{name}.png").exists():
            prompt = PORTRAIT_HEADER.format(species=species[0], anatomy=species[1]) + character
            ref_args = sum((["--reference", str(r)] for r in PORTRAIT_REFS), [])
            run(["image2image", "--prompt", prompt, "--name", name, "--batch", BATCH, "--prompt-id", "C9",
                 "--aspect-ratio", "1:1", *ref_args])
    for name, prompt in TEXTURES.items():
        if not (out / f"{name}.png").exists():
            run(["text2image", "--prompt", prompt, "--name", name, "--batch", BATCH, "--prompt-id", "C9", "--aspect-ratio", "1:1"])
    print("C9_REFS_OK", flush=True)


def isolate() -> None:
    for board, names in BOARD_PIECES.items():
        subprocess.run([sys.executable, str(ROOT / "tools/isolate_board.py"), str(GEN / "refs" / f"{board}.png"),
                        str(GEN / "isolated"), *names], check=True, cwd=ROOT)
    print("C9_ISOLATE_OK", flush=True)


def models(only: list[str] | None) -> None:
    for names in BOARD_PIECES.values():
        for name in names:
            if only and name not in only:
                continue
            if (GEN / f"{name}.fbx").exists():
                print("C9_MODEL_SKIP", name, flush=True)
                continue
            big = name in ("LanternTower", "AncelaneArch", "GreatLantern", "CanyonHouse")
            run(["img3d", "--image", str(GEN / "isolated" / f"{name}.png"), "--name", name, "--batch", BATCH,
                 "--prompt-id", "C9", "--band", "prop", "--target-polycount", "6000" if big else "4000"])
    print("C9_MODELS_OK", flush=True)


def reskin(only: list[str] | None) -> None:
    for name, (model, prompt) in RESKINS.items():
        if only and name not in only:
            continue
        if (GEN / f"{name}.fbx").exists():
            print("C9_RESKIN_SKIP", name, flush=True)
            continue
        run(["retexture", "--model", str(ROOT / model), "--prompt", prompt + ". Preserve palette semantics and original "
             "UV islands; flat clean cel-shaded game texture, no baked lighting.", "--name", name, "--batch", BATCH, "--prompt-id", "C9"])
    print("C9_RESKIN_OK", flush=True)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("stage", choices=["refs", "isolate", "models", "reskin"])
    parser.add_argument("--only", help="comma-separated names")
    a = parser.parse_args()
    only = a.only.split(",") if a.only else None
    GEN.mkdir(parents=True, exist_ok=True)
    {"refs": refs, "isolate": isolate}.get(a.stage, lambda: None)()
    if a.stage == "models":
        models(only)
    if a.stage == "reskin":
        reskin(only)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
