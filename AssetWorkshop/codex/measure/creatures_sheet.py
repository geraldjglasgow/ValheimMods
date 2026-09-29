"""Lays out the creature renders (creatures_render.py) for looking at: a labelled grid of three-quarter views, and a
lineup of side views all at one scale, standing on one ground line beside a 1.8 m player mark. Plain Python and
Pillow; writes into codex/out/creatures/ (gitignored).

    python codex/measure/creatures_sheet.py grid <out name> <creature> ...
    python codex/measure/creatures_sheet.py lineup <out name> <creature> ...
    python codex/measure/creatures_sheet.py categories        (a lineup and a grid per category, from the data)
"""
import json
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "out", "creatures"))
RENDERS = os.path.join(OUT, "renders")
BACKGROUND = (107, 115, 120)


def grid(names, out, columns=6, tile=256):
    """Three-quarter views in rows, each labelled with its name."""
    tiles = [_tile(n, "front", tile) for n in names]
    rows = (len(tiles) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * tile, rows * (tile + 16)), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    for i, (name, image) in enumerate(zip(names, tiles)):
        x, y = (i % columns) * tile, (i // columns) * (tile + 16)
        if image:
            sheet.paste(image, (x, y))
        draw.text((x + 4, y + tile + 2), name, fill=(255, 255, 255))
    sheet.save(os.path.join(OUT, out + ".png"))


def _tile(name, view, size):
    path = os.path.join(RENDERS, f"{name}_{view}.png")
    if not os.path.exists(path):
        return None
    return Image.open(path).convert("RGB").resize((size, size), Image.LANCZOS)


def lineup(names, out, height=720, metres=None):
    """Side views scaled to one metres-per-pixel, feet on one ground line, a 1.8 m mark at the left."""
    framings = {n: _framing(n) for n in names}
    names = [n for n in names if framings[n]]
    metres = metres or max(max(framings[n]["high"][2], 1.9) for n in names) * 1.1
    per_metre = height / metres
    widths = [max(int(framings[n]["ortho_scale"] * per_metre), 8) for n in names]
    sheet = Image.new("RGB", (40 + sum(widths) + 4 * len(names), height + 20), BACKGROUND)
    draw = ImageDraw.Draw(sheet)
    ground = height - 4
    draw.line([(20, ground), (20, ground - 1.8 * per_metre)], fill=(255, 220, 90), width=3)
    x = 40
    for name, width in zip(names, widths):
        _place(sheet, draw, name, framings[name], x, width, ground, per_metre)
        x += width + 4
    sheet.save(os.path.join(OUT, out + ".png"))


def _framing(name):
    path = os.path.join(RENDERS, f"{name}_side.json")
    if not os.path.exists(path):
        return None
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def _place(sheet, draw, name, framing, x, width, ground, per_metre):
    """Pastes one side view: its frame spans ortho_scale metres, centred on the framing centre."""
    image = Image.open(os.path.join(RENDERS, f"{name}_side.png")).convert("RGB").resize((width, width), Image.LANCZOS)
    top = ground - (framing["center"][2] + framing["ortho_scale"] / 2) * per_metre
    sheet.paste(image, (x, int(top)))
    draw.line([(x, ground), (x + width, ground)], fill=(60, 50, 40), width=1)
    draw.text((x + 2, ground + 4), f"{name} {framing['high'][2] - framing['low'][2]:.1f}m", fill=(255, 255, 255))


def categories():
    """lineup_<category>.png and grid_<category>.png of each category's distinct models (data/creatures.json)."""
    with open(os.path.join(HERE, "..", "data", "creatures.json"), encoding="utf-8") as handle:
        data = json.load(handle)["categories"]
    for key, category in data.items():
        names = [s["name"] for s in category["samples"] if s["triangles"] and not s["variant_of"]]
        short = key.split(".")[-1]
        lineup(names, "lineup_" + short)
        grid([s["name"] for s in category["samples"] if s["triangles"]], "grid_" + short)


def main():
    mode = sys.argv[1]
    os.makedirs(OUT, exist_ok=True)
    if mode == "categories":
        categories()
    elif mode == "grid":
        grid(sys.argv[3:], sys.argv[2])
    else:
        lineup(sys.argv[3:], sys.argv[2])


if __name__ == "__main__":
    main()
