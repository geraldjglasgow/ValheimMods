"""Statistics of painted texels: tones, saturation, hue, contrast, and how the variation splits across sizes of blotch.
Plain numpy; every function takes the texels a mask picks.

    tones = paint_stats.tones(rgb_linear, mask)          # value/saturation percentiles, 10/50/90 % colours, hue
    bands = paint_stats.bands(value, mask)               # variance per octave of blotch size, in texture pixels
    paint_stats.correlation_length(value, mask)          # pixels over which the paint stays alike (ACF = 0.5)
"""
import numpy as np

import paint_image

OCTAVES = (1, 2, 4, 8, 16, 32)          # blur sigmas in texture pixels: band k holds detail between two of them


def tones(rgb_linear, mask):
    """Tone and colour statistics of the masked texels (linear RGB in, sRGB value/saturation out)."""
    lin = rgb_linear[mask]
    srgb = paint_image.to_srgb(lin)
    hsv = paint_image.hsv(srgb)
    lum = paint_image.luminance(lin)
    found = {"value": _percentiles(hsv[:, 2]), "saturation": _percentiles(hsv[:, 1]),
             "luminance": _percentiles(lum), "hue": hue_stats(hsv),
             "colours": {f"p{p}": colour_at(lin, lum, p) for p in (10, 50, 90)}}
    found["contrast"] = {"value_span": round(found["value"]["p90"] - found["value"]["p10"], 3),
                         "luminance_ratio": round(found["luminance"]["p90"] / max(found["luminance"]["p10"], 1e-4), 2)}
    return found


def _percentiles(values):
    p10, p50, p90 = np.percentile(values, (10, 50, 90))
    return {"p10": round(float(p10), 3), "p50": round(float(p50), 3), "p90": round(float(p90), 3)}


def hue_stats(hsv):
    """Circular mean hue (degrees) of texels saturated above 0.12, and the hue span holding 80 % of them."""
    hues = hsv[hsv[:, 1] > 0.12, 0]
    if len(hues) < 16:
        return {"mean": None, "p10": None, "p90": None, "saturated": round(len(hues) / max(len(hsv), 1), 3)}
    angles = np.radians(hues)
    mean = np.degrees(np.arctan2(np.sin(angles).mean(), np.cos(angles).mean())) % 360
    offset = (hues - mean + 180) % 360 - 180
    low, high = np.percentile(offset, (10, 90))
    return {"mean": round(float(mean), 1), "p10": round(float((mean + low) % 360), 1),
            "p90": round(float((mean + high) % 360), 1), "saturated": round(len(hues) / len(hsv), 3)}


def colour_at(lin, lum, percentile):
    """The mean linear colour of the texels within 5 percentiles of a luminance percentile, and its sRGB hex."""
    low, high = np.percentile(lum, (max(0, percentile - 5), min(100, percentile + 5)))
    pick = lin[(lum >= low) & (lum <= high)]
    rgb = pick.mean(axis=0) if len(pick) else lin.mean(axis=0)
    srgb = paint_image.to_srgb(rgb)
    return {"linear": [round(float(c), 4) for c in rgb],
            "hex": "#" + "".join(f"{int(round(c * 255)):02x}" for c in srgb)}


def blur(image, mask, sigma):
    """A Gaussian blur of the masked texels only (normalised convolution, wrapping at the edges like the texture)."""
    weights = mask.astype(np.float64)
    kernel = _kernel(image.shape, sigma)
    top = np.real(np.fft.ifft2(np.fft.fft2(image * weights) * kernel))
    bottom = np.real(np.fft.ifft2(np.fft.fft2(weights) * kernel))
    return np.where(bottom > 1e-6, top / np.maximum(bottom, 1e-6), 0.0)


def _kernel(shape, sigma):
    """The Fourier transform of a unit Gaussian of sigma pixels on a wrapped grid of this shape."""
    fy = np.fft.fftfreq(shape[0])[:, None]
    fx = np.fft.fftfreq(shape[1])[None, :]
    return np.exp(-2.0 * (np.pi * sigma) ** 2 * (fx ** 2 + fy ** 2))


def bands(value, mask):
    """Fraction of the masked variance in each octave of blotch size: 'fine' (under 2 px), '2-4' ... '32+' px, and
    the octave holding the most."""
    levels = [value] + [blur(value, mask, s) for s in OCTAVES]
    names = ["fine", "2-4", "4-8", "8-16", "16-32", "32-64", "64+"]
    parts = [levels[i] - levels[i + 1] for i in range(len(OCTAVES))] + [levels[-1]]
    variances = [float(np.var(p[mask])) for p in parts[:-1]] + [float(np.var(parts[-1][mask]))]
    total = sum(variances) or 1.0
    fractions = {name: round(v / total, 3) for name, v in zip(names, variances)}
    fractions["dominant"] = max(names, key=lambda n: fractions[n])
    return fractions


def correlation_length(value, mask):
    """The lag in pixels at which the masked texels' autocorrelation falls to one half (radially averaged)."""
    weights = mask.astype(np.float64)
    centred = (value - value[mask].mean()) * weights
    power = np.abs(np.fft.fft2(centred)) ** 2
    overlap = np.real(np.fft.ifft2(np.abs(np.fft.fft2(weights)) ** 2))
    acf = np.real(np.fft.ifft2(power)) / np.maximum(overlap, 1.0)
    acf /= max(acf[0, 0], 1e-12)
    lags = np.hypot(*np.meshgrid(_wrapped(value.shape[1]), _wrapped(value.shape[0])))
    for lag in range(1, min(value.shape) // 2):
        ring = (lags >= lag - 0.5) & (lags < lag + 0.5) & (overlap > 0.25 * overlap[0, 0])
        if ring.any() and acf[ring].mean() < 0.5:
            return float(lag)
    return float(min(value.shape) // 2)


def _wrapped(n):
    """Signed distances 0, 1, ... n/2, ..., -1 along one axis of a wrapped grid."""
    index = np.arange(n)
    return np.where(index <= n // 2, index, index - n)


def local_contrast(value, mask, sigma=2.0):
    """How much of the value's spread lives within a few pixels: std of value minus its blur, over the total std."""
    detail = value - blur(value, mask, sigma)
    total = float(np.std(value[mask])) or 1.0
    return round(float(np.std(detail[mask])) / total, 3)
