"""The game's particle textures: contact sheets to look at (codex/out/vfx/textures_*.png, gitignored) and numbers for
data/vfx.json (size, colour or greyscale, alpha coverage and softness, flipbook layout, what draws with it).

    python codex/measure/vfx_textures.py        after vfx_corpus.py

Each tile shows the texture three ways: its colour over mid grey (how an alpha-blended particle shows it), its colour
added onto black (how an additive one does), and its alpha channel alone, with the name, size and use count under it.
Point-filtered, since the game's small particle textures are drawn with visible pixels.
"""
import collections
import os

import numpy as np
from PIL import Image, ImageDraw

import game
import vfx_corpus
import vfx_effect

OUT = vfx_corpus.OUT
TILE, LABEL, PER_SHEET, COLUMNS = 128, 30, 24, 4


def usage(effects):
    """{texture path: {'count', 'tiles', 'shaders', 'blends', 'modes', 'examples'}} over every particle system."""
    table = collections.defaultdict(lambda: {"count": 0, "tiles": set(), "shaders": collections.Counter(),
                                             "blends": collections.Counter(), "modes": collections.Counter(),
                                             "examples": []})
    for fx in effects:
        for s in vfx_effect.all_systems(fx):
            m = (s.get("renderer") or {}).get("material") or {}
            if not m.get("texture"):
                continue
            row = table[m["texture"]]
            row["count"] += 1
            row["shaders"][m["shader"]] += 1
            row["blends"][m["blend"]] += 1
            row["modes"][s["renderer"]["mode"]] += 1
            if s.get("sheet"):
                row["tiles"].add(tuple(s["sheet"]["tiles"]))
            if len(row["examples"]) < 4 and fx["name"] not in row["examples"]:
                row["examples"].append(fx["name"])
    return table


def load_rgba(path):
    with Image.open(os.path.join(game.ROOT, path)) as image:
        return np.asarray(image.convert("RGBA"), dtype=np.float32) / 255.0


def measure(path):
    """Size, greyscale or colour, alpha coverage and softness, mean colour of the visible part."""
    rgba = load_rgba(path)
    rgb, alpha = rgba[..., :3], rgba[..., 3]
    has_alpha = float(alpha.min()) < 0.99
    weight = alpha if has_alpha else rgb.max(axis=2)
    visible = weight > 0.05
    chroma = float((rgb.max(axis=2) - rgb.min(axis=2))[visible].mean()) if visible.any() else 0.0
    grad = np.abs(np.diff(weight, axis=0)).mean() + np.abs(np.diff(weight, axis=1)).mean()
    edge = float(((weight > 0.05) & (weight < 0.95)).sum() / max(1, visible.sum()))
    mean_rgb = (rgb * weight[..., None]).sum(axis=(0, 1)) / max(1e-6, weight.sum())
    return {"px": [rgba.shape[1], rgba.shape[0]], "alpha": has_alpha, "greyscale": chroma < 0.06,
            "chroma": round(chroma, 3), "coverage": round(float(visible.mean()), 3),
            "soft_edge": round(edge, 3), "gradient": round(float(grad), 4),
            "mean_rgb": [round(float(c), 3) for c in mean_rgb],
            "value": [round(float(np.percentile(rgb.max(axis=2)[visible], p)), 3) for p in (10, 50, 90)]
            if visible.any() else None}


def import_settings(path):
    """sRGB, point filtering and mipmaps from the texture's .meta, as the game imports it."""
    import re
    try:
        with open(os.path.join(game.ROOT, path + ".meta"), encoding="utf-8") as handle:
            meta = handle.read()
    except OSError:
        return {}
    grab = lambda key: (re.search(rf"^\s+{key}: (-?\d+)", meta, re.MULTILINE) or [None, None])[1]
    return {"srgb": grab("sRGBTexture") == "1", "point": grab("filterMode") == "0", "mips": grab("enableMipMap") == "1"}


def tile(path, row):
    """One contact-sheet tile: colour over grey, colour added onto black, alpha, and a label."""
    rgba = load_rgba(path)
    image = Image.fromarray((rgba * 255).astype(np.uint8), "RGBA").resize((TILE, TILE), Image.NEAREST)
    arr = np.asarray(image, dtype=np.float32) / 255.0
    rgb, alpha = arr[..., :3], arr[..., 3:4]
    over = rgb * alpha + 0.45 * (1 - alpha)
    added = np.clip(rgb * alpha, 0, 1) if alpha.min() < 0.99 else rgb
    panels = [over, added, np.repeat(alpha, 3, axis=2)]
    strip = np.concatenate([np.pad(p, ((0, 0), (0, 4), (0, 0)), constant_values=0.1) for p in panels], axis=1)
    canvas = Image.new("RGB", (strip.shape[1], TILE + LABEL), (26, 26, 26))
    canvas.paste(Image.fromarray((strip * 255).astype(np.uint8)), (0, 0))
    draw = ImageDraw.Draw(canvas)
    w, h = rgba.shape[1], rgba.shape[0]
    tiles = ",".join(f"{a}x{b}" for a, b in sorted(row["tiles"])) or "-"
    draw.text((3, TILE + 2), os.path.basename(path)[:44], fill=(230, 230, 230))
    draw.text((3, TILE + 15), f"{w}x{h} used {row['count']} sheet {tiles}", fill=(170, 170, 170))
    return canvas


def sheets(table, prefix="textures"):
    """Contact sheets of the textures by use, PER_SHEET a sheet."""
    ordered = sorted(table, key=lambda p: -table[p]["count"])
    written = []
    for start in range(0, len(ordered), PER_SHEET):
        tiles = [tile(p, table[p]) for p in ordered[start:start + PER_SHEET]]
        width, height = tiles[0].width, tiles[0].height
        rows = (len(tiles) + COLUMNS - 1) // COLUMNS
        sheet = Image.new("RGB", (COLUMNS * (width + 6), rows * (height + 6)), (12, 12, 12))
        for i, t in enumerate(tiles):
            sheet.paste(t, ((i % COLUMNS) * (width + 6), (i // COLUMNS) * (height + 6)))
        name = os.path.join(OUT, f"{prefix}_{start // PER_SHEET + 1:02d}.png")
        sheet.save(name)
        written.append(name)
    return written


def records(table):
    """The per-texture numbers for data/vfx.json."""
    out = []
    for path in sorted(table, key=lambda p: -table[p]["count"]):
        row = table[path]
        try:
            numbers = measure(path)
        except OSError:
            continue
        out.append({"texture": path, "used": row["count"], "flipbook": sorted(list(t) for t in row["tiles"]),
                    "shaders": dict(row["shaders"].most_common()), "blends": dict(row["blends"].most_common()),
                    "modes": dict(row["modes"].most_common()), "examples": row["examples"], **numbers,
                    **import_settings(path)})
    return out


def main():
    table = usage(vfx_corpus.load())
    for name in sheets(table):
        print(os.path.relpath(name))
    return records(table)


if __name__ == "__main__":
    main()
