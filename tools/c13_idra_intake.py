#!/usr/bin/env python3
"""Idra Sorn's intake: her two bodies, her rescue cutter and her two portrait sheets (tools/meshy/c13_idra_batch.py).

The machinery is chapter 3's (tools/c10_intake.py); only the tables are hers.

  python tools/c13_idra_intake.py views --only IdraNatural   # look first
  python tools/c13_idra_intake.py prepare                    # strip, albedo, emission mask, intake rows
  python tools/c13_idra_intake.py portraits                  # Idra_Natural and Idra_Braced, assembled for PortraitIntake

Her rows name `character` Idra only once that CharacterDef asset exists (GenIntake rejects an unknown character), so run
`prepare` again after it does. Braced is her second ground form: it takes the `shaped` slot of the definition and the
`portraitShaped` slot of the dialogue, with `vanes` 0, because a Rauk wears a frame and shapes nothing.

Then, from PowerShell:
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.GenIntake.BuildPrefix -Marker GEN_INTAKE_OK -ExtraArgs @('-intake-prefix','Idra')
  & .\\scripts\\exec-when-green.ps1 -Method Lattice.EditorTools.PortraitIntake.ImportNamed -Marker PORTRAIT_INTAKE_OK -ExtraArgs @('-portrait-names','Idra_Natural,Idra_Braced')
"""
from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import c10_intake as intake  # noqa: E402

DEFINED = (intake.ROOT / "Lattice/Assets/_Project/Resources/Definitions/CharacterDef/Idra.asset").is_file()
if not DEFINED:
    print("C13_INTAKE_NO_CHARACTERDEF Idra (rows left unbound; prepare again once the definition exists)")
IDRA = "Idra" if DEFINED else None

intake.configure(
    "C13-Hoarfell",
    # id: (file stem of the rigged body, height). She is a head shorter than Taren and Sela (1.9) out of the frame.
    BODY={"IdraNatural": ("IdraNatural", 1.75), "IdraBraced": ("IdraBraced", 1.85)},
    # Her cutter sits beside the first two heroes' craft (3.2 m, fit by depth): the tug of the three.
    STATIC={"IdraFlight": (3.6, "depth", "lamp", "Characters", -90)},
    ROW={
        "IdraNatural": {"character": IDRA, "form": "Natural", "clips_of": "TarenNatural"},
        "IdraBraced": {"character": IDRA, "form": "Shaped", "clips_of": "TarenShaped", "vanes": 0},
        "IdraFlight": {"character": IDRA, "form": "Flight"},
    },
    PORTRAITS=["Idra_Natural", "Idra_Braced"],
)

if __name__ == "__main__":
    raise SystemExit(intake.main())
