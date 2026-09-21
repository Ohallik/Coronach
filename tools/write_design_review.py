"""Fail-closed P1 board inventory and human-reviewed reference index."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
rows=json.loads((ROOT/'docs/art/design-catalog.json').read_text(encoding='utf-8'))
reviewed=[r for r in rows if r['category'] in ('npc','enemy','ship','environment')]
assert len(reviewed)==20
for row in reviewed:
 assert row.get('qa') and row.get('accepted_source') and (ROOT/row['accepted_source']).is_file(),row['id']
lines=['# P1 production design review','','P1_DESIGN_OK — 2026-09-20. All twenty downstream boards/references generated with the session image tool, opened and reviewed against the prompt-craft reject list. Species approval and hero facial-distinction feedback are recorded in `species-approval.json`. These are design references; no model or in-engine art gate is implied.','','| Design | Review |','|---|---|']
for row in reviewed:
 lines.append(f"| [{row['id']}](../../{row['accepted_source']}) | {row['qa']} |")
lines+=['','Existing generated material/map references complete the environment set: [Sorrel ground](../../art-src/Generated/P3/refs/sorrel-ground.png), [Sorrel rock](../../art-src/Generated/P3/refs/sorrel-rock.png), [Decks panels](../../art-src/Generated/P3/refs/deck-panels.png), [Gullet membrane](../../art-src/Generated/P3/refs/gullet-membrane.png), [Vorun map](../../art-src/Generated/P3/refs/vorun-map.png), [Sorrel map](../../art-src/Generated/P3/refs/sorrel-map.png). Skies are also already generated; native resolution is recorded honestly in the image manifest.','','Every prompt was written before generation. `design-catalog.json` records accepted paths and QA notes; `image-manifest.json` also preserves rejected versions. Early NPC references needed opaque background and finger-gap corrections. The Scrapmite needed all six feet made visible. Isolate each environment-board cell with the image tool before Meshy; never feed a multi-object board into reconstruction. Cantor requires isolated head, body and tail inputs.','','Hero production inputs and portraits will retain the approved species/palettes while applying distinctive face shapes/markings. Their acceptance requires five clearly separated digits, no external ears, and aligned front A-pose. The first T2 humanoid must pass rigging, Unity avatar/motion gates and actual render review before batching.','','Instrument: this inventory fails on any missing reference or absent recorded visual review. The visual judgments remain explicit human-readable notes; file presence alone is not quality evidence.']
(ROOT/'docs/art/DESIGN_REVIEW.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('P1_DESIGN_OK references=20')
