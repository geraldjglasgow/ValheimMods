"""Low-poly mesh helpers for the bone battleaxe: rings lofted along a centre line, squashed knobs, flat meshes from
point lists. Everything is built in world space (metres, Z up), so the pipeline's join needs no transforms."""
import math

import bmesh
import bpy
from mathutils import Vector


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


def frames(points, hint=Vector((0.0, 1.0, 0.0))):
    """(tangent, side, up) per point of a polyline: side lies in the plane across the tangent, towards `hint`'s
    perpendicular part, so rings stay square to the line and do not twist."""
    result = []
    for i, point in enumerate(points):
        ahead = points[min(i + 1, len(points) - 1)] - points[max(i - 1, 0)]
        tangent = ahead.normalized()
        up = (hint - tangent * hint.dot(tangent)).normalized()
        result.append((tangent, tangent.cross(up).normalized(), up))
    return result


def ring(centre, side, up, rx, ry, sides, phase=0.0):
    """`sides` points round `centre` on an ellipse: rx along `side`, ry along `up`."""
    return [centre + side * (rx * math.cos(a)) + up * (ry * math.sin(a))
            for a in (phase + 2 * math.pi * k / sides for k in range(sides))]


def loft(name, rings, material, cap_start=True, cap_end=True, smooth=True):
    """Quads between consecutive rings (equal point counts); caps close the ends with a centre point."""
    sides, verts, faces = len(rings[0]), [p for r in rings for p in r], []
    for i in range(len(rings) - 1):
        a, b = i * sides, (i + 1) * sides
        faces += [(a + k, a + (k + 1) % sides, b + (k + 1) % sides, b + k) for k in range(sides)]
    for cap, base, flip in ((cap_start, 0, True), (cap_end, (len(rings) - 1) * sides, False)):
        if cap:
            centre = sum(rings[base // sides], Vector()) / sides
            verts.append(centre)
            fan = [(base + k, base + (k + 1) % sides, len(verts) - 1) for k in range(sides)]
            faces += [tuple(reversed(f)) for f in fan] if flip else fan
    if _faces_inward(rings):
        faces = [tuple(reversed(f)) for f in faces]
    return mesh_object(name, verts, faces, material, smooth)


def _faces_inward(rings):
    """True when the first quad's normal points towards its ring's centre (the ring runs the other way round)."""
    a0, a1, b0 = rings[0][0], rings[0][1], rings[1][0]
    normal = (a1 - a0).cross(b0 - a0)
    centre = sum(rings[0], Vector()) / len(rings[0])
    return normal.dot((a0 + a1) / 2 - centre) < 0


def tube(name, points, radii, material, sides=8, hint=Vector((0.0, 1.0, 0.0)), phase=0.0, **caps):
    """A loft along `points`; radii[i] is (rx, ry): rx across the line in the hint's plane, ry towards the hint."""
    rings = [ring(p, side, up, rx, ry, sides, phase)
             for p, (_, side, up), (rx, ry) in zip(points, frames(points, hint), radii)]
    return loft(name, rings, material, **caps)


def knob(name, centre, radii, material, segments=8, rings=6, smooth=True):
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
    pts = [points[0]] + list(points) + [points[-1]]
    out = []
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for s in range(per_span):
            t = s / per_span
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t))
    out.append(points[-1])
    return out
