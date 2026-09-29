"""The model of one item prefab: what the hand holds (the `attach` child), what the body wears (`attach_skin`,
`attach_<bone>`), what lies on the ground, and each one's triangles, materials, textures, texel density and shape.

    parts = items_model.parts(prefab)          # {"held": [...], "worn": [...], "drop": [...]}
    items_model.measure(prefab, parts["held"], frame)
"""
import os
import re

import numpy as np

import game
import items_geometry
import items_primitives
from workshop import unity

RENDERERS = ("MeshRenderer", "SkinnedMeshRenderer")
HIDDEN_SHADERS = ("WaterMask", "ShadowBlob", "Invis", "DepthWrite", "Distortion")


def frames(prefab):
    """{name: node} of the root's children the game attaches: attach, attach_skin, attach_back, attach_<bone>,
    equipoffset."""
    found = {}
    for child in prefab.children(prefab.root()):
        name = prefab.name(child)
        if name.startswith("attach") or name == "equipoffset":
            found.setdefault(name, child)
    return found


def parts(prefab):
    """Renderer nodes by role: held (under attach), worn (attach_skin and attach_<bone>), back (attach_back) and drop
    (what shows on the ground: everything active outside the attach_* children, plus attach itself)."""
    found = frames(prefab)
    held = _renderers(prefab, found["attach"], True) if "attach" in found else []
    worn = [r for name, node in found.items() if name.startswith("attach_") and name != "attach_back"
            for r in _renderers(prefab, node, False)]
    back = _renderers(prefab, found["attach_back"], False) if "attach_back" in found else []
    skip = {node for name, node in found.items() if name.startswith("attach_") or name == "equipoffset"}
    drop = _renderers(prefab, prefab.root(), True, skip)
    return {"held": held, "worn": worn, "back": back, "drop": drop}


def _renderers(prefab, top, active_only, skip=()):
    """[(node, renderer kind, body, mesh path)] drawn at or under top: enabled, on a mesh, not only in a lower LOD,
    not under an inactive GameObject when active_only (a worn part is inactive in the prefab and switched on when
    worn)."""
    lower = _lower_lods(prefab)
    found, stack = [], [top]
    while stack:
        node = stack.pop()
        if node in skip or (active_only and not prefab.active(node)):
            continue
        stack.extend(prefab.children(node))
        for kind, body in prefab.components(node):
            if kind in RENDERERS and unity.field(body, "m_Enabled") != "0" and _own_id(prefab, body) not in lower:
                mesh = items_primitives.mesh_ref(prefab, node, body)
                if mesh and not _hidden(body):
                    found.append((node, kind, body, mesh))
    return found


def _own_id(prefab, body):
    """The fileID of a component body in its prefab."""
    if not hasattr(prefab, "_ids"):
        prefab._ids = {id(b): i for i, (_, b) in prefab.docs.items()}
    return prefab._ids.get(id(body))


def _lower_lods(prefab):
    """fileIDs of renderers a LODGroup lists only below LOD0."""
    first, rest = set(), set()
    for _, body in prefab.docs.values():
        if "m_LODs:" not in body:
            continue
        for index, lod in enumerate(re.split(r"^  - screenRelativeHeight", body, flags=re.MULTILINE)[1:]):
            ids = set(re.findall(r"renderer: \{fileID: (-?\d+)\}", lod))
            (first if index == 0 else rest).update(ids)
    return rest - first


def _hidden(body):
    paths = [p for p in game.renderer_materials(body) if p]
    return bool(paths) and all(any(h in game.material(p)["shader"] for h in HIDDEN_SHADERS) for p in paths)


