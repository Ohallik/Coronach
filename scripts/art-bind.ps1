<#
.SYNOPSIS
  Binds generated art to definitions that didn't exist when the art was taken in.

  Idra's bodies and chapters 4 to 10's creatures and bosses were staged before `CharacterDef` Idra and those chapters'
  `EnemyDef`s were built, so their intake rows named no character and no enemy (GenIntake rejects an unknown one). Run
  this once those assets exist: it rewrites the rows (they now find their definitions), stages them again, and imports
  the portrait sheets so each binds its speaker. Safe to run twice; a definition still missing just leaves its row
  unbound and says so. (Idra's were bound this way on November 12, as soon as her definition appeared; chapters 4 to 10
  wait for theirs.)

  What binding adds: Idra's three forms on her definition (Braced in the `shaped` slot) and the hero stride parameter
  on her controllers; each enemy's prefab on its definition (the ids in $enemies below, each row named for its EnemyDef
  id), the recoil layer of the bipeds LifecycleCalibration.HuskRig knows (the Drowned Sentry, the Thawed Guard, the Glass
  Sentinel and the Muster Excavator by their meshes; the Host Marshal, the Last Warden, Drask Vaal and his Pikeguards,
  Vessa, the Pruner, Meret's two frames and the Compact Wardens have their own meshes and get it only once HuskRig names
  them too), and the occlusion-fade shader variant for a boss that BossController.Occludes names; `portraitNatural` for
  every speaker in $portraits and `portraitShaped` for Idra's Braced sheet. The chapters' NPC bodies (MeretBody, TernBody,
  LumeBody, TovraBody, DorrowBody, JunBody and the rest) are used by prefab key and bind nothing.

.EXAMPLE
  & .\scripts\art-bind.ps1
#>
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
# Each chapter's rows, in order: a later chapter's coat may copy an earlier chapter's row (chapter 8's Vessa is chapter
# 6's body, chapter 10's Mother of Doors wears chapter 1's Cantor segments).
foreach ($script in 'c13_idra_intake', 'c11_intake', 'c12_intake', 'c13_hoarfell_intake', 'c14_intake', 'c15_intake',
                    'c16_intake', 'c17_intake') {
    python "tools/$script.py" prepare
    if ($LASTEXITCODE -ne 0) { throw "FAILED: $script rows" }
}
$enemies = 'Idra,AncelaneGuard,DrownedSentry,Rotbloom,LureStalk,Bogstrider,VeilRay,RotPod,Lurelight,' +
    'Hulljack,DessikVarn,' +
    'HostMarshal,ThawedGuard,MusterExcavator,RimeStalker,BridleHound,' +
    'LastWarden,Pruner,GlassSentinel,Glassback,ClampTender,RailWarden,Moonshard,' +
    'SlagColossus,DraskVaal,DraskSkiff,VessaKorl,MusterPikeguard,MusterLancer,MusterMortar,AshCrawler,PourDrone,ForgeHound,SensorBuoy,FlakGate,' +
    'FoundrySaint,DebrisCutter,SlagGull,OverrideRig,WeldArm,Interceptor,HeatScavenger,BridleFragment,TowDrone,' +
    'MotherOfDoors,MotherBody,MotherTail,Tollkeeper,MeretShip,MeretGantry,MeretWell,CompactWarden'
& (Join-Path $PSScriptRoot 'exec-when-green.ps1') -Method Lattice.EditorTools.GenIntake.BuildPrefix -Marker GEN_INTAKE_OK `
    -ExtraArgs @('-intake-prefix', $enemies)
if ($LASTEXITCODE -ne 0) { throw 'FAILED: model intake' }
$portraits = 'Idra_Natural,Idra_Braced,Maru_Natural,Ona_Natural,Dov_Natural,Quill_Natural,Pim_Natural,' +
    'Meret_Natural,Dessik_Natural,' +
    'Tern_Natural,Hask_Natural,Iru_Natural,Vessa_Natural,' +
    'Lume_Natural,Warden_Natural,' +
    'Drask_Natural,Tovra_Natural,Odrun_Natural,Pip_Natural,Bryn_Natural,' +
    'Dorrow_Natural,Tam_Natural,Jun_Natural'
& (Join-Path $PSScriptRoot 'exec-when-green.ps1') -Method Lattice.EditorTools.PortraitIntake.ImportNamed -Marker PORTRAIT_INTAKE_OK `
    -ExtraArgs @('-portrait-names', $portraits)
if ($LASTEXITCODE -ne 0) { throw 'FAILED: portrait intake' }
git checkout -- Lattice/Packages/packages-lock.json
'ART_BIND_OK'
