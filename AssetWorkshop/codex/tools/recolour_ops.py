"""The colour operations recolour.py applies to one paint region of a baked albedo, on (n, 3) sRGB arrays (0 to 255).

    ops = recolour_ops.parse("hue:+0.1,sat:0.8,val:1.1")      # or "tint:#8a6d3b" or "tint:#8a6d3b,keep:0.3"
    new = recolour_ops.apply(texels, ops)

hue   turns every texel's hue by a fraction of the colour wheel (+0.1 = 36 degrees); greys stay grey.
sat   multiplies saturation; val multiplies value (HSV of the sRGB texel), clamped.
tint  gives the region's median colour the tint's, keeping each texel's brightness against the median (the baked
      light, grime and occlusion) in linear light, and `keep` (0 to 1, default 0.5) of each texel's own departure in
      hue from the median, so the painted variation survives.
Operations run in the order written.
"""
import numpy as np

LUMA = np.array([0.2126, 0.7152, 0.0722])
NUMERIC = ("hue", "sat", "val", "keep")


def parse(spec):
    """'hue:+0.1,sat:0.8' -> [('hue', 0.1), ('sat', 0.8)]; tint values become (r, g, b) bytes. ValueError if bad."""
    ops = []
    for part in [p.strip() for p in spec.split(",") if p.strip()]:
        name, _, value = part.partition(":")
        name = name.strip().lower()
        if name == "tint":
            ops.append((name, hex_bytes(value.strip())))
        elif name in NUMERIC:
            ops.append((name, float(value)))
        else:
            raise ValueError(f"unknown operation '{name}' in '{spec}' (hue, sat, val, tint, keep)")
    return ops


def apply(texels, ops):
    """The texels (n, 3, sRGB bytes) after the operations, as bytes."""
    rgb = texels.astype(np.float64) / 255.0
    keep = next((v for name, v in ops if name == "keep"), 0.5)
    for name, value in ops:
        if name in ("hue", "sat", "val"):
            rgb = _hsv(rgb, name, value)
        elif name == "tint":
            rgb = tint(rgb, np.array(value) / 255.0, keep)
    return np.clip(np.round(rgb * 255.0), 0, 255).astype(np.uint8)


def tint(rgb, target, keep=0.5):
    """The region with its median colour moved to target (sRGB 0 to 1), value structure and some hue variation kept."""
    linear = to_linear(rgb)
    luma = np.maximum(linear @ LUMA, 1e-5)
    median = np.maximum(np.median(linear, axis=0), 1e-3)
    median_luma = max(float(median @ LUMA), 1e-5)
    departure = np.clip((linear / luma[:, None]) / (median / median_luma), 0.25, 4.0) ** keep
    goal = to_linear(target)
    out = goal * (luma / median_luma)[:, None] * departure
    return to_srgb(np.clip(out, 0.0, 1.0))


def _hsv(rgb, name, value):
    h, s, v = rgb_to_hsv(rgb)
    if name == "hue":
        h = (h + value) % 1.0
    elif name == "sat":
        s = np.clip(s * value, 0.0, 1.0)
    else:
        v = np.clip(v * value, 0.0, 1.0)
    return hsv_to_rgb(h, s, v)


def rgb_to_hsv(rgb):
    """(h, s, v) arrays, each 0 to 1, of (n, 3) RGB in 0 to 1."""
    high, low = rgb.max(axis=1), rgb.min(axis=1)
    chroma = high - low
    safe = np.where(chroma > 0, chroma, 1.0)
    r, g, b = rgb[:, 0], rgb[:, 1], rgb[:, 2]
    h = np.where(high == r, ((g - b) / safe) % 6.0, np.where(high == g, (b - r) / safe + 2.0, (r - g) / safe + 4.0))
    h = np.where(chroma > 0, h / 6.0, 0.0)
    s = np.where(high > 0, chroma / np.where(high > 0, high, 1.0), 0.0)
    return h, s, high


def hsv_to_rgb(h, s, v):
    """(n, 3) RGB of HSV arrays (0 to 1)."""
    sector = np.floor(h * 6.0).astype(int) % 6
    f = h * 6.0 - np.floor(h * 6.0)
    p, q, t = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
    table = [(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]
    out = np.zeros((len(h), 3))
    for index, channels in enumerate(table):
        chosen = sector == index
        out[chosen] = np.stack([c[chosen] for c in channels], axis=1)
    return out


def to_linear(srgb):
    srgb = np.asarray(srgb, dtype=np.float64)
    return np.where(srgb <= 0.04045, srgb / 12.92, ((srgb + 0.055) / 1.055) ** 2.4)


def to_srgb(linear):
    linear = np.asarray(linear, dtype=np.float64)
    return np.where(linear <= 0.0031308, linear * 12.92, 1.055 * np.power(linear, 1 / 2.4) - 0.055)


def hex_bytes(colour):
    """'#8a6d3b' -> (138, 109, 59)."""
    text = colour.lstrip("#")
    if len(text) != 6:
        raise ValueError(f"'{colour}' is not a #rrggbb colour")
    return tuple(int(text[i:i + 2], 16) for i in (0, 2, 4))
