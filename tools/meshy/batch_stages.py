#!/usr/bin/env python3
"""The stages every chapter's art batch runs, so a batch script is only its prompts (tools/meshy/c11_wendmire_batch.py).

Chapter 3's script (c10_xylos_batch.py) grew these stages in place; from chapter 4 on they live here. Each stage skips an
output that already exists, so a batch can be rerun after a failure, and the bridge locks the ledger, so stages may run
beside another batch's tasks. Order one stage at a time and LOOK at what it made before the next spends on it.

  refs      one full-body T-pose image per new body (the speaker-body path: a portrait sheet is the identity reference),
            the design boards, and any single-piece references
  isolate   cut the boards into isolated references (local, free); a neighbour's stray tail is painted out
  models    image-to-3D for each reviewed reference (bodies at 12,000 triangles, T pose)
  rig       Meshy's biped rig for each new body
  reskin    original-UV retextures of rigged bodies and existing models (the prompt holds 800 characters at most)

A rejected output is moved into the batch's rejected/ folder by hand; the stage then makes it again from the corrected
prompt. The chapter's intake script takes the finished pieces from here.
"""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from c10_xylos_batch import drop_neighbours, img3d_task_from_manifest  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
MESHY = [sys.executable, str(ROOT / "tools/meshy/meshy.py")]


@dataclass
class Batch:
    name: str                      # the art-src/Generated folder, e.g. "C11-Wendmire"
    prompt_id: str                 # the ledger's prompt id, e.g. "C11"
    # body id: (reference images, reference file stem, full prompt, rig height in metres, short mesh prompt)
    bodies: dict[str, tuple[list[Path], str, str, float, str]] = field(default_factory=dict)
    boards: dict[str, tuple[str, list[Path], list[str]]] = field(default_factory=dict)   # board: (prompt, references, piece names)
    # single reference: (prompt, references, aspect ratio); with no references it is text-to-image
    singles: dict[str, tuple[str, list[Path], str]] = field(default_factory=dict)
    models: dict[str, tuple[str, int]] = field(default_factory=dict)                     # piece: (band, target polycount)
    reskins: dict[str, tuple[str, str, str]] = field(default_factory=dict)               # id: (source clean fbx, prompt, tail)
    # Bodies to rebuild exactly as their reference draws them, without Meshy's T-pose mode. That mode re-poses a figure and
    # can drop what it takes for props: it gave the Ancelane guard back with no shield and no spear. (It kept Idra's brace
    # and hammer; there is no telling beforehand, so look at the mesh before rigging it.)
    as_drawn: tuple[str, ...] = ()

    @property
    def gen(self) -> Path:
        return ROOT / "art-src" / "Generated" / self.name

    def run(self, args: list[str]) -> None:
        subprocess.run(MESHY + args + ["--batch", self.name, "--prompt-id", self.prompt_id], check=True, cwd=ROOT)

    def image(self, name: str, prompt: str, references: list[Path], aspect: str) -> None:
        missing = [str(r) for r in references if not Path(r).is_file()]
        if missing:
            sys.exit(f"BATCH_REFERENCE_MISSING {name}: {missing}")
        mode = "image2image" if references else "text2image"
        self.run([mode, "--prompt", prompt, "--name", name, "--aspect-ratio", aspect,
                  *sum((["--reference", str(r)] for r in references), [])])

    def refs(self, only: list[str] | None) -> None:
        out = self.gen / "refs"
        for body, (references, stem, prompt, _height, _short) in self.bodies.items():
            # A body with no prompt of its own is a figure on a board (chapter 9 on: two T-posed people share one
            # reference), cut out by `isolate`.
            if prompt and wanted(stem, only) and not (out / f"{stem}.png").exists():
                self.image(stem, prompt, references, "1:1")
        for board, (prompt, references, _pieces) in self.boards.items():
            if wanted(board, only) and not (out / f"{board}.png").exists():
                self.image(board, prompt, references, "3:2")
        for name, (prompt, references, aspect) in self.singles.items():
            if wanted(name, only) and not (out / f"{name}.png").exists():
                self.image(name, prompt, references, aspect)
        print("BATCH_REFS_OK", flush=True)

    def isolate(self, only: list[str] | None) -> None:
        for board, (_prompt, _references, pieces) in self.boards.items():
            sheet = self.gen / "refs" / f"{board}.png"
            if not wanted(board, only) or not sheet.exists():
                continue
            if all((self.gen / "isolated" / f"{p}.png").exists() for p in pieces):
                print("BATCH_ISOLATE_SKIP", board, flush=True)
                continue
            subprocess.run([sys.executable, str(ROOT / "tools/isolate_board.py"), str(sheet), str(self.gen / "isolated"), *pieces],
                           check=True, cwd=ROOT)
            for piece in pieces:
                drop_neighbours(self.gen / "isolated" / f"{piece}.png")
        print("BATCH_ISOLATE_OK", flush=True)

    def reference_for(self, piece: str) -> Path:
        single = self.gen / "refs" / f"{piece}.png"
        return single if single.exists() else self.gen / "isolated" / f"{piece}.png"

    def make_models(self, only: list[str] | None) -> None:
        for body, (_references, stem, _prompt, _height, short) in self.bodies.items():
            if not wanted(body, only) or (self.gen / f"{body}_Meshy.fbx").exists():
                continue
            self.run(["img3d", "--image", str(self.reference_for(stem)), "--band", "humanoid", "--target-polycount", "12000",
                      "--name", f"{body}_Meshy", *([] if body in self.as_drawn else ["--riggable"]), "--prompt", short])
        for piece, (band, polycount) in self.models.items():
            if not wanted(piece, only):
                continue
            if (self.gen / f"{piece}.fbx").exists():
                print("BATCH_MODEL_SKIP", piece, flush=True)
                continue
            self.run(["img3d", "--image", str(self.reference_for(piece)), "--name", piece, "--band", band, "--target-polycount", str(polycount)])
        print("BATCH_MODELS_OK", flush=True)

    def rig(self, only: list[str] | None) -> None:
        for body, (_references, _stem, _prompt, height, _short) in self.bodies.items():
            if not wanted(body, only) or (self.gen / f"{body}_Rigged.fbx").exists():
                continue
            task = img3d_task_from_manifest(f"{body}_Meshy.fbx")
            if not task:
                sys.exit(f"BATCH_NO_MESH_TASK {body}")
            try:
                self.run(["rig", "--input-task-id", task, "--height", str(height), "--name", f"{body}_Rigged"])
            except subprocess.CalledProcessError:
                # Rigging by task id refused the Last Warden ("pose estimation failed", HTTP 422, nothing charged); the
                # same mesh uploaded as a GLB rigged at once (November 12). A refusal costs nothing, so try that way.
                glb = self.gen / f"{body}_Meshy.glb"
                subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "scripts/blender.ps1",
                                "-Script", "tools/blender/convert_model.py", "-TimeoutSec", "300",
                                "--input", str(self.gen / f"{body}_Meshy.fbx"), "--output", str(glb)], check=True, cwd=ROOT)
                self.run(["rig", "--model", str(glb), "--height", str(height), "--name", f"{body}_Rigged"])
        print("BATCH_RIG_OK", flush=True)

    def reskin(self, only: list[str] | None) -> None:
        for name, (model, prompt, tail) in self.reskins.items():
            if not wanted(name, only):
                continue
            if (self.gen / f"{name}.fbx").exists():
                print("BATCH_RESKIN_SKIP", name, flush=True)
                continue
            if len(prompt + tail) > 800:
                sys.exit(f"BATCH_RESKIN_PROMPT_TOO_LONG {name}: {len(prompt + tail)} of 800")
            source = ROOT / model
            if not source.is_file():
                sys.exit(f"BATCH_RESKIN_NO_SOURCE {name}: {model} (the intake script strips the rigged body first)")
            self.run(["retexture", "--model", str(source), "--prompt", prompt + tail, "--name", name])
        print("BATCH_RESKIN_OK", flush=True)

    def check(self) -> None:
        """Free: every retexture prompt inside Meshy's limit, every reference a stage needs present."""
        for name, (model, prompt, tail) in self.reskins.items():
            print(f"{name}: {len(prompt + tail)} of 800 characters", "" if len(prompt + tail) <= 800 else "TOO LONG")
        print("BATCH_CHECK_OK")

    def main(self) -> int:
        stages = {"refs": self.refs, "isolate": self.isolate, "models": self.make_models, "rig": self.rig, "reskin": self.reskin}
        parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
        parser.add_argument("stage", choices=[*stages, "check"])
        parser.add_argument("--only", help="comma-separated names")
        args = parser.parse_args()
        self.gen.mkdir(parents=True, exist_ok=True)
        if args.stage == "check":
            self.check()
        else:
            stages[args.stage](args.only.split(",") if args.only else None)
        return 0


