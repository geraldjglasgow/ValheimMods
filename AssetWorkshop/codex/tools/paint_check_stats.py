"""Statistics of a baked swatch, the same ones codex/measure takes of the game's textures (paint_stats, paint_light),
and their comparison with the family's measured numbers from data/paint.json."""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "measure"))

import paint_image  # noqa: E402
import paint_light  # noqa: E402
import paint_stats  # noqa: E402

# (swatch key, family stats key, tolerance: how far off still counts as a match, "rel" for a ratio)
CHECKS = [("value_p10", "value", 0.06), ("value_p50", "value_p50", 0.06), ("value_p90", "value_p90", 0.07),
          ("saturation", "saturation", 0.08), ("value_span", "value_span", 0.06),
          ("blotch_m", "blotch_m", "rel"), ("local_contrast", "local_contrast", 0.15),
          ("fine", "fine", 0.1), ("blotchy", "blotchy", 0.1),
          ("normal_from_albedo", "normal_from_albedo", 0.25), ("normal_strength", "normal_strength", "rel"),
          ("hooked_from_albedo", "normal_from_albedo", 0.25), ("hooked_strength", "normal_strength", "rel")]


def coverage(triangles, size):
    """bool (h, w): the texels the swatch's UV triangles cover."""
    canvas = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(canvas)
    for tri in triangles:
        draw.polygon([(tri[i] * size, (1.0 - tri[i + 1]) * size) for i in (0, 2, 4)], fill=255)
    return np.asarray(canvas) > 0


def uv_area(triangles):
    t = np.array(triangles).reshape(-1, 3, 2)
    a, b = t[:, 1] - t[:, 0], t[:, 2] - t[:, 0]
    return float(np.abs(a[:, 0] * b[:, 1] - a[:, 1] * b[:, 0]).sum() / 2)


def measure(albedo_path, normal_path, uv):
    """The swatch's numbers: tones, contrast, bands, blotch size in metres, local contrast, normal-from-albedo."""
    with Image.open(albedo_path) as image:
        rgb = np.asarray(image.convert("RGB"), dtype=np.float64) / 255.0
    size = rgb.shape[0]
    mask = coverage(uv["triangles"], size)
    density = size * np.sqrt(uv_area(uv["triangles"]) / uv["area"])
    lin = paint_image.linear(rgb)
    value = paint_image.hsv(rgb)[..., 2]
    tones = paint_stats.tones(lin, mask)
    bands = paint_stats.bands(value, mask)
    corr = paint_stats.correlation_length(value, mask)
    found = {"texture_px": size, "density": round(float(density), 1), "texels": int(mask.sum()),
             "value_p10": tones["value"]["p10"], "value_p50": tones["value"]["p50"], "value_p90": tones["value"]["p90"],
             "saturation": tones["saturation"]["p50"], "value_span": tones["contrast"]["value_span"],
             "hue": tones["hue"]["mean"], "colours": tones["colours"], "bands": bands, "fine": _share(bands, "fine"),
             "blotchy": _share(bands, "4-8", "8-16"), "correlation_px": corr, "blotch_m": round(2 * corr / density, 3),
             "local_contrast": paint_stats.local_contrast(value, mask)}
    found.update(_normal(normal_path, value, mask, size))
    hooked = normal_path.replace("_normal.png", "_normal_hooked.png")
    if os.path.exists(hooked):
        found.update({"hooked_" + k.replace("normal_", ""): v for k, v in _normal(hooked, value, mask, size).items()})
    return found


SMALL = ("fine", "2-4", "4-8", "8-16")


def _share(bands, *names):
    """Share of the variance up to 16 px that lies in these bands: comparable between a small swatch atlas and the
    game's bigger textures, which also hold blotches larger than the swatch's whole atlas."""
    total = sum(bands[b] for b in SMALL) or 1.0
    return round(sum(bands[b] for b in names) / total, 3)


def _normal(path, value, mask, size):
    if not os.path.exists(path):
        return {}
    with Image.open(path) as image:
        normal = np.asarray(image.convert("RGB").resize((size, size), Image.NEAREST), dtype=np.float64) / 255.0 * 2 - 1
    relation = paint_light.normal_relation(value, normal, mask)
    return {"normal_from_albedo": relation["from_albedo_corr"], "normal_strength": relation["from_albedo_strength"],
            "normal_tilt_deg": relation["tilt_mean_deg"]}


def compare(swatch, family_stats):
    """[(name, swatch value, game median, game min, game max, verdict)] for every check the family has numbers for.
    The normal checks are skipped where the game's own normal maps are not the albedo's relief (median correlation
    under 0.5: copper, leather, marble, moss ...), since there is nothing to match."""
    rows = []
    derived = (family_stats.get("normal_from_albedo") or {}).get("median", 0.0) >= 0.5
    for key, target, tolerance in CHECKS:
        if target.startswith("normal_") and not derived:
            continue
        game = _game(family_stats, target)
        mine = swatch.get(key)
        if game is None or mine is None:
            continue
        median, low, high = game
        rows.append((key, mine, median, low, high, _verdict(mine, median, low, high, tolerance)))
    return rows


def _game(stats, target):
    if target in ("fine", "blotchy"):
        bands = stats.get("bands")
        names = ("fine",) if target == "fine" else ("4-8", "8-16")
        return (_share(bands, *names), None, None) if bands else None
    entry = stats.get(target)
    return (entry["median"], entry["min"], entry["max"]) if entry else None


def _verdict(mine, median, low, high, tolerance):
    """'ok' within tolerance of the game's median (a ratio of 0.67 to 1.5 for 'rel'), 'range' within the game's
    own spread of samples, else 'off'."""
    if tolerance == "rel":
        close = median > 0 and 0.67 <= mine / median <= 1.5
    else:
        close = abs(mine - median) <= tolerance
    if close:
        return "ok"
    if low is not None and low <= mine <= high:
        return "range"
    return "off"
