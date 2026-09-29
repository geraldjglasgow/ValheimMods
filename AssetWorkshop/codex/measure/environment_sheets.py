"""Contact sheets of the game's world textures for looking at them (never committed): the terrain's texture array,
and the main textures of the reference prefabs in data/environment.json, one sheet per category, each texture
shown over a mid grey (so cut-out cards read) beside its alpha channel.

    python codex/measure/environment_sheets.py          (after environment.py)

Writes codex/out/environment/textures_<category>.png and terrain_slices.png, each at most 1600 px wide.
"""
import json
import os

from PIL import Image, ImageDraw

import game

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.join(HERE, "..", "data", "environment.json")
OUT = os.path.join(HERE, "..", "out", "environment")
TILE, LABEL, COLUMNS = 180, 28, 8           # a texture and its alpha side by side: 8 tiles make 1600 px with gaps


def tile(path):
    """(colour over grey, alpha as grey) tiles of a texture, nearest-neighbour scaled like the game's point filter."""
    with Image.open(os.path.join(game.ROOT, path)) as image:
        rgba = image.convert("RGBA")
    grey = Image.new("RGBA", rgba.size, (128, 128, 128, 255))
    colour = Image.alpha_composite(grey, rgba).convert("RGB").resize((TILE, TILE), Image.NEAREST)
    alpha = rgba.getchannel("A").convert("RGB").resize((TILE, TILE), Image.NEAREST)
    return colour, alpha


def sheet(items, out):
    """Tiles [(label, texture path)] into one labelled PNG: each entry takes two cells (colour, alpha)."""
    cells = 2 * len(items)
    rows = (cells + COLUMNS - 1) // COLUMNS
    image = Image.new("RGB", (COLUMNS * (TILE + 20), rows * (TILE + LABEL)), (24, 24, 24))
    draw = ImageDraw.Draw(image)
    for i, (label, path) in enumerate(items):
        x, y = (2 * i % COLUMNS) * (TILE + 20), (2 * i // COLUMNS) * (TILE + LABEL)
        try:
            colour, alpha = tile(path)
        except OSError:
            continue
        size = game.texture_size(path)
        image.paste(colour, (x, y + LABEL))
        image.paste(alpha, (x + TILE + 20, y + LABEL))
        draw.text((x + 2, y + 2), f"{label[:44]}", fill=(255, 230, 120))
        draw.text((x + 2, y + 14), f"{size[0]}x{size[1]}  {os.path.basename(path)[:34]}", fill=(200, 200, 200))
    os.makedirs(OUT, exist_ok=True)
    image.save(out)
    return out


def terrain_sheet():
    """The terrain's sixteen slices, each with its alpha."""
    with Image.open(os.path.join(game.ROOT, "world/terrain/terrain_d_array.png")) as array:
        rgba = array.convert("RGBA")
    width = rgba.size[0]
    image = Image.new("RGB", (COLUMNS * (TILE + 20), 4 * (TILE + LABEL)), (24, 24, 24))
    draw = ImageDraw.Draw(image)
    for i in range(rgba.size[1] // width):
        part = rgba.crop((0, i * width, width, (i + 1) * width))
        x, y = (2 * i % COLUMNS) * (TILE + 20), (2 * i // COLUMNS) * (TILE + LABEL)
        image.paste(part.convert("RGB").resize((TILE, TILE), Image.NEAREST), (x, y + LABEL))
        image.paste(part.getchannel("A").convert("RGB").resize((TILE, TILE), Image.NEAREST), (x + TILE + 20, y + LABEL))
        draw.text((x + 2, y + 2), f"slice {i}", fill=(255, 230, 120))
    image.save(os.path.join(OUT, "terrain_slices.png"))


def category_items(category):
    """[(material name, main texture)] over every sample of a category, each texture once."""
    seen, items = set(), []
    for sample in category["samples"]:
        for mat in sample.get("materials_detail", []):
            if mat.get("main") and mat["main"] not in seen:
                seen.add(mat["main"])
                items.append((f"{sample['name']}: {mat['name']}", mat["main"]))
    return items


RENDER_TILE = 390                              # four render tiles a row: 1560 px


def render_sheets():
    """Labelled sheets of the Blender renders (render/index.json, from environment_render.py), one per category:
    each prefab's view and wire view side by side, with its triangles and size."""
    try:
        with open(os.path.join(OUT, "render", "index.json"), encoding="utf-8") as handle:
            index = json.load(handle)
    except OSError:
        return
    with open(DATA, encoding="utf-8") as handle:
        samples = {s["prefab"]: s for c in json.load(handle)["categories"].values() for s in c["samples"]}
    for key, entries in index.items():
        tiles = [(e, t) for e in entries for t in e["tiles"]]
        if tiles:
            render_sheet(key, tiles, samples)


def render_sheet(key, tiles, samples):
    rows = (len(tiles) + 3) // 4
    image = Image.new("RGB", (4 * RENDER_TILE, rows * (RENDER_TILE + LABEL)), (24, 24, 24))
    draw = ImageDraw.Draw(image)
    for i, (entry, path) in enumerate(tiles):
        x, y = (i % 4) * RENDER_TILE, (i // 4) * (RENDER_TILE + LABEL)
        with Image.open(path) as tile_image:
            image.paste(tile_image.convert("RGB").resize((RENDER_TILE, RENDER_TILE)), (x, y + LABEL))
        sample = samples.get(entry["prefab"], {})
        draw.text((x + 2, y + 2), entry["name"][:40], fill=(255, 230, 120))
        draw.text((x + 2, y + 14), f"{sample.get('triangles')} tris  {sample.get('size_m')} m", fill=(200, 200, 200))
    image.save(os.path.join(OUT, f"render_{key.replace('.', '_')}.png"))


def main():
    os.makedirs(OUT, exist_ok=True)
    terrain_sheet()
    render_sheets()
    with open(DATA, encoding="utf-8") as handle:
        data = json.load(handle)
    for key, category in data["categories"].items():
        items = category_items(category)
        if items:
            sheet(items[:48], os.path.join(OUT, f"textures_{key.replace('.', '_')}.png"))
            print(key, len(items))


if __name__ == "__main__":
    main()
