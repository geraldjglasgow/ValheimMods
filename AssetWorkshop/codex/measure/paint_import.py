"""How the game imports and uses its textures: filter mode, mipmaps, compression, wrap and colour space from each
texture's .meta (AssetRipper writes the runtime texture's settings there), per map slot; and albedo sizes and texel
densities per kind of asset (item, piece, creature, env, location).

    python paint_import.py        # prints a summary; paint.py puts it into data/paint.json
"""
import collections
import os
import re

import numpy as np

import game
import paint_uv
import shaders_usage

FORMATS = {3: "RGB24", 4: "RGBA32", 5: "ARGB32", 7: "RGB565", 10: "DXT1", 12: "DXT5", 24: "BC6H", 25: "BC7",
           26: "BC4", 27: "BC5", 28: "DXT1Crunched", 29: "DXT5Crunched", 63: "R8"}
FILTERS = {0: "point", 1: "bilinear", 2: "trilinear"}
SLOTS = {"_MainTex": "albedo", "_BumpMap": "normal", "_MetallicGlossMap": "metal", "_MetallicTex": "metal",
         "_MetalTex": "metal", "_EmissionMap": "emission", "_EmissiveTex": "emission", "_StyleTex": "style",
         "_MossTex": "moss"}
KINDS = ("item", "piece", "creature", "env", "location")


def settings(path):
    """The import settings of one texture from its .meta."""
    try:
        text = game.unity.read(path + ".meta")
    except OSError:
        return None
    number = lambda name: int(float(re.search(rf"^\s*{name}: (-?[\d.]+)", text, re.M).group(1)))
    return {"filter": FILTERS.get(number("filterMode"), "?"), "mipmaps": number("enableMipMap") == 1,
            "srgb": number("sRGBTexture") == 1,
            "format": FORMATS.get(number("textureFormat"), str(number("textureFormat"))),
            "aniso": number("aniso"), "wrap": "clamp" if number("wrapU") == 1 else "repeat",
            "normal_type": number("textureType") == 1}


def survey():
    """Counts of each setting per map slot over every texture the game's prefabs draw."""
    by_slot = collections.defaultdict(set)
    for material, entry in shaders_usage.scan().items():
        if not set(entry["kinds"]) - {"other"}:
            continue
        try:
            textures = game.material(material)["textures"]
        except (OSError, ValueError):
            continue
        for slot, path in textures.items():
            if path and slot in SLOTS:
                by_slot[SLOTS[slot]].add(path)
    return {slot: _count(paths) for slot, paths in sorted(by_slot.items())}


def _count(paths):
    found = [s for s in (settings(p) for p in sorted(paths)) if s]
    table = {"textures": len(found)}
    for key in ("filter", "mipmaps", "srgb", "format", "aniso", "wrap"):
        table[key] = dict(collections.Counter(str(s[key]) for s in found).most_common())
    return table


def per_kind():
    """Albedo size and texel density (px per metre) per kind of asset, over distinct mesh uses."""
    sizes, densities = collections.defaultdict(list), collections.defaultdict(list)
    for texture, uses in paint_uv.uses().items():
        size = game.texture_size(texture)
        if not size:
            continue
        for use in uses:
            for kind in set(use["kind"]) & set(KINDS):
                sizes[kind].append(max(size))
                density = _safe_density(use, size[0])
                if density:
                    densities[kind].append(density)
    return {kind: {"texture_px": _five(sizes[kind]), "texel_density": _five(densities[kind])} for kind in KINDS}


def _safe_density(use, width):
    try:
        density = paint_uv.density(use, width)
    except (ValueError, IndexError):
        return None
    return density if density and 2.0 < density < 2000.0 else None


def _five(values):
    if not values:
        return None
    p = np.percentile(values, (0, 25, 50, 75, 100))
    return {"n": len(values), "min": round(float(p[0]), 1), "p25": round(float(p[1]), 1),
            "median": round(float(p[2]), 1), "p75": round(float(p[3]), 1), "max": round(float(p[4]), 1)}


if __name__ == "__main__":
    import json
    print(json.dumps(survey(), indent=1))
    print(json.dumps(per_kind(), indent=1))
