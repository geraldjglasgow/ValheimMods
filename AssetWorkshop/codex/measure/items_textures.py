"""How the game's item textures are painted: close-up sheets (albedo, albedo with the mesh's uv layout drawn over it,
normal map, metal map, all scaled up nearest-neighbour so every painted pixel shows) and numbers per albedo: tones,
saturation, how many colours, how much is near-black gap or near-white highlight, how busy the paint is.

    python codex/measure/items_textures.py [category ...]

Reads data/items.json (each category's references), writes codex/out/items/paint/<category>.png and returns the
numbers for items.py (texture_stats). Plain Python with numpy and Pillow.
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import game
import items_primitives

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.join(HERE, "..", "data", "items.json")
OUT = os.path.join(HERE, "..", "out", "items", "paint")
CELL = 256
SLOTS = ("_MainTex", "uv", "_BumpMap", "_MetallicGlossMap")
CLOSEUPS = {   # textures looked at pixel by pixel for the paint notes, by the item that wears them
    "metal": ["Battleaxe", "SwordBlackmetal", "SledgeIron", "AxeGold", "AxeBronze", "AtgeirBlackmetal"],
    "wood_leather": ["ShieldWood", "ShieldBlackmetal", "BowFineWood", "SpearWolfFang", "KnifeChitin", "Wood"],
    "armour_things": ["ArmorIronChest", "HelmetIron", "CopperOre", "TrophyBoar", "BowDraugrFang", "MeadHealthMinor"],
}


def main():
    with open(DATA, encoding="utf-8") as handle:
        categories = json.load(handle)["categories"]
    os.makedirs(OUT, exist_ok=True)
    for key in sys.argv[1:] or list(categories):
        rows = [r for r in categories[key]["samples"] if r["prefab"] in categories[key].get("references", [])]
        if rows:
            sheet(key, rows)
    prefabs = {r["name"]: r["prefab"] for c in categories.values() for r in c["samples"]}
    for name, items in CLOSEUPS.items():
        closeups(name, [(item, prefabs[item]) for item in items if item in prefabs])


def closeups(name, items):
    """Whole albedos scaled to 512 px, nearest-neighbour, three to a row: the brushwork pixel by pixel."""
    size, columns = 512, 3
    rows = (len(items) + columns - 1) // columns
    canvas = Image.new("RGB", (size * columns, (size + 16) * rows), (30, 30, 30))
    draw = ImageDraw.Draw(canvas)
    for n, (item, prefab) in enumerate(items):
        material, _ = main_material(prefab)
        image = load(material["textures"].get("_MainTex")) if material else None
        if image is None:
            continue
        x, y = (n % columns) * size, (n // columns) * (size + 16)
        draw.text((x + 4, y + 2), f"{item}: {image.width} x {image.height} px", fill=(230, 230, 220),
                  font=ImageFont.load_default())
        canvas.paste(image.resize((size, size), Image.NEAREST), (x, y + 16))
    canvas.save(os.path.join(OUT, f"closeup_{name}.png"))


def sheet(key, rows):
    """One row per reference item: its first material's maps side by side, labelled."""
    canvas = Image.new("RGB", (CELL * len(SLOTS), (CELL + 16) * len(rows)), (30, 30, 30))
    draw = ImageDraw.Draw(canvas)
    for n, row in enumerate(rows):
        material, mesh = main_material(row["prefab"])
        if not material:
            continue
        y = n * (CELL + 16)
        draw.text((4, y + 2), f"{row['name']}  {material['name']}  ({material['shader']})", fill=(230, 230, 220),
                  font=ImageFont.load_default())
        for col, slot in enumerate(SLOTS):
            image = uv_overlay(material, mesh) if slot == "uv" else load(material["textures"].get(slot))
            if image is not None:
                canvas.paste(image.resize((CELL, CELL), Image.NEAREST), (col * CELL, y + 16))
    canvas.save(os.path.join(OUT, key + ".png"))


def main_material(prefab_path):
    """The material and mesh of the item's largest renderer (most triangles), as data/items.json measured it."""
    import items_model
    prefab = game.prefab(prefab_path)
    parts = items_model.parts(prefab)
    found = parts["held"] or parts["worn"] or parts["drop"]
    if not found:
        return None, None
    node, _, body, mesh = max(found, key=lambda f: items_primitives.stats(f[3])["triangles"])
    paths = [p for p in game.renderer_materials(body) if p]
    return (game.material(paths[0]) if paths else None), mesh


def load(path):
    """A texture as RGB (alpha dropped), or None."""
    if not path:
        return None
    try:
        with Image.open(os.path.join(game.ROOT, path)) as image:
            return image.convert("RGB")
    except OSError:
        return None


def uv_overlay(material, mesh):
    """The albedo scaled up with the mesh's uv triangles drawn over it: which pixels land where on the model."""
    albedo = load(material["textures"].get("_MainTex"))
    if albedo is None or mesh is None or mesh.startswith("builtin:"):
        return None
    _, uv, tris = items_primitives.arrays(mesh)
    if uv is None:
        return None
    big = albedo.resize((CELL * 2, CELL * 2), Image.NEAREST)
    draw = ImageDraw.Draw(big)
    points = [(u % 1.0 * big.width, (1 - v % 1.0) * big.height) for u, v in uv]
    for a, b, c in tris:
        draw.polygon([points[a], points[b], points[c]], outline=(255, 60, 200))
    return big


