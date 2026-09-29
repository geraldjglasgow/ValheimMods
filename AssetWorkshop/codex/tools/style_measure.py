"""What the style check measures on a built asset (assets/<name>/out): the manifest's triangles, texture size and size;
surface, UV area and texel density from the mesh (Blender dumps it once per build, see meshdump.py); and the baked
albedo over the texels the UVs cover: median value, median saturation and contrast, whole and per paint region.

Value and saturation are HSV of the sRGB texel (0 to 1); contrast is the spread of value between the 10th and the
90th percentile. Texel density is texture pixels per metre of surface, px * sqrt(UV area / surface area), the formula
codex/measure/game.py uses for the game's meshes. Blotch size (metres) and local contrast come from the codex's own
paint_stats (codex/measure), so they are measured exactly as the game's paint families were; they are left out when
that module does not import.
"""
import json
import os
import subprocess
import sys

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MESHDUMP = os.path.join(HERE, "meshdump.py")
sys.path.insert(0, os.path.join(HERE, "..", "measure"))
try:
    import paint_stats  # noqa: E402  (codex/measure; pulls in game.py, plain Python)
except Exception:          # a measuring module being rewritten must not stop the check
    paint_stats = None


def blender_path(given=None):
    return given or os.environ.get("WORKSHOP_BLENDER") or os.path.join(
        os.environ.get("USERPROFILE", ""), "tools", "blender", "blender.exe")


def asset(folder, blender=None):
    """Everything measured on the built asset in folder (its out/ must hold the manifest and the albedo)."""
    name = os.path.basename(os.path.normpath(folder))
    out = os.path.join(folder, "out")
    with open(os.path.join(out, name + ".json"), encoding="utf-8") as handle:
        manifest = json.load(handle)
    found = {"name": name, "manifest": manifest, "triangles": manifest.get("triangles"),
             "texture_px": manifest.get("textureSize"), "size_m": manifest.get("size"),
             "longest_m": max(manifest["size"]) if manifest.get("size") else None, "notes": []}
    tris = _mesh(out, name, blender, found["notes"])
    albedo = _rgb(os.path.join(out, manifest["albedo"]))
    covered = coverage(tris["uvs"], albedo.shape[1], albedo.shape[0]) if tris else np.any(albedo > 0, axis=2)
    if tris:
        found.update(surface(tris, found["texture_px"] or albedo.shape[1]))
    found.update(paint(albedo, covered))
    found.update(texture_shape(albedo, covered, found.get("texel_density")))
    found["regions"] = regions(out, manifest, albedo, covered, found.get("texel_density"))
    return found


def surface(tris, px):
    """Surface area (m2), UV area (0 to 1) and texel density (px per metre) of the dumped triangles."""
    p, uv = tris["positions"], tris["uvs"]
    world = np.linalg.norm(np.cross(p[:, 1] - p[:, 0], p[:, 2] - p[:, 0]), axis=1).sum() / 2
    a, b = uv[:, 1] - uv[:, 0], uv[:, 2] - uv[:, 0]
    area = np.abs(a[:, 0] * b[:, 1] - a[:, 1] * b[:, 0]).sum() / 2
    density = float(px * np.sqrt(area / world)) if world > 0 else None
    return {"surface_m2": float(world), "uv_area": float(area), "texel_density": density}


def coverage(uvs, width, height):
    """Bool (height, width): the texels some UV triangle covers (row 0 at the top, as the PNG stores it)."""
    image = Image.new("L", (width, height), 0)
    draw = ImageDraw.Draw(image)
    corners = np.stack([uvs[..., 0] * width, (1.0 - uvs[..., 1]) * height], axis=-1)
    for triangle in corners:
        draw.polygon([tuple(c) for c in triangle], fill=255)
    return np.asarray(image) > 0


def paint(rgb, covered):
    """Median value, median saturation and contrast of the covered texels, plus their share of the atlas."""
    texels = rgb[covered] / 255.0
    if not len(texels):
        return {"albedo_value": None, "albedo_saturation": None, "albedo_contrast": None, "uv_fill": 0.0}
    value = texels.max(axis=1)
    saturation = np.where(value > 0, (value - texels.min(axis=1)) / np.maximum(value, 1e-6), 0.0)
    low, high = np.percentile(value, [10, 90])
    return {"albedo_value": float(np.median(value)), "albedo_saturation": float(np.median(saturation)),
            "albedo_contrast": float(high - low), "uv_fill": float(covered.mean())}


def texture_shape(rgb, mask, density):
    """Correlation length (px), blotch size (m, from the texel density) and local contrast of the value under the
    mask, by codex/measure/paint_stats; {} when that module is missing or the mask is tiny."""
    if paint_stats is None or mask.sum() < 16:
        return {}
    value = rgb.max(axis=2) / 255.0
    try:
        corr = paint_stats.correlation_length(value, mask)
        found = {"correlation_px": corr, "local_contrast": paint_stats.local_contrast(value, mask)}
    except Exception:
        return {}
    if density and corr:
        found["blotch_m"] = 2 * corr / density
    return found


def regions(out, manifest, rgb, covered, density=None):
    """The manifest's paint regions with the paint measured under each (empty when the asset has none)."""
    mask_file = manifest.get("regionMask")
    if not manifest.get("regions") or not mask_file or not os.path.exists(os.path.join(out, mask_file)):
        return []
    ids = _rgb(os.path.join(out, mask_file))
    rows = []
    for region in manifest["regions"]:
        colour = np.array([int(region["colour"][i:i + 2], 16) for i in (1, 3, 5)])
        hit = np.all(ids == colour, axis=2) & covered
        rows.append(dict(region, **paint(rgb, hit), **texture_shape(rgb, hit, density)))
    return rows


def _mesh(out, name, blender, notes):
    """The dumped triangles, dumping them first when the .npz is missing or older than the .blend; None on failure."""
    blend, npz = os.path.join(out, name + ".blend"), os.path.join(out, "stylecheck", "mesh.npz")
    if not os.path.exists(blend):
        notes.append("no .blend in out/: texel density and UV coverage not measured")
        return None
    if not os.path.exists(npz) or os.path.getmtime(npz) < os.path.getmtime(blend):
        os.makedirs(os.path.dirname(npz), exist_ok=True)
        command = [blender_path(blender), "--background", "--factory-startup", blend, "--python", MESHDUMP,
                   "--", "--name", name, "--out", npz]
        result = subprocess.run(command, capture_output=True, text=True)
        if result.returncode != 0 or not os.path.exists(npz):
            notes.append("Blender could not dump the mesh: texel density and UV coverage not measured")
            return None
    with np.load(npz) as data:
        return {"positions": data["positions"], "uvs": data["uvs"]}


def _rgb(path):
    with Image.open(path) as image:
        return np.asarray(image.convert("RGB"), dtype=np.int32)
