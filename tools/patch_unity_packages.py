"""Reproducible Unity 6000.4.7 / Shader Graph 17.4 GUID namespace compatibility fix.
Matches the working Frostbound package's UnityEngine.GUID qualification.
Only touches the Lattice project's generated package cache, never the editor install.
"""
from pathlib import Path
import re
root=Path(__file__).resolve().parents[1]/'Lattice/Library/PackageCache'
count=0
for package in root.glob('com.unity.shadergraph@*'):
    for relative in ['Editor/Generation/Contexts/TargetSetupContext.cs','Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Targets/BuiltInCanvasSubTarget.cs','Editor/Generation/Processors/ShaderSpliceUtil.cs']:
        path=package/relative
        if not path.exists():continue
        text=path.read_text(encoding='utf-8-sig')
        fixed=re.sub(r'(?<![\w.])GUID\b','UnityEngine.GUID',text)
        if fixed!=text:
            path.chmod(0o666)
            path.write_text(fixed,encoding='utf-8')
            count+=1
# Yarn Spinner's source generator is an unsigned compiler plug-in. Windows Smart App Control stops the compiler loading
# it on some days and not on others (a Windows Security toast for every assembly compiled, and warning CS8034), so a
# build was made with or without it depending on the machine. The game registers its Yarn commands by hand
# (DialogueSystem.AddCommandHandler) and uses nothing the generator writes: stop offering it to the compiler.
for meta in root.glob('dev.yarnspinner.unity@*/SourceGenerator/YarnSpinner.Unity.SourceCodeGenerator.dll.meta'):
    text=meta.read_text(encoding='utf-8')
    fixed=re.sub(r'labels:\r?\n- RoslynAnalyzer\r?\n','',text)
    if fixed!=text:
        meta.chmod(0o666)
        meta.write_text(fixed,encoding='utf-8',newline='')
        count+=1
print(f'PACKAGE_COMPAT_OK modified={count}')
