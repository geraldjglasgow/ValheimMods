"""Transforms and shape measures for items: where a prefab's meshes sit in the frame of its `attach` child, which is the
frame the hand holds (VisEquipment.AttachItem parents a copy of `attach` to the hand's attach point at zero position
and rotation; the copy keeps the attach's own scale, the prefab root's is lost). Unity axes throughout, metres.

    m = items_geometry.frame_matrix(prefab, node, attach)   # node's space -> the attach frame
    points = items_geometry.vertices(prefab, node, mesh, m)
    items_geometry.shape(points)                            # extents, long axis, where the fist sits, the edge side
"""
import numpy as np

import items_primitives
from workshop import unity

AXES = "XYZ"


def local_matrix(prefab, node):
    """4x4 of a transform's local position, rotation and scale."""
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
    father = unity.ref(unity.field(prefab.docs[node][1], "m_Father"))[0]
    return father if prefab.is_local(father) else None


def frame_matrix(prefab, node, frame):
    """node's space -> frame's space as the game shows it: the local matrices from node up to frame, then only the
    frame's own scale (its position and rotation are replaced by the joint's). frame None: up to the root, the root's
    scale included (the dropped item)."""
    matrix = np.eye(4)
    while node is not None and node != frame:
        matrix = local_matrix(prefab, node) @ matrix
        node = parent(prefab, node)
    if frame is not None:
        scale = unity.numbers(unity.field(prefab.docs[frame][1], "m_LocalScale")) or (1, 1, 1)
        matrix = np.diag(list(scale) + [1.0]) @ matrix
    return matrix


def vertices(mesh_path, matrix):
    """The mesh's vertex positions moved by matrix, as an (n, 3) array."""
    positions, _, _ = items_primitives.arrays(mesh_path)
    return positions @ matrix[:3, :3].T + matrix[:3, 3]


def surface(mesh_path, matrix, texture_px):
    """(texture pixel area, world area in square metres) of a mesh moved by matrix: texel density is the square root
    of their ratio. texture_px is (width, height)."""
    positions, uv, tris = items_primitives.arrays(mesh_path)
    if uv is None or not len(tris):
        return 0.0, 0.0
    moved = positions @ matrix[:3, :3].T
    world = np.linalg.norm(np.cross(moved[tris[:, 1]] - moved[tris[:, 0]],
                                    moved[tris[:, 2]] - moved[tris[:, 0]]), axis=1).sum() / 2
    a, b = uv[tris[:, 1]] - uv[tris[:, 0]], uv[tris[:, 2]] - uv[tris[:, 0]]
    area = np.abs(a[:, 0] * b[:, 1] - a[:, 1] * b[:, 0]).sum() / 2
    return float(area * texture_px[0] * texture_px[1]), float(world)


def shape(points):
    """How an item sits in its frame: the box, the long axis and which way along it the item reaches from the origin
    (the fist), the fist's place along it (0 = the far end behind the hand, 1 = the tip), the width and thickness
    axes, and the side of the width axis the far third of the item leans to (an axe's edge)."""
    low, high = points.min(axis=0), points.max(axis=0)
    size = high - low
    long_axis = int(np.argmax(size))
    reach = 1 if high[long_axis] >= -low[long_axis] else -1
    others = [a for a in range(3) if a != long_axis]
    width_axis = max(others, key=lambda a: size[a])
    thin_axis = min(others, key=lambda a: size[a])
    return {"box_min": _round(low), "box_max": _round(high), "size": _round(size),
            "long_axis": ("+" if reach > 0 else "-") + AXES[long_axis],
            "fist_at": round(float((0 - low[long_axis]) / size[long_axis]) if reach > 0
                             else float((high[long_axis] - 0) / size[long_axis]), 3),
            "width_axis": AXES[width_axis], "thin_axis": AXES[thin_axis],
            "head_side": _head_side(points, long_axis, reach, width_axis),
            "tilt_deg": _tilt(points, long_axis)}


def _head_side(points, long_axis, reach, width_axis):
    """'+X'/'-X'/'centred': where the far third of the item reaches furthest across the width axis."""
    along = points[:, long_axis] * reach
    far = points[along >= along.min() + (along.max() - along.min()) * 2 / 3]
    across = far[:, width_axis]
    plus, minus = across.max(), -across.min()
    if abs(plus - minus) < 0.15 * max(plus + minus, 1e-6):
        return "centred"
    return ("+" if plus > minus else "-") + AXES[width_axis]


def _tilt(points, long_axis):
    """Degrees between the item's main direction (the principal axis of its vertices) and the long frame axis."""
    centred = points - points.mean(axis=0)
    _, _, vt = np.linalg.svd(centred, full_matrices=False)
    main = vt[0] / np.linalg.norm(vt[0])
    return round(float(np.degrees(np.arccos(min(1.0, abs(main[long_axis]))))), 1)


def profile(points, triangles, shape_, slices=20):
    """Width and thickness of the cross-section at slices along the long axis, from the far end behind the fist to
    the tip: the outline a new item of the kind should follow. [[at 0..1, width m, thickness m, centre across m]]."""
    sign = 1 if shape_["long_axis"][0] == "+" else -1
    long_axis = AXES.index(shape_["long_axis"][1])
    width, thin = AXES.index(shape_["width_axis"]), AXES.index(shape_["thin_axis"])
    along = points[:, long_axis] * sign
    start, length = along.min(), max(along.max() - along.min(), 1e-6)
    edges = np.concatenate([triangles[:, [0, 1]], triangles[:, [1, 2]], triangles[:, [2, 0]]])
    rows = []
    for i in range(slices):
        at = (i + 0.5) / slices
        cut = _section(points, along, edges, start + length * at)
        if not len(cut):
            rows.append([round(at, 3), 0.0, 0.0, 0.0])
            continue
        w = cut[:, width]
        rows.append([round(at, 3), round(float(np.ptp(w)), 3), round(float(np.ptp(cut[:, thin])), 3),
                     round(float((w.max() + w.min()) / 2), 3)])
    return rows


def _section(points, along, edges, level):
    """The points where the mesh's edges cross the plane along == level."""
    a, b = along[edges[:, 0]], along[edges[:, 1]]
    crossing = (a - level) * (b - level) <= 0
    crossing &= a != b
    e = edges[crossing]
    t = ((level - along[e[:, 0]]) / (along[e[:, 1]] - along[e[:, 0]]))[:, None]
    return points[e[:, 0]] + t * (points[e[:, 1]] - points[e[:, 0]])


def _round(vector, places=3):
    return [round(float(v), places) for v in vector]
