"""4x zooms of each family's samples, nearest neighbour, to look at the brushwork: for every sample the 32 x 32
texel window its mask covers best (and, among those, the one with the most variation), side by side per family.
Written to codex/out/paint/zoom_<family>.png (gitignored).

    python paint_zoom.py [family ...]
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

import paint_families
import paint_image
import paint_sheets

WINDOW, FACTOR = 32, 4


def best_window(rgba, mask, window=WINDOW):
    """(y, x) of the window with the highest covered fraction, ties broken by value variance."""
    height, width = mask.shape
    size = min(window, height, width)
    value = rgba[..., :3].max(axis=-1)
    best, key = (0, 0), (-1.0, -1.0)
    for y in range(0, height - size + 1, max(1, size // 4)):
        for x in range(0, width - size + 1, max(1, size // 4)):
            part = mask[y:y + size, x:x + size]
            spread = float(value[y:y + size, x:x + size][part].std()) if part.any() else 0.0
            score = (round(float(part.mean()), 1), spread)
            if score > key:
                best, key = (y, x), score
    return best, size


def zoom_tile(sample):
    rgba = paint_image.load(sample["texture"])
    mask = paint_image.sample_mask(sample, rgba)
    (y, x), size = best_window(rgba, mask)
    rgb = rgba[y:y + size, x:x + size, :3].copy()
    rgb[~mask[y:y + size, x:x + size]] *= 0.3
    image = Image.fromarray((rgb * 255).astype(np.uint8))
    return image.resize((WINDOW * FACTOR, WINDOW * FACTOR), Image.NEAREST)


def family_zoom(family):
    samples = paint_families.FAMILIES[family]["samples"]
    tile = WINDOW * FACTOR
    columns = min(len(samples), 1600 // tile)
    rows = (len(samples) + columns - 1) // columns
    canvas = Image.new("RGB", (columns * tile, rows * (tile + 14)), paint_sheets.BACKGROUND)
    draw = ImageDraw.Draw(canvas)
    for index, sample in enumerate(samples):
        x, y = (index % columns) * tile, (index // columns) * (tile + 14)
        canvas.paste(zoom_tile(sample), (x, y + 14))
        draw.text((x + 2, y + 1), f"{index} {os.path.basename(sample['texture'])}"[:22], fill=(230, 230, 230))
    path = os.path.join(paint_sheets.OUT, "zoom_" + family.replace(".", "_") + ".png")
    canvas.save(path)
    return path


if __name__ == "__main__":
    for name in sys.argv[1:] or paint_families.FAMILIES:
        print(family_zoom(name))
