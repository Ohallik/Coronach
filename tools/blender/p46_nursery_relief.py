"""Keep the generated nursery relief within its 6k prop budget, preserving UVs."""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy
from _common import cli_args, export_model, import_model, inspect_scene, reset, write_json

p = argparse.ArgumentParser()
p.add_argument('--input', required=True)
p.add_argument('--output')
p.add_argument('--report', required=True)
p.add_argument('--check-only', action='store_true')
a = p.parse_args(cli_args())
reset()
import_model(a.input)
before = inspect_scene(a.input)['triangles']
if not a.check_only:
    if not a.output:
        p.error('--output is required for reduction')
    # Leave a small margin for triangulation on reimport. At 6,359 triangles
    # this removes about six percent; the reference silhouette stays intact.
    for obj in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        modifier = obj.modifiers.new('Nursery prop budget', 'DECIMATE')
        modifier.ratio = min(1.0, 5950 / before)
        modifier.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
report = inspect_scene(a.input)
report['triangles_before'] = before
write_json(a.report, report)
if report['triangles'] > 6000:
    raise RuntimeError(f"Nursery prop budget exceeded: {report['triangles']} > 6000")
if not a.check_only:
    export_model(a.output)
print(f"NURSERY_PROP_BUDGET_OK before={before} after={report['triangles']}")
