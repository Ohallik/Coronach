# Vael species and hero approval packet

Status: **NATHAN_SPECIES_APPROVED**, 2026-09-20. Nathan approved the race and continuing production, with more distinctive main-character faces required in the next design pass. The boards below establish species, palettes and silhouettes; hero facial identities will be refined in production references and portraits. Exact response: `species-approval.json`.

## Species anatomy and forms

![Vael male Natural, female Natural, male Shaped, female Flight](../../art-src/Generated/P1/refs/species-sheet-v2.png)

![Vael face and plume details](../../art-src/Generated/P1/refs/species-faces-v2.png)

Matte slate skin, overlapping keratin plume fronds, readable ring irises and branching sync-lines. No hair, tail or external ears. The Flight pose is presented upright for design review; runtime flight is horizontal. Natural forms keep collar, wrists and spine docking points. All species-sheet sync-lines use `#22F5FF`.

## Taren Vosk

![Taren Natural, Shaped and face](../../art-src/Generated/P1/refs/taren-board.png)

Broad shoulders, dark short plumes with copper tips, long wrist edge. Slate skin `#677887`, lighter face `#93A2AD`, dark plumes `#293847`, copper tips/accent `#C98254`, amber-gold sync `#FFB62E`, charcoal underlayer `#283541`, dusty wraps `#9C8C76`.

## Sela Irun

![Sela Natural, Shaped and face](../../art-src/Generated/P1/refs/sela-board-v2.png)

Taller pale plume mane, fine branching cyan lines, larger left emitter and shorter edge. Slate-blue skin `#718A9B`, lighter face `#A2B7C4`, pale plumes `#D7CFF0`, lilac roots `#9C8DC1`, cyan sync `#22F5FF`, accent `#AE9CCF`, underlayer `#334756`.

## QA and downstream intake

Opened and visually reviewed all four final images. The first species sheet was rejected for its dark background and glow spill; a targeted generation edit produced the white sheet. The first face sheet had external ears; they were removed with the image tool. Sela's emitter and plume treatment were corrected with the image tool so her combat silhouette differs from Taren's.

These are design boards for approval, not multi-subject Meshy inputs. After approval, generate isolated single-form, front-view A-pose references with separated digits and limb gaps. Shaped references must show four separately attached folded vane roots, rib-sheathed plumes, and distinct emitter/edge attachments; Unity intake must inspect the actual topology. Palette hex values are authoring targets, not a claim that every antialiased pixel equals the target.

The P1 approval explicitly required by `docs/SLICE_PROMPT.md` is received. Continue downstream boards and production without a second approval gate.
