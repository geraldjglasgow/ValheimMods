"""The effect spec: an effect written in the codex's words (the same keys codex/measure/vfx_modules measures the
game's effects with), expanded into the complete JSON the Unity build reads (unity/Assets/Editor/Vfx/VfxSpec.cs).

    EFFECT = {"name": "ecp_frost_impact", "systems": [{"name": "shards", "lifetime": (0.4, 0.8), ...}], ...}
    spec.expand(EFFECT)      -> dict ready for json.dump

Values take the forms the measurements print, or short ones:
    curve       2.5 | (1, 3) | [[0, 1], [1, 0.3]] | {"const"} | {"min", "max"} | {"curve"} | {"curve_min", "curve_max"}
    colour      [r, g, b, a] | ([..], [..]) two colours | {"colour": [[t, r, g, b]], "alpha": [[t, a]]} a gradient
                | {"colour": [..]} | {"colours": [a, b]} | {"gradient": g} | {"gradients": [g, g]} | {"random": g}
Anything left out takes Unity's default for a new particle system, except where DEFAULTS says the game differs.
"""
import copy

from spec_materials import BLENDS, SHADERS, STANDARD_MODES, material, texture  # noqa: F401  (spec's public names)

DEFAULTS = {  # where the game's effects differ from a new Unity system (codex/vfx/archetypes.md)
    "duration": 3.0, "loop": False, "space": "local", "scaling": "local", "max_particles": 1000,
    "lifetime": 1.0, "speed": 1.0, "size": 0.5, "rotation": 0.0, "gravity": 0.0, "colour": [1, 1, 1, 1],
}


def curve(value, scale=1.0):
    """Any curve form -> {"mode", "c", "min", "max", "scale", "t", "v", "t2", "v2"}."""
    out = {"mode": "const", "c": 0.0, "min": 0.0, "max": 0.0, "scale": 1.0, "t": [], "v": [], "t2": [], "v2": []}
    if value is None:
        return out
    if isinstance(value, (int, float)):
        out.update(c=float(value) * scale)
    elif isinstance(value, tuple) or (isinstance(value, list) and len(value) == 2 and all(isinstance(v, (int, float)) for v in value)):
        out.update(mode="range", min=float(value[0]) * scale, max=float(value[1]) * scale)
    elif isinstance(value, list):
        out.update(mode="curve", t=[float(k[0]) for k in value], v=[float(k[1]) * scale for k in value])
    else:
        _measured_curve(value, scale, out)
    return out


def _measured_curve(value, scale, out):
    if "const" in value:
        out.update(c=float(value["const"]) * scale)
    elif "min" in value:
        out.update(mode="range", min=float(value["min"]) * scale, max=float(value["max"]) * scale)
    elif "curve" in value:
        out.update(mode="curve", t=[k[0] for k in value["curve"]], v=[k[1] * scale for k in value["curve"]])
    else:
        out.update(mode="curves", t=[k[0] for k in value["curve_min"]], v=[k[1] * scale for k in value["curve_min"]],
                   t2=[k[0] for k in value["curve_max"]], v2=[k[1] * scale for k in value["curve_max"]])


def gradient(value):
    """Any colour form -> {"mode", "a", "b", "ck", "ak", "ck2", "ak2"} (keys flattened: t, r, g, b / t, a)."""
    out = {"mode": "colour", "a": [1.0, 1.0, 1.0, 1.0], "b": [1.0, 1.0, 1.0, 1.0], "ck": [], "ak": [], "ck2": [], "ak2": []}
    if value is None:
        return out
    if isinstance(value, list) and len(value) == 4 and all(isinstance(v, (int, float)) for v in value):
        out.update(a=[float(v) for v in value])
    elif isinstance(value, tuple):
        out.update(mode="colours", a=[float(v) for v in value[0]], b=[float(v) for v in value[1]])
    elif "alpha" in value:
        out.update(mode="gradient", **_keys(value))
    else:
        _measured_gradient(value, out)
    return out


