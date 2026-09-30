"""Low-poly mesh helpers for the Deathsquito Queen: rings lofted along a centre line (with a material per face when
wanted), tapered tubes and spikes, squashed knobs, Catmull-Rom curves. Everything is built in world space (metres, Z
up, front -Y), so the pipeline's join needs no transforms."""
import math

import bmesh
import bpy
from mathutils import Vector

UP = Vector((0.0, 0.0, 1.0))


def mesh_object(name, verts, faces, material, smooth=True, face_materials=None):
    """An object from raw vertices and faces. face_materials: one material per face (overrides `material`)."""
    data = bpy.data.meshes.new(name)
    data.from_pydata([tuple(v) for v in verts], [], faces)
    data.validate()
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    mats = [material] if face_materials is None else list(dict.fromkeys(face_materials))
    for mat in mats:
        data.materials.append(mat)
    if face_materials is not None:
        data.polygons.foreach_set('material_index', [mats.index(m) for m in face_materials])
    data.polygons.foreach_set('use_smooth', [smooth] * len(data.polygons))
    return obj


def frames(points, hint=UP):
    """(tangent, side, up) per point of a polyline, square to the line and without twist towards `hint`."""
    result = []
    for i in range(len(points)):
        tangent = (points[min(i + 1, len(points) - 1)] - points[max(i - 1, 0)]).normalized()
        up = hint - tangent * hint.dot(tangent)
        if up.length < 0.2:                       # a line nearly along the hint: square it to Y instead
            up = Vector((0.0, 1.0, 0.0)) - tangent * tangent.y
        up.normalize()
        result.append((tangent, tangent.cross(up).normalized(), up))
    return result


def ring(centre, side, up, rx, ry, sides, phase=0.0, ry_below=None):
    """`sides` points round `centre` on an ellipse: rx along `side`, ry along `up` (ry_below under the middle)."""
    below = ry if ry_below is None else ry_below
    points = []
    for k in range(sides):
        a = phase + 2 * math.pi * k / sides
        points.append(centre + side * (rx * math.cos(a)) + up * ((ry if math.sin(a) >= 0 else below) * math.sin(a)))
    return points


def loft(name, rings, material, cap_start=True, cap_end=True, smooth=True, face_materials=None, cap_material=None):
    """Quads between consecutive rings (equal point counts), band by band, k = 0.. round each; caps close the ends with
    a centre point. face_materials lists one material per quad in that order; caps take cap_material."""
    sides, verts, faces = len(rings[0]), [p for r in rings for p in r], []
    for i in range(len(rings) - 1):
        a, b = i * sides, (i + 1) * sides
        faces += [(a + k, a + (k + 1) % sides, b + (k + 1) % sides, b + k) for k in range(sides)]
    for cap, base, flip in ((cap_start, 0, True), (cap_end, (len(rings) - 1) * sides, False)):
        if cap:
            verts.append(sum(rings[base // sides], Vector()) / sides)
            fan = [(base + k, base + (k + 1) % sides, len(verts) - 1) for k in range(sides)]
            faces += [tuple(reversed(f)) for f in fan] if flip else fan
    if _faces_inward(rings):
        faces = [tuple(reversed(f)) for f in faces]
    if face_materials is not None:
        face_materials = list(face_materials) + [cap_material or material] * (len(faces) - len(face_materials))
    return mesh_object(name, verts, faces, material, smooth, face_materials)


def _faces_inward(rings):
    """True when the first quad's normal points towards its ring's centre (the ring runs the other way round)."""
    a0, a1, b0 = rings[0][0], rings[0][1], rings[1][0]
    centre = sum(rings[0], Vector()) / len(rings[0])
    return (a1 - a0).cross(b0 - a0).dot((a0 + a1) / 2 - centre) < 0


def tube(name, points, radii, material, sides=6, hint=UP, **caps):
    """A loft along `points`; radii[i] is a radius or (rx, ry): rx across the line, ry towards the hint."""
    rings = []
    for p, (_, side, up), r in zip(points, frames(points, hint), radii):
        rx, ry = (r, r) if isinstance(r, (int, float)) else r
        rings.append(ring(p, side, up, rx, ry, sides))
    return loft(name, rings, material, **caps)


def taper(points, root, tip, power=1.0):
    """Radii from root to tip along the points, by arc length."""
    lengths = [0.0]
    for a, b in zip(points, points[1:]):
        lengths.append(lengths[-1] + (b - a).length)
    return [root + (tip - root) * (s / lengths[-1]) ** power for s in lengths]


def spike(name, root, direction, length, radius, material, curl=Vector(), sides=5, steps=4):
    """A curved cone from `root` along `direction`, bending towards `curl` (metres at the tip), ending in a point."""
    direction = direction.normalized()
    points = [root + direction * (length * t) + curl * (t * t) for t in (i / steps for i in range(steps + 1))]
    radii = [max(radius * (1.0 - i / steps) ** 0.85, 0.002) for i in range(steps + 1)]
    return tube(name, points, radii, material, sides, hint=_across(direction), cap_start=False)


def _across(direction):
    return UP if abs(direction.dot(UP)) < 0.9 else Vector((0.0, 1.0, 0.0))


def knob(name, centre, radii, material, segments=8, rings=5, smooth=True):
    """A squashed low-poly sphere: radii (x, y, z)."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=segments, v_segments=rings, radius=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * radii[0], v.co.y * radii[1], v.co.z * radii[2])) + Vector(centre)
    data = bpy.data.meshes.new(name)
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    data.materials.append(material)
    data.polygons.foreach_set('use_smooth', [smooth] * len(data.polygons))
    return obj


def catmull(points, per_span):
    """A Catmull-Rom curve through `points` (open), `per_span` samples between each pair; ends are kept."""
    points = [Vector(p) for p in points]
    pts = [points[0]] + points + [points[-1]]
    out = []
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for s in range(per_span):
            t = s / per_span
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t))
    out.append(points[-1])
    return out


def mirrored(point, side):
    """The point for one side: side 1 is +X (the creature's left, as it faces -Y), -1 its right."""
    return Vector((point[0] * side, point[1], point[2]))
