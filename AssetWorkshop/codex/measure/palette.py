"""Writes codex/data/palette.json: sampled colours per material family (from data/paint.json, which paint.py writes
first) and per biome (palette_biomes, measured here): the 10, 50 and 90 % luminance tones in linear RGB and sRGB hex,
the hue range, saturation and value, and the textures they came from.

    python codex/measure/paint.py && python codex/measure/palette.py
"""
import datetime
import json
import os

import numpy as np

import game
import paint_aggregate
import paint_image
import paint_stats
import palette_biomes

PAINT = os.path.join(game.WORKSHOP, "codex", "data", "paint.json")
DATA = os.path.join(game.WORKSHOP, "codex", "data", "palette.json")


def family_palette(family):
    """One family's palette from its measured samples."""
    samples, stats = family["samples"], family["stats"]
    return {"label": family["label"], "tones": stats["colours"], "hue": _hue_range(samples),
            "saturation": _median_percentiles(samples, "saturation"), "value": _median_percentiles(samples, "value"),
            "sources": [{"texture": os.path.basename(s["texture"]), "tones": [s["colours"][p]["hex"] for p in
                                                                              ("p10", "p50", "p90")]}
                        for s in samples]}


def _hue_range(samples):
    """Circular mean of the samples' mean hues and the widest 10-90 % span among them (degrees)."""
    mean = paint_aggregate._circular([s["hue"]["mean"] for s in samples if s["hue"]["mean"] is not None])
    lows = [s["hue"]["p10"] for s in samples if s["hue"]["p10"] is not None]
    highs = [s["hue"]["p90"] for s in samples if s["hue"]["p90"] is not None]
    return {"mean": mean["mean"] if mean else None, "spread": mean["spread"] if mean else None,
            "p10_median": _circular_median(lows), "p90_median": _circular_median(highs)}


def _circular_median(degrees):
    found = paint_aggregate._circular(degrees)
    return found["mean"] if found else None


def _median_percentiles(samples, key):
    return {p: round(float(np.median([s[key][p] for s in samples])), 3) for p in ("p10", "p50", "p90")}


def measure_tones(sample):
    """Tones of one biome sample: masked texels of the texture times its material's tint."""
    rgba = paint_image.load(sample["texture"])
    mask = paint_image.sample_mask(sample, rgba)
    material = paint_image.sample_material(sample)
    tint = game.material(material)["colors"].get("_Color", (1, 1, 1, 1))[:3] if material else (1, 1, 1)
    lin = paint_image.linear(rgba[..., :3]) * paint_image.linear(np.array(tint, dtype=np.float64))
    tones = paint_stats.tones(lin, mask)
    tones["texture"] = os.path.basename(sample["texture"])
    return tones


def biome_palette(roles):
    """{role: {tones, hue, saturation, value, sources}} and an overall entry pooling the roles' samples."""
    found, pooled = {}, []
    for role, samples in roles.items():
        measured = [measure_tones(s) for s in samples]
        pooled += measured
        found[role] = _summary(measured)
    found["overall"] = _summary(pooled)
    return found


def _summary(measured):
    return {"tones": {p: paint_aggregate._colour([m["colours"][p]["linear"] for m in measured]) for p in
                      ("p10", "p50", "p90")},
            "hue": _hue_range(measured), "saturation": _median_percentiles(measured, "saturation"),
            "value": _median_percentiles(measured, "value"),
            "sources": [{"texture": m["texture"], "tones": [m["colours"][p]["hex"] for p in ("p10", "p50", "p90")]}
                        for m in measured]}


def main():
    with open(PAINT, encoding="utf-8") as handle:
        paint = json.load(handle)
    data = {"topic": "palette", "generated": datetime.date.today().isoformat(), "script": "measure/palette.py",
            "families": {name: family_palette(f) for name, f in paint["families"].items() if f["samples"]},
            "biomes": {name: biome_palette(roles) for name, roles in palette_biomes.BIOMES.items()}}
    with open(DATA, "w", encoding="utf-8") as handle:
        json.dump(data, handle, indent=1)
    print(DATA)


if __name__ == "__main__":
    main()
