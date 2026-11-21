"""Convert a model between the formats _common can read and write (FBX, OBJ, GLB), unchanged.

Meshy's rigging endpoint takes an uploaded model only as GLB; a mesh it generated as FBX goes through here first.

  scripts/blender.ps1 -Script tools/blender/convert_model.py -TimeoutSec 300 --input Body_Meshy.fbx --output Body.glb
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from _common import cli_args, export_model, import_model, reset  # noqa: E402

p = argparse.ArgumentParser()
p.add_argument("--input", required=True)
p.add_argument("--output", required=True)
a = p.parse_args(cli_args())
reset()
import_model(a.input)
export_model(a.output)
print("CONVERT_MODEL_OK", Path(a.output).name)
