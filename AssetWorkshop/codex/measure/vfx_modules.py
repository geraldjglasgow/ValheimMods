"""One ParticleSystem of the game's, its renderer and its material, in the codex's words: the main module, shape,
emission, every enabled module, how it is drawn (vfx_render). Built on vfx_read's YAML parser and curve
summaries.

    import game, vfx_modules
    p = game.prefab("Effects/Fire/fx_Torch_Basic.prefab")
    node = p.root()
    kinds = dict(p.components(node))
    vfx_modules.system(p, node, kinds["ParticleSystem"], kinds.get("ParticleSystemRenderer"))
"""
import math

from vfx_read import (SCALING, SHAPES, SHEET_TIME, SPACES, STOP_ACTIONS, SUB_TYPES, minmax_curve, minmax_gradient,
                      parse)
from vfx_render import (BUILTIN_SHADERS, blend_of, material_summary, quaternion_euler, renderer, transform,  # noqa: F401
                        vertex_streams)
from vfx_render import num as _num, ref_path as _ref_path, vec as _vec
from workshop import unity

DEGREES = 180 / math.pi


def _on(module):
    return isinstance(module, dict) and module.get("enabled") == 1.0


def system(prefab, node, ps_body, renderer_body):
    """One ParticleSystem in words: main module, shape, emission, the enabled modules, and how it is drawn."""
    ps = parse(ps_body)
    out = {"node": prefab.path_to(node), "active": prefab.active(node), "transform": transform(prefab, node)}
    out.update(main_module(ps))
    out.update({"shape": shape_module(ps), "emission": emission_module(ps), "sheet": sheet_module(ps)})
    out.update(motion_modules(ps))
    out.update(over_life_modules(ps))
    out.update(extra_modules(ps, prefab))
    out["renderer"] = renderer(renderer_body) if renderer_body else None
    return out


def main_module(ps):
    """Duration, looping, space, scaling and the start values (lifetime, speed, size, rotation, colour, gravity)."""
    init = ps.get("InitialModule", {})
    size3d = init.get("size3D") == 1.0
    return {"duration": _num(ps.get("lengthInSec")), "loop": ps.get("looping") == 1.0, "prewarm": ps.get("prewarm") == 1.0,
            "delay": minmax_curve(ps.get("startDelay")), "play_on_awake": ps.get("playOnAwake") == 1.0,
            "space": SPACES[int(ps.get("moveWithTransform", 0))], "scaling": SCALING[int(ps.get("scalingMode", 0))],
            "stop_action": STOP_ACTIONS[int(ps.get("stopAction", 0))], "sim_speed": _num(ps.get("simulationSpeed")),
            "max_particles": int(init.get("maxNumParticles", 0)),
            "lifetime": minmax_curve(init.get("startLifetime")), "speed": minmax_curve(init.get("startSpeed")),
            "size": minmax_curve(init.get("startSize")), "size3d": size3d,
            "size_y": minmax_curve(init.get("startSizeY")) if size3d else None,
            "rotation": minmax_curve(init.get("startRotation"), DEGREES), "rotation3d": init.get("rotation3D") == 1.0,
            "flip_rotation": _num(init.get("randomizeRotationDirection")),
            "colour": minmax_gradient(init.get("startColor")), "gravity": minmax_curve(init.get("gravityModifier"))}


def shape_module(ps):
    shape = ps.get("ShapeModule", {})
    if not _on(shape):
        return None
    kind = SHAPES[int(shape.get("type", 0))]
    return {"type": kind, "angle": _num(shape.get("angle")),
            "radius": _num((shape.get("radius") or {}).get("value")), "thickness": _num(shape.get("radiusThickness")),
            "arc": _num((shape.get("arc") or {}).get("value")), "length": _num(shape.get("length")),
            "donut": _num(shape.get("donutRadius")), "position": _vec(shape.get("m_Position")),
            "rotation": _vec(shape.get("m_Rotation")), "scale": _vec(shape.get("m_Scale")),
            "align_to_direction": shape.get("alignToDirection") == 1.0,
            "random_direction": _num(shape.get("randomDirectionAmount")),
            "spherize": _num(shape.get("sphericalDirectionAmount")),
            "random_position": _num(shape.get("randomPositionAmount")),
            "mesh": _ref_path(shape.get("m_Mesh")) if kind == "mesh" else None}


