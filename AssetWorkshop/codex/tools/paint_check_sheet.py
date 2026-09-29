"""The side-by-side sheet of paint_check: per family a row with the baked swatch (4x zoom of its best-covered window,
point filtered), three of the game's samples zoomed the same way, and the swatch's numbers against the game's."""
import os

import numpy as np
from PIL import Image, ImageDraw

import paint_check_stats as stats  # noqa: F401  (puts codex/measure on the path)
import paint_families
import paint_zoom

TILE = paint_zoom.WINDOW * paint_zoom.FACTOR
TEXT = 300
BACKGROUND = (34, 34, 38)


def swatch_tile(albedo_path, triangles):
    with Image.open(albedo_path) as image:
        rgba = np.asarray(image.convert("RGBA"), dtype=np.float64) / 255.0
    mask = stats.coverage(triangles, rgba.shape[0])
    (y, x), size = paint_zoom.best_window(rgba, mask)
    rgb = rgba[y:y + size, x:x + size, :3].copy()
    rgb[~mask[y:y + size, x:x + size]] *= 0.3
    return Image.fromarray((rgb * 255).astype(np.uint8)).resize((TILE, TILE), Image.NEAREST)


def row(family, albedo_path, triangles, rows):
    """One family's strip: swatch, three game zooms, the comparison text."""
    strip = Image.new("RGB", (TILE * 4 + 24 + TEXT, TILE + 16), BACKGROUND)
    draw = ImageDraw.Draw(strip)
    strip.paste(swatch_tile(albedo_path, triangles), (0, 16))
    draw.text((2, 2), f"{family} (swatch)", fill=(255, 220, 120))
    for index, sample in enumerate(paint_families.FAMILIES[family]["samples"][:3]):
        x = TILE * (index + 1) + 8 * (index + 1)
        strip.paste(paint_zoom.zoom_tile(sample), (x, 16))
        draw.text((x + 2, 2), os.path.basename(sample["texture"])[:20], fill=(220, 220, 220))
    for line, (key, mine, median, _, _, verdict) in enumerate(rows):
        colour = {"ok": (140, 220, 140), "range": (230, 210, 120), "off": (240, 120, 110)}[verdict]
        draw.text((TILE * 4 + 30, 18 + line * 11), f"{key:18} {mine:7.3f}  game {median:7.3f}  {verdict}", fill=colour)
    return strip


def sheet(strips, path):
    """Stacks the family strips into one image (split into pages of eight rows, at most 1600 px wide)."""
    pages = []
    for start in range(0, len(strips), 8):
        page = strips[start:start + 8]
        canvas = Image.new("RGB", (max(s.size[0] for s in page), sum(s.size[1] + 4 for s in page)), (20, 20, 22))
        y = 0
        for strip in page:
            canvas.paste(strip, (0, y))
            y += strip.size[1] + 4
        name = path.replace(".png", f"_{start // 8 + 1}.png")
        canvas.save(name)
        pages.append(name)
    return pages
