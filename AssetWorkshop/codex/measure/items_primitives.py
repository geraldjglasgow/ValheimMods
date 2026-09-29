"""Unity's built-in meshes (cube, cylinder, sphere, capsule, plane, quad) as some game items draw them (the Hoe's handle
is a stretched cylinder), so they can be measured beside the game's own mesh files. Their triangle counts are Unity's;
their vertices are coarse stand-ins with the same bounds, good for sizes and outlines.

    items_primitives.mesh_ref(renderer_or_filter_body)  # 'builtin:cylinder', a Mesh .asset path, or None
    items_primitives.arrays(path)                       # (positions, uv or None, triangles) for either kind
    items_primitives.stats(path)                        # like game.mesh_stats
"""
import functools

import numpy as np

import game
from workshop import unity

BUILTIN = {"10202": "cube", "10206": "cylinder", "10207": "sphere", "10208": "capsule", "10209": "plane",
           "10210": "quad"}
TRIANGLES = {"cube": 12, "cylinder": 80, "sphere": 768, "capsule": 832, "plane": 200, "quad": 2}


def mesh_ref(prefab, node, body):
    """The mesh a renderer draws: a Mesh .asset path, 'builtin:<kind>' for Unity's own, or None."""
    path = game.renderer_mesh(prefab, node, body)
    if path:
        return path
    source = body if "m_Mesh:" in body else next((b for k, b in prefab.components(node) if k == "MeshFilter"), "")
    file_id, guid = unity.ref(unity.field(source, "m_Mesh"))
    return f"builtin:{BUILTIN[file_id]}" if guid == unity.BUILTIN_MESHES and file_id in BUILTIN else None


def arrays(path):
    return _builtin(path[8:]) if path.startswith("builtin:") else game.mesh_arrays(path)


def stats(path):
    if not path.startswith("builtin:"):
        return game.mesh_stats(path)
    positions, _, _ = _builtin(path[8:])
    size = positions.max(axis=0) - positions.min(axis=0)
    return {"triangles": TRIANGLES[path[8:]], "vertices": len(positions), "submeshes": 1, "bones": 0,
            "size": [round(float(s), 4) for s in size]}


@functools.lru_cache(maxsize=None)
def _builtin(kind):
    """Coarse vertices with the primitive's bounds (Unity: cube 1 m; cylinder and capsule 1 m across, 2 m tall on
    Y; sphere 1 m; plane 10 m on XZ; quad 1 m on XY). The cube keeps Unity's uv (each face the whole texture)."""
    if kind == "cube":
        return _box()
    if kind in ("plane", "quad"):
        half = 5.0 if kind == "plane" else 0.5
        corners = np.array([[-1, 0, -1], [1, 0, -1], [1, 0, 1], [-1, 0, 1]], float) * half
        if kind == "quad":
            corners = corners[:, [0, 2, 1]]
        uv = np.array([[0, 0], [1, 0], [1, 1], [0, 1]], float)
        return corners, uv, np.array([[0, 1, 2], [0, 2, 3]])
    return _round_solid(1.0 if kind == "sphere" else 2.0, kind != "cylinder")


def _box():
    """The unit cube as six separate faces, each mapped to the whole texture like Unity's."""
    faces, uvs, tris = [], [], []
    for axis in range(3):
        for sign in (-0.5, 0.5):
            u, v = [a for a in range(3) if a != axis]
            quad = np.zeros((4, 3))
            quad[:, axis] = sign
            quad[:, u], quad[:, v] = [-0.5, 0.5, 0.5, -0.5], [-0.5, -0.5, 0.5, 0.5]
            base = len(faces) * 4
            faces.append(quad)
            uvs.append([[0, 0], [1, 0], [1, 1], [0, 1]])
            tris += [[base, base + 1, base + 2], [base, base + 2, base + 3]]
    return np.concatenate(faces), np.array(uvs, float).reshape(-1, 2), np.array(tris)


def _round_solid(height, rounded, sides=16, rings=8):
    """A 1 m wide solid of the given height on Y: a sphere or capsule (rounded ends) or a cylinder, as rings."""
    heights = np.linspace(-height / 2, height / 2, rings + 1)
    angles = np.linspace(0, 2 * np.pi, sides, endpoint=False)
    points = []
    for y in heights:
        cap = max(0.0, abs(y) - (height / 2 - 0.5)) if rounded else 0.0
        radius = np.sqrt(max(0.25 - cap * cap, 0.0))
        points += [[radius * np.cos(a), y, radius * np.sin(a)] for a in angles]
    tris = []
    for ring in range(rings):
        for side in range(sides):
            a, b = ring * sides + side, ring * sides + (side + 1) % sides
            tris += [[a, b, b + sides], [a, b + sides, a + sides]]
    return np.array(points), None, np.array(tris)
