"""Textures of the reference export as numpy arrays, and the masks that pick the texels a measurement is about: the ones
the meshes' UVs cover, the metal or non-metal ones by the material's metal mask, a hand-picked rectangle, the opaque
ones of a cutout. Plain numpy and Pillow.

    rgba = paint_image.load("Characters/Skeleton/model/Texture/Skeleton_d.tga")   # (h, w, 4) sRGB 0..1
    mask = paint_image.sample_mask(sample, rgba)                                   # bool (h, w)
    hsv = paint_image.hsv(rgba[..., :3])
"""
import functools
import os

import numpy as np
from PIL import Image

import game
import paint_uv

METAL_SLOTS = ("_MetallicGlossMap", "_MetallicTex", "_MetalTex")


@functools.lru_cache(maxsize=None)
def load(path):
    """(h, w, 4) float array, sRGB values 0..1, row 0 at the top (v = 1)."""
    with Image.open(os.path.join(game.ROOT, path)) as image:
        return np.asarray(image.convert("RGBA"), dtype=np.float64) / 255.0


def linear(srgb):
    """sRGB 0..1 -> linear 0..1."""
    return np.where(srgb <= 0.04045, srgb / 12.92, ((srgb + 0.055) / 1.055) ** 2.4)


def to_srgb(lin):
    lin = np.clip(lin, 0.0, 1.0)
    return np.where(lin <= 0.0031308, lin * 12.92, 1.055 * lin ** (1 / 2.4) - 0.055)


def hsv(rgb):
    """(h, w, 3) or (n, 3) sRGB -> hue in degrees 0..360, saturation 0..1, value 0..1 (same leading shape)."""
    top, low = rgb.max(axis=-1), rgb.min(axis=-1)
    span = top - low
    saturation = np.where(top > 0, span / np.maximum(top, 1e-9), 0.0)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    safe = np.maximum(span, 1e-9)
    hue = np.where(top == r, ((g - b) / safe) % 6, np.where(top == g, (b - r) / safe + 2, (r - g) / safe + 4))
    return np.stack([np.where(span > 0, hue * 60.0, 0.0), saturation, top], axis=-1)


def luminance(rgb_linear):
    """Rec. 709 relative luminance of linear RGB."""
    return rgb_linear[..., 0] * 0.2126 + rgb_linear[..., 1] * 0.7152 + rgb_linear[..., 2] * 0.0722


def sample_mask(sample, rgba):
    """The texels one family sample measures: UV coverage, then its rectangle, metal choice and alpha cut."""
    height, width = rgba.shape[:2]
    mask = _covered(sample, (width, height))
    if sample.get("crop"):
        mask &= _rectangle(sample["crop"], (width, height))
    if sample.get("mask") in ("metal", "nonmetal"):
        metal = metal_mask(sample, (width, height))
        if metal is not None:
            mask &= metal if sample["mask"] == "metal" else ~metal
    if rgba[..., 3].min() < 0.99:
        mask &= rgba[..., 3] >= 0.5
    return mask


def _covered(sample, size):
    """The UV coverage (of the sample's prefab only, when it names one), or every texel when no mesh draws the
    texture or the UVs cover under 5 % of it."""
    covered = paint_uv.coverage(sample["texture"], size, sample_uses(sample))
    return covered if covered.sum() >= 16 else np.ones(covered.shape, dtype=bool)


def sample_uses(sample):
    """The texture's uses, only the sample's prefab's when it names one."""
    uses = paint_uv.uses().get(sample["texture"], [])
    if sample.get("prefab"):
        uses = [u for u in uses if os.path.basename(u["prefab"]) == sample["prefab"] + ".prefab"]
    return uses


def sample_material(sample):
    """The sample's own material, or the one drawing its texture most often (one with a metal mask first when the
    sample picks by metal)."""
    if sample.get("material"):
        return sample["material"]
    counts = {}
    for use in sample_uses(sample):
        counts[use["material"]] = counts.get(use["material"], 0) + use["count"]
    ranked = sorted(counts, key=lambda m: (-(bool(metal_texture(m)) and bool(sample.get("mask"))), -counts[m], m))
    return ranked[0] if ranked else None


def _rectangle(crop, size):
    """A crop (x0, y0, x1, y1) in fractions of the texture, from the top left, as a mask."""
    width, height = size
    x0, y0, x1, y1 = crop
    mask = np.zeros((height, width), dtype=bool)
    mask[int(round(y0 * height)):int(round(y1 * height)), int(round(x0 * width)):int(round(x1 * width))] = True
    return mask


def metal_mask(sample, size):
    """bool (h, w): the metal mask's red channel above one half, resized to the albedo; None without a mask."""
    path = metal_texture(sample_material(sample))
    if not path:
        return None
    width, height = size
    with Image.open(os.path.join(game.ROOT, path)) as image:
        red = np.asarray(image.convert("RGBA").resize((width, height), Image.NEAREST), dtype=np.float64)[..., 0]
    return red / 255.0 > 0.5


def metal_texture(material):
    """The metal mask texture of a material, None when it has none."""
    if not material:
        return None
    textures = game.material(material)["textures"]
    return next((textures[slot] for slot in METAL_SLOTS if textures.get(slot)), None)


def normal_map(material, size):
    """(h, w, 3) tangent-space normals (x right, y up the texture) resized to the albedo, or None."""
    path = game.material(material)["textures"].get("_BumpMap") if material else None
    if not path:
        return None
    width, height = size
    with Image.open(os.path.join(game.ROOT, path)) as image:
        rgb = np.asarray(image.convert("RGB").resize((width, height), Image.NEAREST), dtype=np.float64) / 255.0
    normal = rgb * 2.0 - 1.0
    normal[..., 2] = np.sqrt(np.clip(1.0 - normal[..., 0] ** 2 - normal[..., 1] ** 2, 0.0, 1.0))
    return normal
