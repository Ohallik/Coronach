#!/usr/bin/env python3
"""Chapter 6 (Hoarfell) intake: the pieces of tools/meshy/c13_hoarfell_batch.py, and its four portrait sheets. Idra's
own pieces share the folder and are tools/c13_idra_intake.py's.

The machinery is chapter 3's (tools/c10_intake.py); only the tables are this chapter's.

  python tools/c13_hoarfell_intake.py views --only TernBody   # look first: four sides, then four from the gameplay camera's height on snow
  python tools/c13_hoarfell_intake.py prepare                 # strip, albedo, emission mask, intake rows for every finished piece
  python tools/c13_hoarfell_intake.py portraits               # Tern, Hask, Iru and Vessa, assembled for PortraitIntake.ImportNamed

The enemy rows (HostMarshal, ThawedGuard, MusterExcavator, RimeStalker, BridleHound) name their enemies only once those
EnemyDef assets exist, so run `prepare` again after the chapter's definitions are built and re-stage
(scripts/art-bind.ps1 does both). The Kept and Vessa are NPC bodies, used by prefab key as chapter 4's are.

Then, from PowerShell:
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.GenIntake.BuildPrefix -Marker GEN_INTAKE_OK -ExtraArgs @('-intake-prefix','TernBody,HaskBody')
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.PortraitIntake.ImportNamed -Marker PORTRAIT_INTAKE_OK -ExtraArgs @('-portrait-names','Tern_Natural,Hask_Natural,Iru_Natural,Vessa_Natural')
"""
from __future__ import annotations

import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import c10_intake as intake  # noqa: E402

GEN = intake.ROOT / "art-src/Generated/C13-Hoarfell"
# Vessa wears Hessa Korl's coat graded to slag and gunmetal. The grade reads its coat from this folder, so the sister's
# albedo (the one her prefab wears) is copied in beside it once.
HESSA_COAT = GEN / "HessaCoat_base_color.png"
if not HESSA_COAT.is_file():
    shutil.copyfile(intake.ROOT / "art-src/Generated/QA-Hessa/HessaBody_albedo.png", HESSA_COAT)

intake.configure(
    "C13-Hoarfell",
    GROUND="0.5,0.6,0.74",   # blue-white snow under a pale sky: what everything on the Reach must stand out from
    # id: (size in metres, fit, emission key, folder, yaw)
    STATIC={
        "RimeStalker": (4.0, "length", "cyan", "Enemies", 0),      # low and long; its eyes glow
        "BridleHound": (2.2, "length", "lamp", "Enemies", 0),      # the guard's amber eye
        "RimeBeast": (2.8, "length", None, "Environment", 0),      # herd animals are scenery and the strays' quest points
        "RibArch": (9.0, "height", None, "Environment", 0),
        "KeptLonghouse": (10.0, "length", "lamp", "Environment", 0),
        "ThawRig": (3.0, "height", "orange", "Environment", 0),
        # The Aurora Shear's support craft (November 12): its thaw core and racks of charges glow. Its cab lies along the
        # mesh's X, as the cutters' noses do, so the yaw turns it to +Z; 5 m from cab to stern.
        "MusterTender": (5.0, "depth", "orange", "Enemies", -90),
    },
    GLOW={"MusterTender": 1.2, "RimeStalker": .8, "BridleHound": .9, "KeptLonghouse": 1.2, "ThawRig": 1.3, "MusterExcavator": 1.1, "ThawedGuard": .9},
    # id: (file stem of the rigged body, height)
    BODY={"TernBody": ("TernBody", 1.45), "HostMarshal": ("HostMarshal", 4.8)},
    BODY_KEY={"HostMarshal": "lamp"},
    # id: (source intake id, folder, size, emission key, fit or None to keep the source's)
    RESKIN={
        "HaskBody": ("TernBody", "Characters", 1.55, None, None),       # broad even for his people
        "IruBody": ("TernBody", "Characters", 1.38, None, None),        # smaller and lighter-built
        "KeptHerder": ("TernBody", "Characters", 1.48, None, None),     # townsfolk: Tern's own coat graded
        "VessaBody": ("HessaKorl", "Characters", 2.6, None, None),
        "ThawedGuard": ("AncelaneGuard", "Enemies", 2.4, "lamp", None),
        "MusterExcavator": ("MusterSergeant", "Enemies", 2.75, "orange", None),
    },
    # Coats that cost nothing: the herder is Tern's coat in other colours, Vessa her sister's.
    TEXTURE_OF={"KeptHerder": "TernBody_Meshy", "VessaBody": "HessaCoat"},
    # (hue from, hue to, hue shift, saturation scale, value gamma).
    GRADE={
        # His retexture came back near black all over: lifted toward his sheet's grey-brown fur over a dark coat.
        "HaskBody": [(0, 360, 0, 1.1, .78)],
        # A herder for the townsfolk: Tern's fleece and hides gone tan and rust, so nobody takes a herder for her.
        # A whole-range band reaches greys too (a hue band skips anything under 0.2 saturation): the cream fleece and
        # silver fur take on tan, then the already-brown hides are held back from going orange.
        "KeptHerder": [(0, 360, -10, 2.8, 3.2), (15, 60, 0, .55, 1.6)],
        # The retexture turned the guard's sandstone to a dark brick red; back toward its own sandstone, so the make reads.
        "ThawedGuard": [(345, 30, 14, .85, .8)],
        # Soot-black came back almost pure black: lifted enough that its plates read on the dig's blue ice.
        "MusterExcavator": [(0, 360, 0, 1.1, .82)],
        # Hessa's rust-orange skin and crimson to Vessa's slag grey and gunmetal (her sheet's colours).
        # The skin plates were bright orange, so the band also comes down to her mid slag grey, a cold blue-grey. A
        # whole-range band goes first: its value replaces any earlier band's (c10_intake.grade blends value per band).
        "VessaBody": [(0, 360, 0, .8, 1.05), (330, 60, 195, .2, 1.6)],
    },
    # GenIntake calibrates the lifecycle of every biped with a Down clip: the husk's way (all five states) for meshes
    # LifecycleCalibration.HuskRig names, and otherwise Down and Revive, which then must both be there. The Marshal's
    # mesh is its own, so it carries the heroes' Revive beside the husk's five until HuskRig names `/HostMarshal_clean`
    # (an engineer's one-line change; the guard's coats are named there already). Vessa is an NPC body and takes the
    # plain three clips: Hessa's Down and Stagger without a Revive failed her intake.
    ROW={"HostMarshal": {"folder": "Enemies", "form": None,
                         "clips": [*intake.CLIPS,
                                   {"state": "Down", "path": "Assets/_Project/Art/Animation/HeroDeath.fbx", "name": "Down"},
                                   {"state": "Stagger", "path": "Assets/_Project/Art/Animation/HeroLocomotion.fbx", "name": "Stagger"},
                                   {"state": "Revive", "path": "Assets/_Project/Art/Animation/HeroRevive.fbx", "name": "Revive"}]},
         "VessaBody": {"clips": intake.CLIPS}},
    PORTRAITS=["Tern_Natural", "Hask_Natural", "Iru_Natural", "Vessa_Natural"],
)

if __name__ == "__main__":
    raise SystemExit(intake.main())
