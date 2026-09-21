"""Re-export a generated FBX without its embedded copy of the texture.

Meshy embeds the albedo INSIDE the FBX. The bridge also caches that same image
beside the model, and Unity binds the sibling PNG (the importer never extracts the
embedded one), so every generated character stores its texture twice and the
second copy is never read. At 2048 square that was ~5 MB per character of pure
duplication, and the FBX files came to 255 MB against 12k-poly meshes that should
weigh about one.

Import, export, change nothing else. No scaling: an armature exported at a scale
other than 1 loses its skin weights through the FBX writer, which is the failure
that produced a file with a correct skeleton and ZERO vertex groups.

  scripts\\blender.ps1 -Script tools\\blender\\p64_strip_embedded.py -- \
      --input <fbx> --output <fbx>
"""

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy

from _common import cli_args, export_model, import_model, reset


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--input", required=True)
    p.add_argument("--output", required=True)
    p.add_argument("--scale-all", action="store_true")
    args = p.parse_args(cli_args())

    before = Path(args.input).stat().st_size
    reset()
    import_model(args.input)

    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    armatures = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
    groups = sum(len(o.vertex_groups) for o in meshes)
    bones = sum(len(o.data.bones) for o in armatures)

    if args.scale_all:
        bpy.ops.export_scene.fbx(filepath=str(Path(args.output).resolve()),use_selection=False,apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=False)
    else:
        export_model(args.output)

    after = Path(args.output).stat().st_size
    # Report the skin contract on both sides: a smaller file that lost its vertex
    # groups is not a saving, it is a broken character.
    print(f"P64_STRIP {Path(args.input).name} {before/1e6:.1f}MB -> {after/1e6:.1f}MB "
          f"meshes={len(meshes)} armatures={len(armatures)} bones={bones} groups={groups}")


if __name__ == "__main__":
    main()
