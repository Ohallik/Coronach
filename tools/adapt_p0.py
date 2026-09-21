from pathlib import Path
import json
import shutil

ROOT=Path(__file__).resolve().parents[1]
CODE=ROOT/'Lattice/Assets/_Project/Scripts'
def edit(path, fn):
    text=path.read_text(encoding='utf-8-sig')
    path.write_text(fn(text),encoding='utf-8')

# Keep the full, proven result adjudicator; discard campaign-specific lint jobs.
edit(ROOT/'scripts/headless.ps1', lambda t: t[:t.index('# ---- POLISH-51:')] + t[t.index('function Stop-HeadlessUnityTree'):])
edit(ROOT/'scripts/headless.ps1', lambda t: t.replace('[int]$StallTimeoutMinutes = 15','[int]$StallTimeoutMinutes = 3').replace('[int]$StallRetries = 1','[int]$StallRetries = 1,\n    [int]$TimeoutSec = 900').replace('BatchTools.CreateTitleScene','BatchTools.CreateBootScenes').replace('Polish50BTools.BuildWindowsDevPlayer','BatchTools.BuildWindowsDevPlayer').replace('if ($silentMinutes -ge $StallTimeoutMinutes)', 'if ($silentMinutes -ge $StallTimeoutMinutes -or ([datetime]::UtcNow - $attemptStartUtc).TotalSeconds -ge $TimeoutSec)'))

# Fix the one-MonoBehaviour-per-file exception inherited inside UiKit.
ui=CODE/'UI/UiKit.cs'
text=ui.read_text(encoding='utf-8')
start=text.index('        public sealed class HoverSelect')
end=text.index('        /// <summary>',start)
hover=text[start:end]
ui.write_text(text[:start]+text[end:],encoding='utf-8')
(CODE/'UI/HoverSelect.cs').write_text('using UnityEngine;\nusing UnityEngine.UI;\nnamespace Lattice.UI\n{\n'+hover+'}\n',encoding='utf-8')
edit(CODE/'Core/PromptService.cs',lambda t:t.replace('!Game.IsReady','GameInput.Current == null').replace('Game.IsReady ? Game.Manager.Input.Find(actionName) : null','GameInput.Current != null ? GameInput.Current.Find(actionName) : null').replace('"pad_ls", "stick"','"pad_ls", "d-pad / stick"'))

# URP asset generation must be idempotent and fail for absent serialized properties.
edit(CODE/'Editor/Art/PipelineConverter.cs',lambda t:t.replace('Assets/Settings/', 'Assets/_Project/Settings/').replace('            // 1. 3D', '            Directory.CreateDirectory("Assets/_Project/Settings");\n            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath) is { } existing) return existing;\n            // 1. 3D').replace('Debug.LogWarning($"[Lattice] missing serialized property {prop}")','throw new InvalidOperationException($"Missing serialized property {prop}")'))

src=Path('C:/Users/natem/.codex/generated_images/01a0c14a-c45a-7961-bd43-e4ef0c191121/exec-25251abc-ee75-401f-9217-2bae38b06947.png')
shutil.copy2(src,ROOT/'art-src/Generated/P0/refs/title-key-art.png')
target=ROOT/'Lattice/Assets/_Project/Resources/UI/Title/key-art.png'
target.parent.mkdir(parents=True,exist_ok=True)
shutil.copy2(src,target)
print('P0_ADAPT_OK')
