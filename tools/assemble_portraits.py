"""Sequential, resumable archive-to-canonical portrait assembly."""
import json,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
rows=json.loads((ROOT/'docs/art/portrait-catalog.json').read_text(encoding='utf-8'))
for row in rows:
 name=row['name'];target=ROOT/'art-src/Generated/P3/portraits'/name/(name+'.png')
 if target.is_file():print('PORTRAIT_ALREADY_ASSEMBLED '+name,flush=True);continue
 assert row.get('qa') and sorted(row['enum_to_source'])==list(range(16))
 subprocess.run([sys.executable,str(ROOT/'tools/portrait_pipeline.py'),'--sheet',row['source'],'--name',name,'--order',','.join(map(str,row['enum_to_source']))],cwd=ROOT,check=True,timeout=900)
print('ALL_PORTRAITS_ASSEMBLED count='+str(len(rows)))