def uv_mask(mesh, size):
    """Which texture pixels the mesh's uv triangles cover (the painted part; atlases leave the rest unused)."""
    if mesh is None or mesh.startswith("builtin:"):
        return None
    _, uv, tris = items_primitives.arrays(mesh)
    if uv is None:
        return None
    image = Image.new("L", size, 0)
    draw = ImageDraw.Draw(image)
    for a, b, c in tris:
        draw.polygon([(uv[i][0] % 1.0 * size[0], (1 - uv[i][1] % 1.0) * size[1]) for i in (a, b, c)], fill=255)
    covered = np.asarray(image).ravel() > 0
    return covered if covered.any() else None


def albedo_stats(path, mesh=None):
    """Numbers that describe how an albedo is painted (sRGB 0-255 values), over the pixels the mesh uses."""
    image = load(path)
    if image is None:
        return None
    pixels = np.asarray(image, dtype=np.float64).reshape(-1, 3)
    used = uv_mask(mesh, image.size)
    if used is not None:
        pixels = pixels[used]
    luma = pixels @ np.array([0.2126, 0.7152, 0.0722])
    high, low = pixels.max(axis=1), pixels.min(axis=1)
    saturation = np.where(high > 0, (high - low) / np.maximum(high, 1), 0)
    quantised = {tuple(p) for p in (pixels // 16).astype(int)}
    grid = np.asarray(image.convert("L"), dtype=np.float64)
    inside = used.reshape(grid.shape) if used is not None else np.ones(grid.shape, bool)
    busy = (np.abs(np.diff(grid, axis=0))[inside[1:] & inside[:-1]].mean()
            + np.abs(np.diff(grid, axis=1))[inside[:, 1:] & inside[:, :-1]].mean()) / 2
    return {"size": list(image.size), "used_share": round(float(used.mean()), 2) if used is not None else None,
            "luma_p5_p50_p95": [round(float(v)) for v in np.percentile(luma, [5, 50, 95])],
            "saturation_median": round(float(np.median(saturation)), 2), "colours_16": len(quantised),
            "near_black": round(float((luma < 25).mean()), 3), "near_white": round(float((luma > 225).mean()), 3),
            "neighbour_step": round(float(busy), 1)}


def normal_stats(path):
    """How much the normal map carries: the mean tilt away from flat, in degrees, and the share of pixels tilted more
    than 10 degrees."""
    image = load(path)
    if image is None:
        return None
    n = np.asarray(image, dtype=np.float64).reshape(-1, 3) / 127.5 - 1.0
    z = np.sqrt(np.clip(1 - n[:, 0] ** 2 - n[:, 1] ** 2, 0, 1))
    tilt = np.degrees(np.arccos(np.clip(z, 0, 1)))
    return {"mean_tilt_deg": round(float(tilt.mean()), 1), "tilted_over_10deg": round(float((tilt > 10).mean()), 2)}


def metal_stats(path):
    """The metal map: how much of it is metal (red over 0.5) and the smoothness (alpha) range."""
    if not path:
        return None
    try:
        with Image.open(os.path.join(game.ROOT, path)) as image:
            rgba = np.asarray(image.convert("RGBA"), dtype=np.float64) / 255
    except OSError:
        return None
    red, alpha = rgba[..., 0].ravel(), rgba[..., 3].ravel()
    return {"metal_share": round(float((red > 0.5).mean()), 2),
            "smooth_p5_p50_p95": [round(float(v), 2) for v in np.percentile(alpha, [5, 50, 95])]}


def split_colours(albedo_path, metal_path, mesh):
    """Median sRGB of the used albedo pixels the metal map marks metal (red over half) and of the rest, with the
    share of each: the colours a new item of the kind paints its metal and its wood, leather or cloth."""
    albedo = load(albedo_path)
    if albedo is None or not metal_path:
        return None
    with Image.open(os.path.join(game.ROOT, metal_path)) as image:
        metal = np.asarray(image.convert("RGBA").resize(albedo.size, Image.NEAREST))[..., 0].ravel() > 127
    pixels = np.asarray(albedo, dtype=np.float64).reshape(-1, 3)
    used = uv_mask(mesh, albedo.size)
    used = used if used is not None else np.ones(len(pixels), bool)
    found = {}
    for name, pick in (("metal", used & metal), ("other", used & ~metal)):
        if pick.sum() >= 4:
            found[name] = {"srgb": [int(v) for v in np.median(pixels[pick], axis=0)],
                           "share": round(float(pick.sum() / used.sum()), 2)}
    return found or None


def texture_stats(prefab_path):
    """All three for one item's main material, for data/items.json."""
    material, mesh = main_material(prefab_path)
    if not material:
        return None
    maps = material["textures"]
    return {"albedo": albedo_stats(maps.get("_MainTex"), mesh), "normal": normal_stats(maps.get("_BumpMap")),
            "metal": metal_stats(maps.get("_MetallicGlossMap")),
            "colours": split_colours(maps.get("_MainTex"), maps.get("_MetallicGlossMap"), mesh)}


if __name__ == "__main__":
    main()
