"""The inventory icons (ItemDrop m_icons): each Sprite asset points into the packed icon atlas; the icon is cut out of
it and measured: size, how much of the square it fills, margins, the angle of its long axis, the dark outline and
the soft shadow round the silhouette, its tones and saturation, and where the light comes from.

    items_icons.section(records)          # the icons block of data/items.json
    python codex/measure/items_icons.py   # also writes contact sheets to codex/out/items/icons/
"""
import collections
import functools
import os
import re

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import game
import items_classify
import items_stats
from workshop import unity

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "out", "items", "icons")


@functools.lru_cache(maxsize=None)
def atlas(path):
    with Image.open(os.path.join(game.ROOT, path)) as image:
        return image.convert("RGBA")


def icon(sprite_path):
    """The icon's pixels (RGBA) cut from its atlas, or None. Sprite rects count from the atlas' bottom left."""
    text = unity.read(sprite_path)
    rd = text[text.index("m_RD:"):text.index("m_AtlasRD:")] if "m_AtlasRD:" in text else text
    found = re.search(r"^    texture: (\{[^}]*\})", rd, re.MULTILINE)
    texture = game.path_of(found.group(1)) if found else None
    rect = re.search(r"textureRect:\s+serializedVersion: \d+\s+x: (\S+)\s+y: (\S+)\s+width: (\S+)\s+height: (\S+)", rd)
    if not texture or not rect:
        return None
    x, y, w, h = (int(round(float(v))) for v in rect.groups())
    sheet = atlas(texture)
    return sheet.crop((x, sheet.height - y - h, x + w, sheet.height - y))


def measure(image):
    """Numbers for one icon."""
    rgba = np.asarray(image, dtype=np.float64)
    alpha = rgba[..., 3] / 255
    solid = alpha > 0.5
    if not solid.any():
        return None
    ys, xs = np.nonzero(solid)
    size = image.width
    luma = rgba[..., :3] @ np.array([0.2126, 0.7152, 0.0722])
    return {"px": [image.width, image.height], "fill": round(float(solid.mean()), 3),
            "span": round(float(max(xs.max() - xs.min(), ys.max() - ys.min()) + 1) / size, 3),
            "margin_px": [int(xs.min()), int(ys.min()), int(size - 1 - xs.max()), int(size - 1 - ys.max())],
            "angle_deg": _angle(xs, ys), "soft_alpha": round(float(((alpha > 0.02) & (alpha < 0.5)).mean()), 3),
            "outline_luma": _ring(luma, solid), "inner_luma": round(float(np.median(luma[_erode(solid, 3)])), 1)
            if _erode(solid, 3).any() else None,
            "saturation": round(float(np.median(_saturation(rgba[..., :3])[solid])), 2),
            "light_top_left": _light(luma, solid)}


def _angle(xs, ys):
    """Angle of the silhouette's long axis in degrees, 0 = horizontal, 45 = rising to the right."""
    points = np.stack([xs, -ys], axis=1).astype(np.float64)
    points -= points.mean(axis=0)
    _, values, vt = np.linalg.svd(points, full_matrices=False)
    if values[0] < 1.3 * values[1]:
        return None     # round or square: no long axis
    angle = float(np.degrees(np.arctan2(vt[0][1], vt[0][0])))
    return round((angle + 180) % 180, 0)


def _erode(mask, steps):
    inside = mask.copy()
    for _ in range(steps):
        inside = inside & np.roll(inside, 1, 0) & np.roll(inside, -1, 0) & np.roll(inside, 1, 1) & np.roll(inside, -1, 1)
    return inside


def _ring(luma, solid):
    """Median brightness of the silhouette's outermost pixel ring (a drawn outline is darker than the inside)."""
    ring = solid & ~_erode(solid, 1)
    return round(float(np.median(luma[ring])), 1) if ring.any() else None


def _saturation(rgb):
    high, low = rgb.max(axis=-1), rgb.min(axis=-1)
    return np.where(high > 0, (high - low) / np.maximum(high, 1), 0)


def _light(luma, solid):
    """Brightness of the silhouette's top-left half minus its bottom-right half (positive: lit from the top left)."""
    ys, xs = np.nonzero(solid)
    upper = (xs - xs.mean()) - (ys - ys.mean()) < 0
    values = luma[ys, xs]
    return round(float(values[~upper].mean() - values[upper].mean()), 1) if upper.any() and (~upper).any() else None


def section(records):
    """Per category: the icon measurements' five numbers, and the icon size counts."""
    found, sizes = {}, collections.Counter()
    for key, rows in _by_category(records).items():
        numbers = [m for m in (measure(image) for _, image in rows) if m]
        sizes.update(f"{m['px'][0]}x{m['px'][1]}" for m in numbers)
        found[key] = {field: items_stats.five([m[field] for m in numbers]) for field in
                      ("fill", "span", "angle_deg", "soft_alpha", "outline_luma", "inner_luma", "saturation", "light_top_left")}
    return {"atlas": "Texture2D/sactx-0-4096x4096-BC7-IconAtlas-7390861a.png", "sizes": dict(sizes),
            "categories": found}


def _by_category(records):
    """{category: [(name, icon image)]} over the classified items that have an icon."""
    found = collections.defaultdict(list)
    for record in records:
        key = items_classify.category(record)
        sprites = [s for s in record["shared"]["icons"] if s]
        if key and sprites:
            image = icon(sprites[0])
            if image is not None:
                found[key].append((record["name"], image))
    return found


def sheets(records):
    """Contact sheets of every category's icons at 2x, on the inventory's dark slot colour, labelled."""
    os.makedirs(OUT, exist_ok=True)
    font = ImageFont.load_default()
    for key, rows in _by_category(records).items():
        rows = sorted(rows)[:96]
        cell, columns = 132, 12
        canvas = Image.new("RGB", (cell * columns, (cell + 12) * ((len(rows) + columns - 1) // columns)), (43, 38, 33))
        draw = ImageDraw.Draw(canvas)
        for n, (name, image) in enumerate(rows):
            x, y = (n % columns) * cell, (n // columns) * (cell + 12)
            big = image.resize((image.width * 2, image.height * 2), Image.NEAREST)
            canvas.paste(big, (x + 2, y + 2), big)
            draw.text((x + 2, y + cell - 2), name[:20], fill=(220, 210, 190), font=font)
        canvas.save(os.path.join(OUT, key + ".png"))


if __name__ == "__main__":
    import items_scan
    sheets(items_scan.load())
