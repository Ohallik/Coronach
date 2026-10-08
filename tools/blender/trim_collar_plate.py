"""Remove the inspected loose underside artifact from the generated collar module.

Retain every other vertex and UV. Fail closed unless exactly one small central
underside component matches the inspected source geometry; never cut the body.
"""
import argparse,collections,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bmesh,bpy
from _common import cli_args,reset,import_model,export_model,write_json
p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--output',required=True);p.add_argument('--report',required=True)
a=p.parse_args(cli_args());reset();import_model(a.input)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert len(meshes)==1,'Expected the single inspected generated mesh'
o=meshes[0];vertices=o.data.vertices;parent=list(range(len(vertices)))
def find(i):
    while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
    return i
def union(i,j):parent[find(i)]=find(j)
positions={}
for v in vertices:
    key=tuple(round(x,5) for x in v.co)
    if key in positions:union(v.index,positions[key])
    positions[key]=v.index
for edge in o.data.edges:union(*edge.vertices)
parts={}
for v in vertices:parts.setdefault(find(v.index),[]).append(v.index)
matches=[]
for indices in parts.values():
    points=[o.matrix_world@vertices[i].co for i in indices]
    lo=[min(v[k] for v in points) for k in range(3)];hi=[max(v[k] for v in points) for k in range(3)]
    if 40<=len(indices)<=65 and lo[0]>.04 and hi[0]<.24 and lo[1]>-.18 and hi[1]<.18 and hi[2]<-.025 and hi[2]-lo[2]<.065:
        matches.append((indices,lo,hi))
assert len(matches)==1,f'Expected one inspected loose tab; found {len(matches)}'
indices,lo,hi=matches[0];doomed=set(indices)
def point(v):return tuple(round(x,7) for x in v.co)
retained=collections.Counter(point(v) for v in vertices if v.index not in doomed)
uv_before=collections.Counter(tuple(round(x,7) for x in o.data.uv_layers.active.data[loop.index].uv) for loop in o.data.loops if loop.vertex_index not in doomed)
before_vertices=len(vertices);before_triangles=sum(len(face.vertices)-2 for face in o.data.polygons)
bm=bmesh.new();bm.from_mesh(o.data);bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm,geom=[bm.verts[i] for i in indices],context='VERTS');bm.to_mesh(o.data);bm.free();o.data.update()
assert collections.Counter(point(v) for v in o.data.vertices)==retained,'Retained geometry changed'
assert collections.Counter(tuple(round(x,7) for x in loop.uv) for loop in o.data.uv_layers.active.data)==uv_before,'Retained UVs changed'
assert not Path(a.output).exists(),'Preserve earlier derivative'
export_model(a.output)
write_json(a.report,dict(source=a.input,output=a.output,removedVertices=len(indices),removedBounds=dict(min=lo,max=hi),beforeVertices=before_vertices,afterVertices=len(o.data.vertices),beforeTriangles=before_triangles,afterTriangles=sum(len(face.vertices)-2 for face in o.data.polygons),retainedPositionsUnchanged=True,retainedUvsUnchanged=True))
print('COLLAR_TAB_TRIMMED vertices='+str(len(indices)))