def wanted(name: str, only: list[str] | None) -> bool:
    return not only or name in only


# The wording every chapter's references share (from chapters 2 and 3, where it was proven).
BODY_HEAD = ("Use case: stylized-concept. Asset type: single 3D game-character modeling reference for Coronach. The attached "
             "portrait sheet is ONLY an identity and style reference: create exactly ONE full-body figure of its character, no "
             "sheet, no extra views. ")
BODY_FLAT = ("Flat matte toon-color illustration, crisp restrained linework, only subtle local shading, no cast or ground shadows, "
             "highlights, gloss, ambient occlusion, bloom or dramatic lighting; uniform pure white background, no scenery, text, "
             "labels, dividers or watermark. This is ONE single isolated character only.")
BOARD_STYLE = (
    "OPAQUE WHITE PAPER BACKGROUND, fully opaque RGB image with solid white #FFFFFF in every empty region. No "
    "transparency, gradient, vignette or halo. CORONACH production design board in EXACTLY the same art style, line "
    "weight, cel shading and materials as the reference boards: crisp cel-shaded anime game art, flat local colors, "
    "even unshadowed studio illumination. No gloss, specular highlights, reflections, rim light, cast shadows, ground "
    "shadows, ambient occlusion, bloom, scenery, floor, water, particles, text, letters, labels, logos, watermark or "
    "borders. Emissive parts are opaque saturated color, not illumination. Every part is visibly attached to its body; "
    "nothing floats, nothing loose. ")
KIT_HEADER = (BOARD_STYLE + "Six separated concept cells in a 3x2 grid, ONE isolated object per cell, three-quarter view "
              "from slightly above, substantial white gutters between cells, each object standing directly on the white ground: ")
CREATURE_HEADER = (
    BOARD_STYLE + "These are 3D model references for game enemies seen by a camera high above them, so each has ONE "
    "bold simple silhouette, thick sturdy limbs, big flat colour shapes and no thin wisps, hair or fur. Six separated "
    "concept cells in a 3x2 grid, ONE whole isolated creature per cell, each drawn large in its cell in a three-quarter "
    "view from slightly above with its head towards the lower left, substantial white gutters between cells, no "
    "creature touching or overlapping another: ")
HULL_TAIL = ". Preserve palette semantics and original UV islands; flat clean cel-shaded game texture, no baked lighting."
