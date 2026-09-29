"""Mesh building blocks for the Lox Hauler: objects from raw data, a subdivided box surface (with explicit rows where
the model needs them), flat straps swept between two rails, bars swept along a path, small oriented boxes and
ellipsoids, a per-vertex float attribute and surface probing. Coordinates are metres in the player's frame (Z up, the
wearer faces -Y); model.py moves everything onto the pivot at the end. Adapted from the Trollhide pack's forms.py.
"""
import math

import bmesh
import bpy
from mathutils import Matrix, Vector
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
    """A float attribute per vertex that the materials read."""
    attr = obj.data.attributes.new(name, 'FLOAT', 'POINT')
    attr.data.foreach_set("value", list(values))


def spread(t):
    """Grid spacing in [-1, 1] that crowds the lines towards the ends, so rounded edges keep their curve."""
    return 0.55 * t + 0.45 * math.sin(math.pi * t / 2)


def spread_rows(count):
    return [spread(k / count * 2 - 1) for k in range(count + 1)]


def box_grid(rows):
    """The surface of the cube [-1, 1]^3 as quads; rows holds each axis' grid values from -1 to 1 (spread_rows gives
    the default spacing). Returns (params, faces)."""
    counts = [len(r) - 1 for r in rows]
    index, params, faces = {}, [], []

    def vertex(key):
        if key not in index:
            index[key] = len(params)
            params.append(tuple(rows[a][k] for a, k in enumerate(key)))
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


def smoothstep(low, high, value):
    t = min(1.0, max(0.0, (value - low) / (high - low)))
    return t * t * (3 - 2 * t)


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


def bar_section(half_u, half_w, points=8, power=4.0):
    """A forged bar's cross-section: a square with soft corners (a superellipse), flat faces towards u and w."""
    section = []
    for k in range(points):
        angle = math.pi / points + 2 * math.pi * k / points
        c, s = math.cos(angle), math.sin(angle)
        norm = (abs(c) ** power + abs(s) ** power) ** (1.0 / power)
        section.append((c / norm * half_u, s / norm * half_w))
    return section


def sweep(name, path, section, side, material, jitter=None):
    """A bar along `path` with `section` ((u, w) pairs), u following `side` squared off the path's direction.
    Smooth along the bar, a sharp rim round each flat end cap. `jitter` nudges each ring (forged unevenness)."""
    mesh = bmesh.new()
    rings = []
    for i, point in enumerate(path):
        along = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
        u = (side - along * side.dot(along)).normalized()
        w = along.cross(u)
        shift = jitter(i) if jitter else Vector()
        rings.append([mesh.verts.new(point + shift + u * a + w * b) for a, b in section])
    n = len(section)
    for a, b in zip(rings, rings[1:]):
        for k in range(n):
            mesh.faces.new((a[k], a[(k + 1) % n], b[(k + 1) % n], b[k]))
    mesh.faces.new(rings[0][::-1])
    mesh.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces)
    ends = set(rings[0]) | set(rings[-1])
    return _finish(name, mesh, material, sharp=lambda e: e.verts[0] in ends and e.verts[1] in ends)


def ellipsoid(name, centre, radii, material, segments=8, rings=5, axes=None):
    """A low-poly ellipsoid, smooth shaded; `axes` (three unit vectors) turns it."""
    mesh = bmesh.new()
    bmesh.ops.create_uvsphere(mesh, u_segments=segments, v_segments=rings, radius=1.0)
    turn = Matrix.Identity(3) if axes is None else Matrix((axes[0], axes[1], axes[2])).transposed()
    for vert in mesh.verts:
        local = Vector((vert.co.x * radii[0], vert.co.y * radii[1], vert.co.z * radii[2]))
        vert.co = centre + turn @ local
    return _finish(name, mesh, material, sharp=lambda e: False)


def tuft(name, base, normal, tip, width, material):
    """A lock of fur: a four-sided spike from a small square sunk into the surface at `base` out to `tip`. Open at
    the base (it is inside the fur), faces wound outwards."""
    across = normal.cross(Vector((0.0, 0.0, 1.0)))
    across = across.normalized() if across.length > 1e-3 else Vector((1.0, 0.0, 0.0))
    down = across.cross(normal).normalized()
    centre = base - normal * 0.006
    corners = [centre + (across * a + down * b) * (width / 2) for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    middle = (sum(corners, Vector()) + tip) / 5
    mesh = bmesh.new()
    verts = [mesh.verts.new(c) for c in corners] + [mesh.verts.new(tip)]
    for k in range(4):
        face = [verts[k], verts[(k + 1) % 4], verts[4]]
        normal_k = (face[1].co - face[0].co).cross(face[2].co - face[0].co)
        if normal_k.dot(face[0].co - middle) < 0:
            face.reverse()
        mesh.faces.new(face)
    return _finish(name, mesh, material, sharp=lambda e: False)


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
