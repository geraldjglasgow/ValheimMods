"""Folds a family's sample records into the family's numbers: the median, least and greatest over samples of every
measured value, circular means for hues and light directions, mean histograms and per-channel median colours."""
import numpy as np

SCALARS = {
    "value": ("value", "p10"), "value_p50": ("value", "p50"), "value_p90": ("value", "p90"),
    "saturation_p10": ("saturation", "p10"), "saturation": ("saturation", "p50"),
    "saturation_p90": ("saturation", "p90"),
    "luminance_p10": ("luminance", "p10"), "luminance": ("luminance", "p50"), "luminance_p90": ("luminance", "p90"),
    "value_span": ("contrast", "value_span"), "luminance_ratio": ("contrast", "luminance_ratio"),
    "correlation_px": ("correlation_px",), "local_contrast": ("local_contrast",), "blotch_m": ("blotch_m",),
    "blotch_cycles_per_m": ("blotch_cycles_per_m",), "texel_density": ("density",), "texture_px": ("size", 0),
    "normal_tilt_deg": ("light", "normal", "tilt_mean_deg"), "normal_flat": ("light", "normal", "flat_fraction"),
    "light_strength": ("light", "normal", "light_strength"),
    "value_vs_curvature": ("light", "normal", "value_vs_curvature"),
    "value_vs_height": ("light", "normal", "value_vs_height"), "detail_shared": ("light", "normal", "detail_shared"),
    "bump_scale": ("light", "normal", "bump_scale"),
    "normal_from_albedo": ("light", "normal", "from_albedo_corr"),
    "normal_strength": ("light", "normal", "from_albedo_strength"), "up_value": ("light", "facing", "up"),
    "side_value": ("light", "facing", "side"), "down_value": ("light", "facing", "down"),
    "value_vs_up": ("light", "facing", "value_vs_up"), "glossiness": ("material_values", "_Glossiness"),
    "metallic": ("material_values", "_Metallic"), "metal_gloss": ("material_values", "_MetalGloss"),
    "metal_red": ("material_values", "metal_mask", "red_mean"),
    "metal_alpha": ("material_values", "metal_mask", "alpha_mean"), "alpha_cut": ("material_values", "alpha_cut"),
}
BANDS = ("fine", "2-4", "4-8", "8-16", "16-32", "32-64", "64+")


def family_stats(records):
    """{name: {n, median, min, max}} over the records, plus hue, bands, histogram, colours and kinds."""
    stats = {name: _five(_values(records, path)) for name, path in SCALARS.items()}
    stats = {k: v for k, v in stats.items() if v}
    stats["hue"] = _circular([r["hue"]["mean"] for r in records if r["hue"]["mean"] is not None])
    stats["light_direction_deg"] = _circular(_values(records, ("light", "normal", "light_direction_deg")))
    stats["bands"] = {b: round(float(np.median([r["bands"][b] for r in records])), 3) for b in BANDS}
    stats["bands"]["dominant"] = max(BANDS, key=lambda b: stats["bands"][b])
    stats["value_histogram"] = [round(float(x), 3) for x in np.mean([r["value_histogram"] for r in records], axis=0)]
    stats["colours"] = {p: _colour([r["colours"][p]["linear"] for r in records]) for p in ("p10", "p50", "p90")}
    stats["kinds"] = sorted({k for r in records for k in r["kinds"]})
    stats["emissive"] = sum(1 for r in records if _emissive(r))
    return stats


def _values(records, path):
    found = []
    for record in records:
        value = record
        for key in path:
            if isinstance(value, dict):
                value = value.get(key)
            elif isinstance(value, list) and isinstance(key, int):
                value = value[key]
            else:
                value = None
            if value is None:
                break
        if isinstance(value, (int, float)) and not isinstance(value, bool):
            found.append(float(value))
    return found


def _five(values):
    if not values:
        return None
    return {"n": len(values), "median": round(float(np.median(values)), 3), "min": round(float(min(values)), 3),
            "max": round(float(max(values)), 3)}


def _circular(degrees):
    if not degrees:
        return None
    angles = np.radians(degrees)
    mean = np.degrees(np.arctan2(np.sin(angles).mean(), np.cos(angles).mean())) % 360
    spread = np.degrees(np.sqrt(-2 * np.log(max(np.hypot(np.sin(angles).mean(), np.cos(angles).mean()), 1e-9))))
    return {"n": len(degrees), "mean": round(float(mean), 1), "spread": round(float(spread), 1)}


def _colour(linears):
    """Per-channel median of linear colours, with its sRGB hex."""
    rgb = np.median(np.array(linears), axis=0)
    srgb = np.where(rgb <= 0.0031308, rgb * 12.92, 1.055 * np.clip(rgb, 0, 1) ** (1 / 2.4) - 0.055)
    return {"linear": [round(float(c), 4) for c in rgb],
            "hex": "#" + "".join(f"{int(round(c * 255)):02x}" for c in np.clip(srgb, 0, 1))}


def _emissive(record):
    colour = record["material_values"].get("_EmissionColor", [0, 0, 0])
    return max(colour[:3]) > 0.05 and "_EmissionMap" in record["material_values"]["maps"]
