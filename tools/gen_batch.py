#!/usr/bin/env python3
"""P64 batch driver: reference image -> Meshy mesh -> rig -> staged FBX.

One command per character was fine for a pilot. This runs the whole approved
roster unattended, and encodes the two rules the pass established:

  * BIPEDS (humans, standing bears) use Meshy's own rigging. Proven end to end.
  * QUADRUPEDS AND BIRDS DO NOT. Meshy's rigging endpoint is humanoid-only
    (probed 2026-08-07: quadruped -> HTTP 422 while a biped through the identical
    path succeeds, and the API has no rig_type parameter at all). Their skeletons
    come from the donor graft instead — the Quaternius Fox for mammals, Birb for
    perched birds — which costs nothing and carries real walk cycles.

Resumable by construction: an entry whose output already exists is skipped, so a
run that dies on credit exhaustion can be re-run when the balance refills and it
picks up exactly where it stopped.

  python tools/p64_batch.py --manifest docs/polish/P64-models/batch.json
  python tools/p64_batch.py --manifest ... --only Lyra,Beatrice
  python tools/p64_batch.py --manifest ... --dry-run
  python tools/p64_batch.py --manifest ... --prompt-id P73_TOWN_CHARACTER \
      --report docs/polish/P73-towncraft/character_batch_results.json
"""

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MESHY = ROOT / "tools" / "meshy" / "meshy.py"
BLENDER_PS1 = ROOT / "scripts" / "blender.ps1"
GEN = ROOT / "art-src" / "Generated"

DONORS = {
    "quadruped": ROOT / "art-src" / "Quaternius_UltimateAnimatedAnimals" / "FBX" / "Fox.fbx",
    "bird": ROOT / "art-src" / "Quaternius_UltimateMonsters" / "FBX" / "Birb.fbx",
    # An upright body Meshy's rigger will not take. Its pose estimator wants a
    # recognisable human silhouette and answers HTTP 422 "pose estimation failed"
    # on anything far enough from one — the Ember Colossus's head is a small block
    # sunk between its shoulders, so there is no neck to find. The graft lane does
    # not care what the body looks like: it fits an armature by bounds and binds.
    # Yeti is the bulkiest owned humanoid and its skeleton is already proven here.
    "graft": ROOT / "art-src" / "Quaternius_UltimateMonsters" / "FBX" / "Yeti.fbx",
}
# Meshy's polycount bands (tools/meshy/meshy.py POLY_BANDS)
BAND = {"biped": ("humanoid", 12000), "quadruped": ("hero", 12000), "bird": ("prop", 6000),
        "static": ("hero", 12000), "graft": ("humanoid", 12000)}

#: Which axis a kind is measured on when the donor armature is fitted.
GRAFT_FIT = {"quadruped": "length", "bird": "height", "graft": "height"}

#: A fourth kind, added in P65. Some creatures have no plausible donor on either
#: lane — a limbless wyrm and a shelled ambusher share no skeleton with a fox, a
#: bird or a human — and Meshy's rigging endpoint is humanoid-only. Pretending
#: otherwise produces a body bound to a skeleton that does not fit it, which
#: animates worse than not animating. These ship STATIC and take their motion
#: procedurally from the game, and the output keeps an honest name rather than
#: being copied to `_Rigged`.
STATIC_KIND = "static"


def run(cmd: list[str], label: str) -> tuple[int, str]:
    print(f"    $ {label}", flush=True)
    proc = subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True, encoding="utf-8",
                          errors="replace", timeout=3900)
    out = (proc.stdout or "") + (proc.stderr or "")
    return proc.returncode, out


def credits_from(text: str) -> int:
    total = 0
    for line in text.splitlines():
        if "credits=" in line and "SUCCEEDED" in line:
            try:
                total = max(total, int(line.split("credits=")[1].split()[0]))
            except (ValueError, IndexError):
                pass
    return total


MANIFEST = ROOT / "docs" / "art" / "gen-manifest.json"


