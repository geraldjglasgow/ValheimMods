"""What an item carries besides its model: the swing trail (MeleeWeaponTrail), the upgrade glow
(ParticleIntensityScaler), the objects switched on only when equipped (`equiped`), lights and particles in the hand,
the ground sparkle on the root, the bones worn parts hang on, and cape cloth (MagicaCloth) settings.

    items_extras.extras(prefab, items_model.frames(prefab))
"""
import re

import game
import items_geometry
import items_shared
from workshop import unity


def extras(prefab, frames):
    attach = frames.get("attach")
    return {"trail": _trail(prefab, attach), "glow": _glow(prefab), "equipped": _equipped(prefab, attach),
            "lights": _lights(prefab, attach), "particles": _particles(prefab, attach),
            "ground_fx": _ground(prefab), "bones": [n[len("attach_"):] for n in frames
                                                    if n.startswith("attach_") and n not in ("attach_skin", "attach_back")],
            "cloth": _cloth(prefab)}


def _trail(prefab, attach):
    """The swing trail: lifetime, material, and its base and tip in the attach frame (where the edge runs)."""
    for node, kind, body in prefab.all_components():
        if kind != "MeleeWeaponTrail":
            continue
        ends = [unity.ref(unity.field(body, f"_{end}"))[0] for end in ("base", "tip")]
        points = [items_geometry.frame_matrix(prefab, e, attach)[:3, 3].round(3).tolist() if prefab.is_local(e)
                  else None for e in ends]
        material = game.path_of(unity.field(body, "_material"))
        return {"life_s": float(unity.field(body, "_lifeTime") or 0), "material": _name(material),
                "base": points[0], "tip": points[1], "subdivisions": int(unity.field(body, "subdivisions") or 0)}
    return None


def _glow(prefab):
    """The upgrade glow: where it sits, its light and particle material, how quality scales it."""
    for node, kind, body in prefab.all_components():
        if kind != "ParticleIntensityScaler":
            continue
        lights = [_light(b) for n in _below(prefab, node) for k, b in prefab.components(n) if k == "Light"]
        mats = [_name(p) for n in _below(prefab, node) for k, b in prefab.components(n)
                if k == "ParticleSystemRenderer" for p in game.renderer_materials(b)]
        return {"node": prefab.path_to(node).split("/", 1)[-1], "active": prefab.active(node),
                "max_quality": int(float(unity.field(body, "maxQuality") or 0)),
                "light_max": float(unity.field(body, "lightIntensityMax") or 0),
                "color_max": float(unity.field(body, "intensityColorMax") or 0), "lights": lights, "materials": mats}
    return None


def _equipped(prefab, attach):
    """Names under the attach's `equiped` child with their components (switched on only in a hand)."""
    node = next((c for c in prefab.children(attach) if prefab.name(c) == "equiped"), None) if attach else None
    if node is None:
        return None
    return [[prefab.path_to(n).split("/equiped", 1)[-1].lstrip("/") or "equiped",
             [k for k, _ in prefab.components(n) if k != "Transform"]] for n in _below(prefab, node)]


def _lights(prefab, attach):
    """Lights in the hand, outside the upgrade glow."""
    return [_light(b) | {"node": prefab.name(n)} for n in (_below(prefab, attach) if attach else [])
            for k, b in prefab.components(n) if k == "Light" and not _in_glow(prefab, n)]


def _particles(prefab, attach):
    """[node name, material] of particle systems in the hand, outside the upgrade glow."""
    return [[prefab.name(n), _name(p)] for n in (_below(prefab, attach) if attach else [])
            for k, b in prefab.components(n) if k == "ParticleSystemRenderer" and not _in_glow(prefab, n)
            for p in game.renderer_materials(b)[:1]]


def _ground(prefab):
    """The material of the particle system on the root (the sparkle a dropped item shows), if any."""
    root = prefab.root()
    mats = [p for k, b in prefab.components(root) if k == "ParticleSystemRenderer" for p in game.renderer_materials(b)]
    return _name(mats[0]) if mats else None


def _cloth(prefab):
    """The main settings of each MagicaCloth on the prefab: what kind of cloth, gravity, damping, stiffness, limits."""
    found = []
    for node, kind, body in prefab.all_components():
        if kind.startswith("Script:MagicaCloth") and "clothType:" in body:
            data = items_shared.block(body, "serializeData", 2)
            pick = lambda *names: _nested_number(data, names)
            found.append({"node": prefab.name(node), "cloth_type": pick("clothType"), "gravity": pick("gravity"),
                          "damping": pick("damping", "value"), "radius": pick("radius", "value"),
                          "reduction_m": pick("reductionSetting", "simpleDistance"),
                          "distance_stiffness": pick("distanceConstraint", "stiffness", "value"),
                          "angle_restoration": pick("angleRestorationConstraint", "stiffness", "value"),
                          "angle_limit_deg": pick("angleLimitConstraint", "limitAngle", "value"),
                          "world_inertia": pick("inertiaConstraint", "worldInertia"),
                          "collision_mode": pick("colliderCollisionConstraint", "mode")})
    return found


def _nested_number(text, names):
    at = 0
    for name in names:
        match = re.compile(rf"^\s*{name}: ?(.*)$", re.MULTILINE).search(text, at)
        if not match:
            return None
        at = match.end()
    try:
        return float(match.group(1))
    except ValueError:
        return None


def _light(body):
    return {"type": int(unity.field(body, "m_Type") or 0),
            "color": [round(c, 3) for c in unity.numbers(unity.field(body, "m_Color"))[:3]],
            "intensity": float(unity.field(body, "m_Intensity") or 0), "range": float(unity.field(body, "m_Range") or 0)}


def _below(prefab, node):
    """The node and everything under it."""
    found, stack = [], [node]
    while stack:
        current = stack.pop()
        found.append(current)
        stack.extend(prefab.children(current))
    return found


def _in_glow(prefab, node):
    return "UpgraderGlow" in prefab.path_to(node) or "Upgrade" in prefab.path_to(node)


def _name(path):
    return path.rsplit("/", 1)[-1].rsplit(".", 1)[0] if path else None
