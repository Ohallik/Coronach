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
print(f'PACKAGE_COMPAT_OK modified={count}')
