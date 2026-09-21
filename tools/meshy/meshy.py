#!/usr/bin/env python3
"""Meshy REST bridge for Lattice's reproducible generative-art pipeline.

All generated files stay in art-src/Generated/<batch> until Unity's
PackStaging imports them.  Never copy a generated model directly into Assets.
"""

from __future__ import annotations

import argparse
import base64
import json
import mimetypes
import os
import sys
import time
import hashlib
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import requests


ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "docs" / "art" / "gen-manifest.json"
API = "https://api.meshy.ai"
ENDPOINTS = {
    "text3d": "/openapi/v2/text-to-3d",
    "img3d": "/openapi/v1/image-to-3d",
    "retexture": "/openapi/v1/retexture",
    "rig": "/openapi/v1/rigging",
}
# Smart-topology (meshy-t2) generates directly at target_polycount, hard cap 15,000.
POLY_BANDS = {"prop": (1_000, 6_000), "hero": (6_000, 15_000), "humanoid": (8_000, 15_000)}
HOUSE_TEXTURE_PROMPT = (
    "Lattice lit-diorama low-poly game art; flat, clean color shapes with soft "
    "hand-painted gradients; no photorealism; no baked lighting, shadows, or ambient "
    "occlusion in the albedo; saturated colors constrained to the named region palette; "
    "simple readable material separation and broad details that survive a 47-degree "
    "three-quarter gameplay camera; compatible with the Quaternius low-poly kit."
)


class MeshyError(RuntimeError):
    pass


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def load_key() -> str:
    key = os.environ.get("MESHY_API_KEY", "").strip()
    if not key:
        env_file = Path(__file__).with_name(".env")
        if env_file.exists():
            for raw in env_file.read_text(encoding="utf-8").splitlines():
                if raw.strip().startswith("MESHY_API_KEY="):
                    key = raw.split("=", 1)[1].strip().strip('"').strip("'")
                    break
    if not key or key == "PASTE_YOUR_MESHY_API_KEY_HERE":
        raise MeshyError(
            "MESHY_API_KEY is absent. Set it in the environment or tools/meshy/.env, "
            "then ask Nathan before continuing."
        )
    return key


def read_manifest() -> list[dict[str, Any]]:
    if not MANIFEST.exists():
        return []
    value = json.loads(MANIFEST.read_text(encoding="utf-8-sig"))
    if not isinstance(value, list):
        raise MeshyError(f"Generation manifest must be a JSON array: {MANIFEST}")
    return value