def _measured_gradient(value, out):
    if "colour" in value:
        out.update(a=[float(v) for v in value["colour"]])
    elif "colours" in value:
        out.update(mode="colours", a=value["colours"][0], b=value["colours"][1])
    elif "gradient" in value:
        out.update(mode="gradient", **_keys(value["gradient"]))
    elif "random" in value:
        out.update(mode="random", **_keys(value["random"]))
    else:
        first, second = _keys(value["gradients"][0]), _keys(value["gradients"][1])
        out.update(mode="gradients", ck=first["ck"], ak=first["ak"], ck2=second["ck"], ak2=second["ak"])


def _keys(g):
    return {"ck": [float(x) for key in g["colour"] for x in key[:4]], "ak": [float(x) for key in g["alpha"] for x in key[:2]]}


def _vec(value, default):
    return [float(v) for v in (value if value is not None else default)]


def expand(effect):
    """The whole effect as the Unity build's JSON: every field present, curves and colours in the flat forms."""
    effect = copy.deepcopy(effect)
    names = {m for m in effect.get("materials", {})}
    return {"name": effect["name"], "category": effect.get("category", ""),
            "textures": [texture(n, t) for n, t in effect.get("textures", {}).items()],
            "materials": [material(n, m) for n, m in effect.get("materials", {}).items()],
            "meshes": [dict({"name": n, "kind": "chunk", "seed": 0, "size": [1, 1, 1], "detail": 1}, **m)
                       for n, m in effect.get("meshes", {}).items()],
            "systems": [system(s, names, effect) for s in effect.get("systems", [])],
            "lights": [light(l) for l in effect.get("lights", [])],
            "timeout": float(effect.get("timeout", 0.0)), "shake": shake(effect.get("shake")),
            "preview": preview(effect.get("preview", {}))}


def system(s, materials, effect):
    """One particle system with every module, enabled or not."""
    d = dict(DEFAULTS, **s)
    out = {"name": d["name"], "role": d.get("role", ""), "parent": d.get("parent", ""), "position": _vec(d.get("position"), [0, 0, 0]),
           "euler": _vec(d.get("euler"), [0, 0, 0]), "scale": _vec(d.get("scale"), [1, 1, 1]),
           "duration": float(d["duration"]), "loop": bool(d["loop"]), "prewarm": bool(d.get("prewarm", False)),
           "delay": curve(d.get("delay", 0)), "play_on_awake": bool(d.get("play_on_awake", True)),
           "space": d["space"], "scaling": d["scaling"], "stop_action": d.get("stop_action", "none"),
           "sim_speed": float(d.get("sim_speed", 1.0)), "max_particles": int(d["max_particles"]), "seed": int(d.get("seed", 0)),
           "lifetime": curve(d["lifetime"]), "speed": curve(d["speed"]), "size": curve(d["size"]),
           "size3d": bool(d.get("size3d", False)), "size_y": curve(d.get("size_y", d["size"] if not isinstance(d["size"], dict) else 1)),
           "rotation": curve(d["rotation"]), "flip_rotation": float(d.get("flip_rotation", 0)),
           "colour": gradient(d["colour"]), "gravity": curve(d["gravity"])}
    out.update(modules(d))
    out["renderer"] = renderer(d.get("renderer", {}), materials, effect)
    return out


