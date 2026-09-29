"""Mesh building blocks for the backpack: objects from raw data, a subdivided box surface, flat straps swept between
two rails, round tubes swept along a path, lathed (turned) parts, small oriented boxes, a per-vertex float attribute
and surface probing. Coordinates are metres in the player's frame (Z up, the wearer faces -Y); model.py moves
everything onto the pivot at the end. Adapted from the Trollhide Backpack's forms.py.
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


def frames(points, up=None, closed=False):
    """Parallel-transported (tangent, normal, binormal) along a path, so a swept tube does not twist."""
    count = len(points)
    if closed:
        tangents = [(points[(i + 1) % count] - points[i - 1]).normalized() for i in range(count)]
    else:
        tangents = [(points[min(i + 1, count - 1)] - points[max(i - 1, 0)]).normalized() for i in range(count)]
    hint = up if up is not None else Vector((0.0, 0.0, 1.0))
    if abs(hint.dot(tangents[0])) > 0.95:
        hint = Vector((1.0, 0.0, 0.0))
    normal = (hint - tangents[0] * hint.dot(tangents[0])).normalized()
    result = []
    for tangent in tangents:
        normal = (normal - tangent * normal.dot(tangent)).normalized()
        result.append((tangent, normal, tangent.cross(normal)))
    return result


def tube(name, points, radii, material, sides=6, up=None, closed=False, caps=True, shape=None):
    """A round tube swept along `points` with a radius per point. `shape(i, k, direction)` may return a factor on
    the radius of ring i, vertex k (lumps, tufts). Open tubes get flat caps unless a radius at the end is ~0."""
    verts, faces = [], []
    for i, (point, (tangent, normal, binormal)) in enumerate(zip(points, frames(points, up, closed))):
        for k in range(sides):
            angle = 2 * math.pi * k / sides
            direction = normal * math.cos(angle) + binormal * math.sin(angle)
            factor = shape(i, k, direction) if shape else 1.0
            verts.append(point + direction * radii[i] * factor)
    rings = len(points)
    for i in range(rings if closed else rings - 1):
        j = (i + 1) % rings
        faces += [(i * sides + k, i * sides + (k + 1) % sides, j * sides + (k + 1) % sides, j * sides + k)
                  for k in range(sides)]
    if caps and not closed:
        for ring, flip in ((0, True), (rings - 1, False)):
            loop = [ring * sides + k for k in range(sides)]
            faces.append(loop[::-1] if flip else loop)
    return mesh_object(name, verts, faces, material)     # the winding faces outwards by construction


def lathe(name, profile, centre, axis, material, segments=12, across=None):
    """A turned part: `profile` is (radius, height) from the base outwards, turned about the unit vector `axis`
    through `centre`. A last point of radius 0 closes the top in a point; the base is closed with a flat disc."""
    axis = Vector(axis).normalized()
    hint = across if across is not None else (Vector((1, 0, 0)) if abs(axis.x) < 0.9 else Vector((0, 1, 0)))
    across = (hint - axis * hint.dot(axis)).normalized()
    up = axis.cross(across)                              # right-handed, so the winding faces outwards
    verts, faces = [], []
    rings = [p for p in profile if p[0] > 1e-6]
    for r, h in rings:
        for k in range(segments):
            angle = 2 * math.pi * k / segments
            verts.append(centre + (across * math.cos(angle) + up * math.sin(angle)) * r + axis * h)
    for i in range(len(rings) - 1):
        faces += [(i * segments + k, i * segments + (k + 1) % segments, (i + 1) * segments + (k + 1) % segments,
                   (i + 1) * segments + k) for k in range(segments)]
    last = (len(rings) - 1) * segments
    if profile[-1][0] <= 1e-6:
        verts.append(centre + axis * profile[-1][1])
        faces += [(last + k, last + (k + 1) % segments, len(verts) - 1) for k in range(segments)]
    else:
        faces.append(tuple(last + k for k in range(segments)))
    faces.append(tuple(range(segments))[::-1])
    return mesh_object(name, verts, faces, material)