def lookup_img3d_task(mesh_fbx: Path, prompt_id: str = "LATTICE_P3") -> str | None:
    """Find the image-to-3D task that produced this mesh, from the provenance
    manifest meshy.py maintains. Lets a resumed run rig a mesh it did not itself
    generate, instead of stranding it."""
    try:
        rows = json.loads(MANIFEST.read_text(encoding="utf-8-sig"))
    except (OSError, ValueError):
        return None
    want = mesh_fbx.name.lower()
    for row in reversed(rows):
        if row.get("mode") != "img3d" or row.get("prompt_id") != prompt_id:
            continue
        for path in (row.get("downloaded") or []) + [row.get("output_path") or ""]:
            if path and Path(path).name.lower() == want:
                return row.get("task_id")
    return None


def task_id_from(text: str) -> str | None:
    for line in text.splitlines():
        line = line.strip()
        if len(line) == 36 and line.count("-") == 4:
            return line
    return None


def process(entry: dict, args) -> dict:
    name = entry["name"]
    kind = entry["kind"]
    batch = entry.get("batch") or f"P64{name}"
    out_dir = GEN / batch
    ref = Path(entry["ref"])
    if not ref.is_absolute():
        ref = ROOT / ref

    result = {
        "name": name,
        "kind": kind,
        "prompt_id": args.prompt_id,
        "credits": 0,
        "status": "pending",
        "notes": [],
    }

    if not ref.is_file():
        result.update(status="skipped", notes=[f"reference missing: {ref}"])
        return result

    mesh_fbx = out_dir / f"{name}_Meshy.fbx"
    final_fbx = out_dir / (f"{name}_Static.fbx" if kind == STATIC_KIND
                           else f"{name}_Rigged.fbx")

    if final_fbx.exists():
        result.update(status="already-done", notes=[str(final_fbx)])
        return result

    if args.dry_run:
        band, poly = BAND[kind]
        donor = Path(entry["donor"]).stem if entry.get("donor") else DONORS.get(kind, Path("?")).stem
        rig = ("meshy-rig" if kind == "biped"
               else "none (static)" if kind == STATIC_KIND
               else f"donor-graft({donor})")
        result.update(status="dry-run", notes=[f"band={band} poly={poly} rig={rig}"])
        return result

    # ---- 1. image-to-3D ----------------------------------------------------
    if not mesh_fbx.exists():
        band, poly = BAND[kind]
        cmd = [sys.executable, str(MESHY), "img3d", "--image", str(ref),
               "--band", band, "--target-polycount", str(poly),
               "--batch", batch, "--prompt-id", args.prompt_id, "--name", f"{name}_Meshy",
               "--prompt", entry.get("prompt", name)]
        if kind == "biped":
            cmd.append("--riggable")
        code, out = run(cmd, f"img3d {name}")
        result["credits"] += credits_from(out)
        if code != 0 or not mesh_fbx.exists():
            low = out.lower()
            if any(w in low for w in ("quota", "credit", "insufficient", "402", "403", "429")):
                result.update(status="out-of-credits", notes=[out.strip()[-400:]])
            else:
                result.update(status="img3d-failed", notes=[out.strip()[-400:]])
            return result
        result["img3d_task"] = task_id_from(out)

    # ---- 2. rig ------------------------------------------------------------
    if kind == STATIC_KIND:
        # No skeleton, by decision rather than by failure. Copy the generation
        # output to the shipping name so the staging table and PackStaging see a
        # normal model — `_Meshy` is filtered as an intermediate, so the mesh has
        # to leave that name behind to reach the game at all.
        shutil.copy2(mesh_fbx, final_fbx)
        result["notes"].append("static: no donor skeleton fits this body; motion is procedural")
    elif kind == "biped":
        # The rigging endpoint will NOT take an FBX: it answers
        # "HTTP 400 the input model is empty or not a valid GLB". Feed it the
        # img3d TASK ID instead — no re-upload, and it is what the pilot used.
        # The id is recoverable from the provenance manifest even on a resumed run
        # where this process never saw the generation happen.
        task = result.get("img3d_task") or lookup_img3d_task(mesh_fbx, args.prompt_id)
        if not task:
            result.update(status="rig-no-task",
                          notes=[f"no img3d task id for {mesh_fbx.name}; "
                                 f"cannot rig without re-uploading a GLB"])
            return result
        cmd = [sys.executable, str(MESHY), "rig", "--input-task-id", task,
               "--height", str(entry.get("height", 1.7)),
               "--batch", batch, "--prompt-id", args.prompt_id, "--name", f"{name}_Rigged"]
        code, out = run(cmd, f"rig {name}")
        result["credits"] += credits_from(out)
        if code != 0 or not final_fbx.exists():
            result.update(status="rig-failed", notes=[out.strip()[-400:]])
            return result
    else:
        donor = Path(entry["donor"]) if entry.get("donor") else DONORS[kind]
        if not donor.is_absolute():
            donor = ROOT / donor
        fit = entry.get("fit") or GRAFT_FIT.get(kind, "length")
        # NO bare "--" here. Dot-invoking the script accepts it, but through
        # `powershell -File` the binder sees "--" as a parameter name and dies with
        # "the parameter name '' is ambiguous" before the script ever runs — which
        # silently failed every graft in the first batch while img3d kept spending.
        # blender.ps1 strips "--" from ScriptArgs and supplies its own anyway.
        cmd = ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(BLENDER_PS1),
               "-Script", r"tools\blender\p64_donor_rig.py",
               "--target", str(mesh_fbx), "--donor", str(donor),
               "--output", str(final_fbx), "--fit", fit]
        if entry.get("yaw"):
            cmd += ["--yaw", str(entry["yaw"])]
        code, out = run(cmd, f"graft {name} <- {donor.stem}")
        if code != 0 or not final_fbx.exists():
            result.update(status="graft-failed", notes=[out.strip()[-500:]])
            return result
        for line in out.splitlines():
            if "P64_GRAFT_VERIFY" in line:
                result["notes"].append(line.strip())

    result.update(status="ok")
    result["output"] = str(final_fbx.relative_to(ROOT))
    return result


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    p = argparse.ArgumentParser()
    p.add_argument("--manifest", required=True)
    p.add_argument("--only", default="", help="comma-separated names to run")
    p.add_argument("--dry-run", action="store_true")
    p.add_argument("--prompt-id", default="LATTICE_P3",
                   help="provenance prompt ID passed to Meshy (default: LATTICE_P3)")
    p.add_argument("--report", default=None,
                   help="result ledger path (default: batch_results.json beside manifest)")
    p.add_argument("--stop-on-empty", action=argparse.BooleanOptionalAction, default=True,
                   help="halt the run when Meshy reports the balance exhausted")
    return p.parse_args(argv)


