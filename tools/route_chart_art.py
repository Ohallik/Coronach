"""Paint the field behind Sela's chart (the pause menu's Chart page), free and local.

A dark vellum sheet: mottled and veined, with faint route fibres running west to east the way the story does, a compass of
lantern marks behind the middle of it, a ruled neatline, and a quieter, darker band along the bottom where the page writes
about the stop under the cursor. Everything on it is low in contrast on purpose: the live routes, the lanterns and every
word are drawn over it by code (Lattice.UI.RouteChart), at the pause menu's type floor.

The sheet is 2048 x 1024 (a power of two, so a default texture import doesn't resample it) and is shown at 1220 x 612.
Its edges fade to nothing so it sits in the menu's panel without a hard rectangle. The drawing is seeded: the same sheet
every run.

    python tools/route_chart_art.py                       writes Lattice/Assets/_Project/Resources/UI/Generated/route-chart.png
    python tools/route_chart_art.py --out some/other.png
    python tools/route_chart_art.py --preview out.png     also a 1220 x 612 proof on the panel's colour, with sample lanterns and words

Last line: ROUTE_CHART_ART_OK <path> <width>x<height>.
"""
import argparse
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Lattice" / "Assets" / "_Project" / "Resources" / "UI" / "Generated" / "route-chart.png"
W, H = 2048, 1024
S = 2                      # lines are drawn at twice the size and brought down, so none of them is stepped
BAND = 0.722               # the rule between the lanterns' field and the band the page writes in, from the top
PLATE = (6, 18, 38)        # UiKit.PlateColor, what the menu's panel shows round the sheet
INK = (150, 172, 204)      # ruled lines and the compass: cold, like the panel's frost text
AMBER = (255, 190, 96)     # lantern marks
TEAL = (86, 168, 176)      # the cold routes
GOLD = (214, 176, 112)     # the neatline


def smooth(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0), 0, 1)
    return t * t * (3 - 2 * t)


def blotches(rng, across, down):
    """Soft noise, -1 to 1: a few random cells stretched over the whole sheet."""
    small = rng.standard_normal((down, across)).astype(np.float32)
    image = Image.fromarray(small).resize((W, H), Image.Resampling.BICUBIC)
    field = np.asarray(image, dtype=np.float32)
    return field / max(1e-6, float(np.abs(field).max()))


def layer():
    return Image.new("L", (W * S, H * S), 0)


def down(image, blur=0.0):
    if blur:
        image = image.filter(ImageFilter.GaussianBlur(blur * S))
    return np.asarray(image.resize((W, H), Image.Resampling.LANCZOS), dtype=np.float32) / 255


def bezier(p0, p1, p2, p3, steps=160):
    points = []
    for i in range(steps + 1):
        t = i / steps
        a, b, c, d = (1 - t) ** 3, 3 * t * (1 - t) ** 2, 3 * t * t * (1 - t), t ** 3
        points.append((a * p0[0] + b * p1[0] + c * p2[0] + d * p3[0], a * p0[1] + b * p1[1] + c * p2[1] + d * p3[1]))
    return points


def stroke(draw, points, width, value=255, dash=None):
    """A polyline at the big size. dash=(on, off) in final pixels along the line."""
    scaled = [(x * S, y * S) for x, y in points]
    if not dash:
        draw.line(scaled, fill=value, width=max(1, round(width * S)), joint="curve")
        return
    run, on, piece = 0.0, True, [scaled[0]]
    for a, b in zip(scaled, scaled[1:]):
        run += math.dist(a, b) / S
        piece.append(b)
        if run >= (dash[0] if on else dash[1]):
            if on and len(piece) > 1:
                draw.line(piece, fill=value, width=max(1, round(width * S)), joint="curve")
            run, on, piece = 0.0, not on, [b]
    if on and len(piece) > 1:
        draw.line(piece, fill=value, width=max(1, round(width * S)), joint="curve")


def dot(draw, x, y, radius, value=255):
    draw.ellipse([(x - radius) * S, (y - radius) * S, (x + radius) * S, (y + radius) * S], fill=value)


