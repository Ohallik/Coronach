"""Apply axes chosen from opened front/side Unity renders, preserving generated art."""
import json
from pathlib import Path
path=Path(__file__).resolve().parents[1]/'docs/art/intake.json';rows=json.loads(path.read_text(encoding='utf-8'))
for row in rows:
 if row['id']=='CantorHead':row.update(pitch=90,yaw=0,roll=0,size=3.8,fit='depth')
 if row['id']=='CantorBody':row.update(pitch=0,yaw=90,roll=0,size=2.6,fit='depth')
 if row['id']=='CantorTail':row.update(pitch=-90,yaw=0,roll=42,size=3,fit='depth')
path.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8');print('CANTOR_AXES_READY')
