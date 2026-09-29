"""The lowest layer of the vfx measurements: Unity's YAML documents as dicts, and MinMaxCurves, gradients and
MinMaxGradients summarised in the codex's words (curves as sampled [t, v] keys, gradients as colour and alpha keys,
times 0 to 1). vfx_modules builds a ParticleSystem's summary on it, vfx_effect a whole prefab's.

    import vfx_read
    ps = vfx_read.parse(body)                               # a '--- !u!198' document body -> dict
    vfx_read.minmax_curve(ps["InitialModule"]["startLifetime"])        # {"min": 1.0, "max": 1.5}

Unity writes a ParticleSystem as a few hundred lines of YAML. Plain Python; nothing read here goes into the
repository but numbers.
"""
import math
import re

VERTEX_STREAMS = ("Position Normal Tangent Color UV UV2 UV3 UV4 AnimBlend AnimFrame Center VertexID SizeX SizeXY "
                  "SizeXYZ Rotation Rotation3D RotationSpeed RotationSpeed3D Velocity Speed AgePercent InvStartLifetime "
                  "StableRandomX StableRandomXY StableRandomXYZ StableRandomXYZW VaryingRandomX VaryingRandomXY "
                  "VaryingRandomXYZ VaryingRandomXYZW Custom1X Custom1XY Custom1XYZ Custom1XYZW Custom2X Custom2XY "
                  "Custom2XYZ Custom2XYZW NoiseSumX NoiseSumXY NoiseSumXYZ NoiseImpulseX NoiseImpulseXY NoiseImpulseXYZ "
                  "MeshIndex ParticleIndex ColorPackedAsTwoFloats MeshAxisOfRotation NextTrailCenter PreviousTrailCenter "
                  "PercentageAlongTrail TrailWidth").split()
SHAPES = ("sphere sphere_shell hemisphere hemisphere_shell cone box mesh cone_shell cone_volume cone_volume_shell "
          "circle circle_edge edge mesh_renderer skinned_mesh_renderer box_shell box_edge donut rectangle sprite "
          "sprite_renderer").split()
RENDER_MODES = ("billboard", "stretched", "horizontal", "vertical", "mesh", "none")
SORT_MODES = ("none", "distance", "oldest_in_front", "youngest_in_front", "depth", "distance_reverse", "depth_reverse")
ALIGNMENTS = ("view", "world", "local", "facing", "velocity")
SUB_TYPES = ("birth", "collision", "death", "trigger", "manual")
SPACES = ("local", "world", "custom")
SCALING = ("hierarchy", "local", "shape")
STOP_ACTIONS = ("none", "disable", "destroy", "callback")
SHEET_TIME = ("lifetime", "speed", "fps")
LIGHT_TYPES = ("spot", "directional", "point", "area", "disc")
BLENDS = {(1, 1): "additive", (5, 1): "additive_alpha", (3, 1): "additive_soft", (5, 10): "alpha", (1, 10): "premultiplied",
          (2, 0): "multiply", (4, 1): "screen", (1, 0): "opaque", (0, 3): "multiply", (2, 3): "multiply_double"}
_NUMBER = re.compile(r"^-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][-+]?\d+)?$")


# ---------------------------------------------------------------- YAML

def parse(body):
    """A Unity YAML document body as nested dicts and lists (numbers as floats, hex blobs and names as strings)."""
    lines = [(len(line) - len(line.lstrip(" ")), line.strip()) for line in body.splitlines() if line.strip()]
    lines = [entry for entry in lines if not entry[1].startswith("---") and entry[1] not in ("ParticleSystem:",)]
    if not lines:
        return {}
    start = 1 if lines[0][1].endswith(":") and lines[0][0] == 0 else 0
    return _mapping(lines, start, lines[start][0])[0] if start < len(lines) else {}


def _mapping(lines, i, indent):
    out = {}
    while i < len(lines):
        depth, text = lines[i]
        if depth < indent or (depth == indent and text.startswith("- ")):
            break
        key, _, rest = text.partition(":")
        rest, i = rest.strip(), i + 1
        if rest:
            out[key] = _scalar(rest)
        elif i < len(lines) and lines[i][0] == depth and lines[i][1].startswith("- "):
            out[key], i = _list(lines, i, depth)
        elif i < len(lines) and lines[i][0] > depth:
            out[key], i = _mapping(lines, i, lines[i][0])
        else:
            out[key] = None
    return out, i


def _list(lines, i, indent):
    items = []
    while i < len(lines) and lines[i][0] == indent and lines[i][1].startswith("- "):
        text = lines[i][1][2:]
        if re.match(r"^[\w.]+:", text) and not text.startswith("{"):
            lines[i] = (indent + 2, text)
            item, i = _mapping(lines, i, indent + 2)
        else:
            item, i = _scalar(text), i + 1
        items.append(item)
    return items, i


def _scalar(text):
    if text == "[]":
        return []
    if text in ("Infinity", "-Infinity", "NaN"):
        return float(text.replace("Infinity", "inf"))
    if text.startswith("{") and text.endswith("}"):
        return {k: _scalar(v.strip()) for k, v in re.findall(r"(\w+): ([^,}]*)", text)}
    if _NUMBER.match(text) and not (len(text) > 1 and text[0] == "0" and text[1] != "."):
        return float(text)
    return text


# ---------------------------------------------------------------- curves and gradients

