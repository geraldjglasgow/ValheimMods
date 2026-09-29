"""Meshes of building pieces in the frame the game places them in: the prefab root's position and rotation are dropped
(the builder's placement replaces them), its scale kept, as workshop.prefab.load does. Unity axes, metres.

    m = pieces_mesh.to_root(prefab, node)            # 4x4, node space -> the piece's own frame
    parts = pieces_mesh.parts(mesh_ref)              # positions, uv, [(submesh, triangles)] of a .asset or built-in
    pieces_mesh.surface(parts, m, submesh)           # (uv area, world area) of one submesh moved by m

Pieces lean on Unity's built-in meshes (the cube for planks and low LODs, the quad for thatch cards and cloth), so the
cube, quad and plane are rebuilt here with Unity's own layout; the sphere and cylinder only give triangle counts.
"""
import functools
import re

import numpy as np

import game
from workshop import unity

BUILTIN_TRIANGLES = {"10202": 12, "10206": 80, "10207": 768, "10208": 832, "10209": 200, "10210": 2}
BUILTIN_NAMES = {"10202": "Cube", "10206": "Cylinder", "10207": "Sphere", "10208": "Capsule", "10209": "Plane",
                 "10210": "Quad"}


def local_matrix(prefab, node):
    """4x4 of a transform's local position, rotation (x, y, z, w) and scale."""
    body = prefab.docs[node][1]
    x, y, z, w = unity.numbers(unity.field(body, "m_LocalRotation")) or (0, 0, 0, 1)
    rotation = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                         [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                         [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
    matrix = np.eye(4)
    matrix[:3, :3] = rotation * np.array(unity.numbers(unity.field(body, "m_LocalScale")) or (1, 1, 1))
    matrix[:3, 3] = unity.numbers(unity.field(body, "m_LocalPosition")) or (0, 0, 0)
    return matrix


def parent(prefab, node):
    """The node's parent transform, None at the root."""
    father = unity.ref(unity.field(prefab.docs[node][1], "m_Father"))[0]
    return father if prefab.is_local(father) else None


def to_root(prefab, node):
    """node space -> the piece's frame: every local matrix up to the root, then only the root's scale."""
    matrix = np.eye(4)
    while parent(prefab, node) is not None:
        matrix = local_matrix(prefab, node) @ matrix
        node = parent(prefab, node)
    scale = unity.numbers(unity.field(prefab.docs[node][1], "m_LocalScale")) or (1, 1, 1)
    return np.diag(list(scale) + [1.0]) @ matrix


def mesh_ref(value):
    """A renderer's or collider's m_Mesh value -> ('asset', path), ('builtin', fileID) or None."""
    file_id, guid = unity.ref(value)
    if guid == unity.BUILTIN_MESHES:
        return ("builtin", file_id)
    path = game.path_of(value)
    return ("asset", path) if path else None


def triangles(ref):
    """Triangle count of a mesh reference."""
    if ref is None:
        return 0
    if ref[0] == "builtin":
        return BUILTIN_TRIANGLES.get(ref[1], 0)
    return game.mesh_stats(ref[1])["triangles"]


@functools.lru_cache(maxsize=None)
def parts(ref):
    """(positions, uv or None, [(submesh index, (m, 3) triangles)]) of a mesh reference, None when it cannot be read."""
    if ref is None:
        return None
    if ref[0] == "builtin":
        return _builtin(ref[1])
    try:
        positions, uv, _ = game.mesh_arrays(ref[1])
    except (ValueError, IndexError, KeyError):
        return None
    return positions, uv, _submeshes(ref[1])


def _submeshes(path):
    """[(submesh index, triangles)] from the index buffer, split by each submesh's firstByte and indexCount."""
    text = unity.read(path)
    wide = re.search(r"^\s*m_IndexFormat: 1", text, re.MULTILINE) is not None
    raw = bytes.fromhex(re.search(r"^\s*m_IndexBuffer: (\w*)", text, re.MULTILINE).group(1))
    index = np.frombuffer(raw, dtype=np.uint32 if wide else np.uint16).astype(np.int64)
    size = 4 if wide else 2
    heads = re.findall(r"firstByte: (\d+)\s+indexCount: (\d+)\s+topology: (\d+)\s+baseVertex: (\d+)", text)
    found = []
    for n, (first, count, topology, base) in enumerate(heads):
        if topology == "0":
            start, count = int(first) // size, int(count) // 3 * 3
            found.append((n, index[start:start + count].reshape(-1, 3) + int(base)))
    return found


def _builtin(file_id):
    """Unity's cube (1 m, each face its own 0-1 square), quad (1 m, XY) and plane (10 m, XZ, 0-1 across)."""
    if file_id == "10210":
        positions = np.array([[-.5, -.5, 0], [.5, -.5, 0], [-.5, .5, 0], [.5, .5, 0]], float)
        return positions, np.array([[0, 0], [1, 0], [0, 1], [1, 1]], float), [(0, np.array([[0, 3, 1], [3, 0, 2]]))]
    if file_id == "10209":
        grid = np.linspace(-5, 5, 11)
        xs, zs = np.meshgrid(grid, grid)
        positions = np.stack([xs.ravel(), np.zeros(121), zs.ravel()], axis=1)
        uv = np.stack([(xs.ravel() + 5) / 10, (zs.ravel() + 5) / 10], axis=1)
        quads = [(r * 11 + c, r * 11 + c + 1, (r + 1) * 11 + c, (r + 1) * 11 + c + 1)
                 for r in range(10) for c in range(10)]
        tris = np.array([t for a, b, c, d in quads for t in ((a, c, b), (b, c, d))])
        return positions, uv, [(0, tris)]
    if file_id == "10202":
        return _cube()
    return None


def _cube():
    """The unit cube as six quads, each with the full 0-1 square."""
    positions, uv, tris = [], [], []
    for axis in range(3):
        for sign in (-0.5, 0.5):
            u, v = [a for a in range(3) if a != axis]
            base = len(positions)
            for du, dv in ((-.5, -.5), (.5, -.5), (-.5, .5), (.5, .5)):
                point = [0.0, 0.0, 0.0]
                point[axis], point[u], point[v] = sign, du, dv
                positions.append(point)
                uv.append([du + .5, dv + .5])
            tris += [(base, base + 3, base + 1), (base, base + 2, base + 3)]
    return np.array(positions), np.array(uv), [(0, np.array(tris))]


def moved(positions, matrix):
    """Positions moved by a 4x4 matrix."""
    return positions @ matrix[:3, :3].T + matrix[:3, 3]


def surface(mesh_parts, matrix, submesh=None, tiling=(1.0, 1.0)):
    """(uv area times the material's tiling, world area in square metres) of one submesh (every one when None)."""
    positions, uv, groups = mesh_parts
    tris = [t for n, t in groups if submesh is None or n == submesh]
    if uv is None or not tris or not len(np.concatenate(tris)):
        return 0.0, 0.0
    tris = np.concatenate(tris)
    world_points = moved(positions, matrix)
    corner = world_points[tris[:, 0]]
    edge1, edge2 = world_points[tris[:, 1]] - corner, world_points[tris[:, 2]] - corner
    world = np.linalg.norm(np.cross(edge1, edge2), axis=1).sum() / 2
    a, b = uv[tris[:, 1]] - uv[tris[:, 0]], uv[tris[:, 2]] - uv[tris[:, 0]]
    area = np.abs(a[:, 0] * b[:, 1] - a[:, 1] * b[:, 0]).sum() / 2 * abs(tiling[0] * tiling[1])
    return float(area), float(world)


def bounds(points):
    """(low, high) of an (n, 3) array, or None when it is empty."""
    if points is None or not len(points):
        return None
    return points.min(axis=0), points.max(axis=0)
