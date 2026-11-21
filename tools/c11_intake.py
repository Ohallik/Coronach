#!/usr/bin/env python3
"""Chapter 4 (Wendmire) intake: the pieces of tools/meshy/c11_wendmire_batch.py, and its five portrait sheets.

The machinery is chapter 3's (tools/c10_intake.py); only the tables are this chapter's.

  python tools/c11_intake.py views --only MaruBody   # look first: four sides, then four from the gameplay camera's height on dark water
  python tools/c11_intake.py prepare                 # strip, albedo, emission mask, intake rows for every finished piece
  python tools/c11_intake.py portraits               # Maru, Ona, Dov, Quill, Pim, assembled for PortraitIntake.ImportNamed

A creature's row names its enemy only once that EnemyDef asset exists, so run `prepare` again after the chapter's
definitions are built and re-stage (it then binds the prefab, and the Drowned Sentry gets its recoil layer).

Then, from PowerShell:
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.GenIntake.BuildPrefix -Marker GEN_INTAKE_OK -ExtraArgs @('-intake-prefix','WeavingHall,PulseBeacon')
"""
from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import c10_intake as intake  # noqa: E402

ILU = 2.3      # a body's height with its antennae; the crown stands at about 2.1 m

intake.configure(
    "C11-Wendmire",
    GROUND="0.02,0.045,0.05",   # dark swamp water: what the mire's creatures must stand out from
    # id: (size in metres, fit, emission key, folder, yaw)
    STATIC={
        "Rotbloom": (9.0, "length", "livid", "Enemies", 0),
        "LureStalk": (4.2, "height", "lamp", "Enemies", 0),
        "Bogstrider": (3.6, "height", None, "Enemies", 0),
        "VeilRay": (9.0, "length", "lamp", "Enemies", 0),
        "RotPod": (1.8, "height", "livid", "Enemies", 0),
        # The Spore Veil's floating pod (November 12): its own egg-shaped sac, no longer the rooted Rot-pod's mesh. Its
        # ring of lime spots glows over violet veins, the Veil's green and violet.
        "SporePod": (2.2, "height", "lime", "Enemies", 0),
        "ReedRaft": (8.0, "length", None, "Environment", 0),
        "WeavingHall": (8.0, "height", "lamp", "Environment", 0),
        "PulseBeacon": (3.6, "height", "lamp", "Environment", 0),
        # Modelled after the chapter (November 12, the chapter 5-6 run): a pump a hero works stands a head above them.
        "AncelanePump": (2.2, "height", "lamp", "Environment", 0),
    },
    GLOW={"Rotbloom": .8, "RotPod": .8, "LureStalk": 1.2, "VeilRay": .8, "PulseBeacon": 1.4, "Lurelight": 1.4, "SporePod": 1.3},
    TINT={"PulseBeacon": "#FFC46B", "Lurelight": "#C8F03A", "SporePod": "#B6F27A"},
    # id: (file stem of the rigged body, height)
    BODY={"MaruBody": ("MaruBody", ILU), "AncelaneGuard": ("AncelaneGuard", 2.4)},
    # The Ilu's glow-spots are painted too pale for the cyan key to find, so their bodies carry no glow.
    BODY_KEY={"AncelaneGuard": "lamp"},
    # Rebuilt as drawn, the guard came back with three hair-thin faces joining its knees.
    CUT_BRIDGES={"AncelaneGuard": .38},
    # id: (source intake id, folder, size, emission key, fit or None to keep the source's)
    RESKIN={
        "OnaBody": ("MaruBody", "Characters", ILU, None, None),
        "DovBody": ("MaruBody", "Characters", ILU, None, None),
        "QuillBody": ("MaruBody", "Characters", 2.2, None, None),         # stooped with age
        "PimBody": ("MaruBody", "Characters", 1.75, None, None),          # an adolescent: about 1.6 m at the crown
        "DrownedSentry": ("AncelaneGuard", "Enemies", 2.4, "cyan", None),
        "Lurelight": ("PulseBeacon", "Enemies", 3.6, "source", None),   # the beacon's own lamp mask, lit a sickly green
    },
    # Coats that cost nothing. The four other Ilu wear Maru's own, graded to their portrait sheets' measured colours: her
    # skin is theirs within a shade, so what tells them apart from the gameplay camera is the colour of vest and
    # trousers, which a grade can set exactly and a retexture has so far set only roughly (chapter 3's all needed
    # grading too). The cap went on pieces a grade cannot make. The Lurelight is the beacon gone sickly.
    TEXTURE_OF={"OnaBody": "MaruBody_Meshy", "DovBody": "MaruBody_Meshy", "QuillBody": "MaruBody_Meshy",
                "PimBody": "MaruBody_Meshy", "Lurelight": "PulseBeacon"},
    # (hue from, hue to, hue shift, saturation scale, value gamma). Maru's coat: lilac skin 225-285, teal vest 160-205,
    # straw reed-cloth 10-60.
    GRADE={
        "OnaBody": [(160, 205, 50, .75, 1.05), (10, 60, -8, 1.2, 1.25), (225, 285, 0, 1.7, 2.1)],      # indigo; deeper violet skin
        "DovBody": [(160, 205, -172, .9, .85), (10, 60, -12, 1.6, 2.3), (225, 285, 0, .95, 1.3)],      # rust shirt, dark leather
        "QuillBody": [(160, 205, 0, .12, 1.0), (10, 60, 0, .7, .8), (225, 285, 0, .8, .85)],           # grey waistcoat, cream; washed out
        "PimBody": [(160, 205, -147, .4, .36)],                                                        # undyed reed-cloth
        "Lurelight": [(15, 75, 28, .45, 1.15)],                                                        # golden reed gone grey-olive
        "DrownedSentry": [(0, 360, 0, .75, .7)],                                                       # paler: it came back the water's own teal
    },
    # The guard is an enemy's body with the husk's five states; its coats copy that.
    ROW={"AncelaneGuard": {"folder": "Enemies", "form": None, "clips_of": "HessaKorl"}},
    PORTRAITS=["Maru_Natural", "Ona_Natural", "Dov_Natural", "Quill_Natural", "Pim_Natural"],
)

if __name__ == "__main__":
    raise SystemExit(intake.main())
