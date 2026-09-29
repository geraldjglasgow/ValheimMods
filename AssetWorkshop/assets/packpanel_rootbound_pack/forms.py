"""Mesh building blocks for the Rootbound Pack: objects from raw data, a subdivided box surface, flat straps swept
between two rails, twisted tubes (the roots), sealant beads lying on a surface (the guck), rings and bands (the
iron), small oriented boxes, per-vertex float attributes and surface probing. Coordinates are metres in the player's
frame (Z up, the wearer faces -Y); model.py moves everything onto the pivot at the end.

The first half is copied from the Trollhide Backpack's forms.py; tube, bead, ring, band and rivet are this pack's.
"""
import math

import bmesh
import bpy
from mathutils import Matrix, Vector, noise
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


def point_values(obj, name, values):
    """A float attribute per vertex that the materials read (wear, ridge, crest: 0 to 1)."""
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


def strap(name, left, right, normals, thickness, material):
    """A flat strap lying between two rails (points along each edge of its underside, in order), standing
    `thickness` out along `normals`. Smooth along its length, sharp along its four long edges."""
    mesh = bmesh.new()
    rings = []
    for l, r, n in zip(left, right, normals):
        rings.append([mesh.verts.new(p) for p in (l, r, r + n * thickness, l + n * thickness)])
    for a, b in zip(rings, rings[1:]):
        for k in range(4):
            mesh.faces.new((a[k], a[(k + 1) % 4], b[(k + 1) % 4], b[k]))
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


# --- paths -----------------------------------------------------------------------------------------------------

def resample(points, step):
    """The polyline through `points` again, with its points `step` apart along it (ends kept)."""
    lengths = [0.0]
    for a, b in zip(points, points[1:]):
        lengths.append(lengths[-1] + (b - a).length)
    count = max(2, round(lengths[-1] / step) + 1)
    out, seg = [], 0
    for i in range(count):
        s = lengths[-1] * i / (count - 1)
        while seg < len(points) - 2 and lengths[seg + 1] < s:
            seg += 1
        span = max(lengths[seg + 1] - lengths[seg], 1e-9)
        out.append(points[seg].lerp(points[seg + 1], min(1.0, (s - lengths[seg]) / span)))
    return out


def smooth(points, passes=2, weight=0.5):
    """Laplacian smoothing that keeps both ends where they are."""
    points = [p.copy() for p in points]
    for _ in range(passes):
        points = [points[0]] + [p.lerp((a + b) / 2, weight) for a, p, b in zip(points, points[1:], points[2:])] \
            + [points[-1]]
    return points


def tangents(points):
    return [(points[min(i + 1, len(points) - 1)] - points[max(i - 1, 0)]).normalized() for i in range(len(points))]


def frames(points):
    """(tangent, normal, binormal) per point, the normal carried along without twisting (parallel transport)."""
    along = tangents(points)
    first = along[0]
    ref = Vector((0.0, 0.0, 1.0)) if abs(first.z) < 0.9 else Vector((1.0, 0.0, 0.0))
    normal = (ref - first * ref.dot(first)).normalized()
    result = []
    for i, t in enumerate(along):
        if i:
            axis = along[i - 1].cross(t)
            if axis.length > 1e-7:
                normal = Matrix.Rotation(along[i - 1].angle(t), 3, axis.normalized()) @ normal
        normal = (normal - t * normal.dot(t)).normalized()
        result.append((t, normal, t.cross(normal)))
    return result


# --- roots -----------------------------------------------------------------------------------------------------

def tube(name, points, radii, material, sides=6, twist=0.0, ridge=0.0, wander=0.0):
    """A root: a tube along `points` with `radii`, closed by a pointed tip at each end. Every other vertex of a
    ring stands out by `ridge` (a fraction of the radius) and the rings turn `twist` radians per metre, give or
    take `wander` radians of noise, so the ridges spiral round it unevenly like strands twisted together. The
    attribute "ridge" is 1 on the ridges, 0 between."""
    verts, ridges, along = [], [], 0.0
    for i, (point, (t, n, b)) in enumerate(zip(points, frames(points))):
        along += (point - points[i - 1]).length if i else 0.0
        turn = twist * along + wander * noise.noise(point * 7.0)
        for k in range(sides):
            angle = 2 * math.pi * k / sides + turn
            high = k % 2 == 0
            r = radii[i] * (1 + ridge if high else 1 - ridge)
            verts.append(point + (n * math.cos(angle) + b * math.sin(angle)) * r)
            ridges.append(1.0 if high else 0.0)
    rings = len(points)
    faces = [(i * sides + k, i * sides + (k + 1) % sides, (i + 1) * sides + (k + 1) % sides, (i + 1) * sides + k)
             for i in range(rings - 1) for k in range(sides)]
    faces += _tips(verts, ridges, points, radii, sides)
    obj = mesh_object(name, verts, faces, material)
    point_values(obj, "ridge", ridges)
    return obj


