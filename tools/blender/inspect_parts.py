"""Report welded geometric components without changing a generated asset."""
import argparse,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import bpy
from _common import cli_args,reset,import_model,write_json
p=argparse.ArgumentParser();p.add_argument('--input',required=True);p.add_argument('--report',required=True);a=p.parse_args(cli_args());reset();import_model(a.input)
rows=[]
for o in [o for o in bpy.context.scene.objects if o.type=='MESH']:
 verts=o.data.vertices;parent=list(range(len(verts)))
 def find(n):
  while parent[n]!=n:parent[n]=parent[parent[n]];n=parent[n]
  return n
 def union(i,j):parent[find(i)]=find(j)
 positions={}
 for v in verts:
  key=tuple(round(x,5) for x in v.co)
  if key in positions:union(v.index,positions[key])
  positions[key]=v.index
 for edge in o.data.edges:union(*edge.vertices)
 parts={}
 for v in verts:parts.setdefault(find(v.index),[]).append(v.index)
 for indices in sorted(parts.values(),key=len,reverse=True):
  points=[o.matrix_world@verts[i].co for i in indices]
  lo=[min(v[k] for v in points) for k in range(3)];hi=[max(v[k] for v in points) for k in range(3)]
  rows.append(dict(object=o.name,vertices=len(indices),min=lo,max=hi,first_index=indices[0]))
write_json(a.report,dict(source=a.input,parts=rows));print('PARTS_REPORT_OK count='+str(len(rows)))
