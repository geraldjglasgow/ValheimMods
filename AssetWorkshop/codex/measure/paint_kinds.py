"""Albedo statistics per kind of asset (item, piece, creature, env, location, all), over every albedo texture the game's
prefabs draw: the same numbers paint_sample takes of a family sample (value, saturation, span, local contrast), so the
style check has a category-level range where no material family is named. A texture counts for the kind that draws it
most often; its texels are those its UVs cover, times its most-used material's tint.

    paint_kinds.categories()   # {"item": {"label", "stats": {...}}, ..., "all": {...}}
"""
import collections

import numpy as np

import paint_image
import paint_stats
import paint_uv

KINDS = ("item", "piece", "creature", "env", "location")
KEYS = ("value_p50", "saturation", "value_span", "local_contrast")


def texture_numbers(texture, uses):
    """The texture's value median, saturation median, value span and local contrast; None when unreadable."""
    try:
        rgba = paint_image.load(texture)
    except OSError:
        return None
    sample = {"texture": texture, "material": max(uses, key=lambda u: u["count"])["material"]}
    mask = paint_image.sample_mask(sample, rgba)
    if mask.sum() < 16:
        return None
    tint = paint_image.game.material(sample["material"])["colors"].get("_Color", (1, 1, 1, 1))[:3]
    lin = paint_image.linear(rgba[..., :3]) * paint_image.linear(np.array(tint, dtype=np.float64))
    hsv = paint_image.hsv(paint_image.to_srgb(lin))
    value, saturation = hsv[..., 2], hsv[..., 1]
    low, mid, high = np.percentile(value[mask], (10, 50, 90))
    return {"value_p50": float(mid), "saturation": float(np.median(saturation[mask])),
            "value_span": float(high - low), "local_contrast": paint_stats.local_contrast(value, mask)}


def _kind(uses):
    counts = collections.Counter()
    for use in uses:
        for kind in use["kind"]:
            counts[kind] += use["count"]
    ranked = [k for k, _ in counts.most_common() if k in KINDS]
    return ranked[0] if ranked else None


def categories():
    """{kind: {"label", "stats"}} plus "all", five-number summaries of each key over the kind's textures."""
    found = collections.defaultdict(list)
    for texture, uses in sorted(paint_uv.uses().items()):
        kind = _kind(uses)
        numbers = texture_numbers(texture, uses) if kind else None
        if numbers:
            found[kind].append(numbers)
            found["all"].append(numbers)
    return {kind: {"label": f"every {kind} albedo" if kind != "all" else "every albedo the game's prefabs draw",
                   "stats": {key: _five([n[key] for n in rows]) for key in KEYS}}
            for kind, rows in sorted(found.items())}


def _five(values):
    p = np.percentile(values, (0, 25, 50, 75, 100))
    return {"n": len(values), "min": round(float(p[0]), 3), "p25": round(float(p[1]), 3),
            "median": round(float(p[2]), 3), "p75": round(float(p[3]), 3), "max": round(float(p[4]), 3)}
