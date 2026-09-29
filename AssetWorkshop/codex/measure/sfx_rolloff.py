"""How loud an AudioSource is at a distance, from its roll-off settings, and the distances that describe it: where it
has fallen to half (-6 dB), to a tenth (-20 dB) and to a hundredth (-40 dB, effectively gone under the game's mix).

Unity's modes: Logarithmic (0) is min_distance / distance past min_distance and stops falling at max_distance, so it
never goes silent; Linear (1) falls in a straight line from 1 at min_distance to 0 at max_distance; Custom (2) is a
curve over distance / max_distance (evaluated here with straight lines between its keys, which is within a few per cent
of Unity's Hermite curve for the game's curves).
"""
import numpy as np

MODES = {0: "log", 1: "linear", 2: "custom"}


def gain(source, distance):
    """Linear gain of the source at a distance (array or float) in metres, before the mixer."""
    low, high = source.get("min_distance") or 1.0, source.get("max_distance") or 500.0
    d = np.asarray(distance, dtype=float)
    mode = source.get("rolloff_mode")
    if mode == 1:
        return np.clip((high - d) / max(high - low, 1e-6), 0.0, 1.0)
    if mode == 2 and source.get("curve"):
        times, values = zip(*source["curve"])
        return np.interp(np.clip(d / high, 0, None), times, values)
    return np.minimum(1.0, low / np.maximum(np.minimum(d, high), 1e-6))


def first_below(source, level, reach):
    """The first distance (0.05 m steps up to reach) where the gain is at or under level, None when it never is."""
    steps = np.arange(0.0, reach, 0.05)
    under = np.flatnonzero(gain(source, steps) <= level)
    return round(float(steps[under[0]]), 2) if len(under) else None


def distances(source):
    """{'half_m', 'tenth_m', 'gone_m', 'floor_db'}: the -6, -20 and -40 dB distances, and the level where a
    logarithmic source stops falling (at max_distance). A 2D source (spatial 0) is the same everywhere."""
    if source.get("spatial") == 0:
        return {"half_m": None, "tenth_m": None, "gone_m": None, "floor_db": 0.0}
    reach = 1.5 * (source.get("max_distance") or 500.0)
    floor = float(gain(source, reach))
    return {"half_m": first_below(source, 0.5, reach), "tenth_m": first_below(source, 0.1, reach),
            "gone_m": first_below(source, 0.01, reach),
            "floor_db": round(20 * np.log10(max(floor, 1e-6)), 1)}
