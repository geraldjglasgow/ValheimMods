"""Each biome's look as numbers, for environment.py: the terrain's texture array (sixteen 256 px slices, sampled for
their 10, 50 and 90 % tones), the colours of the textures its own props are painted with (bark, leaves, rock, grass,
weighted by how many the zone system places), the weather it cycles through and each weather's light and fog.

Colours are sRGB hex. Texture tones come from the exported PNGs (sRGB). Environment colours are the values stored in
EnvSetup, shown as hex of the stored 0 to 1 numbers (what the game's inspector shows).
"""
import functools
import os

import numpy as np
from PIL import Image

import environment_read as read
import environment_zone as zone
import game

TERRAIN = "world/terrain/terrain_d_array.png"
TERRAIN_NOTES = {  # what each slice shows, from looking at it (out/environment/terrain_slices.png) and the old
    0: "green grass (Meadows)", 1: "moss-green patches on brown (forest floor)",          # named source textures
    2: "olive dirt, alpha-masked (dirt)", 3: "brown dirt (cleared ground)", 4: "grey mottled stone",
    5: "grey faceted rock, height in alpha (cliff)", 6: "flat orange placeholder: the snow slot, drawn by the shader",
    7: "near-black ash", 8: "yellow-olive grass (Plains heath)", 9: "sand (beach)",
    10: "dark brown leaf litter, alpha-masked (Black Forest floor)", 11: "yellow-green grass with flowers",
    12: "grey cobbles (paved)", 13: "dark grey-green stone", 14: "dark cracked rock",
    15: "black marble with white veins"}
TONES = (10, 50, 90)


def hex_of(rgb):
    return "#" + "".join(f"{int(round(max(0, min(255, c)))):02x}" for c in rgb[:3])


def tones(pixels):
    """{'10': hex, '50': hex, '90': hex}: the mean colour of the pixels around each luminance percentile."""
    lum = pixels @ np.array([0.2126, 0.7152, 0.0722])
    order = np.argsort(lum)
    found = {}
    for pct in TONES:
        at = int(len(order) * pct / 100)
        band = order[max(0, at - len(order) // 40): at + len(order) // 40 + 1]
        found[str(pct)] = hex_of(pixels[band].mean(axis=0))
    return found


@functools.lru_cache(maxsize=None)
def texture_tones(path):
    """Tones of a texture's visible pixels (alpha above 0.5 when it has alpha), or None when it will not open."""
    try:
        with Image.open(os.path.join(game.ROOT, path)) as image:
            rgba = np.asarray(image.convert("RGBA"), dtype=np.float64).reshape(-1, 4)
    except OSError:
        return None
    visible = rgba[rgba[:, 3] > 127] if (rgba[:, 3] < 250).mean() > 0.02 else rgba
    return tones(visible[:, :3]) if len(visible) else None


def terrain():
    """[{slice, looks, tones}] of the terrain's texture array."""
    with Image.open(os.path.join(game.ROOT, TERRAIN)) as image:
        rgb = np.asarray(image.convert("RGB"), dtype=np.float64)
    size = rgb.shape[1]
    return [{"slice": i, "looks": TERRAIN_NOTES.get(i, ""),
             "tones": tones(rgb[i * size:(i + 1) * size].reshape(-1, 3))} for i in range(rgb.shape[0] // size)]


def biome_materials(biome):
    """{material path: weight} of the materials on what the zone and clutter systems place in a biome, weighted by
    the most placed per zone and the material's share of the prefab's surface."""
    weights = {}
    for entry in zone.vegetation() + zone.clutter():
        if biome not in entry["biomes"] or not entry["prefab"] or not entry["prefab"].startswith("world/"):
            continue
        count = entry.get("per_zone", [0, entry.get("amount", 0)])[1]
        for path, share in _material_shares(entry["prefab"]):
            weights[path] = weights.get(path, 0.0) + count * share
    return dict(sorted(weights.items(), key=lambda kv: -kv[1]))


@functools.lru_cache(maxsize=None)
def _material_shares(prefab_path):
    """[(material path, share of the drawn surface)] of a prefab (1.0 for an instanced clutter prefab)."""
    pf = game.prefab(prefab_path)
    if "InstanceRenderer" in read.scripts(pf):
        body = next(b for _, k, b in pf.all_components() if k == "InstanceRenderer")
        return [(game.path_of(game.unity.field(body, "m_material")), 1.0)]
    drawn, matrices = read.renderers(pf), read.world_matrices(pf)
    near = read.lod_split(pf, drawn)[1]
    areas = {}
    for r in near:
        for sub, mat in zip(read.submeshes(r["mesh"]), r["materials"]):
            got = read.surface(r["mesh"], sub, read.mean_scale(matrices[r["node"]])) if mat else None
            if got:
                areas[mat] = areas.get(mat, 0.0) + got[0]
    total = sum(areas.values())
    return [(m, a / total) for m, a in areas.items()] if total else []


def biome_palette(biome, limit=8):
    """The biome's most used prop materials: [{material, shader, texture, weight share, tones}]."""
    weights = biome_materials(biome)
    total = sum(weights.values()) or 1.0
    rows = []
    for path, weight in list(weights.items())[:limit]:
        info = read.material_info(path)
        rows.append({"material": info["name"], "shader": info["shader"], "texture": info["main"],
                     "share": round(weight / total, 3), "tint": info["tint"][:3],
                     "tones": texture_tones(info["main"]) if info["main"] else None})
    return rows


def environments():
    """{env: {colour hex per field, numbers, flags}} for every environment the biomes use."""
    used = {env for pairs in zone.biome_weather().values() for env, _ in pairs}
    found = {}
    for name, env in zone.environments().items():
        if name in used:
            found[name] = {"colours": {k: hex_of([c * 255 for c in v]) for k, v in env["colours"].items()},
                           "numbers": env["numbers"], "flags": env["flags"], "list": env["list"]}
    return found


def biomes():
    """{biome: {weather: [(env, weight)], palette: [...], clutter: [names], placed: [(name, per zone)]}}."""
    found = {}
    for biome in zone.BIOMES.values():
        placed = [(e["name"], e["per_zone"][1]) for e in zone.vegetation() if biome in e["biomes"]]
        found[biome] = {"weather": zone.biome_weather().get(biome, []), "palette": biome_palette(biome),
                        "clutter": [c["name"] for c in zone.clutter() if biome in c["biomes"]],
                        "placed": sorted(placed, key=lambda kv: -kv[1])[:20]}
    return found
