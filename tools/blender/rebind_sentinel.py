"""Replace failed bone-heat weights on disconnected generated armour shells."""
import argparse,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from _common import cli_args,reset,import_model,export_model
p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--output',required=True);a=p.parse_args(cli_args());reset();import_model(a.input)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
keep={'Hips','Spine02','Spine01','Spine','neck','Head','LeftArm','LeftForeArm','LeftHand','RightArm','RightForeArm','RightHand','LeftUpLeg','LeftLeg','LeftFoot','RightUpLeg','RightLeg','RightFoot'}
segments=[(b.name,arm.matrix_world@b.head_local,arm.matrix_world@b.tail_local) for b in arm.data.bones if b.name in keep]
assert len(segments)==18
def distance(point,head,tail):
 delta=tail-head;t=max(0,min(1,(point-head).dot(delta)/max(delta.length_squared,1e-8)))
 return (point-(head+t*delta)).length
for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
 obj.vertex_groups.clear();groups={n:obj.vertex_groups.new(name=n) for n,_,_ in segments}
 for v in obj.data.vertices:
  point=obj.matrix_world@v.co
  distances=sorted((distance(point,h,t),n) for n,h,t in segments)[:3]
  weights=[1/max(.015,d)**6 for d,n in distances];total=sum(weights)
  for (d,n),w in zip(distances,weights):groups[n].add([v.index],w/total,'REPLACE')
export_model(a.output);print('SENTINEL_REBIND_OK bones=18')
