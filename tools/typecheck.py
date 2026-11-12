"""Type-check the Lattice assemblies with Unity's own Roslyn, without starting Unity.

Reuses the response files Unity's last compile wrote (Library/Bee/artifacts/<dag>/Lattice.*.rsp), swaps in
the current .cs files under each asmdef folder (so new files count), and compiles in dependency order into a
scratch folder, each assembly against the freshly built references of the ones before it. It catches most
compile errors in a few seconds; Unity's own compile (headless.ps1 -Mode verify) is still the gate.

    python tools/typecheck.py                 # every Lattice assembly, editor dag
    python tools/typecheck.py Combat UI       # only these (and still builds what they depend on)
    python tools/typecheck.py --player        # the player dag (no UNITY_EDITOR, no editor/test assemblies)
"""
import os
import re
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = os.path.join(ROOT, "Lattice")
UNITY = r"C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Data"
DOTNET = os.path.join(UNITY, "NetCoreRuntime", "dotnet.exe")
CSC = os.path.join(UNITY, "DotNetSdkRoslyn", "csc.dll")
ASMDEFS = {
    "Lattice.Data": "Assets/_Project/Scripts/Data",
    "Lattice.Core": "Assets/_Project/Scripts/Core",
    "Lattice.Rpg": "Assets/_Project/Scripts/Rpg",
    "Lattice.Dialogue": "Assets/_Project/Scripts/Dialogue",
    "Lattice.Combat": "Assets/_Project/Scripts/Combat",
    "Lattice.World": "Assets/_Project/Scripts/World",
    "Lattice.UI": "Assets/_Project/Scripts/UI",
    "Lattice.Editor": "Assets/_Project/Scripts/Editor",
    "Lattice.Tests.EditMode": "Assets/Tests/EditMode",
    "Lattice.Tests.PlayMode": "Assets/Tests/PlayMode",
}


def find_dag(player):
    base = os.path.join(PROJECT, "Library", "Bee", "artifacts")
    for name in sorted(os.listdir(base)):
        if not name.endswith(".dag"):
            continue
        if player and name.endswith("P.dag"):
            return os.path.join(base, name)
        if not player and name.endswith("E.dag"):
            return os.path.join(base, name)
    sys.exit("no %s dag under %s: run Unity once first" % ("player" if player else "editor", base))


def sources(folder):
    out = []
    top = os.path.join(PROJECT, folder)
    for d, dirs, files in os.walk(top):
        if d != top and any(f.endswith(".asmdef") for f in files):
            dirs[:] = []
            continue
        out += [os.path.join(d, f) for f in files if f.endswith(".cs")]
    return sorted(out)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    player = "--player" in sys.argv
    dag = find_dag(player)
    scratch = os.path.join(tempfile.gettempdir(), "lattice-typecheck" + ("-player" if player else ""))
    os.makedirs(scratch, exist_ok=True)
    rsps = {n: os.path.join(dag, n + ".rsp") for n in ASMDEFS if os.path.exists(os.path.join(dag, n + ".rsp"))}
    deps = {}
    for n, p in rsps.items():
        text = open(p, encoding="utf-8").read()
        deps[n] = [m for m in re.findall(r'-r:"[^"]*?/(Lattice\.[A-Za-z.]+)\.ref\.dll"', text) if m in rsps]
    wanted = set(rsps) if not args else {("Lattice." + a if not a.startswith("Lattice.") else a) for a in args}
    order, seen = [], set()

    def visit(n):
        if n in seen:
            return
        seen.add(n)
        for d in deps.get(n, []):
            visit(d)
        order.append(n)

    for n in sorted(wanted):
        if n not in rsps:
            sys.exit("unknown assembly %s (known: %s)" % (n, ", ".join(sorted(rsps))))
        visit(n)
    failed = 0
    for n in order:
        lines = open(rsps[n], encoding="utf-8").read().splitlines()
        kept = []
        for line in lines:
            s = line.strip()
            if s.endswith('.cs"') or s.endswith(".cs"):
                continue
            if s.startswith("-out:") or s.startswith("-refout:") or s.startswith("/additionalfile:") or s.startswith("-analyzer:"):
                continue
            m = re.match(r'-r:"[^"]*?/(Lattice\.[A-Za-z.]+)\.ref\.dll"', s)
            if m and m.group(1) in rsps:
                s = '-r:"%s"' % os.path.join(scratch, m.group(1) + ".ref.dll")
            kept.append(s)
        kept.append('-out:"%s"' % os.path.join(scratch, n + ".dll"))
        kept.append('-refout:"%s"' % os.path.join(scratch, n + ".ref.dll"))
        kept += ['"%s"' % f for f in sources(ASMDEFS[n])]
        rsp = os.path.join(scratch, n + ".rsp")
        open(rsp, "w", encoding="utf-8").write("\n".join(kept) + "\n")
        r = subprocess.run([DOTNET, CSC, "/noconfig", "@" + rsp], cwd=PROJECT, capture_output=True, text=True)
        errors = [l for l in r.stdout.splitlines() if ": error " in l]
        if r.returncode != 0:
            failed += 1
            print("FAIL %s (%d errors)" % (n, len(errors)))
            for l in errors[:40]:
                print("  " + l.replace(PROJECT + os.sep, ""))
            if not errors:
                print(r.stdout[-3000:], r.stderr[-2000:])
        else:
            print("ok   %s" % n)
    print("TYPECHECK_" + ("FAILED" if failed else "OK"))
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
