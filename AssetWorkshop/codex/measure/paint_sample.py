"""Everything the codex measures on one family sample: where it is used and how big, its tones and colours after the
material's tint, how its variation splits by blotch size, the light painted into it, and what its material adds (metal
mask and gloss, emission, alpha cutout, normal strength).

    record = paint_sample.measure(sample)
"""
import os

import numpy as np

import game
import paint_image
import paint_light
import paint_stats
import paint_uv


def measure(sample):
    """One sample's record (see the module doc); None when its mask picks fewer than 16 texels."""
    rgba = paint_image.load(sample["texture"])
    mask = paint_image.sample_mask(sample, rgba)
    if mask.sum() < 16:
        return None
    material = paint_image.sample_material(sample)
    info = game.material(material) if material else {"floats": {}, "colors": {}, "textures": {}, "shader": "?"}
    lin = paint_image.linear(rgba[..., :3]) * _tint(info)
    value = paint_image.hsv(paint_image.to_srgb(lin))[..., 2]
    record = _where(sample, rgba, mask, material, info)
    record.update(paint_stats.tones(lin, mask))
    record.update(_texture_shape(value, mask, record["density"]))
    record["light"] = _light(sample, info, value, mask, rgba.shape)
    record["material_values"] = _material_values(sample, info, rgba, mask)
    return record


def _tint(info):
    """The material's colour as a linear multiplier (the game renders in linear space)."""
    colour = info["colors"].get("_Color", (1.0, 1.0, 1.0, 1.0))[:3]
    return paint_image.linear(np.array(colour, dtype=np.float64))


def _where(sample, rgba, mask, material, info):
    """Which texture and material, how big, which kinds of asset draw it, and its texel density."""
    uses = paint_image.sample_uses(sample)
    height, width = rgba.shape[:2]
    densities = [(d, u["count"]) for u in uses for d in [_density(u, width)] if d]
    colour = info["colors"].get("_Color", (1, 1, 1, 1))
    return {"texture": sample["texture"], "prefab": sample.get("prefab"), "crop": sample.get("crop"),
            "mask": sample.get("mask"), "material": material, "shader": info["shader"], "size": [width, height],
            "texels": int(mask.sum()), "coverage": round(float(mask.mean()), 3),
            "kinds": sorted({k for u in uses for k in u["kind"]}),
            "density": _weighted_median(densities), "tint": [round(c, 3) for c in colour[:3]]}


def _density(use, width):
    """Pixels per metre of one use; None when its mesh does not decode or the value is absurd (a mesh with
    degenerate UVs)."""
    try:
        density = paint_uv.density(use, width)
    except (ValueError, IndexError):
        return None
    return density if density and 2.0 < density < 2000.0 else None


def _weighted_median(pairs):
    if not pairs:
        return None
    pairs = sorted(pairs)
    total, running = sum(w for _, w in pairs), 0
    for value, weight in pairs:
        running += weight
        if running >= total / 2:
            return round(float(value), 1)
    return round(float(pairs[-1][0]), 1)


def _texture_shape(value, mask, density):
    """Blotch sizes: variance per octave, correlation length in pixels and metres, local contrast, histograms."""
    corr = paint_stats.correlation_length(value, mask)
    shape = {"bands": paint_stats.bands(value, mask), "correlation_px": corr,
             "local_contrast": paint_stats.local_contrast(value, mask),
             "value_histogram": _histogram(value[mask])}
    if density:
        shape["blotch_m"] = round(2 * corr / density, 3)
        shape["blotch_cycles_per_m"] = round(density / (2 * corr), 2)
    return shape


def _histogram(values, bins=10):
    counts, _ = np.histogram(values, bins=bins, range=(0.0, 1.0))
    return [round(float(c) / max(len(values), 1), 3) for c in counts]


def _light(sample, info, value, mask, shape):
    """Painted light against the normal map (when the material has one) and against the mesh's up."""
    found = {}
    normal = paint_image.normal_map(paint_image.sample_material(sample), (shape[1], shape[0]))
    if normal is not None:
        found["normal"] = paint_light.normal_relation(value, normal, mask)
        found["normal"]["map"] = info["textures"].get("_BumpMap")
        found["normal"]["bump_scale"] = info["floats"].get("_BumpScale")
    facing = paint_light.facing(sample["texture"], paint_image.sample_uses(sample), (shape[1], shape[0]), value, mask)
    if facing:
        found["facing"] = facing
    return found


def _material_values(sample, info, rgba, mask):
    """Gloss, metal, emission and cutout settings of the material, and the metal mask's channels under the mask."""
    floats, colours = info["floats"], info["colors"]
    keep = ("_Glossiness", "_Metallic", "_MetalGloss", "_MetallicAlphaGloss", "_UseGlossmap", "_BumpScale",
            "_Cutoff", "_Cull", "_Hue", "_Saturation", "_Value", "_UseStyles", "_AddRain", "_AddSnow")
    found = {k: round(floats[k], 3) for k in keep if k in floats}
    for name in ("_MetalColor", "_EmissionColor"):
        if name in colours:
            found[name] = [round(c, 3) for c in colours[name]]
    found["maps"] = sorted(k for k, v in info["textures"].items() if v)
    metal = _metal_channels(sample, rgba.shape, mask)
    if metal:
        found["metal_mask"] = metal
    found["alpha_cut"] = round(float((rgba[..., 3] < 0.5).mean()), 3) if rgba[..., 3].min() < 0.99 else 0.0
    return found


def _metal_channels(sample, shape, mask):
    """Mean red (metal) and alpha (gloss) of the metal mask over the sample's texels, and the metal fraction."""
    path = paint_image.metal_texture(paint_image.sample_material(sample))
    if not path:
        return None
    from PIL import Image
    with Image.open(os.path.join(game.ROOT, path)) as image:
        channels = np.asarray(image.convert("RGBA").resize((shape[1], shape[0]), Image.NEAREST), dtype=np.float64)
    channels = channels[mask] / 255.0
    return {"texture": path, "metal_fraction": round(float((channels[:, 0] > 0.5).mean()), 3),
            "red_mean": round(float(channels[:, 0].mean()), 3), "alpha_mean": round(float(channels[:, 3].mean()), 3)}