def modules(d):
    shape, emission = d.get("shape", {}), d.get("emission", {"rate": 10})
    return {"shape": dict(_shape_defaults(), **(shape or {}), enabled=shape is not None),
            "emission": {"enabled": True, "rate": curve(emission.get("rate", 0)),
                         "rate_distance": curve(emission.get("rate_distance", 0)),
                         "bursts": [burst(b) for b in emission.get("bursts", [])]},
            "velocity": _module(d.get("velocity"), {"x": 0, "y": 0, "z": 0, "radial": 0, "speed_modifier": 1,
                                                    "orbital_x": 0, "orbital_y": 0, "orbital_z": 0}, {"world": False}),
            "limit": _module(d.get("limit"), {"speed": 1000, "drag": 0}, {"dampen": 0.0}),
            "force": _module(d.get("force"), {"x": 0, "y": 0, "z": 0}, {"world": False, "random_per_frame": False}),
            "noise": _module(d.get("noise"), {"strength": 1, "scroll": 0, "position": 1, "rotation": 0, "size": 0},
                             {"frequency": 0.5, "damping": True, "octaves": 1, "quality": 2}),
            "colour_life": {"enabled": "colour_life" in d, "gradient": gradient(d.get("colour_life"))},
            "size_life": {"enabled": "size_life" in d, "curve": curve(d.get("size_life", 1))},
            "rotation_life": {"enabled": "rotation_life" in d, "curve": curve(d.get("rotation_life", 0))},
            "sheet": sheet(d.get("sheet")), "trail": trail(d.get("trail")), "collision": collision(d.get("collision")),
            "sub_emitters": [dict({"inherit": 0, "probability": 1.0}, **e) for e in d.get("sub_emitters", [])],
            "custom1": custom(d.get("custom", [None, None])[0]), "custom2": custom(d.get("custom", [None, None])[1])}


def _shape_defaults():
    return {"type": "cone", "angle": 25.0, "radius": 1.0, "thickness": 1.0, "arc": 360.0, "length": 5.0, "donut": 0.2,
            "position": [0, 0, 0], "rotation": [0, 0, 0], "scale": [1, 1, 1], "align_to_direction": False,
            "random_direction": 0.0, "spherize": 0.0, "random_position": 0.0, "mesh": ""}


def _module(given, curves, plain):
    """A module with curve fields and plain fields; enabled when given."""
    given = given or {}
    out = {"enabled": bool(given)}
    out.update({k: curve(given.get(k, v)) for k, v in curves.items()})
    out.update({k: given.get(k, v) for k, v in plain.items()})
    return out


def burst(b):
    if isinstance(b, (list, tuple)):
        b = {"time": b[0], "count": b[1]}
    return {"time": float(b.get("time", 0)), "count": curve(b.get("count", 10)), "cycles": int(b.get("cycles", 1)),
            "interval": float(b.get("interval", 0.01)), "probability": float(b.get("probability", 1.0))}


def sheet(s):
    s = s or {}
    tiles = s.get("tiles", [1, 1])
    return {"enabled": bool(s), "tiles_x": int(tiles[0]), "tiles_y": int(tiles[1]), "time": s.get("time", "lifetime"),
            "fps": float(s.get("fps", 30)), "frame": curve(s.get("frame", [[0, 0], [1, 1]])),
            "start_frame": curve(s.get("start_frame", 0)), "cycles": float(s.get("cycles", 1)),
            "single_row": bool(s.get("single_row", False)), "random_row": bool(s.get("random_row", True))}


def trail(t):
    t = t or {}
    return {"enabled": bool(t), "mode": t.get("mode", "per_particle"), "ratio": float(t.get("ratio", 1)),
            "lifetime": curve(t.get("lifetime", 1)), "min_vertex_distance": float(t.get("min_vertex_distance", 0.2)),
            "texture_mode": int(t.get("texture_mode", 0)), "world": bool(t.get("world", False)),
            "width": curve(t.get("width", 1)), "colour_life": gradient(t.get("colour_life")),
            "colour_trail": gradient(t.get("colour_trail")), "inherit_colour": bool(t.get("inherit_colour", True)),
            "die_with_particles": bool(t.get("die_with_particles", True)),
            "size_affects_width": bool(t.get("size_affects_width", True))}


def collision(c):
    c = c or {}
    return {"enabled": bool(c), "type": c.get("type", "world"), "dampen": curve(c.get("dampen", 0)),
            "bounce": curve(c.get("bounce", 1)), "lifetime_loss": curve(c.get("lifetime_loss", 0)),
            "radius_scale": float(c.get("radius_scale", 1)), "quality": int(c.get("quality", 0)),
            "send_messages": bool(c.get("send_messages", False))}


