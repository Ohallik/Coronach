"""Cut away what a T-posed figure holds past one wrist, so Meshy's rigger can find the hand.

Meshy refused to rig the Compact Warden ("pose estimation failed") with the riot shield its reference drew as a box
gripped at the left hand. This deletes every face beyond a share of that arm's length, measured from the body's centre
line (the head's), on the chosen side: the box and the hand inside it go, the arm stays to the wrist. Everything else is
unchanged; the output is what gets uploaded for rigging (GLB).

  scripts/blender.ps1 -Script tools/blender/trim_hand_prop.py -TimeoutSec 300 --input Body_Meshy.fbx --output Body_trim.glb --side +x --keep .82
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bmesh  # noqa: E402
import bpy  # noqa: E402

from _common import cli_args, export_model, import_model, mesh_objects, reset  # noqa: E402

p = argparse.ArgumentParser()
p.add_argument("--input", required=True)
p.add_argument("--output", required=True)
p.add_argument("--side", choices=("+x", "-x"), required=True, help="which side holds the prop, in the model's own X")
p.add_argument("--keep", type=float, default=.82, help="share of the OTHER arm's reach kept on this side")
a = p.parse_args(cli_args())
reset()
import_model(a.input)
meshes = mesh_objects()
points = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
top = max(v.z for v in points)
height = top - min(v.z for v in points)
head = [v.x for v in points if v.z > top - height * .08]
centre = sum(head) / len(head)
reach = centre - min(v.x for v in points) if a.side == "+x" else max(v.x for v in points) - centre
limit = centre + reach * a.keep if a.side == "+x" else centre - reach * a.keep
removed = 0
for obj in meshes:
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    doomed = [f for f in mesh.faces
              if ((obj.matrix_world @ f.calc_center_median()).x > limit) == (a.side == "+x")]
    removed += len(doomed)
    bmesh.ops.delete(mesh, geom=doomed, context="FACES")
    mesh.to_mesh(obj.data)
    mesh.free()
    obj.data.update()
export_model(a.output)
print(f"TRIM_HAND_PROP_OK removed={removed} centre={centre:.3f} limit={limit:.3f}")