def vellum(rng, rnd):
    """The sheet itself, as float RGB 0-255: a dark skin, lighter where it's thin, veined and fibrous."""
    ys, xs = np.mgrid[0:H, 0:W].astype(np.float32)
    u, v = xs / (W - 1), ys / (H - 1)
    # Thin toward the middle of the lantern field, thick and dark toward the edges.
    middle = np.sqrt(((u - .5) / .62) ** 2 + ((v - .38) / .56) ** 2)
    light = 1 - .34 * smooth(.25, 1.2, middle)
    mottle = .55 * blotches(rng, 9, 5) + .3 * blotches(rng, 28, 14) + .15 * blotches(rng, 90, 45)
    warm = blotches(rng, 6, 3)                       # where the skin runs a little browner
    value = light * (1 + .24 * mottle)
    sheet = np.stack([(17 + 8 * warm) * value, (23 + 3 * warm) * value, (33 - 7 * warm) * value], axis=-1)
    # Fibres: thousands of short hairs, each barely there.
    hairs = layer()
    draw = ImageDraw.Draw(hairs)
    for _ in range(7000):
        x, y, turn, length = rnd.uniform(0, W), rnd.uniform(0, H), rnd.uniform(0, math.tau), rnd.uniform(10, 46)
        bend = rnd.uniform(-.5, .5)
        points = [(x + math.cos(turn + bend * t) * length * t, y + math.sin(turn + bend * t) * length * t) for t in (0, .33, .66, 1)]
        stroke(draw, points, .8, rnd.randint(40, 150))
    sheet += down(hairs, .4)[..., None] * np.array([17, 17, 18], dtype=np.float32)
    # Veins: a few long wandering lines, as a skin has.
    veins = layer()
    draw = ImageDraw.Draw(veins)
    for _ in range(26):
        x, y, turn = rnd.uniform(-50, W + 50), rnd.uniform(-50, H + 50), rnd.uniform(0, math.tau)
        points = [(x, y)]
        for _ in range(rnd.randint(30, 80)):
            turn += rnd.uniform(-.22, .22)
            x, y = x + math.cos(turn) * 16, y + math.sin(turn) * 16
            points.append((x, y))
        stroke(draw, points, 1.1, rnd.randint(60, 140))
    sheet += down(veins, .8)[..., None] * np.array([13, 14, 16], dtype=np.float32)
    return sheet, u, v


def routes(rnd):
    """Route fibres: bundles of long curves from the west edge to the east, some of them dashed. Cold and warm, each its own mask."""
    cold, warm = layer(), layer()
    for index in range(13):
        draw = ImageDraw.Draw(warm if index % 3 == 0 else cold)
        y0, y3 = rnd.uniform(.04, .7) * H, rnd.uniform(.04, .7) * H
        p0, p3 = (-60, y0), (W + 60, y3)
        p1 = (W * rnd.uniform(.2, .4), y0 + rnd.uniform(-.34, .34) * H)
        p2 = (W * rnd.uniform(.6, .8), y3 + rnd.uniform(-.34, .34) * H)
        strands = rnd.randint(1, 3)
        for strand in range(strands):
            shift = (strand - (strands - 1) / 2) * rnd.uniform(7, 13)
            line = [(x, y + shift * (.6 + .4 * math.sin(x / 190 + index))) for x, y in bezier(p0, p1, p2, p3)]
            dashed = rnd.random() < .4
            stroke(draw, line, 1.5 if strand == 0 else 1.1, 255 if strand == 0 else 170, dash=(rnd.uniform(14, 26), rnd.uniform(7, 12)) if dashed else None)
    return cold, warm