def custom(c):
    if not c:
        return {"mode": "none", "colour": gradient(None), "vector": []}
    if "colour" in c:
        return {"mode": "colour", "colour": gradient(c["colour"]), "vector": []}
    return {"mode": "vector", "colour": gradient(None), "vector": [curve(v) for v in c["vector"]]}


def renderer(r, materials, effect):
    """How a system draws: render mode, sorting, size limits, stretching, its material and vertex streams."""
    name = r.get("material", "")
    if name and name not in materials:
        raise ValueError(f"renderer material {name} is not in the effect's materials")
    shader = SHADERS[effect["materials"][name].get("shader", "standard_unlit")] if name else None
    return {"mode": r.get("mode", "billboard"), "material": name, "trail_material": r.get("trail_material", ""),
            "mesh": r.get("mesh", ""), "sort": r.get("sort", "none"), "fudge": float(r.get("fudge", 0)),
            "order": int(r.get("order", 0)), "min_size": float(r.get("min_size", 0)),
            "max_size": float(r.get("max_size", 0.5)), "length_scale": float(r.get("length_scale", 2)),
            "speed_scale": float(r.get("speed_scale", 0)), "camera_speed_scale": float(r.get("camera_speed_scale", 0)),
            "alignment": r.get("alignment", "view"), "pivot": _vec(r.get("pivot"), [0, 0, 0]),
            "flip": _vec(r.get("flip"), [0, 0, 0]),
            "streams": r.get("streams", shader["streams"] if shader else []), "shadows": bool(r.get("shadows", False))}


def light(l):
    flicker, lod = l.get("flicker"), l.get("lod", {})
    return {"name": l.get("name", "light"), "parent": l.get("parent", ""), "position": _vec(l.get("position"), [0, 0, 0]),
            "colour": _vec(l.get("colour"), [1, 1, 1]), "intensity": float(l.get("intensity", 1)),
            "range": float(l.get("range", 5)), "shadows": l.get("shadows", "none"),
            "flicker": dict({"enabled": bool(flicker), "intensity": 0.1, "speed": 10.0, "movement": 0.1, "ttl": 0.0,
                             "fade": 0.2, "fade_in": 0.0}, **(flicker or {})),
            "lod": dict({"enabled": True, "distance": 40.0, "shadow_distance": 20.0}, **lod)}


def shake(s):
    return dict({"enabled": bool(s), "strength": 1.0, "range": 20.0, "delay": 0.0, "continuous": False,
                 "continuous_duration": 0.0, "local_only": False}, **(s or {}))


def preview(p):
    """Preview settings; the camera backs off far enough for the effect and its references side by side (40 degrees
    of vertical view at 16:9 see 1.3 m across per metre away)."""
    offset = _vec(p.get("reference_offset"), [3, 0, 0])[0]
    wide = (len(p.get("references", [])) * abs(offset) + 2.5) / 1.3
    return {"seconds": float(p.get("seconds", 3)), "fps": int(p.get("fps", 30)), "width": int(p.get("width", 640)),
            "height": int(p.get("height", 360)), "distance": max(float(p.get("distance", 6)), wide),
            "target_height": float(p.get("target_height", 1.0)), "yaw": float(p.get("yaw", 35)),
            "pitch": float(p.get("pitch", 14)), "references": list(p.get("references", [])),
            "reference_offset": _vec(p.get("reference_offset"), [3, 0, 0]), "warmup": float(p.get("warmup", 0)),
            "figure": bool(p.get("figure", True)), "repeat": float(p.get("repeat", 0)), "lift": float(p.get("lift", 0)),
            "figure_offset": _vec(p.get("figure_offset"), []),
            "sheet_times": [float(t) for t in p.get("sheet_times", [])]}
