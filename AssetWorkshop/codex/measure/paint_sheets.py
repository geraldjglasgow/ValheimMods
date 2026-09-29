"""Contact sheets of the game's textures for looking at them: a grid of textures at one tile size, point filtered
(nearest neighbour, the way the game shows them), each labelled, and 4x zooms of parts to see the brushwork. Written to
codex/out/paint/ (gitignored); they are for eyes only and never enter the repository.

    python paint_sheets.py                  # one sheet per family of paint_families, plus 4x zooms
"""
import os

import numpy as np
from PIL import Image, ImageDraw

import game
import paint_image

OUT = os.path.join(game.WORKSHOP, "codex", "out", "paint")
BACKGROUND = (40, 40, 44)


def sheet(entries, name, tile=192, columns=8, mask=False):
    """entries: [(label, texture path or family sample dict)]; writes out/paint/<name>.png, at most 1600 px wide,
    and returns its path."""
    columns = max(1, min(columns, 1600 // tile, len(entries)))
    rows = (len(entries) + columns - 1) // columns
    canvas = Image.new("RGB", (columns * tile, rows * (tile + 14)), BACKGROUND)
    draw = ImageDraw.Draw(canvas)
    for index, (label, path) in enumerate(entries):
        x, y = (index % columns) * tile, (index // columns) * (tile + 14)
        canvas.paste(_tile(path, tile, mask), (x, y + 14))
        draw.text((x + 2, y + 1), f"{index} {label}"[:tile // 6], fill=(230, 230, 230))
    path = os.path.join(OUT, name + ".png")
    os.makedirs(OUT, exist_ok=True)
    canvas.save(path)
    return path


def _tile(entry, tile, mask):
    """One texture fitted into a tile, nearest neighbour; with mask, texels the sample leaves out are dimmed."""
    sample = entry if isinstance(entry, dict) else {"texture": entry}
    rgba = paint_image.load(sample["texture"])
    rgb = rgba[..., :3].copy()
    if mask:
        rgb[~paint_image.sample_mask(sample, rgba)] *= 0.25
    image = Image.fromarray((rgb * 255).astype(np.uint8))
    scale = tile / max(image.size)
    size = (max(1, int(image.size[0] * scale)), max(1, int(image.size[1] * scale)))
    return image.resize(size, Image.NEAREST)


def zoom(path, crop, name, factor=4):
    """A crop (x0, y0, x1, y1 in fractions) of one texture, blown up factor times, nearest neighbour."""
    rgba = paint_image.load(path)
    height, width = rgba.shape[:2]
    x0, y0, x1, y1 = crop
    part = rgba[int(y0 * height):int(y1 * height), int(x0 * width):int(x1 * width), :3]
    image = Image.fromarray((part * 255).astype(np.uint8))
    image = image.resize((image.size[0] * factor, image.size[1] * factor), Image.NEAREST)
    out = os.path.join(OUT, name + ".png")
    image.save(out)
    return out


def family_sheets(families):
    """One sheet per family: every sample's texture with uncovered texels dimmed; its crops outlined."""
    written = []
    for family, spec in families.items():
        entries = [(os.path.basename(s["texture"]), s) for s in spec["samples"]]
        if entries:
            written.append(sheet(entries, "family_" + family.replace(".", "_"), tile=192, mask=True))
    return written


if __name__ == "__main__":
    import paint_families
    for written in family_sheets(paint_families.FAMILIES):
        print(written)