def curve_keys(curve):
    """[(time, value, in slope, out slope)] of an AnimationCurve dict."""
    return [(k["time"], k["value"], _slope(k.get("inSlope")), _slope(k.get("outSlope"))) for k in curve.get("m_Curve") or []]


def _slope(value):
    return value if isinstance(value, float) and math.isfinite(value) else 0.0


def evaluate(keys, t):
    """An AnimationCurve's value at t (Hermite between keys, clamped at the ends)."""
    if not keys:
        return 0.0
    if t <= keys[0][0]:
        return keys[0][1]
    for (t0, v0, _, out0), (t1, v1, in1, _) in zip(keys, keys[1:]):
        if t <= t1:
            dt = t1 - t0 or 1e-6
            s = (t - t0) / dt
            h00, h10, h01, h11 = 2 * s**3 - 3 * s**2 + 1, s**3 - 2 * s**2 + s, -2 * s**3 + 3 * s**2, s**3 - s**2
            return h00 * v0 + h10 * dt * out0 + h01 * v1 + h11 * dt * in1
    return keys[-1][1]


def sampled(keys, count=6, scale=1.0):
    """The curve at count evenly spaced times from 0 to 1, as [[t, v]]."""
    return [[round(i / (count - 1), 3), round(scale * evaluate(keys, i / (count - 1)), 4)] for i in range(count)]


def minmax_curve(mc, scale=1.0, samples=6):
    """A MinMaxCurve in words: {'const'} / {'min','max'} / {'curve'} / {'curve_min','curve_max'}; scale converts units
    (radians to degrees)."""
    if not isinstance(mc, dict):
        return None
    state, scalar = int(mc.get("minMaxState", 0)), mc.get("scalar", 0.0) * scale
    if state == 0:
        return {"const": round(scalar, 4)}
    if state == 3:
        return {"min": round(mc.get("minScalar", 0.0) * scale, 4), "max": round(scalar, 4)}
    keys_max = curve_keys(mc.get("maxCurve") or {})
    if state == 1:
        return {"curve": sampled(keys_max, samples, scalar)}
    return {"curve_min": sampled(curve_keys(mc.get("minCurve") or {}), samples, scalar),
            "curve_max": sampled(keys_max, samples, scalar)}


def number(value):
    """A summarised number back from JSON: 'inf' strings to float infinity."""
    if isinstance(value, str):
        return float(value.replace("inf", "Infinity").replace("Infinity", "inf"))
    return value


def span(summary):
    """(lowest, highest) value a summarised MinMaxCurve reaches, or None."""
    if not summary:
        return None
    if "const" in summary:
        return number(summary["const"]), number(summary["const"])
    if "min" in summary:
        low, high = number(summary["min"]), number(summary["max"])
        return min(low, high), max(low, high)
    values = [number(v) for key in ("curve", "curve_min", "curve_max") for _, v in summary.get(key, [])]
    return (min(values), max(values)) if values else None


def mean(summary):
    """The middle of a summarised MinMaxCurve's range (for a curve, the mean over its samples), or None."""
    if not summary:
        return None
    if "curve" in summary:
        return sum(number(v) for _, v in summary["curve"]) / len(summary["curve"])
    low_high = span(summary)
    return None if low_high is None else (low_high[0] + low_high[1]) / 2


def gradient(g):
    """A Gradient dict as {'colour': [[t, r, g, b]], 'alpha': [[t, a]], 'fixed': bool}."""
    if not isinstance(g, dict):
        return None
    colours = [[round(g[f"ctime{i}"] / 65535, 3)] + [round(g[f"key{i}"][c], 3) for c in "rgb"]
               for i in range(int(g.get("m_NumColorKeys", 2)))]
    alphas = [[round(g[f"atime{i}"] / 65535, 3), round(g[f"key{i}"]["a"], 3)] for i in range(int(g.get("m_NumAlphaKeys", 2)))]
    return {"colour": colours, "alpha": alphas, "fixed": g.get("m_Mode") == 1.0}


def colour(c):
    return [round(c[k], 3) for k in "rgba"] if isinstance(c, dict) else None


def minmax_gradient(mg):
    """A MinMaxGradient in words: {'colour'} / {'gradient'} / {'colours': [a, b]} / {'gradients'} / {'random'}."""
    if not isinstance(mg, dict):
        return None
    state = int(mg.get("minMaxState", 0))
    if state == 0:
        return {"colour": colour(mg.get("maxColor"))}
    if state == 1:
        return {"gradient": gradient(mg.get("maxGradient"))}
    if state == 2:
        return {"colours": [colour(mg.get("minColor")), colour(mg.get("maxColor"))]}
    if state == 3:
        return {"gradients": [gradient(mg.get("minGradient")), gradient(mg.get("maxGradient"))]}
    return {"random": gradient(mg.get("maxGradient"))}


def gradient_at(g, t):
    """(r, g, b, a) of a summarised gradient at t."""
    def interp(keys, count):
        if t <= keys[0][0]:
            return keys[0][1:1 + count]
        for a, b in zip(keys, keys[1:]):
            if t <= b[0]:
                s = (t - a[0]) / ((b[0] - a[0]) or 1e-6)
                return [x + (y - x) * s for x, y in zip(a[1:1 + count], b[1:1 + count])]
        return keys[-1][1:1 + count]
    return tuple(interp(g["colour"], 3)) + tuple(interp(g["alpha"], 1))
