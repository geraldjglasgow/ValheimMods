"""The Mossback's hard parts, each weighted as it moves: tusks rigid on the jaw, eyes on the head, claws on the last
bone of each finger and on the toes, a leather belt on the hips with a bone buckle, a hide loincloth front and back
that follows the hips and, lower down, both thighs.

Blender axes: metres, Z up, the creature looks down -Y, its left at +X.
"""
import math

import bmesh
import bpy
from mathutils import Vector

from workshop import gamerig, gamerig_weights
from body import mirror

FINGERS = (("HandIndex3", "HandIndex3_end"), ("HandMiddle3", "HandMiddle3_end"), ("HandRing3", "HandRing3_end"),
           ("HandPinky3", "HandPinky3_end"), ("HandThumb3", "HandThumb3_end"))


def build(rig, looks):
    """Every part, weighted; returns them."""
    parts = []
    for side in ("Left", "Right"):
        parts += [_tusk(rig, side, looks["bone"]), _eye(rig, side, looks["glow"])]
        parts += [_claw(rig, side, bone, end, looks["bone"]) for bone, end in FINGERS]
        parts += [_toe(rig, side, x, looks["bone"]) for x in (-0.04, 0.0, 0.04)]
    parts += [_belt(rig, looks["leather"]), _buckle(rig, looks["bone"])]
    parts += [_flap(rig, looks["hide"], front) for front in (True, False)]
    return parts


def loft(name, rings, material, cap=True):
    """A closed tube through rings of points (each ring the same count), capped at both ends; one mesh object."""
    mesh = bmesh.new()
    loops = [[mesh.verts.new(p) for p in ring] for ring in rings]
    count = len(rings[0])
    for a, b in zip(loops, loops[1:]):
        for i in range(count):
            mesh.faces.new((a[i], a[(i + 1) % count], b[(i + 1) % count], b[i]))
    if cap:
        mesh.faces.new(list(reversed(loops[0])))
        mesh.faces.new(loops[-1])
    bmesh.ops.recalc_face_normals(mesh, faces=mesh.faces)
    return _object(name, mesh, material)


def ring(centre, along, radius, sides, squash=1.0, turn=0.0):
    """Points round `centre` in the plane across `along`."""
    frame = along.normalized().to_track_quat('Z', 'Y').to_matrix()
    return [centre + frame @ Vector((math.cos(a) * radius, math.sin(a) * radius * squash, 0.0))
            for a in (turn + 2 * math.pi * i / sides for i in range(sides))]


def cone(name, base, tip, radius, material, sides=6, bend=Vector()):
    """A horn, tusk or claw: base to tip, curved by `bend` at its middle, a point at the tip."""
    rings, steps = [], 3
    for k in range(steps):
        t = k / steps
        centre = base.lerp(tip, t) + bend * math.sin(math.pi * t)
        rings.append(ring(centre, tip - base, radius * (1.0 - t) + 0.002, sides))
    rings.append([tip] * sides)
    obj = loft(name, rings, material, cap=True)
    return _merge_tip(obj)


def _tusk(rig, side, material):
    base = mirror((0.075, -0.19, 1.705), side)
    tip = mirror((0.105, -0.235, 1.8), side)
    tusk = cone(f"tusk_{side}", base, tip, 0.022, material, sides=6, bend=mirror((0.012, -0.015, 0.0), side))
    gamerig_weights.rigid(tusk, "Jaw")
    return tusk


def _eye(rig, side, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=5, radius=0.019,
                                         location=mirror((0.052, -0.19, 1.822), side))
    eye = bpy.context.active_object
    eye.name = f"eye_{side}"
    eye.data.materials.append(material)
    gamerig_weights.rigid(eye, "Head")
    return eye


def _claw(rig, side, bone, end, material):
    """A blunt claw at a fingertip, along the last bone, rigid on it."""
    a, b = gamerig.head(rig, side + bone), gamerig.head(rig, side + end)
    along = (b - a).normalized()
    claw = cone(f"claw_{side}{bone}", b - along * 0.015, b + along * 0.03 + Vector((0, 0, -0.012)), 0.014,
                material, sides=5)
    gamerig_weights.rigid(claw, side + bone)
    return claw


def _toe(rig, side, offset, material):
    toe = gamerig.head(rig, side + "ToeBase")
    base = toe + mirror((offset, -0.07, 0.025), side)
    claw = cone(f"toe_{side}{offset:+.2f}", base, base + Vector((0, -0.05, -0.02)), 0.018, material, sides=5)
    gamerig_weights.rigid(claw, side + "ToeBase")
    return claw


def _belt(rig, material):
    """A leather band round the waist, following the pelvis and belly, rigid on the hips."""
    rings = []
    for z, grow in ((0.99, 0.0), (1.035, 0.012), (1.08, 0.0)):
        rings.append([Vector((math.cos(a) * (0.225 + grow), -0.035 + math.sin(a) * (0.2 + grow), z))
                      for a in (2 * math.pi * i / 18 for i in range(18))])
    belt = loft("belt", rings, material, cap=False)
    _solidify(belt, 0.018)
    gamerig_weights.rigid(belt, "Hips")
    return belt


def _buckle(rig, material):
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=0.045, depth=0.025, location=(0, -0.245, 1.035),
                                        rotation=(math.pi / 2, 0, 0))
    buckle = bpy.context.active_object
    buckle.name = "buckle"
    buckle.data.materials.append(material)
    gamerig_weights.rigid(buckle, "Hips")
    return buckle


def _flap(rig, material, front):
    """A hide flap hanging from the belt, front or back, its hem cut ragged: the hips carry the top, both thighs share
    the hem."""
    sign, rows, hem = (-1.0 if front else 1.0), [], (0.0, 0.05, -0.02, 0.06, 0.01, 0.04)
    for k in range(5):
        t = k / 4
        y = sign * (0.215 - 0.03 * t) - 0.035 + (0.0 if front else 0.02)
        width = 0.14 - 0.02 * t
        rows.append([Vector((x * width, y + sign * 0.012 * abs(x), 1.0 - 0.36 * t + (hem[i] if k == 4 else 0.0)))
                     for i, x in enumerate((-1.0, -0.6, -0.2, 0.2, 0.6, 1.0))])
    flap = _grid(f"flap_{'front' if front else 'back'}", rows, material)
    _solidify(flap, 0.012)
    for v in flap.data.vertices:
        t = min(1.0, max(0.0, (1.0 - v.co.z) / 0.36))
        gamerig_weights.fixed(flap, {"Hips": 1.0 - 0.6 * t, "LeftUpLeg": 0.3 * t, "RightUpLeg": 0.3 * t}, where={v.index})
    return flap


def _grid(name, rows, material):
    mesh = bmesh.new()
    verts = [[mesh.verts.new(p) for p in row] for row in rows]
    for a, b in zip(verts, verts[1:]):
        for i in range(len(a) - 1):
            mesh.faces.new((a[i], a[i + 1], b[i + 1], b[i]))
    return _object(name, mesh, material)


def _solidify(obj, thickness):
    modifier = obj.modifiers.new("thick", 'SOLIDIFY')
    modifier.thickness, modifier.offset = thickness, 0.0
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def _object(name, mesh, material):
    data = bpy.data.meshes.new(name)
    mesh.to_mesh(data)
    mesh.free()
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    data.materials.append(material)
    return obj


def _merge_tip(obj):
    """Welds the tip ring (all at one point) into one vertex."""
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    bmesh.ops.remove_doubles(mesh, verts=mesh.verts, dist=1e-5)
    mesh.to_mesh(obj.data)
    mesh.free()
    obj.data.update()
    return obj

