#!/usr/bin/env python3
"""Neighbourhood Afloat's two boards, painted (chapter 10 look pass, November 21).

The whole-game capture (Builds/quality/nov20-whole/s69/014, 031) showed Orrin's new board as rows of plain white bars and the
message board's notes as blank cards. This paints what is written on them, too small to read from the fixed camera and
plainly writing from it:

  * afloat-chalkboard.png: Orrin's board, slate with chalk dust, three columns under underlined headings (the motions,
    the rotas, the convoy's names) in a hand, tallies, a line struck through, the convoy drawn as beads on a line;
  * afloat-noticeboard.png: the message board's cork, fourteen notes pinned to it at a slant, each in its own paper and
    ink, some with a drawing, every one with its pin;
  * afloat-chalkline.png: the line Orrin chalks up after the handrail, on clear ground (cut out).

  python tools/afloat_boards.py      # writes Lattice/Assets/_Project/Art/Generated/Afloat/afloat-*.png

LastMooringBuilder.Neighbourhood lays them on the boards (and draws the old bars and cards only if they are missing).
Seeded, so a run writes the same pixels.
"""
from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(__file__).resolve().parents[1] / "Lattice/Assets/_Project/Art/Generated/Afloat"


def word(draw: ImageDraw.ImageDraw, x: float, y: float, length: float, height: float, colour, width: int, rng: random.Random):
    """A word in a running hand: a line of loops and strokes along the baseline, a riser or a descender now and then."""
    pts = []
    t = 0.0
    while t < length:
        step = rng.uniform(.35, .7) * height
        h = height * rng.choice([.55, .6, .65, 1.0, .5, .58]) if rng.random() < .85 else -height * .45
        pts.append((x + t, y - h * rng.uniform(.8, 1.05)))
        pts.append((x + t + step * .55, y + rng.uniform(-.06, .08) * height))
        t += step
    if len(pts) > 1:
        draw.line(pts, fill=colour, width=width, joint="curve")
    return x + length


def line_of_words(draw, x, y, length, height, colour, width, rng):
    t = 0.0
    while t < length - height:
        w = min(rng.uniform(1.4, 4.2) * height, length - t)
        word(draw, x + t, y, w, height, colour, width, rng)
        t += w + rng.uniform(.55, .9) * height


def chalkboard(size=(1024, 512)) -> Image.Image:
    rng = random.Random(77)
    w, h = size
    base = Image.new("RGB", size, (34, 46, 42))
    px = base.load()
    for yy in range(h):
        for xx in range(w):
            n = (math.sin(xx * .021 + yy * .013) + math.sin(xx * .007 - yy * .031)) * 2.5 + rng.uniform(-3, 3)
            r, g, b = px[xx, yy]
            px[xx, yy] = (int(r + n), int(g + n), int(b + n))
    # Old chalk wiped and not quite gone: pale clouds.
    dust = Image.new("L", size, 0)
    dd = ImageDraw.Draw(dust)
    for _ in range(26):
        cx, cy, rr = rng.uniform(0, w), rng.uniform(0, h), rng.uniform(30, 120)
        dd.ellipse([cx - rr * 1.6, cy - rr * .5, cx + rr * 1.6, cy + rr * .5], fill=rng.randint(14, 34))
    dust = dust.filter(ImageFilter.GaussianBlur(26))
    base = Image.composite(Image.new("RGB", size, (150, 162, 156)), base, dust)
    ink = Image.new("L", size, 0)
    d = ImageDraw.Draw(ink)
    cols = [(34, 330), (370, 650), (690, 990)]
    for ci, (x0, x1) in enumerate(cols):
        # The heading, bigger, underlined twice.
        word(d, x0 + 10, 62, rng.uniform(150, 210), 30, 235, 5, rng)
        d.line([(x0 + 4, 80), (x1 - 30, 82)], fill=225, width=3)
        d.line([(x0 + 10, 88), (x1 - 60, 89)], fill=170, width=2)
        y = 128
        while y < h - 40:
            if ci == 1 and rng.random() < .22:
                # A rota's tally: groups of five.
                tx = x0 + 14
                for _g in range(rng.randint(2, 4)):
                    for k in range(4):
                        d.line([(tx + k * 9, y - 18), (tx + k * 9 + rng.uniform(-2, 2), y + 2)], fill=220, width=3)
                    d.line([(tx - 4, y - 4), (tx + 34, y - 14)], fill=220, width=3)
                    tx += 52
                word(d, tx + 10, y, max(20, min(rng.uniform(60, 120), x1 - tx - 30)), 15, 215, 3, rng)
            else:
                indent = 26 if rng.random() < .3 else 10
                d.ellipse([x0 + indent - 12, y - 9, x0 + indent - 6, y - 3], fill=220)
                line_of_words(d, x0 + indent, y, rng.uniform(.55, 1) * (x1 - x0 - indent - 20), 16, 225, 3, rng)
                if rng.random() < .12:
                    d.line([(x0 + indent, y - 7), (x0 + indent + rng.uniform(120, 220), y - 9)], fill=230, width=3)
            y += rng.uniform(34, 42)
    # The convoy drawn along the bottom of the names: beads on a line, an arrow at the head.
    yb = h - 30
    d.line([(700, yb), (980, yb - 6)], fill=200, width=3)
    for k in range(11):
        cx = 712 + k * 24
        d.ellipse([cx - 7, yb - 7 - k * .2, cx + 7, yb + 7 - k * .2], outline=230, width=3)
    d.polygon([(980, yb - 6), (964, yb - 16), (966, yb + 4)], fill=230)
    ink = ink.filter(ImageFilter.GaussianBlur(.6))
    grain = Image.effect_noise(size, 60).point(lambda v: 255 if v > 92 else int(v * 2.2))
    ink = Image.composite(ink, Image.new("L", size, 0), grain)
    chalk = Image.new("RGB", size, (238, 240, 232))
    return Image.composite(chalk, base, ink)


