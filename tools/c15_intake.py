#!/usr/bin/env python3
"""Chapter 8 (Harrowgate) intake: the pieces of tools/meshy/c15_vaskar_batch.py, the free grades that dress the rest of
the chapter, and its five portrait sheets.

The machinery is chapter 3's (tools/c10_intake.py); only the tables are this chapter's.

  python tools/c15_intake.py views --only SlagColossus   # look first: four sides, then four from the gameplay camera's height
  python tools/c15_intake.py prepare                     # strip, albedo, emission mask, intake rows for every finished piece
  python tools/c15_intake.py portraits                   # Drask, Tovra, Odrun, Pip, Bryn, assembled for PortraitIntake.ImportNamed

The enemy rows name their enemies only once those EnemyDef assets exist; scripts/art-bind.ps1 prepares again and
re-stages them then.
"""
from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import c10_intake as intake  # noqa: E402

# A biped enemy with a Down clip whose mesh LifecycleCalibration.HuskRig doesn't name must carry a Revive too
# (tools/c13_hoarfell_intake.py says why).
ENEMY_CLIPS = [*intake.CLIPS,
               {"state": "Down", "path": "Assets/_Project/Art/Animation/HeroDeath.fbx", "name": "Down"},
               {"state": "Stagger", "path": "Assets/_Project/Art/Animation/HeroLocomotion.fbx", "name": "Stagger"},
               {"state": "Revive", "path": "Assets/_Project/Art/Animation/HeroRevive.fbx", "name": "Revive"}]

intake.configure(
    "C15-Vaskar",
    GROUND="0.05,0.045,0.045",   # dark crusted basalt: the Flows, the Foundry floor and Harrowgate's terraces
    # id: (size in metres, fit, emission key, folder, yaw)
    STATIC={
        "SlagColossus": (7.0, "height", "orange", "Enemies", 0),
        "MusterLancer": (4.2, "length", "orange", "Enemies", -90),
        "MusterMortar": (2.0, "height", "orange", "Enemies", 0),
        "AshCrawler": (2.6, "length", "orange", "Enemies", 0),
        "PourDrone": (2.4, "height", "orange", "Enemies", 0),
        "BridleCage": (8.0, "height", "orange", "Environment", 0),
        "RouteLock": (14.0, "length", "cyan", "Environment", 0),
    },
    GLOW={"SlagColossus": 1.3, "MusterLancer": .9, "MusterMortar": 1.0, "AshCrawler": .9, "PourDrone": 1.2, "BridleCage": .8,
          "RouteLock": .9, "ForgeHound": 1.0, "DraskSkiff": .9, "FlakGate": .8},
    # id: (file stem of the rigged body, height)
    BODY={"DraskVaal": ("DraskVaal", 2.5)},
    # id: (source intake id, folder, size, emission key, fit or None to keep the source's)
    RESKIN={
        "TovraBody": ("BrekkBody", "Characters", 2.05, None, None),
        "DraskBody": ("BrekkBody", "Characters", 2.3, None, None),
        "OdrunBody": ("BrekkBody", "Characters", 1.8, None, None),      # short for her people and broad
        "PipBody": ("HessaKorl", "Characters", 1.5, None, None),        # a cadet of twelve or thirteen
        "VessaKorl": ("VessaBody", "Enemies", 2.6, None, None),         # chapter 6's body, as the Foundry's elite
        # His guard wing wear the same frame in the Muster's plain gunmetal and rust.
        "MusterPikeguard": ("DraskVaal", "Enemies", 2.3, None, None),
        # The Warmaster's skiff is a Lancer in his colours, half again its size.
        "DraskSkiff": ("MusterLancer", "Enemies", 6.0, "source", None),
        "ForgeHound": ("BridleHound", "Enemies", 2.4, "source", None),   # the Muster's copy of the Bridle-hound
        "SensorBuoy": ("WarpBeacon", "Enemies", 3.0, "source", None),    # a beacon that floats, in raider gunmetal
        "FlakGate": ("RailGate", "Enemies", 10.0, None, None),
        "HarrowgateHouse": ("CanyonHouse", "Environment", 6.0, None, None),
    },
    COATS={"BrekkCoat": "art-src/Generated/QA-Hessa/BrekkBody_albedo.png",
           "HessaCoat": "art-src/Generated/QA-Hessa/HessaBody_albedo.png",
           "VessaCoat": "art-src/Generated/C13-Hoarfell/VessaBody_albedo.png",
           "HoundCoat": "art-src/Generated/C13-Hoarfell/BridleHound_albedo.png",
           "BeaconCoat": "art-src/Generated/P3-WarpBeacon/WarpBeacon_base_color.png",
           "GateCoat": "art-src/Generated/C14-Oriel/RailGate_albedo.png",
           "CanyonCoat": "art-src/Generated/C9-Lantern/CanyonHouse_albedo.png"},
    # Tovra's and Drask's retextures (rejected/) painted bare human faces on Brekk's plated head: both are now grades
    # of Brekk's own coat, which keeps the plates.
    TEXTURE_OF={"TovraBody": "BrekkCoat", "DraskBody": "BrekkCoat", "OdrunBody": "BrekkCoat", "PipBody": "HessaCoat", "VessaKorl": "VessaCoat", "MusterPikeguard": "DraskVaal_Meshy",
                "DraskSkiff": "MusterLancer", "ForgeHound": "HoundCoat", "SensorBuoy": "BeaconCoat", "FlakGate": "GateCoat",
                "HarrowgateHouse": "CanyonCoat"},
    TINT={"ForgeHound": "#FF6A1E", "DraskSkiff": "#FF6A1E"},
    # (hue from, hue to, hue shift, saturation scale, value gamma). A whole-range band goes first.
    GRADE={
        "TovraBody": [(0, 360, 0, .75, 1.6)],                                   # soot-dark plates, a blackened leather apron
        "DraskBody": [(0, 360, 0, 1, 1.05), (10, 50, -32, 1.6, 1.15)],          # the apron dyed a Warmaster's crimson tabard
        "OdrunBody": [(0, 360, 0, .4, .75)],                                    # plates gone pale and chalky; a floured apron
        "PipBody": [(0, 360, 0, .25, .7)],                                      # pale soft plates, grey cadet cloth
        "MusterPikeguard": [(0, 360, 0, .5, 1.25), (20, 60, -25, 2.0, 1.0)],    # plain gunmetal, the bronze trim gone rust
        "DraskSkiff": [(0, 360, 0, 1, .8), (330, 40, 28, 1.6, .7)],             # rust-red panels to the Warmaster's bronze
        "ForgeHound": [(0, 360, 0, .7, 1.7)],                                    # crude dark iron, its amber eye lit orange
        "SensorBuoy": [(0, 360, 0, .5, 1.6)],                                    # the survey beacon in raider gunmetal
        "FlakGate": [(0, 360, 0, .4, 1.6), (60, 160, -100, 1, 1)],               # bronze to gunmetal; its green lamps red
        "HarrowgateHouse": [(0, 360, 0, .18, 1.9)],                              # adobe to dark basalt
    },
    ROW={"DraskVaal": {"folder": "Enemies", "form": None, "clips": ENEMY_CLIPS},
         "MusterPikeguard": {"clips": ENEMY_CLIPS}, "VessaKorl": {"clips": ENEMY_CLIPS},
         "PipBody": {"clips": intake.CLIPS}},
    PORTRAITS=["Drask_Natural", "Tovra_Natural", "Odrun_Natural", "Pip_Natural", "Bryn_Natural"],
)

if __name__ == "__main__":
    raise SystemExit(intake.main())
