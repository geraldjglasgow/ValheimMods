"""One of the game's effect prefabs in the codex's words: its particle systems (vfx_modules), lights and the game
scripts that drive them (LightFlicker, LightLod), trails and lines, meshes, timers, camera shakes, sounds and network
views, plus the prefabs nested in it.

    import vfx_effect
    fx = vfx_effect.effect("Effects/Fire/fx_Torch_Basic.prefab")
    fx["systems"], fx["lights"], fx["scripts"], fx["components"]
"""
import functools

import game
import vfx_modules
from vfx_read import LIGHT_TYPES, minmax_curve, parse

UNITY_FIELDS = {"m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset", "m_GameObject",
                "m_Enabled", "m_EditorHideFlags", "m_Script", "m_Name", "m_EditorClassIdentifier"}


@functools.lru_cache(maxsize=None)
def effect(path, depth=0):
    """Everything an effect prefab draws and runs, and the same for each prefab nested in it (two levels deep)."""
    prefab = game.prefab(path)
    out = {"prefab": path, "name": path.rsplit("/", 1)[-1][:-7], "systems": [], "lights": [], "scripts": [],
           "trails": [], "lines": [], "meshes": [], "components": {}, "nodes": len(prefab.nodes()), "nested": []}
    for node in prefab.nodes():
        _read_node(prefab, node, out)
    if depth < 2:
        out["nested"] = [effect(n, depth + 1) for n in prefab.nested() if n.endswith(".prefab")]
    return out


def effect_subtree(path, subtree):
    """Like effect(), for one GameObject of a big prefab and everything under it ('_Environment/Rain'): the weather
    systems live in the environment prefab, one group per weather."""
    prefab = game.prefab(path)
    top = next(n for n in prefab.nodes() if prefab.path_to(n) == subtree)
    nodes, stack = [], [top]
    while stack:
        node = stack.pop()
        nodes.append(node)
        stack.extend(reversed(prefab.children(node)))
    out = {"prefab": path, "subtree": subtree, "name": subtree.rsplit("/", 1)[-1], "systems": [], "lights": [],
           "scripts": [], "trails": [], "lines": [], "meshes": [], "components": {}, "nodes": len(nodes), "nested": []}
    for node in nodes:
        _read_node(prefab, node, out)
    return out


def _read_node(prefab, node, out):
    kinds = prefab.components(node)
    by_kind = {kind: body for kind, body in kinds}
    for kind, body in kinds:
        out["components"][kind] = out["components"].get(kind, 0) + 1
        if kind == "ParticleSystem":
            out["systems"].append(vfx_modules.system(prefab, node, body, by_kind.get("ParticleSystemRenderer")))
        elif kind == "Light":
            out["lights"].append(light(prefab, node, body))
        elif kind in ("TrailRenderer", "LineRenderer"):
            out["trails" if kind == "TrailRenderer" else "lines"].append(line(prefab, node, kind, body))
        elif kind in ("MeshRenderer", "SkinnedMeshRenderer"):
            out["meshes"].append(mesh(prefab, node, body))
        elif not kind.startswith(("Transform", "ParticleSystemRenderer", "MeshFilter", "class")) and kind[0].isupper():
            if kind not in vfx_modules_native():
                out["scripts"].append(script(prefab, node, kind, body))


@functools.lru_cache(maxsize=None)
def vfx_modules_native():
    return frozenset(game.NATIVE.values())


def light(prefab, node, body):
    """A Light: type, colour, intensity, range, spot angle, shadows, render mode, and the game scripts beside it."""
    fields = parse(body)
    colour = fields.get("m_Color") or {}
    shadows = (fields.get("m_Shadows") or {}).get("m_Type", 0.0)
    return {"node": prefab.path_to(node), "active": prefab.active(node), "enabled": fields.get("m_Enabled") == 1.0,
            "type": LIGHT_TYPES[int(fields.get("m_Type", 2))], "colour": [round(colour.get(c, 0.0), 3) for c in "rgb"],
            "intensity": fields.get("m_Intensity"), "range": fields.get("m_Range"), "spot_angle": fields.get("m_SpotAngle"),
            "shadows": ("none", "hard", "soft")[int(shadows)], "render_mode": int(fields.get("m_RenderMode", 0)),
            "bounce": fields.get("m_BounceIntensity"), "transform": vfx_modules.transform(prefab, node)}


def line(prefab, node, kind, body):
    """A TrailRenderer or LineRenderer: time, width curve, colour gradient, texture mode, material."""
    fields = parse(body)
    params = fields.get("m_Parameters") or {}
    materials = game.renderer_materials(body)
    return {"node": prefab.path_to(node), "kind": kind, "time": fields.get("m_Time"),
            "min_vertex_distance": fields.get("m_MinVertexDistance"), "width": params.get("widthMultiplier"),
            "width_curve": minmax_curve({"minMaxState": 1.0, "scalar": 1.0, "maxCurve": params.get("widthCurve") or {}}),
            "colour": _gradient(params.get("colorGradient")), "texture_mode": params.get("textureMode"),
            "alignment": params.get("alignment"), "corner_vertices": params.get("numCornerVertices"),
            "cap_vertices": params.get("numCapVertices"),
            "material": vfx_modules.material_summary(materials[0]) if materials and materials[0] else None}


def _gradient(g):
    from vfx_read import gradient
    return gradient(g) if isinstance(g, dict) and "key0" in g else None


def mesh(prefab, node, body):
    """A plain mesh in an effect (a shockwave ring, a decal, debris): its mesh size and material."""
    path = game.renderer_mesh(prefab, node, body)
    materials = game.renderer_materials(body)
    stats = game.mesh_stats(path) if path and path.endswith(".asset") else None
    return {"node": prefab.path_to(node), "mesh": path, "triangles": stats and stats["triangles"],
            "size": stats and stats["size"], "transform": vfx_modules.transform(prefab, node),
            "materials": [vfx_modules.material_summary(m) for m in materials if m]}


def script(prefab, node, kind, body):
    """A game script (LightFlicker, TimedDestruction, CamShaker, ZSFX, ...) and its serialized settings."""
    fields = parse(body)
    settings = {k: v for k, v in fields.items() if k not in UNITY_FIELDS and not isinstance(v, (list, dict))}
    lists = {k: len(v) for k, v in fields.items() if isinstance(v, list)}
    return {"node": prefab.path_to(node), "class": kind, "enabled": fields.get("m_Enabled") == 1.0,
            "settings": settings, "lists": lists}


def all_systems(fx):
    """The effect's systems and those of every nested prefab."""
    return fx["systems"] + [s for n in fx["nested"] for s in all_systems(n)]


def all_of(fx, key):
    return fx[key] + [x for n in fx["nested"] for x in all_of(n, key)]