def emission_module(ps):
    emission = ps.get("EmissionModule", {})
    if not _on(emission):
        return None
    bursts = [{"time": _num(b.get("time")), "count": minmax_curve(b.get("countCurve")),
               "cycles": int(b.get("cycleCount", 1)), "interval": _num(b.get("repeatInterval")),
               "probability": _num(b.get("probability", 1.0))}
              for b in emission.get("m_Bursts") or []]
    return {"rate": minmax_curve(emission.get("rateOverTime")),
            "rate_distance": minmax_curve(emission.get("rateOverDistance")), "bursts": bursts}


def motion_modules(ps):
    """Velocity, limit (clamp), force, noise, inherit velocity and external forces, each only when enabled."""
    out = {}
    for key, reader in (("VelocityModule", _velocity), ("ClampVelocityModule", _limit), ("ForceModule", _force),
                        ("NoiseModule", _noise), ("InheritVelocityModule", _inherit)):
        if _on(ps.get(key)):
            name, value = reader(ps[key])
            out[name] = value
    if _on(ps.get("ExternalForcesModule")):
        out["external_forces"] = True
    return out


def _velocity(v):
    return "velocity", {"x": minmax_curve(v.get("x")), "y": minmax_curve(v.get("y")), "z": minmax_curve(v.get("z")),
                        "orbital": [minmax_curve(v.get(f"orbital{a}")) for a in "XYZ"],
                        "radial": minmax_curve(v.get("radial")), "speed_modifier": minmax_curve(v.get("speedModifier")),
                        "world": v.get("inWorldSpace") == 1.0}


def _limit(c):
    return "limit", {"speed": minmax_curve(c.get("magnitude")), "dampen": _num(c.get("dampen")),
                     "drag": minmax_curve(c.get("drag")), "separate_axes": c.get("separateAxis") == 1.0}


def _force(f):
    return "force", {"x": minmax_curve(f.get("x")), "y": minmax_curve(f.get("y")), "z": minmax_curve(f.get("z")),
                     "world": f.get("inWorldSpace") == 1.0, "random_per_frame": f.get("randomizePerFrame") == 1.0}


def _noise(n):
    return "noise", {"strength": minmax_curve(n.get("strength")), "frequency": _num(n.get("frequency")),
                     "scroll": minmax_curve(n.get("scrollSpeed")), "damping": n.get("damping") == 1.0,
                     "octaves": int(n.get("octaves", 1)), "quality": int(n.get("quality", 2)),
                     "position": minmax_curve(n.get("positionAmount")), "rotation": minmax_curve(n.get("rotationAmount")),
                     "size": minmax_curve(n.get("sizeAmount"))}


def _inherit(iv):
    return "inherit_velocity", {"mode": "current" if iv.get("m_Mode") == 1.0 else "initial",
                                "amount": minmax_curve(iv.get("m_Curve"))}


def over_life_modules(ps):
    """Colour, size and rotation over lifetime and by speed, each only when enabled."""
    out = {}
    if _on(ps.get("ColorModule")):
        out["colour_life"] = minmax_gradient(ps["ColorModule"].get("gradient"))
    size = ps.get("SizeModule", {})
    if _on(size):
        out["size_life"] = minmax_curve(size.get("curve"))
        if size.get("separateAxes") == 1.0:
            out["size_life_y"] = minmax_curve(size.get("y"))
    if _on(ps.get("RotationModule")):
        out["rotation_life"] = minmax_curve(ps["RotationModule"].get("curve"), DEGREES)
    if _on(ps.get("ColorBySpeedModule")):
        out["colour_speed"] = minmax_gradient(ps["ColorBySpeedModule"].get("gradient"))
    if _on(ps.get("SizeBySpeedModule")):
        out["size_speed"] = minmax_curve(ps["SizeBySpeedModule"].get("curve"))
    if _on(ps.get("RotationBySpeedModule")):
        out["rotation_speed"] = minmax_curve(ps["RotationBySpeedModule"].get("curve"), DEGREES)
    return out


