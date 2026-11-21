#!/usr/bin/env python3
"""Chapter 5 (Many Hands) intake: the pieces of tools/meshy/c12_many_hands_batch.py, and its two portrait sheets.

The machinery is chapter 3's (tools/c10_intake.py); only the tables are this chapter's.

  python tools/c12_intake.py views --only MeretBody   # look first: four sides, then four from the gameplay camera's height on a lamplit deck
  python tools/c12_intake.py prepare                  # strip, albedo, emission mask, intake rows for every finished piece
  python tools/c12_intake.py portraits                # Meret and Dessik, assembled for PortraitIntake.ImportNamed

The two boss rows (Hulljack, DessikVarn) name their enemies only once those EnemyDef assets exist, so run `prepare`
again after the chapter's definitions are built and re-stage (scripts/art-bind.ps1 does both). The bodies are NPC
bodies, used by prefab key as chapter 4's are, and bind nothing.

Then, from PowerShell:
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.GenIntake.BuildPrefix -Marker GEN_INTAKE_OK -ExtraArgs @('-intake-prefix','MeretBody,DessikBody,Hulljack,DessikVarn,CompactCutter')
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.PortraitIntake.ImportNamed -Marker PORTRAIT_INTAKE_OK -ExtraArgs @('-portrait-names','Meret_Natural,Dessik_Natural')
"""
from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import c10_intake as intake  # noqa: E402

intake.configure(
    "C12-ManyHands",
    GROUND="0.13,0.075,0.035",   # the warm lamplit deck plates of Many Hands
    # id: (size in metres, fit, emission key, folder, yaw)
    STATIC={
        # The boss: four cyan magnet rings are the break targets, lit so they read from the flight camera above a hull.
        "Hulljack": (4.5, "height", "cyan", "Enemies", 0),
        # A ship like Idra's cutter: its nose lies along the mesh's X (turned to +Z by the yaw), fit nose to tail.
        "CompactCutter": (4.0, "depth", "cyan", "Ships", -90),
    },
    GLOW={"Hulljack": 1.1, "CompactCutter": .8, "DessikVarn": .8},
    # id: (file stem of the rigged body, height)
    BODY={"MeretBody": ("MeretBody", 1.85)},
    # id: (source intake id, folder, size, emission key, fit or None to keep the source's)
    RESKIN={
        # The Survivor's miner (P44) in Dessik's coat: the tallest Vael man's height, to carry his weight.
        "DessikBody": ("SurvivorNatural", "Characters", 1.95, None, None),
        # Part two: the same suit after its four feet were cut, scorched and dimmer, its magnet rings still lit.
        "DessikVarn": ("Hulljack", "Enemies", 4.5, "source", None),
    },
    TEXTURE_OF={"DessikVarn": "Hulljack"},
    TINT={"DessikVarn": "#7FEFF5"},   # the source's mask is the cyan magnet rings
    # (hue from, hue to, hue shift, saturation scale, value gamma).
    GRADE={
        "DessikBody": [(0, 360, 0, .8, .75)],   # the retexture came back near black-olive; his shirt is mid-grey
        "DessikVarn": [(0, 360, 0, .8, 1.35), (30, 60, -6, .85, 1.15)],   # scorched: darker overall, the yellow dulled toward ochre
    },
    PORTRAITS=["Meret_Natural", "Dessik_Natural"],
    WHITE_CLOTHES=("Meret_Natural",),   # her white field coat reaches the bottom of every cell
)

if __name__ == "__main__":
    raise SystemExit(intake.main())