def write_manifest(rows: list[dict[str, Any]]) -> None:
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    tmp = MANIFEST.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(rows, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    tmp.replace(MANIFEST)


def append_manifest(row: dict[str, Any]) -> None:
    rows = read_manifest()
    rows.append(row)
    write_manifest(rows)


def update_manifest(task_id: str, **values: Any) -> None:
    rows = read_manifest()
    for row in reversed(rows):
        if row.get("task_id") == task_id:
            row.update({k: v for k, v in values.items() if v is not None})
            row["updated_at"] = utc_now()
            write_manifest(rows)
            return


def data_uri(path_text: str) -> str:
    path = Path(path_text).expanduser().resolve()
    if not path.is_file():
        raise MeshyError(f"Local input not found: {path}")
    mime = mimetypes.guess_type(path.name)[0] or "application/octet-stream"
    payload = base64.b64encode(path.read_bytes()).decode("ascii")
    return f"data:{mime};base64,{payload}"


def task_id_from(response: dict[str, Any]) -> str:
    task_id = response.get("result") or response.get("id") or response.get("task_id")
    if isinstance(task_id, dict):
        task_id = task_id.get("id") or task_id.get("task_id")
    if not task_id:
        raise MeshyError(f"Meshy response contained no task id: {response}")
    return str(task_id)


class Client:
    def __init__(self, key: str):
        self.session = requests.Session()
        self.session.headers.update({"Authorization": f"Bearer {key}", "Content-Type": "application/json"})

    def request(self, method: str, path: str, **kwargs: Any) -> dict[str, Any]:
        try:
            response = self.session.request(method, API + path, timeout=120, **kwargs)
        except requests.RequestException as exc:
            raise MeshyError(f"Meshy request failed: {exc}") from exc
        if response.ok:
            return response.json()
        message = response.text[:1200]
        try:
            body = response.json()
            message = body.get("message") or body.get("error") or message
        except ValueError:
            pass
        quota_words = ("quota", "credit", "plan", "subscription", "insufficient")
        if response.status_code in (402, 403, 429) or any(w in str(message).lower() for w in quota_words):
            raise MeshyError(
                f"Meshy API quota/plan gate (HTTP {response.status_code}): {message}\n"
                "Document this result, then use browser-driven meshy.ai and record the same prompt, "
                "parameters, credits, downloaded output, and staging path in gen-manifest.json."
            )
        raise MeshyError(f"Meshy HTTP {response.status_code}: {message}")

    def create(self, mode: str, payload: dict[str, Any]) -> str:
        return task_id_from(self.request("POST", ENDPOINTS[mode], json=payload))

    def get(self, mode: str, task_id: str) -> dict[str, Any]:
        return self.request("GET", f"{ENDPOINTS[mode]}/{task_id}")


def band_values(args: argparse.Namespace) -> tuple[int, int, int]:
    low, high = POLY_BANDS[args.band]
    target = args.target_polycount if args.target_polycount is not None else high
    if target < low or target > high:
        raise MeshyError(f"target polycount {target} is outside the {args.band} band ({low}-{high} tris)")
    return low, high, target


def task_row(task_id: str, mode: str, prompt: str, params: dict[str, Any], batch: str,
             prompt_id: str) -> dict[str, Any]:
    return {
        "task": f"{prompt_id.lower()}-{mode}-{task_id}",
        "task_id": task_id,
        "prompt_id": prompt_id,
        "tool": "Meshy API",
        "mode": mode,
        "prompt": prompt,
        "params": manifest_safe(params),
        "credits": None,
        "consumed_credits": None,
        "output_path": None,
        "batch": batch,
        "created_at": utc_now(),
    }


def manifest_safe(value: Any) -> Any:
    """Keep data-URI payload bytes out of the checked-in provenance ledger."""
    if isinstance(value, dict):
        return {k: manifest_safe(v) for k, v in value.items()}
    if isinstance(value, list):
        return [manifest_safe(v) for v in value]
    if isinstance(value, str) and value.startswith("data:") and ";base64," in value:
        header, encoded = value.split(",", 1)
        digest = hashlib.sha256(encoded.encode("ascii")).hexdigest()[:16]
        return f"{header},<omitted chars={len(encoded)} sha256(base64)={digest}>"
    return value


def wait_for(client: Client, mode: str, task_id: str, interval: float, timeout: float) -> dict[str, Any]:
    deadline = time.monotonic() + timeout
    while True:
        task = client.get(mode, task_id)
        status = str(task.get("status", "UNKNOWN")).upper()
        credits = task.get("consumed_credits")
        update_manifest(task_id, status=status, credits=credits, consumed_credits=credits)
        print(f"{task_id} {status}" + (f" credits={credits}" if credits is not None else ""), flush=True)
        if status == "SUCCEEDED":
            return task
        if status in {"FAILED", "CANCELED", "CANCELLED", "EXPIRED"}:
            raise MeshyError(f"Meshy task {task_id} ended {status}: {task.get('task_error') or task}")
        if time.monotonic() >= deadline:
            raise MeshyError(f"Timed out waiting for Meshy task {task_id}")
        time.sleep(interval)


def cache_dir(batch: str) -> Path:
    safe = "".join(c for c in batch if c.isalnum() or c in "-_" ).strip("-")
    if not safe:
        raise MeshyError("Batch name contains no safe characters")
    path = ROOT / "art-src" / "Generated" / safe
    path.mkdir(parents=True, exist_ok=True)
    marker = path / "unity-stage.json"
    if not marker.exists():
        marker.write_text(json.dumps({"batch": safe, "gate": "PackStaging"}, indent=2) + "\n", encoding="utf-8")
    return path


def download_task(task: dict[str, Any], task_id: str, batch: str, name: str | None) -> list[Path]:
    urls = task.get("model_urls") or {}
    if not isinstance(urls, dict):
        urls = {}
    fbx_url = urls.get("fbx")
    rig_urls = task.get("result") if isinstance(task.get("result"), dict) else {}
    if not fbx_url:
        fbx_url = rig_urls.get("rigged_character_fbx_url")
    if not fbx_url:
        raise MeshyError(f"Task {task_id} has no FBX output. Use Blender convert.py on another cached format: {urls}")
    stem = name or task_id
    out = cache_dir(batch) / f"{stem}.fbx"
    response = requests.get(fbx_url, timeout=300)
    response.raise_for_status()
    out.write_bytes(response.content)
    downloaded = [out]
    animation_urls = rig_urls.get("basic_animations") if isinstance(rig_urls, dict) else None
    if isinstance(animation_urls, dict):
        rig_urls = {**rig_urls, **animation_urls}
    for key, value in rig_urls.items():
        if key == "rigged_character_fbx_url" or not key.endswith("_fbx_url"):
            continue
        if not isinstance(value, str) or not value.startswith("http"):
            continue
        rig_out = out.with_name(f"{stem}_{key.removesuffix('_url')}.fbx")
        rig_response = requests.get(value, timeout=300)
        rig_response.raise_for_status()
        rig_out.write_bytes(rig_response.content)
        downloaded.append(rig_out)
    textures = task.get("texture_urls") or []
    if isinstance(textures, dict):
        textures = [textures]
    for index, texture_set in enumerate(textures):
        if not isinstance(texture_set, dict):
            continue
        for key, value in texture_set.items():
            if not isinstance(value, str) or not value.startswith("http"):
                continue
            suffix = Path(value.split("?", 1)[0]).suffix or ".png"
            label = f"{key}_{index}" if len(textures) > 1 else key
            tex_out = out.with_name(f"{stem}_{label}{suffix}")
            tex_response = requests.get(value, timeout=300)
            tex_response.raise_for_status()
            tex_out.write_bytes(tex_response.content)
            downloaded.append(tex_out)
    rel = out.relative_to(ROOT).as_posix()
    update_manifest(task_id, output_path=rel, downloaded=[p.relative_to(ROOT).as_posix() for p in downloaded])
    print(f"Cached {out}")
    print("Unity entry gate: run PackStaging.StagePacks; never copy this file directly into Assets.")
    return downloaded


def create_record(client: Client, api_mode: str, manifest_mode: str, payload: dict[str, Any], prompt: str,
                  batch: str, prompt_id: str) -> str:
    approval = ROOT / "docs" / "art" / "species-approval.json"
    if not approval.exists() or json.loads(approval.read_text(encoding="utf-8")).get("status") != "NATHAN_SPECIES_APPROVED":
        raise MeshyError("NATHAN GATE: species approval receipt is required before starting the ordered art batch")
    reserves = {"rig": 10, "retexture": 20, "img3d": 50, "text3d": 50}
    used = sum(row.get("consumed_credits") if row.get("consumed_credits") is not None
               else row.get("credits") if row.get("credits") is not None
               else reserves.get(row.get("mode"), 50) for row in read_manifest())
    if used + reserves.get(api_mode, 50) > 1200:
        raise MeshyError(f"BUDGET_STOP spent_or_reserved={used} cap=1200; remaining assets become ART_PENDING")
    task_id = client.create(api_mode, payload)
    append_manifest(task_row(task_id, manifest_mode, prompt, payload, batch, prompt_id))
    print(task_id, flush=True)
    return task_id


def cmd_text3d(args: argparse.Namespace, client: Client) -> None:
    low, high, target = band_values(args)
    preview = {
        "mode": "preview", "prompt": args.prompt, "ai_model": args.ai_model,
        "model_type": "smart-topology", "target_polycount": target,
        "pose_mode": "t-pose" if args.riggable else "",
        "target_formats": ["fbx"],
    }
    if not preview["pose_mode"]:
        preview.pop("pose_mode")
    preview_id = create_record(client, "text3d", "text3d-preview", preview, args.prompt, args.batch, args.prompt_id)
    preview_task = wait_for(client, "text3d", preview_id, args.interval, args.timeout)
    texture_prompt = args.texture_prompt or HOUSE_TEXTURE_PROMPT
    refine = {
        "mode": "refine", "preview_task_id": preview_id,
        "texture_prompt": texture_prompt, "enable_pbr": False,
        "remove_lighting": True, "target_formats": ["fbx"],
    }
    refine_id = create_record(client, "text3d", "text3d-refine", refine, texture_prompt, args.batch, args.prompt_id)
    task = wait_for(client, "text3d", refine_id, args.interval, args.timeout)
    update_manifest(refine_id, polycount_band={"min": low, "max": high, "target": target})
    if args.download:
        download_task(task, refine_id, args.batch, args.name)


def build_img3d_payload(args: argparse.Namespace, image_url: str) -> tuple[dict[str, Any], dict[str, int]]:
    """Build the Smart Topology payload accepted by the current Image-to-3D API.

    ``image_enhancement`` and ``remove_lighting`` are Standard-model options.
    Sending either with Meshy T2 now fails the request before task creation, so
    preserve Lattice's high-quality reference verbatim and omit both fields.
    """
    low, high, target = band_values(args)
    payload = {
        "image_url": image_url, "ai_model": args.ai_model,
        "model_type": "smart-topology", "target_polycount": target,
        "pose_mode": "t-pose" if args.riggable else "",
        "target_formats": ["fbx"], "should_texture": True,
    }
    if not payload["pose_mode"]:
        payload.pop("pose_mode")
    return payload, {"min": low, "max": high, "target": target}


def cmd_img3d(args: argparse.Namespace, client: Client) -> None:
    payload, polycount_band = build_img3d_payload(args, data_uri(args.image))
    task_id = create_record(client, "img3d", "img3d", payload, args.prompt or f"image:{Path(args.image).name}", args.batch, args.prompt_id)
    update_manifest(task_id, polycount_band=polycount_band)
    task = wait_for(client, "img3d", task_id, args.interval, args.timeout)
    if args.download:
        download_task(task, task_id, args.batch, args.name)


def cmd_retexture(args: argparse.Namespace, client: Client) -> None:
    prompt = args.prompt or HOUSE_TEXTURE_PROMPT
    payload = {
        "model_url": data_uri(args.model), "text_style_prompt": prompt,
        "enable_original_uv": not args.regenerate_uv,
        "remove_lighting": True, "target_formats": ["fbx"],
    }
    task_id = create_record(client, "retexture", "retexture", payload, prompt, args.batch, args.prompt_id)
    task = wait_for(client, "retexture", task_id, args.interval, args.timeout)
    if args.download:
        download_task(task, task_id, args.batch, args.name)


def cmd_rig(args: argparse.Namespace, client: Client) -> None:
    # The current rigging endpoint always returns FBX and GLB result URLs and does
    # not accept target_formats (unlike generation/retexture endpoints).
    payload: dict[str, Any] = {"height_meters": args.height}
    if args.model:
        payload["model_url"] = data_uri(args.model)
    else:
        payload["input_task_id"] = args.input_task_id
    prompt = "humanoid auto-rig; t-pose source; +Z forward; skeleton-measured scale"
    task_id = create_record(client, "rig", "rig", payload, prompt, args.batch, args.prompt_id)
    task = wait_for(client, "rig", task_id, args.interval, args.timeout)
    if args.download:
        download_task(task, task_id, args.batch, args.name)


def cmd_poll(args: argparse.Namespace, client: Client) -> None:
    task = wait_for(client, args.mode, args.task_id, args.interval, args.timeout) if args.wait else client.get(args.mode, args.task_id)
    update_manifest(args.task_id, status=task.get("status"), credits=task.get("consumed_credits"), consumed_credits=task.get("consumed_credits"))
    print(json.dumps(task, indent=2))


def cmd_download(args: argparse.Namespace, client: Client) -> None:
    task = client.get(args.mode, args.task_id)
    if str(task.get("status", "")).upper() != "SUCCEEDED":
        raise MeshyError(f"Task is not ready: {task.get('status')}")
    download_task(task, args.task_id, args.batch, args.name)


def common_generation(parser: argparse.ArgumentParser, bands: bool = True) -> None:
    parser.add_argument("--batch", default="P30Bakeoff")
    parser.add_argument("--prompt-id", default="P30")
    parser.add_argument("--name")
    parser.add_argument("--download", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--interval", type=float, default=12)
    parser.add_argument("--timeout", type=float, default=3600)
    if bands:
        parser.add_argument("--band", choices=POLY_BANDS, default="prop")
        parser.add_argument("--target-polycount", type=int)


def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser(description=__doc__)
    sub = root.add_subparsers(dest="command", required=True)
    p = sub.add_parser("text3d", help="Create preview, refine it, poll, and optionally download FBX")
    p.add_argument("--prompt", required=True)
    p.add_argument("--texture-prompt")
    p.add_argument("--ai-model", default="meshy-t2")
    p.add_argument("--riggable", action="store_true")
    common_generation(p)
    p.set_defaults(func=cmd_text3d)
    p = sub.add_parser("img3d", help="Create 3D from a local image data URI")
    p.add_argument("--image", required=True)
    p.add_argument("--prompt")
    p.add_argument("--ai-model", default="meshy-t2")
    p.add_argument("--riggable", action="store_true")
    common_generation(p)
    p.set_defaults(func=cmd_img3d)
    p = sub.add_parser("retexture", help="Retexture a local mesh data URI, preserving its UVs by default")
    p.add_argument("--model", required=True)
    p.add_argument("--prompt")
    p.add_argument("--regenerate-uv", action="store_true")
    common_generation(p, bands=False)
    p.set_defaults(func=cmd_retexture)
    p = sub.add_parser("rig", help="Rig a humanoid biped only; quadrupeds are forbidden")
    g = p.add_mutually_exclusive_group(required=True)
    g.add_argument("--model")
    g.add_argument("--input-task-id")
    p.add_argument("--height", type=float, required=True)
    common_generation(p, bands=False)
    p.set_defaults(func=cmd_rig)
    for name, func in (("poll", cmd_poll), ("download", cmd_download)):
        p = sub.add_parser(name)
        p.add_argument("mode", choices=ENDPOINTS)
        p.add_argument("task_id")
        p.add_argument("--batch", default="P30Bakeoff")
        p.add_argument("--name")
        if name == "poll":
            p.add_argument("--wait", action="store_true")
            p.add_argument("--interval", type=float, default=12)
            p.add_argument("--timeout", type=float, default=3600)
        p.set_defaults(func=func)
    return root


def main() -> int:
    try:
        args = parser().parse_args()
        client = Client(load_key())
        args.func(args, client)
        return 0
    except (MeshyError, requests.RequestException, json.JSONDecodeError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