def sheet_module(ps):
    uv = ps.get("UVModule", {})
    if not _on(uv):
        return None
    single_row = uv.get("animationType") == 1.0
    return {"tiles": [int(uv.get("tilesX", 1)), int(uv.get("tilesY", 1))], "sprites": uv.get("mode") == 1.0,
            "time": SHEET_TIME[int(uv.get("timeMode", 0))], "fps": _num(uv.get("fps")),
            "frame": minmax_curve(uv.get("frameOverTime")), "start_frame": minmax_curve(uv.get("startFrame")),
            "cycles": _num(uv.get("cycles")), "single_row": single_row,
            "random_row": uv.get("rowMode") == 1.0 if single_row else None}


def extra_modules(ps, prefab):
    """Collision, sub-emitters, lights, trails and custom data, each only when enabled."""
    out = {}
    if _on(ps.get("CollisionModule")):
        out["collision"] = _collision(ps["CollisionModule"])
    if _on(ps.get("SubModule")):
        out["sub_emitters"] = [{"type": SUB_TYPES[int(e.get("type", 0))], "emitter": node_name(prefab, e.get("emitter")),
                                "inherit": int(e.get("properties", 0)), "probability": _num(e.get("emitProbability", 1.0))}
                               for e in ps["SubModule"].get("subEmitters") or []]
    if _on(ps.get("LightsModule")):
        out["light_module"] = _lights(ps["LightsModule"], prefab)
    if _on(ps.get("TrailModule")):
        out["trail"] = _trail(ps["TrailModule"])
    custom = ps.get("CustomDataModule", {})
    if _on(custom):
        out["custom"] = [_custom_stream(custom, i) for i in (0, 1)]
    return out


def _collision(c):
    return {"type": "world" if c.get("type") == 1.0 else "planes", "dampen": minmax_curve(c.get("m_Dampen")),
            "bounce": minmax_curve(c.get("m_Bounce")), "lifetime_loss": minmax_curve(c.get("m_EnergyLossOnCollision")),
            "radius_scale": _num(c.get("radiusScale")), "quality": int(c.get("quality", 0))}


def _lights(lights, prefab):
    return {"ratio": _num(lights.get("ratio")), "max": int(lights.get("maxLights", 0)),
            "light": node_name(prefab, lights.get("light")), "colour": lights.get("color") == 1.0,
            "range": minmax_curve(lights.get("rangeCurve")), "intensity": minmax_curve(lights.get("intensityCurve"))}


def _trail(trail):
    return {"mode": "ribbon" if trail.get("mode") == 1.0 else "per_particle", "ratio": _num(trail.get("ratio")),
            "lifetime": minmax_curve(trail.get("lifetime")), "min_vertex_distance": _num(trail.get("minVertexDistance")),
            "texture_mode": int(trail.get("textureMode", 0)), "world": trail.get("worldSpace") == 1.0,
            "width": minmax_curve(trail.get("widthOverTrail")), "colour_life": minmax_gradient(trail.get("colorOverLifetime")),
            "colour_trail": minmax_gradient(trail.get("colorOverTrail")),
            "inherit_colour": trail.get("inheritParticleColor") == 1.0,
            "die_with_particles": trail.get("dieWithParticles") == 1.0}


def _custom_stream(custom, index):
    mode = int(custom.get(f"mode{index}", 0))
    if mode == 2:
        return {"colour": minmax_gradient(custom.get(f"color{index}"))}
    if mode == 1:
        count = int(custom.get(f"vectorComponentCount{index}", 4))
        return {"vector": [minmax_curve(custom.get(f"vector{index}_{c}")) for c in range(count)]}
    return None


def node_name(prefab, ref):
    """The GameObject name a component reference (an emitter, a light) points at in this prefab, or a file path."""
    if not isinstance(ref, dict):
        return None
    file_id = str(int(ref.get("fileID", 0)))
    if file_id == "0":
        return None
    if ref.get("guid"):
        return _ref_path(ref)
    doc = prefab.docs.get(file_id)
    if not doc:
        return f"#{file_id}"
    go = unity.ref(unity.field(doc[1], "m_GameObject"))[0]
    return unity.field(prefab.docs[go][1], "m_Name") if go in prefab.docs else f"#{file_id}"
