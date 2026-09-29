"""Lays the rendered item tiles (items_render.py) out as contact sheets, at most 1600 px wide, each tile labelled with
the item's name, tier, triangles, texture size and size in metres. Plain Python with Pillow.

    python codex/measure/items_sheets.py [category ...]

Writes codex/out/items/sheets/<category>.png (and <category>_2.png ... when a category needs more than one sheet).
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
TILES = os.path.join(HERE, "..", "out", "items", "tiles")
SHEETS = os.path.join(HERE, "..", "out", "items", "sheets")
WIDTH = 1600
LABEL = 44
PER_SHEET = 30
TIERS = ["meadows", "black forest", "swamp", "mountain", "plains", "mistlands", "ashlands", "deep north"]


def main():
    wanted = sys.argv[1:] or sorted(os.listdir(TILES))
    os.makedirs(SHEETS, exist_ok=True)
    for key in wanted:
        index = os.path.join(TILES, key, "tiles.json")
        if os.path.exists(index):
            with open(index, encoding="utf-8") as handle:
                sheet(key, json.load(handle))


def sheet(key, info):
    tiles = info["tiles"]
    for part in range(0, len(tiles), PER_SHEET):
        chunk = tiles[part:part + PER_SHEET]
        suffix = "" if part == 0 else f"_{part // PER_SHEET + 1}"
        compose(key, info, chunk, os.path.join(SHEETS, key + suffix + ".png"))


def compose(key, info, tiles, path):
    """One sheet: a title line, then the tiles in rows with their labels under them."""
    first = Image.open(os.path.join(TILES, key, tiles[0]["file"]))
    columns = max(1, WIDTH // first.width)
    scale = min(1.0, WIDTH / (columns * first.width))
    tile_w, tile_h = int(first.width * scale), int(first.height * scale)
    rows = (len(tiles) + columns - 1) // columns
    canvas = Image.new("RGB", (columns * tile_w, 30 + rows * (tile_h + LABEL)), (32, 34, 33))
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    title = f"{key}: {len(tiles)} items"
    frame = info.get("frame")
    if frame:
        title += (f"  | attach frame seen from +Y: +Z up, +X right, fist at the red cross; tile = "
                  f"{2 * frame['half_x']:.2f} m across, z {frame['low_z']:.2f} to {frame['high_z']:.2f} m")
    draw.text((8, 8), title, fill=(230, 230, 220), font=font)
    for n, tile in enumerate(tiles):
        x, y = (n % columns) * tile_w, 30 + (n // columns) * (tile_h + LABEL)
        image = Image.open(os.path.join(TILES, key, tile["file"])).convert("RGB")
        canvas.paste(image.resize((tile_w, tile_h), Image.NEAREST), (x, y))
        for line, text in enumerate(label(tile)):
            draw.text((x + 4, y + tile_h + 3 + line * 13), text, fill=(220, 220, 210), font=font)
    canvas.save(path)


def label(tile):
    tier = TIERS[tile["tier"]] if tile.get("tier") is not None else "-"
    size = " x ".join(f"{s:.2f}" for s in tile["size_m"])
    return [tile["name"][:40], f"{tier} | {tile['triangles']} tris | {tile['texture_px'] or '-'} px", f"{size} m"]


if __name__ == "__main__":
    main()