def _tips(verts, ridges, points, radii, sides):
    """A point beyond each end ring, joined to it by a fan of triangles."""
    along = tangents(points)
    faces = []
    for ring, direction, radius, flip in ((0, -along[0], radii[0], True),
                                          (len(points) - 1, along[-1], radii[-1], False)):
        verts.append(points[ring] + direction * max(radius * 1.4, 0.002))
        ridges.append(0.5)
        tip = len(verts) - 1
        fan = [(ring * sides + k, ring * sides + (k + 1) % sides, tip) for k in range(sides)]
        faces += [f[::-1] for f in fan] if flip else fan
    return faces


# --- guck ------------------------------------------------------------------------------------------------------

BEAD_PROFILE = ((-1.0, 0.0), (0.0, 1.0), (1.0, 0.0))


def bead(name, tree, path, normals, widths, heights, material, sink=0.0015):
    """A bead of sealant lying along `path` (points on the surface of `tree` with their `normals`): a soft ridge
    `widths` wide and `heights` high at each point, its edges sunk into the surface. The attribute "crest" is 1 on
    its top line and 0 at its edges."""
    verts, crest = [], []
    for point, normal, t, width, height in zip(path, normals, tangents(path), widths, heights):
        side = t.cross(normal).normalized()
        for u, rise in BEAD_PROFILE:
            guide = point + side * u * width / 2
            hit, _ = probe(tree, guide + normal * 0.03, -normal)
            base = hit if hit is not None and (hit - guide).length < 0.03 else guide
            verts.append(base + normal * (rise * height - (sink if rise == 0.0 else 0.0)))
            crest.append(1.0 - abs(u))
    count = len(BEAD_PROFILE)
    faces = [(i * count + k, i * count + k + 1, (i + 1) * count + k + 1, (i + 1) * count + k)
             for i in range(len(path) - 1) for k in range(count - 1)]
    obj = mesh_object(name, verts, faces, material)
    if _faces_down(obj, normals[len(normals) // 2]):
        obj.data.flip_normals()
    point_values(obj, "crest", crest)
    return obj


def _faces_down(obj, normal):
    poly = obj.data.polygons[len(obj.data.polygons) // 2]
    return poly.normal.dot(normal) < 0


# --- iron ------------------------------------------------------------------------------------------------------

def ring(name, centre, axes, radii, minor, material, segments=10, sides=4):
    """An iron ring round `centre` in the plane of the unit vectors axes[0] and axes[1] (radii along each: an oval
    when they differ), its bar `minor` thick."""
    u, v = axes
    w = u.cross(v).normalized()
    verts = []
    for i in range(segments):
        a = 2 * math.pi * i / segments
        middle = centre + u * math.cos(a) * radii[0] + v * math.sin(a) * radii[1]
        out = (u * math.cos(a) / radii[0] + v * math.sin(a) / radii[1]).normalized()
        for k in range(sides):
            b = 2 * math.pi * k / sides + math.pi / sides
            verts.append(middle + (out * math.cos(b) + w * math.sin(b)) * minor)
    faces = [(i * sides + k, ((i + 1) % segments) * sides + k, ((i + 1) % segments) * sides + (k + 1) % sides,
              i * sides + (k + 1) % sides) for i in range(segments) for k in range(sides)]
    return mesh_object(name, verts, faces, material)


def band(name, centre, axis, radius, width, material, sides=8):
    """A short iron band round a root: an open cylinder along `axis`, its rims rolled in a little."""
    axis = axis.normalized()
    ref = Vector((0.0, 0.0, 1.0)) if abs(axis.z) < 0.9 else Vector((1.0, 0.0, 0.0))
    n = (ref - axis * ref.dot(axis)).normalized()
    b = axis.cross(n)
    rows = ((-0.5, 0.88), (-0.32, 1.0), (0.32, 1.0), (0.5, 0.88))
    verts = [centre + axis * width * s + (n * math.cos(a) + b * math.sin(a)) * radius * r
             for s, r in rows for a in (2 * math.pi * k / sides for k in range(sides))]
    faces = [(j * sides + k, j * sides + (k + 1) % sides, (j + 1) * sides + (k + 1) % sides, (j + 1) * sides + k)
             for j in range(len(rows) - 1) for k in range(sides)]
    return mesh_object(name, verts, faces, material)


def rivet(name, centre, normal, radius, height, material, sides=6):
    """A round rivet head standing on a surface: a low dome of two rings and a point, its base sunk a little."""
    normal = normal.normalized()
    ref = Vector((0.0, 0.0, 1.0)) if abs(normal.z) < 0.9 else Vector((1.0, 0.0, 0.0))
    n = (ref - normal * ref.dot(normal)).normalized()
    b = normal.cross(n)
    verts = []
    for rise, r in ((-0.002, 1.0), (height * 0.65, 0.72)):
        verts += [centre + normal * rise + (n * math.cos(a) + b * math.sin(a)) * radius * r
                  for a in (2 * math.pi * k / sides for k in range(sides))]
    verts.append(centre + normal * height)
    faces = [(k, (k + 1) % sides, sides + (k + 1) % sides, sides + k) for k in range(sides)]
    faces += [(sides + k, sides + (k + 1) % sides, 2 * sides) for k in range(sides)]
    return mesh_object(name, verts, faces, material)