def measure(prefab, found, frame):
    """Triangles, materials, textures, texel density and shape of renderers in the frame's space (frame None: the
    prefab root's, as the item lies on the ground). Skinned meshes are measured in their bind space."""
    if not found:
        return None
    points, triangles, pixel_area, world_area, materials = [], [], 0.0, 0.0, []
    for node, kind, body, mesh in found:
        matrix = items_geometry.frame_matrix(prefab, node, frame)
        mats = [p for p in game.renderer_materials(body)]
        materials += mats
        px = _main_texture_px(mats)
        if px:
            area = items_geometry.surface(mesh, matrix, px)
            pixel_area, world_area = pixel_area + area[0], world_area + area[1]
        moved = items_geometry.vertices(mesh, matrix)
        triangles.append(items_primitives.arrays(mesh)[2] + sum(len(p) for p in points))
        points.append(moved)
    points, triangles = np.concatenate(points), np.concatenate(triangles)
    result = _summary(found, materials)
    result["texel_density"] = round((pixel_area / world_area) ** 0.5, 1) if world_area > 0 else None
    result["shape"] = items_geometry.shape(points)
    result["profile"] = items_geometry.profile(points, triangles, result["shape"])
    return result


def _summary(found, materials):
    """Counts and materials of the renderers: triangles, vertices, submeshes, meshes, and each material's shader and
    textures."""
    stats = [items_primitives.stats(mesh) for _, _, _, mesh in found]
    unique = list(dict.fromkeys(p for p in materials if p))
    return {"triangles": sum(s["triangles"] for s in stats), "vertices": sum(s["vertices"] for s in stats),
            "submeshes": sum(s["submeshes"] for s in stats), "renderers": len(found),
            "skinned": any(kind == "SkinnedMeshRenderer" for _, kind, _, _ in found),
            "bones": max(s["bones"] for s in stats),
            "meshes": [mesh for _, _, _, mesh in found],
            "materials": [describe_material(p) for p in unique],
            "texture_px": max((m["texture_px"] or 0 for m in (describe_material(p) for p in unique)), default=0)}


def describe_material(path):
    """{path, shader, maps: {slot: [w, h] and file}, texture_px (the albedo's longest side)} of a .mat."""
    info = game.material(path)
    maps = {slot: {"file": tex, "size": list(game.texture_size(tex) or (0, 0))}
            for slot, tex in info["textures"].items() if tex}
    main = maps.get("_MainTex", {}).get("size") or [0, 0]
    return {"path": path, "name": info["name"], "shader": info["shader"], "maps": maps,
            "texture_px": max(main) or None,
            "colors": {k: [round(c, 3) for c in v] for k, v in info["colors"].items() if k in ("_Color", "_EmissionColor")},
            "floats": {k: v for k, v in info["floats"].items() if k in ("_Metallic", "_Glossiness", "_MetalGloss",
                                                                       "_BumpScale", "_Cutoff", "_EmissionStrength")}}


def _main_texture_px(materials):
    """(width, height) of the first material's albedo, or None."""
    for path in materials:
        if path:
            tex = game.material(path)["textures"].get("_MainTex")
            size = game.texture_size(tex) if tex else None
            if size:
                return size
    return None


def colliders(prefab):
    """The prefab's colliders ([kind, node path, size or radius, convex]) and its root Rigidbody's settings."""
    found = []
    for node, kind, body in prefab.all_components():
        if kind.endswith("Collider") and kind != "CharacterController":
            found.append({"kind": kind, "node": prefab.path_to(node).split("/", 1)[-1],
                          "size": _collider_size(kind, body), "enabled": unity.field(body, "m_Enabled") != "0",
                          "convex": unity.field(body, "m_Convex") == "1" if kind == "MeshCollider" else None})
    rigid = [b for kind, b in prefab.components(prefab.root()) if kind == "Rigidbody"]
    body = rigid[0] if rigid else ""
    return found, ({"mass": float(unity.field(body, "m_Mass") or 0), "drag": float(unity.field(body, "m_Drag") or 0),
                    "angular_drag": float(unity.field(body, "m_AngularDrag") or 0),
                    "collision_detection": int(unity.field(body, "m_CollisionDetection") or 0)} if body else None)


def _collider_size(kind, body):
    if kind == "BoxCollider":
        return [round(v, 3) for v in unity.numbers(unity.field(body, "m_Size"))]
    if kind in ("SphereCollider", "CapsuleCollider"):
        return [round(float(unity.field(body, "m_Radius") or 0), 3), round(float(unity.field(body, "m_Height") or 0), 3)]
    return None


def name_of(path):
    return os.path.splitext(os.path.basename(path))[0]