def compass(rnd, cx, cy, radius):
    """A compass rose whose points are lanterns: rings, a graduated circle, eight spokes, and sixteen lantern marks on the outer ring."""
    lines, lamps = layer(), layer()
    draw, lit = ImageDraw.Draw(lines), ImageDraw.Draw(lamps)

    def ring(r, width, value=255, dash=None):
        stroke(draw, [(cx + math.cos(a / 240 * math.tau) * r, cy + math.sin(a / 240 * math.tau) * r) for a in range(241)], width, value, dash)

    ring(radius, 1.6)
    ring(radius + 16, 1.0, 190)
    ring(radius * .66, 1.1, 170, dash=(9, 7))
    ring(radius * .34, 1.2, 210)
    for step in range(72):                                   # the graduated circle: every 5 degrees, longer every 15, longest every 45
        turn = step / 72 * math.tau
        reach = 16 if step % 9 == 0 else 11 if step % 3 == 0 else 6
        stroke(draw, [(cx + math.cos(turn) * radius, cy + math.sin(turn) * radius), (cx + math.cos(turn) * (radius - reach), cy + math.sin(turn) * (radius - reach))], 1.2, 230)
    for point in range(8):                                   # the needles: long and thin on the four winds, shorter between
        turn = point / 8 * math.tau - math.pi / 2
        long = point % 2 == 0
        tip, waist, side = radius * (1.06 if long else .9), radius * .34, radius * (.05 if long else .035)
        across = (math.cos(turn + math.pi / 2), math.sin(turn + math.pi / 2))
        shape = [(cx + math.cos(turn) * tip, cy + math.sin(turn) * tip), (cx + math.cos(turn) * waist + across[0] * side, cy + math.sin(turn) * waist + across[1] * side),
                 (cx, cy), (cx + math.cos(turn) * waist - across[0] * side, cy + math.sin(turn) * waist - across[1] * side)]
        draw.polygon([(x * S, y * S) for x, y in shape], outline=255 if long else 190, width=round(1.2 * S))
        stroke(draw, [(cx, cy), shape[0]], .9, 150)
    for mark in range(16):                                   # the lantern marks
        turn = mark / 16 * math.tau - math.pi / 2
        x, y = cx + math.cos(turn) * (radius + 16), cy + math.sin(turn) * (radius + 16)
        dot(lit, x, y, 5.5 if mark % 4 == 0 else 4 if mark % 2 == 0 else 2.6)
    dot(lit, cx, cy, 4.5)
    return lines, lamps


def frame(u, v):
    """The neatline: a double rule round the sheet with a lantern at each corner, and the rule over the band the page writes in."""
    lines, lamps = layer(), layer()
    draw, lit = ImageDraw.Draw(lines), ImageDraw.Draw(lamps)
    for inset, width, value in ((30, 1.6, 255), (40, .9, 170)):
        draw.rectangle([inset * S, inset * S, (W - inset) * S, (H - inset) * S], outline=value, width=round(width * S))
    for x, y in ((30, 30), (W - 30, 30), (30, H - 30), (W - 30, H - 30)):
        dot(lit, x, y, 5)
    band = layer()
    y = BAND * H
    stroke(ImageDraw.Draw(band), [(70, y), (W - 70, y)], 1.4, 255)
    for x in (70, W / 2, W - 70):
        dot(lit, x, y, 4.2)
    # The band's rule runs out of ink toward its ends.
    return np.maximum(down(lines), down(band) * (smooth(0, .22, np.minimum(u, 1 - u) - 70 / W) * .8 + .2)), lamps


def paint():
    rng, rnd = np.random.default_rng(41), random.Random(41)
    sheet, u, v = vellum(rng, rnd)
    # What's drawn on the sheet thins out toward the writing band, so nothing runs under a word.
    above = 1 - smooth(BAND - .09, BAND - .005, v)

    def lay(mask, colour, strength):
        nonlocal sheet
        sheet += (np.array(colour, dtype=np.float32) - sheet) * (np.clip(mask, 0, 1) * strength)[..., None]

    cold, warm = routes(rnd)
    lay(down(cold, 5) * above, TEAL, .1)
    lay(down(cold) * above, TEAL, .2)
    lay(down(warm, 5) * above, AMBER, .09)
    lay(down(warm) * above, AMBER, .17)
    lines, lamps = compass(rnd, W * .6, H * .35, H * .29)
    lay(down(lines) * (.35 + .65 * above), INK, .13)
    lay(down(lamps, 7) * above, AMBER, .3)
    lay(down(lamps) * above, AMBER, .5)
    # Lantern dust: sparse pinpoints, as if the sheet had been pricked through to a light.
    dust = layer()
    draw = ImageDraw.Draw(dust)
    for _ in range(150):
        dot(draw, rnd.uniform(40, W - 40), rnd.uniform(40, H * (BAND - .03)), rnd.choice((.8, .8, 1, 1.3)), rnd.randint(90, 255))
    lay(down(dust, 2.2), AMBER, .22)
    lay(down(dust), (255, 226, 170), .42)
    # The writing band: darker and still. Then the neatline over everything.
    sheet *= (1 - .3 * smooth(BAND - .012, BAND + .05, v))[..., None]
    rule, corner = frame(u, v)
    lay(rule, GOLD, .34)
    lay(down(corner, 6), AMBER, .3)
    lay(down(corner), AMBER, .62)
    # Grain last: it breaks up the banding a dark gradient shows once it's compressed.
    sheet += rng.normal(0, 1.5, (H, W, 1)).astype(np.float32)
    # Out past the neatline the sheet darkens to the panel's own colour and then lets go altogether.
    xs, ys = np.minimum(u, 1 - u) * (W - 1), np.minimum(v, 1 - v) * (H - 1)
    edge = np.minimum(xs, ys)
    corner_cut = np.where((xs < 26) & (ys < 26), 26 - np.sqrt((26 - xs) ** 2 + (26 - ys) ** 2), edge)   # rounded corners
    edge = np.minimum(edge, corner_cut)
    hold = smooth(2, 28, edge)[..., None]
    sheet = sheet * hold + np.array(PLATE, dtype=np.float32) * (1 - hold)
    alpha = smooth(0, 14, edge) * 255
    rgba = np.concatenate([np.clip(sheet, 0, 255), alpha[..., None]], axis=-1).astype(np.uint8)
    return Image.fromarray(rgba)


