"""Mesh building blocks for the Carapace Pack (adapted from the Trollhide Backpack's): objects from raw data, a
subdivided box surface, thick shell plates from a grid of points, flat straps swept between two rails, small oriented
boxes, per-vertex float attributes and surface probing. Coordinates are metres in the player's frame (Z up, the wearer
faces -Y); model.py moves everything onto the pivot at the end.
"""
import math

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree


def mesh_object(name, verts, faces, material, smooth=True):
    data = bpy.data.meshes.new(name)
    data.from_pydata([tuple(v) for v in verts], [], faces)
    data.validate()
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    data.materials.append(material)
    if smooth:
        data.shade_smooth()
    return obj


def add_material(obj, material, faces):
    """A second material on the listed faces (the pipeline bakes every slot into the one atlas)."""
    obj.data.materials.append(material)
    index = len(obj.data.materials) - 1
    for face in faces:
        obj.data.polygons[face].material_index = index


def point_values(obj, name, values):
    """A float attribute per vertex that the materials read (wear: 0 new, 1 scuffed)."""
    attr = obj.data.attributes.new(name, 'FLOAT', 'POINT')
    attr.data.foreach_set("value", list(values))


def spread(t):
    """Grid spacing in [-1, 1] that crowds the lines towards the ends, so rounded edges keep their curve."""
    return 0.55 * t + 0.45 * math.sin(math.pi * t / 2)


def box_grid(counts):
    """The surface of the cube [-1, 1]^3 as quads with counts=(nx, ny, nz) segments; returns (params, faces)."""
    index, params, faces = {}, [], []

    def vertex(key):
        if key not in index:
            index[key] = len(params)
            params.append(tuple(spread(k / n * 2 - 1) for k, n in zip(key, counts)))
        return index[key]

    for axis in range(3):
        ua, va = (axis + 1) % 3, (axis + 2) % 3
        for side in (0, counts[axis]):
            for u in range(counts[ua]):
                for v in range(counts[va]):
                    quad = [vertex(_key(axis, side, ua, uu, va, vv))
                            for uu, vv in ((u, v), (u + 1, v), (u + 1, v + 1), (u, v + 1))]
                    faces.append(quad if side else quad[::-1])
    return params, faces


def _key(axis, side, ua, u, va, v):
    key = [0, 0, 0]
    key[axis], key[ua], key[va] = side, u, v
    return tuple(key)


def rounded(param, power):
    """Pushes a point of the cube's surface onto a superellipsoid: a box with soft, rounded edges."""
    norm = sum(abs(c) ** power for c in param) ** (1.0 / power)
    return tuple(c / norm for c in param)


def edge_wear(param):
    """1 on the cube's edges, 0 in the middle of its faces: the second largest coordinate decides."""
    second = sorted(abs(c) for c in param)[1]
    return max(0.0, min(1.0, (second - 0.55) / 0.4))


def strap(name, left, right, normals, thickness, material, closed=False):
    """A flat strap lying between two rails (points along each edge of its underside, in order), standing
    `thickness` out along `normals`. Smooth along its length, sharp along its four long edges."""
    mesh = bmesh.new()
    rings = []
    for l, r, n in zip(left, right, normals):
        rings.append([mesh.verts.new(p) for p in (l, r, r + n * thickness, l + n * thickness)])
    count = len(rings)
    for i in range(count if closed else count - 1):
        a, b = rings[i], rings[(i + 1) % count]
        for k in range(4):
            mesh.faces.new((a[k], a[(k + 1) % 4], b[(k + 1) % 4], b[k]))
    if not closed:
        mesh.faces.new(rings[0][::-1])
        mesh.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces)
    corner = {v: k for ring in rings for k, v in enumerate(ring)}
    return _finish(name, mesh, material, sharp=lambda e: corner[e.verts[0]] == corner[e.verts[1]])


def oriented_box(name, center, axes, half, material):
    """A box around `center` whose sides follow the three unit vectors `axes`, half sizes `half`, flat shaded."""
    mesh = bmesh.new()
    corners = {}
    for sx in (-1, 1):
        for sy in (-1, 1):
            for sz in (-1, 1):
                offset = axes[0] * sx * half[0] + axes[1] * sy * half[1] + axes[2] * sz * half[2]
                corners[(sx, sy, sz)] = mesh.verts.new(center + offset)
    for quad in _BOX_FACES:
        mesh.faces.new([corners[c] for c in quad])
    bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces)
    return _finish(name, mesh, material, sharp=lambda e: True)


