#!/usr/bin/env python3
"""Chapter 9 (The House That Can Leave) intake: the pieces of tools/meshy/c16_home_batch.py, the free grades that dress
the rest of the chapter, and its three portrait sheets (Bryn's is chapter 8's).

The machinery is chapter 3's (tools/c10_intake.py); only the tables are this chapter's.

  python tools/c16_intake.py views --only DorrowBody   # look first: four sides, then four from the gameplay camera's height
  python tools/c16_intake.py prepare                   # strip, albedo, emission mask, intake rows for every finished piece
  python tools/c16_intake.py portraits                 # Dorrow, Tam, Jun, assembled for PortraitIntake.ImportNamed

The enemy rows name their enemies only once those EnemyDef assets exist; scripts/art-bind.ps1 prepares again and
re-stages them then. The Foundry Saint is two pieces: `FoundrySaint` (the press, its override rig on its back) is the
row its EnemyDef binds; `SaintArm` is one arm, which the fight places six times and lets fall as each is released.
"""
from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import c10_intake as intake  # noqa: E402

intake.configure(
    "C16-Home",
    GROUND="0.09,0.1,0.12",   # the Decks' and the Works' blue-grey plate
    # id: (size in metres, fit, emission key, folder, yaw)
    STATIC={
        "FoundrySaint": (12.0, "height", "cyan", "Enemies", 0),
        "SaintArm": (7.0, "length", None, "Environment", 0),
        "DebrisCutter": (8.0, "length", "cyan", "Enemies", 0),
        "SlagGull": (3.0, "length", "orange", "Enemies", 0),
        "OverrideRig": (1.4, "height", "cyan", "Enemies", 0),
        "CrawlTread": (18.0, "length", "lamp", "Environment", 0),
    },
    GLOW={"FoundrySaint": 1.0, "DebrisCutter": .9, "SlagGull": 1.0, "OverrideRig": 1.1, "CrawlTread": 1.2, "Interceptor": .9,
          "TowDrone": .9},
    # id: (file stem of the rigged body, height)
    BODY={"DorrowBody": ("DorrowBody", 1.3), "JunBody": ("JunBody", 1.6)},
    # id: (source intake id, folder, size, emission key, fit or None to keep the source's)
    RESKIN={
        "TamBody": ("DorrowBody", "Characters", 1.15, None, None),       # an adolescent Crawler, goggles a size too big
        "BrynBody": ("CorraBody", "Characters", 1.85, None, None),
        "OdaBody": ("CorraBody", "Characters", 1.82, None, None),
        "IloBody": ("SurvivorNatural", "Characters", 1.88, None, None),
        "WeldArm": ("SaintArm", "Enemies", 4.5, None, None),             # Taren's welding arm, under somebody else's hand
        "Interceptor": ("DebrisCutter", "Enemies", 4.0, "source", None),
        "HeatScavenger": ("AshCrawler", "Enemies", 2.4, "source", None),
        "BridleFragment": ("BridleCage", "Enemies", 4.5, "source", None),
        "TowDrone": ("ClaimDrone", "Enemies", 2.2, "source", None),
    },
    COATS={"CorraCoat": "art-src/Generated/QA-Hessa/CorraBody_albedo.png",
           "SurvivorCoat": "art-src/Generated/P44-NPCBodies/SurvivorNatural_base_color.png",
           "AshCoat": "art-src/Generated/C15-Vaskar/AshCrawler_albedo.png",
           "CageCoat": "art-src/Generated/C15-Vaskar/BridleCage_albedo.png",
           "DroneCoat": "art-src/Generated/C10-Xylos/ClaimDrone_albedo.png"},
    TEXTURE_OF={"TamBody": "DorrowBody_Meshy", "OdaBody": "CorraCoat", "IloBody": "SurvivorCoat", "WeldArm": "SaintArm",
                "Interceptor": "DebrisCutter", "HeatScavenger": "AshCoat", "BridleFragment": "CageCoat", "TowDrone": "DroneCoat"},
    TINT={"Interceptor": "#7FEFF5", "TowDrone": "#7FEFF5", "HeatScavenger": "#FF6A1E", "BridleFragment": "#FF6A1E"},
    # (hue from, hue to, hue shift, saturation scale, value gamma). A whole-range band goes first.
    GRADE={
        "TamBody": [(25, 70, 170, 1.1, 1.0), (90, 170, -110, 1.6, 1.0)],     # ochre jacket to blue; green wrap to red
        "OdaBody": [(60, 200, -110, 1.6, .7), (15, 55, 0, .25, 1.25)],       # green vest to ochre; tan trousers to grey
        "IloBody": [(10, 45, 195, 1.0, 1.6)],                                 # the orange coverall to Compact navy
        "WeldArm": [(0, 360, 0, 1, 1.2)],                                     # a little darker under the override
        "Interceptor": [(0, 360, 0, .8, 1.05)],
        "HeatScavenger": [(0, 360, 20, 1, .75)],                              # paler, sandier armour of the dawn side
        "BridleFragment": [(0, 360, 0, .8, 1.15)],
        "TowDrone": [(35, 70, 0, .1, .7)],                                    # the claim drone's yellow to Compact white
    },
    ROW={},
    PORTRAITS=["Dorrow_Natural", "Tam_Natural", "Jun_Natural"],
)

if __name__ == "__main__":
    raise SystemExit(intake.main())
