#!/usr/bin/env python3
"""Chapter 7 (Oriel) intake: the pieces of tools/meshy/c14_oriel_batch.py, and its two portrait sheets.

The machinery is chapter 3's (tools/c10_intake.py); only the tables are this chapter's.

  python tools/c14_intake.py views --only LastWarden   # look first: four sides, then four from the gameplay camera's height
  python tools/c14_intake.py prepare                   # strip, albedo, emission mask, intake rows for every finished piece
  python tools/c14_intake.py portraits                 # Lume and the Warden's mask, assembled for PortraitIntake.ImportNamed

Enemy rows (LastWarden, Pruner, GlassSentinel, Glassback, ClampTender, RailWarden, Moonshard) name their enemies only
once those EnemyDef assets exist; scripts/art-bind.ps1 prepares again and re-stages them then.

Then, from PowerShell:
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.GenIntake.BuildPrefix -Marker GEN_INTAKE_OK -ExtraArgs @('-intake-prefix','LastWarden,Gardener')
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.PortraitIntake.ImportNamed -Marker PORTRAIT_INTAKE_OK -ExtraArgs @('-portrait-names','Lume_Natural,Warden_Natural')
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
    "C14-Oriel",
    GROUND="0.26,0.24,0.13",   # the Skyfields' pale gold stone under a green-gold light
    # id: (size in metres, fit, emission key, folder, yaw)
    STATIC={
        "Glassback": (3.2, "length", None, "Enemies", 0),
        "ClampTender": (1.1, "height", "lamp", "Enemies", 0),          # half a person tall, fragile
        # A flight enemy; its lens is lit. The rail runs through it along the mesh's X, as a cutter's nose does: the yaw
        # turns its run to +Z.
        "RailWarden": (2.6, "length", "white", "Enemies", -90),
        "BridleClamp": (4.2, "height", None, "Environment", 0),        # the engine hall's four, and the nave's first
        "RailGate": (12.0, "height", None, "Environment", 0),          # the lights are the machine's state: code drives them
        "GlassTree": (6.0, "height", None, "Environment", 0),
    },
    GLOW={"ClampTender": .9, "RailWarden": .9, "LastWarden": 1.2, "Gardener": .8, "Pruner": 1.0},
    # id: (file stem of the rigged body, height)
    BODY={"LastWarden": ("LastWarden", 3.6), "Gardener": ("Gardener", 2.2)},
    BODY_KEY={"LastWarden": "white", "Gardener": "white"},          # the Warden's glass slot; the gardener's lamp face
    TINT={"LastWarden": "#F2FBFF", "Gardener": "#FFF1CF", "Pruner": "#FF4A36"},
    # id: (source intake id, folder, size, emission key, fit or None to keep the source's)
    RESKIN={
        "LumeBody": ("PellBody", "Characters", 1.72, None, None),
        # A gardener set to prune: the same automaton, its bronze darker and its lamp lit red. Same family on purpose.
        "Pruner": ("Gardener", "Enemies", 2.2, "source", None),
        # The guard cast in glass: its own coat graded from sandstone to pale cyan glass.
        "GlassSentinel": ("AncelaneGuard", "Enemies", 2.4, "lamp", None),
        # The Glass Choir's ribs: Lastlight's rib arch, gone to glass. Debris calved from a moon: a ridge rock, pale.
        "GlassRib": ("RibArch", "Environment", 9.0, None, None),
        "Moonshard": ("RidgeRock2", "Enemies", 2.6, None, "length"),
    },
    COATS={"GuardCoat": "art-src/Generated/C11-Wendmire/AncelaneGuard_albedo.png",
           "RibCoat": "art-src/Generated/C13-Hoarfell/RibArch_albedo.png",
           "RockCoat": "art-src/Generated/P3-RidgeRock2/RidgeRock2_base_color.png"},
    TEXTURE_OF={"Pruner": "Gardener_Meshy", "GlassSentinel": "GuardCoat", "GlassRib": "RibCoat", "Moonshard": "RockCoat"},
    # (hue from, hue to, hue shift, saturation scale, value gamma). A whole-range band goes first.
    GRADE={
        "Pruner": [(0, 360, -12, 1.7, 2.3)],                                  # dark rust-bronze, so the red lamp reads
        "GlassSentinel": [(0, 360, 0, 1, .7), (10, 60, 165, .45, .55)],        # sandstone and bronze to pale cyan glass
        "GlassRib": [(0, 360, 0, 1, .85)],                                    # lifted, then made glass-blue (COLORIZE)
        "LumeBody": [(0, 360, 0, 1, .72)],                                    # her soot-grey smock came back near black
        "Moonshard": [(0, 360, 0, .35, .65)],                                  # pale grey moon rock
    },
    COLORIZE={"GlassRib": (192, .3)},   # the bone is nearly grey, which no hue band can move
    ROW={"LastWarden": {"folder": "Enemies", "form": None, "clips": ENEMY_CLIPS},
         "Pruner": {"clips": ENEMY_CLIPS}, "GlassSentinel": {}},
    PORTRAITS=["Lume_Natural", "Warden_Natural"],
)

if __name__ == "__main__":
    raise SystemExit(intake.main())
