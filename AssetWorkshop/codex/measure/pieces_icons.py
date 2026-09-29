"""The build menu's piece icons: measured (how much of the square the drawing fills, where it sits, how bright and how
saturated it is, whether the background is clear) and laid out as contact sheets in codex/out/pieces/icons/ to look at.

Each Piece.m_icon is a Sprite asset whose m_RD.texture is the packed IconAtlas and whose textureRect is the icon's
square in it (Unity counts y from the bottom). Run after measure/pieces.py:

    python codex/measure/pieces_icons.py
"""
import functools
import json
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import game  # noqa: E402
from workshop import unity  # noqa: E402

DATA = os.path.join(game.WORKSHOP, "codex", "data", "pieces.json")
OUT = os.path.join(game.WORKSHOP, "codex", "out", "pieces", "icons")
CELL, COLUMNS = 128, 10


@functools.lru_cache(maxsize=None)
def _atlas(path):
    return Image.open(os.path.join(unity.ROOT, path)).convert("RGBA")


def crop(sprite):
    """The icon's pixels (RGBA) from its sprite asset, or None."""
    text = unity.read(sprite)
    rect = re.search(r"textureRect:\n\s+serializedVersion: 2\n\s+x: ([\d.]+)\n\s+y: ([\d.]+)\n\s+width: ([\d.]+)\n"
                     r"\s+height: ([\d.]+)", text)
    texture = game.path_of(re.search(r"texture: (\{[^}]*\})", text).group(1))
    if not rect or not texture:
        return None
    x, y, w, h = (int(float(v)) for v in rect.groups())
    atlas = _atlas(texture)
    return atlas.crop((x, atlas.height - y - h, x + w, atlas.height - y))


def measure(icon):
    """{px, fill, box, centre, luminance, saturation, clear_corners} of one icon: fill is the share of pixels with
    alpha over half, box the opaque bounds as a share of the square, centre where the drawing's middle sits."""
    alpha = np.asarray(icon)[:, :, 3] / 255.0
    rgb = np.asarray(icon)[:, :, :3] / 255.0
    solid = alpha > 0.5
    if not solid.any():
        return None
    rows, cols = np.where(solid)
    size = icon.width
    lum = (rgb[solid] @ np.array([0.2126, 0.7152, 0.0722]))
    sat = rgb[solid].max(axis=1) - rgb[solid].min(axis=1)
    corners = [alpha[0, 0], alpha[0, -1], alpha[-1, 0], alpha[-1, -1]]
    return {"px": size, "fill": round(float(solid.mean()), 3),
            "box": [round((cols.max() - cols.min() + 1) / size, 3), round((rows.max() - rows.min() + 1) / size, 3)],
            "centre": [round((cols.max() + cols.min() + 1) / 2 / size, 3), round((rows.max() + rows.min() + 1) / 2 /
                                                                               size, 3)],
            "luminance": round(float(np.median(lum)), 3), "saturation": round(float(np.median(sat)), 3),
            "soft_edge": round(float(((alpha > 0.05) & (alpha < 0.95)).sum() / max(solid.sum(), 1)), 3),
            "clear_corners": bool(max(corners) < 0.05)}


def sheet(rows, path, title):
    """A contact sheet of icons (name, image) on a mid grey, point-upscaled to CELL px, labelled under each."""
    lines = (len(rows) + COLUMNS - 1) // COLUMNS
    image = Image.new("RGB", (COLUMNS * (CELL + 8) + 8, lines * (CELL + 22) + 30), (74, 70, 64))
    draw = ImageDraw.Draw(image)
    draw.text((8, 8), title, fill=(230, 220, 200))
    for n, (name, icon) in enumerate(rows):
        x, y = 8 + (n % COLUMNS) * (CELL + 8), 30 + (n // COLUMNS) * (CELL + 22)
        tile = Image.new("RGBA", (CELL, CELL), (46, 43, 38, 255))
        tile.alpha_composite(icon.resize((CELL, CELL), Image.NEAREST))
        image.paste(tile.convert("RGB"), (x, y))
        draw.text((x, y + CELL + 4), name[:22], fill=(220, 214, 200))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    image.save(path)


def main():
    with open(DATA, encoding="utf-8") as handle:
        data = json.load(handle)
    found = {}
    for key, category in data["categories"].items():
        rows = []
        for sample in category["samples"]:
            icon = crop(sample["icon"]) if sample.get("icon") else None
            if icon is not None:
                rows.append((sample["name"], icon))
                found[sample["name"]] = measure(icon)
        if rows:
            sheet(rows, os.path.join(OUT, key + ".png"), f"{key}: build menu icons, {rows[0][1].width} px, x2")
    stats = {k: [v[k] for v in found.values() if v] for k in ("fill", "luminance", "saturation", "soft_edge")}
    summary = {k: {"median": round(float(np.median(v)), 3), "p10": round(float(np.percentile(v, 10)), 3),
                   "p90": round(float(np.percentile(v, 90)), 3)} for k, v in stats.items()}
    summary["box_long_side"] = _spread([max(v["box"]) for v in found.values() if v])
    summary["clear_corners"] = sum(1 for v in found.values() if v and v["clear_corners"])
    summary["n"] = len(found)
    with open(os.path.join(OUT, "icons.json"), "w", encoding="utf-8") as handle:
        json.dump({"summary": summary, "icons": found}, handle, indent=1, sort_keys=True)
    print(json.dumps(summary, indent=1))


def _spread(values):
    return {"median": round(float(np.median(values)), 3), "p10": round(float(np.percentile(values, 10)), 3),
            "p90": round(float(np.percentile(values, 90)), 3)}


if __name__ == "__main__":
    main()
