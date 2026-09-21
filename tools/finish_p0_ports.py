from pathlib import Path
import json
R=Path(__file__).resolve().parents[1]
C=R/'Lattice/Assets/_Project/Scripts'

# Limit process teardown to descendants launched for this project.
p=R/'scripts/headless.ps1'
t=p.read_text(encoding='utf-8')
start=t.index('function Stop-HeadlessUnityTree')
end=t.index('$Unity =',start)
t=t[:start]+'. (Join-Path $PSScriptRoot "unity-process.ps1")\nfunction Stop-HeadlessUnityTree {\n    param([System.Diagnostics.Process]$UnityProcess)\n    Stop-LatticeProcessTree $UnityProcess.Id\n}\n\n'+t[end:]
t=t.replace('$LogDir = Join-Path', 'Assert-LatticeUnlocked $ProjectPath\n$LogDir = Join-Path',1)
# Verify/build/scene get marker watch, including early completion before shutdown.
needle='Write-Host "Running Unity headless: $Mode $Method (log: $LogFile)"'
replacement='''if ($Mode -in @("verify", "scene", "build", "builddev")) {
    $target = switch ($Mode) {
        'verify' { 'Lattice.EditorTools.BatchTools.Verify' }
        'scene' { 'Lattice.EditorTools.BatchTools.CreateBootScenes' }
        'build' { 'Lattice.EditorTools.BatchTools.BuildWindowsPlayer' }
        'builddev' { 'Lattice.EditorTools.BatchTools.BuildWindowsDevPlayer' }
    }
    $marker = switch ($Mode) { 'verify' {'VERIFY_OK'} 'scene' {'BOOT_SCENES_OK'} default {'BUILD_OK'} }
    & (Join-Path $PSScriptRoot 'exec.ps1') -Method $target -Marker $marker -TimeoutSec $TimeoutSec
    exit $LASTEXITCODE
}
'''+needle
t=t.replace(needle,replacement)
t=t.replace('            Remove-Item -LiteralPath $tempRoot -Recurse -Force','            if (-not ([IO.Path]::GetFullPath($tempRoot).StartsWith([IO.Path]::GetTempPath()))) { throw "Unsafe contract fixture path" }\n            Remove-Item -LiteralPath $tempRoot -Recurse -Force')
p.write_text(t,encoding='utf-8')

ref=R/'tools/port-reference/Tests'
dest=R/'Lattice/Assets/Tests'
for kind in ['EditMode','PlayMode']:
    folder=dest/kind;folder.mkdir(parents=True,exist_ok=True)
    asm={'name':'Lattice.Tests.'+kind,'references':['Lattice.Data','Lattice.Core','Lattice.Rpg','Lattice.Combat','Lattice.World','Lattice.Dialogue','Lattice.UI','Unity.InputSystem','Unity.ugui','Unity.TextMeshPro'],'optionalUnityReferences':['TestAssemblies']}
    if kind=='EditMode':asm['includePlatforms']=['Editor']
    (folder/('Lattice.Tests.'+kind+'.asmdef')).write_text(json.dumps(asm,indent=2),encoding='utf-8')
(dest/'EditMode/PadSupportTests.cs').write_text((ref/'PadSupportTests.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
t=(ref/'PadSupportPlayTests.cs.txt').read_text(encoding='utf-8')
# Keep the real translation tests and full title-navigation test. Campaign menu tests
# depend on Frostbound's turn-based inventory and are replaced in P2.
end=t.index('        // ==================== 4.')
t=t[:end]+'    }\n}\n'
t=t.replace('using Lattice.Menus;\n','').replace('using Lattice.World;\n','')
t=t.replace('            Game.Ensure();','')
t=t.replace('Game.Manager.Input.InteractHeld','GameInput.Current.Held("Interact")').replace('Game.Manager.Input.MenuHeld','GameInput.Current.Held("Pause")').replace('Game.Manager.Input.Move','GameInput.Current.Move')
t=t.replace('SceneManager.LoadScene("Title");','SceneManager.LoadScene("_Boot");').replace('Assert.IsTrue(Game.IsReady);','Assert.IsNotNull(GameServices.Current);')
t=t.replace('Object.FindFirstObjectByType<GameMenu>()','Object.FindFirstObjectByType<TitleScreen>()').replace('menu.IsOpen && menu.IsTitleMode','menu.SettingsOpen').replace('!menu.IsOpen','!menu.SettingsOpen')
t=t.replace('            yield break;','            SceneManager.LoadScene("_Boot");\n            yield return null;\n            yield return null;\n            yield return new WaitForSeconds(.3f);',1)
(dest/'PlayMode/PadSupportPlayTests.cs').write_text(t,encoding='utf-8')

# Correct build root: Assets -> Lattice -> SpaceRPG.
p=C/'Editor/BatchTools.cs'
p.write_text(p.read_text(encoding='utf-8').replace('"../../.."','"../.."'),encoding='utf-8')
# Bridge already uses the list as an append-only task ledger.
(R/'docs/art/gen-manifest.json').write_text('[]\n',encoding='utf-8')
print('P0_PORT_ADAPT_OK')
