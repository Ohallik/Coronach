"""Shared Blender CLI helpers for Lattice art scripts."""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def cli_args() -> list[str]:
    return sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []


def reset() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_model(path_text: str) -> Path:
    path = Path(path_text).expanduser().resolve()
    if not path.is_file():
        raise FileNotFoundError(path)
    ext = path.suffix.lower()
    if ext in {".glb", ".gltf"}:
        bpy.ops.import_scene.gltf(filepath=str(path))
    elif ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(path), use_anim=True)
    elif ext == ".obj":
        bpy.ops.wm.obj_import(filepath=str(path))
    else:
        raise ValueError(f"Unsupported model input: {ext}")
    return path


def export_model(path_text: str) -> Path:
    path = Path(path_text).expanduser().resolve()
    path.parent.mkdir(parents=True, exist_ok=True)
    ext = path.suffix.lower()
    if ext == ".fbx":
        bpy.ops.export_scene.fbx(
            filepath=str(path), use_selection=False, apply_unit_scale=True,
            add_leaf_bones=False, bake_anim=True, path_mode="COPY", embed_textures=False,
        )
    elif ext == ".obj":
        bpy.ops.wm.obj_export(filepath=str(path), export_materials=True)
    elif ext in {".glb", ".gltf"}:
        bpy.ops.export_scene.gltf(filepath=str(path), export_format="GLB" if ext == ".glb" else "GLTF_SEPARATE")
    else:
        raise ValueError(f"Unsupported model output: {ext}")
    return path


def mesh_objects():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def triangle_count(obj) -> int:
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        mesh.calc_loop_triangles()
        return len(mesh.loop_triangles)
    finally:
        evaluated.to_mesh_clear()


def world_bounds(objects=None) -> dict:
    objects = objects or mesh_objects()
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    if not points:
        return {"min": [0, 0, 0], "max": [0, 0, 0], "size": [0, 0, 0]}
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return {"min": list(lo), "max": list(hi), "size": list(hi - lo)}


def skeleton_bounds() -> dict | None:
    # Rigged assets obey the skeleton-measure law: renderer bounds can lie after
    # bone scaling, so report the posed head/tail envelope independently.
    points = []
    for armature in (o for o in bpy.context.scene.objects if o.type == "ARMATURE"):
        for bone in armature.pose.bones:
            points.extend((armature.matrix_world @ bone.head, armature.matrix_world @ bone.tail))
    if not points:
        return None
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return {"min": list(lo), "max": list(hi), "size": list(hi - lo), "bone_points": len(points)}


def inspect_scene(source: str | None = None) -> dict:
    meshes = mesh_objects()
    slots = []
    rows = []
    for obj in meshes:
        mats = [slot.material.name if slot.material else None for slot in obj.material_slots]
        slots.extend(m for m in mats if m)
        rows.append({
            "name": obj.name,
            "triangles": triangle_count(obj),
            "uv_layers": [layer.name for layer in obj.data.uv_layers],
            "has_uv": bool(obj.data.uv_layers),
            "material_slots": mats,
        })
    return {
        "source": source,
        "objects": rows,
        "triangles": sum(row["triangles"] for row in rows),
        "has_uv": bool(meshes) and all(row["has_uv"] for row in rows),
        "bounds": world_bounds(meshes),
        "skeleton_bounds": skeleton_bounds(),
        "armatures": [o.name for o in bpy.context.scene.objects if o.type == "ARMATURE"],
        "material_slots": sorted(set(slots)),
    }


def write_json(path_text: str, value: dict) -> Path:
    path = Path(path_text).expanduser().resolve()
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")
    return path


def require_meshes() -> list:
    values = mesh_objects()
    if not values:
        raise RuntimeError("No mesh objects were imported")
    return values

