"""Author individual Meshy inputs from the approved world/serpent boards."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
STYLE='''Use case: stylized-concept. Single LATTICE production reconstruction reference. OPAQUE PURE WHITE #FFFFFF background in every empty region. Exactly ONE complete isolated object, ONE three-quarter orthographic view, centered with 8-12 percent margin, no turnarounds, insets, additional objects, text, symbols, labels, floor, scenery, cast shadows, halo, gradients or transparency. Crisp cel-shaded anime game art, flat local colors with minimal facet shading. Matte surfaces: no specular highlights, reflection, rim light or bloom. Thick readable connected geometry, no floating or detached ornaments. All protrusions have clear roots in the main body. This will be reconstructed as a textured 3D game model. Preserve the referenced object's design, palette and materials.\n'''
groups={
'cantor':[
 ('CantorHead','Isolate ONLY the head and a very short neck collar of the reference serpent, no body or tail. Elongated navy-violet armored wedge skull, four rooted swept fins, a coral oval resonator centered on forehead outlined with cyan, forked mouth with coral inside. Straight forward-facing head axis, neutral straight neck collar ending in one circular connecting socket. Head length 1.7-2.1 times its width. No bent long neck.'),
 ('CantorBody','Design one SINGLE short straight cylindrical BODY SEGMENT of the reference serpent, no head or tail. Four overlapping navy-violet armor plates surrounding a solid short cylinder; one broad coral circular weak-point organ on TOP rimmed with thin cyan branches. Small rooted dorsal blade ridge. Circular capped ends matching adjacent segments, width-to-length 0.85-1.0, thickness 0.70-0.85 of width. No long curving body; just one reusable straight link.'),
 ('CantorTail','Isolate ONLY a SHORT straight tapering TAIL TIP of the reference serpent. Navy-violet plated connecting collar tapering into a single flat leaf-shaped tail fin, cyan vein down the fin, all connected. No head, coral eye, body chain or curved neck. Length 1.8-2.2 times maximum width, clear flattened fin profile.')],
'sorrel-environment-board':[
 ('MoonGroundA','Only the top-left dusty ochre moon ground tile: square low relief slab, sparse small embedded gravel and shallow cracks; flat walkable top. Width-to-thickness 12-18, square footprint. All gravel attached, no loose outlying stones.'),
 ('MoonGroundB','Only the second cracked ochre moon ground tile: square low relief slab with broad irregular bedrock plates and narrow dark seams; flat walkable top. Width-to-thickness 10-16, square footprint. All stones connected.'),
 ('RidgeRock1','Only the tall narrow stratified sandstone ridge spire from the top row. Warm ochre fractured plates, tapered silhouette, compact integral rocky foot. Height 2.2-2.7 times width. No isolated surrounding pebbles.'),
 ('RidgeRock2','Only the broad low stratified sandstone ridge boulder from top row. Warm ochre angular steps, broad compact silhouette. Width 1.8-2.2 times height. No isolated surrounding pebbles.'),
 ('RidgeRock3','Only the stepped mesa sandstone rock from the far top right: three broad natural ledges with cracked ochre surfaces. Width 1.3-1.7 times height. One connected mass with compact root; no isolated surrounding pebbles.'),
 ('CrystalClusterA','Only the tall three-prong cyan Ridge Crystal cluster: exactly three thick faceted cyan prisms emerging from one connected ochre rock base, all tips separated by white gaps. Tallest crystal 1.5-1.8 times the shorter one. No bloom or detached stones.'),
 ('CrystalClusterB','Only the low broad five-prong cyan Ridge Crystal cluster: five thick faceted cyan prisms emerging from one connected ochre rock base, all tips visible with white gaps. Width 0.9-1.2 times height. No bloom or detached stones.'),
 ('LatticeAnvil','Only the center Lattice Anvil: heavy waist-high slate-blue pedestal, wide tapered ivory-banded top with recessed rectangular cyan interface, flared integral foot. Width 1.5-1.8 times height. No text on display.'),
 ('OutpostHab','Only the rounded outpost habitat module: squat slate dome, ivory door arch and corner braces, small cyan seams, integrated roof vent and short antenna. Width 1.3-1.6 times height, functional door fully attached. No environment.'),
 ('OutpostDrill','Only the upright outpost drilling machine: tall slate central cylinder with copper screw-drill rooted directly below it and four sturdy splayed support legs. Height 2.0-2.5 times foot span. Wide WHITE gaps between every leg and central drill, four feet all distinct. Cyan seams and ivory braces, no detached pieces.'),
 ('LandingPad','Only the low broad circular landing pad, complete round disk and integral short front ramp. Slate blue inset center, ivory ring, restrained copper trim and thin cyan radial seams. Diameter 12-16 times thickness. No letters or symbols.')],
'halo-environment-board':[
 ('RingSegmentA','Only the plain curved station ring corridor segment from upper left: ivory roof and smooth hull, slate lower hull, broad blue-gray window strip and copper end collars; subtle cyan longitudinal seam. Arc spans 15-20 degrees, length 3-4 times cross-section width, two closed modular ends. No surrounding station.'),
 ('RingSegmentB','Only the curved station ring segment with one integral arch brace over its roof, second object top row. Ivory hull, slate base, copper brace and end collars, thin cyan seams. Arc spans 15-20 degrees, length 3-4 times cross-section width; a white open gap under the brace. No surrounding station.'),
 ('DockingPodOffice','Only the OFFICE docking pod at upper right: rounded ivory capsule with broad horizontal blue-gray window band, copper door frame, small door step and one integral tall slim rear vane. Cyan thin seams, slate underside. Width 1.5-1.9 times height. No separate items.'),
 ('DockingPodOutfitter','Only the OUTFITTER docking pod at lower left: rounded ivory capsule with broad display window and projecting copper canopy, slate underside and cyan seams. Width 1.5-1.9 times height. Display stays blank dark teal, no miniature furniture or text.'),
 ('DockingPodRepair','Only the REPAIR docking pod at lower middle: rounded ivory shell with a wide dark OPEN bay and integrated lowered ramp, paired short copper docking arms attached at its sides, cyan seams. White gap outside arms, open bay deep but unoccupied; no tiny equipment. Width 1.3-1.7 times height.'),
 ('WarpBeacon','Only the upright warp beacon at lower right: two tall ivory curved prongs with slate inner faces and cyan strips, joined firmly to one circular base. Broad uninterrupted WHITE gap between prongs. Height 2.0-2.5 times base diameter. No energy sheet, portal fog, detached ornaments or text.')],
'decks-environment-board':[
 ('DeckWall','Only the upright rectangular wall panel from upper left: two matte ivory inset panels, one thin central cyan seam, copper ribs and slate lower kickplate. Width 1.0-1.2 times height, thickness 0.05-0.08 height. One continuous panel.'),
 ('DeckFloor','Only the square floor panel from upper middle: flat walkable matte beige top, copper edge ribs, slate reinforced corners, sparse cyan edge slots. Width 12-16 times thickness. No raised obstacles.'),
 ('DeckDoorway','Only the open doorway arch from upper right: thick rectangular ivory/copper/slate frame with chamfered top corners and cyan edge strips, attached low threshold. A broad fully WHITE EMPTY opening inside, no door leaf. Clear width 0.55-0.65 overall height.'),
 ('DeckConsole','Only the standing terminal console from lower left: slate pedestal with ivory work ledge, one tilted dark blank screen and copper trim, small cyan side strips. Height 0.9-1.2 times width. No text, detached keyboards or floating display.'),
 ('DeckBench','Only the workbench from lower middle: wide beige worktop, copper edges, two broad slate end supports and low integral back board with cyan strip. Width 2.0-2.5 times height. Large white gap below table between end supports. No tools or text.'),
 ('DeckCrate','Only the cargo crate from lower right: one closed rounded rectangular ivory/slate box, copper seam ribs, dark recessed handles and one thin cyan locking strip. Width 1.2-1.4 times height. No labels or other objects.')],
'gullet-environment-board':[
 ('GulletWallA','Derive one single SIDE wall panel from the left open tunnel design. A violet chitin wall with three thick curved ribs framing deep navy membrane panels; tiny sparse cyan veins. ONE wall only, no full tunnel or opposite wall or floor. Upright gently concave rectangle, length 1.5-1.9 times height, thick structural edges. No detached rib tips.'),
 ('GulletWallB','Only the right-hand wall segment with an integral organic ledge: violet chitin ribs around deep navy membrane panels, tiny sparse cyan veins. ONE gently concave wall panel, length 1.5-1.9 times height. Broad flat ledge grown directly from the lower third, no opposite wall or floor.'),
 ('ChoirPod','Only the bottom-left Choir spawning pod: one closed upright ovoid violet organic capsule with three narrow cyan slits, broad rooted rib plates, connected flared organic base. Height 1.5-1.9 times width. No creatures, smoke or separate pieces.')]
}
oldpath=ROOT/'docs/art/isolated-catalog.json';old=json.loads(oldpath.read_text(encoding='utf-8')) if oldpath.exists() else []
rows=[]
for board,entries in groups.items():
 for name,brief in entries:
  prompt=STYLE+'Input image is a design reference only. '+brief+'\nRender this ONE object alone on opaque pure white; do not copy the multi-object layout.\n'
  path=ROOT/'docs/art/prompts'/('model-'+name+'.md');path.write_text(prompt,encoding='utf-8')
  row=dict(id=name,reference='art-src/Generated/P1-production/refs/'+board+'.png',prompt=prompt,prompt_file=path.relative_to(ROOT).as_posix())
  prior=next((r for r in old if r['id']==name),None)
  if prior:row.update({k:v for k,v in prior.items() if k in ('accepted_source','qa')})
  rows.append(row)
oldpath.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8');print('ISOLATED_PROMPTS_READY '+str(len(rows)))