def resolve_report_path(manifest: Path, report: str | None) -> Path:
    """Return an explicit ledger path or preserve P64's sibling default."""
    return Path(report) if report else manifest.with_name("batch_results.json")


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)

    manifest = Path(args.manifest)
    entries = json.loads(manifest.read_text(encoding="utf-8"))
    if args.only:
        wanted = {n.strip().lower() for n in args.only.split(",")}
        entries = [e for e in entries if e["name"].lower() in wanted]

    if not entries:
        raise SystemExit("FAILED: empty generation batch; no art gate can pass")
    print(f"LATTICE_BATCH entries={len(entries)} dry_run={args.dry_run}")
    results, spent = [], 0
    for index, entry in enumerate(entries, 1):
        print(f"[{index}/{len(entries)}] {entry['name']} ({entry['kind']})", flush=True)
        res = process(entry, args)
        spent += res["credits"]
        results.append(res)
        print(f"    -> {res['status']} credits={res['credits']} (run total {spent})", flush=True)
        if res["status"] == "out-of-credits" and args.stop_on_empty:
            print("LATTICE_BATCH_HALT balance exhausted — remaining rows are ART_PENDING; "
                  "completed entries are skipped automatically.")
            break
        if not args.dry_run:
            time.sleep(1)

    report = resolve_report_path(manifest, args.report)
    report.parent.mkdir(parents=True, exist_ok=True)
    report.write_text(json.dumps(results, indent=1) + "\n", encoding="utf-8")

    ok = [r for r in results if r["status"] in ("ok", "already-done")]
    print(f"\nLATTICE_BATCH_DONE ok={len(ok)}/{len(results)} estimated_credits={spent} report={report}")
    for r in results:
        if r["status"] not in ("ok", "already-done", "dry-run"):
            print(f"  !! {r['name']}: {r['status']} {r['notes'][:1]}")
    return 0 if all(r["status"] in ("ok", "already-done", "dry-run") for r in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
