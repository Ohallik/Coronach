"""Repair four movable vanes using the model's own generated lower-vane geometry.
Selection is deliberately specific to the inspected Taren/Sela source files.
No new art geometry or texture is synthesized; UVs and body skin weights survive.
"""
import argparse,sys,math
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy,bmesh
from mathutils import Vector,Matrix
from _common import cli_args,reset,import_model,export_model,write_json
p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--output',required=True);p.add_argument('--character',choices=['Taren','Sela'],required=True);a=p.parse_args(cli_args());reset();import_model(a.input)
source=next(o for o in bpy.context.scene.objects if o.type=='MESH');mesh=source.data;verts=mesh.vertices;parent=list(range(len(verts)))
def find(n):
 while parent[n]!=n:parent[n]=parent[parent[n]];n=parent[n]
 return n
def union(i,j):parent[find(i)]=find(j)
positions={}
for v in verts:
 key=tuple(round(x,5) for x in v.co)
 if key in positions:union(v.index,positions[key])
 positions[key]=v.index
for e in mesh.edges:union(*e.vertices)
parts={}
for v in verts:parts.setdefault(find(v.index),[]).append(v.index)
parts=sorted(parts.values(),key=len,reverse=True)
assert len(verts)>4000
seed=8 if a.character=='Taren' else 5
chosen=set(parts[seed]);points=[source.matrix_world@verts[i].co for i in chosen]
assert all(p.x<-.13 and 0.98<p.z<1.32 for p in points),'Unexpected source component; inspect again'
pivot=Vector((sum(v.x for v in points)/len(points),min(v.y for v in points),max(v.z for v in points)))
source_faces={f.index for f in mesh.polygons if all(i in chosen for i in f.vertices)}
assert len(source_faces)>20,'No usable generated vane'
remove=set();known=[8,9,19,22] if a.character=='Taren' else [5,6]
known_verts=set(i for part in known for i in parts[part])
for f in mesh.polygons:
 center=source.matrix_world@f.center
 x,y,z=center
 if all(i in known_verts for i in f.vertices):remove.add(f.index)
 if a.character=='Taren' and abs(x)>.115 and y>-.075 and 1.25<z<1.83:remove.add(f.index)
 if a.character=='Sela' and x>.135 and ((y>.065 and 1.0<z<1.31) or (-.032<y<.092 and 1.56<z<1.715)):remove.add(f.index)
def retain(obj,indices):
 bm=bmesh.new();bm.from_mesh(obj.data);bm.faces.ensure_lookup_table();bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.index not in indices],context='FACES')
 bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(obj.data);bm.free()
created=[]
for tier in ('Upper','Lower'):
 for side in ('L','R'):
  obj=source.copy();obj.data=source.data.copy();bpy.context.collection.objects.link(obj);obj.name='Vane_'+tier+'_'+side
  retain(obj,source_faces);obj.parent=None;obj.modifiers.clear();obj.vertex_groups.clear();obj.animation_data_clear()
  sign=-1 if side=='L' else 1
  center_y=-.29 if a.character=='Taren' else .14
  anchor=Vector((sign*.16,center_y,1.44 if tier=='Upper' else 1.17))
  factor=1.5 if tier=='Upper' else 1.25
  for v in obj.data.vertices:
   local=source.matrix_world@v.co-pivot
   if side=='R':local.x=-local.x
   if tier=='Upper':local.z=-local.z
   v.co=local*factor
  # Reflection reverses winding; correct normals in the derived piece.
  bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
  obj.matrix_world=Matrix.Translation(anchor);created.append(obj.name)
retain(source,{f.index for f in mesh.polygons if f.index not in remove})
assert len(source.vertex_groups)>=20,'Body lost skinning'
export_model(a.output);write_json(str(Path(a.output).with_suffix('.vanes.json')),dict(source=a.input,source_vane_component=seed,source_vane_faces=len(source_faces),removed_faces=len(remove),parts=created))
print('VANES_SEPARATED_OK '+a.character+' count='+str(len(created)))