def noticeboard(size=(1024, 256)) -> Image.Image:
    rng = random.Random(91)
    w, h = size
    cork = Image.new("RGB", size, (168, 120, 78))
    px = cork.load()
    for yy in range(h):
        for xx in range(w):
            v = rng.randint(-22, 18)
            r, g, b = px[xx, yy]
            px[xx, yy] = (r + v, g + int(v * .85), b + int(v * .6))
    cork = cork.filter(ImageFilter.GaussianBlur(.7))
    papers = [(244, 240, 226), (236, 230, 206), (226, 236, 244), (246, 236, 190), (232, 222, 236), (240, 242, 238), (222, 236, 214)]
    inks = [(40, 44, 70), (30, 30, 34), (70, 40, 40), (30, 60, 70)]
    # On a jittered row, so they spread across the cork as people pin things, not in heaps.
    xs = [12 + i * (w - 140) / 13 + rng.uniform(-14, 14) for i in range(14)]
    for i, x in enumerate(xs):
        pw, ph = rng.randint(70, 130), rng.randint(90, 180)
        y = rng.uniform(12, h - ph - 10)
        sheet = Image.new("RGBA", (pw, ph), papers[i % len(papers)] + (255,))
        sd = ImageDraw.Draw(sheet)
        ink = inks[rng.randrange(len(inks))]
        yy = 18
        if rng.random() < .6:
            word(sd, 8, yy, rng.uniform(.4, .8) * pw, 10, ink, 2, rng)
            sd.line([(8, yy + 4), (pw - 14, yy + 4)], fill=ink, width=1)
            yy += 18
        if i % 5 == 2:
            # A drawing: a valve and its pipes, or a little plan.
            cx, cy = pw * .5, yy + 30
            sd.ellipse([cx - 16, cy - 16, cx + 16, cy + 16], outline=ink, width=2)
            sd.line([(cx - 34, cy), (cx - 16, cy)], fill=ink, width=2)
            sd.line([(cx + 16, cy), (cx + 34, cy)], fill=ink, width=2)
            sd.line([(cx, cy - 16), (cx, cy - 28)], fill=ink, width=2)
            sd.line([(cx - 8, cy - 28), (cx + 8, cy - 28)], fill=ink, width=2)
            yy += 64
        while yy < ph - 10:
            sd2 = sd
            t = 8.0
            ln = rng.uniform(.5, .95) * (pw - 16)
            while t < ln:
                wl = min(rng.uniform(12, 34), ln - t)
                word(sd2, t, yy, wl, 6, ink, 1, rng)
                t += wl + 5
            yy += rng.uniform(10, 13)
        angle = rng.uniform(-7, 7)
        rot = sheet.rotate(angle, resample=Image.BICUBIC, expand=True)
        shadow = Image.new("RGBA", rot.size, (0, 0, 0, 0))
        shadow.paste((40, 26, 16, 110), mask=rot.split()[3])
        shadow = shadow.filter(ImageFilter.GaussianBlur(3))
        cork.paste(shadow, (int(x) + 4, int(y) + 5), shadow)
        cork.paste(rot, (int(x), int(y)), rot)
        pin = (int(x + rot.size[0] * .5), int(y + 9))
        pd = ImageDraw.Draw(cork)
        colour = rng.choice([(196, 48, 40), (214, 170, 60), (60, 120, 170), (60, 150, 90)])
        pd.ellipse([pin[0] - 6, pin[1] - 6, pin[0] + 6, pin[1] + 6], fill=colour)
        pd.ellipse([pin[0] - 3, pin[1] - 4, pin[0], pin[1] - 1], fill=(250, 240, 230))
    return cork


def chalkline(size=(512, 64)) -> Image.Image:
    """Orrin's one more line, chalked after the handrail is mended: a line of his hand on clear ground."""
    rng = random.Random(13)
    ink = Image.new("L", size, 0)
    d = ImageDraw.Draw(ink)
    d.ellipse([6, 30, 14, 38], fill=230)
    line_of_words(d, 22, 44, size[0] - 40, 22, 235, 4, rng)
    ink = ink.filter(ImageFilter.GaussianBlur(.6))
    out = Image.new("RGBA", size, (238, 240, 232, 0))
    out.putalpha(ink.point(lambda v: 255 if v > 70 else 0))
    return out


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    chalkboard().save(OUT / "afloat-chalkboard.png")
    noticeboard().save(OUT / "afloat-noticeboard.png")
    chalkline().save(OUT / "afloat-chalkline.png")
    print("AFLOAT_BOARDS_OK", OUT)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