def preview(sheet, path):
    """The sheet as the menu shows it: 1220 x 612 on the panel's colour, with lanterns, a route, names and the stop's lines where RouteChart draws them."""
    shown = sheet.resize((1220, 612), Image.Resampling.LANCZOS)
    proof = Image.new("RGBA", (1340, 720), PLATE + (255,))
    proof.alpha_composite(shown, (60, 40))
    draw = ImageDraw.Draw(proof)
    try:
        small, large = ImageFont.truetype("arial.ttf", 24), ImageFont.truetype("georgia.ttf", 34)
    except OSError:
        small = large = ImageFont.load_default()

    def place(x, y):                                           # RouteChart.Place, in this proof's pixels
        return 60 + 120 + x * 980, 40 + 612 - (238 + y * 300)

    stops = [("The Decks", .03, .56), ("Sorrel Outpost", .05, .12), ("Tallow Drift", .27, .88), ("Lanternwick", .29, .38), ("Rootmarket", .51, .66), ("Driftreed", .53, .14),
             ("Many Hands", .74, .42), ("Lastlight", .76, .9), ("Uncharted", .98, .64), ("Uncharted", .97, .2)]
    for a, b in ((0, 1), (0, 2), (2, 3), (3, 4), (4, 5), (4, 6), (6, 7), (6, 8), (8, 9)):
        draw.line([place(*stops[a][1:]), place(*stops[b][1:])], fill=(255, 199, 102, 150) if b < 8 else (150, 178, 220, 80), width=3)
    for name, x, y in stops:
        px, py = place(x, y)
        lit = name != "Uncharted"
        if lit:
            draw.ellipse([px - 7, py - 7, px + 7, py + 7], fill=(255, 189, 77, 255))
        else:
            draw.ellipse([px - 7, py - 7, px + 7, py + 7], outline=(143, 163, 194, 200), width=2)
        draw.text((px, py + 38), name, font=small, fill=(247, 250, 255, 255) if lit else (140, 161, 189, 255), anchor="mm")
    draw.text((100, 40 + 306 + 44 + 124), "Lanternwick", font=large, fill=(255, 217, 115, 255), anchor="lm")
    draw.text((1240, 40 + 306 + 44 + 124), "NEXT STEP", font=small, fill=(176, 204, 235, 255), anchor="rm")
    draw.text((100, 40 + 306 + 44 + 164), "The Lantern Road  ·  Shop, Bench, Repair", font=small, fill=(176, 204, 235, 255), anchor="lm")
    draw.text((100, 40 + 306 + 44 + 212), "The Wick-folk's canyon town: the arch market, the Wick House and Aumi's archive.", font=small, fill=(224, 237, 255, 255), anchor="lm")
    proof.convert("RGB").save(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--out", type=Path, default=OUT)
    parser.add_argument("--preview", type=Path, help="also write a proof of the sheet as the menu shows it")
    args = parser.parse_args()
    sheet = paint()
    args.out.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(args.out, optimize=True)
    if args.preview:
        preview(sheet, args.preview)
    print("ROUTE_CHART_ART_OK %s %dx%d" % (args.out, sheet.width, sheet.height))


if __name__ == "__main__":
    main()
