"""How the pieces' main materials are painted, as numbers: for each material set's albedo, the colours inside the UV
islands the pieces actually use (unused atlas space is left out), times the material's _Color tint, new and worn side
by side; how much of the texture the islands cover; and the plank and beam sections the game builds its wood pieces
from (its bevelled unit cube, scaled per part).

    pieces_paint.colours(samples)      # {set: {texture, tint, median_srgb, hex, luminance, saturation, coverage}}
    pieces_paint.sections(samples)     # {piece: [part sizes in metres]} for the cube-built pieces
"""
import os

import numpy as np
from PIL import Image, ImageDraw

import game
import pieces_scan
import pieces_sheets
from workshop import unity

BEVEL_CUBE = "Misc/common_models/Cube_Cube_Material.asset"


def _mask(texture, samples, size):
    """Pixels of the texture inside any UV island of the pieces that draw it (wrapped into the texture)."""
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)
    for prefab in pieces_sheets.spread(samples, texture, 12):
        for tris in pieces_sheets.islands(prefab, texture):
            for tri in tris:
                shift = np.floor(tri.mean(axis=0))
                draw.polygon([((u - shift[0]) * size[0], (1 - (v - shift[1])) * size[1]) for u, v in tri], fill=255)
    return np.asarray(mask) > 0


def _stats(texture, mask, tint):
    """Median sRGB (tint applied in linear light), luminance spread and saturation of the masked pixels."""
    rgb = np.asarray(Image.open(os.path.join(unity.ROOT, texture)).convert("RGB")) / 255.0
    pixels = rgb[mask] if mask.any() else rgb.reshape(-1, 3)
    linear = np.where(pixels <= 0.04045, pixels / 12.92, ((pixels + 0.055) / 1.055) ** 2.4) * np.array(tint[:3])
    srgb = np.where(linear <= 0.0031308, linear * 12.92, 1.055 * np.power(linear, 1 / 2.4) - 0.055)
    lum = srgb @ np.array([0.2126, 0.7152, 0.0722])
    median = np.median(srgb, axis=0)
    return {"median_srgb": [int(round(v * 255)) for v in median],
            "hex": "#" + "".join(f"{int(round(v * 255)):02x}" for v in median),
            "luminance": {q: round(float(np.percentile(lum, p)), 3) for q, p in (("p10", 10), ("median", 50),
                                                                              ("p90", 90))},
            "saturation": round(float(np.median(srgb.max(axis=1) - srgb.min(axis=1))), 3)}


def colours(samples):
    """{set: {texture, px, tint, coverage, new: {...}, worn: {...}}} for the sets pieces_sheets lays out."""
    found = {}
    for name, (new, worn) in pieces_sheets.SETS.items():
        new_path, worn_path = pieces_sheets.material_path(new), pieces_sheets.material_path(worn)
        texture = game.material(new_path)["textures"].get("_MainTex") if new_path else None
        if not texture:
            continue
        size = game.texture_size(texture)
        mask = _mask(texture, samples, size)
        tint = game.material(new_path)["colors"].get("_Color", (1, 1, 1, 1))
        row = {"material": new, "texture": texture, "px": list(size), "tint": [round(v, 3) for v in tint[:3]],
               "coverage": round(float(mask.mean()), 3), "new": _stats(texture, mask, tint)}
        worn_texture = game.material(worn_path)["textures"].get("_MainTex") if worn_path else None
        if worn_texture and game.texture_size(worn_texture) == size:
            worn_tint = game.material(worn_path)["colors"].get("_Color", (1, 1, 1, 1))
            row["worn"] = dict(_stats(worn_texture, mask, worn_tint), material=worn, texture=worn_texture,
                               tint=[round(v, 3) for v in worn_tint[:3]])
        found[name] = row
    return found


def sections(samples):
    """{piece: sorted part sizes [small, middle, long] in metres} of the new look's parts drawn with the bevelled
    cube or Unity's cube: the planks, battens, beams and posts the simple wood pieces are assembled from."""
    found = {}
    for sample in samples:
        scan = pieces_scan.Scan(sample["prefab"])
        root = scan.states.get("new")
        parts = []
        for row in scan.renderers():
            if row["mesh"] in (("asset", BEVEL_CUBE), ("builtin", "10202")) and not row["snow"] and not row["lod"] \
                    and (root is None or scan.under(row["node"], root)):
                parts.append(sorted(round(float(v), 3) for v in np.linalg.norm(row["matrix"][:3, :3], axis=0)))
        if parts:
            found[sample["name"]] = sorted(parts)
    return found
