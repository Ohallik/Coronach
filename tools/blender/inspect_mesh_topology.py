"""Describe welded static-mesh topology and normal consistency without editing it.

This is a diagnostic report, not a model acceptance gate. Boundary edges can be
intentional; visually inspect any repair. Coordinates and volumes are in the
imported model's world space. Only a disposable BMesh is welded/reoriented.
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bmesh
import bpy
from mathutils import Vector
from _common import cli_args, import_model, reset, write_json


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input', required=True)
    parser.add_argument('--report', required=True)
    args = parser.parse_args(cli_args())
    if Path(args.report).exists():
        parser.error('preserve the earlier report')
    reset()
    import_model(args.input)
    result = []
    for obj in (o for o in bpy.context.scene.objects if o.type == 'MESH'):
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        bmesh.ops.transform(mesh, matrix=obj.matrix_world, verts=list(mesh.verts))
        bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=1e-6)
        mesh.normal_update()
        remaining = set(mesh.verts)
        while remaining:
            first = min(remaining, key=lambda v: tuple(v.co))
            vertices, todo = set(), [first]
            while todo:
                vertex = todo.pop()
                if vertex in vertices:
                    continue
                vertices.add(vertex)
                todo.extend(edge.other_vert(vertex) for edge in vertex.link_edges)
            remaining -= vertices
            edges = {e for v in vertices for e in v.link_edges}
            faces = {f for v in vertices for f in v.link_faces}
            center = sum((v.co for v in vertices), Vector()) / len(vertices)
            volume = 0.0
            for face in faces:
                p = [v.co - center for v in face.verts]
                for i in range(1, len(p) - 1):
                    volume += p[0].dot(p[i].cross(p[i + 1])) / 6
            before = {f: f.normal.copy() for f in faces}
            # Diagnostic copy only: count the faces an outside-normal repair
            # would change. Never export or mutate the imported model.
            bmesh.ops.recalc_face_normals(mesh, faces=list(faces))
            mesh.normal_update()
            result.append(dict(object=obj.name, vertices=len(vertices), faces=len(faces),
                               triangles=sum(len(f.verts) - 2 for f in faces),
                               boundaryEdges=sum(len(e.link_faces) == 1 for e in edges),
                               nonmanifoldEdges=sum(len(e.link_faces) not in (1, 2) for e in edges),
                               signedVolumeBefore=volume,
                               facesReversedByOutsideRepair=sum(before[f].dot(f.normal) < -.5 for f in faces),
                               minimum=[min(v.co[i] for v in vertices) for i in range(3)],
                               maximum=[max(v.co[i] for v in vertices) for i in range(3)]))
        mesh.free()
    write_json(args.report, dict(source=args.input, scope='READ_ONLY_DIAGNOSTIC_NOT_ACCEPTANCE', components=result))
    print('MESH_TOPOLOGY_REPORT components=' + str(len(result)))


if __name__ == '__main__':
    main()
