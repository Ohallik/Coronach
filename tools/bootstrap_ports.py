"""One-time P0 import of reusable Frostbound code/tools; never writes to the donor repo."""
from pathlib import Path
import json
import shutil

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path('C:/Users/natem/Projects/FrostboundUnity')
CODE = SOURCE / 'Frostbound/Assets/_Project/Scripts'
DEST = ROOT / 'Lattice/Assets/_Project/Scripts'

def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding='utf-8')

def port(src, dst):
    text = src.read_text(encoding='utf-8-sig')
    text = text.replace('FrostboundUnity', 'SpaceRPG').replace('Frostbound', 'Lattice').replace('FROSTBOUND', 'LATTICE').replace('frostbound', 'lattice')
    write(dst, text)

for folder in ['Art', 'Audio', 'Data', 'Prefabs', 'Resources', 'Scenes', 'Scripts', 'Settings', 'Yarn']:
    (ROOT / 'Lattice/Assets/_Project' / folder).mkdir(parents=True, exist_ok=True)

shutil.copytree(SOURCE / 'Frostbound/Assets/TextMesh Pro', ROOT / 'Lattice/Assets/TextMesh Pro', dirs_exist_ok=True)
shutil.copytree(SOURCE / 'tools/meshy', ROOT / 'tools/meshy', dirs_exist_ok=True, ignore=shutil.ignore_patterns('__pycache__'))
for path in (ROOT / 'tools/meshy').rglob('*.py'):
    text = path.read_text(encoding='utf-8-sig').replace('FrostboundUnity', 'SpaceRPG').replace('Frostbound', 'Lattice')
    text = text.replace('docs" / "polish" / "P30-artdirection', 'docs" / "art')
    write(path, text)

tools = {'p64_batch.py': 'gen_batch.py', 'portraits_redo_slice.py':'portraits_redo_slice.py', 'portraits_redo_assemble.py':'portraits_redo_assemble.py', 'p64_contact_sheet.py':'contact_sheet.py'}
for src, dst in tools.items():
    port(SOURCE / 'tools' / src, ROOT / 'tools' / dst)
for name in ['_common', 'p64_donor_rig', 'p64_strip_embedded', 'p64_anim_check', 'p64_preview', 'p64_handcheck', 'p64_deform_check', 'p70_retarget', 'p70_posecheck']:
    port(SOURCE / 'tools/blender' / (name + '.py'), ROOT / 'tools/blender' / (name + '.py'))
for src, dst in {'headless.ps1':'headless.ps1','exec-log-name.ps1':'exec-log-name.ps1','smoketest.ps1':'smoketest.ps1','p64-exec.ps1':'exec.ps1','blender.ps1':'blender.ps1'}.items():
    port(SOURCE / 'scripts' / src, ROOT / 'scripts' / dst)

direct = ['Core/PadBridge.cs','Core/PadSupport.cs','Core/UiActions.cs','Core/PromptService.cs','UI/PadFocus.cs','UI/UiKit.cs','UI/UiSkin.cs','Data/Art/PortraitSet.cs','Dialogue/EmotionTags.cs','Dialogue/TypewriterPacing.cs','Editor/Art/PipelineConverter.cs']
for name in direct:
    port(CODE / name, DEST / name)

# Preserve donor implementations for adaptation; these copies do not compile into the game.
review = ['Core/GameInput.cs','Core/SaveSystem.cs','Core/SceneFlow.cs','Core/FlagService.cs','Editor/BatchTools.cs','Editor/Art/PackStaging.cs','Editor/Art/ToonAssetPostprocessor.cs','Dialogue/DialogueSystem.cs','Dialogue/FrostboundLinePresenter.cs','Dialogue/FrostboundOptionsPresenter.cs']
for name in review:
    port(CODE / name, ROOT / 'tools/port-reference' / (name.replace('Frostbound','Lattice') + '.txt'))
for kind, name in [('EditMode','PadSupportTests.cs'),('PlayMode','PadSupportPlayTests.cs')]:
    port(SOURCE / 'Frostbound/Assets/Tests' / kind / name, ROOT / 'tools/port-reference/Tests' / (name + '.txt'))
port(SOURCE / 'Frostbound/Assets/_Project/Art/Shaders/FrostboundToon.shader', ROOT / 'Lattice/Assets/_Project/Art/Shaders/LatticeToon.shader')

portrait = DEST / 'Data/Art/PortraitSet.cs'
write(portrait, portrait.read_text(encoding='utf-8').replace('Tech,', 'Synced,'))
dependencies = {'com.unity.render-pipelines.universal':'17.4.0', 'com.unity.inputsystem':'1.19.0','com.unity.cinemachine':'3.1.7','com.unity.ai.navigation':'2.0.13','com.unity.splines':'2.9.1','com.unity.probuilder':'6.1.1','com.unity.nuget.newtonsoft-json':'3.2.2','com.unity.2d.sprite':'1.0.0','com.unity.ugui':'2.0.0','com.unity.timeline':'1.8.12','com.unity.test-framework':'1.4.6','dev.yarnspinner.unity':'https://github.com/YarnSpinnerTool/YarnSpinner-Unity.git#v3.2.4'}
for module in ['animation','audio','imageconversion','imgui','jsonserialize','physics','screencapture','ui','uielements','umbra','unitywebrequest','unitywebrequestassetbundle','unitywebrequestaudio','unitywebrequesttexture','particlesystem']:
    dependencies['com.unity.modules.' + module] = '1.0.0'
write(ROOT / 'Lattice/Packages/manifest.json', json.dumps({'dependencies':dependencies}, indent=2)+'\n')

assemblies = {
    'Data':[],
    'Core':['Lattice.Data','Unity.InputSystem','Unity.ugui','Unity.TextMeshPro','Unity.Cinemachine'],
    'Rpg':['Lattice.Data','Lattice.Core'],
    'Combat':['Lattice.Data','Lattice.Core','Lattice.Rpg'],
    'World':['Lattice.Data','Lattice.Core','Lattice.Rpg','Lattice.Combat'],
    'Dialogue':['Lattice.Data','Lattice.Core','Lattice.Rpg','YarnSpinner','YarnSpinner.Unity','Unity.TextMeshPro','Unity.ugui'],
    'UI':['Lattice.Data','Lattice.Core','Lattice.Rpg','Lattice.Combat','Lattice.World','Lattice.Dialogue','Unity.InputSystem','Unity.TextMeshPro','Unity.ugui'],
    'Editor':['Lattice.Data','Lattice.Core','Lattice.Rpg','Lattice.Combat','Lattice.World','Lattice.Dialogue','Lattice.UI','Unity.InputSystem','Unity.TextMeshPro','Unity.ugui','Unity.RenderPipelines.Core.Runtime','Unity.RenderPipelines.Universal.Runtime','Unity.RenderPipelines.Universal.Editor','Unity.Cinemachine']
}
for name, refs in assemblies.items():
    asm = {'name':'Lattice.'+name,'rootNamespace':'Lattice.'+name,'references':refs}
    if name == 'Editor': asm['includePlatforms']=['Editor']
    write(DEST / name / ('Lattice.'+name+'.asmdef'), json.dumps(asm,indent=2)+'\n')

print('PORT_FILES_OK')
