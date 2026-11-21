#!/usr/bin/env python3
"""Chapter 10 (A Route With Room To Turn Back) intake: the pieces of tools/meshy/c17_stillwater_batch.py and the free
grades that dress the rest of the finale. Nobody new speaks, so there are no portrait sheets.

The machinery is chapter 3's (tools/c10_intake.py); only the tables are this chapter's.

  python tools/c17_intake.py views --only MeretGantry   # look first: four sides, then four from the gameplay camera's height
  python tools/c17_intake.py prepare                    # strip, albedo, emission mask, intake rows for every finished piece

The boss rows are named for their EnemyDef ids (MotherOfDoors, Tollkeeper, MeretShip, MeretGantry, MeretWell) and bind
once those exist; scripts/art-bind.ps1 prepares again and re-stages them then. Mother of Doors is a head (her row) with
the Cantor's body and tail segments in her grade (`MotherBody`, `MotherTail`), chained by code as the Cantor's are. The
Tollkeeper is a hull (its row) and one outrigger (`TollkeeperOutrigger`) the fight places round it and moves.

Four free grades give bodies of their own to what borrowed another chapter's (stream M, November 14): the Clamp Tender
(`StillwaterTender`, chapter 7's brass tender) and the Clamp Gun (`ClampGun`, chapter 8's Flak Gate) in the Compact's
white and teal (LIVERY), the gun's lamps kept lit as its warning light; the captain of the Steady Hand (`PatrolBody`,
the Survivor body in Dessik's coat, his slate and olive turned white and teal and the yellow harness taken into the
white); and the young of Sorrel's voids and the passage (`YoungHead`, `YoungBody`, `YoungTail`), the Cantor's segments
small, pale like their mother and sea-green where hers are lavender.

  python tools/c17_intake.py prepare --only StillwaterTender,ClampGun,PatrolBody,YoungHead,YoungBody,YoungTail
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.GenIntake.BuildPrefix -Marker GEN_INTAKE_OK -ExtraArgs @('-intake-prefix','StillwaterTender,ClampGun,PatrolBody,Young')
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
# The Compact's two colours, as the Warden wears them: a deep teal and a cool white.
TEAL, WHITE = "#163A45", "#EEF3F4"
# The young: the Cantor's grade taken pale like their mother's (her whole-range band, a little lighter), the young
# Cantor's violet plates turned sea-green (the colour of the young's glow in the voids) and its salmon lamps gold like
# hers. Kin to her, never the Cantor's deep violet.
YOUNG = [(0, 360, 0, .5, .45), (245, 300, -80, 1.6, .4), (340, 30, 32, 1.6, .85)]

intake.configure(
    "C17-Stillwater",
    GROUND="0.035,0.045,0.07",   # deep space and the Continuance's dark decks
    # id: (size in metres, fit, emission key, folder, yaw)
    STATIC={
        "MotherOfDoors": (16.0, "length", "cyan", "Enemies", 0),
        "Tollkeeper": (90.0, "length", "cyan", "Enemies", 0),
        "TollkeeperOutrigger": (40.0, "length", "cyan", "Enemies", 0),
        "MeretShip": (9.0, "length", "cyan", "Enemies", 0),
        "StillwaterClamp": (40.0, "height", "cyan", "Environment", 0),
        "AnchorChain": (30.0, "length", None, "Environment", 0),
    },
    GLOW={"MotherOfDoors": 1.1, "Tollkeeper": .9, "TollkeeperOutrigger": .9, "MeretShip": .55,   # its teal bands catch the key too "StillwaterClamp": .9,
          "MeretGantry": 1.0, "CompactWarden": .9, "MeretWell": .9, "MotherBody": 1.1, "MotherTail": 1.1, "Continuance": .9,
          "StillwaterTender": .9, "ClampGun": 1.0, "YoungHead": 1.2, "YoungBody": 1.2, "YoungTail": 1.2},
    # id: (file stem of the rigged body, height)
    BODY={"MeretGantry": ("MeretGantry", 3.2), "CompactWarden": ("CompactWarden", 1.95)},
    BODY_KEY={"MeretGantry": "cyan", "CompactWarden": "cyan"},
    # The tender's lens in the Compact's cyan; the gun's lamps a warning red-orange that holds against white.
    TINT={"MotherBody": "#7FEFF5", "MotherTail": "#7FEFF5", "StillwaterTender": "#7FEFF5", "ClampGun": "#FF5A1E",
          "YoungHead": "#7FEFF5", "YoungBody": "#7FEFF5", "YoungTail": "#7FEFF5"},
    # id: (source intake id, folder, size, emission key, fit or None to keep the source's)
    RESKIN={
        "MeretWell": ("MeretBody", "Enemies", 1.85, "cyan", None),
        # The Cantor's kind grown old: its body and tail segments, larger, in her pale violet.
        "MotherBody": ("CantorBody", "Enemies", 9.0, "source", None),
        "MotherTail": ("CantorTail", "Enemies", 11.0, "source", None),
        # Meret's engineering vessel: the Tollkeeper's hull in her white and gold at twice the size.
        "Continuance": ("Tollkeeper", "Environment", 180.0, "source", None),
        # The anchor's four clamps: Oriel's Bridle clamp, built again by the Compact.
        "AnchorClamp": ("BridleClamp", "Environment", 6.0, None, None),
        # The Compact's clamp hand: chapter 7's tender as the Compact builds it, its lens (the source's mask) lit cyan.
        "StillwaterTender": ("ClampTender", "Enemies", 1.1, "source", None),
        # The clamps' and the cage's guns: chapter 8's Flak Gate frame in white and teal, its ring of lamps a red-orange
        # warning light (the orange key finds them on the white).
        "ClampGun": ("FlakGate", "Enemies", 10.0, "orange", None),
        # The captain of the Steady Hand: the Vael men's body (Dace's, Dessik's, Ilo's) in a Compact officer's coat.
        "PatrolBody": ("DessikBody", "Characters", 1.9, None, None),
        # The young: the Cantor's segments at a third of its size, the Cantor's glow mask lit in the young's cyan.
        "YoungHead": ("CantorHead", "Enemies", 1.6, "source", None),
        "YoungBody": ("CantorBody", "Enemies", 1.1, "source", None),
        "YoungTail": ("CantorTail", "Enemies", 1.3, "source", None),
    },
    COATS={"CantorBodyCoat": "art-src/Generated/P3-CantorBody/CantorBody_base_color.png",
           "CantorTailCoat": "art-src/Generated/P3-CantorTail/CantorTail_base_color.png",
           "CantorHeadCoat": "art-src/Generated/P3-CantorHead/CantorHead_base_color.png",
           "ClampCoat": "art-src/Generated/C14-Oriel/BridleClamp_albedo.png",
           "TenderCoat": "art-src/Generated/C14-Oriel/ClampTender_albedo.png",
           "GateCoat": "art-src/Generated/C14-Oriel/RailGate_albedo.png",
           "DessikCoat": "art-src/Generated/C12-ManyHands/DessikBody_albedo.png"},
    TEXTURE_OF={"MotherBody": "CantorBodyCoat", "MotherTail": "CantorTailCoat", "Continuance": "Tollkeeper",
                "AnchorClamp": "ClampCoat", "StillwaterTender": "TenderCoat", "ClampGun": "GateCoat", "PatrolBody": "DessikCoat",
                "YoungHead": "CantorHeadCoat", "YoungBody": "CantorBodyCoat", "YoungTail": "CantorTailCoat"},
    # (hue from, hue to, hue shift, saturation scale, value gamma). A whole-range band goes first.
    GRADE={
        # Aged: paler and pearlier than the young Cantor, its salmon lamps turned gold like the doors along her head.
        # Her head came out pale lavender, so the young Cantor's deep violet is taken most of the way to it.
        "MotherBody": [(0, 360, 0, .45, .5), (340, 30, 32, 1.6, .85)],
        "MotherTail": [(0, 360, 0, .45, .5), (340, 30, 32, 1.6, .85)],
        "Continuance": [(0, 360, 0, 1, .95), (165, 225, -150, 1.3, .8)],   # the Tollkeeper's teal bands in Meret's gold
        "AnchorClamp": [(0, 360, 0, .2, .45)],              # bronze to the Compact's white (COLORIZE adds its teal cast)
        # The brass lens turned to the Compact's cyan before the livery repaints the brass round it (KEEP holds the lens).
        "StillwaterTender": [(20, 60, 150, 1, 1)],
        # The ring's amber lamps red-orange, its yellow-green ones amber-orange and its green ones red: one warning light,
        # no magenta (a straight shift of the green took the back's lamps pink).
        "ClampGun": [(15, 55, -20, 1.3, 1), (55, 95, -45, 1.3, 1), (95, 150, -105, 1.4, 1)],
        "YoungHead": YOUNG, "YoungBody": YOUNG, "YoungTail": YOUNG,
    },
    COLORIZE={"AnchorClamp": (190, .12)},
    # The Compact's white and teal by light and dark (c10_intake.livery): the machines' brass and bronze are one hue from
    # highlight to seam, so the seams and recesses come out teal and the panels white, as the Warden's plates and suit.
    LIVERY={
        "StillwaterTender": [(0, 360, 0, TEAL, WHITE, .36, .47)],
        "ClampGun": [(0, 360, 0, TEAL, WHITE, .34, .46)],
        # Dessik's slate coat white (its darkest, the boots and gloves, teal), his olive trousers a deep teal, his yellow
        # harness taken into the white so the X across his chest and back is gone. Skin and hair are near grey and keep.
        "PatrolBody": [(185, 245, .03, "#1C4652", "#E9EEF0", .23, .3),
                       (45, 110, .03, "#123842", "#2C6E7A", .2, .45),
                       (25, 60, .3, "#C9D3D6", WHITE, .3, .9)],
    },
    # The gun's lamps are blocks; the bronze's bright bevels are lines a few pixels wide, opened away (7 px).
    KEEP={"StillwaterTender": (.45, .8), "ClampGun": (.45, .72, 7)},
    ROW={"MeretGantry": {"folder": "Enemies", "form": None, "clips": ENEMY_CLIPS},
         "CompactWarden": {"folder": "Enemies", "form": None, "clips": ENEMY_CLIPS},
         "MeretWell": {"clips": ENEMY_CLIPS},
         # He is the speaker Patrol: the row binds his CharacterDef's body, which Many Hands' captain at the seal stands in
         # (a grey capsule until now) as well as the Continuance's.
         "PatrolBody": {"character": "Patrol"}},
    PORTRAITS=[],
)

if __name__ == "__main__":
    raise SystemExit(intake.main())