_BOX_FACES = (
    ((-1, -1, -1), (-1, 1, -1), (1, 1, -1), (1, -1, -1)), ((-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1)),
    ((-1, -1, -1), (1, -1, -1), (1, -1, 1), (-1, -1, 1)), ((-1, 1, -1), (-1, 1, 1), (1, 1, 1), (1, 1, -1)),
    ((-1, -1, -1), (-1, -1, 1), (-1, 1, 1), (-1, 1, -1)), ((1, -1, -1), (1, 1, -1), (1, 1, 1), (1, -1, 1)),
)


def _finish(name, mesh, material, sharp):
    for face in mesh.faces:
        face.smooth = True
    for edge in mesh.edges:
        edge.smooth = not sharp(edge)
    data = bpy.data.meshes.new(name)
    mesh.to_mesh(data)
    mesh.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    data.materials.append(material)
    return obj


def surface(objs):
    """A ray-cast tree over the objects as they will be exported (modifiers applied)."""
    depsgraph = bpy.context.evaluated_depsgraph_get()
    verts, polys = [], []
    for obj in objs:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        start = len(verts)
        verts += [obj.matrix_world @ v.co for v in mesh.vertices]
        polys += [[start + i for i in p.vertices] for p in mesh.polygons]
        evaluated.to_mesh_clear()
    return BVHTree.FromPolygons(verts, polys)


def probe(tree, origin, direction):
    """First surface point and its outward normal seen from `origin` along `direction`, or (None, None)."""
    hit, normal, _, _ = tree.ray_cast(Vector(origin), Vector(direction).normalized())
    if hit is None:
        return None, None
    if normal.dot(direction) > 0:
        normal = -normal
    return hit, normal


def plate(name, rows, normals, thickness, material, values=None):
    """A thick shell plate: `rows` is a grid (rows of points, top row first) of its outer face, `normals` the outward
    normal at each point; the inner face lies `thickness` inside. `values` maps an attribute name to a grid of floats
    (the same on both faces). Smooth over each face, sharp where the faces meet the rim."""
    mesh = bmesh.new()
    count, width = len(rows), len(rows[0])
    layers = {key: mesh.verts.layers.float.new(key) for key in (values or {})}    # before any vertex exists
    outer = [[mesh.verts.new(p) for p in row] for row in rows]
    inner = [[mesh.verts.new(p - n * thickness) for p, n in zip(row, nrm)] for row, nrm in zip(rows, normals)]
    for key, grid in (values or {}).items():
        layer = layers[key]
        for layer_rows in (outer, inner):
            for verts, vals in zip(layer_rows, grid):
                for vert, value in zip(verts, vals):
                    vert[layer] = value
    for j in range(count - 1):
        for i in range(width - 1):
            mesh.faces.new((outer[j][i], outer[j + 1][i], outer[j + 1][i + 1], outer[j][i + 1]))
            mesh.faces.new((inner[j][i], inner[j][i + 1], inner[j + 1][i + 1], inner[j + 1][i]))
    rim = set()
    loop = _outline(count, width)
    for (j0, i0), (j1, i1) in zip(loop, loop[1:] + loop[:1]):
        rim.add(mesh.faces.new((outer[j0][i0], inner[j0][i0], inner[j1][i1], outer[j1][i1])))
    bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces)
    return _finish(name, mesh, material, sharp=lambda e: sum(f in rim for f in e.link_faces) == 1)


def _outline(count, width):
    """The grid's border as (row, column) pairs, once round."""
    top = [(0, i) for i in range(width)]
    right = [(j, width - 1) for j in range(1, count)]
    bottom = [(count - 1, i) for i in range(width - 2, -1, -1)]
    left = [(j, 0) for j in range(count - 2, 0, -1)]
    return top + right + bottom + left


def grid_normals(points, outward):
    """Normals of a grid of points from its neighbours, each turned to agree with `outward` (same shape, vectors)."""
    count, width = len(points), len(points[0])
    result = []
    for j in range(count):
        row = []
        for i in range(width):
            across = points[j][min(i + 1, width - 1)] - points[j][max(i - 1, 0)]
            down = points[min(j + 1, count - 1)][i] - points[max(j - 1, 0)][i]
            normal = across.cross(down).normalized()
            row.append(normal if normal.dot(outward[j][i]) > 0 else -normal)
        result.append(row)
    return result


def tree_from(verts, faces):
    """A ray-cast tree over raw points and faces (a guide surface that is never part of the scene)."""
    return BVHTree.FromPolygons([tuple(v) for v in verts], faces)
